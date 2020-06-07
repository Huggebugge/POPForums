using System.Collections.Generic;
using PopForums.Models;
using PopForums.Repositories;
using System.Threading.Tasks;
using Dapper;
using System.Linq;
using PopForums.Configuration;

namespace PopForums.Sql.Repositories
{
	public class TibiaRepository : ITibiaRepository
	{
		public TibiaRepository(ISqlObjectFactory sqlObjectFactory, ICacheHelper cache)
		{
			_sqlObjectFactory = sqlObjectFactory;
			_cache = cache;
		}
		private readonly ISqlObjectFactory _sqlObjectFactory;
		private readonly ICacheHelper _cache;

		public async Task<List<TibiaCharacter>> GetCharacters()
		{
			Task<IEnumerable<TibiaCharacter>> chars = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(c =>
				chars = c.QueryAsync<TibiaCharacter>("SELECT CharacterID, UserID, Name, Level, IsAlt FROM TibiaCharacter"));
			var list = chars.Result.ToList();
			_cache.RemoveCacheObject("Characters");
			return list;
		}
		public async Task<TibiaCharacter> GetCharacter(string name)
		{
			Task<TibiaCharacter> character = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(c =>
				character = c.QueryFirstAsync<TibiaCharacter>("SELECT CharacterID, UserID, Name, Level, IsAlt FROM TibiaCharacter WHERE Name = @Name",
				new
				{
					Name = name
				}));
			return await character;
		}
		public async void UpdateCharacter(TibiaCharacter character)
		{
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				connection.ExecuteAsync("UPDATE TibiaCharacter SET Level = @Level, Name = @Name, IsAlt = @Alt WHERE CharacterID = @CharacterId", new
				{
					character.Level,
					character.Name,
					Alt = character.IsAlt,
					CharacterId = character.CharacterID
				}));
		}

		public async void AddCharacter(TibiaCharacter character)
		{
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				connection.ExecuteAsync("INSERT INTO TibiaCharacter (UserID, Level, Name, IsAlt) VALUES (@UserID, @Level, @Name, @IsAlt)", new
				{
					character.User.UserID,
					character.Level,
					character.Name,
					character.IsAlt
				}));
		}

		public async void LogOnlineTime(TibiaCharacter character)
		{

			Task<dynamic> exists = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
			exists = connection.QueryFirstOrDefaultAsync("SELECT * FROM [TibiaCharacterOnline] WHERE CAST(GETDATE() AS DATE) = [Date] AND CharacterID = @Character", new
			{
				Character = character.CharacterID
			}));
			if (exists.Result != null)
			{
				await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
					connection.ExecuteAsync("UPDATE TibiaCharacterOnline SET [TimeOnline] = [TimeOnline] + 5 WHERE CAST(GETDATE() AS DATE) = [Date] AND CharacterID = @Character", new
					{
						Character = character.CharacterID
					}));
			}
			else
			{
				await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
					connection.ExecuteAsync("INSERT INTO TibiaCharacterOnline (CharacterID, Date, [TimeOnline]) VALUES (@Character, CAST(GETDATE() AS DATE), 5)", new
					{
						Character = character.CharacterID
					}));
			}
		}

		public async Task<List<TibiaCharacterOnlineStatistics>> GetOnlineStatistics()
		{
			Task<IEnumerable<TibiaCharacterOnlineStatistics>> list = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				list = connection.QueryAsync<TibiaCharacterOnlineStatistics>(
						"WITH ThisWeek AS(SELECT CharacterID, SUM(TimeOnline) as TimeOnline FROM TibiaCharacterOnline WHERE[Date] BETWEEN dateadd(day, -7, GETDATE()) AND GETDATE() GROUP BY CharacterID), ThisMonth AS(SELECT CharacterID, SUM(TimeOnline) as TimeOnline FROM TibiaCharacterOnline WHERE[Date] BETWEEN dateadd(day, -30, GETDATE()) AND GETDATE() GROUP BY CharacterID)" +
						"SELECT TC.CharacterID, TC.UserID, TC.[Name], pu.[Name] AS UserName, coalesce(TOD.TimeOnline, 0) AS Today, coalesce(TW.TimeOnline, 0) As ThisWeek, coalesce(TM.TimeOnline, 0) As ThisMonth FROM TibiaCharacter TC JOIN pf_PopForumsUser pu on TC.UserID = pu.UserID LEFT JOIN TibiaCharacterOnline TOD ON TC.CharacterID = TOD.CharacterID AND TOD.Date BETWEEN dateadd(day, -1, GETDATE()) AND GETDATE() LEFT JOIN ThisWeek TW ON TC.CharacterID = TW.CharacterID LEFT JOIN ThisMonth TM ON TC.CharacterID = TM.CharacterID"));
			return list.Result.ToList();
		}

		public async Task<List<TibiaEvent>> GetEvents()
		{
			return new List<TibiaEvent>();
		}

		public async Task<List<TibiaCharacter>> GetCharactersByUserID(int userID)
		{
			Task<IEnumerable<TibiaCharacter>> list = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(c =>
				list = c.QueryAsync<TibiaCharacter>("SELECT CharacterID, UserID, Name, Level, IsAlt FROM TibiaCharacter WHERE UserID = @UserID", new
				{
					UserID = userID
				}));
			return list.Result.ToList();
		}

		public async void DeleteUserCharacter(string character)
		{
			await _sqlObjectFactory.GetConnection().UsingAsync(connection => connection
				.ExecuteAsync("DELETE FROM [TibiaCharacter] WHERE Name = @Character", new { Character = character }));
		}
	}
}