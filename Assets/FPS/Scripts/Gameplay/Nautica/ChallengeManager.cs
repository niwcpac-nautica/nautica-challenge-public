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

		void Start()
		{
			SetupEnvironmentMode();
			SetupLevels();
			scoreManager = GetComponent<ScoreManager>();
		}

		public override void SetUpNextLevel()
		{
			if (nextLevel >= lastLevel) return;

			nextLevel++;

			SwitchLevel();
		}
	}

}