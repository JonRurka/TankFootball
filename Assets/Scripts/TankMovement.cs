using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using Kinematics;

public class TankMovement : MonoBehaviour {
    public float m_Speed = 12f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
    public float acceloration = 2;
    public float syncSpeed = 5;
    public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
    public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
    public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
    public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.
    public float MPH = 0;
    public float Vel = 0;
    public float AngularVel = 0;
    public bool isOwner = false;
    public byte ID;
    public Vector2 turretRotation;
    public GameObject BallClamp;
    public TurretScript turrentInst;
    public bool HasBall;
    public bool Enabled = true;

    public Renderer leftTrackRend;
    public Renderer rightTrackRend;
    private float leftTexOffset = 0;
    private float rightTexOffset = 0;
    public float rotTrackSpeed = 4;

    private ushort sentNum = 0;
    private ushort receivedNum = 0;

    public Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
    private float lastSyncTime = 0f;
    public float syncDelay = 0f;
    private float syncTime = 0f;

    // current state for threading
    private TankState curMoveData;
    private TankState serverMoveData;
    private bool callSetState;


    private Vector3 curClientPosition;
    private Quaternion curClientRotation;
    private Vector3 predictedPos;
    private Quaternion predictedQuat;

    public ServerState serverState = new ServerState();

    private Vector3 ServerIntPosition;
    private Quaternion ServerIntRotation;


    public float sendCooldown = 1 / 30f;
    private float timer;

    private int calls = 0;
    public int callsPerSecond = 0;

    public float sentDelay = 0;

    private Vector3 targetRotation;
    private InputData lastInput;

    private Timer sendTimer;

    public float averagePing = 0;
    public List<int> pingList;
    private Dictionary<ushort, System.Diagnostics.Stopwatch> watches;
    private System.Diagnostics.Stopwatch syncWatch = new System.Diagnostics.Stopwatch();
    private System.Diagnostics.Stopwatch sentTimer = new System.Diagnostics.Stopwatch();

    float mov = 0;
    float rot = 0;

    private void Awake() {
        m_Rigidbody = transform.parent.GetComponent<Rigidbody>();
        turrentInst = GetComponentInChildren<TurretScript>();
        //SetCurData();
        syncTime = 0f;
        syncDelay = 0f;
        lastSyncTime = Time.time;
        sendTimer = new Timer();
    }

    private void Start() {
        pingList = new List<int>();
        watches = new Dictionary<ushort, System.Diagnostics.Stopwatch>();
        syncWatch.Start();
        sentTimer.Start();
        m_OriginalPitch = m_MovementAudio.pitch;
        if (isOwner) {
            MouseOrbitImproved.Instance.SetTarget(turrentInst.transform);
            //GetComponent<MeshCollider>().isTrigger = false;
            m_Rigidbody.isKinematic = false;
            //InvokeRepeating("SendInput", sendCooldown, sendCooldown);
            sendTimer.Elapsed += new ElapsedEventHandler(SendInput);
            sendTimer.Interval = sendCooldown * 1000;
            sendTimer.Enabled = true;
        }
        else {
            m_MovementAudio.volume = 1;
        }
        MouseOrbitImproved.Instance.SetInputEnable(true);
        serverState.Position = m_Rigidbody.position;
        serverState.Rotation = m_Rigidbody.rotation;
        serverState.CurRotation = m_Rigidbody.rotation;
        serverState.Velocity = 0;
        curClientPosition = m_Rigidbody.position;
        curClientRotation = m_Rigidbody.rotation;
        predictedPos = m_Rigidbody.position;
        predictedQuat = m_Rigidbody.rotation;
        lastInput = new InputData();
        InvokeRepeating("ResetCalls", 1, 1);
        InvokeRepeating("SetAveragePing", 0.1f, 0.1f);
        //GetComponent<MeshCollider>().me
    }

    private void SetAveragePing() {
        averagePing = GetPingAverage();
    }
    
