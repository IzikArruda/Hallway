using UnityEngine;
using System.Collections;

/*
 * An interface for any interactable objects that gives prototypes for the required scripts
 */
public class Interactable : MonoBehaviour {
    

    public virtual void Activated(PlayerController player) {
        /*
         * Runs when the player activates an object
         */

        Debug.Log("ITEM DOES NOT HAVE A PLAYER ACTIVATED CONDITION");
    }

    public virtual void HandleInputs(UserInputs playerInputs) {
        /*
         * Take in the inputs from the user and control how it is handled
         */
    }
}
