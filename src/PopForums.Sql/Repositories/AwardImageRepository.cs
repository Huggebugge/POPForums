using System;
using System.Threading.Tasks;
using Dapper;
using PopForums.Models;
using PopForums.Repositories;

namespace PopForums.Sql.Repositories
{
	public class AwardImageRepository : IAwardImageRepository
	{
		private readonly ISqlObjectFactory _sqlObjectFactory;

		public AwardImageRepository(ISqlObjectFactory sqlObjectFactory)
		{
			_sqlObjectFactory = sqlObjectFactory;
		}

		public async Task DeleteImagesByAwardID(string awardId)
		{
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				connection.ExecuteAsync("DELETE FROM pf_AwardImage WHERE AwardID = @AwardID", new { AwardID = awardId }));
		}

		public async Task<AwardImage> Get(string awardID)
		{

			Task<AwardImage> awardImage = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				awardImage = connection.QuerySingleOrDefaultAsync<AwardImage>("SELECT AwardImageID, AwardID FROM pf_AwardImage WHERE AwardID = @AwardID", new { AwardID = awardID }));
			return await awardImage;
		}

		public async Task<byte[]> GetImageData(string awardID)
		{

			Task<byte[]> data = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				data = connection.ExecuteScalarAsync<byte[]>("SELECT ImageData FROM pf_AwardImage WHERE AwardID = @AwardID", new { AwardID = awardID }));
			return await data;

		}

		public async Task<DateTime?> GetLastModificationDate(string awardID)
		{
			Task<DateTime?> timeStamp = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				timeStamp = connection.QuerySingleOrDefaultAsync<DateTime?>("SELECT [TimeStamp] FROM pf_AwardImage WHERE AwardID = @AwardID", new { AwardID = awardID }));
			return await timeStamp;
		}

		public async Task SaveNewImage(string awardId, byte[] imageData, DateTime utcNow)
		{
			Task<int> userImageID = null;
			await _sqlObjectFactory.GetConnection().UsingAsync(connection =>
				userImageID = connection.QuerySingleAsync<int>("INSERT INTO pf_AwardImage (AwardID, [TimeStamp], ImageData) VALUES (@AwardID, @TimeStamp, @ImageData);SELECT CAST(SCOPE_IDENTITY() as int)", new { AwardID = awardId, TimeStamp = utcNow, ImageData = imageData }));
		}
	}
}