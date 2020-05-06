using System.Collections.Generic;
using Org.BouncyCastle.Crypto.Tls;
using PopForums.Sql;
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
				character = c.QueryFirstAsync<TibiaCharacter>("SELECT CharacterID, UserID, Name, Level, IsAlt FROM TibiaCharacter"));
			return await character;
		}
		public async Task<bool> UpdateCharacter(TibiaCharacter character)
		{
			Task<int> result = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				result = connection.ExecuteAsync("UPDATE TibiaCharacter SET Level = @Level, Name = @Name, IsAlt = @Alt WHERE CharacterID = @Character", new
				{
					character.Level,
					character.Name,
					character.IsAlt,
					character.CharacterID
				}));
			return result.Result == 1;
		}

		public async Task<bool> AddCharacter(TibiaCharacter character)
		{
			Task<bool> exists = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection => exists =
				exists = connection.QuerySingleAsync("SELECT * FROM [TibiaCharacter] WHERE Name = @Character", new
				{
					character.Name
				}));
			if (exists.Result)
				return;
			_sqlObjectFactory.GetConnection().Using(connection =>
				connection.Command(_sqlObjectFactory,
						"INSERT INTO TibiaCharacter (UserID, Level, Name, IsAlt) VALUES (@UserID, @Level, @Name, @IsAlt)")
					.AddParameter(_sqlObjectFactory, "@UserID", character.UserID)
					.AddParameter(_sqlObjectFactory, "@Level", character.Level)
					.AddParameter(_sqlObjectFactory, "@Name", character.Name)
					.AddParameter(_sqlObjectFactory, "@IsAlt", character.IsAlt)
					.ExecuteNonQuery());
		}

		public void LogOnlineTime(TibiaCharacter character)
		{

			var exists = false;
			_sqlObjectFactory.GetConnection().Using(connection => exists =
				connection.Command(_sqlObjectFactory, "SELECT * FROM [TibiaCharacterOnline] WHERE CAST(GETDATE() AS DATE) = [Date] AND CharacterID = @Character")
					.AddParameter(_sqlObjectFactory, "@Character", character.CharacterID)
					.ExecuteReader().Read());
			if (exists)
			{
				_sqlObjectFactory.GetConnection().Using(connection =>
					connection.Command(_sqlObjectFactory,
							"UPDATE TibiaCharacterOnline SET [TimeOnline] = [TimeOnline] + 5 WHERE CAST(GETDATE() AS DATE) = [Date] AND CharacterID = @Character")
						.AddParameter(_sqlObjectFactory, "@Character", character.CharacterID)
						.ExecuteNonQuery());
			}
			else
			{

				_sqlObjectFactory.GetConnection().Using(connection =>
					connection.Command(_sqlObjectFactory,
							"INSERT INTO TibiaCharacterOnline (CharacterID, Date, [TimeOnline]) VALUES (@Character, CAST(GETDATE() AS DATE), 5)")
						.AddParameter(_sqlObjectFactory, "@Character", character.CharacterID)
						.ExecuteNonQuery());
			}
		}

		public List<TibiaCharacterOnlineStatistics> GetOnlineStatistics()
		{
			var list = new List<TibiaCharacterOnlineStatistics>();
			_sqlObjectFactory.GetConnection().Using(connection =>
				connection.Command(_sqlObjectFactory,
						"WITH ThisWeek AS(SELECT CharacterID, SUM(TimeOnline) as TimeOnline FROM TibiaCharacterOnline WHERE[Date] BETWEEN dateadd(day, -7, GETDATE()) AND GETDATE() GROUP BY CharacterID), ThisMonth AS(SELECT CharacterID, SUM(TimeOnline) as TimeOnline FROM TibiaCharacterOnline WHERE[Date] BETWEEN dateadd(day, -30, GETDATE()) AND GETDATE() GROUP BY CharacterID)" +
						"SELECT TC.CharacterID, TC.UserID, TC.[Name], pu.[Name], coalesce(TOD.TimeOnline, 0) AS Today, coalesce(TW.TimeOnline, 0) As ThisWeek, coalesce(TM.TimeOnline, 0) As ThisMonth FROM TibiaCharacter TC JOIN pf_PopForumsUser pu on TC.UserID = pu.UserID LEFT JOIN TibiaCharacterOnline TOD ON TC.CharacterID = TOD.CharacterID AND TOD.Date BETWEEN dateadd(day, -1, GETDATE()) AND GETDATE() LEFT JOIN ThisWeek TW ON TC.CharacterID = TW.CharacterID LEFT JOIN ThisMonth TM ON TC.CharacterID = TM.CharacterID")
					.ExecuteReader()
					.ReadAll(r => list.Add(new TibiaCharacterOnlineStatistics
					{
						CharacterID = r.GetInt32(0),
						UserID = r.GetInt32(1),
						Name = r.GetString(2),
						UserName = r.GetString(3),
						OnlineTimeToday = r.GetInt32(4),
						OnlineTimeThisWeek = r.GetInt32(5),
						OnlineTimeThisMonth = r.GetInt32(6)
					})));
			return list;
		}

		public List<TibiaEvent> GetEvents()
		{
			return new List<TibiaEvent>();
		}

		public List<TibiaCharacter> GetCharactersByUserID(int userID)
		{
			var list = new List<TibiaCharacter>();
			_sqlObjectFactory.GetConnection().Using(c =>
				c.Command(_sqlObjectFactory, "SELECT CharacterID, UserID, Name, Level, IsAlt FROM TibiaCharacter WHERE UserID = @UserID")
					.AddParameter(_sqlObjectFactory, "@UserID", userID)
					.ExecuteReader()
					.ReadAll(r => list.Add(new TibiaCharacter
					{
						CharacterID = r.GetInt32(0),
						UserID = r.GetInt32(1),
						Name = r.GetString(2),
						Level = r.GetInt32(3),
						IsAlt = r.GetBoolean(4)
					})));
			return list;
		}

		public void DeleteUserCharacter(string character)
		{
			_sqlObjectFactory.GetConnection().Using(connection => connection
				.Command(_sqlObjectFactory, "DELETE FROM [TibiaCharacter] WHERE Name = @Character")
				.AddParameter(_sqlObjectFactory, "@Character", character)
				.ExecuteNonQuery());
		}
	}
}