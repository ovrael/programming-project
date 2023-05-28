namespace MyWebsiteBlazor.Data.Database.Models
{
    public class Response
    {
        public bool Result { get; private set; }
        public string Message { get; private set; }

        public Response(bool result, string message)
        {
            Result = result;
            Message = message;
        }

        public override string ToString()
        {
            return $"Result: {Result}" + Environment.NewLine + $"Message: {Message}";
        }
    }
}
