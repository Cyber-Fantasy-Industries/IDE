using System;
using System.IO;
using System.Security.Cryptography;
using NSec.Cryptography;

namespace GatewayIDE.App.Services.Network
{
    public static class WireGuardKeys
    {
        public static void EnsureForUser(string userKey, out string publicKeyB64)
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GatewayIDE", "network", userKey
            );
            Directory.CreateDirectory(dir);

            var pubPath  = Path.Combine(dir, "wg_public.b64");
            var privPath = Path.Combine(dir, "wg_private.dpapi");

            if (File.Exists(pubPath) && File.Exists(privPath))
            {
                publicKeyB64 = File.ReadAllText(pubPath).Trim();
                return;
            }

            // Generate X25519 keypair (WireGuard-compatible)
            var algorithm = KeyAgreementAlgorithm.X25519;
            using var key = new Key(algorithm, new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            });

            var privRaw = key.Export(KeyBlobFormat.RawPrivateKey);    // 32 bytes
            var pubRaw  = key.PublicKey.Export(KeyBlobFormat.RawPublicKey); // 32 bytes

            publicKeyB64 = Convert.ToBase64String(pubRaw);

            // DPAPI is Windows-only → guard the call so analyzer is happy.
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("DPAPI (ProtectedData) is only supported on Windows.");

            var protectedPriv = ProtectedData.Protect(
                privRaw,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser
            );

            File.WriteAllText(pubPath, publicKeyB64);
            File.WriteAllBytes(privPath, protectedPriv);
        }
    }
}
