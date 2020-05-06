namespace PopForums.Models
{
	public class TibiaCharacter
	{
		public int CharacterID { get; set; }
		public int UserID { get; set; }
		public User User { get; set; }
		public string Name { get; set; }
		public int Level { get; set; }
		public bool IsAlt { get; set; }
	}
}
