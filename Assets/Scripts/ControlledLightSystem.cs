using UnityEngine;
using System.Collections;

/*
 * Controls the light's state. Can be called to turn the light on or off,
 * and can be toggled to turn on when the player enter's it's range/off when they leave,
 * the range being the box collider of the script's linked object.
 * 
 * There can be multiple lights linked to this script, which is usually the case
 * due to "hidden lights" behind portals.
 * 
 * Note: when using turnOn(), if the player was never in the light's range, the light
 * won't turn off automatically unless the player enters the range and leaves themselves.
 */
public class ControlledLightSystem : MonoBehaviour {

    /* The original lights linked to this system */
    public Light[] originalLights;

    /* Hidden lights that have been linked to this light system from portals */
    public ArrayList hiddenLights = new ArrayList();

    /* The model of the light fixtured used to represent the light visually */
    public GameObject lightFixture = null;

    /* The material applied on the light fixture when it is turned on and off */
    public Material lightMaterialOn;
    public Material lightMaterialOff;
    public bool motionActivated;

    void OnTriggerEnter(Collider collider) {
        /* 
         * Check if the player has entered the trigger and if the light is set to motion tracking
         */
        if(motionActivated) {
            if(collider.transform.tag == "Player") {
                turnOn();
            }
        }
    }

    void OnTriggerExit(Collider collider) {
        /*
         * Check if the player has left the trigger and if the light is set to motion tracking
         */
        if(motionActivated) {
            if(collider.transform.tag == "Player") {
                turnOff();
            }
        }
    }

    public void turnOn() {
        /*
         * Turn the light on
         */
         
        /* Turn on the original lights */
        for(int i = 0; i < originalLights.Length; i++) {
            originalLights[i].enabled = true;
        }

        /* Turn on the hidden lights linked to this light system */
        foreach(Light light in hiddenLights) {
            light.enabled = true;
        }

        lightFixture.GetComponent<Renderer>().material = lightMaterialOn;
    }

    public void turnOff() {
        /*
         * Turn the light off
         */

        /* Turn off the original lights */
        for(int i = 0; i < originalLights.Length; i++) {
            originalLights[i].enabled = false;
        }

        /* Turn off the hidden lights linked to this light system */
        foreach(Light light in hiddenLights) {
            light.enabled = false;
        }


        lightFixture.GetComponent<Renderer>().material = lightMaterialOff;
    }
}

