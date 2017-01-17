using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Constraints.TwoEntity.Joints;
using bp = BEPUutilities;
using MI = MIConvexHull;

public class BPTest : MonoBehaviour {
    public class Vertex : MI.IVertex {

        public double[] Position
        {
            get;
            set;
        }

        public Vertex(double x, double y, double z) {
            Position = new double[] { x, y, z };
        }
    }

    public class Face : MI.ConvexFace<Vertex, Face> {

    }

    public BEPUphysics.Space space;
    public GameObject boxPrefab;
    public GameObject UBoxPrefab;
    public GameObject TankPrefab;
    public GameObject TurretPrefab;
    public PhysicMaterial mat;

    public int averageIteration = 0;

    public float m_Speed = 12f;
    public float m_TurnSpeed = 2.5f;

    public Vector3 euler;

    Box ground;
    Transform groudTrans;

    Box ForwardWall;
    Transform ForwardWallTrans;

    Box BackWall;
    Transform BackWallTrans;

    Box RightWall;
    Transform RightWallTrans;

    Box LeftWall;
    Transform LeftWallTrans;

    Entity BPtankHull;
    Transform BPtankTrans;

    Rigidbody UTank;

    bool _run = true;
    int iterations = 0;
    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

    float frameDelta;
    System.Diagnostics.Stopwatch deltaWatch = new System.Diagnostics.Stopwatch();

    float tankHalfHeight = 0;

    // Use this for initialization
    void Start () {
        space = new BEPUphysics.Space();
        //space.ForceUpdater.Gravity = new bp.Vector3(0, -9.81f, 0);
        ground = new Box(new bp.Vector3(0, -1f, 0), 1000, 1, 1000);
        //space.Add(ground);

        //groudTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(0, -1f, 0), Quaternion.identity)).transform;
        //BoxCollider coll = groudTrans.gameObject.AddComponent<BoxCollider>();
        //coll.size = new Vector3(1000, 1, 1000);
        //Rigidbody body = groudTrans.gameObject.AddComponent<Rigidbody>();
        //body.isKinematic = true;
        //groudTrans.localScale = new Vector3(1000, 1, 1000);

        //CreateForwardWall();
        //CreateBackWall();
        //CreateRightWall();
        //CreateLeftWall();

        Mesh mesh = TankPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        
        List<Vertex> verts = new List<Vertex>(mesh.vertices.Length);
        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 v = mesh.vertices[i];
            verts.Add(new Vertex(v.x, v.y, v.z));
        }

        var convexHull = MI.ConvexHull.Create<Vertex, Face>(verts);
        verts = convexHull.Points.ToList();

        bp.Vector3[] uVerts = new bp.Vector3[verts.Count];
        for (int i = 0; i < uVerts.Length; i++) {
            uVerts[i] = new bp.Vector3((float)verts[i].Position[0], (float)verts[i].Position[1], (float)verts[i].Position[2]);
        }

