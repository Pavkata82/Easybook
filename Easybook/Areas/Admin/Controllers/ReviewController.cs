using Easybook.Constants;
using Easybook.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Easybook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)] // Restrict access to Admins only
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult ManageReviews(string search)
        {
            // Start by querying all reviews
            var reviewsQuery = _context.Reviews
                .Include(r => r.User)    // Include the User navigation property
                .Include(r => r.Hotel)   // Include the Hotel navigation property
                .AsQueryable();

            // If search query is provided, filter reviews by user email
            if (!string.IsNullOrEmpty(search))
            {
                reviewsQuery = reviewsQuery.Where(r => r.User.Email.Contains(search)); // Search by email
            }

            // Execute the query and get the result
            var reviews = reviewsQuery.ToList();

            return View(reviews); // Pass the filtered reviews to the view
        }


        public IActionResult Details(int id)
        {
            // Retrieve the review with its associated User and Hotel data
            var review = _context.Reviews
                .Include(r => r.User)   // Include the User navigation property
                .Include(r => r.Hotel)  // Include the Hotel navigation property
                .FirstOrDefault(r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound(); // Return a 404 if the review is not found
            }

            return View(review); // Pass the review to the view
        }


        [HttpPost]
        public IActionResult Delete(int id)
        {
            var review = _context.Reviews.Find(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                _context.SaveChanges();
            }
            return RedirectToAction("ManageReviews");
        }
    }
}