    private void OnGUI() {
        if (!isOwner) {
            //GUI.Label(new Rect(Screen.width / 2 - 50, 20, 100, 20), "Vel: " + serverVel.magnitude);
            GUI.Label(new Rect(Screen.width / 2 - 50, 50, 100, 20), "Ping: " + averagePing);
            GUI.Label(new Rect(Screen.width / 2 - 50, 80, 100, 20), "Received/S: " + callsPerSecond);
        }
    }

    private void Update() {
        syncTime += Time.deltaTime;
        curClientPosition = m_Rigidbody.position;
        curClientRotation = m_Rigidbody.rotation;
        if (callSetState) {
            callSetState = false;
            //SetState(serverMoveData);
        }
        if (isOwner) {
            mov = 0;
            rot = 0;
            if (MouseOrbitImproved.Instance.inputEnabled) {
                mov = Input.GetAxisRaw("Vertical");
                rot = Input.GetAxisRaw("Horizontal");

                if (Input.GetKeyDown(KeyCode.G)) {
                    MainServerConnect.Instance.Send(ServerCMD.GetBall);
                }

                if (Input.GetKeyDown(KeyCode.K)) {
                    MainServerConnect.Instance.Send(ServerCMD.SetKick);
                }
            }

            float movement = mov * (m_Speed);
            Vector3 dir = (m_Rigidbody.rotation * Vector3.forward).normalized;
            //m_Rigidbody.velocity = new Vector3(dir.x * movement, 0, dir.z * movement);
            //m_Rigidbody.angularVelocity = new Vector3(0, rot * (m_TurnSpeed - (13.5f * Mathf.Deg2Rad)), 0);
            m_Rigidbody.velocity = Vector3.zero; //serverState.Direction * serverState.Velocity;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.position = new Vector3(m_Rigidbody.position.x, 0, m_Rigidbody.position.z);
            m_Rigidbody.rotation = Quaternion.Euler(new Vector3(0, m_Rigidbody.rotation.eulerAngles.y, 0));

            //AngularVel = m_Rigidbody.angularVelocity.y;
            //Vel = transform.InverseTransformDirection(m_Rigidbody.velocity).z;
            //MPH = Vel * 2.237f;

            Vel = serverState.Velocity;
            MPH = serverState.Velocity * 2.237f;

            /*if (serverState.Velocity > 0) {
                float distance = Vector3.Distance(m_Rigidbody.position, serverState.Position);
                float frameTravel = serverState.Velocity * Time.deltaTime;
                float t = MovementData.Scale(frameTravel, 0, distance, 0, 1);
                m_Rigidbody.position = Vector3.Lerp(m_Rigidbody.position, serverState.Position, t);
            }
            else {
                m_Rigidbody.position = Vector3.Lerp(m_Rigidbody.position, serverState.Position, Time.deltaTime * syncSpeed);
            }*/
            //Vector3 pos = m_Rigidbody.position;
            //Quaternion rot = m_Rigidbody.rotation;
            float t = syncTime / syncDelay;
            if (syncDelay == 0 || float.IsNaN(t)) {
                Debug.LogWarning("t is zer or nan!");
                t = 1;
            }
            if (IsQuatZero(serverState.CurRotation) && IsQuatZero(serverState.Rotation)) {
                serverState.Rotation = new Quaternion(0, 0, 0, 1);
            }
            Quaternion newRot = new Quaternion(0, 0, 0, 1);
            try {
                newRot = Quaternion.Lerp(serverState.CurRotation, serverState.Rotation, t); ;
            }
            catch(Exception) {
                Debug.LogErrorFormat("Invalid Quat: {0}, {1} = {2}", serverState.CurRotation, serverState.Rotation, newRot);
            }
            m_Rigidbody.position = Vector3.Lerp(serverState.CurPosition, serverState.Position, t);
            m_Rigidbody.rotation = newRot;


            //Debug.LogFormat("{0} / {1} = {2}", syncTime, syncDelay, syncTime / syncDelay);

            //Debug.LogFormat("{0} / {1} = {2}", syncTime, syncDelay, syncTime / syncDelay);
            //m_Rigidbody.position = Vector3.Lerp(syncStartPosition, predictedPos, syncTime / syncDelay);

            //m_Rigidbody.position = Vector3.Lerp(m_Rigidbody.position, ServerIntPosition, Time.deltaTime * syncSpeed);
            //m_Rigidbody.rotation = Quaternion.Lerp(m_Rigidbody.rotation, predictedQuat, Time.deltaTime * syncSpeed);

            //m_Rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
            //m_Rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);

            //m_Rigidbody.position = syncEndPosition;
            //m_Rigidbody.rotation = syncEndRotation;
        }
        else {
            if (IsQuatZero(serverState.CurRotation) && IsQuatZero(serverState.Rotation)) {
                serverState.Rotation = new Quaternion(0, 0, 0, 1);
            }
            m_Rigidbody.position = Vector3.Lerp(serverState.CurPosition, serverState.Position, syncTime / syncDelay);
            m_Rigidbody.rotation = Quaternion.Lerp(serverState.CurRotation, serverState.Rotation, syncTime / syncDelay);

        }
        EngineAudio();
        TrackAnimation();

        /*if (isOwner || GameControl.Instance.IsServer) {
            float mov = 0;
            float rot = 0;

            if (isOwner) {
                if (MouseOrbitImproved.Instance.inputEnabled) {
                    mov = Input.GetAxisRaw("Vertical");
                    rot = Input.GetAxisRaw("Horizontal");
                }
            }

            if (GameControl.Instance.IsServer) {
                if (!isOwner) {
                    if (lastInput.Forward)
                        mov = 1;
                    else if (lastInput.Backwards)
                        mov = -1;
                    if (lastInput.Right)
                        rot = 1;
                    else if (lastInput.Left)
                        rot = -1;
                }

                float movement = mov * (m_Speed + m_speedAdjust) * Time.deltaTime;
                m_Rigidbody.velocity = transform.TransformDirection(new Vector3(0, m_Rigidbody.velocity.y, movement));
                m_Rigidbody.angularVelocity = new Vector3(0, rot * m_TurnSpeed * Time.deltaTime, 0);
                m_Rigidbody.rotation = Quaternion.Euler(0, m_Rigidbody.rotation.eulerAngles.y, 0);
                AngularVel = m_Rigidbody.angularVelocity.y;
                Vel = transform.InverseTransformDirection(m_Rigidbody.velocity).z;
                MPH = Vel * 2.237f;

            }
            else {
                syncTime += Time.deltaTime;
                if (isOwner) {
                    //float movement = mov * (m_Speed + m_speedAdjust) * Time.deltaTime;
                    //m_Rigidbody.velocity = transform.TransformDirection(new Vector3(0, m_Rigidbody.velocity.y, movement));
                    //m_Rigidbody.angularVelocity = new Vector3(0, rot * m_TurnSpeed * Time.deltaTime, 0);
                    //m_Rigidbody.rotation = Quaternion.Euler(0, m_Rigidbody.rotation.eulerAngles.y, 0);

                    ServerIntPosition = Vector3.Lerp(syncEndPosition, predictedPos, syncTime / syncDelay);

                    m_Rigidbody.position = Vector3.Slerp(m_Rigidbody.position, syncEndPosition, Time.deltaTime * 5);
                    m_Rigidbody.rotation = Quaternion.Slerp(m_Rigidbody.rotation, syncEndRotation, Time.deltaTime * 5);

                    //Debug.DrawRay(predictedPos + Vector3.up, Vector3.down, Color.blue, 1f); // predicted position
                }
            }
            SetCurData();
        }
        else {
            if (syncDelay == 0) {
                m_Rigidbody.position = syncEndPosition;
                m_Rigidbody.rotation = syncEndRotation;
            }
            else {
                m_Rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
                m_Rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
            }
        }*/
    }

