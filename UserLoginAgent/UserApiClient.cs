using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UserLoginAgent
{
    /// <summary>
    /// REST HTTP Client for UserLoginAgent communications with PCAccess server.
    /// WHAT: Sends login credentials and ping requests to UserAgentAPIController.
    /// REASON: Zero external dependencies; leverages native System.Net.Http and Newtonsoft.Json.
    /// </summary>
    public static class UserApiClient
    {
        private static readonly HttpClient _httpClient;

        static UserApiClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Set browser-like User-Agent and JSON headers to prevent IIS / WAF / Cloudflare 403 Forbidden blocks
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 PCAccessUserLoginAgent/1.0");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<bool> PingAsync(string serverUrl)
        {
            try
            {
                string url = serverUrl.TrimEnd('/') + "/UserAgentAPI/Ping";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<UserAgentApiResponse<UserAgentLoginResponse>> LoginAsync(string serverUrl, string usernameOrEmail, string password)
        {
            try
            {
                string url = serverUrl.TrimEnd('/') + "/UserAgentAPI/Login";
                var req = new UserAgentLoginRequest
                {
                    username_or_email = usernameOrEmail,
                    password = password,
                    client_machine = Environment.MachineName
                };

                string jsonContent = JsonConvert.SerializeObject(req);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage httpResponse = await _httpClient.PostAsync(url, content);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                // Validate HTTP status code before attempting JSON deserialization
                if (!httpResponse.IsSuccessStatusCode)
                {
                    string hint = "";
                    int code = (int)httpResponse.StatusCode;
                    if (code == 403)
                    {
                        hint = " (Access forbidden: Server security / firewall blocked the request)";
                    }
                    else if (code == 404)
                    {
                        hint = " (Endpoint /UserAgentAPI/Login not found on server)";
                    }
                    else if (code >= 500)
                    {
                        hint = " (Server internal error occurred)";
                    }

                    return new UserAgentApiResponse<UserAgentLoginResponse>
                    {
                        status = "error",
                        message = $"Server returned HTTP {code} ({httpResponse.ReasonPhrase}){hint}"
                    };
                }

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<UserAgentApiResponse<UserAgentLoginResponse>>(responseBody);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    catch (JsonReaderException)
                    {
                        return new UserAgentApiResponse<UserAgentLoginResponse>
                        {
                            status = "error",
                            message = $"Server returned invalid response format (not valid JSON). HTTP {(int)httpResponse.StatusCode}"
                        };
                    }
                }

                return new UserAgentApiResponse<UserAgentLoginResponse>
                {
                    status = "error",
                    message = $"Server returned empty response (HTTP {(int)httpResponse.StatusCode})"
                };
            }
            catch (Exception ex)
            {
                return new UserAgentApiResponse<UserAgentLoginResponse>
                {
                    status = "error",
                    message = $"Connection failed: {ex.Message}"
                };
            }
        }
    }
}
