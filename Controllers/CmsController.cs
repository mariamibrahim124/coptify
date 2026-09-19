using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Coptify.Web.Data;
using Coptify.Web.Models;

namespace Coptify.Web.Controllers;

[ApiController, Route("api")]
public class CmsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CmsController(AppDbContext db) => _db = db;

    [HttpGet("videos")]
    public async Task<IActionResult> Videos()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return Ok(await _db.Videos.Where(v => v.Published).OrderBy(v => v.SortOrder).ThenByDescending(v => v.Id).ToListAsync());
    }

    [Authorize(Roles = "Admin"), HttpGet("admin/videos")]
    public async Task<IEnumerable<Video>> AdminVideos() => await _db.Videos.OrderBy(v => v.SortOrder).ThenByDescending(v => v.Id).ToListAsync();

    [Authorize(Roles = "Admin"), HttpPost("videos")]
    public async Task<ActionResult<Video>> AddVideo(Video video)
    {
        video.Id = 0;
        _db.Videos.Add(video);
        await _db.SaveChangesAsync();
        return Ok(video);
    }

    [Authorize(Roles = "Admin"), HttpPut("videos/{id:int}")]
    public async Task<IActionResult> EditVideo(int id, Video input)
    {
        var v = await _db.Videos.FindAsync(id);
        if (v is null) return NotFound();
        input.Id = id;
        _db.Entry(v).CurrentValues.SetValues(input);
        await _db.SaveChangesAsync();
        return Ok(v);
    }

    [Authorize(Roles = "Admin"), HttpDelete("videos/{id:int}")]
    public async Task<IActionResult> DeleteVideo(int id)
    {
        var v = await _db.Videos.FindAsync(id);
        if (v is null) return NotFound();
        _db.Videos.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("content")]
    public async Task<IActionResult> Content()
    {
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return Ok(await _db.SiteContents.OrderBy(x => x.Section).ThenBy(x => x.Id).ToListAsync());
    }

    [Authorize(Roles = "Admin"), HttpGet("admin/content")]
    public async Task<IEnumerable<SiteContent>> AdminContent() => await _db.SiteContents.OrderBy(x => x.Section).ThenBy(x => x.Id).ToListAsync();

    [Authorize(Roles = "Admin"), HttpPost("content")]
    public async Task<ActionResult<SiteContent>> AddContent(SiteContent input)
    {
        _db.SiteContents.Add(input);
        await _db.SaveChangesAsync();
        return Ok(input);
    }

    [Authorize(Roles = "Admin"), HttpPut("content/{id:int}")]
    public async Task<IActionResult> EditContent(int id, SiteContent input)
    {
        var c = await _db.SiteContents.FindAsync(id);
        if (c is null) return NotFound();
        c.Label = input.Label;
        c.Section = input.Section;
        c.Value = input.Value;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [Authorize(Roles = "Admin"), HttpDelete("content/{id:int}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        var c = await _db.SiteContents.FindAsync(id);
        if (c is null) return NotFound();
        _db.SiteContents.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me() => User.Identity?.IsAuthenticated == true
        ? Ok(new { authenticated = true, user = User.Identity.Name })
        : Unauthorized();

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest input)
    {
        var user = await _db.AdminUsers.SingleOrDefaultAsync(x => x.Username == input.Username.Trim());
        if (user is null || !PasswordService.Verify(input.Password, user.PasswordSalt, user.PasswordHash))
            return Unauthorized(new { message = "بيانات الدخول غير صحيحة" });

        await SignInAdmin(user.Username);
        return Ok(new { authenticated = true, user = user.Username });
    }

    [Authorize(Roles = "Admin"), HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [Authorize(Roles = "Admin"), HttpGet("admin/account")]
    public async Task<IActionResult> Account()
    {
        var user = await CurrentAdmin();
        return Ok(new { username = user?.Username ?? User.Identity?.Name });
    }

    [Authorize(Roles = "Admin"), HttpPut("admin/account")]
    public async Task<IActionResult> ChangeAccount(AccountChangeRequest input)
    {
        var user = await CurrentAdmin();
        if (user is null) return Unauthorized();
        if (!PasswordService.Verify(input.CurrentPassword, user.PasswordSalt, user.PasswordHash))
            return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة." });

        var newUsername = string.IsNullOrWhiteSpace(input.NewUsername) ? user.Username : input.NewUsername.Trim();
        if (newUsername.Length < 3) return BadRequest(new { message = "اسم المستخدم يجب أن يكون 3 أحرف على الأقل." });
        var duplicate = await _db.AdminUsers.AnyAsync(x => x.Id != user.Id && x.Username == newUsername);
        if (duplicate) return BadRequest(new { message = "اسم المستخدم مستخدم بالفعل." });

        user.Username = newUsername;
        if (!string.IsNullOrWhiteSpace(input.NewPassword))
        {
            if (input.NewPassword.Length < 8) return BadRequest(new { message = "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل." });
            var p = PasswordService.Create(input.NewPassword);
            user.PasswordSalt = p.Salt;
            user.PasswordHash = p.Hash;
        }
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await SignInAdmin(user.Username);
        return Ok(new { username = user.Username });
    }

    private async Task<AdminUser?> CurrentAdmin()
    {
        var username = User.Identity?.Name;
        return string.IsNullOrWhiteSpace(username) ? null : await _db.AdminUsers.SingleOrDefaultAsync(x => x.Username == username);
    }

    private async Task SignInAdmin(string username)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}

public record LoginRequest(string Username, string Password);
public record AccountChangeRequest(string CurrentPassword, string? NewUsername, string? NewPassword);

public static class PasswordService
{
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static (string Salt, string Hash) Create(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public static bool Verify(string password, string saltText, string hashText)
    {
        try
        {
            var salt = Convert.FromBase64String(saltText);
            var expected = Convert.FromBase64String(hashText);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }
}
