using MyWebsiteBlazor.Database;

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
        return $"Title: {gameData.Title}\n" +
            $"Description: {gameData.Description}\n" +
            $"Tags: {gameData.Tags}\n" +
            $"OriginalPrice: {gameData.OriginalPrice}\n" +
            $"DiscountedPrice: {gameData.DiscountedPrice}\n" +
            $"RatingInPercantage: {gameData.RatingInPercantage} \n" +
            $"SteamUrl: {gameData.SteamUrl} \n" +
            $"ReleaseDate: {gameData.ReleaseDate} \n" +
            $"LastUpdated: {gameData.LastUpdated}";
    }
}