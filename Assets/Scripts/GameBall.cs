using UnityEngine;
using System;
using System.Collections;
using Kinematics;

public class GameBall : MonoBehaviour {
    /*public class BallMovementData {
        public const int SendBufferLenght = 18;

        public Vector3 Location { get; private set; }
        public Vector3 Rotation { get; private set; }
        public Vector3 Velocity { get; private set; }
        public byte[] EncodedData { get; private set; }

        public BallMovementData(Vector3 location, Vector3 rotation) {
            this.Location = location;
            this.Rotation = rotation;
            this.EncodedData = Encode();
        }

        public BallMovementData(byte[] encodedData) {
            this.EncodedData = encodedData;
            Decode();
        }

        public byte[] Encode() {
            Int16 psx = MovementData.ToShort(Location.x, 500); // position x
            Int16 psy = MovementData.ToShort(Location.y, 500); // position y
            Int16 psz = MovementData.ToShort(Location.z, 500); // position z
            UInt16 rsx = MovementData.ToUnsighedShort(MovementData.Repeat(Rotation.x, 360), 90); // rotation x
            UInt16 rsy = MovementData.ToUnsighedShort(MovementData.Repeat(Rotation.y, 360), 90); // rotation y
            UInt16 rsz = MovementData.ToUnsighedShort(MovementData.Repeat(Rotation.z, 360), 90); // rotation z
            Int16 vsx = MovementData.ToShort(Velocity.x, 600); // Velocity x
            Int16 vsy = MovementData.ToShort(Velocity.y, 600); // Velocity y
            Int16 vsz = MovementData.ToShort(Velocity.z, 600); // Velocity z

            byte[] pbx = MovementData.ToByte(psx);
            byte[] pby = MovementData.ToByte(psy);
            byte[] pbz = MovementData.ToByte(psz);
            byte[] rbx = MovementData.ToByte(rsx);
            byte[] rby = MovementData.ToByte(rsy);
            byte[] rbz = MovementData.ToByte(rsz);
            byte[] vbx = MovementData.ToByte(vsx);
            byte[] vby = MovementData.ToByte(vsy);
            byte[] vbz = MovementData.ToByte(vsz);

            byte[] sendBuffer = new byte[SendBufferLenght];

            sendBuffer[0] = pbx[0];
            sendBuffer[1] = pbx[1];
            sendBuffer[2] = pby[0];
            sendBuffer[3] = pby[1];
            sendBuffer[4] = pbz[0];
            sendBuffer[5] = pbz[1];
            sendBuffer[6] = rbx[0];
            sendBuffer[7] = rbx[1];
            sendBuffer[8] = rby[0];
            sendBuffer[9] = rby[1];
            sendBuffer[10] = rbz[0];
            sendBuffer[11] = rbz[1];
            sendBuffer[12] = vbx[0];
            sendBuffer[13] = vbx[1];
            sendBuffer[14] = vby[0];
            sendBuffer[15] = vby[1];
            sendBuffer[16] = vbz[0];
            sendBuffer[17] = vbz[1];

            return sendBuffer;
        }

        public void Decode() {
            Int16 psx = MovementData.FromByte(new byte[] { EncodedData[0], EncodedData[1] });
            Int16 psy = MovementData.FromByte(new byte[] { EncodedData[2], EncodedData[3] });
            Int16 psz = MovementData.FromByte(new byte[] { EncodedData[4], EncodedData[5] });
            UInt16 rsx = MovementData.FromByteUnsigned(new byte[] { EncodedData[6], EncodedData[7] });
            UInt16 rsy = MovementData.FromByteUnsigned(new byte[] { EncodedData[8], EncodedData[9] });
            UInt16 rsz = MovementData.FromByteUnsigned(new byte[] { EncodedData[10], EncodedData[11] });
            Int16 vsx = MovementData.FromByte(new byte[] { EncodedData[12], EncodedData[13] });
            Int16 vsy = MovementData.FromByte(new byte[] { EncodedData[14], EncodedData[15] });
            Int16 vsz = MovementData.FromByte(new byte[] { EncodedData[16], EncodedData[17] });

            Location = new Vector3(MovementData.ToFloat(psx, 500), MovementData.ToFloat(psy, 500), MovementData.ToFloat(psz, 500));
            Rotation = new Vector3(MovementData.ToFloat(rsx, 90), MovementData.ToFloat(rsy, 90), MovementData.ToFloat(rsz, 90));
            Velocity = new Vector3(MovementData.ToFloat(vsx, 600), MovementData.ToFloat(vsy, 600), MovementData.ToFloat(vsz, 600));
        }
    }*/

