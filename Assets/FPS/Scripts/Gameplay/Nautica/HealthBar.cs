using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nautica
{
    public class HealthBar : MonoBehaviour
    {
        public Image healthBar;
        private ChallengeManager challengeManager;
        private AbstractNauticaAgent agent;
        private float lastHealthLevel;
        private float health;
        
        void Start()
        {
            challengeManager = GetComponent<ChallengeManager>();
            agent = challengeManager.GetAgent();

            healthBar.fillAmount = 1f;
            lastHealthLevel = 1f;
            health = 1f;
        }

        void Update()
        {
            float newHealth = agent.GetAgentHealth();
            if (lastHealthLevel != newHealth)
            {
                lastHealthLevel = newHealth;
                health += lastHealthLevel;
                healthBar.fillAmount = health;
                Debug.Log("agent health: " + health);
            }
        }

        public void ResetHealthBar()
        {
            health = 1f;
            healthBar.fillAmount = 1f;
        }
    }
}