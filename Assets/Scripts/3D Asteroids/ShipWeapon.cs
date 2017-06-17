using UnityEngine;
using System.Collections;

/*
 * An interface for any weapon that can be fired using a ShipWeaponSystem
 */
public class ShipWeapon : MonoBehaviour {

    [HideInInspector]
    public float currentCooldown;
    public float maxCooldown;

    public virtual void Update() {
        /*
         * A per-frame update function is required for each weapon to reduce cooldowns and other functions.
         */
        Debug.Log("WARNING: WEAPON DOES NOT HAVE AN UPDATE METHOD");
    }

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
