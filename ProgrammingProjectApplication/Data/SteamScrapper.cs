﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ProgrammingProjectApplication.Data
{

    public class SteamScrapper
    {
        private const string UrlFormat = "https://store.steampowered.com/search/results/?query=&start={0}&count=50&dynamic_data=&sort_by=_ASC&supportedlang=polish&os=win&snr=1_7_7_globaltopsellers_7&filter=globaltopsellers&infinite=1";

        private readonly HttpClient _httpClient;

        private string _htmlDocumentParsed;

        StringBuilder sb = new StringBuilder();

        public SteamScrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SteamGameInfo>> Scrape()
        {


            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://store.steampowered.com/search/results/?query=&start={0}&count=50&dynamic_data=&sort_by=_ASC&supportedlang=polish&os=win&snr=1_7_7_globaltopsellers_7&filter=globaltopsellers&infinite=1");

            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonString);

            var resultsHtml = jsonObject.RootElement.GetProperty("results_html").GetString();


            var doc = new HtmlDocument();
            doc.LoadHtml(resultsHtml);

            var node = doc.DocumentNode.SelectNodes("//a");



            List<SteamGameInfo> gameInfos = new List<SteamGameInfo>();

            foreach (var nodeX in node)
            {

                var steamGame = new SteamGameInfo();

                var titleNode = nodeX.SelectSingleNode(".//span[contains(@class, 'title')]");
                if (titleNode != null)
                {
                    steamGame.Title = titleNode.InnerText.Trim();
                }

              
                var priceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_price discounted')]");

                if(priceNode == null)
                {
                    priceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_price')]");
                }
                if (priceNode != null)
                {
                    steamGame.OriginalPrice = priceNode.InnerText.Trim();
                }

                var discountPriceNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_discount')]");
                if (discountPriceNode != null)
                {
                    steamGame.DiscountedPrice = discountPriceNode.InnerText.Trim();
                }


                var releaseDateNode = nodeX.SelectSingleNode(".//div[contains(@class, 'search_released')]");
                if (releaseDateNode != null)
                {
                    steamGame.ReleaseDate = releaseDateNode.InnerText.Trim();
                }

                gameInfos.Add(steamGame);
                
            }

            return gameInfos;


        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


    }

 

   
}