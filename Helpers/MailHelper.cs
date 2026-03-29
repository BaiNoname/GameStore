using System.Net;
using System.Net.Mail;

namespace GameStore.Helpers
{
    public class MailHelper
    {
        private readonly IConfiguration _config;

        public MailHelper(IConfiguration config)
        {
            _config = config;
        }

        public bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Lấy config cơ bản
                var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _config["Email:SmtpHost"];
                var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? _config["Email:SmtpPort"]);
                var fromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? _config["Email:FromEmail"];
                var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? _config["Email:Password"];

                var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(fromEmail, password),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, "GameStore"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                client.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendEmail error: " + ex.Message); // debug
                return false;
            }
        }
    }
}
