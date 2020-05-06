namespace PopForums.Models
{
	public class TibiaCharacterOnlineStatistics
	{
		public int CharacterID { get; set; }
		public int UserID { get; set; }
		public string UserName { get; set; }
		public string Name { get; set; }
		public int OnlineTimeToday { get; set; }
		public int OnlineTimeThisWeek { get; set; }
		public int OnlineTimeThisMonth { get; set; }
	}
}
