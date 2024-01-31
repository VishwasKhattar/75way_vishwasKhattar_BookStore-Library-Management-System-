using System;
using Login_Signup_Api.Dto;
using Login_Signup_Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace Login_Signup_Api.Services
{
    public interface IBookService
    {
        Task<int> CreateBook(BookModel bookModel); //To create a book
        Task<List<BookModel>> getAllBooks(); //To get all books
        Task<BookModel> GetBookById(int bookId); //To get the book by id
        Task<int> DeleteBookById(int bookId); //To delete the book by id
        Task<int> CreateBooksFromExcel(string filepath); //to cretae book from excel file
        Task UpdateBookQuantity(int bookId, int newQuantity); //to update book quantity (used while renting the book)
        Task<List<BookDto>> getAllBooksAndQuantity(); //To get all books and their quantity
        Task UpdateBookTimesRented(int bookId, int newTimesRented); //To update "times rented" attribute of books table (used in renting book)
        Task<List<BookModel>> GetTop10PopularBooks(); // To get Most popular books

    }

    public class BookService : IBookService
    {
        public IConfiguration _config { get; set; }
        public IDBConnection _db { get; set; }

        public BookService(IConfiguration config , IDBConnection db)
        {
            _config = config;
            _db = db;
        }

        //Service Method to Create Book in database

        public async Task<int> CreateBook(BookModel bookModel)
        {
            try
            {
                int newBookId;
                var sql = "INSERT INTO Books (Title, Author, Quantity, TimesRented , Price) VALUES (@Title, @Author, @Quantity, @TimesRented , @Price);";
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@Title", bookModel.Title);
                        cmd.Parameters.AddWithValue("@Author", bookModel.Author);
                        cmd.Parameters.AddWithValue("@Quantity", bookModel.Quantity);
                        cmd.Parameters.AddWithValue("@TimesRented", bookModel.TimesRented);
                        cmd.Parameters.AddWithValue("@Price", bookModel.Price);
                        newBookId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                }
                return newBookId;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }

        //Service method to get all books

        public async Task<List<BookModel>> getAllBooks()
        {
            List<BookModel> result = new List<BookModel>();
            try
            {
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    string sql = "select * from books";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        var data = cmd.ExecuteReader();

                        if (data.HasRows)
                        {
                            while (data.Read())
                            {
                                BookModel newBook = new BookModel();
                                newBook.Title = data["title"].ToString();
                                newBook.Author = data["author"].ToString();
                                newBook.Quantity = Convert.ToInt32(data["quantity"]);
                                newBook.TimesRented = Convert.ToInt32(data["timesRented"]);
                                result.Add(newBook);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        //Service method to get the book based on particular id

        public async Task<BookModel> GetBookById(int bookId)
        {
            BookModel result = new BookModel();
            try
            {
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    string sql = "SELECT * FROM Books WHERE Id = @BookId";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@BookId", bookId);
                        var data = await cmd.ExecuteReaderAsync();

                        if (data.HasRows)
                        {
                            while (data.Read())
                            {
                                result.Title = data["Title"].ToString();
                                result.Author = data["Author"].ToString();
                                result.Quantity = Convert.ToInt32(data["Quantity"]);
                                result.TimesRented = Convert.ToInt32(data["TimesRented"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        //Service method to delete the book

        public async Task<int> DeleteBookById(int bookId)
        {
            try
            {
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    string sql = "DELETE FROM Books WHERE Id = @BookId";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@BookId", bookId);
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to create book from excel file

        public async Task<int> CreateBooksFromExcel(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);

                //If File does not exists
                if(fileInfo == null)
                {
                    return 0;
                }

                using (var excelPackage = new ExcelPackage(fileInfo))
                {
                    var worksheet = excelPackage.Workbook.Worksheets[0]; // We assume that data is in the first sheet

                    // Verifying that all headers are present in the sheet that we require
                    var expectedHeaders = new List<string> { "Title", "Author", "Quantity", "TimesRented" , "Price"};
                    var actualHeaders = new List<string>();

                    foreach (var cell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
                    {
                        actualHeaders.Add(cell.Text.Trim());
                    }

                    if (!expectedHeaders.SequenceEqual(actualHeaders, StringComparer.OrdinalIgnoreCase))
                    {
                        throw new Exception("Invalid headers in the Excel file.");
                    }

                    var rows = worksheet.Dimension.End.Row;

                    for (int row = 2; row <= rows; row++)
                    {
                        var bookModel = new BookModel
                        {
                            Title = worksheet.Cells[row, 1].Text.Trim(),
                            Author = worksheet.Cells[row, 2].Text.Trim(),
                            Quantity = Convert.ToInt32(worksheet.Cells[row, 3].Text.Trim()),
                            TimesRented = Convert.ToInt32(worksheet.Cells[row, 4].Text.Trim()),
                            Price = Convert.ToInt32(worksheet.Cells[row, 5].Text.Trim())
                        };

                        // Inserting into the database using CreateBook method
                        await CreateBook(bookModel);
                    }

                    return rows - 1; // Returning the number of successfully inserted rows from the xlsx sheet
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to update book quantity called when a book is rented by someone

        public async Task UpdateBookQuantity(int bookId, int newQuantity)
        {
            try
            {
                var sql = "UPDATE Books SET Quantity = @Quantity WHERE Id = @BookId;";

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@BookId", bookId);
                        cmd.Parameters.AddWithValue("@Quantity", newQuantity);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to get the list of all books and their quantity in store

        public async Task<List<BookDto>> getAllBooksAndQuantity()
        {
            List<BookDto> result = new List<BookDto>();
            try
            {
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    string sql = "select id,quantity from books";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        var data = cmd.ExecuteReader();

                        if (data.HasRows)
                        {
                            while (data.Read())
                            {
                                BookDto newBook = new BookDto();
                                newBook.bookId = Convert.ToInt32(data["id"]);
                                newBook.quantity = Convert.ToInt32(data["quantity"]);
                                result.Add(newBook);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        //Service method to update times rented called during renting the book

        public async Task UpdateBookTimesRented(int bookId, int newTimesRented)
        {
            try
            {
                var sql = "UPDATE Books SET TimesRented = @NewTimesRented WHERE Id = @BookId;";

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@BookId", bookId);
                        cmd.Parameters.AddWithValue("@NewTimesRented", newTimesRented);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //Service method to get Top 10 books

        public async Task<List<BookModel>> GetTop10PopularBooks()
        {
            try
            {
                var sql = @"
            SELECT TOP 10 Id, Title, Author, Quantity, TimesRented
            FROM Books
            WHERE TimesRented > 1
            ORDER BY TimesRented DESC;";                //It will find the top 10 records after aranging the books in descending order of(Times Rented)

                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var popularBooks = new List<BookModel>();

                            while (await reader.ReadAsync())
                            {
                                var book = new BookModel
                                {
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"].ToString(),
                                    Quantity = Convert.ToInt32(reader["Quantity"]),
                                    TimesRented = Convert.ToInt32(reader["TimesRented"])
                                };

                                popularBooks.Add(book);
                            }

                            return popularBooks;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

