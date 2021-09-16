using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine.SceneManagement;

namespace Nautica
{
    public class ChallengeLevelManager : TrainingLevelManager
    {
		private const float WinReward = 1.0f;
		private const float LoseReward = -1.0f;
		private const string LOGTAG = nameof(TrainingLevelManager);
		private ChallengeManager challengeManager;
		public string LoseSceneName = "GameOver";

		public void SetManager(ChallengeManager manager)
        {
			challengeManager = manager;
        }

		void FixedUpdate()
		{
			if (AgentDidNotMove()) return;
			if (AgentDoesNotExistInLevel()) return;

			CheckForEndOfEpisodeEvent();
		}

		protected override void CheckForEndOfEpisodeEvent()
		{
			if (AgentIsDead())
			{
				RewardAgent(LoseReward, "Agent loses, cumulative reward = ");
				SceneManager.LoadScene(LoseSceneName);
				return;
			}

			if (AllEnemiesAreDead())
			{
				RewardAgent(WinReward, "Agent wins, cumulative reward = ");
				Reset();
				MoveToNextLevel();
				return;
			}
		}

		protected override void MoveToNextLevel()
		{
			ResetAgentHealth();
			challengeManager.SetUpNextLevel();
		}

		private void ResetAgentHealth()
        {
			var agentHealth = agentObj.GetComponent<Health>();
			agentHealth.Revive();
			challengeManager.ResetHealthBar();
		}
	}
}
