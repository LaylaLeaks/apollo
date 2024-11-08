using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Http;
using Apollo.Properties;
using DiscordRPC;
using DiscordRPC.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Apollo
{
    public partial class MainWindow : Window
    {
        private DiscordRpcClient client;
        private Timer timer;
        private CancellationTokenSource allCosmeticsCancellationTokenSource;
        private static readonly HttpClient httpClient = new HttpClient();
        private string accessToken = null;
        private string authorizationCode;

        private bool ChangelogShow;

        private int elapsedTimeSeconds;
        private int consoleLineCount = 0;

        private string userName;
        public MainWindow()
        {
            InitializeComponent();
            InitializeDiscordRPC();
            InitializeUserName();
            CheckAndCreateFolder();

            ChangelogShow = Convert.ToBoolean(ConfigurationManager.AppSettings["ChangelogShow"]);

            if (!ChangelogShow)
            {
                DisplayChangelog();
                UpdateAppConfigChangeLogShown(true);
            }
        }

        // Changelog
        private void UpdateAppConfigChangeLogShown(bool value)
        {
            System.Configuration.Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["ChangelogShow"].Value = value.ToString();
            configuration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void DisplayChangelog()
        {
            string[] changelog = new string[]
            {
            "",
            "Path notes:",
            "",
            "+ Mnemonics",
            "",
            "UPCOMING STUFF:",
            "",
            "• Battle Pass rewards",
            "• Crew History",
            "• Augments",
            "• Vehicle and Weapon Stats",
            ""
            };

            foreach (var entry in changelog)
            {
                DisplayInConsole(entry);
            }
        }

        // Create and Check for folder exists
        private void CheckAndCreateFolder()
        {
            string mappingsFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "mappings");
            string newCosmeticsFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "new_cosmetics");
            string allCosmeticsFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "all_cosmetics");
            string othersFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "others");
            string shopSections = System.IO.Path.Combine(Environment.CurrentDirectory, "shopSections");
            string battlePassImages = System.IO.Path.Combine(Environment.CurrentDirectory, "Battle Pass");

            if (!Directory.Exists(mappingsFolder))
            {
                Directory.CreateDirectory(mappingsFolder);
                DisplayInConsole("Folder for Mappings Created.");
            }

            if (!Directory.Exists(newCosmeticsFolder))
            {
                Directory.CreateDirectory(newCosmeticsFolder);
                DisplayInConsole("Folder for new Cosmetics Created.");
            }

            if (!Directory.Exists(allCosmeticsFolder))
            {
                Directory.CreateDirectory(allCosmeticsFolder);
                DisplayInConsole("Folder for all Cosmetics Created.");
            }

            if (!Directory.Exists(othersFolder))
            {
                Directory.CreateDirectory(othersFolder);
                DisplayInConsole("Folder for another stuff Created.");
            }

            if (!Directory.Exists(shopSections))
            {
                Directory.CreateDirectory(shopSections);
                DisplayInConsole("Folder for shop sections Created.");
            }
        }

        // UserName
        private void InitializeUserName()
        {
            if (string.IsNullOrEmpty(Settings.Default.UserName))
            {
                userName = PromptUserName();

                Settings.Default.UserName = userName;
                Settings.Default.Save();
            }
            else
            {
                userName = Settings.Default.UserName;
            }
            DisplayInConsole($"Thank you for using Apollo, you like Apollo? leave a star <3 | Created by @Layla_Leaks");
            DisplayInConsole($"Welcome {Settings.Default.UserName}");
        }

        private string PromptUserName()
        {
            string userName = null;

            var promptWindow = new UsernamePromptWindow();
            if (promptWindow.ShowDialog() == true)
            {
                userName = promptWindow.UserName;
            }

            return userName;
        }

        // Discord RPC
        private void InitializeDiscordRPC()
        {
            client = new DiscordRpcClient("1251212968625049631");

            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
                DisplayInConsole("Discord RPC Successfully.");
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

            client.Initialize();
            UpdatePresence("Search stuff");
        }

        private void UpdatePresence(string details)
        {
            client.SetPresence(new RichPresence()
            {
                Details = details,
                Assets = new Assets()
                {
                    LargeImageKey = "apollo_logo",
                    LargeImageText = "Apollo 1.4"
                },
                Timestamps = new Timestamps()
                {
                    Start = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(elapsedTimeSeconds))
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            client.Dispose();
            base.OnClosed(e);
        }

        // Buttons event

        // help (dropdown)
        private void Help_Discord_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/3AUtgD8sWy") { UseShellExecute = true });
        }

        // mappings
        private async void Mappings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<MappingInfo> mappings = await FetchMappingsAsync();

                foreach (var mapping in mappings)
                {
                    await DownloadMappingAsync(mapping);
                    DisplayInConsole($"Downloaded: {mapping.fileName}");
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error downloading mappings: {ex.Message}");
            }
        }

        private async Task<List<MappingInfo>> FetchMappingsAsync()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("https://fortnitecentral.genxgames.gg/api/v1/mappings");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<MappingInfo>>(jsonString);
            }
        }

        private async Task DownloadMappingAsync(MappingInfo mapping)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(mapping.url);
                response.EnsureSuccessStatusCode();

                var filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "mappings", mapping.fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                }

            }
        }

        // aeskeys
        private async void AesKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fortnitecentral.genxgames.gg/api/v1/aes");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject aesData = JObject.Parse(responseBody);
                    JArray dynamicKeys = (JArray)aesData["dynamicKeys"];

                    DisplayInConsole("");
                    DisplayInConsole("Decrypted:");
                    foreach (var key in dynamicKeys)
                    {
                        DisplayInConsole($"{key["name"]} | Files: {key["fileCount"]} | Size: {key["size"]["formatted"]}");
                    }

                    JArray unloadedKeys = (JArray)aesData["unloaded"];

                    DisplayInConsole("");
                    DisplayInConsole("Encrypted:");
                    foreach (var key in unloadedKeys)
                    {
                        DisplayInConsole($"{key["name"]} | Files: {key["fileCount"]} | Size: {key["size"]["formatted"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error searching aeskeys: {ex.Message}");
            }
        }

        // new cosmetics
        private async void NewCosmetics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fortnite-api.com/v2/cosmetics/new");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    JObject items = (JObject)json["data"]["items"];

                    Dictionary<string, string> categoryFolders = new Dictionary<string, string>
                    {
                        { "br", "br" },
                        { "instruments", "festival" },
                        { "cars", "rocket_racing" },
                        { "lego", "lego" },
                        { "beans", "fall_guys" }
                    };

                    string baseFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "new_cosmetics");

                    if (!Directory.Exists(baseFolder))
                    {
                        Directory.CreateDirectory(baseFolder);
                        DisplayInConsole("Main folder for new Cosmetics created.");
                    }

                    foreach (var category in categoryFolders)
                    {
                        string categoryName = category.Key;
                        string folderName = category.Value;

                        string categoryFolderPath = System.IO.Path.Combine(baseFolder, folderName);
                        if (!Directory.Exists(categoryFolderPath))
                        {
                            Directory.CreateDirectory(categoryFolderPath);
                            DisplayInConsole($"Folder created for category: {folderName}");
                        }

                        if (items[categoryName] is JArray categoryItems)
                        {
                            foreach (JToken item in categoryItems)
                            {
                                string itemName = (string)item["name"];
                                string itemId = (string)item["id"];

                                string iconUrl = (string)item["images"]["icon"];
                                if (string.IsNullOrEmpty(iconUrl) || !Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                                {
                                    iconUrl = (string)item["images"]["smallIcon"];
                                }
                                if (string.IsNullOrEmpty(iconUrl) || !Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                                {
                                    iconUrl = (string)item["images"]["small"];
                                }

                                if (string.IsNullOrEmpty(iconUrl) || !Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                                {
                                    iconUrl = "https://i.imgur.com/a7T092l.png";
                                }

                                string fileName = $"{itemId}.png";
                                string filePath = System.IO.Path.Combine(categoryFolderPath, fileName);

                                using (var iconResponse = await client.GetAsync(iconUrl))
                                {
                                    iconResponse.EnsureSuccessStatusCode();
                                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                    {
                                        await iconResponse.Content.CopyToAsync(fileStream);
                                    }
                                }

                                DisplayInConsole($"Downloaded icon for: {itemName} (ID: {itemId}) in category: {folderName}");
                            }
                        }
                        else
                        {
                            DisplayInConsole($"No items found for category: {categoryName}");
                        }
                    }

                    DisplayInConsole("All cosmetic icons downloaded successfully.");
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error downloading cosmetic icons: {ex.Message}");
            }
        }


        // all cosemtics
        private async void AllCosmetics_Click(object sender, RoutedEventArgs e)
        {
            if (allCosmeticsCancellationTokenSource != null && !allCosmeticsCancellationTokenSource.Token.IsCancellationRequested)
            {
                allCosmeticsCancellationTokenSource.Cancel();
                allCosmeticsCancellationTokenSource = null;
                DisplayInConsole("Download cancelled.");
                return;
            }

            allCosmeticsCancellationTokenSource = new CancellationTokenSource();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fortnite-api.com/v2/cosmetics/br");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseBody);
                    JArray items = (JArray)json["data"];
                    string allCosmeticsFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "all_cosmetics");
                    if (!Directory.Exists(allCosmeticsFolder))
                    {
                        Directory.CreateDirectory(allCosmeticsFolder);
                        DisplayInConsole("Folder for all Cosmetics Created.");
                    }
                    foreach (JToken item in items)
                    {
                        if (allCosmeticsCancellationTokenSource == null || allCosmeticsCancellationTokenSource.Token.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }

                        string itemName = (string)item["name"];
                        string itemId = (string)item["id"];
                        string iconUrl = (string)item["images"]["icon"];
                        if (string.IsNullOrEmpty(iconUrl))
                        {
                            iconUrl = "https://i.imgur.com/a7T092l.png";
                        }
                        else if (!Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                        {
                            Uri baseUri = new Uri("https://fortnite-api.com");
                            iconUrl = new Uri(baseUri, iconUrl).AbsoluteUri;
                        }
                        string fileName = $"{itemName}_{itemId}.png";
                        string filePath = System.IO.Path.Combine(allCosmeticsFolder, fileName);
                        using (HttpClient httpClient = new HttpClient())
                        {
                            using (HttpResponseMessage imageResponse = await httpClient.GetAsync(iconUrl))
                            {
                                using (Stream imageStream = await imageResponse.Content.ReadAsStreamAsync())
                                {
                                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                                    {
                                        await imageStream.CopyToAsync(fileStream);
                                    }
                                }
                            }
                        }
                        DisplayInConsole($"Downloaded: {fileName}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DisplayInConsole("Download cancelled.");
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error searching all cosmetics: {ex.Message}");
            }
            finally
            {
                allCosmeticsCancellationTokenSource = null;
            }
        }

        // map
        private async void Others_Map_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    MapWindow mapWindow = new MapWindow();
                    mapWindow.Show();

                    HttpResponseMessage response = await client.GetAsync("https://fortnite-api.com/v1/map");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    JObject data = (JObject)json["data"];
                    JObject images = (JObject)data["images"];

                    string blankImageUrl = (string)images["blank"];
                    string poisImageUrl = (string)images["pois"];

                    string othersFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "others");
                    if (!Directory.Exists(othersFolder))
                    {
                        Directory.CreateDirectory(othersFolder);
                        DisplayInConsole("Folder for another stuff Created.");
                    }

                    await DownloadImageAsync(blankImageUrl, System.IO.Path.Combine(othersFolder, "map_blank.png"));
                    await DownloadImageAsync(poisImageUrl, System.IO.Path.Combine(othersFolder, "map_pois.png"));

                    DisplayInConsole("Map successfully downloaded.");
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error downloading map: {ex.Message}");
            }
        }

        private async Task DownloadImageAsync(string imageUrl, string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }

        // shop sections
        private async void shopSections_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fortnitecontent-website-prod07.ol.epicgames.com/content/api/pages/fortnite-game/mp-item-shop");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    JArray sections = (JArray)json["shopData"]["sections"];

                    List<string> sectionList = new List<string>();

                    foreach (var section in sections)
                    {
                        string sectionId = section["sectionID"].ToString();
                        string sectionName = section["displayName"].ToString();

                        if (sectionId.IndexOf("Test", StringComparison.OrdinalIgnoreCase) == -1 &&
                            sectionName.IndexOf("Test", StringComparison.OrdinalIgnoreCase) == -1 &&
                            sectionId.IndexOf("JamTracks", StringComparison.OrdinalIgnoreCase) == -1 &&
                            sectionName.IndexOf("JamTracks", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            List<string> dates = new List<string>();  

                            JObject metadata = (JObject)section["metadata"];
                            if (metadata != null)
                            {
                                JArray metaStackRanks = (JArray)metadata["stackRanks"];
                                if (metaStackRanks != null && metaStackRanks.Count > 0)
                                {
                                    foreach (JObject stackRank in metaStackRanks)
                                    {
                                        string startDate = stackRank["startDate"]?.ToString();
                                        if (!string.IsNullOrEmpty(startDate))
                                        {
                                            dates.Add(startDate);
                                        }
                                    }
                                }

                                JArray offerGroups = (JArray)metadata["offerGroups"];
                                if (offerGroups != null)
                                {
                                    foreach (JObject offerGroup in offerGroups)
                                    {
                                        JArray offerStackRanks = (JArray)offerGroup["stackRanks"];
                                        if (offerStackRanks != null && offerStackRanks.Count > 0)
                                        {
                                            foreach (JObject offerStackRank in offerStackRanks)
                                            {
                                                string startDate = offerStackRank["startDate"]?.ToString();
                                                if (!string.IsNullOrEmpty(startDate))
                                                {
                                                    dates.Add(startDate);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            sectionList.Add($"[{sectionId}] - {sectionName}");

                            foreach (var date in dates)
                            {
                                sectionList.Add($"[{date}]");
                            }

                            if (dates.Count == 0)
                            {
                                sectionList.Add("[N/A]");
                            }

                            sectionList.Add("");
                        }
                    }

                    string fileName = $"{DateTime.Now:yyyy.MM.dd}.txt";
                    string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "shopSections", fileName);
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        foreach (string line in sectionList)
                        {
                            await writer.WriteLineAsync(line);
                        }
                    }

                    DisplayInConsole($"Shop section saved to {fileName}");
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error fetching shop sections: {ex.Message}");
            }
        }

        // apollo button
        private void Help_Apollo_Click(object sender, EventArgs e)
        {
            ApolloInfo apollo = new ApolloInfo();
            apollo.Show();
        }

        // settings button
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }

        // Display Console
        private void DisplayInConsole(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                consoleLineCount++;
                consoleTextBox.AppendText($"{consoleLineCount}. {message}\n");
                consoleTextBox.ScrollToEnd();
            });
        }

        private void mnemonic_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(authorizationCode))
            {
                DisplayInConsole("Authorization code is required to open Mnemonic Search.");
                return;
            }

            MnemonicSearchWindow mnemonicWindow = new MnemonicSearchWindow(authorizationCode);
            mnemonicWindow.Show();
        }

        private async void Others_AuthCode_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputDialog = new InputDialog();
            if (inputDialog.ShowDialog() == true)
            {
                authorizationCode = inputDialog.ResponseText;
                if (string.IsNullOrEmpty(authorizationCode))
                {
                    DisplayInConsole("Authorization code is required.");
                    return;
                }
                DisplayInConsole("Authorization code successfully entered.");
            }
            else
            {
                DisplayInConsole("Authorization code input was canceled.");
            }
        }

        private class MappingInfo
        {
            public string url { get; set; }
            public string fileName { get; set; }
            public string hash { get; set; }
            public int lengh { get; set; }
            public DateTime uploaded { get; set; }
            public MetaInfo meta { get; set; }
        }

        private class MetaInfo
        {
            public string version { get; set; }
            public string compressionMethod { get; set; }
            public string platform { get; set; }
        }
    }
}