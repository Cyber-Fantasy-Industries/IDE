using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayIDE.App.Services.Network
{
    public sealed class GitHubDeviceFlow
    {
        private readonly HttpClient _http;
        private readonly string _clientId;

        public GitHubDeviceFlow(HttpClient http, string clientId)
        {
            _http = http;
            _clientId = clientId;
        }

        public async Task<DeviceCodeResponse> StartAsync(CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/device/code");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _clientId,
                ["scope"] = "read:user"
            });

            using var resp = await _http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            resp.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<DeviceCodeResponse>(json, JsonOpts())!;
        }

        public async Task<TokenResponse> PollTokenAsync(string deviceCode, int intervalSeconds, CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                using var req = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _clientId,
                    ["device_code"] = deviceCode,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                });

                using var resp = await _http.SendAsync(req, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                resp.EnsureSuccessStatusCode();

                var token = JsonSerializer.Deserialize<TokenResponse>(json, JsonOpts())!;
                if (!string.IsNullOrWhiteSpace(token.access_token))
                    return token;

                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
            }
        }

        public async Task<GhUser> GetUserAsync(string accessToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.UserAgent.ParseAdd("GatewayIDE");

            using var resp = await _http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            resp.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<GhUser>(json, JsonOpts())!;
        }

        private static JsonSerializerOptions JsonOpts() => new()
        {
            PropertyNameCaseInsensitive = true
        };

        public sealed class DeviceCodeResponse
        {
            public string device_code { get; set; } = "";
            public string user_code { get; set; } = "";
            public string verification_uri { get; set; } = "";
            public string verification_uri_complete { get; set; } = "";
            public int expires_in { get; set; }
            public int interval { get; set; }
        }

        public sealed class TokenResponse
        {
            public string access_token { get; set; } = "";
            public string token_type { get; set; } = "";
            public string scope { get; set; } = "";

            public string error { get; set; } = "";
            public string error_description { get; set; } = "";
        }

        public sealed class GhUser
        {
            public long id { get; set; }
            public string login { get; set; } = "";
        }
    }
}
