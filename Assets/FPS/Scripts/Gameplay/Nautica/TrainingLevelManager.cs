using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;


namespace Nautica {
	/// <summary>
	/// Attached to a level prefab, manages training for a single level.
	/// Handles spawning of agent prefab and sets agent anchor for resets,
	/// also handles the win/lose logic checks for the level (all enemies killed, etc)
	/// rewarding agent appropriately.
	/// </summary>
	public class TrainingLevelManager : MonoBehaviour
	{
		public Action OnEpisodeReset;

		public int level = 0;  // set this to level number for tracking by main TrainingManager
		public GameObject agentAnchor;  // for now assume only single agent
		public GameObject agentObj { get; private set; }
		public AbstractNauticaAgent agent { get; private set; }
		public List<GameObject> enemies = new List<GameObject>();
		public List<GameObject> pickups = new List<GameObject>();
		private const float WinReward = 1.0f;
		private const float LoseReward = -1.0f;
		private const string LOGTAG = nameof(TrainingLevelManager);


		/// <summary>
		/// Setup the agent for this training level.
		/// Assumes agent is instantiated somehwere else, and we just set it here
		/// </summary>
		/// <param name="newAgent">The agent to use, or null</param>
		public void SetAgent(GameObject newAgent)
		{
			CreateNewAgent(newAgent);
			ResetAgentAnchor();
			SetActorManager();
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

		public bool AllEnemiesAreDead()
        {
			return enemies.All(e => e != null && e.GetComponent<Health>().CurrentHealth <= 0);
		}

		private bool AgentReachedMaxSteps()
        {
			return agent.StepCount >= agent.MaxStep - 1; 
        }

		private void MoveToNextLevel()
		{
			GameObject manager = FindManager();
			if (manager == null) return;

			var trainingManager = manager.GetComponent<TrainingManager>();
			trainingManager.SetUpNextLevel();
		}

		private GameObject FindManager()
        {
			GameObject manager = GameObject.Find("TrainingManager");
			if (manager != null)
			{
				return manager;
			}

			manager = GameObject.Find("ChallengeManager");
			if (manager != null)
			{
				return manager;
			}

			return null;
        }

		public void Reset()
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
