using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using BulletSharp;
//using bm = BulletSharp.Math;
using System.IO;
using System;

public class BulletTest : MonoBehaviour {
    public abstract class BulletTestBase {
        /*public Clock Clock { get; private set; }

        public float FrameDelta { get; private set; }
        public float FramesPerSecond { get; private set; }
        float _frameAccumulator;

        public DynamicsWorld World { get; protected set; }

        protected CollisionConfiguration CollisionConf;
        protected CollisionDispatcher Dispatcher;
        protected BroadphaseInterface Broadphase;
        protected ConstraintSolver Solver;
        public List<CollisionShape> CollisionShapes { get; private set; }

        protected BoxShape shootBoxShape;
        protected float shootBoxInitialSpeed = 40;
        BulletSharp.RigidBody pickedBody;
        protected TypedConstraint pickConstraint;
        float oldPickingDist;
        bool prevCanSleep;
        MultiBodyPoint2Point pickingMultiBodyPoint2Point;


        public BulletTestBase() {
            CollisionShapes = new List<CollisionShape>();
            Clock = new Clock();
        }

        public void Run() {
            OnInitialize();
            if (World == null) {
                OnInitializePhysics();
            }
        }

        protected virtual void OnInitialize() {
        }

        protected abstract void OnInitializePhysics();

        public virtual void ClientResetScene() {
            RemovePickingConstraint();
            ExitPhysics();
            OnInitializePhysics();
        }

        public virtual void ExitPhysics() {
            if (World != null) {
                //remove/dispose constraints
                int i;
                for (i = World.NumConstraints - 1;i >= 0; i--) {
                    TypedConstraint constraint = World.GetConstraint(i);
                    World.RemoveConstraint(constraint);
                    constraint.Dispose();
                }

                //remove the rigidbodies from the dynamics world and delete them
                for (i = World.NumCollisionObjects - 1; i >= 0; i--) {
                    CollisionObject obj = World.CollisionObjectArray[i];
                    BulletSharp.RigidBody body = obj as RigidBody;
                    if (body != null && body.MotionState != null) {
                        body.MotionState.Dispose();
                    }
                    World.RemoveCollisionObject(obj);
                    //obj.Dispose();
                }

                //delete collision shapes
                foreach (CollisionShape shape in CollisionShapes)
                    shape.Dispose();
                CollisionShapes.Clear();

                World.Dispose();
                Broadphase.Dispose();
                Dispatcher.Dispose();
                CollisionConf.Dispose();
            }

            if (Broadphase != null) {
                Broadphase.Dispose();
            }
            if (Dispatcher != null) {
                Dispatcher.Dispose();
            }
            if (CollisionConf != null) {
                CollisionConf.Dispose();
            }
        }

        public virtual void OnUpdate() {
            FrameDelta = Clock.GetFrameDelta();
            _frameAccumulator += FrameDelta;
            if (_frameAccumulator >= 1.0f) {
                FramesPerSecond = Clock.FrameCount / _frameAccumulator;
                _frameAccumulator = 0.0f;
                Clock.Reset();
            }

            if (World != null) {
                Clock.StartPhysics();
                World.StepSimulation(FrameDelta);
                Clock.StopPhysics();
            }
        }

        public void Dispose() {
            Dispose(true);
            //GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing)
                ExitPhysics();
        }

        public virtual void OnHandleInput() {
            return;
            if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1)) {
                bm.Vector3 rayTo = GetRayTo(Input.mousePosition, FreeLook.Instance.Eye, FreeLook.Instance.Target, FreeLook.Instance.cam.fieldOfView);
                if (Input.GetKey(KeyCode.Mouse1)) {
                    if (World != null) {
                        bm.Vector3 rayFrom = FreeLook.Instance.Eye;
                        ClosestRayResultCallback rayCallback = new ClosestRayResultCallback(ref rayFrom, ref rayTo);
                        World.RayTestRef(ref rayFrom, ref rayTo, rayCallback);
                        if (rayCallback.HasHit) {
                            bm.Vector3 pickPos = rayCallback.HitPointWorld;
                            RigidBody body = rayCallback.CollisionObject as RigidBody;
                            if (body != null) {
                                if (!(body.IsStaticObject || body.IsKinematicObject)) {
                                    pickedBody = body;
                                    pickedBody.ActivationState = ActivationState.DisableDeactivation;

                                    bm.Vector3 localPivot = bm.Vector3.TransformCoordinate(pickPos, bm.Matrix.Invert(body.CenterOfMassTransform));

                                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                                        Generic6DofConstraint dof6 = new Generic6DofConstraint(body, bm.Matrix.Translation(localPivot), false) {
                                            LinearLowerLimit = bm.Vector3.Zero,
                                            LinearUpperLimit = bm.Vector3.Zero,
                                            AngularLowerLimit = bm.Vector3.Zero,
                                            AngularUpperLimit = bm.Vector3.Zero
                                        };

                                        World.AddConstraint(dof6);
                                        pickConstraint = dof6;

                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 0);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 1);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 2);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 3);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 4);
                                        dof6.SetParam(ConstraintParam.StopCfm, 0.8f, 5);

                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 0);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 1);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 2);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 3);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 4);
                                        dof6.SetParam(ConstraintParam.StopErp, 0.1f, 5);
                                    }
                                    else {
                                        Point2PointConstraint p2p = new Point2PointConstraint(body, localPivot);
                                        World.AddConstraint(p2p);
                                        pickConstraint = p2p;
                                        p2p.Setting.ImpulseClamp = 30;
                                        //very weak constraint for picking
                                        p2p.Setting.Tau = 0.001f;
                                    }
                                }
                            }
                            else {
                                MultiBodyLinkCollider multiCol = rayCallback.CollisionObject as MultiBodyLinkCollider;
                                if (multiCol != null && multiCol.MultiBody != null) {
                                    MultiBody mb = multiCol.MultiBody;

                                    prevCanSleep = mb.CanSleep;
                                    mb.CanSleep = false;
                                    bm.Vector3 pivotInA = mb.WorldPosToLocal(multiCol.Link, pickPos);

                                    MultiBodyPoint2Point p2p = new MultiBodyPoint2Point(mb, multiCol.Link, null, pivotInA, pickPos);
                                    p2p.MaxAppliedImpulse = 2;

                                    (World as MultiBodyDynamicsWorld).AddMultiBodyConstraint(p2p);
                                    pickingMultiBodyPoint2Point = p2p;
                                }
                            }
                            oldPickingDist = (pickPos - rayFrom).Length;
                        }
                        rayCallback.Dispose();
                    }
                }
            } 
            else if (Input.GetMouseButtonUp(1)) {
                RemovePickingConstraint();
            }

            if (Input.GetMouseButton(1)) {
                MovePickedBody();
            }
        }

        private void MovePickedBody() {
            if (pickConstraint != null) {
                bm.Vector3 rayFrom = FreeLook.Instance.Eye;
                bm.Vector3 camTarget = FreeLook.Instance.Target;
                bm.Vector3 newRayTo = GetRayTo(Input.mousePosition, rayFrom, camTarget, FreeLook.Instance.cam.fieldOfView);

                bm.Vector3 dir = newRayTo - rayFrom;
                dir.Normalize();
                dir *= oldPickingDist;

                if (pickConstraint.ConstraintType == TypedConstraintType.D6) {
                    Generic6DofConstraint pickCon = pickConstraint as Generic6DofConstraint;

                    bm.Matrix tempFrameOffsetA = pickCon.FrameOffsetA;
                    tempFrameOffsetA.Origin = rayFrom + dir;
                    pickCon.SetFrames(tempFrameOffsetA, pickCon.FrameOffsetB);
                }
                else {
                    Point2PointConstraint pickCon = pickConstraint as Point2PointConstraint;

                    pickCon.PivotInB = rayFrom + dir;
                }
            }
            else if (pickingMultiBodyPoint2Point != null) {
                bm.Vector3 rayFrom = FreeLook.Instance.Eye;
                bm.Vector3 camTarget = FreeLook.Instance.Target;
                bm.Vector3 newRayTo = GetRayTo(Input.mousePosition, rayFrom, camTarget, FreeLook.Instance.cam.fieldOfView);

                bm.Vector3 dir = (newRayTo - rayFrom);
                dir.Normalize();
                dir *= oldPickingDist;
                pickingMultiBodyPoint2Point.PivotInB = rayFrom + dir;
            }
        }

        private void RemovePickingConstraint() {
            if (pickConstraint != null && World != null) {
                World.RemoveConstraint(pickConstraint);
                pickConstraint.Dispose();
                pickConstraint = null;
                pickedBody.ForceActivationState(ActivationState.ActiveTag);
                pickedBody.DeactivationTime = 0;
                pickedBody = null;
            }

            if (pickingMultiBodyPoint2Point != null) {
                pickingMultiBodyPoint2Point.MultiBodyA.CanSleep = prevCanSleep;
                (World as MultiBodyDynamicsWorld).RemoveMultiBodyConstraint(pickingMultiBodyPoint2Point);
                pickingMultiBodyPoint2Point.Dispose();
                pickingMultiBodyPoint2Point = null;
            }
        }

        protected bm.Vector3 GetRayTo(Vector3 point, bm.Vector3 eye, bm.Vector3 target, float fov) {
            float aspect;

            bm.Vector3 rayForward = target - eye;
            rayForward.Normalize();
            const float farPlane = 10000.0f;
            rayForward *= farPlane;

            bm.Vector3 vertical = FreeLook.Instance.Up;

            bm.Vector3 hor = bm.Vector3.Cross(rayForward, vertical);
            hor.Normalize();
            vertical = bm.Vector3.Cross(hor, rayForward);
            vertical.Normalize();

            float tanFov = (float)Math.Tan(fov / 2);
            hor *= 2.0f * farPlane * tanFov;
            vertical *= 2.0f * farPlane * tanFov;

            if (Screen.width > Screen.height) {
                aspect = (float)Screen.width / (float)Screen.height;
                hor *= aspect;
            }
            else {
                aspect = (float)Screen.height / (float)Screen.width;
            }

            bm.Vector3 rayToCenter = eye + rayForward;
            bm.Vector3 dHor = hor / (float)Screen.width;
            bm.Vector3 dVert = vertical / (float)Screen.height;

            bm.Vector3 rayTo = rayToCenter - 0.5f * hor + 0.5f * vertical;
            rayTo += (Screen.width - point.x) * dHor;
            rayTo -= point.y * dVert;
            return rayTo;
        }

        public virtual void ShootBox(bm.Vector3 camPos, bm.Vector3 destination) {
            if (World == null)
                return;

            const float mass = 1.0f;

            if (shootBoxShape == null)
                shootBoxShape = new BoxShape(1.0f);

            BulletSharp.RigidBody body = LocalCreateRigidBody(mass, bm.Matrix.Translation(camPos), shootBoxShape);
            body.LinearFactor = new bm.Vector3(1, 1, 1);

            bm.Vector3 linVel = destination - camPos;
            linVel.Normalize();

            body.LinearVelocity = linVel * shootBoxInitialSpeed;
            body.CcdMotionThreshold = 0.5f;
            body.CcdSweptSphereRadius = 0.9f;
        }

        public virtual BulletSharp.RigidBody LocalCreateRigidBody(float mass, bm.Matrix startTransform, CollisionShape shape) {
            bool isDynamic = (mass != 0.0f);

            bm.Vector3 localInertia = bm.Vector3.Zero;
            if (isDynamic)
                shape.CalculateLocalInertia(mass, out localInertia);

            DefaultMotionState motionState = new DefaultMotionState(startTransform);

            RigidBodyConstructionInfo rbInfo = new RigidBodyConstructionInfo(mass, motionState, shape, localInertia);
            rbInfo.Friction = 0.6f;
            rbInfo.RollingFriction = 0.6f;
            BulletSharp.RigidBody body = new BulletSharp.RigidBody(rbInfo);
            rbInfo.Dispose();

            World.AddRigidBody(body);
            return body; 
        }

        public static bm.Vector3 MultiplyQuaternion(bm.Quaternion rotation, bm.Vector3 point) {
            bm.Vector3 vector3 = new bm.Vector3();
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
        }*/
    }

