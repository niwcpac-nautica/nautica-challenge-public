using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;


namespace Nautica {
    /// <summary>
    /// this is a test agent. we will kill bots in the tutorial game, to test out our states/actions/rewards, and run training tests
    /// </summary>
    public class BotKillerAgent : AbstractNauticaAgent
    {
        public float enemyHit;
        public float playerHit;
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();
        [SerializeField] private List<GameObject> pickups = new List<GameObject>();
        [SerializeField] private GameObject lvl;
        [SerializeField] private GameObject player;

        [SerializeField] public float trackedHealth;
        [SerializeField] private List<float> trackedEnemyHealth = new List<float>();
		private PlayerWeaponsManager playerWeaponsManager;
		private WeaponController weaponController;
		private List<string> debugEnemyBufferSensorObs = new List<string>();  // this is just for debug display output during testing
		private List<string> debugPickupBufferSensorObs = new List<string>();  // this is just for debug display output during testing
		private List<float> enemyAngles = new List<float>();  // testing reward shaping based on angle to enemies
		private float weaponAmmoRatio;
        private const string LOGTAG = nameof(BotKillerAgent);

        private const float minDistanceToEnemy = 30f;

		protected override void Awake()
		{
			base.Awake();
		}

        protected override void Start()
        {
            base.Start();

			playerWeaponsManager = GetComponent<PlayerWeaponsManager>();
			weaponController = playerWeaponsManager?.GetActiveWeapon();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

			// DANGER: this assumes agent is nested under the level prefab
			// TrainingManager shuffles agents around to different levels, so need to be careful
			var trainingLevelManager = transform.parent.GetComponent<TrainingLevelManager>();
			if (!trainingLevelManager)
			{
				Debug.unityLogger.Log(LOGTAG, "Could not find TrainingLevelManager!");
			}

			enemies = trainingLevelManager.enemies;
			pickups = trainingLevelManager.pickups;

            trackedHealth = 1f;
			trackedEnemyHealth.Clear();
			foreach (var e in enemies) trackedEnemyHealth.Add(1f);

            // randomize player spawn position
            // lvl = GameObject.FindGameObjectWithTag("Level");
            // var p1 = lvl.transform.TransformPoint(10, 0, -120);
            // var p2 = lvl.transform.TransformPoint(110, 0, -20);
            // var w = p2.x - p1.x;
            // var h = p2.z - p1.z;

            // float x, y;
            // (x, y) = RandomLocation(w, h);
            // player.transform.position = new Vector3(x, 5, y);

            // randomize enemy spawn position (not close to player)
            // float ex = x;
            // float ey = y;
            // foreach (var e in enemies)
            // {
            //     while ((ex >= x - minDistanceToEnemy && ex <= x + minDistanceToEnemy) && (ey >= y - minDistanceToEnemy && ey <= y + minDistanceToEnemy)) 
            //     {
            //         (ex, ey) = RandomLocation(w, h);
            //     }
            //     e.transform.position = new Vector3(ex, 5, ey);
            // }
        }

