using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PopForums.Configuration;
using PopForums.Email;
using PopForums.Extensions;
using PopForums.Feeds;
using PopForums.Models;
using PopForums.Mvc.Areas.Forums.Authorization;
using PopForums.Mvc.Areas.Forums.Models;
using PopForums.Mvc.Areas.Forums.Services;
using PopForums.ScoringGame;
using PopForums.Services;
using PopForums.Mvc.Areas.Forums.Extensions;

namespace PopForums.Mvc.Areas.Forums.Controllers
{
	[Area("Forums")]
	public class AccountController : Controller
	{
		public AccountController(IUserService userService, IProfileService profileService, INewAccountMailer newAccountMailer, ISettingsManager settingsManager, IPostService postService, ITopicService topicService, IForumService forumService, ILastReadService lastReadService, IClientSettingsMapper clientSettingsMapper, IUserEmailer userEmailer, IImageService imageService, IFeedService feedService, IUserAwardService userAwardService, IUserRetrievalShim userRetrievalShim, IConfig config, IReCaptchaService reCaptchaService, ITibiaService tibiaService)
		{
			_userService = userService;
			_settingsManager = settingsManager;
			_profileService = profileService;
			_newAccountMailer = newAccountMailer;
			_postService = postService;
			_topicService = topicService;
			_forumService = forumService;
			_lastReadService = lastReadService;
			_clientSettingsMapper = clientSettingsMapper;
			_userEmailer = userEmailer;
			_imageService = imageService;
			_feedService = feedService;
			_userAwardService = userAwardService;
			_userRetrievalShim = userRetrievalShim;
			_config = config;
			_reCaptchaService = reCaptchaService;
			_tibiaService = tibiaService;
		}

		public static string Name = "Account";
		public static string CoppaDateKey = "CoppaDateKey";
		public static string TosKey = "TosKey";
		public static string ServerTimeZoneKey = "ServerTimeZoneKey";

		private readonly IUserService _userService;
		private readonly ISettingsManager _settingsManager;
		private readonly IProfileService _profileService;
		private readonly INewAccountMailer _newAccountMailer;
		private readonly IPostService _postService;
		private readonly ITopicService _topicService;
		private readonly IForumService _forumService;
		private readonly ILastReadService _lastReadService;
		private readonly IClientSettingsMapper _clientSettingsMapper;
		private readonly IUserEmailer _userEmailer;
		private readonly IImageService _imageService;
		private readonly IFeedService _feedService;
		private readonly IUserAwardService _userAwardService;
		private readonly IUserRetrievalShim _userRetrievalShim;
		private readonly IConfig _config;
		private readonly IReCaptchaService _reCaptchaService;
		private readonly ITibiaService _tibiaService;

		[PopForumsAuthorizationIgnore]
		public ViewResult Create()
		{
			SetupCreateData();
			var signupData = new SignupData
			{
				IsDaylightSaving = true,
				IsSubscribed = true,
				TimeZone = _settingsManager.Current.ServerTimeZone
			};
			return View(signupData);
		}

		private void SetupCreateData()
		{
			ViewData[CoppaDateKey] = SignupData.GetCoppaDate();
			ViewData[TosKey] = _settingsManager.Current.TermsOfService;
			ViewData[ServerTimeZoneKey] = _settingsManager.Current.ServerTimeZone;
		}

		[PopForumsAuthorizationIgnore]
		[HttpPost]
		public async Task<ViewResult> Create(SignupData signupData)
		{
			var ip = HttpContext.Connection.RemoteIpAddress.ToString();
			if (_config.UseReCaptcha)
			{
				var reCaptchaResponse = await _reCaptchaService.VerifyToken(signupData.Token, ip);
				if (!reCaptchaResponse.IsSuccess)
					ModelState.AddModelError("Email", Resources.BotError);
			}
			await ValidateSignupData(signupData, ModelState, ip);
			if (ModelState.IsValid)
			{
				var user = await _userService.CreateUser(signupData, ip);
				await _profileService.Create(user, signupData);
				// TODO: get rid of FullUrlHelper extension
				var verifyUrl = this.FullUrlHelper("Verify", "Account");
				if (_settingsManager.Current.IsNewUserApproved)
				{
					ViewData["Result"] = Resources.AccountReady;
					await _userService.Login(user, ip);
				}

				await IdentityController.PerformSignInAsync(user, HttpContext);
				_tibiaService.AddCharacter(new TibiaCharacter
				{
					Name = user.Name,
					Level = 0,
					UserID = user.UserID
				});
				return View("AccountCreated", user);
			}
			SetupCreateData();
			return View(signupData);
		}