    public class BasicTest : BulletTestBase {
        /*const int ArraySizeX = 5, ArraySizeY = 5, ArraySizeZ = 5;
        bm.Vector3 startPosition = new bm.Vector3(0, 2, 0);

        private Transform groundTrans;
        private Transform[] boxes;
        private RigidBody[] bodies;

        protected override void OnInitialize() {
            
        }

        protected override void OnInitializePhysics() {
            CollisionConf = new DefaultCollisionConfiguration();
            Dispatcher = new CollisionDispatcher(CollisionConf);

            Broadphase = new DbvtBroadphase();

            World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, null, CollisionConf);
            World.Gravity = new bm.Vector3(5, -10, 2);

            CreateGround();
            CreateBoxes();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (boxes != null && bodies != null) {
                for (int i = 0; i < boxes.Length; i++) {
                    bm.Vector3 massCenter = bodies[i].CenterOfMassPosition;
                    boxes[i].position = new Vector3(massCenter.X, massCenter.Y, massCenter.Z);
                    bm.Quaternion quat = bodies[i].Orientation;
                    boxes[i].rotation = new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
                }
            }
        }

        private void CreateGround() {
            Debug.Log("Ground create.");
            groundTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, Vector3.zero, Quaternion.identity)).transform;
            groundTrans.localScale = new Vector3(100, 2, 100);

            var groundShape = new BoxShape(50, 1, 50);

            CollisionShapes.Add(groundShape);
            CollisionObject ground = LocalCreateRigidBody(0, bm.Matrix.Identity, groundShape);
            ground.UserObject = "Ground";
        }

        private void CreateBoxes() {
            Debug.Log("box create.");

            const float mass = 1.0f;
            var colshape = new BoxShape(1);
            CollisionShapes.Add(colshape);
            bm.Vector3 localInertia = colshape.CalculateLocalInertia(mass);

            var rbInfo = new RigidBodyConstructionInfo(mass, null, colshape, localInertia);

            boxes = new Transform[ArraySizeX * ArraySizeY * ArraySizeZ];
            bodies = new RigidBody[ArraySizeX * ArraySizeY * ArraySizeZ];

            for (int y = 0; y < ArraySizeY; y++) {
                for (int x = 0; x < ArraySizeX; x++) {
                    for (int z = 0; z < ArraySizeZ; z++) {
                        int index = LinearIndex(x, y, z);

                        bm.Vector3 position = startPosition + 3 * new bm.Vector3(x, y, z);
                        position += new bm.Vector3(0, 10, 0);

                        boxes[index] = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(position.X, position.Y, position.Z), Quaternion.identity)).transform;
                        boxes[index].localScale = new Vector3(2, 2, 2);

                        rbInfo.MotionState = new DefaultMotionState(bm.Matrix.Translation(position));
                        var body = new RigidBody(rbInfo);

                        World.AddRigidBody(body);
                        bodies[index] = body;
                    }
                }
            }
        }

        private int LinearIndex(int x, int y, int z) {
            return x + ArraySizeY * (y + ArraySizeZ * z);
        }*/
    }