        private void DebugLogReward(float reward, string rewardtype)
        {
            string rewardOutput = "<b>Reward:</b> ";
            if (reward < 0) rewardOutput = "<color=red>";
            if (reward > 0) rewardOutput = "<color=green>";
            rewardOutput += rewardtype + " <b>" + reward.ToString() + "</b></color>, STEP: " + StepCount.ToString() + " Cumulative: " + GetCumulativeReward().ToString();
            Debug.unityLogger.Log(LOGTAG, rewardOutput);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // base.CollectObservations(sensor);

            // self health
            var healthComponent = GetComponent<Unity.FPS.Game.Health>();
            float normalizedHealth = healthComponent.GetRatio();
            sensor.AddObservation(normalizedHealth);

            if (normalizedHealth != trackedHealth)
            {
                float reward = normalizedHealth - trackedHealth;
                AddReward(reward);
                playerHit = reward;
                trackedHealth = normalizedHealth;
                DebugLogReward(reward, "Agent Health");
            }

            // self ammo
			weaponAmmoRatio = 0f;
			if (weaponController)
			{
				weaponAmmoRatio = weaponController.CurrentAmmoRatio;
			}
			sensor.AddObservation(weaponAmmoRatio);

            // self crouching, jumping, running (one-hot)
			enemyAngles.Clear();
			debugEnemyBufferSensorObs.Clear();
			debugPickupBufferSensorObs.Clear();

            for (int i=0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy) continue;

                // distance to enemy
                float distance = Vector3.Distance(enemy.transform.position, transform.position);
				float normalizedDistance = Mathf.Clamp(distance / 150f, -1f, 1f);

                // angle to enemy
                float relativeBearing = Vector3.SignedAngle(transform.forward, (enemy.transform.position - transform.position), Vector3.up);
                float normalizedRelativeBearing = Mathf.Clamp(relativeBearing / 180f, -1f, 1f);

                // angle enemy is pointing
                float heading = Vector3.SignedAngle(enemy.transform.forward, (transform.position - enemy.transform.position), Vector3.up);
                float normalizedHeading = Mathf.Clamp(heading / 180f, -1f, 1f);

                // enemy health
                var enemyHealthComponent = enemy.GetComponent<Unity.FPS.Game.Health>();
                float enemyNormalizedHealth = enemyHealthComponent.GetRatio();

                if (enemyNormalizedHealth < trackedEnemyHealth[i])
                {
                    float reward = (trackedEnemyHealth[i] - enemyNormalizedHealth);
                    AddReward(reward);
                    enemyHit = reward;
                    trackedEnemyHealth[i] = enemyNormalizedHealth;
                    DebugLogReward(reward, "HIT ENEMY");
                }

                // do we have line-of-sight to enemy?  see note below to clear up misunderstanding of code
				// NOTE: the example code below merely checks if the agent has obstacles between itself and the current enemy[i], regardless of which way agent is facing
				// i.e. you could be facing away from the enemy and if there are no rocks/trees between agent and enemy, result is true
				// (agent could be facing toward the enemy, but rocks between them, returns false)
				// the intent is just to know if the agent is moving itself to a good position to have a chance at a clear shot,
				// and hopefully avoid shooting at enemies which may be nearby but behind a wall, etc
				// but no idea if this is a useful observation to make.  Might be better to actually take into account the direction the agent is facing
				// this is left up to the student to do
                RaycastHit hit;
                bool los = false;
				const float raycastDistance = 100.0f;
				Vector3 startpt = new Vector3(transform.position.x, transform.position.y + 1.36f, transform.position.z);  // slight elevation over the ground (based on the enemy bot height)
                if (Physics.SphereCast(startpt, 0.5f, (enemy.transform.position - transform.position), out hit, raycastDistance))
                {
                    // if raycast hit is hitting the enemy gameobject, LOS = true
                    // otherwise it's hitting an obstacle or a different enemy and is false
                    if (hit.transform.gameObject == enemy)
					{
						los = true;
						// Debug.DrawRay(startpt, (enemy.transform.position - transform.position), Color.green);
					}
					// else
					// {
					// 	Debug.DrawRay(startpt, (enemy.transform.position - transform.position), Color.red);
					// }
                }
				float normalizedLos = los ? 1.0f : 0.0f;

				if (enemyNormalizedHealth <= 0.0f)
				{
					// if they're dead, zero out observations before saving/sending them below
					normalizedDistance = 0f;
					normalizedRelativeBearing = 0f;
					normalizedHeading = 0f;
					enemyNormalizedHealth = 0f;
					normalizedLos = 0f;
				}
				else
				{
					// if not dead, add their angle to the list of enemy angles to use later -- we're only adding live enemies angles here
					enemyAngles.Add(Mathf.Abs(normalizedRelativeBearing));
				}

				float[] enemyObs = { normalizedDistance, normalizedRelativeBearing, normalizedHeading, enemyNormalizedHealth, normalizedLos };
				enemyBufferSensor.AppendObservation(enemyObs);
				// since there's no easy way to query buffer sensor for obs, we need to manually track below, for debug display / testing
				debugEnemyBufferSensorObs.Add(string.Format("Enemy[{0}]\nDistance: {1}\nRelBearing: {2}\nHeading: {3}\nHealth: {4}\nLOS: {5}",
					i, normalizedDistance, normalizedRelativeBearing, normalizedHeading, enemyNormalizedHealth, normalizedLos));

				// pants on fire reward
				AddReward(-1.0f / MaxStep);
            }

