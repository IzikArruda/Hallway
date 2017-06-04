using UnityEngine;
using System.Collections;

public class ShipControls : MonoBehaviour {

    /* The shipInteractable these controls connect to */
    public ShipSeatInteractable linkedShipInteractable;

    /* Global handler */
    public GlobalEventHandler globalHandler;

    /* How the ship will handle Move and Rotate.
     * 0: Move will go forward and back using W/S, Rotate will rotate around Up using A/D
     * 1: Move will go forward/back and side to side using WASD. Rotate around Up and Left with mouseX/mouseY */
    public int controlState = 1;
    

    public void Update() {
        
    }

    public void ConvertInputs(UserInputs inputs) {
        /*
         * Convert user inputs into variables that will be handled by the ship controls
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
            /* Move the ship's position */
            linkedShipInteractable.ship.Move(0, inputs.playerMovementYRaw);

            /* Rotate the ship */
            linkedShipInteractable.ship.Yaw(inputs.playerMovementXRaw);
        }
        else if(controlState == 1) {
            /* Move the ship's position */
            linkedShipInteractable.ship.Move(inputs.playerMovementXRaw, inputs.playerMovementYRaw);

            /* Rotate the ship */
            linkedShipInteractable.ship.Yaw(inputs.mouseX);
            linkedShipInteractable.ship.Pitch(inputs.mouseY);
        }
    }
}
