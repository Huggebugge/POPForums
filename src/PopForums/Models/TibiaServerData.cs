using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PopForums.Models
{
	public class TibiaServerData
	{ 
		[JsonPropertyName("world")]
		public TibiaWorldData World { get; set; }
	}
	public class TibiaWorldData
	{
		[JsonPropertyName("world_information")]
		public WorldInformation WorldInformation { get; set; }
		[JsonPropertyName("players_online")]
		public List<TibiaCharacter> PlayersOnline { get; set; }
	}
	public class WorldInformation
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }
		[JsonPropertyName("players_online")]
		public int PlayersOnline { get; set; }
	}
}
