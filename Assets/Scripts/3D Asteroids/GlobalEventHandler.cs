using UnityEngine;
using System.Collections;

/*
 * Handle any events that will have a global effect on the objects of the scene
 */
public class GlobalEventHandler : MonoBehaviour {

    /* The entire Ship object */
    public ShipObject ship;

    public void Move() {
        /*
         * Move all linked objects in space
         */
    }


    public Vector3 ShipMove(float x, float y) {
        /*
         * Recieve a moveShip request. This step can be used to modify the ship's movement speed.
         * Return a movement vector of the ship's movement
         */

        return ship.Move(x, y);
    }
}
