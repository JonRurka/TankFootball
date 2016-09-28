using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

public class PlayerControl : MonoBehaviour {
    public class MovementData {
        public const int SendBufferLenght = 12;

        public byte Id { get; private set; }
        public Vector2 Location { get; private set; }
        public Vector2 Velocity { get; private set; }
        public float Rotation { get; private set; }
        public Vector2 TurretRotation { get; private set; }
        public float Sound { get; private set; }
        public byte[] EncodedData { get; private set; }

        public MovementData(byte id, Vector2 location, Vector2 velocity, float rotation, Vector2 turretRotation, float sound) {
            this.Id = id;
            this.Location = location;
            this.Velocity = velocity;
            this.Rotation = rotation;
            this.TurretRotation = turretRotation;
            this.Sound = Mathf.Clamp01(sound);
            this.EncodedData = Encode();
        }

        public MovementData(byte[] encodedData) {
            this.EncodedData = encodedData;
            Decode();
        }

        public byte[] Encode() {
            Int16 psx = ToShort(Location.x, 500); // position x
            Int16 psz = ToShort(Location.y, 500); // position z
            Int16 vsx = ToShort(Velocity.x, 600); // velocity x
            Int16 vsz = ToShort(Velocity.y, 600); // velocity z
            UInt16 rsy = ToUnsighedShort(Repeat(Rotation, 360), 90); // rotation y
            UInt16 tsx = ToUnsighedShort(Repeat(TurretRotation.x, 360), 90); // turret rotation x
            UInt16 tsy = ToUnsighedShort(Repeat(TurretRotation.y, 360), 90); // turret rotation y

            byte[] pbx = ToByte(psx);
            byte[] pby = ToByte(psz);
            byte[] vbx = ToByte(vsx);
            byte[] vbz = ToByte(vsz);
            byte[] rby = ToByte(rsy);
            byte[] tbx = ToByte(tsx);
            byte[] tby = ToByte(tsy);
            byte sb = (byte)Mathf.Clamp((Sound * 255), 0, 255);

            byte[] sendBuffer = new byte[SendBufferLenght];
            #region send Velocity
            /*sendBuffer[0] = id;
            sendBuffer[1] = pbx[0];
            sendBuffer[2] = pbx[1];
            sendBuffer[3] = pby[0];
            sendBuffer[4] = pby[1];
            sendBuffer[5] = vbx[0];
            sendBuffer[6] = vbx[1];
            sendBuffer[7] = vbz[0];
            sendBuffer[8] = vbz[1];
            sendBuffer[9] = rby[0];
            sendBuffer[10] = rby[1];
            sendBuffer[11] = tbx[0];
            sendBuffer[12] = tbx[1];
            sendBuffer[13] = tby[0];
            sendBuffer[14] = tby[1];*/
            #endregion
            sendBuffer[0] = Id;
            sendBuffer[1] = pbx[0];
            sendBuffer[2] = pbx[1];
            sendBuffer[3] = pby[0];
            sendBuffer[4] = pby[1];
            sendBuffer[5] = rby[0];
            sendBuffer[6] = rby[1];
            sendBuffer[7] = tbx[0];
            sendBuffer[8] = tbx[1];
            sendBuffer[9] = tby[0];
            sendBuffer[10] = tby[1];
            sendBuffer[11] = sb;

            return sendBuffer;
        }