        BPtankHull = new Box(new bp.Vector3(0, 0, 0), 1, 1, 1.5f, 1);
        BPtankHull.AngularDamping = 0;
        BPtankHull.LinearDamping = 0;
        space.Add(BPtankHull);
        BPtankTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(0, 0, 0), Quaternion.identity)).transform;
        BPtankTrans.GetComponent<Renderer>().material.color = Color.blue;

        //BPtankHull = new ConvexHull(new bp.Vector3(0, 0, 0), uVerts, 1);
        //space.Add(BPtankHull);
        //BPtankTrans = ((GameObject)Instantiate(TankPrefab, new Vector3(0, 0, 0), Quaternion.identity)).transform;

        tankHalfHeight = BPtankHull.CollisionInformation.BoundingBox.Max.Y - BPtankHull.Position.Y;

        var locked = BPtankHull.LocalInertiaTensorInverse;
        locked.Right = new bp.Vector3();
        locked.Forward = new bp.Vector3();
        BPtankHull.LocalInertiaTensorInverse = locked;

        

        UTank = ((GameObject)Instantiate(UBoxPrefab, new Vector3(0, 0, 0), Quaternion.identity)).GetComponent<Rigidbody>();
        UTank.GetComponent<Renderer>().material.color = Color.red;
        UTank.useGravity = false;


        Camera cam = FindObjectOfType<Camera>();
        cam.transform.parent = BPtankTrans;
        cam.transform.localPosition = new Vector3(0, 2, -5);

        watch.Start();
        deltaWatch.Start();
    }

    void Update() {
        //Debug.LogFormat("U:{0}, B:{1}, A:{2}", UTank.angularVelocity.y * Mathf.Rad2Deg, BPtankHull.AngularVelocity.Y * Mathf.Rad2Deg, 
        //                 Quaternion.Angle(BPtankTrans.rotation, UTank.rotation));
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        iterations++;
        if (watch.Elapsed.Seconds >= 1) {
            averageIteration = iterations;
            iterations = 0;
            watch.Reset();
        }


        bp.Vector3 groundPos = ground.Position;
        //groudTrans.position = new Vector3(groundPos.X, groundPos.Y, groundPos.Z);


        float mov = Input.GetAxis("Vertical");
        float rot = Input.GetAxis("Horizontal");

        InputData input = new InputData(mov, rot);

        float _mov = 0;
        float _rot = 0;

        if (input.Forward)
            _mov = 1;
        else if (input.Backwards)
            _mov = -1;
        else
            _mov = 0;
        if (input.Right)
            _rot = 1;
        else if (input.Left)
            _rot = -1;
        else {
            _rot = 0;
        }

        if (_mov < -0.01f)
            _rot *= -1;

        // unity tank
        float uMovement = _mov * m_Speed;
        float uAMove = _rot * (m_TurnSpeed - (13.5f * Mathf.Deg2Rad));

        Debug.Log(uMovement);

        //float movement = 0;
        //float aMove = 0;

        Vector3 dir = UTank.rotation * Vector3.forward;
        UTank.velocity = new Vector3(dir.x * uMovement, 0, dir.z * uMovement);
        UTank.angularVelocity = new Vector3(0, uAMove, 0);

        UTank.rotation = Quaternion.Euler(0, UTank.rotation.eulerAngles.y, 0);


        // BP tank
        float bMovement = _mov * m_Speed;
        float baMove = _rot * m_TurnSpeed;
        bp.Vector3 BPdir = MultiplyQuaternion(BPtankHull.Orientation, new bp.Vector3(0, 0, 1));
        BPtankHull.LinearVelocity = new bp.Vector3(BPdir.X * bMovement, 0, BPdir.Z * bMovement);
        BPtankHull.AngularVelocity = new bp.Vector3(0, baMove, 0);

        BPtankHull.Orientation = EulerToQuat(new bp.Vector3(0, QuatToEuler(BPtankHull.Orientation).y, 0));
        BPtankHull.Position = new bp.Vector3(BPtankHull.Position.X, 0, BPtankHull.Position.Z);

        bp.Vector3 tPos = BPtankHull.Position;
        bp.Quaternion tquat = BPtankHull.Orientation;

        BPtankTrans.position = new Vector3(tPos.X, tPos.Y, tPos.Z);
        BPtankTrans.rotation = new Quaternion(tquat.X, tquat.Y, tquat.Z, tquat.W);



        frameDelta = deltaWatch.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency;
        deltaWatch.Stop();
        deltaWatch.Reset();

        space.Update(frameDelta);

        deltaWatch.Start();
    }

    private void CreateForwardWall() {
        ForwardWallTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(0, 0, 29), Quaternion.identity)).transform;
        ForwardWallTrans.localScale = new Vector3(126, 4, 2);
        ForwardWallTrans.GetComponent<MeshRenderer>().material.color = Color.red;

        ForwardWall = new Box(new bp.Vector3(0, 0, 29), 126, 4, 2);
        space.Add(ForwardWall);
    }

    private void CreateBackWall() {
        BackWallTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(0, 0, -29), Quaternion.identity)).transform;
        BackWallTrans.localScale = new Vector3(126, 4, 2);
        BackWallTrans.GetComponent<MeshRenderer>().material.color = Color.red;

        BackWall = new Box(new bp.Vector3(0, 0, -29), 126, 4, 2);
        space.Add(BackWall);
    }

    private void CreateRightWall() {
        RightWallTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(62.27f, 0, 0), Quaternion.identity)).transform;
        RightWallTrans.localScale = new Vector3(0.3836873f, 4, 60);
        RightWallTrans.GetComponent<MeshRenderer>().material.color = Color.red;

        RightWall = new Box(new bp.Vector3(62.27f, 0, 0), 0.3836873f, 4, 60);
        space.Add(RightWall);
    }

    private void CreateLeftWall() {
        LeftWallTrans = ((GameObject)Instantiate(boxPrefab, new Vector3(-62.27f, 0, 0), Quaternion.identity)).transform;
        LeftWallTrans.localScale = new Vector3(0.3836873f, 4, 60);
        LeftWallTrans.GetComponent<MeshRenderer>().material.color = Color.red;

        LeftWall = new Box(new bp.Vector3(-62.27f, 0, 0), 0.3836873f, 4, 60);
        space.Add(LeftWall);
    }

    public static bp.Vector3 MultiplyQuaternion(bp.Quaternion rotation, bp.Vector3 point) {
        bp.Vector3 vector3 = new bp.Vector3();
        float single = rotation.X * 2f;
        float single1 = rotation.Y * 2f;
        float single2 = rotation.Z * 2f;
        float single3 = rotation.X * single;
        float single4 = rotation.Y * single1;
        float single5 = rotation.Z * single2;
        float single6 = rotation.X * single1;
        float single7 = rotation.X * single2;
        float single8 = rotation.Y * single2;
        float single9 = rotation.W * single;
        float single10 = rotation.W * single1;
        float single11 = rotation.W * single2;
        vector3.X = (1f - (single4 + single5)) * point.X + (single6 - single11) * point.Y + (single7 + single10) * point.Z;
        vector3.Y = (single6 + single11) * point.X + (1f - (single3 + single5)) * point.Y + (single8 - single9) * point.Z;
        vector3.Z = (single7 - single10) * point.X + (single8 + single9) * point.Y + (1f - (single3 + single4)) * point.Z;
        return vector3;
    }

    public static Vector3 QuatToEuler(bp.Quaternion q) {
        float qw = q.W;
        float qx = q.X;
        float qy = q.Y;
        float qz = q.Z;

        float qw2 = qw * qw;
        float qx2 = qx * qx;
        float qy2 = qy * qy;
        float qz2 = qz * qz;

        float test = qx * qy + qz * qw;
        if (test > 0.499f) {
            return new Vector3(360 / Mathf.PI * Mathf.Atan2(qx, qw), 90, 0);
        }
        if (test < -0.499) {
            return new Vector3(-(360 / Mathf.PI * Mathf.Atan2(qx, qw)), -90, 0);
        }
        float h = Mathf.Atan2(2 * qy * qw - 2 * qx * qz, 1 - 2 * qy2 - 2 * qz2);
        float a = Mathf.Asin(2 * qx * qy + 2 * qz * qw);
        float b = Mathf.Atan2(2 * qx * qw - 2 * qy * qz, 1 - 2 * qx2 - 2 * qz2);
        return new Vector3(Mathf.Round(b * 180 / Mathf.PI), Mathf.Round(h * 180 / Mathf.PI), Mathf.Round(a * 180 / Mathf.PI));
    }

    public static bp.Quaternion EulerToQuat(bp.Vector3 euler) {
        double h = euler.Y * System.Math.PI / 360d;
        double a = euler.Z * System.Math.PI / 360d;
        double b = euler.X * System.Math.PI / 360d;

        double c1 = System.Math.Cos(h);
        double c2 = System.Math.Cos(a);
        double c3 = System.Math.Cos(b);
        double s1 = System.Math.Sin(h);
        double s2 = System.Math.Sin(a);
        double s3 = System.Math.Sin(b);

        float qw = (float)(System.Math.Round((c1 * c2 * c3 - s1 * s2 * s3) * 100000d) / 100000d);
        float qx = (float)(System.Math.Round((s1 * s2 * c3 + c1 * c2 * s3) * 100000d) / 100000d);
        float qy = (float)(System.Math.Round((s1 * c2 * c3 + c1 * s2 * s3) * 100000d) / 100000d);
        float qz = (float)(System.Math.Round((c1 * s2 * c3 - s1 * c2 * s3) * 100000d) / 100000d);
        return new bp.Quaternion(qx, qy, qz, qw);
    }

    void OnApplicationQuit() {
        _run = false;
    }
}
