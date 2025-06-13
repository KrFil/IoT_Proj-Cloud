using Azure;
using Azure.Communication.Email;
using System.Threading.Tasks;

namespace Backend
{
    public class EmailOperator
    {
        private readonly EmailClient client;
        private readonly string sender;

        public EmailOperator(string connectionString, string senderAddress)
        {
            client = new EmailClient(connectionString);
            sender = senderAddress;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var message = new EmailMessage(sender, to, new EmailContent(subject)
            {
                PlainText = body
            });

            var response = await client.SendAsync(WaitUntil.Completed, message);
            Console.WriteLine($"E-mail wysłany. Status: {response.Value.Status}");

        }
    }
}
