using UnityEngine;
using System.Collections;

/*
 * Handles any function that involve the laser acting upon a laser-mirror collision.
 * 
 * Any new mirror objects added to the game will require this script to undergo some additions.
 */
public class LaserReflection : MonoBehaviour {

    public float nextReflectionPoint(Vector3 newPosition, ref Vector3 newDirection) {
        /*
		 * Return the amount of distance that needs to be travelled for the given point to land on 
		 * It's current tile's mirror's reflection point. If the returned value is 0 or less, 
		 * That means the reflection point has already passed or there is no mirror in the tile.
         * 
         * Change the newDirection vector to match the new direction that the laser will undergo if it hits a mirror
		 */
        Entity foundEntity = null;
        EntityClass foundType = EntityClass.None;
        float directionDistance = 0;

        /* Get the entity that occupies the given position */
        foundEntity = GridSystem.instance.gridObjectsGet(newPosition);
        if(foundEntity != null) {
            foundType = foundEntity.type;
        }

        /* If there is a mirror entity present, find the upcomming reflection point using the given position and direction  */
        if(EnumClassMethods.isMirror(foundType)) {

            /* Get a vector that goes from the given position to the center of the tile */
            Vector3 DirectionTowardsCenter = GridSystem.instance.CenterPosition(newPosition) - newPosition;

            /* Lock the vector to the newDirection */
            Vector3 directionVector = new Vector3(DirectionTowardsCenter.x*newDirection.x, 0, DirectionTowardsCenter.z*newDirection.z);

            /* Save the lost values when locking the vector */
            Vector3 lostVector = DirectionTowardsCenter - new Vector3(directionVector.x * newDirection.x, 0, directionVector.z * newDirection.z);

            /* Get the distance of the direction vector */
            directionDistance = directionVector.x + directionVector.z;

            /* Get the amount of distance lost when locking to the direction vector */
            float lostDistance = lostVector.x + lostVector.z;

            /* Properly change the laser's properties depending on the mirror hit */
            switch(foundType) {
                case EntityClass.Mirror_Cross:
                    /* A cross mirror is just both diagonnal mirrors in one space. Reflection will be treated as such
                     * unless the position gets close enough to the center of the tile (10% the size of the tiles) */
                    //directionDistance -= (GridSystem.instance.tileSize / 2.0f) * 0.1f;

                    /* Do a 180 reflection if the laser gets too close to the center of the cross mirror.
                     * Do not reflect from the center if the lostDistance is too larger (point far from center) */
                    float distanceToCenter = 0.0f;
                    distanceToCenter = directionDistance - (GridSystem.instance.tileSize / 2.0f) * 0.1f;
                    if(distanceToCenter <= 0 || Mathf.Abs(lostDistance) > (GridSystem.instance.tileSize / 2.0f) * 0.1f) {
                        distanceToCenter = float.MaxValue;
                    }

                    /* Check the point along the given direction for the diagonnal mirrors that form the cross */
                    float distanceToBLMirror = 0.0f;
                    distanceToBLMirror = directionDistance + (-newDirection.x + -newDirection.z) * lostDistance;
                    if(distanceToBLMirror <= 0) {
                        distanceToBLMirror = float.MaxValue;
                    }

                    float distanceToTLMirror = 0.0f;
                    distanceToTLMirror = directionDistance + (newDirection.x + newDirection.z) * lostDistance;
                    if(distanceToTLMirror <= 0) {
                        distanceToTLMirror = float.MaxValue;
                    }

                    /* Get the closest point and use it's distance */
                    directionDistance = Mathf.Min(distanceToCenter, distanceToBLMirror, distanceToTLMirror);

                    /* Properly reflect the direction depending on which point is closest */
                    if(directionDistance == distanceToCenter) {
                        newDirection *= -1;
                    }
                    else if(directionDistance == distanceToBLMirror) {
                        newDirection = new Vector3(newDirection.z, 0, newDirection.x);
                    }
                    else if(directionDistance == distanceToTLMirror) {
                        newDirection = new Vector3(-newDirection.z, 0, -newDirection.x);
                    }


                    break;
                //due to the laser being pushed once it reflects, we need to make sure it wont be pushed pass the other mirror that makes the cross
                case EntityClass.Mirror_Diagonal:
                    //Debug.Log(newPosition);
                    if(foundEntity.GetComponent<Entity>().angle == Direction.Up || foundEntity.GetComponent<Entity>().angle == Direction.Down) {
                        //BL to TR
                        /* Use the lostDistance value to determine how off-center the laser is from mirror to properly adjust it's reflection point. The mirror angle changes how the distance is handled. */
                        directionDistance += (-newDirection.x + -newDirection.z) * lostDistance;
                        newDirection = new Vector3(newDirection.z, 0, newDirection.x);
                    }
                    else {
                        //TL to BR
                        /* Use the lostDistance value to determine how off-center the laser is from mirror to properly adjust it's reflection point. The mirror angle changes how the distance is handled. */
                        directionDistance += (newDirection.x + newDirection.z) * lostDistance;
                        newDirection = new Vector3(-newDirection.z, 0, -newDirection.x);
                    }
                    break;
                case EntityClass.Mirror_Square:
                    /* The square mirror is 10% smaller than the tile formed by tileSizeBuffer */
                    //directionDistance = transform.GetComponent<LaserCollision>().CurrentGridEdge(newPosition, newDirection);
                    //float extraDist = (GridSystem.instance.tileSize / 2.0f) * (GridSystem.instance.tileSizeBuffer) * 0.9f;
                    //directionDistance -= 0;
                    //Debug.Log(directionDistance);

                    ////////////////It has to do with the value of directionDistance
                    //directionDistance = transform.GetComponent<LaserCollision>().CurrentGridEdge(newPosition, newDirection) +
                            //(GridSystem.instance.tileSize / 2.0f) * (GridSystem.instance.tileSizeBuffer) * 0.1f;
                    //directionDistance = transform.GetComponent<LaserCollision>().CurrentGridEdge(newPosition, newDirection);
                    directionDistance -= (GridSystem.instance.tileSize / 2.0f) * (GridSystem.instance.tileSizeBuffer) * 0.9f;

                    ///////////////////////////////////////////////////////
                    //Seems like this value get very small and then the game loops.
                    //When the laser hits the PoI of this mirror while reflecting, the loop occurs
                    //For some reason, the laser comming from the X axis is fine while Z axis loops
                    //Debug.Log(directionDistance);
                    //Debug.Log("---------------");

                    if(directionDistance < 0.000001 && directionDistance > 0) {
                        //Debug.Log("freeze");
                        //directionDistance = 0.001f;
                    }


                    /* A laser hitting a square tile will simply undergo a 180 degree rotation */
                    newDirection *= -1;
                    break;
                default:
                    /* Default output whenver a mirror is not yet set-up for reflection */
                    Debug.Log("ERROR: CANNOT HANDLE THE REFLECTION OF THE ENCOUNTERED MIRROR TYPE " + foundType);
                    break;
            }
        }


        return directionDistance;
    }
}
