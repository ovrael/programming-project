namespace ProgrammingProjectApplication.Data
{
    public class SteamGameInfo
    {

        public string Title { get; set; }
        public string ImageSource { get; set; }
        public string ReleaseDate { get; set; }
        public string OriginalPrice { get; set; }
        public string DiscountedPrice { get; set; }
        public double DiscountAmount { get; set; }
        public string UrlLink { get; set; }

        public bool ShowTags { get; set; }

        public List<string> GameTags;
    }
}
