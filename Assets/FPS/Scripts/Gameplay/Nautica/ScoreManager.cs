using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Nautica;

namespace Nautica
{
    /// <summary>
    /// This class is responsible for having a visual score for the NAUTICA Challenge. It takes the rewards from the BotKillerAgent class that have to do with hitting an enemy or getting hit.
    /// ScoreManager will be a way for us to score how well an agent does when put to the test in the challenge.
    /// </summary>
    [RequireComponent(typeof(ChallengeManager))]
    public class ScoreManager : MonoBehaviour
    {
        public Text scoreText;

        private AbstractNauticaAgent agent;
        private float score;
        private float lastEnemyHitScore;
        private ChallengeManager challengeManager;
        public ScoreLog scoreLog;

        void Start()
        {
            challengeManager = GetComponent<ChallengeManager>();
            scoreLog = GetComponent<ScoreLog>();

            agent = challengeManager.GetAgent();
            score = 0;
            SaveScoreToLog();
        }

        void awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        void Update()
        {
            float newScore = agent.GetEnemyHitScore();
            if (lastEnemyHitScore != newScore)
            {
                lastEnemyHitScore = newScore;
                score += newScore;
                SaveScoreToLog();
            }
            DisplayScore();
        }

        public void ResetScore()
        {
            score = 0f;
            agent.SetEnemyHitScore(0f);
        }

        void DisplayScore()
        {
            scoreText.text = (" " + score.ToString("0.000")); 
        }

        private void SaveScoreToLog()
        {
            scoreLog.AddNewScore(score.ToString());
        }
    }
}
 