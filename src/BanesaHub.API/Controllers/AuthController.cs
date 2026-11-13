using System.Net.Mail;
using System.Text;
using BanesaHub.API.Auth;
using Microsoft.AspNetCore.WebUtilities;
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ResetRequest request, [FromServices] IPasswordResetDelivery delivery,
        [FromServices] ILogger<AuthController> logger, CancellationToken cancellationToken)
    {
        if (!delivery.IsConfigured)
        {
            return Problem(statusCode: 503, title: "Rivendosja e fjalëkalimit nuk është në dispozicion.");
        }

        var user = await users.FindByEmailAsync(request.Email);
        if (user?.Email is { } email)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            try
            {
                await delivery.SendAsync(email, code, cancellationToken);
            }
            catch (SmtpException)
            {
                logger.LogWarning("Password reset email could not be sent.");
            }
        }

        return Accepted();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetConfirmation request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            try
            {
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
                var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
                if (result.Succeeded)
                {
                    return NoContent();
                }
            }
            catch (FormatException)
            {
                // Malformed and invalid tokens receive the same response.
            }
        }

        return Problem(statusCode: 400, title: "Rivendosja nuk u krye. Kontrolloni kodin dhe fjalëkalimin e ri.");
    }

    public sealed record ResetRequest([Required, EmailAddress, StringLength(256)] string Email);

    public sealed record ResetConfirmation(
        [Required, EmailAddress, StringLength(256)] string Email,
        [Required, StringLength(2048)] string Code,
        [Required, StringLength(128)] string NewPassword);

    public sealed record Credentials(
        [Required, EmailAddress, StringLength(256)] string Email,
        [Required, StringLength(128)] string Password);
}
