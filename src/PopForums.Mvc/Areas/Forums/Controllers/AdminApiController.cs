﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PopForums.Configuration;
using PopForums.Extensions;
using PopForums.Models;
using PopForums.Mvc.Areas.Forums.Authorization;
using PopForums.Mvc.Areas.Forums.Extensions;
using PopForums.Mvc.Areas.Forums.Models;
using PopForums.Mvc.Areas.Forums.Services;
using PopForums.ScoringGame;
using PopForums.Services;

namespace PopForums.Mvc.Areas.Forums.Controllers
{
	[Authorize(Policy = PermanentRoles.Admin, AuthenticationSchemes = PopForumsAuthorizationDefaults.AuthenticationScheme)]
	[Area("Forums")]
	[Produces("application/json")]
	[ApiController]
	public class AdminApiController : Controller
	{
		private readonly ISettingsManager _settingsManager;
		private readonly ICategoryService _categoryService;
		private readonly IForumService _forumService;
		private readonly IUserService _userService;
		private readonly ISearchService _searchService;
		private readonly IProfileService _profileService;
		private readonly IUserRetrievalShim _userRetrievalShim;
		private readonly IImageService _imageService;
		private readonly IBanService _banService;
		private readonly IMailingListService _mailingListService;
		private readonly IEventDefinitionService _eventDefinitionService;
		private readonly IAwardDefinitionService _awardDefinitionService;
		private readonly IEventPublisher _eventPublisher;
		private readonly IIPHistoryService _ipHistoryService;
		private readonly ISecurityLogService _securityLogService;
		private readonly IModerationLogService _moderationLogService;
		private readonly IErrorLog _errorLog;
		private readonly IServiceHeartbeatService _serviceHeartbeatService;

		public AdminApiController(ISettingsManager settingsManager, ICategoryService categoryService, IForumService forumService, IUserService userService, ISearchService searchService, IProfileService profileService, IUserRetrievalShim userRetrievalShim, IImageService imageService, IBanService banService, IMailingListService mailingListService, IEventDefinitionService eventDefinitionService, IAwardDefinitionService awardDefinitionService, IEventPublisher eventPublisher, IIPHistoryService ipHistoryService, ISecurityLogService securityLogService, IModerationLogService moderationLogService, IErrorLog errorLog, IServiceHeartbeatService serviceHeartbeatService)
		{
			_settingsManager = settingsManager;
			_categoryService = categoryService;
			_forumService = forumService;
			_userService = userService;
			_searchService = searchService;
			_profileService = profileService;
			_userRetrievalShim = userRetrievalShim;
			_imageService = imageService;
			_banService = banService;
			_mailingListService = mailingListService;
			_eventDefinitionService = eventDefinitionService;
			_awardDefinitionService = awardDefinitionService;
			_eventPublisher = eventPublisher;
			_ipHistoryService = ipHistoryService;
			_securityLogService = securityLogService;
			_moderationLogService = moderationLogService;
			_errorLog = errorLog;
			_serviceHeartbeatService = serviceHeartbeatService;
		}

		// ********** settings

		[HttpGet("/Forums/AdminApi/GetSettings")]
		public ActionResult<Settings> GetSettings()
		{
			var settings = _settingsManager.Current;
			return settings;
		}

		[HttpPost("/Forums/AdminApi/SaveSettings")]
		public ActionResult<Settings> SaveSettings([FromBody]Settings settings)
		{
			_settingsManager.Save(settings);
			var newSettings = _settingsManager.Current;
			return newSettings;
		}

		// ********** categories

		[HttpGet("/Forums/AdminApi/GetCategories")]
		public ActionResult<List<Category>> GetCategories()
		{
			var categories = _categoryService.GetAll();
			return categories;
		}

		[HttpPost("/Forums/AdminApi/AddCategory")]
		public ActionResult<List<Category>> AddCategory([FromBody]Category category)
		{
			_categoryService.Create(category.Title);
			var categories = _categoryService.GetAll();
			return categories;
		}

		[HttpPost("/Forums/AdminApi/DeleteCategory/{id}")]
		public ActionResult<List<Category>> DeleteCategory(int id)
		{
			_categoryService.Delete(id);
			var categories = _categoryService.GetAll();
			return categories;
		}

		[HttpPost("/Forums/AdminApi/MoveCategoryUp/{id}")]
		public ActionResult<List<Category>> MoveCategoryUp(int id)
		{
			_categoryService.MoveCategoryUp(id);
			var categories = _categoryService.GetAll();
			return categories;
		}

