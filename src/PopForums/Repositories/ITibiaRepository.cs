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
		List<TibiaCharacterOnlineStatistics> GetOnlineStatistics();
		List<TibiaEvent> GetEvents();
		List<TibiaCharacter> GetCharactersByUserID(int userID);
		void DeleteUserCharacter(string character);
	}
}
