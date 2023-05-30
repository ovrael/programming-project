using HtmlAgilityPack;
using MyWebsiteBlazor.Data.Database.Models;
using MyWebsiteBlazor.Database;
using System.Text.Json;

namespace ProgrammingProjectApplication.Data
{
    public class SteamScrapper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        private readonly HttpClient _httpClient;

        private readonly List<SteamGameInfo> gameInfos = new List<SteamGameInfo>();

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

        public static async Task<Response> ScrapeSearchedGame(string title)
        {
            string formatedTitle = title.Replace(' ', '+');
            string searchUrl = $"https://store.steampowered.com/search/results/?query=&term={formatedTitle}&infinite=1";
            string resultsHtml = await GetResultHTML(searchUrl);
            if (resultsHtml.Length == 0)
                return new Response(false, $"Couldn't find game with title:{title}", new GameData());

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            var node = doc.DocumentNode.SelectNodes("//a");

            if (node is null)
                return new Response(false, $"Couldn't find game with title:{title}", new GameData());

            var firstGameNode = node.First();
            GameData gameData = new GameData();

            var titleNode = firstGameNode.SelectSingleNode(".//span[contains(@class, 'title')]");
            gameData.Title = titleNode is not null ? titleNode.InnerText.Trim() : string.Empty;


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

            return new Response(true, $"Found: {gameData.Title} game", gameData);
        }

        private static async Task<string> GetResultHTML(string url)
        {
            var response = await httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            // idk why it crashes here, cant convert to json...
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

            var response = await httpClient.GetAsync(url);
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
            additionalData.Add("reviewsCount", reviewsCount);


            // Image source
            var imageNode = docNode.SelectSingleNode("//img[contains(@class, 'game_header_image_full')]");
            string imageSource = imageNode is not null ? imageNode.Attributes["src"].Value : string.Empty;
            additionalData.Add("imageSource", imageSource);

            return additionalData;
        }
    }
}
