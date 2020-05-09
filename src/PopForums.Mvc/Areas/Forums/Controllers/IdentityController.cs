using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PopForums.Configuration;
using PopForums.Models;
using PopForums.Mvc.Areas.Forums.Authorization;
using PopForums.Mvc.Areas.Forums.Extensions;
using PopForums.Mvc.Areas.Forums.Models;
using PopForums.Mvc.Areas.Forums.Services;
using PopForums.Services;
using PopIdentity;
using PopIdentity.Providers.Facebook;
using PopIdentity.Providers.Google;
using PopIdentity.Providers.Microsoft;
using PopIdentity.Providers.OAuth2;

namespace PopForums.Mvc.Areas.Forums.Controllers
{
	[Area("Forums")]
	public class IdentityController : Controller
	{
		private readonly ILoginLinkFactory _loginLinkFactory;
		private readonly IStateHashingService _stateHashingService;
		private readonly ISettingsManager _settingsManager;
		private readonly IUserService _userService;
		private readonly IUserRetrievalShim _userRetrievalShim;
		private readonly ISecurityLogService _securityLogService;

		public IdentityController(ILoginLinkFactory loginLinkFactory, IStateHashingService stateHashingService, ISettingsManager settingsManager, IUserService userService, IUserRetrievalShim userRetrievalShim, ISecurityLogService securityLogService)
		{
			_loginLinkFactory = loginLinkFactory;
			_stateHashingService = stateHashingService;
			_settingsManager = settingsManager;
			_userService = userService;
			_userRetrievalShim = userRetrievalShim;
			_securityLogService = securityLogService;
		}

		public static string Name = "Identity";

		[PopForumsAuthorizationIgnore]
		[HttpPost]
		public async Task<IActionResult> Login(string email, string password)
		{
			var (result, user) = await _userService.Login(email, password, HttpContext.Connection.RemoteIpAddress.ToString());
			if (result)
			{
				await PerformSignInAsync(user, HttpContext);
				return Json(new BasicJsonMessage { Result = true });
			}

			return Json(new BasicJsonMessage { Result = false, Message = Resources.LoginBad });
		}

		[HttpGet]
		public async Task<RedirectResult> Logout()
		{
			string link;
			if (Request == null || string.IsNullOrWhiteSpace(Request.Headers["Referer"]))
				link = Url.Action("Index", HomeController.Name);
			else
			{
				link = Request.Headers["Referer"];
				if (!link.Contains(Request.Host.Value))
					link = Url.Action("Index", HomeController.Name);
			}
			var user = _userRetrievalShim.GetUser();
			await _userService.Logout(user, HttpContext.Connection.RemoteIpAddress.ToString());
			await HttpContext.SignOutAsync(PopForumsAuthorizationDefaults.AuthenticationScheme);
			return Redirect(link);
		}

		[HttpPost]
		public async Task<JsonResult> LogoutAsync()
		{
			var user = _userRetrievalShim.GetUser();
			await _userService.Logout(user, HttpContext.Connection.RemoteIpAddress.ToString());
			await HttpContext.SignOutAsync(PopForumsAuthorizationDefaults.AuthenticationScheme);
			return Json(new BasicJsonMessage { Result = true });
		}

		public static async Task PerformSignInAsync(User user, HttpContext httpContext)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.Name, user.Name)
			};

			var props = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTime.UtcNow.AddYears(1)
			};

			var id = new ClaimsIdentity(claims, PopForumsAuthorizationDefaults.AuthenticationScheme);
			await httpContext.SignInAsync(PopForumsAuthorizationDefaults.AuthenticationScheme, new ClaimsPrincipal(id), props);
		}
	}
}