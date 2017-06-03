using UnityEngine;
using System.Collections;

/*
 * Controls all doors linked to the given LinkedPortal. Handles door rotation and open/closed detection.
 */
public class DoorController : MonoBehaviour {
    
    public Portal LinkedPortal;

    public float currentDoorAngle = 0;
    public float currentAutocloseTime;
    public float openingSpeed;
    public float openLimitPositive;
    public float openLimitNegative;
    
    [HideInInspector]
    public float currentDoorSpeed = 0;
    public bool closed = true;
    [HideInInspector]
    public float tempAutocloseTime;


    void Start() {
        /*
         * Check if the door is starting open or closed
         */

        if(currentDoorAngle == 0) {
            DoorClosed(true);
        }
        else {
            DoorClosed(false);
            LinkedPortal.SetPortalAngle(0, currentDoorAngle, 0);
        }
    }

    void Update() {
        Autoclose();
        ApplyRotation();
    }

    /* ---------- Update functions -------------------------------------------------------- */

    public void Autoclose() {
        /*
         * Check the autoclose values to determine if the door should close on itself automatically
         */

        currentAutocloseTime = tempAutocloseTime;

        if(currentAutocloseTime == 0 && currentDoorAngle != 0 && !isMoving()) {
            Debug.Log("autoclose the door");
            if(currentDoorAngle == openLimitPositive) {
                currentDoorSpeed = -openingSpeed;
            }
            else if(currentDoorAngle == openLimitNegative) {
                currentDoorSpeed = openingSpeed;
            }
        }

        tempAutocloseTime = 0;
    }

    public void ApplyRotation() {
        /*
         * Rotate the door around it's point of origin. The amount of rotation is given by currentDoorSpeed
         * and will stop at given angle limits (openLimitPositive, openLimitNegative and 0).
         */
         
        if(isMoving()) {
            /* Get the amount of rotation distance the door will undergo */
            float rotationDistance = currentDoorSpeed * Time.deltaTime;
            currentDoorAngle += rotationDistance;

            /* Stop the door once it reaches the openLimitPositive */
            if(currentDoorAngle >= openLimitPositive) {
                currentDoorAngle = openLimitPositive;
                StopDoor();
            }
            /* Stop the door once it reaches the openLimitNegative */
            else if(currentDoorAngle <= openLimitNegative) {
                currentDoorAngle = openLimitNegative;
                StopDoor();
            }
            /* Stop the door just as it passes or lands on the closed angle of 0 */
            else if((currentDoorAngle >= 0 && currentDoorAngle - rotationDistance < 0)
                    || (currentDoorAngle <= 0 && currentDoorAngle - rotationDistance > 0)) {
                currentDoorAngle = 0;
                DoorClosed(true);
                StopDoor();
            }

            /* Apply the rotation to the door */
            LinkedPortal.SetPortalAngle(0, currentDoorAngle, 0);
        }
    }


    /* ---------- Value changing functions -------------------------------------------------------- */

    public void Activated(int side) {
        /* 
         * Doors will swing away from the player when activated and will open from both sides.
         * If activated while open, it will rotate towards the closed position. 
         */

        /* Start opening/closing the door if it's not currently moving */
        if(!isMoving()) {

            /* Have the door try to close if it's activated while fully open */
            if(currentDoorAngle == openLimitPositive) {
                currentDoorSpeed = -openingSpeed;
            }
            else if(currentDoorAngle == openLimitNegative) {
                currentDoorSpeed = openingSpeed;
            }
            /* Check if the player can open this door if it's closed */
            else if(Openable()) {
                /* Change the speed's sign depending on what side of the door the player is on */
                currentDoorSpeed = openingSpeed * side;
                DoorClosed(false);
            }
        }
        /* Reverse the door swinging speed if the door is already opening/closing */
        else {
            currentDoorSpeed *= -1;
        }
    }

    public void StopDoor() {
        /* 
         * Stop the door while it's trying to open
         */

        currentDoorSpeed = 0;
    }

    public void DoorClosed(bool state) {
        /*
         * Set the state of all connected doors' portals whenever the door's closed state changes
         */

        closed = state;
        LinkedPortal.SetPortalsActiveState(!closed);
    }

    /* ---------- Status check functions -------------------------------------------------------- */

    public bool isMoving() {
        /*
         * Return true if the door is currently rotating, false if it is stopped. Movement is determined by currentDoorSpeed.
         */

        return (currentDoorSpeed != 0);
    }

    public bool Openable() {
        /*
         * Return true if the door can be opened, false if it will remain closed
         */

        return true;
    }









}
