using PopForums.Configuration;
using PopForums.ScoringGame;
using System;
using Microsoft.Extensions.DependencyInjection;
namespace PopForums.Services
{
	public class TibiaApplicationService: ApplicationServiceBase
	{
		public override void Start(IServiceProvider serviceProvider)
		{
			_tibiaService = serviceProvider.GetService<ITibiaService>();
			_settingsManager = serviceProvider.GetService<ISettingsManager>();
			_eventPublisher = serviceProvider.GetService<IEventPublisher>();
			base.Start(serviceProvider);
		}
		private IEventPublisher _eventPublisher;
		private ITibiaService _tibiaService;
		private ISettingsManager _settingsManager;

		protected override void ServiceAction()
		{
			TibiaServiceWorker.Instance.GetOnlinePlayers(_tibiaService, _eventPublisher);

		}

		protected override int GetInterval()
		{
			return _settingsManager.Current.TibiaUpdateInterval;
		}

	}
}
