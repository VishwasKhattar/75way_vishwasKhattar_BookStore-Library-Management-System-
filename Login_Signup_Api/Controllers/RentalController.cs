using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookstoreAPI.Models;
using BookstoreAPI.Services;
using Login_Signup_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BookstoreAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RentalController : ControllerBase
    {
        private IConfiguration _config;
        private IRentalService _rentalService;

        public RentalController(IConfiguration config, IRentalService rentalService)
        {
            _config = config;
            _rentalService = rentalService;
        }

        //1. Route to rent a book

        [HttpPost("/rentBook")]
        public async Task<IActionResult> RentBook(string userId, int bookId)
        {
            try
            {
                await _rentalService.RentBook(userId, bookId);
                return Ok(new { status = true, message = "Book rented successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }


        //2. Route to get the total number of rentals laste month

        [HttpGet("/getRentalsLastMonth")]
        public async Task<IActionResult> GetRentalsLastMonth()
        {
            try
            {
                var rentals = await _rentalService.GetRentalsLastMonth();
                return Ok(new { status = true, rentals });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }


        //3. Route to Calculate the penalty of a particular rental

        [HttpGet("/calculatePenalty/{rentalId}")]
        public async Task<IActionResult> CalculatePenalty(int rentalId)
        {
            try
            {
                var penalty = await _rentalService.CalculatePenalty(rentalId);
                return Ok(new { status = true, penalty });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }


        //4. Route to find the revenue in last one month

        [HttpGet("/revenueLastMonth")]
        public async Task<IActionResult> GetRevenueLastMonth()
        {
            try
            {
                var revenue = await _rentalService.GetRevenueLastMonth();
                return Ok(new { status = true, revenue });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }
    }
}
