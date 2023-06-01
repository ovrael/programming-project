using ProgrammingProjectApplication.Database;
using System.Reflection;
using System.Text;

public enum MessageType
{
    Normal,
    Warning,
    Error,
    Success
}

public static class DebugHelper
{
    public static void WriteMessage(string message, MessageType messageType = MessageType.Normal)
    {
        System.Diagnostics.Debug.WriteLine($"------------------------------------------------ {messageType}");
        System.Diagnostics.Debug.WriteLine(message);

    }

    public static string GameDataToString(GameData gameData)
    {
        StringBuilder gameDataProperties = new StringBuilder();

        Type gameDataType = gameData.GetType();
        PropertyInfo[] properties = gameDataType.GetProperties();

        foreach (PropertyInfo property in properties)
        {
            gameDataProperties.AppendLine($"{property.Name}: {property.GetValue(gameData, null)}");
        }
        return gameDataProperties.ToString();
        //return $"Title: {gameData.Title}\n" +
        //    $"Description: {gameData.Description}\n" +
        //    $"Tags: {gameData.Tags}\n" +
        //    $"OriginalPrice: {gameData.OriginalPrice}\n" +
        //    $"DiscountedPrice: {gameData.DiscountedPrice}\n" +
        //    $"RatingInPercantage: {gameData.RatingInPercantage} \n" +
        //    $"SteamUrl: {gameData.SteamUrl} \n" +
        //    $"ReleaseDate: {gameData.ReleaseDate} \n" +
        //    $"LastUpdated: {gameData.LastUpdated}";
    }
}