        public void Decode() {
            #region send velocity
            /*Int16 psx = PlayerControl.FromByte(new byte[] { encodedData[1], encodedData[2] });
            Int16 psz = PlayerControl.FromByte(new byte[] { encodedData[3], encodedData[4] });
            Int16 vsx = PlayerControl.FromByte(new byte[] { encodedData[5], encodedData[6] });
            Int16 vsz = PlayerControl.FromByte(new byte[] { encodedData[7], encodedData[8] });
            UInt16 rsy = PlayerControl.FromByteUnsigned(new byte[] { encodedData[9], encodedData[10] });
            UInt16 tsx = PlayerControl.FromByteUnsigned(new byte[] { encodedData[11], encodedData[12] });
            UInt16 tsy = PlayerControl.FromByteUnsigned(new byte[] { encodedData[13], encodedData[14] });*/
            #endregion
            Int16 psx = FromByte(new byte[] { EncodedData[1], EncodedData[2] });
            Int16 psz = FromByte(new byte[] { EncodedData[3], EncodedData[4] });
            UInt16 rsy = FromByteUnsigned(new byte[] { EncodedData[5], EncodedData[6] });
            UInt16 tsx = FromByteUnsigned(new byte[] { EncodedData[7], EncodedData[8] });
            UInt16 tsy = FromByteUnsigned(new byte[] { EncodedData[9], EncodedData[10] });
            Sound = Mathf.Clamp01(EncodedData[11] / 255);

            Location = new Vector2(ToFloat(psx, 500), ToFloat(psz, 500));
            //velocity = new Vector2(PlayerControl.ToFloat(vsx, 600), PlayerControl.ToFloat(vsz, 600));
            Rotation = ToFloat(rsy, 90);
            TurretRotation = new Vector2(ToFloat(tsx, 90), ToFloat(tsy, 90));
        }
    }

    public static PlayerControl Instance;
    public float sendCooldown = 1 / 15f;
    private float timer;

    public GameObject tankPrefab;
    public List<Player> playerList = new List<Player>();
    
    
    void Awake() {
        Instance = this;
        NetClient.Instance.AddCommands(this);
        NetServer.Instance.AddCommands(this);
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (GameControl.Instance.IsServer) {
            if (timer > 0)
                timer -= Time.deltaTime;
            if (timer < 0)
                timer = 0;
            if (timer == 0) {
                timer = sendCooldown;
                UpdateClientPositions();
            }
        }
        playerList.Clear();
        playerList.AddRange(GameControl.Instance.players.Values.ToArray());
    }

    public void UpdateClientPositions() {
        int playerBufferLength = MovementData.SendBufferLenght;
        byte[] sendBuffer = new byte[GameControl.Instance.players.Count * playerBufferLength];
        int pindex = 0;
        foreach (byte key in GameControl.Instance.players.Keys) {
            int i = 0;
            try {
                if (GameControl.Instance.players[key].TankExists) {
                    byte[] playerBuffer = GameControl.Instance.players[key].Instance.GetPlayerData().EncodedData;
                    for (i = 0; i < playerBufferLength; i++) {
                        sendBuffer[pindex * playerBufferLength + i] = playerBuffer[i];
                    }
                }
                pindex++;
            }
            catch (Exception e) {
                Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
                Debug.LogError(pindex + ", " + i);
                Debug.Break();
            }
        }
        NetServer.Instance.Send(NetClient.OpCodes.UpdatePositions, sendBuffer);
    }

    public void SetPosition(byte id, Vector3 position, Vector3 euler) {
        GameControl.Instance.players[id].Instance.SetPosition(position);
        GameControl.Instance.players[id].Instance.SetRotation(euler);
    }

    public void AddTank(byte id, bool updateOthers = false) {
        string sendStr = "";
        Player user = GameControl.Instance.GetUser(id);
        GameObject tank = (GameObject)Instantiate(tankPrefab, Vector3.zero, Quaternion.identity);
        tank.name = user.Name;
        if (!user.SetTank(tank)) {
            Debug.LogError("Failed to set tank: " + id);
        }
        //Debug.LogFormat("Tank added at {0}, {1}", position, ToShort(position.x, 500));
        user.Instance.turrentInst.SetShootMode(TurretScript.ShootMode.none);
        Vector3 position = GamePlay.Instance.SetTank(user);
        if (id == GameControl.Instance.netID)
            user.Instance.isOwner = true;
        user.Instance.ID = id;
        sendStr += id + "★" + user.Name + "★" + ToShort(position.x, 500) + "," + ToShort(position.z, 500);
        //Debug.Log("Send string: " + sendStr);
        SetClientTank(id, sendStr);

    }

