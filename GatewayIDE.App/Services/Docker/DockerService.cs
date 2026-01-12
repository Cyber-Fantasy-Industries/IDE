using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ✅ UnitConfig: z.B. in GatewayIDE.App.ViewModels
// public sealed class UnitConfig
// {
//     public string Id { get; init; } = "";
//     public string DisplayName { get; init; } = "";
//     public string ComposeFile { get; init; } = "docker-compose.yml";
//     public string ProjectName { get; init; } = "";
//     public string ServiceName { get; init; } = "";
//     public string ContainerName { get; init; } = "";
// }

namespace GatewayIDE.App.Services.Processes
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
            // Root-Marker: Compose-Standardnamen (Option A)
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
        // Legacy compose (global)
        private static Task<int> ComposeAsync(
            string args,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => RunAsync("docker", $"compose -f \"{ComposePath()}\" {args}", o, e, ct);

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
        // ✅ ADD: compose up/down with --profile support (Compose v2)
        // (Needed for your docker-compose.yml profiles: dev/prod)
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

        // ✅ ADD: convenience wrappers for your docker-compose.yml
        // services: network-dev (profile dev), network-prod (profile prod)
        public static Task<int> NetworkUpDevAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUpProfileAsync("dev", "network-dev", o, e, ct);

        public static Task<int> NetworkUpProdAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUpProfileAsync("prod", "network-prod", o, e, ct);

        public static Task<int> NetworkDownDevAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeDownProfileAsync("dev", o, e, ct);

        public static Task<int> NetworkDownProdAsync(
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeDownProfileAsync("prod", o, e, ct);

        // ✅ Unit compose: -f + (optional --profile) + -p + args + (optional service)
        private static Task<int> ComposeUnitAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            string argsWithoutService,
            bool appendServiceName,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            if (u == null) throw new ArgumentNullException(nameof(u));

            var composeFile = string.IsNullOrWhiteSpace(u.ComposeFile) ? ComposePath() : u.ComposeFile.Trim();
            var project = string.IsNullOrWhiteSpace(u.ProjectName) ? $"gateway-{u.Id}" : u.ProjectName.Trim();
            var service = u.ServiceName?.Trim() ?? "";

            // ✅ NEW: optional compose profile (e.g. "dev")
            var profileArg = string.IsNullOrWhiteSpace(u.ComposeProfile)
                ? ""
                : $" --profile \"{u.ComposeProfile.Trim()}\"";

            var args = $"compose -f \"{composeFile}\"{profileArg} -p \"{project}\" {argsWithoutService}";
            if (appendServiceName && !string.IsNullOrWhiteSpace(service))
                args += $" {service}";

            return RunAsync("docker", args, o, e, ct);
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

        // ✅ Generic container status (by container name)
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

        // ✅ Unit status (uses ContainerName)
        public static Task<ContainerStatus> GetUnitStatusAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => GetContainerStatusAsync(u.ContainerName, o, e, ct);

        // ----------------------------
        // Unit operations (compose)
        // ----------------------------

        public static Task<int> UnitUpAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "up -d", appendServiceName: true, o, e, ct);

        public static Task<int> UnitStopAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "stop", appendServiceName: true, o, e, ct);

        public static Task<int> UnitDownAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            // down ist project-scope; service name ist hier nicht nötig
            => ComposeUnitAsync(u, "down", appendServiceName: false, o, e, ct);

        public static Task<int> UnitRestartAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "restart", appendServiceName: true, o, e, ct);

        public static Task<int> UnitBuildNoCacheAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "build --no-cache", appendServiceName: true, o, e, ct);

        public static Task<int> UnitTailLogsAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "logs -f", appendServiceName: true, o, e, ct);

        public static Task<int> UnitRemoveContainerAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
            => ComposeUnitAsync(u, "rm -f", appendServiceName: true, o, e, ct);

        // ----------------------------
        // Unit exec (docker exec by container name)
        // ----------------------------

        public static Task<int> UnitExecAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            string command,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            var container = u.ContainerName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(container))
                throw new InvalidOperationException($"Unit '{u.DisplayName}' hat keinen ContainerName konfiguriert.");

            var escaped = EscapeForBash(command);
            return RunAsync("docker", $"exec {container} bash -lc \"{escaped}\"", o, e, ct);
        }

        // ----------------------------
        // Wipe (unit-scoped)
        // ----------------------------

        // ✅ safer wipe: only the unit project (not global system prune)
        public static async Task UnitWipeAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
            Action<string>? o = null,
            Action<string>? e = null,
            CancellationToken ct = default)
        {
            o?.Invoke($"🧹 [{u.DisplayName}] compose down -v --remove-orphans\n");
            var rc = await ComposeUnitAsync(u, "down -v --remove-orphans", appendServiceName: false, o, e, ct).ConfigureAwait(false);
            if (rc != 0) throw new Exception($"docker compose down fehlgeschlagen (rc={rc}).");
        }

        public static async Task UnitFullRebuildAsync(
            GatewayIDE.App.ViewModels.UnitConfig u,
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
