using UnityEngine;
using System.Collections;

/*
 * A "spaceObject" is anything that occupies space in the world. These include Ship, Asteroids,
 * The projectiles from weapons (but not lasers).
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

    public void UpdatePosition() {
        /*
         * Apply the object's current positional velocity to itself
         */
    }

    public void UpdateRotation() {
        /*
         * Apply the object's current rotational velocity to itself
         */
    }
}