    public void SetClientTank(byte userId, string updateStr) {
        string sendStr = "";
        foreach (byte id in GameControl.Instance.players.Keys.ToArray()) {
            if (id != userId && GameControl.Instance.players[id].TankExists) {
                Player user = GameControl.Instance.GetUser(id);
                Vector3 position = GameControl.Instance.players[id].GameObj.transform.position;
                sendStr += id + "★" + user.Name + "★" + ToShort(position.x, 500) + "," + ToShort(position.z, 500) + "❤";
            }
        }
        //Debug.Log(sendStr);
        NetServer.Instance.Send(NetClient.OpCodes.AddTank, updateStr);
        NetServer.Instance.Send(userId, NetClient.OpCodes.SetTanks, sendStr);
    }

    public void RemoveTank(byte id) {
        if (GameControl.Instance.UserExists(id)) {
            Destroy(GameControl.Instance.players[id].GameObj);
            NetServer.Instance.Send(NetClient.OpCodes.RemoveTank, new byte[] { id });
        }
        else
            Debug.Log("ID not found: " + id);
    }

    public static short ToShort(float value, float scale) {
        return (short)(value * scale);
    }

    public static float ToFloat(short value, float scale) {
        return (value / scale);
    }

    public static ushort ToUnsighedShort(float value, float scale) {
        return (ushort)(value * scale);
    }

    public static float ToFloat(ushort value, float scale) {
        return (value / scale);
    }

    public static byte[] ToByte(Int16 value) {
        return BitConverter.GetBytes(value);
    }

    public static Int16 FromByte(byte[] value) {
        return BitConverter.ToInt16(value, 0);
    }

    public static byte[] ToByte(UInt16 value) {
        return BitConverter.GetBytes(value);
    }

    public static UInt16 FromByteUnsigned(byte[] value) {
        return BitConverter.ToUInt16(value, 0);
    }

    public static float Repeat(float value, float mod) {
        float result = value % mod;
        if (result < 0)
            result += mod;
        return result;
    }

    public void Close() {
        foreach (byte pID in GameControl.Instance.players.Keys) {
            Destroy(GameControl.Instance.players[pID].GameObj);
            GameControl.Instance.players[pID].Instance = null;
        }
    }

    // Server commands

    [ServerCommand(NetServer.OpCodes.SubmitInput)]
    public Traffic SubmitInput_CMD(Player user, byte[] data) {
        if (GameControl.Instance.UserExists(user.ID) && user.TankExists) {
            try {
                byte vert = data[0];
                byte horz = data[1];
                byte[] bx = { data[2], data[3] };
                byte[] by = { data[4], data[5] };
                UInt16 sx = FromByteUnsigned(bx);
                UInt16 sy = FromByteUnsigned(by);
                Vector2 turretRot = new Vector2(ToFloat(sx, 90), ToFloat(sy, 90));
                user.Instance.SetInput(vert, horz, turretRot);
            }
            catch (Exception e) {
                Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
            }
        }
        return default(Traffic);
    }

    [ServerCommand(NetServer.OpCodes.Shoot)]
    public Traffic Shoot_CMD(Player user, byte[] data) {
        if (user != null && user.TankExists) {
            TankMovement tank = user.Instance;
            tank.turrentInst.ServerShoot(data);
        }
        else
            Debug.LogError("Player null: " + user.ID);
        return default(Traffic);
    }


    // client commands

