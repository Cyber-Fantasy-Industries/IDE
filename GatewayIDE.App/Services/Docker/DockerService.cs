using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnitConfig = GatewayIDE.App.Views.Docker.UnitConfig;


namespace GatewayIDE.App.Services.Docker
{
    public enum DesktopStatus { Open, Closed, NotInstalled, Unknown }
    public enum ContainerStatus { NotFound, Running, Exited, Unknown }

    public static class DockerService
    {
        // Image-Name muss zu deinem Build/Compose passen.
        // (Wenn du später umbenennst, hier nachziehen.)
        private const string GatewayImage = "deploy-gateway:latest";

        // Standard: Compose liegt im Repo-Root und heißt docker-compose.yml
        private const string DefaultComposeFile = "docker-compose.yml";

        private static string ComposePath()
        {
            // Optionaler Override (z.B. "compose.dev.yml" oder "deploy/compose.yml")
            // Relative Pfade werden relativ zum RepoRoot interpretiert (weil WorkingDirectory=RepoRoot)
            var env = Environment.GetEnvironmentVariable("GATEWAY_COMPOSE_PATH");
            return string.IsNullOrWhiteSpace(env) ? DefaultComposeFile : env.Trim();
        }

        private static bool HasAnyComposeFile(string dir)
        {
            // Root-Marker: Compose-Standardnamen
            return File.Exists(Path.Combine(dir, "docker-compose.yml")) ||
                   File.Exists(Path.Combine(dir, "docker-compose.yaml")) ||
                   File.Exists(Path.Combine(dir, "compose.yml")) ||
                   File.Exists(Path.Combine(dir, "compose.yaml"));
        }

        private static string FindRepoRoot()
        {
            // Kandidaten: BaseDirectory (bei published exe wichtig) + CurrentDirectory (bei dev)
            var candidates = new[] { AppContext.BaseDirectory, Environment.CurrentDirectory };

            foreach (var start in candidates)
            {
                var dir = Path.GetFullPath(start);

                while (!string.IsNullOrEmpty(dir))
                {
                    // Root-Marker (deploy ist KEIN Marker mehr)
                    var hasSln = File.Exists(Path.Combine(dir, "GatewayIDE.sln"));
                    var hasGit = Directory.Exists(Path.Combine(dir, ".git"));
                    var hasCompose = HasAnyComposeFile(dir);

                    if (hasSln || hasGit || hasCompose)
                        return dir;

                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                }
            }

            // Fallback
            return Directory.GetCurrentDirectory();
        }

        private static string _cachedRepoRoot = string.Empty;

        private static string RepoRoot()
        {
            if (!string.IsNullOrEmpty(_cachedRepoRoot) && Directory.Exists(_cachedRepoRoot))
                return _cachedRepoRoot;

            _cachedRepoRoot = FindRepoRoot();
            return _cachedRepoRoot;
        }

        // ---- low-level runner (streamt stdout/err, wartet auf Exit) ----
        private static async Task<int> RunAsync(
            string file,
            string args,
            Action<string>? onOut = null,
            Action<string>? onErr = null,
            CancellationToken ct = default)
        {
            var psi = new ProcessStartInfo(file, args)
            {
                WorkingDirectory = RepoRoot(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

            p.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    onOut?.Invoke(e.Data + Environment.NewLine);
            };

            p.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    onErr?.Invoke(e.Data + Environment.NewLine);
            };

            if (!p.Start())
                throw new InvalidOperationException($"Prozessstart fehlgeschlagen: {file} {args}");

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            try
            {
                // Wartet auf Exit oder Cancellation
                await p.WaitForExitAsync(ct).ConfigureAwait(false);
                return p.ExitCode;
            }
            catch (OperationCanceledException)
            {
                // Best effort kill
                try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }

                // Warten ohne ct (damit wir sauber aufräumen)
                try { await p.WaitForExitAsync().ConfigureAwait(false); } catch { /* ignore */ }

                return -1; // Signal: abgebrochen
            }
        }

        // ----------------------------
        // Compose helpers
        // ----------------------------
        private static string BuildEnvFileArg(string? envFile)
        {
            if (string.IsNullOrWhiteSpace(envFile))
                return "";

            // docker compose --env-file "<path>"
            return $" --env-file \"{envFile.Trim()}\"";
        }

        private static async Task<(int rc, string stdout)> RunCaptureAsync(
            string file,
            string args,
            Action<string>? onErr = null,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder();
            var rc = await RunAsync(file, args, s => sb.Append(s), onErr, ct).ConfigureAwait(false);
            return (rc, sb.ToString());
        }

        private static async Task<int> ComposeAsync(
            string argsWithoutDockerPrefix,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            return await RunAsync(
                "docker",
                $"compose -f \"{ComposePath()}\" {argsWithoutDockerPrefix}",
                o, e, ct).ConfigureAwait(false);
        }

        private static async Task<string?> ResolveContainerIdAsync(
            UnitConfig u,
            CancellationToken ct = default)
        {
            var composeFile = string.IsNullOrWhiteSpace(u.ComposeFile) ? ComposePath() : u.ComposeFile.Trim();
            var service = u.ServiceName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(service))
                return null;

