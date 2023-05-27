using ProgrammingProjectApplication.Data;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ScrapperTest
{
    public class UnitTest1
    {
        [Fact]
        public async Task Scrape_ReturnsGameInfoList()
        {
            // Arrange
            var httpClient = new HttpClient();
            var steamScrapper = new SteamScrapper(httpClient);

            // Act
            var gameInfos = await steamScrapper.Scrape(1, false).ToListAsync();

            // Assert
            Assert.NotNull(gameInfos);
            Assert.NotEmpty(gameInfos);
            Assert.IsType<List<SteamGameInfo>>(gameInfos);
        }

        public class WebCrawlerTests
        {
            [Fact]
            public async Task CrawlAsync_ReturnsListOfUrls()
            {
                // Arrange
                var webCrawler = new WebCrawler();
                var url = "https://store.steampowered.com/"; // Replace with the URL you want to crawl

                // Act
                var crawledUrls = await webCrawler.CrawlAsync(url);

                // Assert
                Assert.NotNull(crawledUrls);
                Assert.NotEmpty(crawledUrls);
                Assert.All(crawledUrls, url => Assert.IsType<string>(url));
            }


        }
    }
}