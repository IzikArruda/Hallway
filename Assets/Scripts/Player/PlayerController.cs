using UnityEngine;
using System.Collections;

/*
 * Handle any inputs and movement to be done with the player and their camera
 */
public class PlayerController : MonoBehaviour {
    
    /* The position of the player's camera when used for their view. */
    public Transform restingCameraTransform;
    /* The camera used for the player's main view */
    public Transform playerCamera;

    /* Player stats that adjust how they control */
    [Range(1, 0)]
    public float sliding;
    public float movementSpeed;
    public float runSpeedMultiplier;
    public float gravity;
    public float interactReach;

    /* Hidden values used for player movement */
    private Vector3 inputVector = Vector3.zero;
    private Vector3 gravityVector = Vector3.zero;
    private float gravityMaxVelocity = 5;
    private float yVelocity;
    private float yRotation = 0;
    private float xRotation = 0;

    /* The state the player is in */
    private Enums.PlayerStates state;
    private Interactable overriddenScript = null;

    /* Saved values used when handling camera movements through interactables. */
    public Transform camCurrentTransform;
    public Transform camDestinationTransform;
    public Enums.PlayerStates destinationState;
    public float camLERPTime;
    
    /* the state of the player's inputs on a frame */
    private UserInputs inputs = new UserInputs();


    /* -------------- Unity Functions ---------------------------------------------- */

    void Start() {
        /*
         * The player starts in control of themselves
         */

        state = Enums.PlayerStates.Control;
        camCurrentTransform = restingCameraTransform;
    }

    void OnTriggerEnter() {
        /*
         * If the player enters a trigger, send a HitByPlayer signal to the trigger
         */
         
    }

    void Update() {
        /*
         * Handle any player inputs. If they need to be redirected to a new script,
         * send the input signals to the current overriddenScript.
         */

        /* Get the inputs of the player on this frame */
        UpdateInputs();

        /* The player is free to control their character */
        if(state == Enums.PlayerStates.Control) {
            PlayerMovement();
            CameraMovement();
            PlayerInteract();
        }
        /* The player has no input during the camera's move to it's new position */
        else if(state == Enums.PlayerStates.MovingCamera) {
            LERPCamera();
        }
        /* The player's controls will be redirected to the overriden script */
        else if(state == Enums.PlayerStates.Overridden) {
            overriddenScript.HandleInputs(inputs);
        }
    }


    /* -------------- Per-Frame Update Functions ---------------------------------------------- */
    
    void UpdateInputs() {
        /*
         * Update the input values of the player for this frame
         */
         
        inputs.playerMovementX = Input.GetAxis("Horizontal");
        inputs.playerMovementY = Input.GetAxis("Vertical");
        inputs.playerMovementXRaw = Input.GetAxisRaw("Horizontal");
        inputs.playerMovementYRaw = Input.GetAxisRaw("Vertical");
        inputs.leftMouseButton = Input.GetMouseButtonDown(0);
        inputs.rightMouseButton = Input.GetMouseButtonDown(1);
        inputs.mouseX = Input.GetAxis("Mouse X");
        inputs.mouseY = Input.GetAxis("Mouse Y");
        inputs.spaceBar = Input.GetKeyDown("space");
    }

    void PlayerMovement() {
        /*
         * Handle the displacement of the player character in the world through the WASD keys and gravity.
         * Movement is calculated by adding together the inputVector and the gravityVector.
         */
        CharacterController playerController = GetComponent<CharacterController>();

        /* Use two input types for each axis to allow more control on player movement */
        inputVector = new Vector3((1-sliding)*Input.GetAxisRaw("Horizontal") + sliding*Input.GetAxis("Horizontal"),
                0, (1-sliding)*Input.GetAxisRaw("Vertical") + sliding*Input.GetAxis("Vertical"));
        
        /* Keep the movement's maginitude from going above 1 */
        if(inputVector.magnitude > 1) {
            inputVector.Normalize();
        }

        /* Add the player speed to the movement vector */
        if(Input.GetKey(KeyCode.LeftShift)) {
            inputVector *= movementSpeed*runSpeedMultiplier;
        }else {
            inputVector *= movementSpeed;
        }
        
        /* Rotate the input direction to match the player's view (only use the Y angle of the camera) */
        Transform cameraYAngle = restingCameraTransform.transform;
        cameraYAngle.localEulerAngles = new Vector3(0, cameraYAngle.localEulerAngles.y, 0);
        inputVector = cameraYAngle.rotation*inputVector;


        /* Calculate the gravity velocity to apply to the player and add it to the movement vector.
         * The current angle of the player model's rotation will alter where gravity is applying force. */
        //For now, handle gravity with two different cases: if there is no rotation in X or Z meaning the player
        //is on  flat ground, and if not, they are not on flat ground so dont use gravity.
        bool normalGravity = true;
        if(transform.eulerAngles.x != 0 || transform.localEulerAngles.z != 0) {
            normalGravity = false;
        }

        /* Normal gravity uses the playerController to determine if the player is grounded */
        if(normalGravity) {
            if(!playerController.isGrounded) {
                gravityVector += gravity * Time.deltaTime * (transform.rotation * Vector3.down);
            }
        }
        /* Modified gravity does nothing so far. So far this will only be used in the ship.
         * Possible idea is to "lock" the player to the ground using a collision box at their feet. */
        else {
            gravityVector = Vector3.zero;
        }
        Debug.Log(normalGravity);

        /* Prevent the gravity vector from becoming too large */
        if(gravityVector.magnitude > gravityMaxVelocity) {
            gravityVector = gravityVector.normalized*gravityVector.magnitude;
        }


        /* Apply the movement to the player character */
        playerController.Move((inputVector + gravityVector) * Time.deltaTime*60);
    }

