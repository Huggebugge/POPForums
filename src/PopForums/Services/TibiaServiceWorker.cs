using PopForums.ScoringGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PopForums.Services
{
	public class TibiaServiceWorker
	{
		private TibiaServiceWorker()
		{
			_run = false;
		}
		private bool _run;
		private bool _hasRun
		{
			get
			{
				if (_run == true)
					return true;
				else _run = true;
				return false;
			}
		}
		private static TibiaServiceWorker _instance;
		public async void GetOnlinePlayers(ITibiaService tibiaService, IEventPublisher eventPublisher)
		{
			if (_hasRun)
			{
				var members = await tibiaService.GetMemberCharacters();
				var onlinePlayers = tibiaService.GetOnlineCharactersFromTibia();
				foreach (var member in members)
				{
					if (onlinePlayers.Any(o => o.Name == member.Name))
					{
						tibiaService.LogOnlineTime(member);
						var onlinePlayer = onlinePlayers.Single(o => o.Name == member.Name);
						if (member.Level == 0)
						{
							member.Level = onlinePlayer.Level;
							tibiaService.UpdateCharacter(member);
						}
						if (member.Level < onlinePlayer.Level)
						{
							await eventPublisher.ProcessEvent($"{member.Name} has leveled up!", member.User, EventDefinitionService.StaticEventIDs.LevelUp, false);
							member.Level = onlinePlayer.Level;
							tibiaService.UpdateCharacter(member);
						}

					}
				}
			}
		}
		public static TibiaServiceWorker Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new TibiaServiceWorker();
				}
				return _instance;
			}
		}
	}
}