    public static GameBall Instance;

    public Player curHolder;
    public GameObject teePrefab;

    public bool playerHasBall;

    private float lastSyncTime = 0f;
    private float syncDelay = 0f;
    private float syncTime = 0f;
    private Vector3 syncStartPosition;
    private Vector3 syncEndPosition;
    private Quaternion syncStartRotation;
    private Quaternion syncEndRotation;
    //private Rigidbody m_Rigidbody;
    private GameObject teeObj;
    private CapsuleCollider col;

    public float sendCooldown = 1 / 20f;
    private float timer;

    void Awake() {
        Instance = this;
    }

    // Use this for initialization
    void Start () {
        //NetClient.Instance.AddCommands(this);
        MainServerConnect.Instance.AddCommands(this);
        //m_Rigidbody = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        //if (!GameControl.Instance.IsServer)
        syncStartPosition = transform.position;
        syncEndPosition = transform.position;
        syncStartRotation = transform.rotation;
        syncEndRotation = transform.rotation;
    }
	
	// Update is called once per frame
	void Update () {
        try {
            if (playerHasBall) {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(new Vector3(90, 0, 0));
                syncStartPosition = transform.position;
                syncEndPosition = transform.position;
                syncStartRotation = transform.rotation;
                syncEndRotation = transform.rotation;
            }
            else {
                syncTime += Time.deltaTime;
                transform.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
                transform.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
            }


            /*if (playerHasBall) {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
            else {
                if (GameControl.Instance.IsServer) {
                    if (timer > 0)
                        timer -= Time.deltaTime;
                    if (timer < 0)
                        timer = 0;
                    if (timer == 0) {
                        timer = sendCooldown;
                        byte[] sendBuffer = GetMoveData().EncodedData;
                        NetServer.Instance.Send(NetClient.OpCodes.UpdateBallPos, sendBuffer);
                    }
                }
                else {
                    syncTime += Time.deltaTime;
                    m_Rigidbody.position = Mathfx.Hermite(syncStartPosition, syncEndPosition, syncTime / syncDelay);
                    m_Rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
                }
            }*/
        }
        catch(Exception e) {
            Debug.LogFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
        }
    }

    // OLD
    public BallStateData GetMoveData() {
        return new BallStateData(transform.position, transform.rotation);
    }

    public void SetMoveData(BallStateData data) {
        if (!playerHasBall) {
            syncTime = 0f;
            syncDelay = Time.time - lastSyncTime;
            lastSyncTime = Time.time;

            syncStartPosition = transform.position;
            syncEndPosition = new Vector3(data.Location.x, data.Location.y, data.Location.z)/* + new Vector3(data.Velocity.x, 0, data.Velocity.y) * syncDelay*/;

            syncStartRotation = transform.rotation;
            syncEndRotation = data.Rotation;
        }
    }

    public void SetLocation(Vector3 location) {
        transform.position = location;
    }

    public void SetRotation(Vector3 rotation) {
        transform.rotation = Quaternion.Euler(rotation);
    }

    // OLD
    public void Fire(Vector3 tankVelocity, Vector3 velocity) {
        if (playerHasBall && curHolder != null) {
            FreeBall();
            Vector3 globalVelocity = tankVelocity + transform.TransformDirection(velocity);
            //transform.velocity = globalVelocity;
        }
    }

    // OLD
    public void GiveBall(byte id) {
        //m_Rigidbody.isKinematic = true;
        curHolder = GameControl.Instance.GetUser(id);
        if (curHolder != null) {
            playerHasBall = true;
            curHolder.Instance.HasBall = true;
            curHolder.Instance.turrentInst.GiveBall();
            transform.parent = curHolder.Instance.BallClamp.transform;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            GetComponent<CapsuleCollider>().enabled = false;
        }
        NetServer.Instance.Send(NetClient.OpCodes.GiveBall, new byte[] { id });
    }

