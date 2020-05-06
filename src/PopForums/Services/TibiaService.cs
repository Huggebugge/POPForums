using PopForums.Models;
using PopForums.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PopForums.Services
{
	public interface ITibiaService
	{
		Task<List<TibiaCharacter>> GetMemberCharacters();

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
	}
}