    [ClientCommand(NetClient.OpCodes.SetTanks)]
    public void SetTanks_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        string input = Encoding.UTF8.GetString(data);
        string[] instances = input.Split(new char[] { '❤' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < instances.Length; i++) {
            string[] parts = instances[i].Split('★');
            if (parts.Length == 3) {
                byte id = byte.Parse(parts[0]);
                string name = parts[1];
                string[] posParts = parts[2].Split(',');
                if (posParts.Length == 2) {
                    short sx = short.Parse(posParts[0]);
                    short sz = short.Parse(posParts[1]);
                    if (!GameControl.Instance.UserExists(id)) {
                        Vector3 position = new Vector3(ToFloat(sx, 500), 0, ToFloat(sz, 500));
                        GameObject obj = (GameObject)Instantiate(tankPrefab, position, Quaternion.identity);
                        obj.name = name;
                        Player player = GameControl.Instance.GetUser(id);
                        player.SetTank(obj);
                        if (id == GameControl.Instance.netID)
                            player.Instance.isOwner = true;
                        player.Instance.ID = id;
                        Debug.LogFormat("Set Tanks: Adding new tank {0}({1}) at {2}.", name, id, position);
                    }
                }
            }
        }
    }

    [ClientCommand(NetClient.OpCodes.AddTank)]
    public void AddTank_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        string input = Encoding.UTF8.GetString(data);
        //Debug.Log(input);
        string[] parts = input.Split('★');
        if (parts.Length == 3) {
            byte id = byte.Parse(parts[0]);
            string name = parts[1];
            string[] posParts = parts[2].Split(',');
            if (posParts.Length == 2) {
                short sx = short.Parse(posParts[0]);
                short sz = short.Parse(posParts[1]);
                Vector3 position = new Vector3(ToFloat(sx, 500), 0, ToFloat(sz, 500));
                GameObject obj = (GameObject)Instantiate(tankPrefab, position, Quaternion.identity);
                obj.name = name;
                Player player = GameControl.Instance.GetUser(id);
                player.SetTank(obj);
                if (id == GameControl.Instance.netID)
                    player.Instance.isOwner = true;
                player.Instance.ID = id;
                Debug.LogFormat("Add Tank: Adding tank {0}({1}) at {2}", name, id, position);
                
            }
            else {
                Debug.LogError("location incorrectfully formated");
            }
        }
        else {
            Debug.LogError("invalid formate");
        }
    }

    [ClientCommand(NetClient.OpCodes.RemoveTank)]
    public void RemoveTank_CMD(byte[] data) {
        byte id = data[0];
        if (GameControl.Instance.UserExists(id)) {
            Destroy(GameControl.Instance.players[id].GameObj);
        }
        else
            Debug.Log("ID not found: " + id);
    }

    [ClientCommand(NetClient.OpCodes.UpdatePositions)]
    public void UpdatePositions_CMD(byte[] data) {
        if (!GameControl.Instance.IsServer) {
            int playerBufferLength = MovementData.SendBufferLenght;
            for (int pindex = 0; pindex < GameControl.Instance.players.Count; pindex++) {
                byte[] playerBuffer = new byte[playerBufferLength];
                for (int i = 0; i < playerBufferLength; i++) {
                    playerBuffer[i] = data[pindex * playerBufferLength + i];
                }
                byte id = playerBuffer[0];
                if (GameControl.Instance.UserExists(id))
                    if (GameControl.Instance.players[id].TankExists) {
                        GameControl.Instance.players[id].Instance.SetPlayerData(new MovementData(playerBuffer));
                    }
                    else
                        Debug.LogError("Tank not created: " + id);
                else
                    Debug.LogError("Tank id not found: " + id);
            }
        }
    }

    [ClientCommand(NetClient.OpCodes.Shoot)]
    public void Clientshoot_CMD(byte[] data) {
        Player player = GameControl.Instance.GetUser(data[0]);
        if (player != null) {
            TankMovement tank = player.Instance;
            tank.turrentInst.ClientShootEffect((TurretScript.ShootMode)data[1]);
        }
    }

    [ClientCommand(NetClient.OpCodes.SetEnabled)]
    public void SetEnabled_CMD(byte[] data) {
        byte id = data[0];
        bool tankEnabled = BitConverter.ToBoolean(data, 1);
        if (GameControl.Instance.UserExists(id))
            GameControl.Instance.players[id].Instance.SetEnabled(tankEnabled);
    }

}
