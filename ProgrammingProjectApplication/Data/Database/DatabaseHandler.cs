using MyWebsiteBlazor.Data.Database.Models;
using SQLite;

namespace MyWebsiteBlazor.Database
{
    public static class DatabaseHandler
    {

        private static readonly SQLiteAsyncConnection databaseAsyncConnection;

        static DatabaseHandler()
        {
            string fileName = "gamesData.db";
            string path = Path.Combine(Environment.CurrentDirectory, "Data", "Database", "Files", fileName);

            if (!File.Exists(path))
            {
                databaseAsyncConnection = new SQLiteAsyncConnection(path, SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite);
                databaseAsyncConnection.CreateTableAsync<GameData>();
                return;
            }

            try
            {
                databaseAsyncConnection = new SQLiteAsyncConnection(path);
                databaseAsyncConnection.CreateTableAsync<GameData>();
            }
            catch (Exception e)
            {
                DebugHelper.WriteMessage($"Cannot load database in path: {path}. \nError msg: {e.Message}");
                DebugHelper.WriteMessage($"Database will be created in the project location");
                string projectDirectory = Directory.GetCurrentDirectory();
                string emergencyDbName = "_emergencyGameData.db";
                string emergencyDbPath = Path.Combine(projectDirectory, emergencyDbName);

                databaseAsyncConnection = new SQLiteAsyncConnection(emergencyDbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite);
                databaseAsyncConnection.CreateTableAsync<GameData>();
            }
        }

        #region GameData
        public static async Task<Response> AddGameDataAsync(GameData gameData)
        {
            if (await GameDataExistsAsync(gameData.Title, gameData.ReleaseDate.Year))
            {
                return new Response(false, $"Game with that title already exists in database! Game title: \'{gameData.Title}\'");
            }

            int rows = await databaseAsyncConnection.InsertAsync(gameData);

            if (rows > 0)
                return new Response(true, "Added to database");
            else
                return new Response(false, "Insert operation failed.");
        }

        public static async Task<bool> GameDataExistsAsync(string title, int releaseYear)
        {
            var tableContent = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            var gameData = tableContent.FirstOrDefault(gd => gd.Title == title && gd.ReleaseDate.Year == releaseYear);
            return gameData is not null;
        }

        public static async Task<GameData[]> GetAllGamesDataAsync()
        {
            return await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
        }

        public static async Task<Response> DeleteGameDataAsync(string title)
        {
            try
            {
                await databaseAsyncConnection.Table<GameData>().DeleteAsync(gd => gd.Title == title);
                return new Response(true, $"Game with title:{title} succesfully deleted!");
            }
            catch (Exception e)
            {
                return new Response(false, $"Can't delete game with title:{title}! Error: {e.Message}");
            }
        }

        public static async Task<GameData> GetGameDataAsync(string title)
        {
            if (string.IsNullOrEmpty(title))
                return new GameData();

            var tableContent = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            var gameData = tableContent.FirstOrDefault(gd => gd.Title == title);

            return gameData is null ? new GameData() : gameData;
        }

        /// <summary>
        /// Gets game data about game from database
        /// </summary>
        /// <param name="title"></param>
        /// <param name="releaseYear"></param>
        /// <returns>
        /// <list type="bullet">
        /// <item> Filled data if exists </item>
        /// <item> New GameData object if game doesnt exist </item>
        /// </list>
        /// </returns>
        public static async Task<GameData> GetGameDataAsync(string title, int releaseYear)
        {
            if (string.IsNullOrEmpty(title))
                return new GameData();

            var tableContent = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            var gameData = tableContent.FirstOrDefault(gd => gd.Title == title && gd.ReleaseDate.Year == releaseYear);

            return gameData is null ? new GameData() : gameData;
        }

        public static async Task<Response> UpdateGameDataAsync(GameData gameData)
        {
            try
            {
                int modifiedRows = await databaseAsyncConnection.UpdateAsync(gameData);
                if (modifiedRows > 0)
                    return new Response(true, $"Game:{gameData.Title} succesfully updated!");
                else
                    return new Response(false, $"Update operation failed.");
            }
            catch (Exception e)
            {
                return new Response(false, $"Can't update:{gameData.Title}! Exception message: {e.Message}");
            }
        }
        #endregion
    }
}
