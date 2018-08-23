using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB.Models;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Models.AccountViewModels;
using MachinaTrader.Globals.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/auth/")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ISmsSender smsSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _smsSender = smsSender;
        }

        // Token based Auth -> Needed for nodejs and signalr
        // GET: /api/auth/token  
        [HttpGet]
        [AllowAnonymous, Route("token")]
        public async Task<string> Token(string returnUrl = null)
        {
            LoginViewModel model = new LoginViewModel();
            model.UserName = HttpContext.Request.Query["email"];
            model.Password = HttpContext.Request.Query["password"];
            model.RememberMe = true;

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                if (!user.AccountEnabled)
                {
                    Global.Logger.Information("User account locked out.");
                    return null;
                }
            }

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, model.UserName) };
                var credentials = new SigningCredentials(Startup.SecurityKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken("MachinaTrader", "MachinaTrader", claims, expires: DateTime.UtcNow.AddSeconds(30), signingCredentials: credentials);
                return Startup.JwtTokenHandler.WriteToken(token);
            }
            return null;
        }

        // Token based Auth -> Needed for nodejs and signalr
        // GET: /api/auth/tokenJson 
        [HttpPost]
        [AllowAnonymous, Route("tokenJson")]
        public async Task<string> TokenJson([FromBody] JObject data)
        {
            LoginViewModel model = new LoginViewModel();
            model.UserName = (string)data["email"];
            model.Password = (string)data["password"];
            model.RememberMe = true;

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user != null)
            {
                if (!user.AccountEnabled)
                {
                    Global.Logger.Information("User account locked out.");
                    return null;
                }
            }

            var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                Console.WriteLine((string)data["email"]);
                Console.WriteLine((string)data["password"]);
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, model.UserName) };
                var credentials = new SigningCredentials(Startup.SecurityKey, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken("MachinaTrader", "MachinaTrader", claims, expires: DateTime.UtcNow.AddSeconds(30), signingCredentials: credentials);
                return Startup.JwtTokenHandler.WriteToken(token);
            }
            return null;
        }

        // GET: /api/auth/check
        [HttpGet]
        [AllowAnonymous, Route("check")]
        public IActionResult Check()
        {
            dynamic checkLoginResult = new JObject();
            checkLoginResult.success = false;
            if (User.Identity.IsAuthenticated)
            {
                checkLoginResult.success = true;
            }
            return new JsonResult(checkLoginResult);
        }

        //
        // POST: /api/auth/logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("logout")]
        public async Task<IActionResult> LogOff()
        {
            JObject checkLoginResult = new JObject();
            checkLoginResult["success"] = false;
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                await _signInManager.SignOutAsync();
                Global.Logger.Information("User logged out.");
                checkLoginResult["success"] = true;
            }
            // Clear the principal to ensure the user does not retain any authentication
            HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
            Console.WriteLine("User logged out.");
            return new JsonResult(checkLoginResult);
        }
    }
}
