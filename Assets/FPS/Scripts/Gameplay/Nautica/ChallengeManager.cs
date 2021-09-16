using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using Unity.FPS.AI;
using Unity.MLAgents;

namespace Nautica
{
	public class ChallengeManager : TrainingManager
	{
		private const string LOGTAG = nameof(ChallengeManager);
		public ScoreManager scoreManager;
		public HealthBar healthBar;

		void Start()
		{
			SetupEnvironmentMode();
			SetupLevels();
			scoreManager = GetComponent<ScoreManager>();
			healthBar = GetComponent<HealthBar>();
		}

		protected override void SetupLevels()
		{
			foreach (var level in levels)
			{
				ActivateCurrentLevelOnly(level);
			}
		}

		protected override void ActivateCurrentLevelOnly(GameObject level)
		{
			var manager = level.GetComponent<ChallengeLevelManager>();
			if (manager == null) return;

			manager.SetManager(this);

			if (manager.level != currentLevel)
			{
				manager.gameObject.SetActive(false);
				return;
			}

			InstantiateAgentUsingAgentAnchor(level, manager);
		}

		public override void SetUpNextLevel()
		{
			if (nextLevel >= lastLevel) return;

			nextLevel++;

			SwitchLevel();
		}

		public void ResetHealthBar()
        {
			healthBar.ResetHealthBar();
        }
	}

}