		private async Task ValidateSignupData(SignupData signupData, ModelStateDictionary modelState, string ip)
		{
			if (!signupData.IsCoppa)
				modelState.AddModelError("IsCoppa", Resources.MustBe13);
			if (!signupData.IsTos)
				modelState.AddModelError("IsTos", Resources.MustAcceptTOS);
			var passwordValid = _userService.IsPasswordValid(signupData.Password, out var passwordError);
			if (!passwordValid)
				modelState.AddModelError("Password", passwordError);
			if (signupData.Password != signupData.PasswordRetype)
				modelState.AddModelError("PasswordRetype", Resources.RetypeYourPassword);
			if (string.IsNullOrWhiteSpace(signupData.Name))
				modelState.AddModelError("Name", Resources.NameRequired);
			else if (await _userService.IsNameInUse(signupData.Name))
				modelState.AddModelError("Name", Resources.NameInUse);
			if (string.IsNullOrWhiteSpace(signupData.Email))
				modelState.AddModelError("Email", Resources.EmailRequired);
			else
				if (!signupData.Email.IsEmailAddress())
					modelState.AddModelError("Email", Resources.ValidEmailAddressRequired);
			else if (signupData.Email != null && await _userService.IsEmailInUse(signupData.Email))
				modelState.AddModelError("Email", Resources.EmailInUse);
			if (signupData.Email != null && await _userService.IsEmailBanned(signupData.Email))
				modelState.AddModelError("Email", Resources.EmailBanned);
			if (await _userService.IsIPBanned(ip))
				modelState.AddModelError("Email", Resources.IPBanned);

		}

		[PopForumsAuthorizationIgnore]
		public ViewResult Forgot()
		{
			return View();
		}

		[PopForumsAuthorizationIgnore]
		[HttpPost]
		public async Task<ViewResult> Forgot(string email)
		{
			var user = await _userService.GetUserByEmail(email);
			if (user == null)
			{
				ViewBag.Result = Resources.EmailNotFound;
			}
			else
			{
				ViewBag.Result = Resources.ForgotInstructionsSent;
				var resetLink = this.FullUrlHelper("ResetPassword", "Account");
				await _userService.GeneratePasswordResetEmail(user, resetLink);
			}
			return View();
		}

		[PopForumsAuthorizationIgnore]
		public async Task<ActionResult> ResetPassword(string id)
		{
			var authKey = Guid.Empty;
			if (!string.IsNullOrWhiteSpace(id) && !Guid.TryParse(id, out authKey))
				return StatusCode(403);
			var user = await _userService.GetUserByAuhtorizationKey(authKey);
			var container = new PasswordResetContainer();
			if (user == null)
				container.IsValidUser = false;
			else
				container.IsValidUser = true;
			return View(container);
		}

		[PopForumsAuthorizationIgnore]
		[HttpPost]
		public async Task<ActionResult> ResetPassword(string id, PasswordResetContainer resetContainer)
		{
			var authKey = Guid.Empty;
			if (!string.IsNullOrWhiteSpace(id) && !Guid.TryParse(id, out authKey))
				return StatusCode(403);
			var user = await _userService.GetUserByAuhtorizationKey(authKey);
			resetContainer.IsValidUser = true;
			if (resetContainer.Password != resetContainer.PasswordRetype)
				ModelState.AddModelError("PasswordRetype", Resources.RetypePasswordMustMatch);
			string errorMessage;
			_userService.IsPasswordValid(resetContainer.Password, out errorMessage);
			if (!string.IsNullOrWhiteSpace(errorMessage))
				ModelState.AddModelError("Password", errorMessage);
			if (!ModelState.IsValid)
				return View(resetContainer);
			await _userService.ResetPassword(user, resetContainer.Password, HttpContext.Connection.RemoteIpAddress.ToString());
			return RedirectToAction("ResetPasswordSuccess");
		}

		[PopForumsAuthorizationIgnore]
		public ActionResult ResetPasswordSuccess()
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return RedirectToAction("Login");
			return View();
		}

