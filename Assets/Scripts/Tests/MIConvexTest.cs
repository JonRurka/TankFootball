using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using MIConvexHull;

public class MIConvexTest : MonoBehaviour {

    public class Vertex : IVertex {

        public double[] Position
        {
            get;
            set;
        }

        public Vertex(double x, double y, double z) {
            Position = new double[] { x, y, z };
        }
    }

    public class Face : ConvexFace<Vertex, Face> {
        
    }

    public MeshFilter meshFilter;

	// Use this for initialization
	void Start () {
        Mesh mesh = meshFilter.mesh;

        List<Vertex> verts = new List<Vertex>(mesh.vertices.Length);    
        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 v = mesh.vertices[i];
            verts.Add(new Vertex(v.x, v.y, v.z));
        }

        Debug.Log(verts.Count);

        var convexHull = ConvexHull.Create<Vertex, Face>(verts);
        verts = convexHull.Points.ToList();
        List<Face> faces = convexHull.Faces.ToList();

        Debug.Log(verts.Count + ", " + faces.Count);

        Vector3[] uVerts = new Vector3[verts.Count];
        for (int i = 0; i < uVerts.Length; i++) {
            uVerts[i] = new Vector3((float)verts[i].Position[0], (float)verts[i].Position[1], (float)verts[i].Position[2]);
        }

        List<int> triangles = new List<int>();
        foreach(var f in faces) {
            triangles.Add(verts.IndexOf(f.Vertices[0]));
            triangles.Add(verts.IndexOf(f.Vertices[1]));
            triangles.Add(verts.IndexOf(f.Vertices[2]));
        }

        mesh.triangles = triangles.ToArray();
        mesh.vertices = uVerts.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        string objStr = ObjExporter.MeshToString(meshFilter);

        string file = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "\\";
        Debug.Log(file);

        //meshFilter.mesh = mesh;
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
