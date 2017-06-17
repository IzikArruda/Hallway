using UnityEngine;
using System.Collections;

/*
 * Recieves requests from the ShipObject to fire the Weapon objects linked to this script.
 * 
 * Handles any functions that need to be used on all the linked weapons, such as reducing cooldowns
 */
public class ShipWeaponSystem : MonoBehaviour {

    /* All weapons linked to this weapon system */
    public ShipWeapon[] linkedWeapons;
	

    public void Update() {
        /*
         * Reduce the cooldowns on each linked weapon
         */

        /* Reduce the weapon's cooldowns */
        for(int i = 0; i < linkedWeapons.Length; i++) {
            linkedWeapons[i].ReduceCooldown(Time.deltaTime);
        }
    }
	
    public void Fire() {
        /*
         * Send a request to fire the linked weapons
         */

        for(int i = 0; i < linkedWeapons.Length; i++) {
            linkedWeapons[i].FireWeaponRequest();
        }
    }
}
