//using System.Net;
//using System.Net.Mail;

//namespace GameStore.Helpers
//{
//    public class MailHelper
//    {
//        private readonly IConfiguration _config;

//        public MailHelper(IConfiguration config)
//        {
//            _config = config;
//        }

//        public bool SendEmail(string toEmail, string subject, string body)
//        {
//            try
//            {
//                var smtpHost = _config["Email:SmtpHost"];
//                var smtpPort = int.Parse(_config["Email:SmtpPort"]);
//                var fromEmail = _config["Email:FromEmail"];
//                var password = _config["Email:Password"];

//                var client = new SmtpClient(smtpHost, smtpPort)
//                {
//                    Credentials = new NetworkCredential(fromEmail, password),
//                    EnableSsl = true
//                };

//                var mail = new MailMessage
//                {
//                    From = new MailAddress(fromEmail, "GameStore"),
//                    Subject = subject,
//                    Body = body,
//                    IsBodyHtml = true
//                };

//                mail.To.Add(toEmail);

//                client.Send(mail);

//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//    }
//}


using Resend;

namespace GameStore.Helpers
{
    public class MailHelper
    {
        private readonly ResendClient _resend;

        public MailHelper(ResendClient resend)
        {
            _resend = resend;
        }

        // Gửi email sử dụng Resend API
        public async Task<bool> SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Tạo đối tượng email message với thông tin cần thiết
                var message = new EmailMessage()
                {
                    From = "onboarding@resend.dev",
                    Subject = subject,
                    HtmlBody = body
                };

                message.To.Add(toEmail);

                // Gửi email bất đồng bộ và chờ kết quả
                await _resend.EmailSendAsync(message);

                Console.WriteLine("✅ Email sent");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Resend error: " + ex.ToString());
                return false;
            }
        }
    }
}