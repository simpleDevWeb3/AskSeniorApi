using AskSeniorApi.DTO;
using AskSeniorApi.Helper;
using AskSeniorApi.Models;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using System.IdentityModel.Tokens.Jwt;
using static AskSeniorApi.Models.Auth;
using static Supabase.Postgrest.Constants;

namespace AskSeniorApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Client _supabase;


        public AuthController(Client supabase)
        {
            _supabase = supabase;
           
        }

        // ---------------- SIGNUP ----------------
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromForm] SignUpRequest req)
        {
            try
            {
                // req.AvatarFile and req.BannerFile are IFormFile
                var avatarFile = req.AvatarFile;
                var bannerFile = req.BannerFile;

                // save files to server or cloud storage, generate URLs
                string avatarUrl = await UploadFile.UploadFileAsync(req.AvatarFile, "Avatar", _supabase);
                string bannerUrl = await UploadFile.UploadFileAsync(req.BannerFile, "Banner", _supabase);


                var res = await _supabase.Auth.SignUp(req.Email, req.Password);

                if (res.User != null)
                {
                    var dtoData = new User
                    {
                        id = res.User.Id,
                        name = req.name,
                        avatar_url = avatarUrl,
                        banner_url = bannerUrl,
                        bio = req.bio,
                        created_at = DateTime.UtcNow
                    };

                    var insertRes = await _supabase.From<User>().Insert(dtoData);

                    if (insertRes == null)
                        return BadRequest(new { error = "User profile creation failed." });

                    if (req.Preference != null)
                    {
                        foreach (var topicId in req.Preference)
                        {
                            var userTopicPreference = new UserTopicPreference
                            {
                                user_id = res.User.Id,
                                topic_id = topicId,
                                created_at = DateTime.UtcNow
                            };
                            await _supabase.From<UserTopicPreference>().Insert(userTopicPreference);
                        }
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
                return BadRequest(new { error = ex.Message });
            }
        }


        // ---------------- LOGIN ----------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LogInRequest req)
        {
            try
            {
                var res = await _supabase.Auth.SignIn(req.Email, req.Password);

                if (res.User == null)
                    return Unauthorized(new { message = "Invalid email or password." });

                var profileRes = await _supabase
                                    .From<User>()
                                    .Select("*")
                                    .Filter("id", Operator.Equals, res.User.Id)
                                    .Single();

                if (profileRes == null)
                    return BadRequest(new { error = "Profile not found." });

                var profile = profileRes;

                var profileDto = new UserDto
                {
                    id = profile.id,
                    name = profile.name,
                    avatar_url = profile.avatar_url,
                    banner_url = profile.banner_url,
                    bio = profile.bio,
                    created_at = profile.created_at
                };

                return Ok(new
                {
                    role = res.User.Role,
                    accessToken = res.AccessToken,
                    message = "Login successful.",
                    profile = profileDto
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ---------------- LOGOUT ----------------
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var session = _supabase.Auth.CurrentSession;

                if (session == null)
                    return BadRequest(new { message = "No active session. User is not logged in." });

                await _supabase.Auth.SignOut();

                return Ok(new { message = "User successfully signed out." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ---------------- CURRENT USER ----------------
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var accessToken = Request.Headers["Authorization"]
                    .ToString()
                    .Replace("Bearer ", "");

                if (string.IsNullOrEmpty(accessToken))
                    return Unauthorized(new { message = "Missing access token" });

                // Decode JWT to get user id
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(accessToken);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Invalid token" });

                // Fetch profile from Supabase
                var profile = await _supabase
                    .From<User>()
                    .Select("*")
                    .Filter("id", Operator.Equals, userId)
                    .Single();

                if (profile == null)
                    return NotFound(new { message = "Profile not found" });

                var profileDto = new UserDto
                {
                    id = profile.id,
                    name = profile.name,
                    avatar_url = profile.avatar_url,
                    banner_url = profile.banner_url,
                    bio = profile.bio,
                    created_at = profile.created_at
                };

                return Ok(new
                {
                    isAuthenticated = true,
                    profile = profileDto
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Invalid or expired token", error = ex.Message });
            }
        }
    }
}
