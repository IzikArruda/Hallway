using UnityEngine;
using System.Collections;

/*
 * Used to control the ship object it's linked to by sending commands to run certain events,
 * such as firing a shot or changing the velocity. Uses a userInput object to determine what to run.
 */
public class ShipControls : MonoBehaviour {

    /* The shipInteractable these controls connect to */
    public ShipSeatInteractable linkedShipInteractable;

    /* The ship this script is in control of */
    public ShipObject controlledShip;

    /* How the ship will handle Move and Rotate.
     * 0: Move will go forward and back using W/S, Rotate will rotate around Up using A/D. 2D asteroids
     * 1: Move will go forward/back and side to side using WASD. Rotate around Up and Left with mouseX/mouseY */
    public int controlState = 1;
    

    public void Update() {
        
    }



    public void ConvertInputs(UserInputs inputs) {
        /*
         * Send commands to the linked ship object using user input.
         */
         
        /* Handle any user inputs that will apply a movement or rotation to the ship */
        HandleMovementInputs(inputs);
    }


    public void HandleMovementInputs(UserInputs inputs) {
        /*
         * Handle ship movement and ship rotation that will be applied to the control's linked interactable's ship object.
         * Depending on the controlState, inputs will be handled differently.
         */

        if(controlState == 0) {
            /* Apply a velocity change to the ship */
            controlledShip.IncreaseForwardVelocity(inputs.playerMovementYRaw);
            
            /* Rotate the ship */
            linkedShipInteractable.ship.Yaw(inputs.playerMovementXRaw);
        }
        else if(controlState == 1) {
            /* Apply a velocity change to the ship */
            controlledShip.IncreaseForwardVelocity(inputs.playerMovementYRaw);
            controlledShip.IncreaseLeftVelocity(inputs.playerMovementXRaw);

            /* Rotate the ship */
            linkedShipInteractable.ship.Yaw(inputs.mouseX);
            linkedShipInteractable.ship.Pitch(inputs.mouseY);
        }
    }
}
