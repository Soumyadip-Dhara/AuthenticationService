using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UserManagement.RbbitMQ
{
    public class ValidationError
    {
        public string PropertyName { get; set; } = default!;
        public string ErrorMessage { get; set; } = default!;
    }
    public class ErrorPayload
    {
        public string OriginalMessage { get; set; } = default!;
        public string ErrorType { get; set; } = default!;
        public IEnumerable<ValidationError> ValidationErrors { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
    public class AckPayloadModel
    {
        public Guid MessageId { get; set; }
        public string Status { get; set; }
        [JsonIgnore]
        public string? StatusMsg { get; set; }
        [JsonPropertyName("StatusMsg")]
        public JsonElement StatusMsgJson
        {
            get
            {
                // Case 1: If StatusMsg is null/empty, return JsonElement with "null"
                if (string.IsNullOrWhiteSpace(StatusMsg))
                    return JsonSerializer.Deserialize<JsonElement>("null");

                // Case 2: If StatusMsg is a valid JSON object/array, parse it
                try
                {
                    return JsonSerializer.Deserialize<JsonElement>(StatusMsg);
                }
                catch (JsonException)
                {
                    // Case 3: If StatusMsg is a plain string, serialize it as a JSON string
                    //return JsonSerializer.Deserialize<JsonElement>($"\"{StatusMsg}\"");
                    var safeJson = JsonSerializer.Serialize(StatusMsg);
                    return JsonSerializer.Deserialize<JsonElement>(safeJson);
                }
            }
            set => StatusMsg = value.GetRawText();
        }

        public string? FailedType { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class AckPayloadNullGuidModel : AckPayloadModel
    {
        public Guid? MessageId { get; set; }
        public string ModuleId { get; set; }
    }
}