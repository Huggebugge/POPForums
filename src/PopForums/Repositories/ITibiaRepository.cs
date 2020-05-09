using PopForums.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PopForums.Repositories
{
	public interface ITibiaRepository
	{
		Task<List<TibiaCharacter>> GetCharacters();
		void UpdateCharacter(TibiaCharacter character);
		void AddCharacter(TibiaCharacter character);
		void LogOnlineTime(TibiaCharacter character);
		Task<List<TibiaCharacterOnlineStatistics>> GetOnlineStatistics();
		Task<List<TibiaEvent>> GetEvents();
		Task<List<TibiaCharacter>> GetCharactersByUserID(int userID);
		void DeleteUserCharacter(string character);
	}
}
