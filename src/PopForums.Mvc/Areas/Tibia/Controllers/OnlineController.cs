using Microsoft.AspNetCore.Mvc;
using PopForums.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PopForums.Mvc.Areas.Tibia.Controllers
{
	[Area("Tibia")]
	public class OnlineController : Controller
	{
		private readonly IUserService _userService;
		private readonly ITibiaService _tibiaService;

		public OnlineController(IUserService userService, ITibiaService tibiaService)
		{
			_userService = userService;
			_tibiaService = tibiaService;
		}
		// GET: /<controller>/
		public IActionResult Index()
		{
			ViewBag.Statistics = _tibiaService.GetOnlineCharactersFromTibia();
			return View();
		}
	}
}
