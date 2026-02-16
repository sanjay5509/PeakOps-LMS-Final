using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeakOps.Core.Interfaces;
using PeakOps.Core.ViewModels;
using System.Security.Claims;

namespace PeakOps.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _authService.Login(model);

            if (user != null)
            {
                
                var claims = new List<Claim>
        {
          new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UserId", user.Id.ToString())
        };

                
                
                var role = user.UserRoles?.FirstOrDefault()?.Role?.RoleName;
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                
                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");

                
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                
                await HttpContext.SignInAsync("CookieAuth", claimsPrincipal);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Invalid Email or Password!";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var isSuccess = _authService.RegisterUser(model);

            if (isSuccess)
            {
                
                return RedirectToAction("Login");
            }
            else
            {
               
                ViewBag.Error = "Email already exists!";
                return View(model);
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}