namespace PopForums.Models
{
	public class TibiaCharacterOnlineStatistics
	{
		public int CharacterID { get; set; }
		public int UserID { get; set; }
		public string UserName { get; set; }
		public string Name { get; set; }
		public int Today { get; set; }
		public int ThisWeek { get; set; }
		public int	ThisMonth { get; set; }
	}
}
