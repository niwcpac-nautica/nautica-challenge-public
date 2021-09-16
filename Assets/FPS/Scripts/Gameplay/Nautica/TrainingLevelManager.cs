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
		public GameObject agentObj { get; protected set; }
		public AbstractNauticaAgent agent { get; protected set; }
		public List<GameObject> enemies = new List<GameObject>();
		public List<GameObject> pickups = new List<GameObject>();
		public const float WinReward = 1.0f;
		public const float LoseReward = -1.0f;
		private const string LOGTAG = nameof(TrainingLevelManager);
		private TrainingManager trainingManager;

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

		protected void CreateNewAgent(GameObject newAgent)
        {
			agentObj = newAgent;  // regardless of whether this is null, because we may want to unset it

			if (agentObj)
			{
				agent = agentObj.GetComponent<AbstractNauticaAgent>();
			}
		}

		protected void ResetAgentAnchor()
        {
			if (agentAnchor == null) return;

			var agentResetAnchor = agentAnchor.GetComponent<AgentResetAnchor>();
			if (agentResetAnchor)
			{
				agentResetAnchor.entity = agentObj;
			}
		}

		protected void SetActorManager()
        {
			var actorsManager = GetComponent<ActorsManager>();

			if (actorsManager == null) return;

			SetAgentAsPlayerInActorsManager(actorsManager);
		}

		protected void SetAgentAsPlayerInActorsManager(ActorsManager actorsManager)
        {
			actorsManager.SetPlayer(agentObj);

			if (agentObj == null) return;

			AddPlayerActorToListOfActors(actorsManager);
		}

		protected void AddPlayerActorToListOfActors(ActorsManager actorsManager)
        {
			var agentActor = agentObj.GetComponent<Actor>();
			if (agentAnchor == null) return;

			if (!actorsManager.Actors.Contains(agentActor))
			{
				actorsManager.Actors.Add(agentActor);
			}
		}

		public void SetManager(TrainingManager manager)
        {
			trainingManager = manager;
        }

		public AbstractNauticaAgent GetAgent()
		{
			return agent;
		}

		void FixedUpdate()
		{
			if (AgentDidNotMove()) return;
			if (AgentDoesNotExistInLevel()) return;

			CheckForEndOfEpisodeEvent();
		}

		protected bool AgentDidNotMove()
        {
			return Academy.Instance.StepCount == 0;
		}

		protected bool AgentDoesNotExistInLevel()
        {
			return !agentObj || !agent;
		}

		protected virtual void CheckForEndOfEpisodeEvent()
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
		
		protected bool AgentIsDead()
		{
			var agentHealth = agentObj.GetComponent<Health>();
			return agentHealth && agentHealth.CurrentHealth <= 0;
		}

		protected void RewardAgent(float reward, String message)
        {
			agent.AddReward(reward);
			Debug.unityLogger.Log(LOGTAG, message + agent.GetCumulativeReward());
			Reset();
		}

		public bool AllEnemiesAreDead()
        {
			return enemies.All(e => e != null && e.GetComponent<Health>().CurrentHealth <= 0);
		}

		protected bool AgentReachedMaxSteps()
        {
			return agent.StepCount >= agent.MaxStep - 1; 
        }

		protected virtual void MoveToNextLevel()
		{
			trainingManager.SetUpNextLevel();
		}

		public void Reset()
		{
			CleanupLevel();
			agent?.EndEpisode();
			OnEpisodeReset?.Invoke();
		}

		protected void CleanupLevel()
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
