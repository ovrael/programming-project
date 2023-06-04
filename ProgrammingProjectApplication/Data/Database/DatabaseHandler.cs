using ProgrammingProjectApplication.Data.Database.Models;
using SQLite;

namespace ProgrammingProjectApplication.Database
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
                CreateTables();
                return;
            }

            try
            {
                databaseAsyncConnection = new SQLiteAsyncConnection(path);
                CreateTables();
            }
            catch (Exception e)
            {
                DebugHelper.WriteMessage($"Cannot load database in path: {path}. \nError msg: {e.Message}");
                DebugHelper.WriteMessage($"Database will be created in the project location");
                string projectDirectory = Directory.GetCurrentDirectory();
                string emergencyDbName = "_emergencyGameData.db";
                string emergencyDbPath = Path.Combine(projectDirectory, emergencyDbName);

                databaseAsyncConnection = new SQLiteAsyncConnection(emergencyDbPath, SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite);
                CreateTables();
            }
        }

        private static void CreateTables()
        {
            databaseAsyncConnection.CreateTableAsync<GameData>();
            databaseAsyncConnection.CreateTableAsync<SteamTag>();
        }

        #region GameData
        public static async Task<Response> AddGameDataAsync(GameData gameData)
        {
            int gameDataIndex = await GameDataExistsAsync(gameData.Title, gameData.ReleaseDate.Year);
            if (gameDataIndex >= 0)
            {
                return new Response(false, $"Game with that title already exists in database! Game title: \'{gameData.Title}\'");
            }

            int rows = await databaseAsyncConnection.InsertAsync(gameData);

            if (rows > 0)
                return new Response(true, "Added to database");
            else
                return new Response(false, "Insert operation failed.");
        }

        public static async Task<int> GameDataExistsAsync(string title, int releaseYear)
        {
            var tableContent = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            var gameData = tableContent.FirstOrDefault(gd => gd.Title == title && gd.ReleaseDate.Year == releaseYear);
            gameData = gameData is null ? new GameData() : gameData;
            return gameData.Id;
        }

        public static async Task<GameData[]> GetAllGamesDataAsync()
        {
            return await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
        }
        public static async Task<GameData[]> GetClusteredGamesDataAsync(int clusterId)
        {
            var games = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            return games.Where(g => g.Cluster == clusterId).ToArray();
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

        public static async Task<GameData> GetGameDataAsync(int id)
        {
            if (id < 0)
                return new GameData();

            var tableContent = await databaseAsyncConnection.Table<GameData>().ToArrayAsync();
            var gameData = tableContent.FirstOrDefault(gd => gd.Id == id);

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
                int modifiedRows = await databaseAsyncConnection.UpdateAsync(gameData, gameData.GetType());
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

        public static async Task<Response> UpdateManyGameDataAsync(GameData[] gameDatas)
        {
            try
            {
                int modifiedRows = await databaseAsyncConnection.UpdateAllAsync(gameDatas);
                if (modifiedRows > 0)
                    return new Response(true, $"{modifiedRows} games succesfully updated!");
                else
                    return new Response(false, $"Update operation failed.");
            }
            catch (Exception e)
            {
                return new Response(false, $"Can't update many games! Exception message: {e.Message}");
            }
        }
        #endregion

        #region SteamTags

        public static async Task<Response> AddSteamTag(SteamTag steamTag)
        {
            if (await SteamTagExists(steamTag.Name, steamTag.Value))
            {
                return new Response(false, $"Steam tag exists!");
            }

            int rows = await databaseAsyncConnection.InsertAsync(steamTag);

            if (rows > 0)
                return new Response(true, "Added to database");
            else
                return new Response(false, "Insert operation failed.");
        }

        public static async Task<Response> ClearSteamTagsTable()
        {
            int rows = await databaseAsyncConnection.DeleteAllAsync<SteamTag>();

            if (rows > 0)
                return new Response(true, $"Deleted {rows} rows from database");
            else
                return new Response(false, "Delete operation failed.");
        }

        public static async Task<Response> AddManySteamTags(IEnumerable<SteamTag> steamTags)
        {
            int rows = await databaseAsyncConnection.InsertAllAsync(steamTags);

            if (rows > 0)
                return new Response(true, $"Added {rows} rows to database");
            else
                return new Response(false, "Insert operation failed.");
        }

        public static async Task<bool> SteamTagExists(string name, int value)
        {
            var tableContent = await databaseAsyncConnection.Table<SteamTag>().ToArrayAsync();
            var steamTag = tableContent.FirstOrDefault(st => st.Name == name && st.Value == value);
            return steamTag is not null;
        }

        public static async Task<SteamTag[]> GetAllSteamTags()
        {
            return await databaseAsyncConnection.Table<SteamTag>().ToArrayAsync();
        }

        #endregion
    }
}
