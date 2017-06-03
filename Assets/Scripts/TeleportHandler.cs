using UnityEngine;
using System.Collections;

/*
 * Have classes inherit this class to be able to react upon a portal teleporting the player.
 * Treat it as an interface for scripts that will react to player teleporting.
 */
public class TeleportHandler : MonoBehaviour {

    public virtual void playerTeleported() {
        Debug.Log("MISSING PLAYER TELEPORT HANDLER");
    }
}
