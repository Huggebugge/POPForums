using System.Text.Json.Serialization;

namespace PopForums.Models
{
	public class TibiaCharacter
	{
		public int CharacterID { get; set; }
		public int UserID { get; set; }
		public User User { get; set; }
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[JsonPropertyName("level")]
		public int Level { get; set; }
		public bool IsAlt { get; set; }

		[JsonPropertyName("vocation")]
		public string Vocation { get; set; }
		[JsonPropertyName("comment")]
		public string Comment { get; set; }
	}
}
