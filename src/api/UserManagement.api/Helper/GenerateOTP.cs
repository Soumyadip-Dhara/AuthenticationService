namespace UserManagement.Helper
{
    public class GenerateOTP
    {
        public static string Generate()
        {
            Random random = new Random();
            int otpNumber = random.Next(100000, 1000000);
            return otpNumber.ToString();
        }
    }
}
