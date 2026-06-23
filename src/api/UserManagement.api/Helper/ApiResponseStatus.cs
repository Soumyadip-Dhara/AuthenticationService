using UserManagement.Enum;

namespace UserManagement.Helper
{
    public class APIResponseClass<T>
    {
        public T? result { get; set; }
        public APIResponseStatus apiResponseStatus {    get; set; }
        public string message { get; set; }
    }
}
