using UnityEngine;
using System.Collections;

public class BallCatchTrigger : MonoBehaviour {

    public TurretScript turret;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider other) {
        if (turret != null) {
            turret.ObjectCaught(other.gameObject);
        }
    }
}
