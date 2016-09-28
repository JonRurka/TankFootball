using UnityEngine;
using System.Collections;

public class BackSideRender : MonoBehaviour {

    

    // Use this for initialization
    void Start() {
        var coll = GetComponent<MeshCollider>();
        var mesh = GetComponent<MeshCollider>().sharedMesh;
        var vertices = mesh.vertices;
        var szV = vertices.Length;
        var newVerts = new Vector3[szV];
        for (var j = 0; j < szV; j++) {
            // duplicate vertices and uvs:
            newVerts[j] = vertices[j];
        }
        var triangles = mesh.triangles;
        var szT = triangles.Length;
        var newTris = new int[szT]; // double the triangles
        for (var i = 0; i < szT; i += 3) {
            // save the new reversed triangle
            int j = i ;
            newTris[j] = triangles[i];
            newTris[j + 2] = triangles[i + 1];
            newTris[j + 1] = triangles[i + 2];
        }
        mesh.vertices = newVerts;
        mesh.triangles = newTris; // assign triangles last!
        coll.sharedMesh = mesh;
    }

    // Update is called once per frame
    void Update () {
	
	}
}
