using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using MyWebsiteBlazor.Database;
using ProgrammingProjectApplication.Pages;

namespace ProgrammingProjectApplication.Data
{
    public class SteamScrapper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly HttpClient _httpClient;

        private List<SteamGameInfo> gameInfos = new List<SteamGameInfo>();

        public SteamScrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async IAsyncEnumerable<SteamGameInfo> Scrape(int loadedGamesCounter, bool scrapeTags)
        {

            int counter = 0;
            var httpClient = new HttpClient();

            do
            {

                int howManyGames = counter * 50;

                string UrlFormat = $"https://store.steampowered.com/search/results/?query=&start={howManyGames}&count=50&dynamic_data=&sort_by=_ASC&supportedlang=polish&os=win&snr=1_7_7_globaltopsellers_7&filter=globaltopsellers&infinite=1";

                var response = await httpClient.GetAsync(UrlFormat);

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonString);

                var resultsHtml = jsonObject.RootElement.GetProperty("results_html").GetString();


                var doc = new HtmlDocument();
                doc.LoadHtml(resultsHtml);

                var node = doc.DocumentNode.SelectNodes("//a");

                if (node != null)
                {
                    foreach (var nodeX in node)
                    {

                        var steamGame = new SteamGameInfo();

                        var titleNode = nodeX.SelectSingleNode(".//span[contains(@class, 'title')]");
                        if (titleNode != null)
                        {
                            steamGame.Title = titleNode.InnerText.Trim();
                        }


                        var priceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_price discounted')]");
                        string originalPrice = string.Empty;
                        string discountedPrice = string.Empty;

                        if (priceNode == null)
                        {
                            priceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_price')]");
                        }
                        if (priceNode != null)
                        {
                            string[] prices = priceNode.InnerText.Trim().Split("zł");

                            if (prices.Length >= 1)
                            {
                                originalPrice = prices[0].Trim();
                            }

                            if (prices.Length >= 2)
                            {
                                discountedPrice = prices[1].Trim();
                            }
                        }

                        steamGame.OriginalPrice = originalPrice;
                        steamGame.DiscountedPrice = discountedPrice;

                        var discountPriceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_discount')]");
                        if (discountPriceNode != null)
                        {
                            string discountString = discountPriceNode.InnerText.Trim();
                            steamGame.DiscountAmount = !string.IsNullOrEmpty(discountString) ? double.Parse(discountString.Replace("-", "").Replace("%", "")) : 0;
                        }
                        else
                        {
                            steamGame.DiscountAmount = 0;
                        }

                        var releaseDateNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_released')]");
                        if (releaseDateNode != null)
                        {
                            steamGame.ReleaseDate = releaseDateNode.InnerText.Trim();
                        }

                        var urlLinkNode = nodeX.SelectSingleNode("//a[@href]");
                        if (urlLinkNode != null)
                        {
                            string hrefValue = nodeX.Attributes["href"].Value;
                            steamGame.UrlLink = hrefValue;

                            if (scrapeTags == true)
                            {
                                steamGame.GameTags = await ScrapeTagsFromUrl(steamGame.UrlLink);
                            }

                        }

                        var imageNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_capsule')]/img");
                        if (imageNode != null)
                        {
                            steamGame.ImageSource = imageNode.Attributes["src"].Value;
                        }

                        //gameInfos.Add(steamGame);
                        yield return steamGame;

                    }


                }

                counter++;



            } while (counter < loadedGamesCounter);

            httpClient.Dispose();



        }

        private async Task<List<string>> ScrapeTagsFromUrl(string url)
        {
            var response = await httpClient.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tagNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'app_tag')]");
            var tags = new List<string>();
            if (tagNodes != null)
            {
                foreach (var node in tagNodes)
                {
                    tags.Add(node.InnerText.Trim());
                }
            }
            return tags;
        }

        public static async Task<GameData> ScrapeSearchedGame(string title)
        {
            string formatedTitle = title.Replace(' ', '+');
            string searchUrl = $"https://store.steampowered.com/search/results/?query=&term={formatedTitle}&infinite=1";
            string resultsHtml = await GetResultHTML(searchUrl);
            if (resultsHtml.Length == 0) return new GameData();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            var node = doc.DocumentNode.SelectNodes("//a");

            if (node is null) return new GameData();

            var firstGameNode = node.First();
            GameData gameData = new GameData();

            var titleNode = firstGameNode.SelectSingleNode(".//span[contains(@class, 'title')]");
            if (titleNode != null)
            {
                gameData.Title = titleNode.InnerText.Trim();
            }


            var priceNode = firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_price discounted')]");
            priceNode ??= firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_price')]");

            string[] prices = priceNode.InnerText.Trim().Split("zł");

            if (prices.Length >= 1)
            {
                double.TryParse(prices[0].Trim(), out double originalPrice);
                gameData.OriginalPrice = originalPrice;
            }

            if (prices.Length >= 2)
            {
                double.TryParse(prices[1].Trim(), out double discountedPrice);
                gameData.DiscountedPrice = discountedPrice;
            }


            var releaseDateNode = firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_released')]");
            if (releaseDateNode != null)
            {
                DateTime.TryParse(releaseDateNode.InnerText.Trim(), out DateTime releaseDate);
                gameData.ReleaseDate = releaseDate;
            }

            var urlLinkNode = firstGameNode.SelectSingleNode("//a[@href]");
            if (urlLinkNode != null)
            {
                string hrefValue = firstGameNode.Attributes["href"].Value;
                gameData.SteamUrl = hrefValue;
                var tagsFromUrl = await ScrapeGameTagsFromSteam(gameData.SteamUrl);
                gameData.Tags = string.Join(';', tagsFromUrl);
            }

            var imageNode = firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_capsule')]/img");
            if (imageNode != null)
            {
                gameData.SteamUrl = imageNode.Attributes["src"].Value;
            }

            return gameData;
        }

        private static async Task<string> GetResultHTML(string url)
        {
            var response = await httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonString);

            if (jsonObject is null) return string.Empty;

            var resultsHtml = jsonObject.RootElement.GetProperty("results_html").GetString();

            if (resultsHtml is null) return string.Empty;

            return resultsHtml;
        }

        private static async Task<string[]> ScrapeGameTagsFromSteam(string url)
        {
            var response = await httpClient.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tagNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'app_tag')]");

            if (tagNodes is null) return Array.Empty<string>();

            string[] tags = new string[tagNodes.Count];
            for (int i = 0; i < tags.Length; i++)
            {
                tags[i] = tagNodes[i].InnerText.Trim();
            }

            return tags;
        }
    }
}
