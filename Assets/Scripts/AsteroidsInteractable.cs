using UnityEngine;
using System.Collections;

/*
 * Controls how the asteroid canvas functions when having the player interact with it.
 * 
 * When the player interacts with the canvas, take control of their inputs using controlOverride.
 */
public class AsteroidsInteractable : Interactable {

    /* The transform that the camera will be in when the player is interacting with the asteroids game */
    public Transform viewingTransform;

    /* The Asteroids game script linked to this interactable */
    public AsteroidsGame asteroidsGame;

    /* linked player that is interacting with the object */
    private PlayerController playerLink = null;

	public override void Activated(PlayerController player) {
        /*
         * When the player interacts with the canvas, send a request to take control of the player's inputs
         */
        Debug.Log("Player activated the asteroids canvas");
        
        if(player.ControlOverrideRequest(this, viewingTransform, Enums.PlayerStates.Overridden)) {
            playerLink = player;
            asteroidsGame.LinkPlayer();
        }
    }

    public override void HandleInputs(UserInputs playerInputs) {
        /*
         * Take in a set of user inputs and apply them to the Asteroids game. 
         * Right click will send a request to unlock the player from the game.
         */

        /* Check if a right click has sucessfully unlocked the player from the asteroids game */
        if(playerInputs.rightMouseButton == true && asteroidsGame.UnlinkPlayer() == true) {
            if(playerLink.ReturnControlRequest() == true) {
                playerLink = null;
            }
        /* Send the user inputs into the asteroids game for it to use */
        }else {
            asteroidsGame.ConvertInputs(playerInputs);
        }
    }
}