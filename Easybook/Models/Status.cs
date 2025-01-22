namespace Easybook.Models
{
    public class Status
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<Booking> bookings { get; set; } = new List<Booking>();
    }
}
