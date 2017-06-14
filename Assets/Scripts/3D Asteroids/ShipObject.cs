﻿using UnityEngine;
using System.Collections;

/*
 * A ship that can move and be controlled in space. All it's functions focus on how it's 
 * mechanics function, such as rotation, firing shots or collisions.
 */
public class ShipObject : MonoBehaviour {

    /* The center of mass of the ship. The point that the ship rotates around */
    public GameObject centerOfMass;

    /* The controls of this ship */
    public ShipControls shipControls;

    /* Stats that define how the ship controls */
    public float thrusterPower = 1;
    public float pitchRotationSpeed = 1;
    public float yawRotationSpeed = 1;

    /* The current rotation of the ship */
    [HideInInspector]
    public Quaternion rotation;

    /* Ship's velocity */
    [HideInInspector]
    public Vector3 velocity = new Vector3(0, 0, 0);
    public float maxSpeed;

    /* Particle system used to represent the ship's velocity vector */
    public ParticleSystem velocityVectorParticles;


    /* ------------ Built-in Unity Functions ------------------------------------------------------------ */

    void Start() {

        /* Set the startign rotation of the ship */
        rotation = transform.rotation;

        /* Start the ship off by adding forward momentum */
        //IncreaseForwardVelocity(0.1f);
        //IncreaseVelocity(new Vector3(0.01f, 0, 0));
    }

    void Update() {
        /*
         * Handle everything a ship needs to do on each frame
         */

        /* Move the ship and it's contents by it's current velocity */
        UpdatePosition();

        /* Properly place the vector dust particle system to reflect the ship's velocity */
        UpdateVelocityDust();
    }



    /* ----------- Update Functions ------------------------------------------------------------------ */

    public void UpdatePosition() {
        /*
         * Use the ship's velocity movement to move the ship and it's contents in the world
         */

        /* Move the ship */
        transform.position += velocity;

        /* Move the player and their saved camera positions */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipMove(velocity);
    }

    public void UpdateVelocityDust() {
        /*
         * Update the ship's dust velocity particle system to reflect the direction and magnitude of the ship's velocity
         */
        float minDustSpeedLimit = maxSpeed / 4f;

        /* Set the speed of the particles. The particle speed is based on the ship's speed */
        float particleBaseSpeed = 800*velocity.magnitude;
        velocityVectorParticles.startSpeed = -particleBaseSpeed;
        

        /* Set the emission rate for the velocity dust particles */
        var particleEmission = velocityVectorParticles.emission;
        if(velocity.magnitude < minDustSpeedLimit) {
            /* If the ship travels too slowly, stop producing particles */
            particleEmission.rate = 0;
        }
        else {
            /* The amount of particles produced is relative to the ship's speed */
            particleEmission.rate = (velocity.magnitude - minDustSpeedLimit)*particleBaseSpeed*4;
        }


        /* Place the system half the bounds length from the ship's center in the velocity's direction */
        if(velocity.magnitude != 0) {
            velocityVectorParticles.transform.rotation = Quaternion.LookRotation(velocity.normalized);
            velocityVectorParticles.transform.position = transform.position + velocity.normalized*(particleBaseSpeed*velocityVectorParticles.startLifetime)/2f;
        }
    }


    /* --------- Event Functions ------------------------------------------------------------------- */


    public void IncreaseVelocity(Vector3 velocityIncrease) {
        /*
         * Use the given direction vector to add to the ship's current velocity. The magnitude
         * of the given vector effects how much velocity will be added.
         */

        velocity += velocityIncrease*0.005f;

        /* Prevent the velocity from going above the limit */
        if(velocity.magnitude > maxSpeed) {
            velocity = velocity.normalized*maxSpeed;
        }
    }

    public void IncreaseForwardVelocity(float forwardVelocity) {
        /*
         * Increase the velocity of the ship in it's forward direction (positive Z).
         */

        IncreaseVelocity(transform.rotation*Vector3.forward*forwardVelocity);
    }

    public void IncreaseUpVelocity(float upVelocity) {
        /*
         * Increase the velocity of the ship upward relative to it's forward direction.
         */

        IncreaseVelocity(transform.rotation*Vector3.up*upVelocity);
    }

    public void IncreaseLeftVelocity(float leftVelocity) {
        /*
         * Increase the velocity of the ship towards the left vector relative to it's rotation
         */

        IncreaseVelocity(transform.rotation*Vector3.left*leftVelocity);
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
