using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using HtmlAgilityPack;


namespace ProgrammingProjectApplication.Data
{
    public class WebCrawler
    {

            public async Task<List<string>> CrawlAsync(string url)
            {
                var urlsToCrawl = new List<string> { url };
                var visitedUrls = new List<string>();

              
                
                    var currentUrl = urlsToCrawl.First();
                    urlsToCrawl.Remove(currentUrl);
                    visitedUrls.Add(currentUrl);

                    var html = await GetHtmlFromUrlAsync(currentUrl);
                    var document = new HtmlDocument();
                    document.LoadHtml(html);

                    var links = document.DocumentNode.Descendants("a")
                        .Select(a => a.GetAttributeValue("href", null))
                        .Where(href => !String.IsNullOrEmpty(href))
                        .Select(href => MakeAbsoluteUrl(currentUrl, href))
                        .Where(url => !visitedUrls.Contains(url))
                        .ToList();


                    foreach (var link in links)
                    {
                        urlsToCrawl.Add(link);
                    }
                

            return urlsToCrawl;
            }

            private async Task<string> GetHtmlFromUrlAsync(string url)
            {
                using (var client = new HttpClient())
                {
                    return await client.GetStringAsync(url);
                }
            }

            private string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
            {
                var uri = new Uri(baseUrl);
                var newUri = new Uri(uri, relativeUrl);
                return newUri.AbsoluteUri;
            }

        }

    
}
