using System.Text.Json;

namespace UserManagement.Helper
{
    public class JSONHelper
    {
        public static string ObjectToJson(object obj)
        {
            try
            {
                // Use JsonSerializer to serialize the object to a JSON string
                return JsonSerializer.Serialize(obj);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that might occur during the serialization
                Console.WriteLine("Error converting object to JSON: " + ex.Message);
                return null; // Return null or handle the error as appropriate for your use case
            }
        }
    }
}
