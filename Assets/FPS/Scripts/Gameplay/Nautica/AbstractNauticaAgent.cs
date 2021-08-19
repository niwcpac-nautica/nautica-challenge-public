using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.FPS.Gameplay;


namespace Nautica {
    [RequireComponent(typeof(PlayerCharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    /// <summary>
    /// This is the agent used to control a player in the fps game
    /// It sits between the player input handler and the player controller,
    /// this is because it maps player input to agent actions for the Heuristic;
    /// 
    /// Built-in Observations
    /// all agents receive these minimum observations and augment with visual data, grid data, etc
    ///     enemy1 position (normalized) x, y, z (0-1)
    ///     enemy1 health (normalized) 0-1
    ///     enemy1 line-of-sight (0,1)
    ///     enemy2 position
    ///     enemy2 health
    ///     enemy2 line-of-sight
    ///     friend1 position (normalized) x, y, z
    ///     friend1 health (normalized) 0-1
    ///     self position (normalized) x, y, z
    ///     self health (normalized) 0-1
    ///     self ammo (normalized) 0-1
    ///     self crouching, jumping, running (one-hot)
    ///     
    /// Action Space (continuous): 2 actions
    ///     mouselook horizontal (-1.0 to +1.0)
    ///     mouselook vertical (-1.0 to +1.0)
    /// 
    /// Action Space (discrete): [3],[3],[3]
    ///     move    0: nothing, 1: forward, 2: back
    ///     strafe  0: nothing, 1: left, 2: right
    ///     fire    0: nothing, 1: fire, 2: reload
    ///     TODO: future  one-hot:  crouching, running, jumping (0,0,0)
    /// </summary>
    public class AbstractNauticaAgent : Agent
    {
        public float ForwardInput { get; protected set; }
        public float SidewaysInput { get; protected set; }
        public float GunInput { get; protected set; }
        public float LookHorizontal { get; protected set; }
        public float LookVertical { get; protected set; }
        [SerializeField] protected bool crouching = false;
        public bool Crouching { get => crouching; }
        [SerializeField] protected bool running = false;
        public bool Running { get => running; }
        [SerializeField] protected bool jumping = false;
        public bool Jumping { get => jumping; }
        // NOTE: can do a one-hot bool[] instead?  Can make these into properties with get

        private PlayerCharacterController characterController;
        private PlayerInputHandler inputHandler;

		// adding buffer sensors to base class, since all agents would presumably need it
		// if not used, they'd just be empty and shouldn't affect anything
		// NOTE: these are handled separate from the default vector sensor
		protected BufferSensorComponent enemyBufferSensor;
		protected BufferSensorComponent pickupBufferSensor;
		private const string enemyBufferSensorName = "BotBufferSensor";
		private const string pickupBufferSensorName = "PickupBufferSensor";

        private const string LOGTAG = nameof(AbstractNauticaAgent);
        private float enemyHit = 0f;
        private float playerHit = 0f;

        protected virtual void Awake()
        {
            characterController = GetComponent<PlayerCharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();

			BufferSensorComponent[] bufferSensors = GetComponents<BufferSensorComponent>();
			foreach (var sensor in bufferSensors)
			{
				if (sensor.SensorName == enemyBufferSensorName) enemyBufferSensor = sensor;
				else if (sensor.SensorName == pickupBufferSensorName) pickupBufferSensor = sensor;
			}
			Debug.AssertFormat((enemyBufferSensor != null), "Enemy Buffer Sensor not found!");
			Debug.AssertFormat((pickupBufferSensor != null), "Pickup Buffer Sensor not found!");
        }

		protected virtual void Start()
		{
		}

        protected virtual void Update()
        {
            // cache input in Update() since Heuristic and agent code goes by FixedUpdate(), we lose frames
            crouching = inputHandler.GetCrouchInputDown();
            running = inputHandler.GetSprintInputHeld();
            jumping = inputHandler.GetJumpInputDown();
        }

        /// <summary>
        /// helper function for getting agent action in Heuristic function.
        /// This is just to convert an input axis which goes from -1 to +1 into discrete values of 0, 1, 2
        /// </summary>
        /// <param name="axisValue">raw input from an input axis (ranges from -1.0 to +1.0)</param>
        /// <param name="flipped">whether </param>
        /// <returns>int used for a discrete action where typically 0 = do nothing, 1 = left, 2 = right, etc</returns>
        private int MapInputAxisToDiscreteAction(float axisValue, bool flipped=false)
        {
            if (flipped)
            {
                if (axisValue > 0) return 1;
                if (axisValue < 0) return 2;
            }
            else
            {
                if (axisValue < 0) return 1;
                if (axisValue > 0) return 2;
            }
            return 0;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            if (!inputHandler) return;
            Vector3 movementInput = inputHandler.GetMoveInput();
            var discreteActionsOut = actionsOut.DiscreteActions;

            discreteActionsOut[0] = MapInputAxisToDiscreteAction(movementInput.z, flipped: true);
            ForwardInput = discreteActionsOut[0];

            discreteActionsOut[1] = MapInputAxisToDiscreteAction(movementInput.x);
            SidewaysInput = discreteActionsOut[1];

            discreteActionsOut[2] = 0;  // default nothing
            if (inputHandler.GetFireInputHeld()) discreteActionsOut[2] = 1;  // fire, ignore button hold for now
            else if (inputHandler.GetReloadButtonDown()) discreteActionsOut[2] = 2;  // reload
            GunInput = discreteActionsOut[2];

            discreteActionsOut[3] = MapInputAxisToDiscreteAction(inputHandler.GetLookInputsHorizontal());
            LookHorizontal = discreteActionsOut[3];

            discreteActionsOut[4] = MapInputAxisToDiscreteAction(inputHandler.GetLookInputsVertical());
            LookVertical = discreteActionsOut[4];
        }

        /// <summary>
        /// Keep to the same format as the PlayerInputHandler, so PlayerCharacterController can use it
        /// Note that the values are grabbed from PlayerInputHandler, so they are already clamped for use
        /// </summary>
        /// <returns>vector3 in the format PlayerCharacterController expects</returns>
        public Vector3 GetMoveInput()
        {
            float x = 0f;
            float z = 0f;
            if (Mathf.RoundToInt(ForwardInput) == 1) z = 1.0f;  // 1 = forward
            else if (Mathf.RoundToInt(ForwardInput) == 2) z = -1.0f;  // 2 = backward
            if (Mathf.RoundToInt(SidewaysInput) == 1) x = -1.0f;  // 1 = left
            else if (Mathf.RoundToInt(SidewaysInput) == 2) x = 1.0f;  // 2 = right
            Vector3 move = new Vector3(x, 0f, z);
            return move;
        }

        public float GetLookInputsVertical()
        {
            const float moveAmount = 0.0015f;
            switch (LookVertical)
            {
                case 0 : return 0f;
                case 1 : return -moveAmount;  // look left
                case 2 : return moveAmount;  // look right
                default : return 0f;
            }
        }

        public float GetLookInputsHorizontal()
        {
            const float moveAmount = 0.0015f;
            switch (LookHorizontal)
            {
                case 0 : return 0f;
                case 1 : return -moveAmount;  // look up
                case 2 : return moveAmount;  // look down
                default : return 0f;
            }
        }

        public bool GetReloadButtonDown()
        {
            if (Mathf.RoundToInt(GunInput) == 2) return true;
            return false;
        }

        public bool GetFireInputHeld()
        {
            if (Mathf.RoundToInt(GunInput) == 1) return true;
            return false;
        }

        public bool GetFireInputDown()
        {
            if (Mathf.RoundToInt(GunInput) == 1) return true;
            return false;
        }

        public bool GetFireInputReleased() => false;

        public bool GetAimInputHeld() => false;

		public string GetCurrentObservationsText()
		{
			string output = "";
			var observations = GetObservations();
			for (int i=0; i < observations.Count; i++)
			{
				output += "observation" + i.ToString() + ": " + observations[i].ToString() + "\n";
			}

			if (enemyBufferSensor != null)
			{
				output += GetEnemyBufferSensorObservations() + "\n\n";
			}

			if (pickupBufferSensor != null)
			{
				output += GetPickupBufferSensorObservations();
			}

			return output;
		}

		protected virtual string GetEnemyBufferSensorObservations() => "";
		protected virtual string GetPickupBufferSensorObservations() => "";

		public string GetCurrentActionsText()
		{
			string output = "";
			var discreteActions = GetStoredActionBuffers().DiscreteActions;
			for (int i=0; i < discreteActions.Length; i++)
			{
				output += "action[" + i.ToString() + "]: " + discreteActions[i].ToString() + "\n";
			}
			return output;
		}

		public string GetCurrentRewardsText()
		{
			return "Cumulative Reward Total: " + GetCumulativeReward().ToString();
		}

        public virtual float GetPlayerHitScore()
        {
            return playerHit;
        }

        public virtual float GetEnemyHitScore()
        {
            return enemyHit;
        }

        public virtual void SetPlayerHitScore(float score)
        {
            playerHit = score;
        }

        public virtual void SetEnemyHitScore(float score)
        {
            enemyHit = score;
        }
    }
}
