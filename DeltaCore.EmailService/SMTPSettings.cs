

namespace DeltaCore.EmailService
{
    public class SMTPSettings
    {
        public string SMTPHost { get; set; }
        public int SMTPPort { get; set; }
        public string SMTPUser { get; set; }
        public string SMTPPassword { get; set; }
        public string FromEmail { get; set; }
        public string OrganizationName { get; set; }
        public string OTPHtmlBodyTemplate { get; set; }
        public string OTPHtmlBodyTemplatePath { get; set; }

    }
}
