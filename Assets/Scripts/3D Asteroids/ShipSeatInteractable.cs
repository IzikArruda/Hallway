using UnityEngine;
using System.Collections;

/*
 * Connects the player to the shipControls. This will allow the player to send 
 * their inputs to the shipControls.
 */
public class ShipSeatInteractable : Interactable {

    /* The ship Object that the interactable is controlling */
    public ShipObject ship;

    /* The position and angle the camera will be in when the player is interacting with the interactable */
    public Transform viewingTransform;
    /* The relative position and the angle the camera will start in when interacting with the ship */
    private Vector3 startingPosition;
    private Vector3 startingAngle;

    /* The controls that are linked to this ship seat */
    public ShipControls shipControls;

    /* linked player that is interacting with the interactable */
    public CustomPlayerController playerLink = null;

    /* A link to the player if they are present inside the ship */
    public CustomPlayerController PlayerInShip;

    
    /* -------- Built-in Unity Functions ------------------------------------------------------- */

    public void Start() {

        /* Set the starting position and angle of the viewingTransform so it can be reset when the player links themselves */
        startingPosition = viewingTransform.localPosition;
        startingAngle = viewingTransform.localEulerAngles;
    }
    

    /* -------- Inherited Interactable Functions ------------------------------------------------------- */

    public override void Activated(CustomPlayerController player) {
        /*
         * When the player interacts with the canvas, send a request to take control of the player's inputs
         */
        Debug.Log("Player activated the asteroids canvas");

        if(player.ControlOverrideRequest(this, viewingTransform, Enums.PlayerStates.Overridden)) {
            ResetViewingTransform();
            playerLink = player;
        }
    }
    
    public override void HandleInputs(UserInputs playerInputs) {
        /*
         * Take in a set of user inputs and apply them to the Asteroids game. 
         * Right click will send a request to unlock the player from the interactable.
         */

        /* Check if a right click press has succesfully unlocked the player from the interactable */
        if(playerInputs.rightMouseButtonPressed == true) {
            if(playerLink.ReturnControlRequest() == true) {
                playerLink = null;
            }
        }
        /* Send the inputs to the ship's controls  */
        else {
            shipControls.HandleInputs(playerInputs);
        }
    }
    

    /* -------- Camera Fix Functions ---------------------------------------------------------- */

    public void CameraTransformUpdated() {
        /*
         * The camera's resting transform has been updated, so apply the update to the player's camera
         */

         if(playerLink != null) {
            playerLink.RepositionCamera();
        }
    }

    public void ResetViewingTransform() {
        /*
         * Reset the viewingTransform for the camera's resting position. This  should be called each time a
         * player interacts/links themselves to this script to ensure proper camera placement.
         */

        viewingTransform.localPosition = startingPosition;
        viewingTransform.localEulerAngles = startingAngle;
    }

    
    /* -------- Player Adjusting Functions ---------------------------------------------------- */

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

    public void AdjustPlayerAfterShipRotation(Vector3 point, Vector3 rotation) {
        /*
         * Runs anytime the ship undergoes a rotation. Keeps the player
         * in their relative position in the ship when it rotates.
         */

        if(PlayerInShip != null) {
            PlayerInShip.transform.RotateAround(point, Vector3.left, rotation.x);
        }
    }
}
