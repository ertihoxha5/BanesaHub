using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BanesaHub.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AutoValidateAntiforgeryToken]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    UserManager<IdentityUser<Guid>> users,
    SignInManager<IdentityUser<Guid>> signIn,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("antiforgery")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetAntiforgeryToken() =>
        Ok(new { requestToken = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("register")]
    public async Task<IActionResult> Register(Credentials request)
    {
        var user = new IdentityUser<Guid>
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email
        };

        var result = await users.CreateAsync(user, request.Password);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created)
            : Problem(statusCode: 400, title: "Regjistrimi nuk u krye. Kontrolloni të dhënat dhe kërkesat e fjalëkalimit.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(Credentials request)
    {
        var result = await signIn.PasswordSignInAsync(
            request.Email, request.Password, isPersistent: false, lockoutOnFailure: true);

        return result.Succeeded
            ? NoContent()
            : Problem(statusCode: 401, title: "Hyrja nuk u krye. Kontrolloni të dhënat ose provoni më vonë.");
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    public sealed record Credentials(
        [Required, EmailAddress, StringLength(256)] string Email,
        [Required, StringLength(128)] string Password);
}
