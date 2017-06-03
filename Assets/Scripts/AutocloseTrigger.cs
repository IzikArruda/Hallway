using UnityEngine;
using System.Collections;

/*
 * Keep autocloseTime at maxCloseTime when the player enters and stays in the trigger,
 * and count down from maxCloseTime to 0 whenever the player leaves the trigger.
 * 
 * Link this to a door to connect to it's doorController to be able to auto close a portal.
 */
public class AutocloseTrigger : MonoBehaviour {

    public Door attachedDoor;

    public float autocloseTime = 0;
    public float maxCloseTime = 2.0f;
    public bool outsideRange = true;

    void OnTriggerEnter(Collider collision) {
        /*
         * When the player enters the trigger, reset the autoclose time and stop counting it down
         */

        if(collision.tag == "Player") {
            autocloseTime = maxCloseTime;
            outsideRange = false;
        }
    }

    void OnTriggerExit(Collider collision) {
        /*
         * When the player leaves the trigger, start counting the autoclose time down
         */

        if(collision.tag == "Player") {
            outsideRange = true;
        }
    }

    void Update() {
        /*
         * Decrement the autocloseTime value if the player is outside the door's range.
         * Update the current value of the linked door's controller's autoclose value.
         */

        if(outsideRange && autocloseTime > 0) {
            autocloseTime -= Time.deltaTime;
            if(autocloseTime < 0) {
                autocloseTime = 0;
            }
        }

        if(attachedDoor.linkedController.tempAutocloseTime < autocloseTime) {
            attachedDoor.linkedController.tempAutocloseTime = autocloseTime;
        }
    }
}
