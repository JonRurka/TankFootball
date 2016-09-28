using UnityEngine;
using System.Collections;

public class SetTiling : MonoBehaviour {
    public float texturesPerMeter = 1;
    public bool scaleY = false;

    // Use this for initialization
    void Start () {
        float yScale = 1;
        if (scaleY)
            yScale = transform.lossyScale.y;
        GetComponent<Renderer>().material.mainTextureScale = new Vector2(transform.lossyScale.x / texturesPerMeter, yScale / texturesPerMeter);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
