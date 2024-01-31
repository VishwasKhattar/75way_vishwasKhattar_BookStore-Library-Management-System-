using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Login_Signup_Api.Dto;
using Login_Signup_Api.Models;
using Login_Signup_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;


namespace Login_Signup_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        public IConfiguration _config { get; set; }
        public IAuthService _authService { get; set; }

        public AuthController(IConfiguration config , IAuthService authService)
        {
            _config = config;
            _authService = authService;
        }

        //1. Route to Register a new user
        [HttpPost("/register")]
        public async Task<IActionResult> addUser(UserModel userModel)
        {
            try
            {
                var createUser = await _authService.Register(userModel);
                return Ok(new {status = true , createUser});
            }catch(Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        //2. Route to login the user and get the jwt token for authorization
        [HttpPost("/login")]
        public async Task<IActionResult> login(UserModelDto userModelDto)
        {
            try
            {
                //Getting token in data from auth Service login functionality
                var data = _authService.Login(userModelDto);
                return Ok(new {status = true , data});

            }catch(Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        

    }
}

