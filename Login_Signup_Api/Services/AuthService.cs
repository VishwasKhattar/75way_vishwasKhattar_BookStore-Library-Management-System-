using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure.Core;
using Login_Signup_Api.Dto;
using Login_Signup_Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace Login_Signup_Api.Services
{
    public interface IAuthService
    {
        Task<bool> Register(UserModel userModel);
        Task<string> Login(UserModelDto userModelDto);

    }
    public class AuthService : IAuthService
	{
        public IDBConnection _db { get; set; }
        public IConfiguration _config { get; set; }

        public AuthService(IDBConnection db , IConfiguration config)
		{
            _db = db;
            _config = config;
		}

        //Service for register Functionality
        public async Task<bool> Register(UserModel userModel)
        {
            try
            {
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userModel.passwords);
                string sql = "Insert into users(name , email , passwords) values (@name , @email , @passwords)";
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    using(var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@name", userModel.name);
                        cmd.Parameters.AddWithValue("@email" , userModel.email);
                        cmd.Parameters.AddWithValue("@passwords" , hashedPassword);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message); 
            }


            return true;
        }

        //Service for Login Functionality where after login you will get the JWT token to get authorized pages
        public async Task<string> Login(UserModelDto userModelDto)
        {
            try
            {
                if (userModelDto.email == null || userModelDto.passwords == null)
                {
                    return "Please enter all the fields properly";
                }

                string sql = "SELECT passwords FROM users WHERE email = @email";
                using (var conn = new SqlConnection(_db.GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@email", userModelDto.email);
                        var hashedPassword = cmd.ExecuteScalar() as string;

                        if (hashedPassword != null)
                        {
                            // Use BCrypt.Verify to check if the entered password matches the stored hashed password
                            if (BCrypt.Net.BCrypt.Verify(userModelDto.passwords, hashedPassword))
                            {
                                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                                var claims = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Email, userModelDto.email)
                                };

                                var Sectoken = new JwtSecurityToken(_config["Jwt:Issuer"],
                                  _config["Jwt:Issuer"],
                                  claims,
                                  null,
                                  expires: DateTime.Now.AddMinutes(120),
                                  signingCredentials: credentials);

                                var token = new JwtSecurityTokenHandler().WriteToken(Sectoken);
                                return token; // Login successful
                            }
                        }

                        return "Invalid Credentials";
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        
	}
}

