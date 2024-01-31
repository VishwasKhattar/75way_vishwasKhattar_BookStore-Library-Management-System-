using System;
using Login_Signup_Api.Dto;
using Login_Signup_Api.Models;
using Login_Signup_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Login_Signup_Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]

    public class BookController : ControllerBase
	{

        public IConfiguration _config { get; set; }
        public IBookService _bookService { get; set; }

        public BookController(IConfiguration config , IBookService bookService)
		{
            _config = config;
            _bookService = bookService;
		}

        //1. Route to create the book

        [HttpPost("/createBook")]
        public async Task<IActionResult> CreateBook([FromBody]BookModel bookModel)
        {
            try
            {
                int newBookId = await _bookService.CreateBook(bookModel);
                return Ok(new { status = true, newBookId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //2. Route to get the book

        [HttpGet("/getAllBooks")]
        public async Task<IActionResult> GetAllBooks()
        {
            try
            {
                var data = await _bookService.getAllBooks();
                return Ok(new { status = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //3. Route to get the book by id

        [HttpGet("/getBookById/{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            try
            {
                var book = await _bookService.GetBookById(id);

                if (book != null)
                {
                    return Ok(new { status = true, book });
                }
                else
                {
                    return NotFound(new { status = false, message = "Book not found" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //4. Route for Deleting the book by id

        [HttpDelete("/deleteBookById/{id}")]
        public async Task<IActionResult> DeleteBookById(int id)
        {
            try
            {
                int rowsDeleted = await _bookService.DeleteBookById(id);

                if (rowsDeleted > 0)
                {
                    return Ok(new { status = true, message = "Book deleted successfully" });
                }
                else
                {
                    return NotFound(new { status = false, message = "Book not found" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //5. Route for creating the book by extracting the data from excel file

        [HttpPost("createBooksFromExcel")]
        public async Task<IActionResult> CreateBooksFromExcel([FromBody] string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return BadRequest(new { status = false, message = "File path not provided" });
                }

                int insertedRecords = await _bookService.CreateBooksFromExcel(filePath);

                return Ok(new { status = true, message = $"{insertedRecords} records inserted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //6. Get list of All books with the left quantity

        [HttpGet("/getAllBooksAndQuantity")]
        public async Task<IActionResult> GetAllBooksAndQuantity()
        {
            try
            {
                List<BookDto> data = new List<BookDto>();
                data = await _bookService.getAllBooksAndQuantity();
                return Ok(new { status = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //7. Get Top 10 popular bookas

        [HttpGet("/popularBooks")]
        public async Task<IActionResult> getPopularBooks()
        {
            try
            {
                List<BookModel> data = new List<BookModel>();
                data = await _bookService.GetTop10PopularBooks();
                return Ok(new { status = true , data});
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

