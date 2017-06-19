using UnityEngine;
using System.Collections;

/*
 * A basic laser gun that fires a laser every few seconds.
 * 
 * This basic laser fires a full line from it's origin to it's destination for a moment before going on cooldown.
 * This allows the shot to hit the target instantly.
 */
public class LaserGun : ShipWeapon {

    /* The laser object that will be "fired" */
    private GameObject laserBeam;

    /* The particle system that activates when the laser collides with an object */
    public ParticleSystem laserHitParticles;

    /* Power determines how much "active" the laser is, which determines it's size, length, etc */
    [HideInInspector]
    public float laserCurrentPower;
    public float laserMaxPower;

    /* How much power is put into the laser when active and reduced when not active */
    public float fireWeaponPower;
    public float dissipatePower;

    /* How long and wide the laser will be when at full power */
    public float laserMaxLength;
    public float laserMaxWidth;

    /* The current length of the laser. Lasers will stop short if they collide into an object */
    public float currentLaserLength;

    /* Whether the laser has dissipated at the end or collided into an object */
    public bool laserCollision;
    
    /* For how long power will be put into a laser once fired */
    [HideInInspector]
    public float currentActiveTime;
    public float maxActiveTime;

    /* The base damage of the laser */
    public float baseDamage;


    /* -------- Built-in Unity Functions ------------------------------------------------------- */

    public void Start() {
        /*
         * Initilize the laser beam of the gun
         */

        laserBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laserBeam.transform.parent = transform;

        ResetBeam();

    }

    public override void Update() {
        /*
         * Reduce the cooldowns on the weapon, reduce the power of the laser and update it's position/size.
         */
        float time = Time.deltaTime;

        /* Reduce the cooldowns of the weapon */
        ReduceCooldown(time);
        
        /* Update the power of the laser using currentActiveTime */
        UpdateLaserPower(time);

        /* Check if the laser runs into anything */
        CheckForCollisions();

        /* Update the size of the laser to reflect any changes */
        UpdateLaserSize();



    }


    /* -------- Inherited Weapon Functions ------------------------------------------------------- */

    public override void FireWeaponRequest() {
        /*
         * Check the cooldowns of the gun to see if it can fire
         */

        if(currentCooldown <= 0) {
            SuccessfulFire();
        }
    }

    public override void SuccessfulFire() {
        /*
         * Fire the weapon and reset it's cooldown
         */
         
        currentCooldown = maxCooldown;

        /* Start putting power into the laser */
        currentActiveTime = maxActiveTime;
    }

    public override void ReduceCooldown(float time) {
        /*
         * Reduce the current cooldown
         */

        /* Reduce the firing cooldown */
        currentCooldown -= time;
        if(currentCooldown < 0) {
            currentCooldown = 0;
        }
    }


    /* -------- Collision Detection Functions ------------------------------------------------------- */

    public void CheckForCollisions() {
        /*
         * Check if the laser has collided with any colliders. Lasers detect if they hit a collider using
         * a sphere cast from their origin. The sphere's radius is equal to the laser's width and it is
         * cast along the laser's length.
         * 
         * If the collider hit has a SpaceObject attached to their parent, run it's HitByLaser() function.
         */
        float sphereRadius = GetLaserWidth();
        float laserLength = GetLaserLength() - sphereRadius*2f;
        RaycastHit laserHitInfo;

        /* The direction the laser is heading */
        Vector3 laserDirection = transform.forward;

        /* Place the center of the sphere so that the it's center is a radius from the firing point */
        Vector3 sphereCenter = transform.position + laserDirection*sphereRadius;

        /* Check if the laser collides with any space objects */
        if(Physics.SphereCast(sphereCenter, sphereRadius, laserDirection, out laserHitInfo, laserLength)) {
            SpaceObject hitObject = laserHitInfo.collider.transform.GetComponent<SpaceObject>();

            /* Stop the laser at the collision point */
            currentLaserLength = laserHitInfo.distance + sphereRadius*2f;
            laserCollision = true;

            /* Place the laser hit particle emitter at the hit point and emit a burst of particles */
            laserHitParticles.transform.position = sphereCenter + laserDirection*laserHitInfo.distance;
            EmitLaserHitParticles();

            /* Send the hit object a HitByLaser signal if it's a SpaceObject */
            if(hitObject != null) {
                hitObject.HitByLaser(this);
            }
            else {
                Debug.Log("LASER HIT SOMETHING THAT IS NOT A SPACEOBJECT");
            }
        }
        else {
            /* The laser continued for it's full length and did not collide with anything */
            currentLaserLength = GetLaserLength();
            laserCollision = false;
        }
    }


