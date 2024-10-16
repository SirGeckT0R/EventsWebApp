using EventsWebApp.Application.Interfaces.Services;
using EventsWebApp.Application.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
namespace EventsWebApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly IUserService _userService;

        public AuthenticationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
        {
            var (accessToken, refreshToken) = await _userService.Login(loginRequest.email, loginRequest.password, cancellationToken);
            HttpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions { Domain = "localhost" });
            HttpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions { Domain = "localhost" });
            return Ok((accessToken, refreshToken));
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
        {
            var (accessToken,refreshToken) = await _userService.Register(registerRequest.email, registerRequest.password, registerRequest.username, cancellationToken);
            HttpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions { Domain ="localhost"});
            HttpContext.Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions { Domain = "localhost" });
            return Ok((accessToken, refreshToken));
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            if (HttpContext.Request.Cookies["accessToken"] != null)
            {
                HttpContext.Response.Cookies.Append("accessToken", "", new CookieOptions { Domain = "localhost", Expires = DateTime.Now.AddDays(-1) });
            }

            if (HttpContext.Request.Cookies["refreshToken"] != null)
            {
                HttpContext.Response.Cookies.Append("refreshToken", "", new CookieOptions { Domain = "localhost", Expires = DateTime.Now.AddDays(-1) });
            }
            return Ok();
        }

        [HttpGet("/getRole")]
        [Authorize]
        public IActionResult GetRole(CancellationToken cancellationToken)
        {
            var accessToken = HttpContext.Request.Cookies["accessToken"];

            cancellationToken.ThrowIfCancellationRequested();
            var role = accessToken.IsNullOrEmpty() ? null : _userService.GetRoleByToken(accessToken, cancellationToken);

            return Ok(role);
        }

        [HttpPost("/refresh")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var accessToken = HttpContext.Request.Cookies["accessToken"];
            var refreshToken = HttpContext.Request.Cookies["refreshToken"];

            cancellationToken.ThrowIfCancellationRequested();
            accessToken = await _userService.RefreshToken(accessToken, refreshToken, cancellationToken);
            
            HttpContext.Response.Cookies.Append("accessToken", accessToken, new CookieOptions { Domain = "localhost" });
            return Ok((accessToken, refreshToken));
        }
    }
}