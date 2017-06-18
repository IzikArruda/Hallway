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

    /* Ship's positiontal velocity */
    [HideInInspector]
    public Vector3 positionalVelocity = new Vector3(0, 0, 0);

    /* Rotational velocity. X = pitch, Y = yaw, Z = Roll */
    [HideInInspector]
    public Vector3 rotationalVelocity = new Vector3(0, 0, 0);

    /* Particle system used to represent the ship's velocity vector */
    public ParticleSystem velocityVectorParticles;
    
    /* Weapon system and it's linked  weapons */
    public ShipWeaponSystem weaponSystem;

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
    }

    void Update() {
        /*
         * Handle everything a ship needs to do on each frame
         */

        /* Move the ship and it's contents by it's current velocity */
        ApplyPositionalVelocity();

        /* Properly rotate the ship */
        ApplyRotationalVelocity();

        /* Properly place the vector dust particle system to reflect the ship's velocity */
        UpdateVelocityDust();
    }

    
    /* ----------- Update Functions ------------------------------------------------------------------ */

    public void ApplyPositionalVelocity() {
        /*
         * Use the ship's velocity movement to move the ship and it's contents in the world
         */
        float velocityDragTotal;

        /* Move the ship */
        transform.position += positionalVelocity;

        /* Move the player and their saved camera positions */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipMove(positionalVelocity);

        /* Apply drag to the velocity */
        velocityDragTotal = velocityDragSetAmount + positionalVelocity.magnitude*velocityDragPercentage;
        if(velocityDragTotal < Mathf.Abs(positionalVelocity.magnitude)) {
            positionalVelocity -= positionalVelocity.normalized*velocityDragTotal;
        }
        else {
            positionalVelocity = Vector3.zero;
        }
    }

    public void ApplyRotationalVelocity() {
        /*
         * Apply the rotation velocity to the ship on this frame. Reduce the rotation amount each frame.
         */
        float rotationDragTotal;

        /* Apply the pitch rotation */
        ApplyRotation(rotationalVelocity.x, new Vector3(1, 0, 0));

        /* Apply the yaw rotation */
        ApplyRotation(rotationalVelocity.y, new Vector3(0, 1, 0));
        
        /* Apply the roll rotation */
        ApplyRotation(rotationalVelocity.z, new Vector3(0, 0, 1));



        /* Reduce the pitch rotational velocity */
        rotationDragTotal = rotationDragSetAmount*Mathf.Sign(rotationalVelocity.x) + rotationDragPercentage*rotationalVelocity.x;
        if(rotationDragTotal < Mathf.Abs(rotationalVelocity.x)) {
            rotationalVelocity.x -= rotationDragTotal;
        }
        else {
            rotationalVelocity.x = 0;
        }

        /* Reduce the yaw rotational velocity */
        rotationDragTotal = rotationDragSetAmount*Mathf.Sign(rotationalVelocity.y) + rotationDragPercentage*rotationalVelocity.y;
        if(rotationDragTotal < Mathf.Abs(rotationalVelocity.y)) {
            rotationalVelocity.y -= rotationDragTotal;
        }
        else {
            rotationalVelocity.y = 0;
        }

        /* Reduce the roll rotational velocity */
        rotationDragTotal = rotationDragSetAmount*Mathf.Sign(rotationalVelocity.z) + rotationDragPercentage*rotationalVelocity.z;
        if(rotationDragTotal < Mathf.Abs(rotationalVelocity.z)) {
            rotationalVelocity.z -= rotationDragTotal;
        }
        else {
            rotationalVelocity.z = 0;
        }
    }

    public void ApplyRotation(float rotationAmount, Vector3 rotationAxis) {
        /*
         * Use the given amount and axis to rotate the ship. Any rotation will also be
         * applied to the player inside the ship.
         */

        /* Adjust the rotationAxis so it is relative to the ship's current rotation */
        rotationAxis = transform.rotation*rotationAxis;
        
        /* Apply the rotation to the ship */
        transform.RotateAround(centerOfMass.transform.position, rotationAxis, rotationAmount);

        /* Rotate the player and their saved camera position */
        shipControls.linkedShipInteractable.AdjustPlayerAfterShipRotation(centerOfMass.transform.position, rotationAxis, rotationAmount);

    }

    public void UpdateVelocityDust() {
        /*
         * Update the ship's dust velocity particle system to reflect the direction and magnitude of the ship's velocity.
         * If the ship is moving too slow, the particles will not be generated.
         */
        float currentSpeedToMaxRatio = positionalVelocity.magnitude / maxVelocity;
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
        if(positionalVelocity != Vector3.zero) {
            velocityVectorParticles.transform.rotation = Quaternion.LookRotation(positionalVelocity.normalized);
            velocityVectorParticles.transform.position = transform.position + positionalVelocity.normalized*(particleBaseSpeed*velocityVectorParticles.startLifetime)/2f;
        }
    }


    /* ---------- Ship Controls Functions ---------------------------------------------------------- */

    public void IncreaseVelocity(Vector3 velocityIncrease) {
        /*
         * Use the given direction vector to add to the ship's current velocity. The magnitude
         * of the given vector effects how much velocity will be added.
         */

        positionalVelocity += velocityIncrease*velocityPower;

        /* Prevent the velocity from going above the limit */
        if(positionalVelocity.magnitude > maxVelocity) {
            positionalVelocity = positionalVelocity.normalized*maxVelocity;
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

        /* Prevent the scenario of adding enough yaw to push the rotation past it's limit */
        yaw = LimitRotationVelocityIncrease(yaw, rotationalVelocity.y);

        /* Apply the added yaw rotation to the current amount */
        rotationalVelocity.y += yaw;
    }

    public void IncreasePitchVelocity(float pitch) {
        /*
         * Increase the speed of the ship's pitch rotation
         */

        /* Modifiy the rotation amount relative to the ship's rotation strength */
        pitch *= rotationPower;

        /* Prevent the scenario of adding enough pitch to push the rotation past it's limit */
        pitch = LimitRotationVelocityIncrease(pitch, rotationalVelocity.x);

        /* Apply the added pitch rotation to the current amount */
        rotationalVelocity.x += pitch;
    }

    public void IncreaseRollVelocity(float roll) {
        /*
         * Increase the speed of the ship's roll rotation
         */

        /* Modifiy the rotation amount relative to the ship's rotation strength */
        roll *= rotationPower;

        /* Prevent the scenario of adding enough roll to push the rotation past it's limit */
        roll = LimitRotationVelocityIncrease(roll, rotationalVelocity.z);

        /* Apply the added roll rotation to the current amount */
        rotationalVelocity.z += roll;
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
         * Send a request to the ship's weapon system to fire the linked weapons
         */

        weaponSystem.Fire();
    }
}