		public async Task<ViewResult> EditProfile()
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			var profile = await _profileService.GetProfileForEdit(user);
			var userEdit = new UserEditProfile(profile);
			return View(userEdit);
		}

		[HttpPost]
		public async Task<ViewResult> EditProfile(UserEditProfile userEdit)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			await _userService.EditUserProfile(user, userEdit);
			ViewBag.Result = Resources.ProfileUpdated;
			return View(userEdit);
		}

		public ViewResult Security()
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			var isNewUserApproved = _settingsManager.Current.IsNewUserApproved;
			var userEdit = new UserEditSecurity(user, isNewUserApproved);
			return View(userEdit);
		}

		[HttpPost]
		public async Task<ViewResult> ChangePassword(UserEditSecurity userEdit)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			var (isPasswordPassed, _) = await _userService.CheckPassword(user.Email, userEdit.OldPassword);
			if (!isPasswordPassed)
				ViewBag.PasswordResult = Resources.OldPasswordIncorrect;
			else if (!userEdit.NewPasswordsMatch())
				ViewBag.PasswordResult = Resources.RetypePasswordMustMatch;
			else if (!_userService.IsPasswordValid(userEdit.NewPassword, out var errorMessage))
				ViewBag.PasswordResult = errorMessage;
			else
			{
				await _userService.SetPassword(user, userEdit.NewPassword, HttpContext.Connection.RemoteIpAddress.ToString(), user);
				ViewBag.PasswordResult = Resources.NewPasswordSaved;
			}
			return View("Security", new UserEditSecurity { NewEmail = String.Empty, NewEmailRetype = String.Empty, IsNewUserApproved = _settingsManager.Current.IsNewUserApproved });
		}

		[HttpPost]
		public async Task<ViewResult> ChangeEmail(UserEditSecurity userEdit)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			if (string.IsNullOrWhiteSpace(userEdit.NewEmail) || !userEdit.NewEmail.IsEmailAddress())
				ViewBag.EmailResult = Resources.ValidEmailAddressRequired;
			else if (userEdit.NewEmail != userEdit.NewEmailRetype)
				ViewBag.EmailResult = Resources.EmailsMustMatch;
			else if (await _userService.IsEmailInUseByDifferentUser(user, userEdit.NewEmail))
				ViewBag.EmailResult = Resources.EmailInUse;
			else
			{
				await _userService.ChangeEmail(user, userEdit.NewEmail, user, HttpContext.Connection.RemoteIpAddress.ToString());
				if (_settingsManager.Current.IsNewUserApproved)
					ViewBag.EmailResult = Resources.EmailChangeSuccess;
				else
				{
					ViewBag.EmailResult = Resources.VerificationEmailSent;
					var verifyUrl = this.FullUrlHelper("Verify", "Account");
					var result = _newAccountMailer.Send(user, verifyUrl);
					if (result != SmtpStatusCode.Ok)
						ViewBag.EmailResult = Resources.EmailProblemAccount + result;
				}
			}
			return View("Security", new UserEditSecurity { NewEmail = String.Empty, NewEmailRetype = String.Empty, IsNewUserApproved = _settingsManager.Current.IsNewUserApproved });
		}
		public ViewResult Validate()
		{
			var user = _userRetrievalShim.GetUser(); 
			if (user.IsApproved)
				RedirectToAction("EditProfile");
			if(_tibiaService.IsGuidInTibiaCharWebSite(user.AuthorizationKey, user.Name)){
				_userService.UpdateIsApproved(user, true, null, HttpContext.Connection.RemoteIpAddress.ToString());
				return View("EditProfile");
			}
			return View("VerifyFail");
		}
		public async Task<ViewResult> ManagePhotos()
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			var profile = await _profileService.GetProfile(user);
			var userEdit = new UserEditPhoto(profile);
			if (profile.ImageID.HasValue)
				userEdit.IsImageApproved = await _imageService.IsUserImageApproved(profile.ImageID.Value);
			return View(userEdit);
		}
		
		[HttpPost]
		public async Task<ActionResult> ManagePhotos(UserEditPhoto userEdit)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return View("EditAccountNoUser");
			byte[] avatarFile = null;
			if (userEdit.AvatarFile != null)
				avatarFile = userEdit.AvatarFile.OpenReadStream().ToBytes();
			byte[] photoFile = null;
			if (userEdit.PhotoFile != null)
				photoFile = userEdit.PhotoFile.OpenReadStream().ToBytes();
			await _userService.EditUserProfileImages(user, userEdit.DeleteAvatar, userEdit.DeleteImage, avatarFile, photoFile);
			return RedirectToAction("ManagePhotos");
		}

		public async Task<ViewResult> MiniProfile(int id)
		{
			var user = await _userService.GetUser(id);
			if (user == null)
				return View("MiniUserNotFound");
			var profile = await _profileService.GetProfile(user);
			UserImage userImage = null;
			if (profile.ImageID.HasValue)
				userImage = await _imageService.GetUserImage(profile.ImageID.Value);
			var model = new DisplayProfile(user, profile, userImage);
			model.PostCount = await _postService.GetPostCount(user);
			var viewingUser = _userRetrievalShim.GetUser();
			if (viewingUser == null)
				model.ShowDetails = false;
			return View(model);
		}

		public async Task<ActionResult> ViewProfile(int id)
		{
			var user = await _userService.GetUser(id);
			if (user == null)
				return NotFound();
			var profile = await _profileService.GetProfile(user);
			UserImage userImage = null;
			if (profile.ImageID.HasValue)
				userImage = await _imageService.GetUserImage(profile.ImageID.Value);
			var model = new DisplayProfile(user, profile, userImage);
			model.PostCount = await _postService.GetPostCount(user);
			model.Feed = await _feedService.GetFeed(user);
			model.UserAwards = await _userAwardService.GetAwards(user);
			var viewingUser = _userRetrievalShim.GetUser();
			if (viewingUser == null)
				model.ShowDetails = false;
			return View(model);
		}

		public async Task<ActionResult> Posts(int id, int pageNumber = 1)
		{
			var postUser = await _userService.GetUser(id);
			if (postUser == null)
				return NotFound();
			var includeDeleted = false;
			var user = _userRetrievalShim.GetUser();
			if (user != null && user.IsInRole(PermanentRoles.Moderator))
				includeDeleted = true;
			var titles = _forumService.GetAllForumTitles();
			var (topics, pagerContext) = await _topicService.GetTopics(user, postUser, includeDeleted, pageNumber);
			var container = new PagedTopicContainer { ForumTitles = titles, PagerContext = pagerContext, Topics = topics };
			await _lastReadService.GetTopicReadStatus(user, container);
			ViewBag.PostUserName = postUser.Name;
			return View(container);
		}

		public async Task<JsonResult> ClientSettings()
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return Json(_clientSettingsMapper.GetDefault());
			var profile = await _profileService.GetProfile(user);
			return Json(_clientSettingsMapper.GetClientSettings(profile));
		}

		[PopForumsAuthorizationIgnore]
		public ViewResult Login()
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
			ViewBag.Referrer = link;

			return View();
		}

		public async Task<ActionResult> EmailUser(int id)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return StatusCode(403);
			var toUser = await _userService.GetUser(id);
			if (toUser == null)
				return NotFound();
			if (await _userEmailer.IsUserEmailable(toUser) == false)
				return StatusCode(403);
			ViewBag.IP = HttpContext.Connection.RemoteIpAddress.ToString();
			return View(toUser);
		}

		[HttpPost]
		public async Task<ActionResult> EmailUser(int id, string subject, string text)
		{
			var user = _userRetrievalShim.GetUser();
			if (user == null)
				return StatusCode(403);
			var toUser = await _userService.GetUser(id);
			if (toUser == null)
				return NotFound();
			if (await _userEmailer.IsUserEmailable(toUser) == false)
				return StatusCode(403);
			if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(text))
			{
				ViewBag.EmailResult = Resources.PMCreateWarnings;
				ViewBag.IP = HttpContext.Connection.RemoteIpAddress.ToString();
				return View(toUser);
			}
			await _userEmailer.ComposeAndQueue(toUser, user, HttpContext.Connection.RemoteIpAddress.ToString(), subject, text);
			return View("EmailSent");
		}

		[PopForumsAuthorizationIgnore]
		public async Task<ViewResult> Unsubscribe(int id, string key)
		{
			var user = await _userService.GetUser(id);
			if (user == null || (await _profileService.Unsubscribe(user, key) == false))
				return View("UnsubscribeFailure");
			return View();
		}
	}
}
