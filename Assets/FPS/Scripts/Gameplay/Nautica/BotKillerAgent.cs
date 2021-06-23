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
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();
        [SerializeField] private List<GameObject> pickups = new List<GameObject>();
        [SerializeField] private GameObject lvl;
        [SerializeField] private GameObject player;

        [SerializeField] private float trackedHealth;
        [SerializeField] private List<float> trackedEnemyHealth = new List<float>();
		private PlayerWeaponsManager playerWeaponsManager;
		private WeaponController weaponController;
		private List<string> debugEnemyBufferSensorObs = new List<string>();  // this is just for debug display output during testing
		private List<string> debugPickupBufferSensorObs = new List<string>();  // this is just for debug display output during testing
        private const string LOGTAG = nameof(BotKillerAgent);

        private const float minDistanceToEnemy = 30f;

        protected override void Start()
        {
            base.Start();

			playerWeaponsManager = GetComponent<PlayerWeaponsManager>();
			weaponController = playerWeaponsManager?.GetActiveWeapon();

            player = GameObject.FindGameObjectWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList<GameObject>();
            foreach (var e in enemies) trackedEnemyHealth.Add(1f);
            pickups = GameObject.FindGameObjectsWithTag("Pickup").ToList<GameObject>();
            // TODO: note that if we change scenes while training, these caches will be incorrect
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
            trackedHealth = 1f;

            // reset tracked enemy healths
            for (int i=0; i < trackedEnemyHealth.Count; i++)
            {
                trackedEnemyHealth[i] = 1f;
            }

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
                trackedHealth = normalizedHealth;
                DebugLogReward(reward, "Agent Health");
            }

            // self ammo
			float weaponAmmoRatio = 0f;
			if (weaponController)
			{
				weaponAmmoRatio = weaponController.CurrentAmmoRatio;
			}
			sensor.AddObservation(weaponAmmoRatio);

            // self crouching, jumping, running (one-hot)

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
                    trackedEnemyHealth[i] = enemyNormalizedHealth;
                    DebugLogReward(reward, "HIT ENEMY");
                }

                // do we have line-of-sight to enemy?
                RaycastHit hit;
                bool los = false;
				const float raycastDistance = 100.0f;
				const int layerMask = 1 << 14;
                if (Physics.SphereCast(transform.position, 1.0f, (enemy.transform.position - transform.position), out hit, raycastDistance, layerMask))
                {
                    // if raycast hit == enemy, LOS raycast is hitting this enemy,
                    // otherwise it's hitting an obstacle or a different enemy and is false
                    if (hit.transform.root.gameObject == enemy) los = true;
                }
				float normalizedLos = los ? 1.0f : 0.0f;

				float[] enemyObs = { normalizedDistance, normalizedRelativeBearing, normalizedHeading, enemyNormalizedHealth, normalizedLos };
				enemyBufferSensor.AppendObservation(enemyObs);
				// since there's no easy way to query buffer sensor for obs, we need to manually track below, for debug display / testing
				debugEnemyBufferSensorObs.Add(string.Format("Enemy[{0}]\nDistance: {1}\nRelBearing: {2}\nHeading: {3}\nHealth: {4}\nLOS: {5}",
					i, normalizedDistance, normalizedRelativeBearing, normalizedHeading, enemyNormalizedHealth, normalizedLos));

				// pants on fire reward
				AddReward(-1.0f / MaxStep);
            }

			if (pickupBufferSensor != null)
			{
				foreach (GameObject pickup in pickups)
				{
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
						
						debugPickupBufferSensorObs.Add(string.Format("Pickup: Distance: {0}, RelBearing: {1}",
							normalizedDistance, normalizedRelativeBearing));
					}
				}
			}
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
