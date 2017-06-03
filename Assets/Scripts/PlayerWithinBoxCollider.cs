using UnityEngine;
using System.Collections;

/*
 * Determines if the player is touching the linked box collider.
 */
public class PlayerWithinBoxCollider : MonoBehaviour {

    public BoxCollider linkedBoxCollider;
    public bool playerTouching;

    void OnTriggerEnter(Collider collider) {
        /* 
         * Check if the player has entered the trigger
         */

        playerTouching = true;
    }

    void OnTriggerExit(Collider collider) {
        /*
         * Check if the player has left the trigger
         */

        playerTouching = false;
    }
}