    public class TankTest : BulletTestBase {
        /*public float m_Speed = 12f;
        public float m_TurnSpeed = 2.5f;

        public Transform tankObj { get; private set; }
        public Transform groundTrans { get; private set; }
        public Transform ForwardWallTrans;
        public Transform BackWallTrans;
        public Transform RightWallTrans;
        public Transform LeftWallTrans;

        //private CollisionObject ground;
        //private CollisionObject ForwardWall;
        //private CollisionObject BackWall;
        //private CollisionObject RightWall;
        //private CollisionObject LeftWall;
        //private RigidBody tank;

        private Vector3[] tankVerts;
        private int[] tankTriangles;

        public TankTest(Vector3[] verts, int[] triangles) : base() {
            tankVerts = verts;
            tankTriangles = triangles;
        }

        protected override void OnInitializePhysics() {
            //CollisionConf = new DefaultCollisionConfiguration();
            //Dispatcher = new CollisionDispatcher(CollisionConf);

            //Broadphase = new DbvtBroadphase();

            //World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, null, CollisionConf);
            //World.Gravity = new bm.Vector3(0, -9.81f, 0);

            CreateGround();
            CreateForwardWall();
            CreateBackWall();
            CreateRightWall();
            CreateLeftWall();
            CreateTank(tankVerts, tankTriangles);
        }

        public override void OnUpdate() {
            base.OnUpdate();
            if (tankObj != null) {
                float mov = Input.GetAxis("Vertical");
                float rot = Input.GetAxis("Horizontal");

                if (mov < -0.01f)
                    rot *= -1;

                float aMov = rot * m_TurnSpeed;
                Debug.Log(aMov);
                float movement = mov * m_Speed;
                //bm.Quaternion quat = tank.Orientation;
                //bm.Vector3 dir = MultiplyQuaternion(quat, new bm.Vector3(0, 0, 1));
                //tank.LinearVelocity = new bm.Vector3(dir.X * movement, tank.LinearVelocity.Y, dir.Z * movement);
                //tank.AngularVelocity = new bm.Vector3(0, aMov, 0);
                UpdateUnityObjects();
            }
        }

        private void UpdateUnityObjects() {
            //bm.Vector3 massCenter = tank.CenterOfMassPosition;
            //tankObj.position = new Vector3(massCenter.X, massCenter.Y, massCenter.Z);
            //bm.Quaternion quat = tank.Orientation;
            //tankObj.rotation = new Quaternion(quat.X, quat.Y, quat.Z, quat.W);

        }

        private void CreateGround() {
            Debug.Log("Ground create.");
            groundTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(0, -0.5f, 0), Quaternion.identity)).transform;
            groundTrans.localScale = new Vector3(126, 1, 60);

            var groundShape = new BoxShape(63, 0.5f, 30);
            CollisionShapes.Add(groundShape);
            ground = LocalCreateRigidBody(0, bm.Matrix.Translation(new bm.Vector3(0, -0.5f, 0)), groundShape);
            ground.UserObject = "groundTrans";
        }

        // pos = (0,0,30)
        private void CreateForwardWall() {
            Debug.Log("ForwardWall create.");
            ForwardWallTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(0, 0, 29), Quaternion.identity)).transform;
            ForwardWallTrans.localScale = new Vector3(126, 4, 2);

            //var wallShape = new BoxShape(63, 2, 1);
            //CollisionShapes.Add(wallShape);
            //ForwardWall = LocalCreateRigidBody(0, bm.Matrix.Translation(new bm.Vector3(0, 0, 29)), wallShape);
            //ForwardWall.UserObject = "ForwardWallTrans";
        }

        // pos = (0,0,-30)
        private void CreateBackWall() {
            Debug.Log("ForwardWall create.");
            BackWallTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(0, 0, -29), Quaternion.identity)).transform;
            BackWallTrans.localScale = new Vector3(126, 4, 2);

            //var wallShape = new BoxShape(63, 2, 1);
            //CollisionShapes.Add(wallShape);
            //BackWall = LocalCreateRigidBody(0, bm.Matrix.Translation(new bm.Vector3(0, 0, -29)), wallShape);
            //BackWall.UserObject = "BackWallTrans";
        }

        //pos = (62.27,0,0)
        private void CreateRightWall() {
            Debug.Log("RightWall create.");
            RightWallTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(62.27f, 0, 0), Quaternion.identity)).transform;
            RightWallTrans.localScale = new Vector3(0.3836873f, 4, 60);

            //var wallShape = new BoxShape(0.19184365f, 2, 30);
            //CollisionShapes.Add(wallShape);
            //RightWall = LocalCreateRigidBody(0, bm.Matrix.Translation(new bm.Vector3(62.27f, 0, 0)), wallShape);
            //RightWall.UserObject = "RightWallTrans";
        }

        //pos = (-62.27,0,0)
        private void CreateLeftWall() {
            Debug.Log("RightWall create.");
            LeftWallTrans = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(-62.27f, 0, 0), Quaternion.identity)).transform;
            LeftWallTrans.localScale = new Vector3(0.3836873f, 4, 60);

            //var wallShape = new BoxShape(0.19184365f, 2, 30);
            //CollisionShapes.Add(wallShape);
            //LeftWall = LocalCreateRigidBody(0, bm.Matrix.Translation(new bm.Vector3(-62.27f, 0, 0)), wallShape);
            //LeftWall.UserObject = "LeftWallTrans";
        }

        public void CreateTank() {
            //GameObject phsxObj = (GameObject)Instantiate(Instance.TestCubePhysxPrefab, new Vector3(0, 2, 0), Quaternion.identity);
            //phsxObj.transform.localScale = new Vector3(1f, 1f, 2);
            //phsxObj.GetComponent<MeshRenderer>().material.color = Color.blue;

            tankObj = ((GameObject)Instantiate(Instance.TestCubeBulletPrefab, new Vector3(0, 2, 0), Quaternion.identity)).transform;
            tankObj.localScale = new Vector3(1f, 1f, 2);
            tankObj.GetComponent<MeshRenderer>().material.color = Color.red;

            FreeLook.Instance.transform.parent = tankObj;
            FreeLook.Instance.transform.localPosition = new Vector3(0, 4, -8);

            const float mass = 1.0f;
            var TankShape = new BoxShape(0.5f, 0.5f, 1f);
            CollisionShapes.Add(TankShape);
            bm.Vector3 localInertia = TankShape.CalculateLocalInertia(mass);
            var rbInfo = new RigidBodyConstructionInfo(mass, null, TankShape, localInertia);
            rbInfo.MotionState = new DefaultMotionState(bm.Matrix.Translation(new bm.Vector3(0, 2, 0)));
            rbInfo.Friction = 0.6f;
            rbInfo.RollingFriction = 0.6f;
            tank = new RigidBody(rbInfo);
            World.AddRigidBody(tank);

            Quaternion quat = Quaternion.Euler(0, 90, 0);
            bm.Quaternion bulletQuat = new bm.Quaternion(quat.x, quat.y, quat.z, quat.w);
            tank.WorldTransform = bm.Matrix.RotationQuaternion(bulletQuat);

            Debug.LogFormat("bullet friction: {0}, {1}", tank.Friction, tank.RollingFriction);
        }

        public void CreateTank(Vector3[] verts, int[] triangles) {
            tankObj = ((GameObject)Instantiate(Instance.TestTankPrefab, new Vector3(0, 4, 0), Quaternion.identity)).transform;

            FreeLook.Instance.transform.parent = tankObj;
            FreeLook.Instance.transform.localPosition = new Vector3(0, 1, -2);

            const float mass = 7600.0f;
            CollisionShape shape = GetTankShape(verts, triangles);
            bm.Vector3 localInertia = shape.CalculateLocalInertia(mass);
            var rbInfo = new RigidBodyConstructionInfo(mass, null, shape, localInertia);
            rbInfo.MotionState = new DefaultMotionState(bm.Matrix.Translation(new bm.Vector3(0, 4, 0)));
            rbInfo.Friction = 0.6f;
            rbInfo.RollingFriction = 0.6f;
            tank = new RigidBody(rbInfo);
            World.AddRigidBody(tank);

            Quaternion quat = Quaternion.Euler(0, 90, 0);
            bm.Quaternion bulletQuat = new bm.Quaternion(quat.x, quat.y, quat.z, quat.w);
            tank.WorldTransform = bm.Matrix.RotationQuaternion(bulletQuat);
        }

        public CollisionShape GetTankShape(Vector3[] verts, int[] triangles) {
            IndexedMesh mesh = new IndexedMesh();
            mesh.Allocate(triangles.Length, verts.Length);
            using (BinaryWriter triWriter = new BinaryWriter(mesh.GetTriangleStream())) {
                for (int i = 0; i < triangles.Length; i++) {
                    triWriter.Write(triangles[i]);
                }
            }
            using (BinaryWriter vertWriter = new BinaryWriter(mesh.GetVertexStream())) {
                for (int i = 0; i < verts.Length; i++) {
                    vertWriter.Write(verts[i].x);
                    vertWriter.Write(verts[i].y);
                    vertWriter.Write(verts[i].z);
                }
            }

            TriangleIndexVertexArray vertexArrays = new TriangleIndexVertexArray();
            vertexArrays.AddIndexedMesh(mesh);
            const bool useQuantizedAabbCompression = true;
            CollisionShape shape = new BvhTriangleMeshShape(vertexArrays, useQuantizedAabbCompression);
            CollisionShapes.Add(shape);
            return shape;
        }*/
    }

    public static BulletTest Instance;

    public GameObject TestCubeBulletPrefab;
    public GameObject TestCubePhysxPrefab;
    public GameObject TestTankPrefab;

    private BulletTestBase Test;
    private Transform[] trasforms;



    // Use this for initialization
    void Start () {
        Instance = this;
        Mesh tankMesh = TestTankPrefab.GetComponent<MeshFilter>().sharedMesh;
        //Test = new TankTest(tankMesh.vertices, tankMesh.triangles);
        //Test.Run();
    }
	
	// Update is called once per frame
	void Update () {
    }

    void FixedUpdate() {
        //Test.OnUpdate();
    }

    void OnApplicationQuit() {
        //Test.Dispose();
        Debug.Log("Bullet physics closed.");
    }

    void SaveMesh() {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string file = folder + "\\TankBody.obj";
        string tankMeshStr = ObjExporter.MeshToString(TestTankPrefab.GetComponent<MeshFilter>());
        StreamWriter writer = File.CreateText(file);
        writer.Write(tankMeshStr);
        writer.Close();
    }
}
