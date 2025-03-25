namespace Easybook.Models.ViewModels
{
    public class HotelsDatesViewModel
    {
        public List<HotelViewModel>? Hotels { get; set; }
        public SearchViewModel? SearchParams { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
