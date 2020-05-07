using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PopForums.Models;
using PopForums.Repositories;

namespace PopForums.ScoringGame
{
	public interface IUserAwardService
	{
		Task IssueAward(User user, AwardDefinition awardDefinition);
		Task<bool> IsAwarded(User user, AwardDefinition awardDefinition);
		Task<List<UserAward>> GetAwards(User user);
		Task<List<UserAward>> GetAllAwards(List<Post> posts);
	}

	public class UserAwardService : IUserAwardService
	{
		public UserAwardService(IUserAwardRepository userAwardRepository)
		{
			_userAwardRepository = userAwardRepository;
		}

		private readonly IUserAwardRepository _userAwardRepository;

		public async Task IssueAward(User user, AwardDefinition awardDefinition)
		{
			await _userAwardRepository.IssueAward(user.UserID, awardDefinition.AwardDefinitionID, awardDefinition.Title, awardDefinition.Description, DateTime.UtcNow);
		}

		public async Task<bool> IsAwarded(User user, AwardDefinition awardDefinition)
		{
			return await _userAwardRepository.IsAwarded(user.UserID, awardDefinition.AwardDefinitionID);
		}

		public async Task<List<UserAward>> GetAwards(User user)
		{
			return await _userAwardRepository.GetAwards(user.UserID);
		}

		public async Task<List<UserAward>> GetAllAwards(List<Post> posts)
		{
			var userIDs = posts.Select(p => p.UserID).Distinct().ToList();
			return await _userAwardRepository.GetAllAwards(userIDs);
		}
	}
}