		[HttpPost("/Forums/AdminApi/MoveCategoryDown/{id}")]
		public ActionResult<List<Category>> MoveCategoryDown(int id)
		{
			_categoryService.MoveCategoryDown(id);
			var categories = _categoryService.GetAll();
			return categories;
		}

		[HttpPost("/Forums/AdminApi/EditCategory")]
		public ActionResult<List<Category>> EditCategory([FromBody]Category category)
		{
			_categoryService.UpdateTitle(category.CategoryID, category.Title);
			var categories = _categoryService.GetAll();
			return categories;
		}

		// ********** forums

		[HttpGet("/Forums/AdminApi/GetForums")]
		public ActionResult<List<CategoryContainerWithForums>> GetForums()
		{
			var forums = _forumService.GetCategoryContainersWithForums();
			return forums;
		}

		[HttpPost("/Forums/AdminApi/MoveForumUp/{id}")]
		public ActionResult<List<CategoryContainerWithForums>> MoveForumUp(int id)
		{
			_forumService.MoveForumUp(id);
			var forums = _forumService.GetCategoryContainersWithForums();
			return forums;
		}

		[HttpPost("/Forums/AdminApi/MoveForumDown/{id}")]
		public ActionResult<List<CategoryContainerWithForums>> MoveForumDown(int id)
		{
			_forumService.MoveForumDown(id);
			var forums = _forumService.GetCategoryContainersWithForums();
			return forums;
		}

		[HttpPost("/Forums/AdminApi/SaveForum")]
		public ActionResult<List<CategoryContainerWithForums>> SaveForum([FromBody]Forum forumEdit)
		{
			if (forumEdit.CategoryID == 0)
				forumEdit.CategoryID = null;
			if (forumEdit.ForumID == 0)
				_forumService.Create(forumEdit.CategoryID, forumEdit.Title, forumEdit.Description, forumEdit.IsVisible, forumEdit.IsArchived, -1, forumEdit.ForumAdapterName, forumEdit.IsQAForum);
			else
			{
				var forum = _forumService.Get(forumEdit.ForumID);
				if (forum == null)
					return NotFound();
				_forumService.Update(forum, forumEdit.CategoryID, forumEdit.Title, forumEdit.Description, forumEdit.IsVisible, forumEdit.IsArchived, forumEdit.ForumAdapterName, forumEdit.IsQAForum);
			}
			var forums = _forumService.GetCategoryContainersWithForums();
			return forums;
		}

		// ********** forum permissions

		[HttpGet("/Forums/AdminApi/GetForumPermissions/{id}")]
		public ActionResult<ForumPermissionContainer> GetForumPermissions(int id)
		{
			var forum = _forumService.Get(id);
			if (forum == null)
				return NotFound();
			var container = new ForumPermissionContainer
			{
				ForumID = forum.ForumID,
				AllRoles = _userService.GetAllRoles(),
				PostRoles = _forumService.GetForumPostRoles(forum),
				ViewRoles = _forumService.GetForumViewRoles(forum)
			};
			return container;
		}

		[HttpPost("/Forums/AdminApi/ModifyForumRoles")]
		public NoContentResult ModifyForumRoles(ModifyForumRolesContainer container)
		{
			_forumService.ModifyForumRoles(container);
			return NoContent();
		}

		// ********** search

		[HttpGet("/Forums/AdminApi/GetJunkWords")]
		public ActionResult<IEnumerable<string>> GetJunkWords()
		{
			var words = _searchService.GetJunkWords();
			return words;
		}

		[HttpPost("/Forums/AdminApi/CreateJunkWord/{word}")]
		public NoContentResult CreateJunkWord(string word)
		{
			_searchService.CreateJunkWord(word);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/DeleteJunkWord/{word}")]
		public NoContentResult DeleteJunkWord(string word)
		{
			_searchService.DeleteJunkWord(word);
			return NoContent();
		}

		// ********** edit user

		[HttpPost("/Forums/AdminApi/EditUserSearch")]
		public ActionResult<List<User>> EditUserSearch(UserSearch userSearch)
		{
			List<User> users;
			switch (userSearch.SearchType)
			{
				case UserSearch.UserSearchType.Email:
					users = _userService.SearchByEmail(userSearch.SearchText);
					break;
				case UserSearch.UserSearchType.Name:
					users = _userService.SearchByName(userSearch.SearchText);
					break;
				case UserSearch.UserSearchType.Role:
					users = _userService.SearchByRole(userSearch.SearchText);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(userSearch));
			}
			return users;
		}

