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


    /* -------- Input Handling Functions ------------------------------------------------------- */
    
    public void HandleInputs(UserInputs inputs) {
        /*
         * Send commands to the linked ship object using user input.
         * The player calls this with their current inputs for the frame.
         */

        /* Handle any user inputs that will apply a movement or rotation to the ship */
        HandleMovementInputs(inputs);

        /* Handle any inputs that will use the ship's weaponSystem */
        HandleWeaponFiring(inputs);
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
            linkedShipInteractable.ship.IncreaseYawVelocity(inputs.playerMovementXRaw);

            /* Rotate the camera when moving the mouse */
            RotateCamera(inputs.mouseX, inputs.mouseY);
        }
        else if(controlState == 1) {
            /* Apply a velocity change to the ship */
            controlledShip.IncreaseForwardVelocity(inputs.playerMovementYRaw);
            controlledShip.IncreaseLeftVelocity(inputs.playerMovementXRaw);

            /* Rotate the ship */
            linkedShipInteractable.ship.IncreaseYawVelocity(inputs.mouseX);
            linkedShipInteractable.ship.IncreasePitchVelocity(inputs.mouseY);
        }
    }


    public void HandleWeaponFiring(UserInputs inputs) {
        /*
         * Fire a shot if the player is pressing the left-click
         */

        if(inputs.leftMouseButtonHeld) {
            controlledShip.FireGuns();
        }
    }

    /* -------- Outside Event functions ------------------------------------------------------- */

    public void RotateCamera(float x, float y) {
        /*
         * Rotate the camera's resting transform by the given amount. Prevent the transform from flipping upside down.
         */
        Vector3 newViewingAngle = linkedShipInteractable.viewingTransform.localEulerAngles;
        float xRotAngleMax = 60;

        /* Add the mouse movement to the viewing angle and clamp the x angle to be within [0+xRotAngleMax, 360-xRotAngleMax]*/
        newViewingAngle += new Vector3(-y, x, 0);
        if(newViewingAngle.x <= 180 && newViewingAngle.x > xRotAngleMax) {
            newViewingAngle.x = xRotAngleMax;
        }else if(newViewingAngle.x > 180 && newViewingAngle.x < 360-xRotAngleMax) {
            newViewingAngle.x = 360-xRotAngleMax;
        }

        /* The viewing angle should never have a z value thats not 0 */
        newViewingAngle.z = 0;

        /* Set the new viewing angle */
        linkedShipInteractable.viewingTransform.localEulerAngles = newViewingAngle;

        /* Update the PlayerController to reflect the change in the transform */
        linkedShipInteractable.CameraTransformUpdated();
    }
}
