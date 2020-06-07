using PopForums.Models;
using PopForums.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net;
using System.Text.Json.Serialization;
using System;

namespace PopForums.Services
{
	public interface ITibiaService
	{
		Task<List<TibiaCharacter>> GetMemberCharacters();
		List<TibiaCharacter> GetOnlineCharactersFromTibia();
		bool IsGuidInTibiaCharWebSite(Guid authorizationKey, string name);
		void AddCharacter(TibiaCharacter character);
		public void UpdateCharacter(TibiaCharacter character);
		Task<List<TibiaCharacterOnlineStatistics>> GetOnlineStatistics(); 
		Task<List<TibiaCharacter>> GetOnlineMembers(); 
		void LogOnlineTime(TibiaCharacter character);
		Task<List<TibiaCharacter>> GetUserCharacters(User user);
		void DeleteUserCharacter(string character);
	}
	public class TibiaService : ITibiaService
	{
		public TibiaService(ITibiaRepository tibiaRepository, IUserRepository userRepository, IRoleRepository roleRepository)
		{
			_userRepository = userRepository;
			_tibiaRepository = tibiaRepository;
			_roleRepository = roleRepository;
		}
		
		private readonly IUserRepository _userRepository;
		private readonly ITibiaRepository _tibiaRepository;
		private readonly IRoleRepository _roleRepository;

		public async Task<List<TibiaCharacter>> GetMemberCharacters()
		{
			var list = await _tibiaRepository.GetCharacters();
			var users = await _userRepository.GetUsersFromIDs(list.Select(c => c.UserID).Distinct().ToList());
			foreach (var character in list)
			{
				var user = users.Single(u => u.UserID == character.UserID);
				user.Roles = await _roleRepository.GetUserRoles(user.UserID);
				character.User = user;
			}
			return list;
		}

		public List<TibiaCharacter> GetOnlineCharactersFromTibia()
		{
			var chars = new List<TibiaCharacter>();
			using (var w = new WebClient())
			{
				var data = w.DownloadData(@"https://api.tibiadata.com/v2/world/Antica.json");
				var world = JsonSerializer.Deserialize<TibiaServerData>(data);
				return world.World.PlayersOnline;
			}
		}

		public bool IsGuidInTibiaCharWebSite(Guid authorizationKey, string name)
		{
			using (var w = new WebClient())
			{
				var str = $"https://api.tibiadata.com/v2/characters/{name}.json";
				var data = w.DownloadData(str);
				var character = JsonSerializer.Deserialize<TibiaCharacterWebData>(data);
				return character.Character.Data.Comment.Contains(authorizationKey.ToString());
			}
		}

		public void AddCharacter(TibiaCharacter character)
		{
			_tibiaRepository.AddCharacter(character);
		}

		public async Task<List<TibiaCharacterOnlineStatistics>> GetOnlineStatistics()
		{
			return await _tibiaRepository.GetOnlineStatistics();
		}

		public async Task<List<TibiaCharacter>> GetOnlineMembers()
		{
			var list = new List<TibiaCharacter>();
			var memberData = await GetMemberCharacters();
			var members = memberData.Where(c => c.User.IsInRole(PermanentRoles.Member) || c.User.IsInRole(PermanentRoles.Novice));
			var onlineChars = GetOnlineCharactersFromTibia();
			foreach (var member in members)
			{
				if (onlineChars.Any(o => o.Name == member.Name))
				{
					list.Add(member);
				}
			}
			return list;
		}
		public void LogOnlineTime(TibiaCharacter character)
		{
			_tibiaRepository.LogOnlineTime(character);
		}
		public void UpdateCharacter(TibiaCharacter character)
		{
			_tibiaRepository.UpdateCharacter(character);
		}

		public Task<List<TibiaCharacter>> GetUserCharacters(User user)
		{
			return _tibiaRepository.GetCharactersByUserID(user.UserID);
		}

		public void DeleteUserCharacter(string character)
		{
			_tibiaRepository.DeleteUserCharacter(character);
		}
	}
}
