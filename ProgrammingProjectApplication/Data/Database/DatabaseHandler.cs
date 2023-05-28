using MyWebsiteBlazor.Data.Database.Models;
using SQLite;
using SQLitePCL;

namespace MyWebsiteBlazor.Database
{
    public static class DatabaseHandler
    {

        private static readonly SQLiteConnection databaseConnection;

        static DatabaseHandler()
        {
            string fileName = "gamesData.db";
            string path = Path.Combine(Environment.CurrentDirectory, "Data", "Database", "Files", fileName);

            if (!File.Exists(path))
            {
                databaseConnection = new SQLiteConnection(path, SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite);
                databaseConnection.CreateTable<GameData>();
                return;
            }

            try
            {
                databaseConnection = new SQLiteConnection(path);
                databaseConnection.CreateTable<GameData>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot load database in path: {path}. \nError msg: {e.Message}");
            }
        }

        #region GameData
        public static Response AddGameData(GameData gameData)
        {
            if (databaseConnection.Table<GameData>().Any(p => p.Title == gameData.Title))
            {
                return new Response(false, $"Game with that title already exists in database! Game title: \'{gameData.Title}\'");
            }

            databaseConnection.Insert(gameData);
            return new Response(true, "Added to database");
        }

        public static GameData[] GetAllGamesData()
        {
            return databaseConnection.Table<GameData>().ToArray();
        }

        public static Response DeleteGameData(string title)
        {
            try
            {
                databaseConnection.Table<GameData>().Delete(p => p.Title == title);
                return new Response(true, $"Game with title:{title} succesfully deleted!");
            }
            catch (Exception e)
            {
                return new Response(false, $"Can't delete game with title:{title}! Error: {e.Message}");
            }
        }

        public static GameData GetGameData(string title)
        {
            if (string.IsNullOrEmpty(title))
                return new GameData();

            var gameData = databaseConnection.Table<GameData>().First(p => p.Title == title);

            return gameData is null ? new GameData() : gameData;
        }

        public static Response UpdateGameData(GameData gameData)
        {
            try
            {
                databaseConnection.Update(gameData);
                return new Response(true, $"Game:{gameData.Title} succesfully updated!");
            }
            catch (Exception e)
            {
                return new Response(false, $"Can't update:{gameData.Title}! Error: {e.Message}");
            }
        }
        #endregion
    }
}
