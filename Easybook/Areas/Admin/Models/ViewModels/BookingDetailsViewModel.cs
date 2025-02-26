namespace Easybook.Areas.Admin.Models.ViewModels
{
    public class BookingDetailsViewModel
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public string HotelName { get; set; }
        public decimal TotalPrice { get; set; }
        public string SpecialRequests { get; set; }
        public List<(string RoomType, int Quantity)> RoomDetails { get; set; }
    }

}
