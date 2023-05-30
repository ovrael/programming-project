namespace MyWebsiteBlazor.Data.Database.Models
{
    public class Response
    {
        public bool Result { get; private set; }
        public string Message { get; private set; }
        public object ReturnedObject { get; private set; }

        public Response(bool result, string message)
        {
            Result = result;
            Message = message;
            ReturnedObject = new object();
        }

        public Response(bool result, string message, object returnedObject)
        {
            Result = result;
            Message = message;
            ReturnedObject = returnedObject;
        }

        public override string ToString()
        {
            return $"Result: {Result}" + Environment.NewLine + $"Message: {Message}";
        }
    }
}
