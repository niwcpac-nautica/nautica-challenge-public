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

        public TrainingManager train;

        public GameObject clone;
        public BotKillerAgent botKiller;
        
        void Start()
        {

            if (!train) train = FindObjectOfType<TrainingManager>();

            clone = train.cloneAgent;
               
            
            //This might be overkill but I needed to reference The BotKillerAgent clone script created by the training manager.
            botKiller = clone.GetComponent<BotKillerAgent>();
            playerHit = botKiller.playerHit;
            enemyHit = botKiller.enemyHit;
          
        }
        void awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        // Update is called once per frame
        void Update()
        {
            //The rewards are saved in the BotKillerAgent and then ported over here. the score only updates if the last score changes from what it was previously.
            if(playerHit != botKiller.playerHit) {
                playerHit = botKiller.playerHit;
                score += playerHit;
            }
            else if(enemyHit != botKiller.enemyHit)
            {
                enemyHit = botKiller.enemyHit;
                score += enemyHit;
            }
            
            DisplayScore(score);
            //Debug.unityLogger.Log("Score: " + score);
        }

        void DisplayScore(float scoreToDisplay)
        {
            scoreText.text = (" " + scoreToDisplay); 
        } 
        
            
    }

}
 