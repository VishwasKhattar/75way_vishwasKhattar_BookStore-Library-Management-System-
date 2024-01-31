using System;
using Login_Signup_Api.Models;
using Microsoft.Data.SqlClient;

namespace Login_Signup_Api.Services
{
	public interface IMonthlyRentalService
	{
        Task AddMonthlyRevenue(int bookId, int month);

    }

	public class MonthlyRentalService : IMonthlyRentalService
	{
        public IConfiguration _config { get; set; }
        public IDBConnection _db { get; set; }
        public IBookService _bookService { get; set; }

        public MonthlyRentalService(IConfiguration config, IDBConnection db, IBookService bookService)
		{
            _config = config;
            _db = db;
            _bookService = bookService;
        }

        //Service to directly add the book price to the current revenue of the month
        public async Task AddMonthlyRevenue (int bookId , int month)
        {
            try
            {
                BookModel data = new BookModel();
                data = await _bookService.GetBookById(bookId);

                var price = data.Price;

                bool checkMonth = MonthExist(month);

                if (checkMonth)
                {
                    string sql = "UPDATE MonthlyRentals SET revenue = revenue + @price WHERE months = @CurrentMonth;";
                    using(var conn = new SqlConnection(_db.GetConnectionString()))
                    {
                        conn.Open();
                        using(var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    string sql = "Insert into MonthlyRentals(month , revenue) values(@month , @revenue);";
                    using(var conn = new SqlConnection(_db.GetConnectionString()))
                    {
                        conn.Open();
                        using(var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.Parameters.AddWithValue("@month", month);
                            cmd.Parameters.AddWithValue("@revenue", price);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }

        private bool MonthExist(int month)
        {
            try
            {
                var result = 0;
                string sql = "Select * from monthlyRentals where months = @month";
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@month", month);
                        result = cmd.ExecuteNonQuery();
                    }
                }
                if (result > 0)
                {
                    return true;
                }
                return false;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }

    }
}

