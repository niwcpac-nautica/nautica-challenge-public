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
    /// Manager for agent training scene.
    /// During training, we'll disable the GameFlowManager and ObjectiveManager,
    /// instead of tracking objectives, we'll just check if all enemies are dead.
    /// If agent runs out of max steps, or all enemies are dead, we'll reset episode.
    /// Reset will reset the agent and respawn the enemies.
    /// We also have things in here for managing 2 teams of agents;
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        public List<GameObject> team1AgentPrefabs = new List<GameObject>();
        public List<GameObject> team1AgentSpawnPoints = new List<GameObject>();
        [SerializeField] private List<AbstractNauticaAgent> team1Agents = new List<AbstractNauticaAgent>();
        [SerializeField] private List<GameObject> team1 = new List<GameObject>();
        // TODO: future team management for later
        // public List<GameObject> team2AgentPrefabs = new List<GameObject>();
        // private List<AbstractNauticaAgent> team2Agents = new List<AbstractNauticaAgent>();
        // private List<GameObject> team2 = new List<GameObject>();
        // public Color team1Color;
        // public Color team2Color;

        // initially we'll only have bots as enemies
        public List<GameObject> enemyPrefabs = new List<GameObject>();
        public List<GameObject> enemySpawnPoints = new List<GameObject>();
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();

        // TODO: pickups

        public bool humanControl = false;
        public bool debugOutput = false;
        private const string LOGTAG = nameof(TrainingManager);


        void Awake()
        {
        }

        void Start()
        {
            Init();

			var debugDisplayer = FindObjectOfType<DebugDisplayer>();
			if (debugDisplayer)
			{
				// TODO: this is a hack, grabs any agent in scene, for now we only have single player
				debugDisplayer.agent = FindObjectOfType<AbstractNauticaAgent>();
			}

            if (Academy.Instance.IsCommunicatorOn) humanControl = false;  // when in training mode, force agent control
        }

        private void Reset()
        {
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "Resetting environment...");
            // assume all enemies, agents are already initialized, tracked in lists
            for (int i=0; i < enemies.Count; i++)
            {
                if (i >= enemySpawnPoints.Count) break;
                ResetBot(enemies[i], enemySpawnPoints[i]);
            }

            for (int i=0; i < team1.Count; i++)
            {
                if (i >= team1AgentSpawnPoints.Count) break;
                ResetAgent(team1[i], team1AgentSpawnPoints[i]);
            }

            // TODO: team2
        }

        private void Init()
        {
            // clear out any earlier enemies first, just in case
            foreach (var enemy in enemies)
            {
                Destroy(enemy);
            }
            enemies.Clear();

            // clear out any earlier team1 agents first, just in case
            foreach (var agent in team1)
            {
                Destroy(agent);
            }
            team1.Clear();

            // TODO: team2

            // spawn all enemies
            for (int i=0; i < enemyPrefabs.Count; i++)
            {
                // assume we have a matching spawnpoint for each prefab, otherwise stop (or could spawn at 0,0,0)
                if (i >= enemySpawnPoints.Count) break;
                var instance = SpawnEntity(enemyPrefabs[i], enemySpawnPoints[i]);
                if (instance)
                {
                    enemies.Add(instance);
                    if (debugOutput) Debug.unityLogger.Log(LOGTAG, "spawned enemy: " + instance.name);
                }
            }

            // spawn all team1 agents
            for (int i=0; i < team1AgentPrefabs.Count; i++)
            {
                if (i >= team1AgentSpawnPoints.Count) break;
                var instance = SpawnEntity(team1AgentPrefabs[i], team1AgentSpawnPoints[i]);
                if (instance)
                {
                    team1.Add(instance);
                    var agent = instance.GetComponent<AbstractNauticaAgent>();
                    if (agent)
                    {
                        team1Agents.Add(agent);
                        if (debugOutput) Debug.unityLogger.Log(LOGTAG, "spawned team1 member: " + agent.name);
                    }
                }
            }

            // TODO: team2
        }

        private GameObject SpawnEntity(GameObject prefab, GameObject spawnpoint)
        {
            if (!prefab || !spawnpoint) return null;
            // TODO: forgot to add rotation as well
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "SpawnEntity " + prefab.name);
            GameObject instance = Instantiate(prefab, spawnpoint.transform.position, spawnpoint.transform.rotation);
            return instance;
        }

        private void ResetAgent(GameObject agentObj, GameObject spawnpoint)
        {
            if (!agentObj || !spawnpoint) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetAgent " + agentObj.name);
            ResetEntity(agentObj, spawnpoint);

            // any specific agent related stuff
            var weaponManager = agentObj.GetComponent<PlayerWeaponsManager>();
            weaponManager.SwitchToWeaponIndex(0);
            var currentWeapon = weaponManager.GetActiveWeapon();
            if (currentWeapon) currentWeapon.ResetAmmo();
            // TODO: note that this is supposed to work, but seems like a timing issue on PlayerCharacterController.cs OnDie() sets to -1 (invalid) after we try to set it
            // seems like they do this to lower the weapon when dead, so it nicely animates the gun up when starting over
            // so this is removed in PlayerCharacterController.cs, which is hacky but it works
        }

        private void ResetBot(GameObject botObj, GameObject spawnpoint)
        {
            if (!botObj || !spawnpoint) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetBot " + botObj.name);
            ResetEntity(botObj, spawnpoint);

            // bot specific stuff
            var detectionModule = botObj.GetComponentInChildren<DetectionModule>();
            if (detectionModule)
            {
                detectionModule.Reset();
            }
            var enemyMobile = botObj.GetComponent<EnemyMobile>();
            if (enemyMobile)
            {
                enemyMobile.Reset();
            }
        }

        private void ResetEntity(GameObject entity, GameObject spawnpoint)
        {
            if (!entity || !spawnpoint) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntity " + entity.name);
            entity.SetActive(true);
            ResetEntityHealth(entity);
            ResetEntityPos(entity, spawnpoint.transform.position);
			ResetEntityRot(entity, spawnpoint.transform.rotation);
            // add more here if theres other stuff to reset
        }

        private void ResetEntityHealth(GameObject entity)
        {
            if (!entity) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityHealth on " + entity.name);
            Health health = entity.GetComponent<Health>();
            if (health)
            {
                health.Revive();
                if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityHealth found and called Revive()");
            }
        }

        private void ResetEntityPos(GameObject entity, Vector3 newPos)
        {
            if (!entity) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityPos with " + entity.name + " to " + newPos.ToString());

            var navmeshAgent = entity.GetComponent<NavMeshAgent>();
            if (navmeshAgent)
            {
                if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityPos found NavMeshAgent, calling Warp()");
                navmeshAgent.Warp(newPos);
            }
            else
            {
                if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityPos no NavMeshAgent, set transform.position");
                entity.transform.position = new Vector3(newPos.x, newPos.y, newPos.z);
                var characterController = entity.GetComponent<CharacterController>();
                if (characterController)
                {
                    Physics.SyncTransforms();
                }
            }

            // if rigidbody exists, may have physics operating, need to zero out velocities
            var rb = entity.gameObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityPos found and reset rigidbody velocities");
            }
        }

		private void ResetEntityRot(GameObject entity, Quaternion rot)
		{
            if (!entity) return;
            if (debugOutput) Debug.unityLogger.Log(LOGTAG, "ResetEntityRot with " + entity.name + " to " + rot.eulerAngles.ToString());
			entity.transform.rotation = rot;
		}

        void FixedUpdate()
        {
            bool reset = false;

            // if all agents dead, trigger end episode
            if (team1.All(t => t != null && t.GetComponent<Health>()?.CurrentHealth == 0))
            {
                // before agents,enemies spawn, they all look dead, so on step0, just skip our checks
                if (Academy.Instance.StepCount == 0) return;

                Debug.unityLogger.Log(LOGTAG, "All team1 agents dead!");

                // end episode with loss
                foreach (var agent in team1Agents)
                {
                    agent.AddReward(-1.0f);
                    Debug.unityLogger.Log("AGENT TOTAL REWARD: " + agent.GetCumulativeReward());
                    agent.EndEpisode();
                }
                reset = true;
            }

            // if all enemies dead, trigger end episode
            else if (enemies.All(e => e != null && e.GetComponent<Health>().CurrentHealth == 0))
            {
                Debug.unityLogger.Log(LOGTAG, "All enemies dead!");

                // end episode with win
                foreach (var agent in team1Agents)
                {
                    agent.AddReward(1.0f);
                    Debug.unityLogger.Log("AGENT TOTAL REWARD: " + agent.GetCumulativeReward());
                    agent.EndEpisode();
                }
                reset = true;
            }

            // if any agent reaches end of episode (MAX_STEPS) we reset everyone to make it easier
            else if (team1Agents.Any(t => t != null && t.StepCount >= t.MaxStep -1))
            {
                Debug.unityLogger.Log(LOGTAG, "Agents reached MAX_STEP");

                // reached end of episode, need to reset with no win
                foreach (var agent in team1Agents)
                {
                    agent.AddReward(0f);
                    Debug.unityLogger.Log("AGENT TOTAL REWARD: " + agent.GetCumulativeReward());
                    agent.EndEpisode();
                }
                reset = true;
            }

            if (reset)
            {
                Debug.unityLogger.Log(LOGTAG, "Trigger Reset Environment...");
                Reset();
            }
        }
    }
}
