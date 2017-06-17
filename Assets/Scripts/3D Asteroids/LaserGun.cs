using UnityEngine;
using System.Collections;

/*
 * A basic laser gun that fires a laser every few seconds
 */
public class LaserGun : ShipWeapon {


    override public void FireWeaponRequest() {
        /*
         * Check the cooldowns of the gun to see if it can fire
         */

        if(currentCooldown <= 0) {
            SuccessfulFire();
        }
    }

    public override void SuccessfulFire() {
        /*
         * Fire the weapon and reset it's cooldown
         */
         
        Debug.Log("Fire laser");
        currentCooldown = maxCooldown;
    }

    public override void ReduceCooldown(float time) {
        /*
         * Reduce the current cooldown
         */

        currentCooldown -= time;
        if(currentCooldown < 0) {
            currentCooldown = 0;
        }
    }
}
