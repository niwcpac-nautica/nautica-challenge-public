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
		public bool debug = false; 

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
			ObserveHealth(sensor);
			ObserveAmmo(sensor);
			ObserveEnemies(sensor);
			ObservePickups(sensor);
		}

		private void ObserveHealth(VectorSensor sensor)
		{
			var healthComponent = GetComponent<Health>();
			float normalizedHealth = healthComponent.GetRatio();
			sensor.AddObservation(normalizedHealth);
			UpdateAgentTrackedHealth(normalizedHealth);
		}

		private void UpdateAgentTrackedHealth(float agentHealth)
		{
			if (agentHealth == trackedHealth) return; 

			float reward = agentHealth - trackedHealth;
			SetPlayerHitScore(reward);
			trackedHealth = agentHealth;
			RewardAgent(reward, "Agent Health");
		}

		private void RewardAgent(float reward, string message)
        {
			AddReward(reward);
			if(debug)
            {
				DebugLogReward(reward, message);
			}
		}

		private void ObserveAmmo(VectorSensor sensor)
		{
			weaponAmmoRatio = 0f;
			if (weaponController)
			{
				weaponAmmoRatio = weaponController.CurrentAmmoRatio;
			}
			sensor.AddObservation(weaponAmmoRatio);
		}

		private void ObserveEnemies(VectorSensor sensor)
		{
			// self crouching, jumping, running (one-hot)
			enemyAngles.Clear();
			debugEnemyBufferSensorObs.Clear();

			for (int i = 0; i < enemies.Count; i++)
			{
				var enemy = enemies[i];
				if (!enemy) continue;

				float[] enemyObservations = GetEnemyObservations(sensor, enemy, i);
				enemyBufferSensor.AppendObservation(enemyObservations);

				// since there's no easy way to query buffer sensor for obs, we need to manually track below, for debug display / testing
				debugEnemyBufferSensorObs.Add(string.Format("Enemy[{0}]\nDistance: {1}\nRelBearing: {2}\nHeading: {3}\nHealth: {4}\nLOS: {5}",
					i, enemyObservations[0], enemyObservations[1], enemyObservations[2], enemyObservations[3], enemyObservations[4]));

				// pants on fire reward
				AddReward(-1.0f / MaxStep);
			}
		}

		private float[] GetEnemyObservations(VectorSensor sensor, GameObject enemy, int enemyIndex)
		{
			float distance = 0f;
			float bearing = 0f;
			float heading = 0f;
			float lineOfSight = 0f;
			float health = ObserveEnemyHealth(sensor, enemy, enemyIndex);

			if (health > 0.0f)
			{
				distance = DetermineDistanceFrom(enemy);
				bearing = DetermineBearingTo(enemy);
				heading = DetermineHeadingOf(enemy);
				lineOfSight = DoesAgentHaveLineOfSightOf(enemy);
				enemyAngles.Add(Mathf.Abs(bearing));
			}

			float[] observations = { distance, bearing, heading, health, lineOfSight };
			return observations;
		}

		private float ObserveEnemyHealth(VectorSensor sensor, GameObject enemy, int enemyIndex)
		{
			var enemyHealthComponent = enemy.GetComponent<Health>();
			if (enemyHealthComponent == null) return 0f; 

			float enemyNormalizedHealth = enemyHealthComponent.GetRatio();
			UpdateEnemyTrackedHealth(enemyNormalizedHealth, enemyIndex);
			return enemyNormalizedHealth;
		}

		private void UpdateEnemyTrackedHealth(float enemyHealth, int enemyIndex)
		{
			if (enemyHealth >= trackedEnemyHealth[enemyIndex]) return; 
			
			float reward = (trackedEnemyHealth[enemyIndex] - enemyHealth);
			SetEnemyHitScore(reward);
			trackedEnemyHealth[enemyIndex] = enemyHealth;
			RewardAgent(reward, "HIT ENEMY");
		}

		private float DoesAgentHaveLineOfSightOf(GameObject enemy)
		{
			// NOTE: the example code below merely checks if the agent has obstacles between itself and the current enemy[i], regardless of which way agent is facing
			// i.e. you could be facing away from the enemy and if there are no rocks/trees between agent and enemy, result is true
			// (agent could be facing toward the enemy, but rocks between them, returns false)
			// the intent is just to know if the agent is moving itself to a good position to have a chance at a clear shot,
			// and hopefully avoid shooting at enemies which may be nearby but behind a wall, etc
			// but no idea if this is a useful observation to make.  Might be better to actually take into account the direction the agent is facing
			// this is left up to the student to do
			return AreObstaclesBetweenAgentAnd(enemy);
		}

		private float AreObstaclesBetweenAgentAnd(GameObject enemy)
		{
			RaycastHit hit;
			bool obstaclesBetweenAgentAndEnemy = false;
			const float raycastDistance = 100.0f;
			Vector3 startpt = new Vector3(transform.position.x, transform.position.y + 1.36f, transform.position.z);  // slight elevation over the ground (based on the enemy bot height)
			if (Physics.SphereCast(startpt, 0.5f, (enemy.transform.position - transform.position), out hit, raycastDistance))
			{
				// if raycast hit is hitting the enemy gameobject, LOS = true
				// otherwise it's hitting an obstacle or a different enemy and is false
				if (hit.transform.gameObject == enemy)
				{
					obstaclesBetweenAgentAndEnemy = true;
					// Debug.DrawRay(startpt, (enemy.transform.position - transform.position), Color.green);
				}
			}
			return obstaclesBetweenAgentAndEnemy ? 1.0f : 0.0f;
		}

		private void ObservePickups(VectorSensor sensor)
		{
			debugPickupBufferSensorObs.Clear();

			for (int i = 0; i < pickups.Count; i++)
			{
				var pickup = pickups[i];
				float[] pickupObservation = GetPickupLocation(pickup);
				pickupBufferSensor.AppendObservation(pickupObservation);
				debugPickupBufferSensorObs.Add(string.Format("Pickup[{0}]\nDistance: {1}\nRelBearing: {2}", i, pickupObservation[0], pickupObservation[1]));
			}
		}

		private float[] GetPickupLocation(GameObject pickup)
		{
			float distance = 0f;
			float bearing = 0f;
			if (pickup && pickup.activeSelf)
			{
				distance = DetermineDistanceFrom(pickup);
				bearing = DetermineBearingTo(pickup);
			}
			float[] location = { distance, bearing };
			return location;
		}

		private float DetermineDistanceFrom(GameObject thing)
		{
			float distance = Vector3.Distance(thing.transform.position, transform.position);
			float normalizedDistance = Mathf.Clamp(distance / 150f, -1f, 1f);
			return normalizedDistance;
		}

		private float DetermineBearingTo(GameObject thing)
		{
			//angle to enemy
			float relativeBearing = Vector3.SignedAngle(transform.forward, (thing.transform.position - transform.position), Vector3.up);
			float normalizedRelativeBearing = Mathf.Clamp(relativeBearing / 180f, -1f, 1f);
			return normalizedRelativeBearing;
		}

		private float DetermineHeadingOf(GameObject thing)
		{
			// angle enemy is pointing
			float heading = Vector3.SignedAngle(thing.transform.forward, (transform.position - thing.transform.position), Vector3.up);
			float normalizedHeading = Mathf.Clamp(heading / 180f, -1f, 1f);
			return normalizedHeading;
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

			var discreteActions = actions.DiscreteActions;
			ForwardInput = discreteActions[0];
			SidewaysInput = discreteActions[1];
			GunInput = discreteActions[2];
			LookHorizontal = discreteActions[3];
			LookVertical = discreteActions[4];

			// reward shaping testing: reward based on angle to enemy
			const float enemy_angle_penalty_threshold = 0.5f;  // penalize agent if ALL enemy angles are > enemy_angle_penalty, agent is shooting away from all enemies not even close
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
			}
		}

		public (float, float) RandomLocation(float w, float h)
		{
			float randomNumberX = Random.Range(-0.5f * w, 0.5f * w);
			float randomNumberY = Random.Range(-0.5f * h, 0.5f * h);

			// TODO: exclude enemy locations

			return (randomNumberX, randomNumberY);
		}
	}
}
