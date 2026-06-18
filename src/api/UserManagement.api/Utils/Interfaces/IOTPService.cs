namespace UserManagement.Utils.Interfaces
{
    public interface IOTPService
    {
        public Task SendOtp(string mobileNumber, string username, short type);
    }
}
