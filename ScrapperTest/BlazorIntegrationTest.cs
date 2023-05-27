using System;
using System.Net.Http;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using ProgrammingProjectApplication.Pages;
using ProgrammingProjectApplication.Data;


namespace ScrapperTest
{
    public class CrawledUrlsIntegrationTest : IDisposable
    {
        private readonly TestContext context;
        private readonly Mock<WebCrawler> webCrawlerMock;

        public CrawledUrlsIntegrationTest()
        {
            webCrawlerMock = new Mock<WebCrawler>();
            context = new TestContext();

            // Register the mocked WebCrawler service in the component's service provider
            context.Services.AddSingleton(webCrawlerMock.Object);
        }

        public void Dispose()
        {
            context.Dispose();
        }

        [Fact]
        public async Task CrawledUrls_ShouldReturnValidUrls()
        {
            
            var component = context.RenderComponent<CrawledUrls>();

            
            component.Find("#urlInput").Change("https://example.com"); // Enter a valid URL
            component.Find("button").Click();

            // Wait for the loading to finish (assuming it takes less than 5 seconds)
            await Task.Delay(5000);

           
            var urlsList = component.FindAll("ul li a");
            Assert.NotEmpty(urlsList);

            foreach (var urlElement in urlsList)
            {
                var url = urlElement.GetAttribute("href");
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    Assert.True(false, $"URL '{url}' does not start with 'http://' or 'https://'.");
                }
            }
        }

    }
}
