using Microsoft.AspNetCore.Mvc;
using Easybook.Services;
using Easybook.Data;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace Easybook.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, StripeService stripeService, IConfiguration configuration)
        {
            _context = context;
            _stripeService = stripeService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.StatusId != 4)  // Status 4 = Payment
            {
                return NotFound("Резервацията не е намерена или не е готова за плащане.");
            }

            var successUrl = Url.Action("Success", "Payment", new { bookingId = booking.BookingId }, Request.Scheme);
            var cancelUrl = Url.Action("Failure", "Payment", new { bookingId = booking.BookingId }, Request.Scheme);

            var sessionUrl = await _stripeService.CreateCheckoutSession(booking.TotalPrice, booking.BookingId, successUrl, cancelUrl);

            return Redirect(sessionUrl);
        }


        public async Task<IActionResult> Success(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.IsPaid = true;
                booking.StatusId = 1; // Status 4 = Paid
                await _context.SaveChangesAsync();
            }
            return View();
        }

        public async Task<IActionResult> Failure(int bookingId)
        {
            // You can load the booking here if needed, for example:
            var booking = await _context.Bookings.FindAsync(bookingId);

            // If the booking is not found, return an error page or redirect
            if (booking == null)
            {
                return NotFound("Резервацията не е намерена.");
            }

            return View(booking); // Pass bookingId to the view
        }


        [HttpPost]
        [Route("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"] // Store Webhook Secret in appsettings.json
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null && session.Metadata.TryGetValue("bookingId", out string bookingIdStr))
                    {
                        if (int.TryParse(bookingIdStr, out int bookingId))
                        {
                            var booking = await _context.Bookings.FindAsync(bookingId);
                            if (booking != null)
                            {
                                booking.IsPaid = true;
                                booking.StatusId = 1;  // Status 1 = Approved (Paid)
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest($"Webhook error: {e.Message}");
            }
        }

    }
}
