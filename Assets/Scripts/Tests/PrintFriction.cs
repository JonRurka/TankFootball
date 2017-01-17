using UnityEngine;
using System.Collections;

public class PrintFriction : MonoBehaviour {
    // Use this for initialization
    void Start () {
        //Rigidbody m_Rigidbody = GetComponent<Rigidbody>();
        //Debug.Log(m_Rigidbody.centerOfMass);
        Debug.Log("center: " + gameObject.GetComponent<Renderer>().bounds.center);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
