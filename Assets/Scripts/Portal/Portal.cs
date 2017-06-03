using UnityEngine;
using System.Collections;

/*
 * Holds references to important elements of the portal.
 * 
 * Portals will have a "hiddenLights" list, which is a list of lights between both portals that are
 * hidden and are used to emulate the light from light fixtures passing through the portals.
 * These lights are only active when the portal's door is open.
 */
public class Portal : MonoBehaviour {
    
    /* The "entrance" portalObjects of the portal the user will consider an "entrance" */
    public PortalObjects EntrancePortal;

    /* The exit portalObjects of the entrance portal that leads into a new area */
    public PortalObjects ExitPortal;

    /* An array of extra "entrance" portalObjects. These will most likely not be teleporting the player */
    public PortalObjects[] ExtraEntrancePortals;

    /* An array of lights that reach the portal. These lights will be recreated on the other side of the  portal 
     * and will be added as "hidden lights" to their respective lightSystems to simulate light through the portal */
    public ControlledLightSystem[] LinkedLights;


    void Start() {
        /*
         * On startup, link the hidden lights to their corresponding ControlledLight script 
         */
        ControlledLightSystem lightSystem;
        Light newHiddenLight;
        Vector3 localPosition;
        Quaternion localRotation;
        bool entranceClosest;
        
        /* Take the LinkedLights list and recreate the lights they are connected 
         * to be hidden lights for the other side of the portal */
        for(int i = 0; i < LinkedLights.Length; i++) {
            lightSystem = (ControlledLightSystem) LinkedLights[i];
            
            foreach(Light light in lightSystem.originalLights) {
                /* Clone the light to create the new hidden light */
                newHiddenLight = Instantiate(light);
                newHiddenLight.transform.parent = light.transform.parent;
                newHiddenLight.transform.position = light.transform.position;
                newHiddenLight.transform.rotation = light.transform.rotation;
                newHiddenLight.name = "Hidden Light";

                /* Save the light's local transform relative to it's closest main portal when it is a child to it */
                if(Vector3.Distance(newHiddenLight.transform.position, EntrancePortal.transform.position) <
                        Vector3.Distance(newHiddenLight.transform.position, ExitPortal.transform.position)) {
                    entranceClosest = true;
                    newHiddenLight.transform.parent = EntrancePortal.transform;
                }else {
                    entranceClosest = false;
                    newHiddenLight.transform.parent = ExitPortal.transform;
                }

                /* Save the light's local position and rotation */
                localPosition = newHiddenLight.transform.localPosition;
                localRotation = newHiddenLight.transform.localRotation;

                /* move the hidden light to the other main portal as a child and use the preserved transform from before */
                if(entranceClosest) {
                    newHiddenLight.transform.parent = ExitPortal.transform;
                    newHiddenLight.transform.localPosition = localPosition;
                    newHiddenLight.transform.localRotation = localRotation;
                }
                else {
                    newHiddenLight.transform.parent = EntrancePortal.transform;
                    newHiddenLight.transform.localPosition = localPosition;
                    newHiddenLight.transform.localRotation = localRotation;
                }

                /* Set the hiddenLight's parent back to it's lightSystem */
                newHiddenLight.transform.parent = light.transform.parent;

                /* Add the light to it's lightSystem's hiddenLight list */
                lightSystem.hiddenLights.Add(newHiddenLight);
            }
        }
    }

    public void SetPortalAngle(float x, float y, float z) {
        /*
         * Set the angle of each door linked to this portal
         */

        EntrancePortal.portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        ExitPortal.portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        
        for(int i = 0; i < ExtraEntrancePortals.Length; i++) {
            ExtraEntrancePortals[i].portalDoor.transform.localEulerAngles = new Vector3(x, y, z);
        }
    }

    public void SetPortalsActiveState(bool closed) {
        /*
         * Set the portal's meshes active state
         */

        EntrancePortal.portalMesh.SetActive(closed);
        ExitPortal.portalMesh.SetActive(closed);

        for(int i = 0; i < ExtraEntrancePortals.Length; i++) {
            ExtraEntrancePortals[i].portalMesh.SetActive(closed);
        }
    }
}
