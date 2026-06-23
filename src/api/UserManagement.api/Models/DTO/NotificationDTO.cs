namespace UserManagement.Models.DTO;
public class EmailPayload
{
    public string Email { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}

public class SmsDTO
{
    public string TemplateId { get; set; }
    public string Message { get; set; }
    public string PhoneNumber { get; set; }

}

public class SmsPayload
{
    public string TemplateId { get; set; }
    public string PhoneNumber { get; set; }
    public string? Name { get; set; }
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public string? Otp { get; set; }
}


public class OTPHashLogPayload
{
    public string OtpHash { get; set; }
    public string Salt { get; set; }
    public short OtpType { get; set; }
    public string PhoneNumber { get; set; }
    public string Username { get; set; }
    public DateTime TimeStamp { get; set; } 

    
    //public string? Name { get; set; }
    //public string? UserId { get; set; }
    //public string? Password { get; set; }
    //public string? Otp { get; set; }
}
