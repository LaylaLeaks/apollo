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

namespace Apollo
{
    public partial class MainWindow : Window
    {
        private DiscordRpcClient client;
        private Timer timer;
        private CancellationTokenSource allCosmeticsCancellationTokenSource;

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
            "+ All battle pass icons downloading via others",
            "",
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

            if (!Directory.Exists(battlePassImages))
            {
                Directory.CreateDirectory(battlePassImages);
                DisplayInConsole("Folder for Battle Pass images Created.");
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
                    LargeImageText = "Apollo Beta"
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

        // new cosemtics
        private async void NewCosmetics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fortnite-api.com/v2/cosmetics/br/new");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JObject json = JObject.Parse(responseBody);
                    JArray items = (JArray)json["data"]["items"];

                    string newCosmeticsFolder = System.IO.Path.Combine(Environment.CurrentDirectory, "new_cosmetics");
                    if (!Directory.Exists(newCosmeticsFolder))
                    {
                        Directory.CreateDirectory(newCosmeticsFolder);
                        DisplayInConsole("Folder for new Cosmetics Created.");
                    }

                    foreach (JToken item in items)
                    {
                        string itemName = (string)item["name"];
                        string itemId = (string)item["id"];
                        string iconUrl = (string)item["images"]["icon"];

                        if (string.IsNullOrEmpty(iconUrl))
                        {
                            iconUrl = "https://laylaleaks.netlify.app/src/img/Placeholder.png";
                        }
                        else if (!Uri.IsWellFormedUriString(iconUrl, UriKind.Absolute))
                        {
                            Uri baseUri = new Uri("https://fortnite-api.com");
                            iconUrl = new Uri(baseUri, iconUrl).AbsoluteUri;
                        }

                        string fileName = itemId + ".png";

                        string filePath = System.IO.Path.Combine(newCosmeticsFolder, fileName);
                        using (var iconResponse = await client.GetAsync(iconUrl))
                        {
                            iconResponse.EnsureSuccessStatusCode();
                            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await iconResponse.Content.CopyToAsync(fileStream);
                            }
                        }

                        DisplayInConsole($"Downloaded icon for: {itemName} (ID: {itemId})");
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
                            iconUrl = "https://laylaleaks.netlify.app/src/img/Placeholder.png";
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

        private async void Others_BP_Images_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://fnapi2.netlify.app/api/v1/battlePass");
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    string battlePassImages = System.IO.Path.Combine(Environment.CurrentDirectory, "Battle Pass");
                    if (!Directory.Exists(battlePassImages))
                    {
                        Directory.CreateDirectory(battlePassImages);
                        DisplayInConsole("Folder for new battle pass icons created.");
                    }

                    var urls = ExtractImageUrls(responseBody);

                    foreach (var url in urls)
                    {
                        try
                        {
                            var fileName = System.IO.Path.GetFileName(new Uri(url).LocalPath);
                            var filePath = System.IO.Path.Combine(battlePassImages, fileName);

                            byte[] imageBytes = await client.GetByteArrayAsync(url);
                            File.WriteAllBytes(filePath, imageBytes);

                            DisplayInConsole($"Image saved: {fileName}");
                        }
                        catch (Exception ex)
                        {
                            DisplayInConsole($"Error saving image {url}: {ex.Message}");
                        }
                    }

                    DisplayInConsole("All battle pass icons downloaded successfully.");
                }
            }
            catch (Exception ex)
            {
                DisplayInConsole($"Error downloading battle pass icons: {ex.Message}");
            }
        }

        private IEnumerable<string> ExtractImageUrls(string content)
        {
            var imageUrls = new List<string>();
            var regex = new Regex(@"https?:\/\/.*?\.(png|jpg)", RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(content))
            {
                imageUrls.Add(match.Value);
            }
            return imageUrls;
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
                        sectionList.Add($"[{sectionId}] - {sectionName}");
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