using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Apollo
{
    public partial class MnemonicSearchWindow : Window
    {
        private readonly HttpClient httpClient;
        private string accessToken = null;
        private readonly string authorizationCode;

        public MnemonicSearchWindow(string authorizationCode)
        {
            InitializeComponent();
            httpClient = new HttpClient();
            this.authorizationCode = authorizationCode;
        }

        private readonly string mnemonicsFolderPath = "mnemonics";

        private void EnsureMnemonicsFolderExists()
        {
            if (!Directory.Exists(mnemonicsFolderPath))
            {
                Directory.CreateDirectory(mnemonicsFolderPath);
            }
        }

        private void SaveFormattedMnemonicResponse(string mnemonic, string jsonResponse)
        {
            string mnemonicsFolderPath = "mnemonics";
            if (!Directory.Exists(mnemonicsFolderPath))
            {
                Directory.CreateDirectory(mnemonicsFolderPath);
            }

            string filePath = Path.Combine(mnemonicsFolderPath, $"{mnemonic}.json");

            var jsonDocument = JsonDocument.Parse(jsonResponse);
            string formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(filePath, formattedJson);
        }

        private async void SearchMnemonic_Click(object sender, RoutedEventArgs e)
        {
            string mnemonic = mnemonicInput.Text.Trim();
            if (string.IsNullOrEmpty(mnemonic))
            {
                mnemonicResultTextBox.Text = "Enter a mnemonic.";
                return;
            }

            mnemonicResultTextBox.Text = "Searching...";

            try
            {
                if (string.IsNullOrEmpty(accessToken))
                {
                    accessToken = await GetAccessTokenAsync(authorizationCode);
                }

                string result = await SearchMnemonicAsync(mnemonic, accessToken);

                mnemonicResultTextBox.Text = result == "Not found!"
                    ? "Mnemonic not found!"
                    : "Mnemonic found!";

                SaveFormattedMnemonicResponse(mnemonic, result);
            }
            catch (Exception ex)
            {
                mnemonicResultTextBox.Text = $"Error: {ex.Message}";
            }
        }

        private async Task<string> GetAccessTokenAsync(string code)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code)
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://account-public-service-prod.ol.epicgames.com/account/api/oauth/token")
            {
                Content = requestContent
            };

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "M2Y2OWU1NmM3NjQ5NDkyYzhjYzI5ZjFhZjA4YThhMTI6YjUxZWU5Y2IxMjIzNGY1MGE2OWVmYTY3ZWY1MzgxMmU=");

            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JsonDocument json = JsonDocument.Parse(responseBody);
                return json.RootElement.GetProperty("access_token").GetString();
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get access token. Error: {errorContent}");
            }
        }

        private async Task<string> SearchMnemonicAsync(string mnemonic, string accessToken)
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await httpClient.GetAsync($"https://links-public-service-live.ol.epicgames.com/links/api/fn/mnemonic/{mnemonic}/related");

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                return data;
            }
            return "Not found!";
        }
    }
}
