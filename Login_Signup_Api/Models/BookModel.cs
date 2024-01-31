using System;
namespace Login_Signup_Api.Models
{
	public class BookModel
	{
        public string Title { get; set; }
        public string Author { get; set; }
        public int Quantity { get; set; }
        public int TimesRented { get; set; }
        public int Price { get; set; }
    }
}

