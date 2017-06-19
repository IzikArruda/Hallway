using UnityEngine;
using System.Collections;

public class AsteroidObject : SpaceObject {


    public void Start() {
        rotationalVelocity = new Vector3(0, 0, 0);
        positionalVelocity = new Vector3(0, 0, 0);
    }


    /* ----------- Update Functions ------------------------------------------------------------------ */

    public override void Update() {
        /*
         * On each frame, an asteroid will update it's position and rotation using it's velocity
         */

        ApplyPositionalVelocity();
        ApplyRotationalVelocity();
    }

    public override void ApplyPositionalVelocity() {
        /*
         * Use the saved positional velocity to move the asteroid
         */

        /* Move the asteroid */
        transform.position += positionalVelocity;

    }

    public override void ApplyRotationalVelocity() {
        /*
         * Apply the asteroid's rotation velocity 
         */

        /* Apply the pitch rotation */
        ApplyRotation(rotationalVelocity.x, new Vector3(1, 0, 0));

        /* Apply the yaw rotation */
        ApplyRotation(rotationalVelocity.y, new Vector3(0, 1, 0));

        /* Apply the roll rotation */
        ApplyRotation(rotationalVelocity.z, new Vector3(0, 0, 1));
    }

    public override void ApplyRotation(float rotationAmount, Vector3 rotationAxis) {
        /*
         * Rotate the asteroid in the given axis by the given amount
         */        
        
        /* Adjust the rotationAxis so it is relative to the asteroid's current rotation */
        rotationAxis = transform.rotation*rotationAxis;

        /* Apply the rotation to the asteroid */
        transform.RotateAround(centerOfMass.transform.position, rotationAxis, rotationAmount);
    }


    /* ----------- Collision Functions ------------------------------------------------------------------ */

    public override void HitByLaser(LaserGun laserGun) {
        /*
         * React to the asteroid being shot by a laser
         */

        //Debug.Log("Asteroid shot by laser");
    }
}