            var envArg = BuildEnvFileArg(u.EnvFile);

            var profileArg = string.IsNullOrWhiteSpace(u.ComposeProfile)
                ? ""
                : $" --profile \"{u.ComposeProfile.Trim()}\"";

            // ✅ kein -p erzwingen; nur wenn explizit gesetzt
            var projectArg = string.IsNullOrWhiteSpace(u.ProjectName)
                ? ""
                : $" -p \"{u.ProjectName.Trim()}\"";

            // ✅ liefert Container-ID (egal wie der Container heißt)
            var args = $"compose{envArg} -f \"{composeFile}\"{profileArg}{projectArg} ps -q {service}";
            var (rc, outText) = await RunCaptureAsync("docker", args, null, ct).ConfigureAwait(false);
            if (rc != 0) return null;

            var id = outText.Trim();
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }

        // ✅ ADD: compose up -d <service> mit env-file (Compose v2: --env-file)
        // docker compose --env-file "<envFile>" -f "<compose>" up -d <service>
        public static Task<int> ComposeUpWithEnvFileAsync(
            string envFile,
            string serviceName,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("serviceName fehlt.", nameof(serviceName));

            // Kein env-file? -> normales compose up
            if (string.IsNullOrWhiteSpace(envFile))
                return ComposeAsync($"up -d {serviceName}", o, e, ct);

            return RunAsync(
                "docker",
                $"compose --env-file \"{envFile}\" -f \"{ComposePath()}\" up -d {serviceName}",
                o, e, ct);
        }

        // ✅ ADD: optionales Down mit env-file (praktisch für Stop/Reset)
        public static Task<int> ComposeDownWithEnvFileAsync(
            string envFile,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(envFile))
                return ComposeAsync("down", o, e, ct);

            return RunAsync(
                "docker",
                $"compose --env-file \"{envFile}\" -f \"{ComposePath()}\" down",
                o, e, ct);
        }

        // =======================================================
        // compose up/down with --profile support (Compose v2)
        // =======================================================

        public static Task<int> ComposeUpProfileAsync(
            string profile,
            string serviceName,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(profile))
                throw new ArgumentException("profile fehlt.", nameof(profile));
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("serviceName fehlt.", nameof(serviceName));

            return RunAsync(
                "docker",
                $"compose --profile \"{profile.Trim()}\" -f \"{ComposePath()}\" up -d {serviceName.Trim()}",
                o, e, ct);
        }

        public static Task<int> ComposeDownProfileAsync(
            string profile,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(profile))
                throw new ArgumentException("profile fehlt.", nameof(profile));

            return RunAsync(
                "docker",
                $"compose --profile \"{profile.Trim()}\" -f \"{ComposePath()}\" down",
                o, e, ct);
        }

        // ✅ convenience wrappers
        public static Task<int> NetworkUpDevAsync(Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUpProfileAsync("dev", "network-dev", o, e, ct);

        public static Task<int> NetworkUpProdAsync(Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUpProfileAsync("prod", "network-prod", o, e, ct);

        public static Task<int> NetworkDownDevAsync(Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeDownProfileAsync("dev", o, e, ct);

        public static Task<int> NetworkDownProdAsync(Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeDownProfileAsync("prod", o, e, ct);

        // ✅ Unit compose: -f + (optional --profile) + (optional -p) + args + (optional service)
        private static Task<int> ComposeUnitAsync(
            UnitConfig u,
            string argsWithoutService,
            bool appendServiceName,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (u == null) throw new ArgumentNullException(nameof(u));

            var composeFile = string.IsNullOrWhiteSpace(u.ComposeFile) ? ComposePath() : u.ComposeFile.Trim();
            var service = u.ServiceName?.Trim() ?? "";

            var envArg = BuildEnvFileArg(u.EnvFile);

            // ✅ optional compose profile (e.g. "dev")
            var profileArg = string.IsNullOrWhiteSpace(u.ComposeProfile)
                ? ""
                : $" --profile \"{u.ComposeProfile.Trim()}\"";

            // ✅ KEIN -p erzwingen (nur wenn explizit gesetzt)
            var projectArg = string.IsNullOrWhiteSpace(u.ProjectName)
                ? ""
                : $" -p \"{u.ProjectName.Trim()}\"";

            var cmdArgs = $"compose{envArg} -f \"{composeFile}\"{profileArg}{projectArg} {argsWithoutService}";
            if (appendServiceName && !string.IsNullOrWhiteSpace(service))
                cmdArgs += $" {service}";

            return RunAsync("docker", cmdArgs, o, e, ct);
        }

        private static string EscapeForBash(string s)
            => (s ?? "").Replace("\"", "\\\"");

        // ----------------------------
        // Status (Host + Container)
        // ----------------------------
        public static async Task<DesktopStatus> GetDockerDesktopStatusAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            // 1) Schnelltest: docker info
            var rc = await RunAsync("docker", "info", o, e, ct).ConfigureAwait(false);
            if (rc == 0) return DesktopStatus.Open;

            // 2) Windows Service check (Docker Desktop Backend)
            if (OperatingSystem.IsWindows())
            {
                var sb = new StringBuilder();
                var rc2 = await RunAsync(
                    "sc",
                    "query com.docker.service",
                    s => sb.AppendLine(s),
                    e,
                    ct).ConfigureAwait(false);

                if (rc2 == 0)
                {
                    var txt = sb.ToString();
                    if (txt.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0)
                        return DesktopStatus.Open;
                    if (txt.IndexOf("STOPPED", StringComparison.OrdinalIgnoreCase) >= 0)
                        return DesktopStatus.Closed;

                    return DesktopStatus.Closed; // konservativ
                }

                return DesktopStatus.NotInstalled;
            }

            return DesktopStatus.Unknown;
        }

        public static async Task<bool> IsImageAvailableAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            var rc = await RunAsync("docker", $"image inspect {GatewayImage}", o, e, ct).ConfigureAwait(false);
            return rc == 0;
        }

        // ✅ Generic container status (by container name or id)
        public static async Task<ContainerStatus> GetContainerStatusAsync(
            string containerName,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                return ContainerStatus.NotFound;

            var sb = new StringBuilder();
            int rc = await RunAsync(
                "docker",
                $"inspect -f \"{{{{.State.Status}}}}\" {containerName.Trim()}",
                s => sb.Append(s),
                _ => { },
                ct).ConfigureAwait(false);

            if (rc != 0) return ContainerStatus.NotFound;

            var status = sb.ToString().Trim().ToLowerInvariant();
            return status switch
            {
                "running" => ContainerStatus.Running,
                "exited" => ContainerStatus.Exited,
                "created" => ContainerStatus.Exited,
                "dead" => ContainerStatus.Exited,
                _ => ContainerStatus.Unknown
            };
        }

        // ✅ Unit status (uses ContainerName if present, else resolve via compose)
        public static async Task<ContainerStatus> GetUnitStatusAsync(
            UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            // 1) Override (falls ihr ausnahmsweise doch mal Namen setzt)
            if (!string.IsNullOrWhiteSpace(u.ContainerName))
                return await GetContainerStatusAsync(u.ContainerName, o, e, ct).ConfigureAwait(false);

            // 2) Standard: per compose den Container finden
            var id = await ResolveContainerIdAsync(u, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(id))
                return ContainerStatus.NotFound;

            return await GetContainerStatusAsync(id, o, e, ct).ConfigureAwait(false);
        }

        // ----------------------------
        // Unit operations (compose)
        // ----------------------------
        public static Task<int> UnitUpAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "up -d", appendServiceName: true, o, e, ct);

        public static Task<int> UnitStopAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "stop", appendServiceName: true, o, e, ct);

        public static Task<int> UnitDownAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            // down ist project-scope; service name ist hier nicht nötig
            => ComposeUnitAsync(u, "down", appendServiceName: false, o, e, ct);

        public static Task<int> UnitRestartAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "restart", appendServiceName: true, o, e, ct);

        public static Task<int> UnitBuildNoCacheAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "build --no-cache", appendServiceName: true, o, e, ct);

        public static Task<int> UnitTailLogsAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "logs -f", appendServiceName: true, o, e, ct);

        public static Task<int> UnitRemoveContainerAsync(UnitConfig u, Action<string>? o = null, Action<string>? e = null, CancellationToken ct = default)
            => ComposeUnitAsync(u, "rm -f", appendServiceName: true, o, e, ct);

        // ----------------------------
        // Unit exec (docker exec by container id/name)
        // ----------------------------
        public static async Task<int> UnitExecAsync(
            UnitConfig u,
            string command,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            var container = u.ContainerName?.Trim();
            if (string.IsNullOrWhiteSpace(container))
                container = await ResolveContainerIdAsync(u, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(container))
                throw new InvalidOperationException(
                    $"Unit '{u.DisplayName}' hat keinen Container gefunden (ServiceName='{u.ServiceName}')."
                );

            var escaped = EscapeForBash(command);
            return await RunAsync("docker", $"exec {container} bash -lc \"{escaped}\"", o, e, ct).ConfigureAwait(false);
        }

        // ----------------------------
        // Wipe (unit-scoped)
        // ----------------------------

        // ✅ safer wipe: only the unit project (not global system prune)
        public static async Task UnitWipeAsync(
            UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            o?.Invoke($"🧹 [{u.DisplayName}] compose down -v --remove-orphans\n");
            var rc = await ComposeUnitAsync(u, "down -v --remove-orphans", appendServiceName: false, o, e, ct).ConfigureAwait(false);
            if (rc != 0) throw new Exception($"docker compose down fehlgeschlagen (rc={rc}).");
        }

        public static async Task UnitFullRebuildAsync(
            UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            await UnitWipeAsync(u, o, e, ct).ConfigureAwait(false);

            o?.Invoke($"🏗️  [{u.DisplayName}] build --no-cache {u.ServiceName}\n");
            var rc = await UnitBuildNoCacheAsync(u, o, e, ct).ConfigureAwait(false);
            if (rc != 0) throw new Exception($"docker compose build fehlgeschlagen (rc={rc}).");
        }
    }
}