    void CameraMovement() {
        /* 
         * Handle mouse movement to change the camera's viewing angle and the player's forward vector.
         */

        /* Rotate the camera's resting position for any X mouse movement */
        xRotation -= Input.GetAxis("Mouse X");
        if(xRotation < 0) { xRotation += 360; }
        else if(xRotation > 360) { xRotation -= 360; }

        /* Rotate the camera's resting position for any Y mouse movement */
        yRotation += Input.GetAxis("Mouse Y");
        yRotation = Mathf.Clamp(yRotation, -75, 75);

        /* Apply the rotation to the camera's resting position */
        restingCameraTransform.transform.localEulerAngles = new Vector3(-yRotation, -xRotation, 0);
        
        /* Update the camera's position with the new restingCamera transform */
        playerCamera.rotation = restingCameraTransform.transform.rotation;
    }

    void PlayerInteract() {
        /*
         * Handle player inputs to catch when the player wants to interact with an object.
         * An object that is hit by a raytrace originating from the camera will be activated.
         * 
         * Some objects will take over the player's control or redirect them to another system.
         * When this occurs, the player will be in a "frozen" state where certain movements 
         * will not be done until the player stops interacting with the same object.
         */

        if(Input.GetMouseButtonDown(0)) {

            /* Set up the ray */
            Ray interactRay = new Ray(playerCamera.position, playerCamera.forward);
            RaycastHit hit;
            int ignoreTriggersLayer = 1 << 8;
            ignoreTriggersLayer = ~ignoreTriggersLayer;

            /* Fire the ray */
            if(Physics.Raycast(interactRay, out hit, Mathf.Infinity, ignoreTriggersLayer)) {
                if(hit.distance < interactReach) {
                    if(hit.transform.tag == "Interactable") {
                        print("I'm looking at " + hit.transform.name);
                        hit.transform.GetComponent<Interactable>().Activated(this);
                    }else {
                        Debug.Log("some object " + hit.transform.name);
                    }
                }
                else {
                    Debug.Log("hit too far");
                }
            }
            else {
                Debug.Log("Hit nothing");
            }
        }
    }


    /* -------------- Event Functions ---------------------------------------------- */

    public bool ControlOverrideRequest(Interactable interactableScript, 
            Transform newCamTransform, Enums.PlayerStates newState) {
        /*
         * Send a request to take over the player's controls. If the request is accepted, the player's state will be 
         * set to "MovingCamera" as the camera moves to the given position. Once it reaches the destination,
         * the player's state will be set to the given new state.
         * 
         * Return true if the request is accepted, false if it is denied.
         */
        bool requestAccepted = false;
         
        /* Accept the move request if the player is in control of the camera */
        if(state == Enums.PlayerStates.Control || state == Enums.PlayerStates.Overridden) {
            camDestinationTransform = newCamTransform;
            destinationState = newState;
            camLERPTime = 0;
            state = Enums.PlayerStates.MovingCamera;
            overriddenScript = interactableScript;
            requestAccepted = true;
        }

        return requestAccepted;
    }

    public bool ReturnControlRequest() {
        /*
         * Take in a request to break the current control override and give back player control.
         * If it breaks from the override, then return true. False if it does not break.
         */
        bool breakState = false;

        /* There is no control to break if the player is already controlling themselves */
        if(state != Enums.PlayerStates.Control) {

            /* Send a request to move the camera back to the proper player viewing position */
            if(ControlOverrideRequest(null, restingCameraTransform, Enums.PlayerStates.Control)) {
                breakState = true;
            }
        }

        return breakState;
    }
    
    public void LERPCamera() {
        /*
         * Move the camera's current transform to the destination transform. Set the state once it reaches it's destination
         */
        camLERPTime += 0.01f *Time.deltaTime*60;

        playerCamera.position = Vector3.Lerp(camCurrentTransform.position, camDestinationTransform.position, camLERPTime);
        playerCamera.rotation = Quaternion.Slerp(camCurrentTransform.rotation, camDestinationTransform.rotation, camLERPTime);

        /* Once the camera has reached the destination position, set the player state to override */
        if(camLERPTime >= 1) {
            state = destinationState;
            camCurrentTransform = camDestinationTransform;
        }
    }
}