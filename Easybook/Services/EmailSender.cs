using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace Easybook.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _fromEmail = "pavelpavlov20b@gmail.com"; // SES verified email
        private readonly AmazonSimpleEmailServiceClient _sesClient;

        // Initialize SES client
        public EmailSender(IConfiguration configuration)
        {
            var accessKey = configuration["AWS:AccessKey"];
            var secretKey = configuration["AWS:SecretKey"];
            var region = RegionEndpoint.GetBySystemName(configuration["AWS:Region"]);

            // Initialize SES client with credentials from appsettings.json
            _sesClient = new AmazonSimpleEmailServiceClient(accessKey, secretKey, region);
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var sendRequest = new SendEmailRequest
            {
                Source = _fromEmail, // SES verified email
                Destination = new Destination
                {
                    ToAddresses = new List<string> { email }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content(message)
                    }
                }
            };

            try
            {
                // Send the email through SES
                var response = await _sesClient.SendEmailAsync(sendRequest);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
