using UnityEngine;
using System.Collections;

/*
 * Door specific scripts that call to their DoorController, such as the player position relative to the door.
 */
public class Door : Interactable {

    public DoorController linkedController;
	

    public override void Activated(CustomPlayerController player) {
        /* 
         * When activated by the player, the door will check what side the player is on and send a signal
         * to it's door controller to open all linked doors away from the player, if they need to be opened.
         */
        Vector3 doorFacing = transform.TransformDirection(Vector3.right);
        Plane doorPlane = new Plane(doorFacing, transform.position);
        int side = -1;

        if(doorPlane.GetSide(player.transform.position)) {
            side *= -1;
        }

        linkedController.Activated(side);
    }
}
