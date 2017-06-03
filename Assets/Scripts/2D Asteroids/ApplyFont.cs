using UnityEngine;
using System.Collections;

/*
 * Changes the linked gameObject's textMesh into the given font linked to this script.
 * It also gives the text a custom shader that allows the text to be obstructed from objects.
 */
[ExecuteInEditMode]
public class ApplyFont : MonoBehaviour {

    public Font fontObject;

	// Use this for initialization
	void Start () {

        /* Create a texture using a shader that allows 3D text to be obstructed */
        Material textMat = new Material(Shader.Find("GUI/3D Text Shader"));
        textMat.mainTexture = fontObject.material.mainTexture;
        
        /* Apply the texture and font to the linked gameObject */
        GetComponent<TextMesh>().font = fontObject;
        GetComponent<MeshRenderer>().material = textMat;
    }
}
