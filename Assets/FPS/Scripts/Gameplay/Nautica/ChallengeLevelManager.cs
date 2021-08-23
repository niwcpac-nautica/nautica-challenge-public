using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace Nautica
{
    public class ChallengeLevelManager : TrainingLevelManager
    {
		private const float WinReward = 1.0f;
		private const float LoseReward = -1.0f;
		private const string LOGTAG = nameof(TrainingLevelManager);
		private ChallengeManager challengeManager;

		/// <summary>
		/// Setup the agent for this training level.
		/// Assumes agent is instantiated somehwere else, and we just set it here
		/// </summary>
		/// <param name="newAgent">The agent to use, or null</param>
		public override void SetAgent(GameObject newAgent)
		{
			CreateNewAgent(newAgent);
			ResetAgentAnchor();
			SetActorManager();
			FindTrainingManager();
		}

		private void CreateNewAgent(GameObject newAgent)
		{
			agentObj = newAgent;  // regardless of whether this is null, because we may want to unset it

			if (agentObj)
			{
				agent = agentObj.GetComponent<AbstractNauticaAgent>();
			}
		}

		private void ResetAgentAnchor()
		{
			if (agentAnchor == null) return;

			var agentResetAnchor = agentAnchor.GetComponent<AgentResetAnchor>();
			if (agentResetAnchor)
			{
				agentResetAnchor.entity = agentObj;
			}
		}

		private void SetActorManager()
		{
			var actorsManager = GetComponent<ActorsManager>();

			if (actorsManager == null) return;

			SetAgentAsPlayerInActorsManager(actorsManager);
		}

		private void SetAgentAsPlayerInActorsManager(ActorsManager actorsManager)
		{
			actorsManager.SetPlayer(agentObj);

			if (agentObj == null) return;

			AddPlayerActorToListOfActors(actorsManager);
		}

		private void AddPlayerActorToListOfActors(ActorsManager actorsManager)
		{
			var agentActor = agentObj.GetComponent<Actor>();
			if (agentAnchor == null) return;

			if (!actorsManager.Actors.Contains(agentActor))
			{
				actorsManager.Actors.Add(agentActor);
			}
		}

		private void FindTrainingManager()
		{
			GameObject manager = FindManager();
			if (manager == null) return;

			challengeManager = manager.GetComponent<ChallengeManager>();
		}

		public override GameObject FindManager()
		{
			GameObject manager = GameObject.Find("ChallengeManager");
			if (manager != null)
			{
				return manager;
			}

			return null;
		}

		void FixedUpdate()
		{
			if (AgentDidNotMove()) return;
			if (AgentDoesNotExistInLevel()) return;

			CheckForEndOfEpisodeEvent();
		}

		private bool AgentDidNotMove()
		{
			return Academy.Instance.StepCount == 0;
		}

		private bool AgentDoesNotExistInLevel()
		{
			return !agentObj || !agent;
		}

		private void CheckForEndOfEpisodeEvent()
		{
			if (AgentIsDead())
			{
				RewardAgent(LoseReward, "Agent loses, cumulative reward = ");
				challengeManager.ResetScoreDisplay();
				return;
			}

			if (AllEnemiesAreDead())
			{
				RewardAgent(WinReward, "Agent wins, cumulative reward = ");
				MoveToNextLevel();
				return;
			}

			if (AgentReachedMaxSteps())
			{
				RewardAgent(0.0f, "Agent reached MAX_STEPS, cumulative reward = ");
			}
		}

		private bool AgentIsDead()
		{
			var agentHealth = agentObj.GetComponent<Health>();
			return agentHealth && agentHealth.CurrentHealth <= 0;
		}

		private void RewardAgent(float reward, String message)
		{
			agent.AddReward(reward);
			Debug.unityLogger.Log(LOGTAG, message + agent.GetCumulativeReward());
			Reset();
		}

		private bool AgentReachedMaxSteps()
		{
			return agent.StepCount >= agent.MaxStep - 1;
		}

		private void MoveToNextLevel()
		{
			challengeManager.SetUpNextLevel();
		}

		public override void Reset()
		{
			CleanupLevel();
			agent?.EndEpisode();
			OnEpisodeReset?.Invoke();
		}

		private void CleanupLevel()
		{
			// TODO: this will destroy projectiles in other agent's levels
			// var projectiles = GameObject.FindObjectsOfType<ProjectileStandard>();

			var projectiles = GetComponentsInChildren<ProjectileStandard>();
			foreach (var p in projectiles)
			{
				Destroy(p.gameObject);
			}
		}
	}
}