    /* -------- Event Functions -------------------------------------------------------------------- */

    public void UpdateLaserPower(float time) {
        /*
         * Update the current power of the laser. Depending on the active state of the laser, change it's power.
         * The active state of the laser is reduced on each frame.
         */

        /* If the laser is active, apply more power to the laser */
        if(currentActiveTime > 0) {
            IncreaseLaserPower(time);
        }
        /* If the laser is not active, reduce the current power of the laser */
        else {
            DecreaseLaserPower(time);
        }

        /* Reduce the remaining active time of the laser */
        currentActiveTime -= time;
        if(currentActiveTime < 0) {
            currentActiveTime = 0;
        }
    }

    public void IncreaseLaserPower(float time) {
        /*
         * Increase the power of the laser and prevent it from going above it's max
         */

        laserCurrentPower += fireWeaponPower*time;

        if(laserCurrentPower > laserMaxPower) {
            laserCurrentPower = laserMaxPower;
        }
    }

    public void DecreaseLaserPower(float time) {
        /*
         * Decrease the power of the laser and prevent it from going bellow 0
         */

        laserCurrentPower -= dissipatePower*time;

        if(laserCurrentPower < 0) {
            laserCurrentPower = 0;
        }
    }
    
    public float GetLaserLength() {
        /*
         * Return the max length of the laser with it's current power
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxLength;
    }

    public float GetLaserWidth() {
        /*
         * Get the current width of the laser
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxWidth;
    }
    
    public float GetLaserDamage() {
        /*
         * Calculate how much damage the laser can cause in it's current state
         */
        float damage = baseDamage;

        /* The laser's damage is increased when it's current power is 90% or more of it's max */
        if(laserCurrentPower >= laserMaxPower*0.9f) {
            damage *= 5f;
        }
        /* The laser deals less damage when it's power is bellow 50% */
        else if(laserCurrentPower < laserMaxPower*0.5f) {
            damage *= 0.25f;
        }

        return damage;
    }

    public void UpdateLaserSize() {
        /*
         * Update the size of the laser. The size of the laser is affected by laserCurrentPower
         */
        float length, width;

        /* Get the current length and width of the laser */
        length = currentLaserLength;
        width = GetLaserWidth();

        /* Set the laser size */
        laserBeam.transform.localScale = new Vector3(width, length/2, width);

        /* Reposition the laser */
        laserBeam.transform.localPosition = new Vector3(0, 0, length/2f);

    }

    public void ResetBeam() {
        /*
         * Reset the properties of the laser beam back to it's neutral state
         */

        laserBeam.transform.localPosition = new Vector3(0, 0, laserMaxLength/2f);
        laserBeam.transform.localEulerAngles = new Vector3(90, 0, 0);
        laserBeam.transform.localScale = new Vector3(1, laserMaxLength/2f, 1);
        Destroy(laserBeam.GetComponent<CapsuleCollider>());
    }

    public void EmitLaserHitParticles() {
        /*
         * Emit hit sparks from the laser hitting an obecjt using the laserHitParticles particle system.
         * The amount of particles emitted is all relative to the damage dealt
         */
        float baseParticleSpeed = 20;
        float baseParticleLifetime = 0.5f;

        /* Set the radius of the shape used to emit particles relative to the laser's width */
        var particleShape = laserHitParticles.shape;
        particleShape.radius = GetLaserWidth();

        /* Get the amount of damage the laser will do */
        float laserDamage = GetLaserDamage();
        Debug.Log(laserDamage);

        /* The speed, lifetime and amount of particles emitted are all relative to the damage dealt */
        //Set their speed
        laserHitParticles.startSpeed = baseParticleSpeed*(laserDamage/15f);
        //Set the lifetime
        laserHitParticles.startLifetime = baseParticleLifetime*(laserDamage/15f);
        //Emit the particles
        laserHitParticles.Emit((int) laserDamage);
    }
}
