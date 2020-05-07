using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PopForums.Configuration;
using PopForums.Models;
using PopForums.Repositories;
using PopForums.Services;

namespace PopForums.ScoringGame
{
	public interface IAwardDefinitionService
	{
		Task<AwardDefinition> Get(string awardDefinitionID);
		Task<List<AwardDefinition>> GetByEventDefinitionID(string eventDefinitionID);
		Task Create(AwardDefinition awardDefinition);
		Task Delete(string awardDefinitionID);
		Task<List<AwardCondition>> GetConditions(string awardDefinitionID);
		Task SaveConditions(AwardDefinition awardDefinition, List<AwardCondition> conditions);
		Task<List<AwardDefinition>> GetAll();
		Task DeleteCondition(string awardDefinitionID, string eventDefinitionID);
		Task AddCondition(AwardCondition awardDefintion); 
		Task EditAwardImage(string awardId, byte[] awardFile);
	}

	public class AwardDefinitionService : IAwardDefinitionService
	{
		public AwardDefinitionService(IAwardDefinitionRepository awardDefintionRepository, IAwardConditionRepository awardConditionRepository, IAwardImageRepository awardImageRepository, IImageService imageService, ISettingsManager settingsManager)
		{
			_awardDefinitionRepository = awardDefintionRepository;
			_awardConditionRepository = awardConditionRepository;
			_awardImageRepository = awardImageRepository;
			_imageService = imageService;
			_settingsManager = settingsManager;
		}

		private readonly IAwardDefinitionRepository _awardDefinitionRepository;
		private readonly IAwardConditionRepository _awardConditionRepository;
		private readonly IAwardImageRepository _awardImageRepository;
		private readonly IImageService _imageService;
		private readonly ISettingsManager _settingsManager;

		public async Task<AwardDefinition> Get(string awardDefinitionID)
		{
			return await _awardDefinitionRepository.Get(awardDefinitionID);
		}

		public async Task<List<AwardDefinition>> GetAll()
		{
			return await _awardDefinitionRepository.GetAll();
		}

		public async Task<List<AwardDefinition>> GetByEventDefinitionID(string eventDefinitionID)
		{
			return await _awardDefinitionRepository.GetByEventDefinitionID(eventDefinitionID);
		}

		public async Task Create(AwardDefinition awardDefinition)
		{
			await _awardDefinitionRepository.Create(awardDefinition.AwardDefinitionID, awardDefinition.Title, awardDefinition.Description, awardDefinition.IsSingleTimeAward);
		}

		public async Task Delete(string awardDefinitionID)
		{
			await _awardDefinitionRepository.Delete(awardDefinitionID);
		}

		public async Task<List<AwardCondition>> GetConditions(string awardDefinitionID)
		{
			return await _awardConditionRepository.GetConditions(awardDefinitionID);
		}

		public async Task SaveConditions(AwardDefinition awardDefinition, List<AwardCondition> conditions)
		{
			await _awardConditionRepository.DeleteConditions(awardDefinition.AwardDefinitionID);
			foreach (var condition in conditions)
				condition.AwardDefinitionID = awardDefinition.AwardDefinitionID;
			await _awardConditionRepository.SaveConditions(conditions);
		}

		public async Task DeleteCondition(string awardDefinitionID, string eventDefinitionID)
		{
			await _awardConditionRepository.DeleteCondition(awardDefinitionID, eventDefinitionID);
		}

		public async Task AddCondition(AwardCondition awardDefintion)
		{
			await _awardConditionRepository.SaveConditions(new List<AwardCondition> {awardDefintion});
		}

		public async Task EditAwardImage(string awardId, byte[] awardFile)
		{

			if (awardFile != null && awardFile.Length > 0)
			{
				await _awardImageRepository.DeleteImagesByAwardID(awardId);
				var bytes = _imageService.ConstrainResize(awardFile, _settingsManager.Current.UserImageMaxWidth, _settingsManager.Current.UserImageMaxHeight, 70);
				await _awardImageRepository.SaveNewImage(awardId, bytes, DateTime.UtcNow);
			}
		}
	}
}
