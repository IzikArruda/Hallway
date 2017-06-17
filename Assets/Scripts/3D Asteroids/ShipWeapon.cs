using UnityEngine;
using System.Collections;

/*
 * An interface for any weapon that can be fired using a ShipWeaponSystem
 */
public class ShipWeapon : MonoBehaviour {

    public float maxCooldown;
    public float currentCooldown;

    public virtual void FireWeaponRequest() {
        /*
         * Runs whenever the weapon recieves a request to fire
         */
        Debug.Log("WARNING: WEAPON DOES NOT HAVE A REQUEST FIRE METHOD");
    }

    public virtual void SuccessfulFire() {
        /*
         * Runs whenver a fireWeaponRequest is accepted to fire the weapon
         */
        Debug.Log("WARNING: WEAPON DOES NOT HAVE A SUCCESSFUL FIRE METHOD");
    }

    public virtual void ReduceCooldown(float time) {
        /*
         * The given float is how much time is reduced from the cooldown
         */
        Debug.Log("WARNING: WEAPON DOES NOT HAVE A PROPER COOLDOWN METHOD");
    }
}
