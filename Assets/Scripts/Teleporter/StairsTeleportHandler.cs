using UnityEngine;
using System.Collections;

/*
 * When the player is teleported by the teleporter at the stairs, readjust the lights on the same frame.
 */
public class StairsTeleportHandler : TeleportHandler {

    //The two lights that the player will be teleported into
    public ControlledLightSystem entering1;
    public ControlledLightSystem entering2;

    //The two lights the player is teleported away from
    public ControlledLightSystem leaving1;
    public ControlledLightSystem leaving2;


    public override void playerTeleported() {
        /*
         * When the player gets teleported, turn on the lights they are entering and turn off the lights they are leaving
         */

        entering1.turnOn();
        entering2.turnOn();
        leaving1.turnOff();
        leaving2.turnOff();
    }
}
