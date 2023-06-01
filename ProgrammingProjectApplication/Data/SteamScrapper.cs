using HtmlAgilityPack;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using ProgrammingProjectApplication.Data.Database.Models;
using ProgrammingProjectApplication.Database;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace ProgrammingProjectApplication.Data
{
    public static class SteamScrapper
    {
        public static async IAsyncEnumerable<SteamGameInfo> Scrape(int loadedGamesCounter, bool scrapeTags)
        {
            int counter = 0;

            HttpClient httpClient = new HttpClient();
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

        private static async Task<List<string>> ScrapeTagsFromUrl(string url)
        {
            HttpClient httpClient = new HttpClient();

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
            httpClient.Dispose();
            return tags;
        }

        public static async Task<Response> ScrapeSearchedGame(string title)
        {
            string formatedTitle = title.Replace(' ', '+');
            string searchUrl = $"https://store.steampowered.com/search/results/?query=&term={formatedTitle}&infinite=1&cc=us";
            string resultsHtml = await GetResultHTML(searchUrl);
            if (resultsHtml.Length == 0)
                return new Response(false, $"Couldn't find game with title:{title}", new GameData());

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            var node = doc.DocumentNode.SelectNodes("//a");

            if (node is null)
                return new Response(false, $"Couldn't find game with title:{title}", new GameData());
            return await GetGameDatFromHtmlNode(node.First());
            //var firstGameNode = node.First();
            //GameData gameData = new GameData() { Id = 0 };

            //var titleNode = firstGameNode.SelectSingleNode(".//span[contains(@class, 'title')]");
            //gameData.Title = titleNode is not null ? titleNode.InnerText.Trim() : string.Empty;


            //var priceNode = firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_price discounted')]");
            //priceNode ??= firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_price')]");

            //string[] prices = priceNode.InnerText.Trim().Split("$").Skip(1).ToArray();

            //if (prices.Length >= 1)
            //{
            //    if (prices[0].Trim().ToLower() == "free to play")
            //        gameData.OriginalPrice = 0.0;
            //    else
            //    {
            //        double.TryParse(prices[0].Trim().Replace(',', '.'), out double originalPrice);
            //        gameData.OriginalPrice = originalPrice;
            //    }
            //}

            //if (prices.Length >= 2)
            //{
            //    double.TryParse(prices[1].Trim().Replace(',', '.'), out double discountedPrice);
            //    gameData.DiscountedPrice = discountedPrice;
            //}

            //var releaseDateNode = firstGameNode.SelectSingleNode(".//div[contains(@class, 'search_released')]");
            //if (releaseDateNode != null)
            //{
            //    DateTime.TryParse(releaseDateNode.InnerText.Trim(), out DateTime releaseDate);
            //    gameData.ReleaseDate = releaseDate;
            //}

            //var urlLinkNode = firstGameNode.SelectSingleNode("//a[@href]");
            //if (urlLinkNode != null)
            //{
            //    string hrefValue = firstGameNode.Attributes["href"].Value;
            //    gameData.SteamUrl = hrefValue;
            //}

            //if (gameData.SteamUrl is null || gameData.SteamUrl.Length == 0)
            //    return new Response(true, $"Found: {gameData.Title} game", gameData);

            //var additionalData = await ScrapeAdditionalGameData(gameData.SteamUrl);
            //gameData.Tags = additionalData["tags"];
            //gameData.Description = additionalData["description"];
            //gameData.ImageSource = additionalData["imageSource"];

            //int.TryParse(additionalData["ratingPercentage"], out int ratingPercentage);
            //int.TryParse(additionalData["reviewsCount"], out int reviewsCount);
            //gameData.RatingInPercantage = ratingPercentage;
            //gameData.ReviewsCount = reviewsCount;
            //gameData.LastUpdated = DateTime.Today;

            //return new Response(true, $"Found: {gameData.Title} game", gameData);
        }

        private static async Task<string> GetResultHTML(string url)
        {
            HttpClient httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();

            try
            {
                var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonString);

                if (jsonObject is null) return string.Empty;

                var resultsHtml = jsonObject.RootElement.GetProperty("results_html").GetString();

                if (resultsHtml is null) return string.Empty;

                return resultsHtml;
            }
            catch (Exception)
            {
                return jsonString;
                throw;
            }
        }

        /// <summary>
        /// Gets additional data about game from steam store.
        /// </summary>
        /// <param name="url">Steam url to game</param>
        /// <returns>
        /// Dictionary with keys: 
        /// <list type="bullet">
        /// <item>tags</item>
        /// <item>description</item>
        /// <item>ratingPercentage</item>
        /// <item>reviewsCount</item>
        /// <item>imageSource</item>
        /// </list>
        /// </returns>
        private static async Task<Dictionary<string, string>> ScrapeAdditionalGameData(string url)
        {
            Dictionary<string, string> additionalData = new Dictionary<string, string>();

            var baseAddress = new Uri("http://example.com");
            var cookieContainer = new CookieContainer();
            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                cookieContainer.Add(new Uri(url), new Cookie("birthtime", "568022401"));
                var response = await client.GetAsync(url);

                // Handle this exception!
                response.EnsureSuccessStatusCode();

                // Load html
                var html = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var docNode = doc.DocumentNode;

                // Joined Tags
                var tagNodes = docNode.SelectNodes("//a[contains(@class, 'app_tag')]");
                string joinedTags = string.Empty;
                if (tagNodes is not null && tagNodes.Count > 0)
                {
                    var tags = tagNodes.Select(t => t.InnerText.Trim());
                    joinedTags = string.Join(";", tags);
                }
                additionalData.Add("tags", joinedTags);


                // Description
                var descriptionNode = docNode.SelectSingleNode("//div[contains(@class, 'game_description_snippet')]");
                string description = descriptionNode is not null ? descriptionNode.InnerText.Trim() : string.Empty;
                additionalData.Add("description", description);


                // Rating in percantage
                var ratingNode = docNode.SelectSingleNode("//span[contains(@class, 'responsive_reviewdesc_short')]");
                string rating = ratingNode is not null ? ratingNode.InnerText.Trim() : string.Empty;
                rating = rating.Trim('(', ')').Split('%')[0];
                additionalData.Add("ratingPercentage", rating);

                // Reviews count
                var reviewsCountNode = docNode.SelectSingleNode("//span[contains(@class, 'user_reviews_count')]");
                string reviewsCount = reviewsCountNode is not null ? reviewsCountNode.InnerText.Trim('(', ')') : string.Empty;
                reviewsCount = reviewsCount.Replace(",", string.Empty);
                reviewsCount = reviewsCount.Replace(".", string.Empty);
                additionalData.Add("reviewsCount", reviewsCount);


                // Image source
                var imageNode = docNode.SelectSingleNode("//img[contains(@class, 'game_header_image_full')]");
                string imageSource = imageNode is not null ? imageNode.Attributes["src"].Value : string.Empty;
                additionalData.Add("imageSource", imageSource);
            }

            return additionalData;
        }

        public static async Task<SteamTag[]> ScrapeSteamTags()
        {
            List<SteamTag> tags = new List<SteamTag>();

            string steamSearchUrl = @"https://store.steampowered.com/search/?supportedlang=english&ndl=1";
            string resultsHtml = await GetResultHTML(steamSearchUrl);
            if (resultsHtml.Length == 0)
                return Array.Empty<SteamTag>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            HtmlNode tagsContainerNode = doc.DocumentNode.SelectSingleNode("//*[@id='TagFilter_Container']");
            HtmlNodeCollection tagsNodes = tagsContainerNode.SelectNodes("//div[@class='tab_filter_control_row ']");

            foreach (var tagNode in tagsNodes)
            {
                if (tagNode is null) continue;

                string tagName = tagNode.Attributes["data-loc"].Value;
                string tagValue = tagNode.Attributes["data-value"].Value;


                if (int.TryParse(tagValue, out int tagIndex))
                {
                    tags.Add(new SteamTag()
                    {
                        Name = tagName,
                        Value = tagIndex
                    });
                }
            }

            return tags.ToArray();
        }


        public static async Task<GameData[]> ScrapeGameData(IEnumerable<SteamTag> steamTags, int gamesCount = 100)
        {
            // Category1=998 -> search only games (not bundles)
            // Count=1000 -> get 1000 titles at max
            string steamSearchUrl = @$"https://store.steampowered.com/search/?supportedlang=english&ndl=1&category1=998&cc=us&infinite=1&count={gamesCount}";
            if (steamTags.Count() > 0)
            {
                StringBuilder tagsQuery = new StringBuilder($"&tags={steamTags.ElementAt(0).Value}"); // First tag is cleared, to the rest '2C' is added
                for (int i = 1; i < steamTags.Count(); i++)
                {
                    tagsQuery.Append($"&2C{steamTags.ElementAt(0).Value}");
                }
                steamSearchUrl += tagsQuery.ToString();
            }

            string resultsHtml = await GetResultHTML(steamSearchUrl);

            var doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            var gamesCollection = doc.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");
            DebugHelper.WriteMessage($"gamesCount:{gamesCount}");
            DebugHelper.WriteMessage($"Games collection count:{gamesCollection.Count}");

            List<GameData> games = new List<GameData>();

            int gameCounter = 0;
            foreach (var gameRow in gamesCollection)
            {
                DebugHelper.WriteMessage($"Scrapping {gameCounter} game row");
                gameCounter++;

                var gameResponse = await GetGameDatFromHtmlNode(gameRow);
                if (!gameResponse.Result)
                    continue;

                GameData gameData = gameResponse.ReturnedObject as GameData;
                if (gameData is null || gameData.Title.Length == 0)
                    continue;

                games.Add(gameData);
            }

            DebugHelper.WriteMessage($"Games after all:{games.Count}");


            return games.ToArray();
        }

        private static async Task<Response> GetGameDatFromHtmlNode(HtmlNode gameNode)
        {
            GameData gameData = new GameData() { Id = 0 };

            var titleNode = gameNode.SelectSingleNode(".//span[contains(@class, 'title')]");
            gameData.Title = titleNode is not null ? titleNode.InnerText.Trim() : string.Empty;


            var priceNode = gameNode.SelectSingleNode(".//div[contains(@class, 'search_price discounted')]");
            priceNode ??= gameNode.SelectSingleNode(".//div[contains(@class, 'search_price')]");

            string[] prices = priceNode.InnerText.Trim().Split("$").Skip(1).ToArray();

            if (prices.Length >= 1)
            {
                if (prices[0].Trim().ToLower() == "free to play")
                    gameData.OriginalPrice = 0.0;
                else
                {
                    double.TryParse(prices[0].Trim().Replace(',', '.'), out double originalPrice);
                    gameData.OriginalPrice = originalPrice;
                }
            }

            if (prices.Length >= 2)
            {
                double.TryParse(prices[1].Trim().Replace(',', '.'), out double discountedPrice);
                gameData.DiscountedPrice = discountedPrice;
            }

            var releaseDateNode = gameNode.SelectSingleNode(".//div[contains(@class, 'search_released')]");
            if (releaseDateNode != null)
            {
                string dateText = releaseDateNode.InnerText.Trim();

                if (dateText == "Coming soon")
                {
                    return new Response(false, "Game hasn't out yet!", new GameData());
                }

                DateTime.TryParse(dateText, out DateTime releaseDate);
                gameData.ReleaseDate = releaseDate;
            }

            var urlLinkNode = gameNode.SelectSingleNode("//a[@href]");
            if (urlLinkNode != null)
            {
                string hrefValue = gameNode.Attributes["href"].Value;
                gameData.SteamUrl = hrefValue;
            }

            if (gameData.SteamUrl is null || gameData.SteamUrl.Length == 0)
                return new Response(true, $"Found: {gameData.Title} game", gameData);

            var additionalData = await ScrapeAdditionalGameData(gameData.SteamUrl);
            gameData.Tags = additionalData["tags"];
            gameData.Description = additionalData["description"];
            gameData.ImageSource = additionalData["imageSource"];

            int.TryParse(additionalData["ratingPercentage"], out int ratingPercentage);
            int.TryParse(additionalData["reviewsCount"], out int reviewsCount);
            gameData.RatingInPercantage = ratingPercentage;
            gameData.ReviewsCount = reviewsCount;
            gameData.LastUpdated = DateTime.Today;

            return new Response(true, $"Found: {gameData.Title} game", gameData);
        }
    }
}