		[HttpGet("/Forums/AdminApi/GetUser/{id}")]
		public ActionResult<UserEdit> GetUser(int id)
		{
			var user = _userService.GetUser(id);
			if (user == null)
				return NotFound();
			var profile = _profileService.GetProfileForEdit(user);
			var model = new UserEdit(user, profile);
			return model;
		}

		[HttpPost("/Forums/AdminApi/UpdateUserAvatar/{id}")]
		public ActionResult<dynamic> UpdateUserAvatar(int id)
		{
			var user = _userService.GetUser(id);
			if (user == null)
				return NotFound();
			if (Request.Form?.Files?.Count != 1)
			{
				_userService.EditUserProfileImages(user, true, false, null, null);
				return new {AvatarID = (int?)null};
			}
			var file = Request.Form.Files[0];
			_userService.EditUserProfileImages(user, false, false, file.OpenReadStream().ToBytes(), null);
			var profile = _profileService.GetProfileForEdit(user);
			return new {profile.AvatarID};
		}

		[HttpPost("/Forums/AdminApi/UpdateUserImage/{id}")]
		public ActionResult<dynamic> UpdateUserImage(int id)
		{
			var user = _userService.GetUser(id);
			if (user == null)
				return NotFound();
			if (Request.Form?.Files?.Count != 1)
			{
				_userService.EditUserProfileImages(user, false, true, null, null);
				return new { ImageID = (int?)null };
			}
			var file = Request.Form.Files[0];
			_userService.EditUserProfileImages(user, false, false, null, file.OpenReadStream().ToBytes());
			var profile = _profileService.GetProfileForEdit(user);
			return new { profile.ImageID };
		}

