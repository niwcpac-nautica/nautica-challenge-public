using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.FPS.Game;


namespace Nautica {
	/// <summary>
	/// This class is used to reset an entity (agent, enemy or pickup) when training episode resets.
	/// Instead of having a monolithic manager track and reset everything,
	/// we will attach to an anchor gameobject and point to the entity we want to reset.
	/// Target gameobject entity will be reset to the position and rotation of the anchor.
	/// Attach script to the anchor instead of the entity because some entities may be disabled when killed,
	/// we can keep the anchors always enabled and not worry about it.
	/// Resets when MLAgents Academy OnEnvironmentReset event triggers
	/// </summary>
	public class ResetAnchor : MonoBehaviour
	{
		public GameObject entity;
		private TrainingLevelManager trainingLevelManager;
		private const string LOGTAG = nameof(ResetAnchor);

		// setup to listen for OnEnvironmentReset event
		void Awake()
		{
			// Academy.Instance.OnEnvironmentReset += ResetEntity;
			trainingLevelManager = GetComponentInParent<TrainingLevelManager>();
			if (trainingLevelManager)
			{
				trainingLevelManager.OnEpisodeReset += ResetEntity;
			}
		}

		void OnDestroy()
		{
			// Academy.Instance.OnEnvironmentReset -= ResetEntity;
			if (trainingLevelManager)
			{
				trainingLevelManager.OnEpisodeReset -= ResetEntity;
			}
		}

		/// <summary>
		/// callback for when the OnEnvironmentReset event triggers
		/// </summary>
		public void ResetEntity()
		{
			if (!entity) return;
			entity.SetActive(true);
			ResetHealth(entity);
			ResetPosition(entity);
			ResetRotation(entity);
			ResetSpecificStuff(entity);
		}

		/// <summary>
		/// subclass ResetAnchor and override this to use with entities with specific concerns
		/// </summary>
		/// <param name="target">the entity we're resetting</param>
		protected virtual void ResetSpecificStuff(GameObject target)
		{
		}

		void ResetHealth(GameObject target)
		{
			if (!target) return;
			Health health = target.GetComponent<Health>();
			if (health)
			{
				health.Revive();
			}
		}

		void ResetPosition(GameObject target)
		{
			if (!target) return;
            var navmeshAgent = target.GetComponent<NavMeshAgent>();
            if (navmeshAgent)
            {
                navmeshAgent.Warp(transform.position);
            }
            else
            {
                target.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                var characterController = target.GetComponent<CharacterController>();
                if (characterController)
                {
                    Physics.SyncTransforms();
                }
            }

            // if rigidbody exists, may have physics operating, need to zero out velocities
            var rb = target.gameObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
		}

		void ResetRotation(GameObject target)
		{
			if (!target) return;
			target.transform.rotation = transform.rotation;
		}
	}
}
