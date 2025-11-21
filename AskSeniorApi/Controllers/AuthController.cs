using AskSeniorApi.DTO;
using  AskSeniorApi.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using static AskSeniorApi.Models.Auth;

using static Supabase.Postgrest.Constants;

namespace AskSeniorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public AuthController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest req)
        {
            try
            {
                var res = await _supabase.Auth.SignUp(req.Email, req.Password);

                if (res.User != null)
                {

                    var dtoData =  new User
                    {
                        id = res.User.Id,
                        name = req.name,
                        avatar_url = req.avatar_url,
                        banner_url = req.banner_url,
                        bio = req.bio,
                        created_at = DateTime.UtcNow
                    };

                    var insertRes = await _supabase.From<User>().Insert(dtoData);

                    if (insertRes == null)
                    {
                        return BadRequest(new { error = "User profile creation failed." });
                    }


                }

                return Ok(new
                {
                    user = res.User,
                    accessToken = res.AccessToken,
                    refreshToken = res.RefreshToken,
                    expiresIn = res.ExpiresIn,
                    tokenType = res.TokenType,
                    message = res.User == null
                        ? "User created. Email confirmation required."
                        : "User created and logged in."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            try
            {
                var res = await _supabase.Auth.SignIn(req.Email, req.Password);

                if (res.User == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password."
                    });
                }

                var profileRes = await _supabase
                                .From<User>()
                                .Select("*")
                                .Filter("id", Operator.Equals, res.User.Id)
                                .Single();



                if (profileRes == null)
                {
                    return BadRequest(new { error = "Profile not found." });
                }

                var profile = profileRes;

                var profileDto = new UserDto
                {
                    id = profile.id,
                    name = profile.name,
                    avatar_url = profile.avatar_url,
                    banner_url = profile.banner_url,
                    bio = profile.bio,
                    created_at= profile.created_at,
                };



                return Ok(new
                {
                    user = res.User,
                    accessToken = res.AccessToken,
                    refreshToken = res.RefreshToken,
                    expiresIn = res.ExpiresIn,
                    tokenType = res.TokenType,
                    message = "Login successful.",
                    profile = profileDto,
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var session = _supabase.Auth.CurrentSession;

                if (session == null)
                {
                    return BadRequest(new
                    {
                        message = "No active session. User is not logged in."
                    });
                }

                await _supabase.Auth.SignOut();

                return Ok(new
                {
                    message = "User successfully signed out."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }





    }
}
