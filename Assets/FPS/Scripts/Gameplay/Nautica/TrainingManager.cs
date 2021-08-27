using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using Unity.FPS.AI;
using Unity.MLAgents;


namespace Nautica {
	/// <summary>
	/// Manager for overall agent training.
	/// Handles multiple levels prefabs, with agents running in each.
	/// These levels may be different difficulty (use different level prefabs),
	/// and will teleport the agent to different levels based on what its
	/// currentLevel is set to.
	/// Intent is for currentLevel to be modified by curriculum learning eventually.
	/// TrainingManager lets TrainingLevelManagers and ResetAnchors handle the
	/// details of scoring and resetting the levels.
	/// It does enable/disable levels and teleport agents into the correct levels as needed.
	/// </summary>
	[RequireComponent(typeof(ScoreManager))]
	public class TrainingManager : MonoBehaviour
    {
		[SerializeField] private int currentLevel = 0;  // the level we're currently training agents in
		public int nextLevel = 0;  // if this doesn't match currentLevel, it means we're going to reset to the new level
		public GameObject agentPrefab;
		public List<GameObject> levels = new List<GameObject>();
		private List<TrainingLevelManager> trainingLevelManagers = new List<TrainingLevelManager>();
		public bool humanControl = false;
		private bool inTrainingMode = false; 
        public bool debugOutput = false;
        private const string LOGTAG = nameof(TrainingManager);
		public int lastLevel = 3;
		private bool inChallengeTrials = false;
		private TrainingLevelManager currentLevelManager;
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

			if(this.transform.parent.name == "ChallengeManager")
            {
				inChallengeTrials = true;
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
			var manager = level.GetComponent<TrainingLevelManager>();
			if (manager == null) return;

			manager.SetManager(this);

			if (manager.level != currentLevel)
			{
				manager.gameObject.SetActive(false);
				return;
			}

			InstantiateAgentUsingAgentAnchor(level, manager);
		}

		private void InstantiateAgentUsingAgentAnchor(GameObject level, TrainingLevelManager manager)
        {
			GameObject agentAnchor = manager.agentAnchor;
			GameObject newAgent = Instantiate(agentPrefab, agentAnchor.transform.position, agentAnchor.transform.rotation);
			currentLevelManager = manager;
			newAgent.transform.parent = level.transform;
			manager.SetAgent(newAgent);
		}

		public AbstractNauticaAgent GetAgent()
		{
			AbstractNauticaAgent agent = currentLevelManager.GetAgent();
			return agent;
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

		public void SetUpNextLevel()
		{
			if (nextLevel >= lastLevel) return;

			if (inTrainingMode && !inChallengeTrials) 
			{
				nextLevel = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("level", nextLevel);
			}
			else
			{
				nextLevel++;
			}

			SwitchLevel();
		}

        public void ResetScoreDisplay()
        {
			scoreManager.ResetScore();
        }
    }
}
