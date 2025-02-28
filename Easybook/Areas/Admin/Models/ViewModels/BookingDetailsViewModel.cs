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
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfDays => (CheckOutDate - CheckInDate).Days;
        public List<RoomDetailViewModel> RoomDetails { get; set; }
    }

    public class RoomDetailViewModel
    {
        public string RoomType { get; set; }
        public int Quantity { get; set; }
        public decimal RoomPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }


}
