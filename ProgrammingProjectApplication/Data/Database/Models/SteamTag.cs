using SQLite;

namespace ProgrammingProjectApplication.Database
{
    [Table("SteamTag")]
    public class SteamTag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; } = 0;
    }
}
