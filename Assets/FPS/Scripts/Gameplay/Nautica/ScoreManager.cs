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
    public class ScoreManager : MonoBehaviour
    {
        public Text scoreText;

        public float enemyHit;
        public float playerHit;

        public TrainingManager trainingManager;
        private AbstractNauticaAgent agent;
        private float score;
        
        void Start()
        {
            InitializeScores();
        }

        private void InitializeScores()
        {
            if (!trainingManager) trainingManager = FindObjectOfType<TrainingManager>();

            agent = trainingManager.GetAgent();

            playerHit = agent.GetPlayerHitScore();
            enemyHit = agent.GetEnemyHitScore();
        }

        void awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        void Update()
        {
            score = agent.GetCumulativeReward();
            DisplayScore();
        }

        void DisplayScore()
        {
            SetScoreColor();
            scoreText.text = (" " + score.ToString("0.000")); 
        } 

        private void SetScoreColor()
        {
            if (score < 0) scoreText.color = Color.red;
            if (score > 0) scoreText.color = Color.green;
        }
    }
}
 