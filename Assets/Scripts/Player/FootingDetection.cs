using UnityEngine;
using System.Collections;

public class FootingDetection : MonoBehaviour {

    public int insideCount = 0;
    

    void OnTriggerEnter(Collider collider) {
        insideCount++;
    }

    void OnTriggerExit(Collider collider) {
        insideCount--;
    }
}
