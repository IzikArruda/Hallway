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
    
    /* For how long power will be put into a laser once fired */
    [HideInInspector]
    public float currentActiveTime;
    public float maxActiveTime;

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
         
        Debug.Log("Fire laser");
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
         * Return the current length of the laser
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxLength;
    }

    public float GetLaserWidth() {
        /*
         * Get the current width of the laser
         */

        return (laserCurrentPower/laserMaxPower)*laserMaxWidth;
    }
    
    public void UpdateLaserSize() {
        /*
         * Update the size of the laser. The size of the laser is affected by laserCurrentPower
         */
        float length, width;

        /* Get the current length and width of the laser */
        length = GetLaserLength();
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
}
