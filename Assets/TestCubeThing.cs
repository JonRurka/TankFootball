using UnityEngine;
using System.Collections;

public class TestCubeThing : MonoBehaviour {

	// Use this for initialization
	void Start () {
        transform.rotation = Quaternion.Euler(new Vector3(180, 0, 0));
	}
	
	// Update is called once per frame
	void Update () {
        transform.rotation = Quaternion.Euler(new Vector3(180, 0, 0));
    }
}
