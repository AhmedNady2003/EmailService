# EmailService
# DeltaCore.EmailService

`DeltaCore.EmailService` is a .NET 8-based library that provides email services with support for sending verification emails (OTP), caching mechanisms (using Memory or Redis), and retry policies for robust email delivery.

## Features

- **Send Email**: Easily send emails with customizable subjects and body content.
- **Send OTP**: Securely send one-time password (OTP) emails for user verification.
- **OTP Verification**: Verify OTP codes to ensure secure user validation.
- **Caching Support**: Store OTPs in-memory or Redis to improve performance.
- **Retry Mechanism**: Automatically retries sending emails in case of failure using exponential backoff.
- **Customizable Email Templates**: Customize OTP email templates with placeholders.

## Installation

You can install the `DeltaCore.EmailService` NuGet package in your .NET 8 project by running the following command in your terminal:

```bash
dotnet add package DeltaCore.EmailService
```
Alternatively, you can search for DeltaCore.EmailService on NuGet and install it directly.

---
## Usage
- **Setting Up Email Service**
To use the SMTPService class, you need to configure your email settings in the appsettings.json file:
```json
{
  "SMTPSettings": {
    "SMTPHost": "smtp.your-email-provider.com",
    "SMTPPort": 587,
    "SMTPUser": "your-email@example.com",
    "SMTPPassword": "your-email-password",
    "FromEmail": "your-email@example.com",
    "OrganizationName": "Your Organization",
    "OTPHtmlBodyTemplate": "<h3>Your verification code is: {code}</h3>"
  }
}
```
In your Program.cs (for .NET 8, this file replaces Startup.cs), register the SMTPService as a singleton service:
```csharp
using DeltaCore.EmailService;

var builder = WebApplication.CreateBuilder(args);

// Register services to the container
builder.Services.AddSingleton<IEmailService, SMTPService>();
builder.Services.Configure<SMTPSettings>(builder.Configuration.GetSection("SMTPSettings"));

// Add other services
builder.Services.AddMemoryCache(); // Required for MemoryCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

// Build and run the application
var app = builder.Build();
app.MapGet("/", () => "Hello World!");
app.Run();
```
- **Sending an Email**
  You can send an email like this:
```csharp
  public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<IActionResult> SendVerificationEmail(string email)
    {
        var result = await _emailService.SendEmailAsync(email, "Welcome", "Welcome to our service!", CancellationToken.None);
        return Ok(result ? "Email sent successfully" : "Failed to send email");
    }
}
```
- **Sending OTP**
  You can send an OTP (One-Time Password) for email verification:
```csharp
public async Task<IActionResult> SendOTP(string email)
{
    var result = await _emailService.SendOTPAsync(email, IEmailService.cashIn.REDIS, CancellationToken.None);
    return Ok(result ? "OTP sent successfully" : "Failed to send OTP");
}
```
- **Verifying OTP**
  To verify the OTP:
```csharp
public async Task<IActionResult> VerifyOTP(string email, string code)
{
    var result = await _emailService.VerifyOTP(email, code, IEmailService.cashIn.REDIS);
    return Ok(result ? "OTP verified successfully" : "Invalid OTP");
}
```

---
## Configuration Options
- SMTPHost: SMTP server host (e.g., smtp.gmail.com).

- SMTPPort: Port for the SMTP server (usually 587 or 465).

- SMTPUser: Your email username for authentication.

- SMTPPassword: Your email password or app-specific password.

- FromEmail: The email address from which emails will be sent.

- OrganizationName: The name of your organization (used in the email template).

- OTPHtmlBodyTemplate: The HTML template for the OTP email. 
- Use {code} and {organization} placeholders.

---
## Caching Options
The library supports two caching mechanisms for OTP storage:

- Memory: Stores OTP in-memory (using MemoryCache).

- Redis: Stores OTP in a Redis database (requires RedisCacheService).
  
---
## Retry Mechanism
The SMTPService uses a retry policy with exponential backoff. It will retry sending emails up to three times if it encounters an error, waiting 2, 4, and 8 seconds between attempts.

---
## Logging
DeltaCore.EmailService integrates with ILogger for logging. You can log messages related to email sending, OTP generation, and verification.

---
## Contributing
Feel free to fork the repository, make improvements, and submit pull requests. Please ensure that your contributions follow the existing coding standards and include appropriate tests.

- Fork the repository.

- Clone your fork and create a new branch.

- Make your changes and commit them.

- Push to your fork and submit a pull request.

---
###License
This project is licensed under the MIT License.

---
## Support
If you encounter any issues or have questions about using DeltaCore.EmailService, feel free to create an issue on GitHub or reach out to us.

---
## Example Usage Flow:
- Send OTP: When a user registers, send them an OTP to verify their email address.

- Verify OTP: User submits the OTP they received, and you verify it.

- Send Email: Once the OTP is verified, send a welcome email or any other relevant communication.

---
## Author


👤 **Ahmed Nady**

[![GitHub](https://img.shields.io/badge/GitHub-000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/AhmedNady2003) [![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/ahmed-nady-386383266/) [![Gmail](https://img.shields.io/badge/Gmail-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:ahmednady122003@gmail.com)
