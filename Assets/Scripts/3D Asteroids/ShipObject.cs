using UnityEngine;
using System.Collections;

public class ShipObject : MonoBehaviour {

    /* The center of mass of the ship. The point that the ship rotates around */
    public GameObject centerOfMass;

    /* The controls of this ship */
    public ShipControls shipControls;

    public float thrusterPower = 1;

    public float pitchRotationSpeed = 1;
    public float yawRotationSpeed = 1;

    /* Ship's velocity */
    public float dX = 0;
    public float dY = 0;
    public float dZ = 0;

    public Vector3 Move(float xInput, float yInput) {
        /*
         * Use the given X and Y inputs to calculate an increase in the ship's velocity.
         * Any movement applied to the ship will also be applied to the player inside.
         */
        float xVelocity = xInput*thrusterPower;
        float zVelocity = yInput*thrusterPower;
        Vector3 movementVector = new Vector3(xVelocity*Time.deltaTime, 0, zVelocity*Time.deltaTime);
        
        dX += xVelocity;
        dZ += zVelocity;
        
        /* Move the ship */
        transform.position += movementVector;

        /* Move the player and their saved camera positions */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipMove(movementVector);

        return movementVector;
    }

    public float Yaw(float xInput) {
        /*
         * Use the given inputs to calculate how much the ship will rotate along the Up vector.
         * Any Yaw rotation applied to the ship will also be applied to the player inside.
         */
        float rotationAmount = xInput*yawRotationSpeed;

        /* Rotate the ship */
        transform.RotateAround(centerOfMass.transform.position, Vector3.up, rotationAmount);

        /* Rotate the player and their saved camera position */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipYaw(centerOfMass.transform.position, rotationAmount);

        return rotationAmount;
    }

    public float Pitch(float yInput) {
        /*
         * Use the given inputs to calculate how much the ship will rotate along the Left vector
         * Any pitch applied to the ship will also be applied to the player inside.
         */
        float rotationAmount = yInput*pitchRotationSpeed;

        /* Rotate the ship */
        transform.RotateAround(centerOfMass.transform.position, Vector3.left, rotationAmount);

        /* Rotate the player and their saved camera position */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipPitch(centerOfMass.transform.position, rotationAmount);
        
        return rotationAmount;
    }
}