    // OLD
    public void UpdateState(byte player) {
        if (playerHasBall) {
            //m_Rigidbody.isKinematic = true;
            NetServer.Instance.Send(player, NetClient.OpCodes.GiveBall, new byte[] { curHolder.ID });
        }
    }

    // OLD
    public void FreeBall() {
        //m_Rigidbody.isKinematic = false;
        if (curHolder != null) {
            curHolder.Instance.HasBall = false;
            curHolder.Instance.turrentInst.RemoveBall();
            curHolder = null;
        }
        playerHasBall = false;
        transform.parent = null;
        GetComponent<CapsuleCollider>().enabled = true;
        NetServer.Instance.Send(NetClient.OpCodes.FreeBall);
    }

    // OLD
    public void SpawnTee(float xLocation, int orientation) {
        if (orientation > 0)
            orientation = 1;
        else if (orientation < 0)
            orientation = -1;
        else
            throw new Exception("orientation cannot be 0.");

        if (playerHasBall)
            FreeBall();

        RemoveTee();
        teeObj = (GameObject)Instantiate(teePrefab, new Vector3(xLocation, 0.07f, 0), Quaternion.identity);

        float distance = orientation * 0.1156f;
        float xRot = orientation * 65.7798f;

        //m_Rigidbody.velocity = Vector3.zero;
        //m_Rigidbody.angularVelocity = Vector3.zero;
        //m_Rigidbody.position = new Vector3(xLocation + distance, 0.1851f, 0);
        //m_Rigidbody.rotation = Quaternion.Euler(new Vector3(xRot, 90, 0));
    }

    // OLD
    public void RemoveTee() {
        if (teeObj != null) {
            Destroy(teeObj);
        }
    }

    [Command(ClientCMD.UpdateBallPos)] // udp
    public void UpdateBallPos_CMD(Data data) {
        SetMoveData(new BallStateData(data.Buffer));
    }

    [Command(ClientCMD.SetBallState)] // tcp
    public void UpdateBallState_CMD(Data data) {
        if (data.Buffer[0] == 0) { // Give Ball
            byte id = data.Buffer[1];
            curHolder = GameControl.Instance.GetUser(id);
            if (curHolder != null) {
                playerHasBall = true;
                curHolder.Instance.HasBall = true;
                transform.parent = curHolder.Instance.BallClamp.transform;
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = new Vector3(90, 0, 0);
                col.enabled = false;
            }
        }
        else if (data.Buffer[0] == 1) { // Free Ball
            if (curHolder != null) {
                if (curHolder.TankExists) {
                    curHolder.Instance.HasBall = false;
                    curHolder.Instance.turrentInst.RemoveBall();
                }
            }
            playerHasBall = false;
            transform.parent = null;
            col.enabled = true;
            curHolder = null;
        }
    }

    // OLD
    [ClientCommand(NetClient.OpCodes.UpdateBallPos)]
    private void UpdateBallPos_CMD(byte[] data) {
        SetMoveData(new BallStateData(data));
    }

    // OLD
    [ClientCommand(NetClient.OpCodes.GiveBall)]
    private void GiveBall_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        curHolder = GameControl.Instance.GetUser(data[0]);
        if (curHolder != null) {
            playerHasBall = true;
            curHolder.Instance.HasBall = true;
            transform.parent = curHolder.Instance.BallClamp.transform;
            transform.localPosition = Vector3.zero;
            transform.localEulerAngles = Vector3.zero;
            GetComponent<CapsuleCollider>().enabled = false;
        }
    }

    // OLD
    [ClientCommand(NetClient.OpCodes.FreeBall)]
    private void FreeBall_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        if (curHolder != null) {
            curHolder.Instance.HasBall = false;
            curHolder.Instance.turrentInst.RemoveBall();
            curHolder = null;
        }
        playerHasBall = false;
        transform.parent = null;
        GetComponent<CapsuleCollider>().enabled = true;
    }
}
