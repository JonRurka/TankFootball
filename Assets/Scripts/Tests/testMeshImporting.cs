using UnityEngine;
using System.Collections;

public class testMeshImporting : MonoBehaviour {
    public MeshCollider fromCol;
    public GameObject toObj;

	// Use this for initialization
	void Start () {
        Mesh mesh = fromCol.sharedMesh;
        toObj.GetComponent<MeshFilter>().sharedMesh = mesh;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