			// if (pickupBufferSensor != null)
			// {
				for (int i=0; i < pickups.Count; i++)
				{
					var pickup = pickups[i];
					if (pickup && pickup.activeSelf)
					{
						// distance to pickup
						float distance = Vector3.Distance(pickup.transform.position, transform.position);
						float normalizedDistance = Mathf.Clamp(distance / 150f, -1f, 1f);

						// angle to pickup
						float relativeBearing = Vector3.SignedAngle(transform.forward, (pickup.transform.position - transform.position), Vector3.up);
						float normalizedRelativeBearing = Mathf.Clamp(relativeBearing / 180f, -1f, 1f);

						float[] pickupObs = { normalizedDistance, normalizedRelativeBearing };
						pickupBufferSensor.AppendObservation(pickupObs);

						debugPickupBufferSensorObs.Add(string.Format("Pickup[{0}]\nDistance: {1}\nRelBearing: {2}",
							i, normalizedDistance, normalizedRelativeBearing));
					}
					else
					{
						float[] emptyObs = { 0f, 0f };
						pickupBufferSensor.AppendObservation(emptyObs);

						debugPickupBufferSensorObs.Add(string.Format("Pickup[{0}]\nDistance: {1}\nRelBearing: {2}", i, 0f, 0f));
					}
				}
			// }
        }
		
		protected override string GetEnemyBufferSensorObservations()
		{
			return string.Join("\n", debugEnemyBufferSensorObs);
		}

		protected override string GetPickupBufferSensorObservations()
		{
			return string.Join("\n", debugPickupBufferSensorObs);
		}

        public override void OnActionReceived(ActionBuffers actions)
        {
            base.OnActionReceived(actions);

            // var continuousActions = actions.ContinuousActions;
            var discreteActions = actions.DiscreteActions;
            ForwardInput = discreteActions[0];
            SidewaysInput = discreteActions[1];
            GunInput = discreteActions[2];
            LookHorizontal = discreteActions[3];
            LookVertical = discreteActions[4];
            // LookVertical = 0f;

			// reward shaping testing: reward based on angle to enemy
			const float enemy_angle_reward_threshold = 0.1f;  // reward agent if ANY enemies angle is < enemy_angle_reward, agent pointing directly at an enemy and shooting
			const float enemy_angle_penalty_threshold = 0.5f;  // penalize agent if ALL enemy angles are > enemy_angle_penalty, agent is shooting away from all enemies not even close
			const float enemy_angle_reward = 0.01f;
			const float enemy_angle_penalty = -0.01f;
			// NOTE: this gets called 5 times per step by default,
			// this is because the agent default DecisionRequester only requests decisions every 5 frames,
			// and it re-uses the same actions for those 5 frames
			// since this is called multiple times, the reward below is also processed multiple times.
			// so scale the reward accordingly
			if (discreteActions[2] == 1)
			{
				// if enemy action is shooting and has ammo to actually fire
				if (enemyAngles.All(angle => angle > enemy_angle_penalty_threshold))
				{
					AddReward(enemy_angle_penalty);
					// Debug.unityLogger.Log(LOGTAG, "Agent is shooting! Got penalized due to facing wrong direction!!");
				}
				// if (enemyAngles.Any(angle => angle < enemy_angle_reward_threshold)) AddReward(enemy_angle_reward);
			}
        }

        // public override void Heuristic(in ActionBuffers actionsOut)
        // {
        //     handle everything randomly

        //     var continuousActionsOut = actionsOut.ContinuousActions;
        //     var discreteActionsOut = actionsOut.DiscreteActions;

        //     continuousActionsOut[0] = Random.Range(-0.1f, 0.1f);
        //     continuousActionsOut[1] = 0f;
        //     LookHorizontal = continuousActionsOut[0];
        //     LookVertical = continuousActionsOut[1];

        //     Vector3 movementInput = new Vector3(Random.Range(-1.0f, 1.0f), 0f, Random.Range(-1.0f, 1.0f));
        //     discreteActionsOut[0] = 0; // default nothing
        //     if (movementInput.z > 0) discreteActionsOut[0] = 1;  // forward
        //     else if (movementInput.z < 0) discreteActionsOut[0] = 2;  // backward
        //     ForwardInput = discreteActionsOut[0];
        //     discreteActionsOut[1] = 0;  // default nothing
        //     if (movementInput.x < 0) discreteActionsOut[1] = 1;  // left
        //     else if (movementInput.x > 0) discreteActionsOut[1] = 2;  // right
        //     SidewaysInput = discreteActionsOut[1];

        //     discreteActionsOut[2] = Random.Range(0, 2);
        //     GunInput = discreteActionsOut[2];
        // }

        public (float, float) RandomLocation(float w, float h)
        {
            float randomNumberX = Random.Range(-0.5f * w, 0.5f * w);
            float randomNumberY = Random.Range(-0.5f * h, 0.5f * h);
            
            // TODO: exclude enemy locs

            return (randomNumberX, randomNumberY);
        }
    }
}
