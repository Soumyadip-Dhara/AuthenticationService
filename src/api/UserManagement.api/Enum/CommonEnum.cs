namespace UserManagement.Enum
{
    public enum APIResponseStatus
    {
        Success = 1,
        Warning = 2,
        Error = 3
    }

    public enum OTPType
    {
        ForgotPassword = 1,
        Login = 2,
        ChangeEmail = 3
    }
    public class ConsumeStatusEnums
    {
        public const string SUCCESS = "SUCCESS";
        public const string FAILED = "FAILED";
        public const string PENDING = "PENDING";
        public const string RESOLVED = "RESOLVED";
        public const string NO_ACTION = "NO ACTION";
    }
}
