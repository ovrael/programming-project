using SQLite;

namespace MyWebsiteBlazor.Database
{
    [Table("GameData")]
    public class GameData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageSource { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string SteamUrl { get; set; } = string.Empty;

        public int RatingInPercantage { get; set; } = 0;
        public int ReviewsCount { get; set; } = 0;

        public double OriginalPrice { get; set; } = 0.0;
        public double DiscountedPrice { get; set; } = 0.0;


        public DateTime ReleaseDate { get; set; } = new DateTime(2000, 1, 1, 0, 0, 0, 0, 0);
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
