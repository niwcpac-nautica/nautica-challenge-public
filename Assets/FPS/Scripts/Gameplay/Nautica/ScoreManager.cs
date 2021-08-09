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

    public class ScoreManager : MonoBehaviour
    {

        public float score;

        public Text scoreText;

        //public GameObject botKiller;
        public AbstractNauticaAgent agent;
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();
        [SerializeField] private float trackedHealth;
        [SerializeField] private List<float> trackedEnemyHealth = new List<float>();

        // Start is called before the first frame update
        void Start()
        {
            scoreText = GetComponent<Text>();
            if(!agent) agent = FindObjectOfType<AbstractNauticaAgent>();
            //Debug.unityLogger.Log(AbstractNauticaAgent);

            var trainingLevelManager = transform.parent.GetComponent<TrainingLevelManager>();
            if (!trainingLevelManager)
            {
                Debug.unityLogger.Log( "Could not find TrainingLevelManager!");
            }

            enemies = trainingLevelManager.enemies;

            trackedHealth = 1f;
            trackedEnemyHealth.Clear();
            foreach (var e in enemies) trackedEnemyHealth.Add(1f);
        }
        void awake()
        {
            DontDestroyOnLoad(transform.gameObject);
        }

        // Update is called once per frame
        void Update()
        {

            var healthComponent = GetComponent<Unity.FPS.Game.Health>();
            float normalizedHealth = healthComponent.GetRatio();

            if (normalizedHealth != trackedHealth)
            {
                float agentHit = normalizedHealth - trackedHealth;
                score += agentHit;
                trackedHealth = normalizedHealth;
                Debug.unityLogger.Log(agentHit + "Agent Health2");
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy) continue;

            
                // enemy health
                var enemyHealthComponent = enemy.GetComponent<Unity.FPS.Game.Health>();
                float enemyNormalizedHealth = enemyHealthComponent.GetRatio();

                if (enemyNormalizedHealth < trackedEnemyHealth[i])
                {
                    float enemyHit = (trackedEnemyHealth[i] - enemyNormalizedHealth);
                    score += enemyHit;
                    trackedEnemyHealth[i] = enemyNormalizedHealth;
                    Debug.unityLogger.Log(enemyHit + "HIT ENEMY2");
                }

                

                if (enemyNormalizedHealth <= 0.0f)
                {
                    enemyNormalizedHealth = 0f;
                }
            }

            DisplayScore(score);
            
        }
        void DisplayScore(float scoreToDisplay)
        {
            scoreText.text = string.Format("{0:0.##}", scoreToDisplay);
        }
    }

}
