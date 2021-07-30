using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.AI;


namespace Nautica {
	/// <summary>
	/// reset anchor with specifics for enemy bots
	/// </summary>
	public class BotResetAnchor : ResetAnchor
	{
		private const string LOGTAG = nameof(BotResetAnchor);

		/// <summary>
		/// subclass ResetAnchor and override this to use with entities with specific concerns
		/// </summary>
		/// <param name="target">the entity we're resetting</param>
		protected override void ResetSpecificStuff(GameObject target)
		{
			if (!target) return;
			var detectionModule = target.GetComponentInChildren<DetectionModule>();
			if (detectionModule)
			{
				detectionModule.Reset();
			}

			var enemyMobile = target.GetComponent<EnemyMobile>();
			if (enemyMobile)
			{
				enemyMobile.Reset();
			}
		}
	}
}
