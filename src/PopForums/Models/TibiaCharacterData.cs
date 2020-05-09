using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PopForums.Models
{
	public class TibiaCharacterWebData
	{
		[JsonPropertyName("characters")]
		public TibiaCharacterData Character { get; set; }
	}
	public class TibiaCharacterData
	{
		[JsonPropertyName("data")]
		public TibiaCharacter Data {get;set;}
	}
}
