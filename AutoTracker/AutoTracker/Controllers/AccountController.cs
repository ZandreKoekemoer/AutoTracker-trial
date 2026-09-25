using AutoTracker.Models;
using AutoTracker.Services;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoTracker.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _authService;

    public AccountController(AuthService authService) => _authService = authService;

    public IActionResult Register()
    {
        if (_authService.HasUsers())
        {
            TempData["Info"] = "Staff accounts are created by an administrator or workshop manager.";
            return RedirectToAction(nameof(Login));
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (_authService.HasUsers())
        {
            ModelState.AddModelError("", "Staff accounts are created by an administrator or workshop manager.");
            return View(model);
        }
        if (!ModelState.IsValid) return View(model);
        if (_authService.EmailExists(model.Email))
        {
            ModelState.AddModelError("", "Unable to create the account with those details.");
            return View(model);
        }

        var user = _authService.Register(model);
        await SignInUserAsync(user, true);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        ViewBag.CanRegister = !_authService.HasUsers();
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CanRegister = !_authService.HasUsers();
            return View(model);
        }

        var user = _authService.Login(model);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            ViewBag.CanRegister = !_authService.HasUsers();
            return View(model);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await SignInUserAsync(user, model.RememberMe);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignInUserAsync(AppUser user, bool persistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("company_id", user.CompanyId.ToString()),
            new("session_version", user.SessionVersion.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties
        {
            IsPersistent = persistent,
            AllowRefresh = true,
            ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            properties);

        HttpContext.Session.SetInt32("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.FullName);
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetInt32("CompanyId", user.CompanyId);
    }
}
