using System;
using System.Net.Http;
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
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

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

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    var result = JsonConvert.DeserializeObject<UserAgentApiResponse<UserAgentLoginResponse>>(responseBody);
                    return result;
                }

                return new UserAgentApiResponse<UserAgentLoginResponse>
                {
                    status = "error",
                    message = $"Server returned HTTP {(int)httpResponse.StatusCode} ({httpResponse.ReasonPhrase})"
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
