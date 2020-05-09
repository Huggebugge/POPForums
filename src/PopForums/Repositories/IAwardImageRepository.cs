using PopForums.Models;
using System;
using System.Threading.Tasks;

namespace PopForums.Repositories
{
	public interface IAwardImageRepository
	{
		Task<byte[]> GetImageData(string awardID);
		Task<DateTime?> GetLastModificationDate(string awardID);
		Task<AwardImage> Get(string awardID);
		Task DeleteImagesByAwardID(string awardId);
		Task SaveNewImage(string awardId, byte[] bytes, DateTime utcNow);
	}
}
