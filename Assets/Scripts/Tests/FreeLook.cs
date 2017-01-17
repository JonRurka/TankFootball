using UnityEngine;
using System.Collections;
//using bm = BulletSharp.Math;

public class FreeLook : MonoBehaviour {
    public static FreeLook Instance;

    public Camera cam { get; private set; }
    //public bm.Vector3 Eye { get; private set; }
    //public bm.Vector3 Target { get; private set; }
    //public bm.Vector3 Up { get; set; }

    void Awake() {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    // Use this for initialization
    void Start () {
        SetProperties();
    }
	
	// Update is called once per frame
	void Update () {
        SetProperties();
    }

    public void SetProperties() {
        //Eye = new bm.Vector3(transform.position.x, transform.position.y, transform.position.z);
        //Target = Eye + new bm.Vector3(transform.forward.x, transform.forward.y, transform.forward.z);
        //Up = new bm.Vector3(transform.up.x, transform.up.y, transform.up.z);
    }
}
