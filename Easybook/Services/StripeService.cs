using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;

namespace Easybook.Services
{
    public class StripeService
    {
        private readonly IConfiguration _configuration;

        public StripeService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSession(decimal amount, int bookingId, string successUrl, string cancelUrl)
        {
            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "bgn",  // Or any other currency you're using
                            UnitAmount = (long)(amount * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Hotel Booking"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Locale = "bg",
                Metadata = new Dictionary<string, string>
                {
                    { "bookingId", bookingId.ToString() }  // Pass the bookingId in metadata
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(sessionOptions);

            return session.Url; // Return checkout URL to redirect the user
        }
    }
}
