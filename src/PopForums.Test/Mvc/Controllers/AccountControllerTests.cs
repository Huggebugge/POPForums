using Microsoft.AspNetCore.Authentication;
using Moq;
using PopForums.Configuration;
using PopForums.Email;
using PopForums.Feeds;
using PopForums.Mvc.Areas.Forums.Controllers;
using PopForums.Mvc.Areas.Forums.Services;
using PopForums.ScoringGame;
using PopForums.Services;
using Xunit;

namespace PopForums.Test.Mvc.Controllers
{
	public class AccountControllerTests
	{
		private Mock<IUserService> _userService;
		private Mock<IProfileService> _profileService;
		private Mock<ISettingsManager> _settingsManager;
		private Mock<INewAccountMailer> _newAccountMailer;
		private Mock<IPostService> _postService;
		private Mock<ITopicService> _topicService;
		private Mock<IForumService> _forumService;
		private Mock<ILastReadService> _lastReadService;
		private Mock<IClientSettingsMapper> _clientSettingsMapper;
		private Mock<IUserEmailer> _userEmailer;
		private Mock<IImageService> _imageService;
		private Mock<IFeedService> _feedService;
		private Mock<IUserAwardService> _userAwardService;
		private Mock<IUserRetrievalShim> _userRetrievalShim;
		private Mock<IConfig> _config;
		private Mock<IReCaptchaService> _recaptchaService;
		private Mock<ITibiaService> _tibiaService;

		private AccountController GetController()
		{
			_userService = new Mock<IUserService>();
			_profileService = new Mock<IProfileService>();
			_settingsManager = new Mock<ISettingsManager>();
			_newAccountMailer = new Mock<INewAccountMailer>();
			_postService = new Mock<IPostService>();
			_topicService = new Mock<ITopicService>();
			_forumService = new Mock<IForumService>();
			_lastReadService = new Mock<ILastReadService>();
			_clientSettingsMapper = new Mock<IClientSettingsMapper>();
			_userEmailer = new Mock<IUserEmailer>();
			_imageService = new Mock<IImageService>();
			_feedService = new Mock<IFeedService>();
			_userAwardService = new Mock<IUserAwardService>();
			_userRetrievalShim = new Mock<IUserRetrievalShim>();
			_config = new Mock<IConfig>();
			_recaptchaService = new Mock<IReCaptchaService>();
			_tibiaService = new Mock<ITibiaService>();
			return new AccountController(_userService.Object, _profileService.Object, _newAccountMailer.Object, _settingsManager.Object, _postService.Object, _topicService.Object, _forumService.Object, _lastReadService.Object, _clientSettingsMapper.Object, _userEmailer.Object, _imageService.Object, _feedService.Object, _userAwardService.Object, _userRetrievalShim.Object, _config.Object, _recaptchaService.Object, _tibiaService.Object);
		}

		[Fact]
		public void CreateViewPopulatesDefaultValues()
		{
			var controller = GetController();
			_settingsManager.Setup(x => x.Current.ServerTimeZone).Returns(-5);
			_settingsManager.Setup(x => x.Current.TermsOfService).Returns("tos");

			var result = controller.Create();

			Assert.Equal(-5, result.ViewData[AccountController.ServerTimeZoneKey]);
			Assert.Equal("tos", result.ViewData[AccountController.TosKey]);
		}
	}
}