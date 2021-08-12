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
			agentObj = newAgent;  // regardless of whether this is null, because we may want to unset it

			if (agentObj)
			{
				agent = agentObj.GetComponent<AbstractNauticaAgent>();
			}

			if (agentAnchor)
			{
				var agentResetAnchor = agentAnchor.GetComponent<AgentResetAnchor>();
				if (agentResetAnchor)
				{
					agentResetAnchor.entity = agentObj;
				}
			}

			var actorsManager = GetComponent<ActorsManager>();
			if (actorsManager)
			{
				// set actor manager player
				actorsManager.SetPlayer(agentObj);
				if (agentObj)
				{
					// add player Actor component to list of actors
					var agentActor = agentObj.GetComponent<Actor>();
					if (agentActor)
					{
						if (!actorsManager.Actors.Contains(agentActor))
						{
							actorsManager.Actors.Add(agentActor);
						}
					}
				}
			}
		}

		void FixedUpdate()
		{
			if (Academy.Instance.StepCount == 0) return;
			if (!agentObj || !agent) return;

			// check agent dead
			var agentHealth = agentObj.GetComponent<Health>();
			if (agentHealth && agentHealth.CurrentHealth <= 0)
			{
				agent.AddReward(LoseReward);
				Debug.unityLogger.Log(LOGTAG, "Agent loses, cumulative reward = " + agent.GetCumulativeReward());
				Reset();
			}

			// check all enemies dead
			else if (enemies.All(e => e != null && e.GetComponent<Health>().CurrentHealth <= 0))
			{
				agent.AddReward(WinReward);
				Debug.unityLogger.Log(LOGTAG, "Agent wins, cumulative reward = " + agent.GetCumulativeReward());
				Reset();
			}

			else if (agent.StepCount >= agent.MaxStep-1)
			{
				agent.AddReward(0.0f);
				Debug.unityLogger.Log(LOGTAG, "Agent reached MAX_STEPS, cumulative reward = " + agent.GetCumulativeReward());
				Reset();
			}
		}

		public bool AllEnemiesAreDead()
        {
			return enemies.All(e => e != null && e.GetComponent<Health>().CurrentHealth <= 0);
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
