using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinematics;

public class ArcRender : MonoBehaviour {
    public Color lineColor;
    public float timeStep = 0.1f;

    private LineRenderer lineRender;

	// Use this for initialization
	void Start () {
        lineRender = gameObject.GetComponent<LineRenderer>();
        lineRender.SetColors(lineColor, lineColor);
        lineRender.SetWidth(0.1f, 0.1f);
        lineRender.enabled = false;
    }
	
	// Update is called once per frame
	void Update () {
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, transform.eulerAngles.z);
	}
    
    public void Render(Vector3 position, Vector3 velocity, float gravity) {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        List<Vector3> points = new List<Vector3>();
        Vector3 curPoint = position;
        points.Add(curPoint);
        float time = 0f;
        for (int i = 0; i < 1000; i++) {
            time += timeStep;
            curPoint.x += velocity.x * timeStep;
            curPoint.z += velocity.z * timeStep;
            curPoint.y = position.y + FreeFall.EndYposition(velocity.y, gravity, time);
            
            if (transform.TransformPoint(curPoint).y < 0f)
                break;

            points.Add(curPoint);
        }
        lineRender.enabled = true;
        lineRender.SetVertexCount(points.Count);
        lineRender.SetPositions(points.ToArray());
        watch.Stop();
        //Debug.Log("Line created in " + watch.Elapsed);
    }

    public void Disable() {
        lineRender.SetVertexCount(0);
        lineRender.SetPositions(new Vector3[0]);
        lineRender.enabled = false;
    }
}
