using UnityEngine;
using System.Collections;

/*
 * A "spaceObject" is anything that occupies space in the world. These include Ship, Asteroids,
 * The projectiles from weapons (but not lasers). These all have a required set of variables and functions.
 * 
 * SpaceObjects will have a trigger used to represent their hitbox, a positonal and rotational velocity vector,
 * functions that update the position and rotation of the object and a gameObject that points to their center of gravity.
 */
public class SpaceObject : MonoBehaviour {
    
    /* The center of mass of the object. This is where the object will rotate around */
    public GameObject centerOfMass;

    /* Object's positiontal velocity */
    [HideInInspector]
    public Vector3 positionalVelocity = new Vector3(0, 0, 0);

    /* Rotational velocity. X = pitch, Y = yaw, Z = Roll */
    [HideInInspector]
    public Vector3 rotationalVelocity = new Vector3(0, 0, 0);
   

    /* ----------- Update Functions ------------------------------------------------------------------ */

    public virtual void Update() {
        /*
         * Each space object must handle it's own update function
         */
    }

    public virtual void ApplyPositionalVelocity() {
        /*
         * Apply the object's current positional velocity to itself
         */
    }

    public virtual void ApplyRotationalVelocity() {
        /*
         * Apply the object's current rotational velocity to itself
         */
    }

    public virtual void ApplyRotation(float rotationAmount, Vector3 rotationAxis) {
        /*
         * A helper function for ApplyRotationalVelocity to be used for each rotation axis
         */
    }


    /* ----------- Collision Functions ------------------------------------------------------------------ */

    public virtual void HitByLaser(LaserGun laserGun) {
        /*
         * What to do when this object is hit by a laser
         */
    }
}
