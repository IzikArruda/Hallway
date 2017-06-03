using UnityEngine;
using System.Collections;

public class ShipSeatInteractable : Interactable {

    /* The ship Object that the interactable is controlling */
    public ShipObject ship;

    /* The position and angle the camera will be in when the player is interacting with the interactable */
    public Transform viewingTransform;

    /* The controls that are linked to this ship seat */
    public ShipControls shipControls;

    /* linked player that is interacting with the interactable */
    public PlayerController playerLink = null;

    /* A link to the player if they are present inside the ship */
    public PlayerController PlayerInShip;

    public override void Activated(PlayerController player) {
        /*
         * When the player interacts with the canvas, send a request to take control of the player's inputs
         */
        Debug.Log("Player activated the asteroids canvas");

        if(player.ControlOverrideRequest(this, viewingTransform, Enums.PlayerStates.Overridden)) {
            playerLink = player;
        }
    }
    
    public override void HandleInputs(UserInputs playerInputs) {
        /*
         * Take in a set of user inputs and apply them to the Asteroids game. 
         * Right click will send a request to unlock the player from the interactable.
         */

        /* Check if a right click has succesfully unlocked the player from the interactable */
        if(playerInputs.rightMouseButton == true) {
            if(playerLink.ReturnControlRequest() == true) {
                playerLink = null;
            }
        }

        /* Send the inputs to the ship's controls and update the camera's viewing position */
        else {
            shipControls.ConvertInputs(playerInputs);
        }
    }
    

    public void AdjustPlayerAfterShipMove(Vector3 shipMovementVector) {
        /*
         * Run anytime the ship moves. Keeps the player's position relative to the ship.
         */

        if(PlayerInShip != null) {
            PlayerInShip.transform.position += shipMovementVector;
        }
    }

    public void AdjustPlayerAfterShipYaw(Vector3 point, float yawAmount) {
        /*
         * Run anytime the ship undergoes yaw rotation around the given point. 
         * Keeps the player in their position relative to the ship.
         */

        if(PlayerInShip != null) {
            PlayerInShip.transform.RotateAround(point, Vector3.up, yawAmount);
        }
    }

    public void AdjustPlayerAfterShipPitch(Vector3 point, float pitchAmount) {
        /*
         * Run anytime the ship undergoes pitch rotation around the given point. 
         * Keeps the player in their position relative to the ship.
         */

        if(PlayerInShip != null) {
            PlayerInShip.transform.RotateAround(point, Vector3.left, pitchAmount);
        }
    }
}