    private void OnEnable() {
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void OnDisable() {
        if (isOwner) {
            //sendTimer.Enabled = false;
            CancelInvoke();
        }
    }

    private void EngineAudio() {
        // If there is no input (the tank is stationary)...
        float sound = serverState.Power;
        float pitchRange = UnityEngine.Random.Range(-0.07f * sound, 0.07f * sound);
        m_MovementAudio.pitch = m_OriginalPitch + pitchRange + sound * 5/10;
        return;
        /*if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f) {
            // ... and if the audio source is currently playing the driving clip...
            if (m_MovementAudio.clip == m_EngineDriving) {
                // ... change the clip to idling and play it.
                m_MovementAudio.clip = m_EngineIdling;
                //m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
        else {
            // Otherwise if the tank is moving and if the idling clip is currently playing...
            if (m_MovementAudio.clip == m_EngineIdling) {
                // ... change the clip to driving and play.
                m_MovementAudio.clip = m_EngineDriving;
                //m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }*/
    }

    private void TrackAnimation() {

        if (serverState.Power > 0) {
            if (serverState.Velocity > 0.1f) {
                leftTexOffset += serverState.Velocity * Time.deltaTime;
                rightTexOffset += serverState.Velocity * Time.deltaTime;
            }
            else if (serverState.Velocity < -0.1f) {
                leftTexOffset += serverState.Velocity * Time.deltaTime;
                rightTexOffset += serverState.Velocity * Time.deltaTime;
            }
            else if (serverState.RotDir == RotationDir.Left) {
                leftTexOffset -= rotTrackSpeed * serverState.Power * Time.deltaTime;
                rightTexOffset += rotTrackSpeed * serverState.Power * Time.deltaTime;
            }
            else if (serverState.RotDir == RotationDir.Right) {
                leftTexOffset += rotTrackSpeed * serverState.Power * Time.deltaTime;
                rightTexOffset -= rotTrackSpeed * serverState.Power * Time.deltaTime;
            }

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }

        //float mov = Mathf.Clamp(0, -1, 1);
        //float rot = Mathf.Clamp(0, -1, 1);

        /*if (mov < 0) {
            rot *= -1;
            // backwards tracks
            leftTexOffset += trackSpeed * mov * Time.deltaTime;
            rightTexOffset += trackSpeed * mov * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (mov > 0) {
            // forwards tracks
            leftTexOffset += trackSpeed * mov * Time.deltaTime;
            rightTexOffset += trackSpeed * mov * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (rot < 0) {
            // left turn tracks
            leftTexOffset += trackSpeed * rot * Time.deltaTime;
            rightTexOffset -= trackSpeed * rot * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }
        else if (rot > 0) {
            // right turn tracks
            leftTexOffset -= trackSpeed * rot * Time.deltaTime;
            rightTexOffset += trackSpeed * rot * Time.deltaTime;

            leftTrackRend.material.SetTextureOffset("_MainTex", new Vector2(leftTexOffset, 0));
            leftTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(leftTexOffset, 0));

            rightTrackRend.material.SetTextureOffset("_MainTex", new Vector2(rightTexOffset, 0));
            rightTrackRend.material.SetTextureOffset("_BumpMap", new Vector2(rightTexOffset, 0));
        }*/

        if (leftTexOffset >= 10)
            leftTexOffset = 0;
        if (rightTexOffset >= 10)
            rightTexOffset = 0;
    }

    // called on owner.
    /*private void SendPosition() {
        MovementData moveData = GetPlayerData();
        byte[] sendBuffer = moveData.EncodedData;
        NetClient.Instance.Send(NetServer.OpCodes.SubmitPosition, sendBuffer, Protocal.Udp);
    }*/

    // called on owner.
    private void SendInput(object source, ElapsedEventArgs e) {
        sentDelay = (float)sentTimer.Elapsed.TotalSeconds;
        sentTimer.Reset();
        sentTimer.Start();
        sentNum++;
        InputData input = new InputData(mov, rot);
        DataEntries ent = new DataEntries();
        ent.AddEntry(sentNum);
        ent.AddEntry(input.GetByte());
        ent.AddEntry((byte)TankState.Scale(TankState.Repeat(turrentInst.turretRotation.x, 360), 0, 360, 0, 255));
        ent.AddEntry((byte)TankState.Scale(TankState.Repeat(turrentInst.turretRotation.y, 360), 0, 360, 0, 255));
        ent.AddEntry((ushort)GetPingAverage());
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        watches.Add(sentNum, watch);
        MainServerConnect.Instance.Send(ServerCMD.SubmitInput, ent.Encode(false), Protocal.Udp);
    }

    private void SendPosition(object source, ElapsedEventArgs e) {
        MainServerConnect.Instance.Send(ServerCMD.SubmitPosition, curMoveData.EncodedData, 
                                        MainServerConnect.Instance.udpEnabled ? Protocal.Udp : Protocal.Tcp);
    }

    public void SetPosition(Vector3 position) {
        m_Rigidbody.position = position;
    }

    public void SetRotation(Vector3 rotation) {
        m_Rigidbody.rotation = Quaternion.Euler(rotation);
    }

    public void SetEnabled(bool enabled) {
        Enabled = enabled;
        turrentInst.SetEnabled(enabled);
        //if (GameControl.Instance.IsServer)
        //    NetServer.Instance.Send(ID, NetClient.OpCodes.SetEnabled, new byte[] { ID, BitConverter.GetBytes(enabled)[0] });
    }

    // OLD
    // only called on server.
    public void SetInput(ushort sentNum, InputData input, Vector2 turrent) {
        if (GameControl.Instance.IsServer) {
            receivedNum = sentNum;
            lastInput = input;
            turretRotation = turrent;
            if (!isOwner) {
                turrentInst.SetTurretRotation(turrent);
                TankState state = GetPlayerData();
                byte[] sendBuff = BufferEdit.Add(BitConverter.GetBytes((Int16)receivedNum), state.EncodedData);
                //Debug.LogFormat("Server: {0} - {1}, {2}\n {3}", ID, receivedNum, state.ToString(), state.EncodedData);
                NetServer.Instance.Send(ID, NetClient.OpCodes.UpdateTankPosition, sendBuff, MainServerConnect.Instance.udpEnabled ? Protocal.Udp : Protocal.Tcp);
            }
           
        }
    }

    public TankState GetPlayerData() {
        return curMoveData;
    }

    // called on all but owner
    public void SetPlayerData(TankState data) {
        if (!isOwner) {
            serverMoveData = data;
            SetState(data);
            //callSetState = true;
            if (!GameControl.Instance.IsServer)
                turrentInst.SetTurretRotation(data.TurretRotation);
        }
    }

    // called on owner
    public void SetOwnerState(ushort receiveIndex, TankState data) {
        try {
            if (isOwner) {
                if (receivedNum <= receiveIndex) {
                    receivedNum = receiveIndex;
                    watches[receiveIndex].Stop();
                    AddPing(watches[receiveIndex].Elapsed.Milliseconds);
                    watches.Remove(receiveIndex);
                    serverMoveData = data;
                    SetState(data);
                }
                else {
                    watches.Remove(receiveIndex);
                }
            }
        }
        catch (KeyNotFoundException ex) {
            Debug.LogError("received index: " + receiveIndex);
        }
    }

    private void SetState(TankState data) {
        syncTime = 0f;
        //syncDelay = Time.time - lastSyncTime;
        //lastSyncTime = Time.time;
        syncDelay = (float)syncWatch.Elapsed.TotalSeconds/* - lastSyncTime*/;
        //lastSyncTime = (float)syncWatch.Elapsed.TotalSeconds;
        syncWatch.Reset();
        syncWatch.Start();

        calls++;

        if (data == null)
            return;

        if (data.Location.x == float.NaN) {
            SafeDebug.LogError(ID + ": Location.x NaN");
            return;
        }
        if (data.Location.y == float.NaN) {
            SafeDebug.LogError(ID + ": Location.y NaN");
            return;
        }
        if (data.Rotation == float.NaN) {
            SafeDebug.LogError(ID + ": Rotation NaN");
            return;
        }

        serverState = new ServerState();
        serverState.Position = new Vector3(data.Location.x, 0, data.Location.y);
        serverState.CurPosition = curClientPosition;
        serverState.Rotation = Quaternion.Euler(new Vector3(0, data.Rotation, 0));
        serverState.CurRotation = curClientRotation;
        serverState.Velocity = data.Velocity;
        serverState.Power = data.Power;
        Vector3 curDir = serverState.CurRotation * Vector3.forward;
        Vector3 nextDir = serverState.Rotation * Vector3.forward;
        float cross = Vector3.Cross(curDir, nextDir).y;
        if (cross > 0.01f)
            serverState.RotDir = RotationDir.Right;
        else if (cross < -0.01f)
            serverState.RotDir = RotationDir.Left;
        else
            serverState.RotDir = RotationDir.None;

        //Debug.LogFormat("{0}, {1}", serverState.Distance, serverState.Velocity);

            //syncStartPosition = m_Rigidbody.position;
            //syncEndPosition = new Vector3(data.Location.x, 0, data.Location.y)/* + new Vector3(data.Velocity.x, 0, data.Velocity.y) * syncDelay*/;

            //syncStartRotation = m_Rigidbody.rotation;
            //syncEndRotation = Quaternion.Euler(new Vector3(0, data.Rotation, 0));

            //Debug.LogFormat(syncEndRotation.eulerAngles.ToString());

            /*if (isOwner) {
                Quaternion currentQuat = Quaternion.AngleAxis(syncEndRotation.eulerAngles.y, Vector3.up);
                Vector3 currentDir = currentQuat * Vector3.forward;
                Direction = currentDir;

                Vel = data.Velocity;
                AngularVel = data.AngularVelecity;
                MPH = Vel * 2.237f;

                if (Mathf.Abs(AngularVel) > 0.05f) {
                    float predictedYrot = syncEndRotation.eulerAngles.y + ((AngularVel) * (syncDelay * 10));
                    predictedQuat = Quaternion.AngleAxis(predictedYrot, Vector3.up);
                    Vector3 predictedDir = predictedQuat * Vector3.forward;

                    float radius = Vel / AngularVel;
                    float angle = AngleDiff(predictedQuat.eulerAngles.y, syncEndRotation.eulerAngles.y);
                    float predictedDist = ((radius / 2) * Mathf.Sin(angle / 2f));
                    predictedPos = syncEndPosition + (predictedDir * predictedDist);
                }
                else {
                    predictedPos = syncEndPosition + (Direction * Vel * (syncDelay * 5));
                }
            }*/
    }

    public void ResetCalls() {
        callsPerSecond = calls;
        calls = 0;
    }

    private void AddPing(int ping) {
        if (pingList.Count >= 10) {
            pingList.RemoveAt(0);
            pingList.Add(ping);
        }
        else {
            pingList.Add(ping);
        }  
    }

    private float GetPingAverage() {
        int total = 0;
        for (int i = 0; i < pingList.Count; i++) {
            total += pingList[i];
        }
        return (total / (float)pingList.Count);
    }

    private void SetCurData() {
        curMoveData = new TankState(ID,
                            new Vector2(m_Rigidbody.position.x, m_Rigidbody.position.z),
                            transform.InverseTransformDirection(m_Rigidbody.velocity).z,
                            m_Rigidbody.rotation.eulerAngles.y, m_Rigidbody.angularVelocity.y,
                            turrentInst.turretRotation, 0);
    }

    private float AngleDiff(float a, float b) {
        float angle = a - b;
        if (angle > 180)
            angle -= 360;
        if (angle < -180)
            angle += 360;
        return angle;
    }

    private bool IsQuatZero(Quaternion quat) {
        return quat.x == 0 && quat.y == 0 && quat.z == 0 && quat.w == 0;
    }
}
