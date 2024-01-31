using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using BookstoreAPI.Models;
using Login_Signup_Api.Models;
using Login_Signup_Api;
using Login_Signup_Api.Services;
using Login_Signup_Api.Dto;

namespace BookstoreAPI.Services
{
    public interface IRentalService
    {
        Task RentBook(string userId, int bookId); //To rent a book
        Task<int> GetRentalsLastMonth(); // to get last month Rentals
        Task<int> CalculatePenalty(int rentalId);// To calculate penalty by rental id
        Task<int> GetRevenueLastMonth(); // To get Last month Revenue
    }

    public class RentalService : IRentalService
    {
        public IConfiguration _config { get; set; }
        public IDBConnection _db { get; set; }
        public IBookService _bookService { get; set; }
        public IMonthlyRentalService _monthlyRentalService { get; set; }


        public RentalService(IConfiguration config, IDBConnection db , IBookService bookService , IMonthlyRentalService monthlyRentalService)
        {
            _config = config;
            _db = db;
            _bookService = bookService;
            _monthlyRentalService = monthlyRentalService;
        }

        //Service method to rent a book to a user

        public async Task RentBook(string userId, int bookId)
        {
            try
            {
                // Check if the book is available for rent
                var book = await _bookService.GetBookById(bookId);

                if (book == null)
                {
                    throw new Exception("Book not found.");
                }

                if (book.Quantity <= 0)
                {
                    throw new Exception("The book is not available for rent.");
                }

                // Update book quantity (decrease by 1)
                await _bookService.UpdateBookQuantity(bookId, book.Quantity - 1);

                //Update times rented part in books table
                await _bookService.UpdateBookTimesRented(bookId, book.TimesRented + 1);

                //Update Monthly rental for the rented book id
                await _monthlyRentalService.AddMonthlyRevenue(bookId, DateTime.Now.Month);

                //Creating a new object to pass in insert rental record method
                var rental = new RentalDto
                {
                    UserId = userId,
                    BookId = bookId,
                    RentalDate = DateTime.Now,
                    ReturnDate = DateTime.Now.AddDays(5),
                    PenaltyAmount = 0
                };

                
                await InsertRentalRecord(rental);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Private function to insert rental records in database

        private async Task InsertRentalRecord(RentalDto rental)
        {
            try
            {
                var sql = "INSERT INTO Rentals (UserId, BookId, RentalDate, ReturnDate, PenaltyAmount) VALUES (@UserId, @BookId, @RentalDate, @ReturnDate, @PenaltyAmount);";

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@UserId", rental.UserId);
                        cmd.Parameters.AddWithValue("@BookId", rental.BookId);
                        cmd.Parameters.AddWithValue("@RentalDate", rental.RentalDate);
                        cmd.Parameters.AddWithValue("@ReturnDate", rental.ReturnDate);
                        cmd.Parameters.AddWithValue("@PenaltyAmount", rental.PenaltyAmount);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to find the total number of rentals in last one month

        public async Task<int> GetRentalsLastMonth()
        {
            try
            {
                var sql = "SELECT COUNT(DISTINCT BookId) AS TotalBooksRented FROM Rentals WHERE RentalDate >= DATEADD(month, DATEDIFF(month, 0, GETDATE()) - 1, 0) AND RentalDate < DATEADD(month, DATEDIFF(month, 0, GETDATE()), 0);";

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;

                        // ExecuteScalarAsync is used to retrieve a single value
                        var totalBooksRented = await cmd.ExecuteScalarAsync();

                        return (int)totalBooksRented;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to Calculate penalty

        public async Task<int> CalculatePenalty(int rentalId)
        {
            try
            {
                // Get rental details including return date
                var rental = await GetRentalById(rentalId);

                if (rental == null)
                {
                    throw new Exception("Invalid Rental id");
                }

                // If there is no return date or the return date is after today, penalty is 0
                if (!rental.ReturnDate.HasValue || rental.ReturnDate > DateTime.Now)
                {
                    return 0;
                }

                // Calculating the number of extra days after the return date
                int daysOverdue = (int)(DateTime.Now - rental.ReturnDate.Value).TotalDays;

                // Calculating penalty at the rate of 5 Rs/day
                int penalty = daysOverdue * 5;

                return penalty > 0 ? penalty : 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        //Private function to find the detail of particular rental from rental id

        private async Task<RentalDto> GetRentalById(int rentalId)
        {
            try
            {
                var sql = "SELECT * FROM Rentals WHERE Id = @RentalId;";

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@RentalId", rentalId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                var rental = new RentalDto
                                {
                                    BookId = Convert.ToInt32(reader["BookId"]),
                                    UserId = reader["UserId"].ToString(),
                                    RentalDate = Convert.ToDateTime(reader["RentalDate"]),
                                    ReturnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null,
                                    PenaltyAmount = Convert.ToInt32(reader["PenaltyAmount"])
                                };

                                return rental;
                            }
                            else
                            { 
                                throw new Exception("Rental Record not found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        //Service method to get the monthly revenue last month

        public async Task<int> GetRevenueLastMonth()
        {
            try
            {
                int revenueLastMonth = 0;
                int monthToCheck = DateTime.Now.Month - 1;
                string sql = "Select revenue from MonthlyRentals where month = @month;";
                using(var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@month", monthToCheck);
                        revenueLastMonth = (int)cmd.ExecuteScalar();
                    }
                }
                return revenueLastMonth;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


    }
}
