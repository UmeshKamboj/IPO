using System.Text.Json.Serialization;

namespace IPOClient.Models.Responses
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResponseType
    {
        Success = 1,
        Error = 2,
        Warning = 3
    }
}
