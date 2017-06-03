using UnityEngine;
using System.Collections;

/*
 * When  the player teleports, check the given potential light systems that will need
 * to be updated based on the player's new position in the world.
 */
public class LightFixtureTeleportHandler : TeleportHandler {

    //The potential lights that the player will be moving into
    public ControlledLightSystem[] enteringLights;

    //The potential lights the player will be moving away from
    public ControlledLightSystem[] leavingLights;


    public override void playerTeleported() {
        /*
         * When the player gets teleported, update the light fixtures's new state as they get teleported
         */

        //Turn on the lights that the player is entering 
        for(int i = 0; i < enteringLights.Length; i++) {
            enteringLights[i].turnOn();
        }

        //Turn off the lights the player is leaving
        for(int i = 0; i < leavingLights.Length; i++) {
            leavingLights[i].turnOff();
        }
    }
}
