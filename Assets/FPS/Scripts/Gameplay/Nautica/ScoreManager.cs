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
        
        static public float score;

        public Text scoreText;

        public float enemyHit;
        public float playerHit;

        public TrainingManager trainingManager;
        private AbstractNauticaAgent agent;
        private float newPlayerhitScore;
        private float newEnemyHitscore;
        
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

        void Awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        void Update()
        {
            newEnemyHitscore = agent.GetEnemyHitScore();
            newPlayerhitScore = agent.GetPlayerHitScore();

            UpdateScore();
            
            DisplayScore(score);
        }

        private void UpdateScore()
        {
            if(PlayerWasHit())
            {
                SetNewPlayerHitScore();
            }
            else if(EnemyWasHit())
            {
                SetNewEnemyHitScore();
            }
        }

        private bool PlayerWasHit()
        {
            return playerHit != newPlayerhitScore;
        }

        private bool EnemyWasHit()
        {
            return enemyHit != newEnemyHitscore;
        }

        private void SetNewPlayerHitScore()
        {
            playerHit = newPlayerhitScore;
            score += playerHit;
        }

        private void SetNewEnemyHitScore()
        {
            enemyHit = newEnemyHitscore;
            score += enemyHit;
        }

        public void ResetScore()
        {
            playerHit = 0f;
            enemyHit = 0f;
            DisplayScore(0f);
        }

        void DisplayScore(float scoreToDisplay)
        {
            scoreText.text = (" " + scoreToDisplay); 
        } 
    }
}
 