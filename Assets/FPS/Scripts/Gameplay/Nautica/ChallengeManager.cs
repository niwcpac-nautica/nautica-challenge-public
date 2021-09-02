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
	[RequireComponent(typeof(ScoreManager))]
	public class ChallengeManager : TrainingManager
	{
		private const string LOGTAG = nameof(ChallengeManager);
		public ScoreManager scoreManager;

		void Start()
		{
			SetupEnvironmentMode();
			SetupLevels();
			scoreManager = GetComponent<ScoreManager>();
		}

		private void SetupEnvironmentMode()
		{
			if (Academy.Instance.IsCommunicatorOn)
			{
				humanControl = false;
				inTrainingMode = true;
			}
		}

		private void SetupLevels()
		{
			foreach (var level in levels)
			{
				ActivateCurrentLevelOnly(level);
			}
		}

		private void ActivateCurrentLevelOnly(GameObject level)
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

		private void InstantiateAgentUsingAgentAnchor(GameObject level, ChallengeLevelManager manager)
		{
			GameObject agentAnchor = manager.agentAnchor;
			GameObject newAgent = Instantiate(agentPrefab, agentAnchor.transform.position, agentAnchor.transform.rotation);
			currentLevelManager = manager;
			newAgent.transform.parent = level.transform;
			manager.SetAgent(newAgent);
		}

		private List<TrainingLevelManager> GetLevelManagers(int level)
		{
			List<TrainingLevelManager> result = levels.Where(l => l != null)
				.Select(l => l.GetComponent<TrainingLevelManager>())
				.Where(t => t.level == level)
				.ToList();
			if (debugOutput)
			{
				string names = "";
				foreach (var r in result) names += "\n" + r.gameObject.name;
				Debug.unityLogger.Log(LOGTAG, "GetLevelManagers: Found " + result.Count.ToString() + " levels with level " + level.ToString() + names);
			}
			return result;
		}

		private List<GameObject> GetLevelObjects(int level)
		{
			return GetLevelManagers(level)
				.Select(l => l.gameObject)
				.ToList();
		}

		private void SwitchLevel()
		{
			if (nextLevel == currentLevel) return;

			if (debugOutput) Debug.unityLogger.Log(LOGTAG, "Switch from level " + currentLevel.ToString() + " to level " + nextLevel.ToString());
			List<TrainingLevelManager> oldManagers = GetLevelManagers(currentLevel);
			List<TrainingLevelManager> newManagers = GetLevelManagers(nextLevel);

			// NOTE: the counts for the levels are supposed to be the same
			for (int i = 0; i < oldManagers.Count; i++)
			{
				// if new levels > old levels, new levels are not enabled
				if (i < newManagers.Count && newManagers[i] != null)
				{
					// re-enable new level prefabs
					newManagers[i].gameObject.SetActive(true);  // not working??

					// move agent over to new level
					oldManagers[i].agentObj.transform.parent = newManagers[i].gameObject.transform;

					SwapToNewAnchor(oldManagers[i], newManagers[i]);

					// set agent in new level TrainingLevelManager
					newManagers[i].SetAgent(oldManagers[i].agentObj);

					// old manager still pointing to the agent, but it's disabled, should be ok
					// just in case, unset agent in old manager
					oldManagers[i].SetAgent(null);

					// reset the level, which resets the agent's episode and triggers the level OnEpisodeReset event, which should call the anchors to reset
					newManagers[i].Reset();
				}

				// disable old level prefabs
				// if old levels > new levels, the extra old levels agents are disabled along with the old level
				oldManagers[i].gameObject.SetActive(false);
			}

			for (int i = oldManagers.Count; i < newManagers.Count; i++)
			{
				// if new levels > old levels and somehow did get enabled, turn them off
				newManagers[i].gameObject.SetActive(false);
			}

			currentLevel = nextLevel;
			// agent end episode should trigger agents to be reset into new anchors
		}

		private void SwapToNewAnchor(TrainingLevelManager oldManager, TrainingLevelManager newManager)
		{
			var newAnchor = oldManager.agentAnchor.GetComponent<AgentResetAnchor>();
			var oldAnchor = newManager.agentAnchor.GetComponent<AgentResetAnchor>();
			if (newAnchor && oldAnchor)
			{
				newAnchor.entity = oldAnchor.entity;
				oldAnchor.entity = null;
			}
		}

		public override void SetUpNextLevel()
		{
			if (nextLevel >= lastLevel) return;

			nextLevel++;

			SwitchLevel();
		}

		public void ResetScoreDisplay()
		{
			scoreManager.ResetScore();
		}
	}

}