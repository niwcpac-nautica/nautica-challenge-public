using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;


namespace Nautica {
	/// <summary>
	/// reset anchor with specifics for player / agent
	/// </summary>
	public class AgentResetAnchor : ResetAnchor
	{
		private const string LOGTAG = nameof(AgentResetAnchor);

		/// <summary>
		/// subclass ResetAnchor and override this to use with entities with specific concerns
		/// </summary>
		/// <param name="target">the entity we're resetting</param>
		protected override void ResetSpecificStuff(GameObject target)
		{
			if (!target) return;

			var weaponManager = target.GetComponent<PlayerWeaponsManager>();
			if (weaponManager) weaponManager.SwitchToWeaponIndex(0);

			var currentWeapon = weaponManager.GetActiveWeapon();
			if (currentWeapon) currentWeapon.ResetAmmo();
		}
	}
}
