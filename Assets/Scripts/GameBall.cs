using UnityEngine;
using System;
using System.Collections;

public class GameBall : MonoBehaviour {
    public class MovementData {
        public const int SendBufferLenght = 12;

        public Vector3 Location { get; private set; }
        public Vector3 Rotation { get; private set; }
        public byte[] EncodedData { get; private set; }

        public MovementData(Vector3 location, Vector3 rotation) {
            this.Location = location;
            this.Rotation = rotation;
            this.EncodedData = Encode();
        }

        public MovementData(byte[] encodedData) {
            this.EncodedData = encodedData;
            Decode();
        }

        public byte[] Encode() {
            Int16 psx = PlayerControl.ToShort(Location.x, 500); // position x
            Int16 psy = PlayerControl.ToShort(Location.y, 500); // position y
            Int16 psz = PlayerControl.ToShort(Location.z, 500); // position z
            UInt16 rsx = PlayerControl.ToUnsighedShort(PlayerControl.Repeat(Rotation.x, 360), 90); // rotation x
            UInt16 rsy = PlayerControl.ToUnsighedShort(PlayerControl.Repeat(Rotation.y, 360), 90); // rotation y
            UInt16 rsz = PlayerControl.ToUnsighedShort(PlayerControl.Repeat(Rotation.z, 360), 90); // rotation z

            byte[] pbx = PlayerControl.ToByte(psx);
            byte[] pby = PlayerControl.ToByte(psy);
            byte[] pbz = PlayerControl.ToByte(psz);
            byte[] rbx = PlayerControl.ToByte(rsx);
            byte[] rby = PlayerControl.ToByte(rsy);
            byte[] rbz = PlayerControl.ToByte(rsz);

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

            return sendBuffer;
        }

        public void Decode() {
            Int16 psx = PlayerControl.FromByte(new byte[] { EncodedData[0], EncodedData[1] });
            Int16 psy = PlayerControl.FromByte(new byte[] { EncodedData[2], EncodedData[3] });
            Int16 psz = PlayerControl.FromByte(new byte[] { EncodedData[4], EncodedData[5] });
            UInt16 rsx = PlayerControl.FromByteUnsigned(new byte[] { EncodedData[6], EncodedData[7] });
            UInt16 rsy = PlayerControl.FromByteUnsigned(new byte[] { EncodedData[8], EncodedData[9] });
            UInt16 rsz = PlayerControl.FromByteUnsigned(new byte[] { EncodedData[10], EncodedData[11] });

            Location = new Vector3(PlayerControl.ToFloat(psx, 500), PlayerControl.ToFloat(psy, 500), PlayerControl.ToFloat(psz, 500));
            Rotation = new Vector3(PlayerControl.ToFloat(rsx, 90), PlayerControl.ToFloat(rsy, 90), PlayerControl.ToFloat(rsz, 90));
        }
    }

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
    private Rigidbody m_Rigidbody;
    private GameObject teeObj;

    public float sendCooldown = 1 / 15f;
    private float timer;

    void Awake() {
        Instance = this;
    }

    // Use this for initialization
    void Start () {
        NetClient.Instance.AddCommands(this);
        m_Rigidbody = GetComponent<Rigidbody>();
        if (!GameControl.Instance.IsServer)
            m_Rigidbody.isKinematic = true;
        syncStartPosition = m_Rigidbody.position;
        syncEndPosition = m_Rigidbody.position;
        syncStartRotation = m_Rigidbody.rotation;
        syncEndRotation = m_Rigidbody.rotation;
    }
	
	// Update is called once per frame
	void Update () {
        try {
            if (playerHasBall) {
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
                    m_Rigidbody.position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
                    m_Rigidbody.rotation = Quaternion.Lerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
                }
            }
        }
        catch(Exception e) {
            Debug.LogFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
        }
    }

    public MovementData GetMoveData() {
        return new MovementData(m_Rigidbody.position, m_Rigidbody.rotation.eulerAngles);
    }

    public void SetMoveData(MovementData data) {
        if (!GameControl.Instance.IsServer) {
            syncTime = 0f;
            syncDelay = Time.time - lastSyncTime;
            lastSyncTime = Time.time;

            syncStartPosition = m_Rigidbody.position;
            syncEndPosition = data.Location/* + new Vector3(data.velocity.x, 0, data.velocity.y) * syncDelay*/;

            syncStartRotation = m_Rigidbody.rotation;
            syncEndRotation = Quaternion.Euler(data.Rotation);
        }
    }

    public void SetLocation(Vector3 location) {
        m_Rigidbody.position = location;
    }

    public void SetRotation(Vector3 rotation) {
        m_Rigidbody.rotation = Quaternion.Euler(rotation);
    }

    public void Fire(Vector3 tankVelocity, Vector3 velocity) {
        if (playerHasBall && curHolder != null) {
            FreeBall();
            Vector3 globalVelocity = tankVelocity + transform.TransformDirection(velocity);
            m_Rigidbody.velocity = globalVelocity;
        }
    }

    public void GiveBall(byte id) {
        m_Rigidbody.isKinematic = true;
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

    public void UpdateState(byte player) {
        if (playerHasBall) {
            m_Rigidbody.isKinematic = true;
            NetServer.Instance.Send(player, NetClient.OpCodes.GiveBall, new byte[] { curHolder.ID });
        }
    }

    public void FreeBall() {
        m_Rigidbody.isKinematic = false;
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

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.position = new Vector3(xLocation + distance, 0.1851f, 0);
        m_Rigidbody.rotation = Quaternion.Euler(new Vector3(xRot, 90, 0));
    }

    public void RemoveTee() {
        if (teeObj != null) {
            Destroy(teeObj);
        }
    }

    [ClientCommand(NetClient.OpCodes.UpdateBallPos)]
    private void UpdateBallPos_CMD(byte[] data) {
        SetMoveData(new MovementData(data));
    }

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