		[HttpPost("/Forums/AdminApi/SaveUser")]
		public ActionResult SaveUser([FromBody] UserEdit userEdit)
		{
			var adminUser = _userRetrievalShim.GetUser(HttpContext);
			var ip = HttpContext.Connection.RemoteIpAddress.ToString();
			var user = _userService.GetUser(userEdit.UserID);
			_userService.EditUser(user, userEdit, false, false, null, null, ip, adminUser);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/DeleteUser/{id}")]
		public ActionResult DeleteUser(int id)
		{
			DeleteUser(id, false);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/DeleteAndBanUser/{id}")]
		public ActionResult DeleteAndBanUser(int id)
		{
			DeleteUser(id, true);
			return Ok();
		}

		private void DeleteUser(int userID, bool isBanned)
		{
			var adminUser = _userRetrievalShim.GetUser(HttpContext);
			var ip = HttpContext.Connection.RemoteIpAddress.ToString();
			var user = _userService.GetUser(userID);
			_userService.DeleteUser(user, adminUser, ip, isBanned);
		}

		// ********** user roles

		[HttpGet("/Forums/AdminApi/GetAllRoles")]
		public ActionResult<IEnumerable<string>> GetAllRoles()
		{
			var roles = _userService.GetAllRoles();
			return roles;
		}

		[HttpPost("/Forums/AdminApi/CreateRole/{role}")]
		public ActionResult CreateRole(string role)
		{
			var user = _userRetrievalShim.GetUser(HttpContext);
			var ip = HttpContext.Connection.RemoteIpAddress.ToString();
			_userService.CreateRole(role, user, ip);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/DeleteRole/{role}")]
		public ActionResult DeleteRole(string role)
		{
			if (role == PermanentRoles.Admin || role == PermanentRoles.Moderator)
				return NoContent();
			var user = _userRetrievalShim.GetUser(HttpContext);
			var ip = HttpContext.Connection.RemoteIpAddress.ToString();
			_userService.DeleteRole(role, user, ip);
			return NoContent();
		}

		// ********** user image approval

		[HttpGet("/Forums/AdminApi/GetImageApproval")]
		public ActionResult<UserImageApprovalContainer> GetImageApproval()
		{
			var container = _imageService.GetUnapprovedUserImageContainer();
			return container;
		}

		[HttpPost("/Forums/AdminApi/ApproveUserImage/{id}")]
		public ActionResult ApproveUserImage(int id)
		{
			_imageService.ApproveUserImage(id);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/DeleteUserImage/{id}")]
		public ActionResult DeleteUserImage(int id)
		{
			_imageService.DeleteUserImage(id);
			return NoContent();
		}

		// ********** email ip ban

		[HttpGet("/Forums/AdminApi/GetEmailIPBan")]
		public ActionResult<object> GetEmailIPBan()
		{
			var emails = _banService.GetEmailBans();
			var ips = _banService.GetIPBans();
			var container = new {emails, ips};
			return container;
		}

		[HttpPost("/Forums/AdminApi/BanEmail")]
		public ActionResult BanEmail([FromBody] SingleString val)
		{
			_banService.BanEmail(val.String);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/RemoveEmail")]
		public ActionResult RemoveEmail([FromBody] SingleString val)
		{
			_banService.RemoveEmailBan(val.String);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/BanIP")]
		public ActionResult BanIP([FromBody] SingleString val)
		{
			_banService.BanIP(val.String);
			return NoContent();
		}

		[HttpPost("/Forums/AdminApi/RemoveIP")]
		public ActionResult RemoveIP([FromBody] SingleString val)
		{
			_banService.RemoveIPBan(val.String);
			return NoContent();
		}

		// ********** email users

		[HttpPost("/Forums/AdminApi/EmailUsers")]
		public ActionResult EmailUsers([FromBody]EmailUsersContainer container)
		{
			if (string.IsNullOrWhiteSpace(container.Subject) || string.IsNullOrWhiteSpace(container.Body))
				return StatusCode((int)HttpStatusCode.BadRequest, new {Error = Resources.SubjectAndBodyNotEmpty});
			var baseString = this.FullUrlHelper("Unsubscribe", AccountController.Name, new { id = "--id--", key = "--key--" });
			baseString = baseString.Replace("--id--", "{0}").Replace("--key--", "{1}");
			string UnsubscribeLinkGenerator(User user) => string.Format(baseString, user.UserID, _profileService.GetUnsubscribeHash(user));
			_mailingListService.MailUsers(container.Subject, container.Body, container.HtmlBody, UnsubscribeLinkGenerator);
			return Ok();
		}

		// ********** event definitions

		[HttpGet("/Forums/AdminApi/GetAllEventDefinitions")]
		public ActionResult<object> GetAllEventDefinitions()
		{
			var events = _eventDefinitionService.GetAll();
			var staticIDs = EventDefinitionService.StaticEvents.Select(x => x.Key).ToArray();
			var container = new {AllEvents = events, StaticIDs = staticIDs};
			return container;
		}

		[HttpPost("/Forums/AdminApi/CreateEvent")]
		public ActionResult CreateEvent([FromBody]EventDefinition newEvent)
		{
			_eventDefinitionService.Create(newEvent);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/DeleteEvent/{id}")]
		public ActionResult DeleteEvent(string id)
		{
			_eventDefinitionService.Delete(id);
			return Ok();
		}

		// ********** award definitions

		[HttpGet("/Forums/AdminApi/GetAllAwardDefinitions")]
		public ActionResult<List<AwardDefinition>> GetAllAwardDefinitions()
		{
			var awardDefinitions = _awardDefinitionService.GetAll();
			return awardDefinitions;
		}

		[HttpPost("/Forums/AdminApi/CreateAward")]
		public ActionResult CreateAward([FromBody]AwardDefinition newAward)
		{
			_awardDefinitionService.Create(newAward);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/DeleteAward/{id}")]
		public ActionResult DeleteAward(string id)
		{
			_awardDefinitionService.Delete(id);
			return Ok();
		}

		[HttpGet("/Forums/AdminApi/GetAward/{id}")]
		public ActionResult<object> GetAward(string id)
		{
			var award = _awardDefinitionService.Get(id);
			var conditions = _awardDefinitionService.GetConditions(award.AwardDefinitionID);
			var allEvents = _eventDefinitionService.GetAll();
			var container = new {Award = award, Conditions = conditions, AllEvents = allEvents};
			return container;
		}

		[HttpPost("/Forums/AdminApi/CreateCondition")]
		public ActionResult CreateCondition([FromBody]AwardCondition newCondition)
		{
			_awardDefinitionService.AddCondition(newCondition);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/DeleteCondition")]
		public ActionResult DeleteCondition([FromBody]AwardConditionDeleteContainer container)
		{
			_awardDefinitionService.DeleteCondition(container.AwardDefinitionID, container.EventDefinitionID);
			return Ok();
		}

		// ********** manual event

		[HttpPost("/Forums/AdminApi/GetNames")]
		public ActionResult<IEnumerable<object>> GetNames(SingleString name)
		{
			var users = _userService.SearchByName(name.String);
			var projection = users.Select(u => new { u.UserID, u.Name }).ToArray();
			return projection;
		}

		[HttpGet("/Forums/AdminApi/GetAllEvents")]
		public ActionResult<IEnumerable<EventDefinition>> GetAllEvents()
		{
			var events = _eventDefinitionService.GetAll();
			return events;
		}

		[HttpPost("/Forums/AdminApi/CreateManualEvent")]
		public ActionResult CreateManualEvent([FromBody] ManualEvent manualEvent)
		{
			if (!string.IsNullOrEmpty(manualEvent.EventDefinitionID))
				return BadRequest("Can't specify an EventDefinitionID.");
			var user = _userService.GetUser(manualEvent.UserID);
			if (user == null)
				return BadRequest($"UserID {manualEvent.UserID} does not exist.");
			if (!manualEvent.Points.HasValue)
				return BadRequest("Point value required.");
			_eventPublisher.ProcessManualEvent(manualEvent.Message, user, manualEvent.Points.Value);
			return Ok();
		}

		[HttpPost("/Forums/AdminApi/CreateExistingManualEvent")]
		public ActionResult CreateExistingManualEvent([FromBody] ManualEvent manualEvent)
		{
			if (string.IsNullOrEmpty(manualEvent.EventDefinitionID))
				return BadRequest("Must specify an EventDefinitionID.");
			var user = _userService.GetUser(manualEvent.UserID);
			if (user == null)
				return BadRequest($"UserID {manualEvent.UserID} does not exist.");
			if (manualEvent.Points.HasValue)
				return BadRequest("Point value can't be specified.");
			_eventPublisher.ProcessEvent(manualEvent.Message, user, manualEvent.EventDefinitionID, false);
			return Ok();
		}

		// ********** ip history

		[HttpPost("/Forums/AdminApi/QueryIPHistory")]
		public ActionResult<List<IPHistoryEvent>> QueryIPHistory([FromBody] IPHistoryQuery query)
		{
			var history = _ipHistoryService.GetHistory(query.IP, query.Start, query.End);
			return history;
		}

		// ********** security log

		[HttpPost("/Forums/AdminApi/QuerySecurityLog")]
		public ActionResult<List<SecurityLogEntry>> QuerySecurityLog([FromBody] SecurityLogQuery query)
		{
			List<SecurityLogEntry> list;
			switch (query.Type.ToLower())
			{
				case "userid":
					list = _securityLogService.GetLogEntriesByUserID(Convert.ToInt32(query.SearchTerm), query.Start, query.End);
					break;
				case "name":
					list = _securityLogService.GetLogEntriesByUserName(query.SearchTerm, query.Start, query.End);
					break;
				default:
					return BadRequest("Search type invalid.");
			}
			return list;
		}

		// ********** moderation log

		[HttpPost("/Forums/AdminApi/QueryModerationLog")]
		public ActionResult<List<ModerationLogEntry>> QueryModerationLog([FromBody] IPHistoryQuery query)
		{
			var history = _moderationLogService.GetLog(query.Start, query.End);
			return history;
		}

		// ********** error log

		[HttpGet("/Forums/AdminApi/GetErrorLog/{page}")]
		public ActionResult<PagedList<ErrorLogEntry>> GetErrorLog(int page)
		{
			var list = _errorLog.GetErrors(page, 20);
			return list;
		}

		[HttpPost("/Forums/AdminApi/DeleteAllErrors")]
		public ActionResult DeleteAllErrors()
		{
			_errorLog.DeleteAllErrors();
			return Ok();
		}

		// ********** error log

		[HttpGet("/Forums/AdminApi/GetServices")]
		public ActionResult<List<ServiceHeartbeat>> GetServices()
		{
			var list = _serviceHeartbeatService.GetAll();
			return list;
		}

		[HttpPost("/Forums/AdminApi/ClearServices")]
		public ActionResult<List<ServiceHeartbeat>> ClearServices()
		{
			_serviceHeartbeatService.ClearAll();
			var list = _serviceHeartbeatService.GetAll();
			return list;
		}
	}
}
