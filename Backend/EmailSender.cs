using MailKit.Net.Smtp;
using MimeKit;
using System;

namespace Backend
{
    public static class EmailService
    {
        public static void SendEmail(string deviceId, int errorCode)
        {
            
            string to = "f59115351@gmail.com";
            string subject = "Błąd urządzenia Device1";
            string body = $"Wykryto nowy kod błędu {errorCode} na urządzeniu {deviceId}.";

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Admin", "kfil59145@gmail.com"));  
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart("plain") { Text = body };

            try
            {
                using var smtp = new SmtpClient();
                smtp.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                smtp.Authenticate("kfil59145@gmail.com", "paasss12121#@GG%'");  
                smtp.Send(email);
                smtp.Disconnect(true);

                Console.WriteLine("Mail wysłany pomyślnie!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd przy wysyłaniu maila: {ex.Message}");
            }
        }
    }
}
