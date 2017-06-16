using UnityEngine;
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

    /* The current rotation of the ship */
    [HideInInspector]
    public Quaternion rotation;

    /* Ship's velocity */
    [HideInInspector]
    public Vector3 velocity = new Vector3(0, 0, 0);
    [HideInInspector]
    public float yawVelocity;
    [HideInInspector]
    public float pitchVelocity;
    [HideInInspector]
    public float rollVelocity;
    
    /* Particle system used to represent the ship's velocity vector */
    public ParticleSystem velocityVectorParticles;
    
    /* 
     * Stats that define how the ship controls 
     */
     /* How fast the ship accelerates */
    public float velocityPower;
    public float rotationPower;
    
    /* The player cannot add more velocity to the ship if it's at the max */
    public float maxVelocity;
    public float maxRotationSpeed;

    /* How much is removed from the velocity every frame. DragPercentage is the percentage amount of the current velocity */
    public float velocityDragSetAmount;
    public float velocityDragPercentage;
    public float rotationDragSetAmount;
    public float rotationDragPercentage;


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
        
        /* Properly rotate the ship */
        UpdateRotation();

        /* Properly place the vector dust particle system to reflect the ship's velocity */
        UpdateVelocityDust();
    }

    
    /* ----------- Update Functions ------------------------------------------------------------------ */

    public void UpdatePosition() {
        /*
         * Use the ship's velocity movement to move the ship and it's contents in the world
         */
        float velocityDragTotal;

        /* Move the ship */
        transform.position += velocity;

        /* Move the player and their saved camera positions */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipMove(velocity);

        /* Apply drag to the velocity */
        velocityDragTotal = velocityDragSetAmount + velocity.magnitude*velocityDragPercentage;
        if(velocityDragTotal < Mathf.Abs(velocity.magnitude)) {
            velocity -= velocity.normalized*velocityDragTotal;
        }
        else {
            velocity = Vector3.zero;
        }
    }

    public void UpdateRotation() {
        /*
         * Apply the rotation velocity to the ship on this frame. Reduce the rotation amount each frame.
         */
        float rotationDragTotal;
        
        /* Apply the rotation */
        UpdateYaw();
        UpdatePitch();
        
        /* Reduce the rotation velocity every frame */
        rotationDragTotal = rotationDragSetAmount*Mathf.Sign(yawVelocity) + rotationDragPercentage*yawVelocity;
        float yawReduction = yawVelocity*0.99f;
        if(rotationDragTotal < Mathf.Abs(yawVelocity)) {
            yawVelocity -= rotationDragTotal;
        }else {
            yawVelocity = 0;
        }


    }

    public void UpdateYaw() {
        /*
         * Use the given inputs to calculate how much the ship will rotate along the Up vector.
         * Any Yaw rotation applied to the ship will also be applied to the player inside.
         */
        float rotationAmount = yawVelocity;

        /* Rotate the ship */
        transform.RotateAround(centerOfMass.transform.position, Vector3.up, rotationAmount);

        /* Rotate the player and their saved camera position */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipYaw(centerOfMass.transform.position, rotationAmount);
    }

    public void UpdatePitch() {
        /*
         * Use the given inputs to calculate how much the ship will rotate along the Left vector
         * Any pitch applied to the ship will also be applied to the player inside.
         */
        float rotationAmount = pitchVelocity;

        /* Rotate the ship */
        transform.RotateAround(centerOfMass.transform.position, Vector3.left, rotationAmount);

        /* Rotate the player and their saved camera position */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipPitch(centerOfMass.transform.position, rotationAmount);
    }

    public void UpdateVelocityDust() {
        /*
         * Update the ship's dust velocity particle system to reflect the direction and magnitude of the ship's velocity.
         * If the ship is moving too slow, the particles will not be generated.
         */
        float currentSpeedToMaxRatio = velocity.magnitude / maxVelocity;
        float minDustSpeedRatio = 0.1f;
        float maxDustSpeedLimit = 800;
        int maxDustEmissionRate = 500;


        /* Set the speed of the particles. The particle speed is based on the ship's speed */
        float particleBaseSpeed = currentSpeedToMaxRatio*(maxDustSpeedLimit);
        velocityVectorParticles.startSpeed = -particleBaseSpeed;


        /* Set the emission rate for the velocity dust particles */
        var particleEmission = velocityVectorParticles.emission;
        if(currentSpeedToMaxRatio < minDustSpeedRatio) {
            /* If the ship travels too slowly, stop producing particles */
            particleEmission.rate = 0;
        }
        else {
            /* The amount of particles produced is relative to the ship's speed */
            particleEmission.rate = ((currentSpeedToMaxRatio - minDustSpeedRatio) / (1 - minDustSpeedRatio))*maxDustEmissionRate;
        }
        //Set the limit to the dust emission rate
        velocityVectorParticles.maxParticles = maxDustEmissionRate;


        /* Place the system half the bounds length from the ship's center in the velocity's direction */
        if(velocity != Vector3.zero) {
            velocityVectorParticles.transform.rotation = Quaternion.LookRotation(velocity.normalized);
            velocityVectorParticles.transform.position = transform.position + velocity.normalized*(particleBaseSpeed*velocityVectorParticles.startLifetime)/2f;
        }
    }


    /* --------- Event Functions ------------------------------------------------------------------- */








    /* ---------- Ship Controls Functions ---------------------------------------------------------- */

    public void IncreaseVelocity(Vector3 velocityIncrease) {
        /*
         * Use the given direction vector to add to the ship's current velocity. The magnitude
         * of the given vector effects how much velocity will be added.
         */

        velocity += velocityIncrease*velocityPower;

        /* Prevent the velocity from going above the limit */
        if(velocity.magnitude > maxVelocity) {
            velocity = velocity.normalized*maxVelocity;
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
    
    public void IncreaseYawVelocity(float yaw) {
        /*
         * Increase the speed of the ship's yaw rotation. Do not increase it beyond it's speed limit.
         */
         
        /* Modifiy the rotation amount relative to the ship's rotation strength */
        yaw *= rotationPower;

        /* Prevent from adding rotation velocity to push it over the maxRotationSpeed */
        yaw = LimitRotationVelocityIncrease(yaw, yawVelocity);
        
        yawVelocity += yaw;
    }
    
    public void IncreasePitchVelocity(float pitch) {
        /*
         * Increase the speed of the ship's pitch rotation
         */

        /* Modifiy the rotation amount relative to the ship's rotation strength */
        pitch *= rotationPower;

        /* Prevent from adding rotation velocity to push it over the maxRotationSpeed */
        pitch = LimitRotationVelocityIncrease(pitch, pitchVelocity);
        
        pitchVelocity += pitch;
    }

    public void IncreaseRollVelocity(float roll) {
        /*
         * Increase the speed of the ship's roll rotation
         */

        /* Modifiy the rotation amount relative to the ship's rotation strength */
        roll *= rotationPower;

        /* Prevent from adding rotation velocity to push it over the maxRotationSpeed */
        roll = LimitRotationVelocityIncrease(roll, rollVelocity);

        rollVelocity += roll;
    }

    public float LimitRotationVelocityIncrease(float addedVel, float currentVel) {
        /*
         * Limit how much velocity will be added to the given currentVelocity
         */

        /* Alter the velocity increase if it adds to the current velocity */
        if(Mathf.Sign(currentVel) == Mathf.Sign(addedVel)) {

            /* Do not add any rotation velocity if it will only push it further past it's rotation speed limit */
            if(Mathf.Abs(currentVel) > maxRotationSpeed) {
                addedVel = 0;
            }
            /* Reduce the amount of added rotation if it will make the current rotation go above it's limit */
            else if(Mathf.Abs(addedVel) + Mathf.Abs(currentVel) > maxRotationSpeed) {
                addedVel = maxRotationSpeed - Mathf.Abs(currentVel);
            }
        }

        return addedVel;
    }

    
    public void FireGuns() {
        /*
         * Generic "fire weapon" function
         */
    }
}
