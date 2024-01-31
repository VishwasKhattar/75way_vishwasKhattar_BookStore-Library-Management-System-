namespace BookstoreAPI.Models
{
    public class Rental
    {
        public int BookId { get; set; }
        public string UserId { get; set; }
        public DateTime RentalDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public int PenaltyAmount { get; set; }
    }
}