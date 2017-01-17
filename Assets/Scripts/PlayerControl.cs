using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Timers;
using System.Linq;
using System.Text;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using bp = BEPUutilities;

public class PlayerControl : MonoBehaviour {

    public static PlayerControl Instance;
    public float sendCooldown = 1 / 10f;
    private float timer;

    public GameObject tankPrefab;
    public Player[] playerList = new Player[0];
    public bool TanksSet { get; private set; }
    public int TankInstanceCount
    {
        get
        {
            int instances = 0;
            foreach (Player player in new List<Player>(GameControl.Instance.players.Values)) {
                if (player.TankExists)
                    instances++;
            }
            return instances;
        }
    }

    private int _sent = 0;
    public int sentPS = 0;
    public System.Diagnostics.Stopwatch watch;

    private Timer sendTimer;

    void Awake() {
        Instance = this;
        //NetClient.Instance.AddCommands(this);
        //TanksSet = false;
    }

	// Use this for initialization
	void Start () {
        MainServerConnect.Instance.AddCommands(this);
        sendTimer = new Timer();
        watch = new System.Diagnostics.Stopwatch();
        watch.Start();
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
                //UpdateClientPositions();
                _sent++;
            }
        }
        playerList = GameControl.Instance.players.Values.ToArray();

        if (watch.Elapsed.Seconds >= 1) {
            sentPS = _sent;
            _sent = 0;
            watch.Stop();
            watch.Reset();
            watch.Start();
        }
    }

    // OLD
    /*public void SetServer() {
        if (GameControl.Instance.IsServer) {
            sendTimer.Elapsed += new ElapsedEventHandler(UpdateClientPositions);
            sendTimer.Interval = sendCooldown * 1000;
            sendTimer.Enabled = true;
        }
    }*/

    // OLD
    /*public void UpdateClientPositions(object source, ElapsedEventArgs e) {
        int playerBufferLength = MovementData.SendBufferLenght;
        byte[] keys = GameControl.Instance.players.Keys.ToArray();
        byte[] sendBuffer = new byte[keys.Length * playerBufferLength];
        int pindex = 0;
        foreach (byte key in keys) {
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
            catch (Exception ex) {
                SafeDebug.LogError(string.Format("{0}: {1}\n{2}", e.GetType(), ex.Message, ex.StackTrace));
                SafeDebug.LogError(pindex + ", " + i);
            }
        }
        NetServer.Instance.Send(NetClient.OpCodes.UpdatePositions, sendBuffer, MainServerConnect.Instance.udpEnabled ? Protocal.Udp : Protocal.Tcp);
    }*/

    public void SetPosition(byte id, Vector3 position, Vector3 euler) {
        GameControl.Instance.players[id].Instance.SetPosition(position);
        GameControl.Instance.players[id].Instance.SetRotation(euler);
    }

    // OLD
    /*public void AddTank(byte id) {
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
        //sendStr = id + "★" + user.Name + "★" + ToShort(position.x, 500) + "," + ToShort(position.z, 500);
        //Debug.Log("Send string: " + sendStr);
        //SetClientTank(id, sendStr);

    }*/

    // OLD
    /*public void SetClientTanks() {
        string sendStr = "";
        foreach (byte id in GameControl.Instance.players.Keys.ToArray()) {
            if (GameControl.Instance.players[id].TankExists) {
                Player user = GameControl.Instance.GetUser(id);
                Vector3 position = GameControl.Instance.players[id].GameObj.transform.position;
                sendStr += id + "★" + user.Name + "★" + MovementData.ToShort(position.x, 500) + "," + MovementData.ToShort(position.z, 500) + "❤";
            }
        }

        Debug.Log("Set Tank:" + sendStr);
        NetServer.Instance.Send(NetClient.OpCodes.SetTanks, sendStr);
        TanksSet = true;
    }*/

    public void RemoveTank(byte id) {
        if (GameControl.Instance.UserExists(id)) {
            GameControl.Instance.GetUser(id).DestroyTank();
            //NetServer.Instance.Send(NetClient.OpCodes.RemoveTank, new byte[] { id });
        }
        else
            Debug.Log("ID not found: " + id);
    }

    public void Close() {
        foreach (Player player in GameControl.Instance.players.Values) {
            player.DestroyTank();
        }
        playerList = new Player[0];
        sendTimer.Enabled = false;
    }

    public void OnDisable() {
        sendTimer.Enabled = false;
    }

    [Command(ClientCMD.SetTanks)]
    public void SetTanks_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer, DataTypePresets.SetTanks);
        byte count = (byte)ent.GetEntryValue(0);
        byte[] ent2Buff = (byte[])ent.GetEntryValue(1);
        DataTypes[] metaData = new DataTypes[count];
        for (int i = 0; i < count; i++)
            metaData[i] = DataTypes.Buffer;
        DataEntries ent2 = DataDecoder.Decode(ent2Buff, metaData);
        for (int p = 0; p < count; p++) {
            DataEntries ent3 = DataDecoder.Decode((byte[])ent2.GetEntryValue(p), DataTypePresets.SetTanksData);
            byte id = (byte)ent3.GetEntryValue(0);
            Vector2 sPos = (Vector2)ent3.GetEntryValue(1);
            byte rot = (byte)ent3.GetEntryValue(2);
            float maxS = (float)ent3.GetEntryValue(3);
            Player user = GameControl.Instance.GetUser(id);
            if (user != null && !user.TankExists) {
                Vector3 position = new Vector3(TankState.Scale(sPos.x, Int16.MinValue, Int16.MaxValue, -63, 63), 0,
                                               TankState.Scale(sPos.y, Int16.MinValue, Int16.MaxValue, -63, 63));
                float rotation = TankState.Scale(rot, 0, 255, 0, 360);
                GameObject obj = Instantiate(tankPrefab, position, Quaternion.AngleAxis(rotation, Vector3.up));
                obj.name = user.Name;
                user.SetTank(obj);
                user.Instance.ID = id;
                user.Instance.m_Speed = maxS;
                if (id == GameControl.Instance.netID) {
                    GameControl.Instance.SetMouseLocked(true);
                    user.Instance.isOwner = true;
                    Debug.LogFormat("{0}: set owner.", user.Name);
                }
                Debug.LogFormat("Set Tanks: Adding new tank {0}({1}) at {2}.", user.Name, id, position);
            }
        }
    }

    [Command(ClientCMD.UpdateOwnerTankPosition, true)]
    public void UpdateOwnerTankPosition_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer, DataTypePresets.UpdateOwnerTankPosition);
        ushort receivedNum = (ushort)ent.GetEntryValue(0);
        byte[] playerBuff = (byte[])ent.GetEntryValue(1);
        TankState state = new TankState(playerBuff);
        if (GameControl.Instance.UserExists(GameControl.Instance.netID)) {
            Player user = GameControl.Instance.GetUser();
            if (user != null)
                if (user.TankExists) {
                    user.Instance.SetOwnerState(receivedNum, state);
                }
                else
                    Debug.LogErrorFormat("{0}: user instance null!", user.Name);
            else
                Debug.LogError("user null!");
        }
    }

    [Command(ClientCMD.UpdateClientTankPosition)]
    public void UpdateClientTankPosition_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer, DataTypePresets.UpdateClientTankPosition);
        byte count = (byte)ent.GetEntryValue(0);
        byte[] ent2Buff = (byte[])ent.GetEntryValue(1);
        DataTypes[] metaData = new DataTypes[count];
        for (int i = 0; i < count; i++) {
            metaData[i] = DataTypes.Buffer;
        }
        DataEntries ent2 = DataDecoder.Decode(ent2Buff, metaData);
        for (int i = 0; i < count; i++) {
            byte[] playerBuff = (byte[])ent2.GetEntryValue(i);
            TankState state = new TankState(playerBuff);
            if (GameControl.Instance.UserExists(state.Id)) {
                Player user = GameControl.Instance.GetUser(state.Id);
                if (user.TankExists) {
                    if (!user.Instance.isOwner) {
                        user.Instance.SetPlayerData(state);
                    }
                }
                else {
                    Debug.LogWarning("Tank doesn't exist: " + user.ID);
                }
            }
            else {
                Debug.LogError("Player id not found: " + state.Id);
            }
        }
    }

    [Command(ClientCMD.RemovePlayer)]
    public void RemovePlayer_CMD(Data data) {
        if (GameControl.Instance.UserExists(data.Buffer[0])) {
            GameControl.Instance.GetUser(data.Buffer[0]).DestroyTank();
        }
    }

    [Command(ClientCMD.SetShootMode)]
    public void SetShootMode_CMD(Data data) {
        Player player = GameControl.Instance.GetUser();
        if (player != null && player.TankExists) {
            player.Instance.turrentInst.SetShootMode((TurretScript.ShootMode)data.Buffer[0]);
        }
    }

    [Command(ClientCMD.ShootFx)]
    public void ShootFx_CMD(Data data) {
        if (GameControl.Instance.GameStarted) {
            if (GameControl.Instance.UserExists(data.Buffer[0])) {
                Player player = GameControl.Instance.GetUser(data.Buffer[0]);
                if (player != null && player.TankExists) {
                    TurretScript.ShootMode mode = (TurretScript.ShootMode)data.Buffer[1];
                    player.Instance.turrentInst.ClientShootEffect(mode);
                }
            }
        }
    }

    // ----------------- OLD ----------------- 

    // Server commands

    // OLD
    /*[ServerCommand(NetServer.OpCodes.SubmitInput)]
    public Traffic SubmitInput_CMD(Player user, byte[] data) {
        if (GameControl.Instance.UserExists(user.ID) && user.TankExists) {
            try {
                short sentNum = BitConverter.ToInt16(data, 0);
                byte inputBitFlags = data[2];
                InputData input = new InputData(inputBitFlags);
                float tx = MovementData.Scale(data[3], 0, 255, 0, 360);
                float ty = MovementData.Scale(data[4], 0, 255, 0, 360);
                Vector2 turretRot = new Vector2(tx, ty);
                user.Instance.SetInput(sentNum, input, turretRot);
            }
            catch (Exception e) {
                Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
            }
        }
        return default(Traffic);
    }*/

    // OLD
    /*[ServerCommand(NetServer.OpCodes.SubmitPosition)]
    public Traffic SubmitPosition_CMD(Player user, byte[] data) {
        MovementData movement = new MovementData(data);
        user.Instance.SetPlayerData(movement);
        return default(Traffic);
    }*/

    // OLD
    /*[ServerCommand(NetServer.OpCodes.Shoot)]
    public Traffic Shoot_CMD(Player user, byte[] data) {
        if (user != null && user.TankExists) {
            TankMovement tank = user.Instance;
            tank.turrentInst.ServerShoot(data);
        }
        else
            Debug.LogError("Player null: " + user.ID);
        return default(Traffic);
    }*/


    // client commands

    // OLD
    /*[ClientCommand(NetClient.OpCodes.SetTanks)]
    public void SetTanks_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        Debug.Log("Set Tank");

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
                    if (!GameControl.Instance.GetUser(id).TankExists) {
                        Vector3 position = new Vector3(MovementData.ToFloat(sx, 500), 0, MovementData.ToFloat(sz, 500));
                        GameObject obj = (GameObject)Instantiate(tankPrefab, position, Quaternion.identity);
                        obj.name = name;
                        Player player = GameControl.Instance.GetUser(id);
                        player.SetTank(obj);
                        if (id == GameControl.Instance.netID)
                            player.Instance.isOwner = true;
                        player.Instance.ID = id;
                        Debug.LogFormat("Set Tanks: Adding new tank {0}({1}) at {2}.", name, id, position);
                    }
                    else
                        Debug.LogWarning("Set Tank: User doesn't exist: " + id);
                }
                else
                    Debug.LogWarning("Set Tank: Invalid format 2 (" + id + "): " + posParts);
            }
            else
                Debug.LogWarning("Set Tank: Invalid format 1: " + instances[i]);
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.AddTank)]
    public void AddTank_CMD(byte[] data) {
        if (GameControl.Instance.IsServer)
            return;

        string input = Encoding.UTF8.GetString(data);
        Debug.Log("Add Tank: " + input + "\nByte: " + BitConverter.ToString(data));
        //Debug.Log(input);
        string[] parts = input.Split('★');
        if (parts.Length == 3) {
            byte id = byte.Parse(parts[0]);
            string name = parts[1];
            string[] posParts = parts[2].Split(',');
            if (posParts.Length == 2) {
                short sx = short.Parse(posParts[0]);
                short sz = short.Parse(posParts[1]);
                Vector3 position = new Vector3(MovementData.ToFloat(sx, 500), 0, MovementData.ToFloat(sz, 500));
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
                Debug.LogWarning("Add Tank: location incorrectfully formated: " + input);
            }
        }
        else {
            Debug.LogWarning("Add Tank: invalid format: " + input);
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.RemoveTank)]
    public void RemoveTank_CMD(byte[] data) {
        byte id = data[0];
        if (GameControl.Instance.UserExists(id)) {
            GameControl.Instance.GetUser(id).DestroyTank();
        }
        else
            Debug.Log("ID not found: " + id);
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.UpdatePositions)]
    public void UpdatePositions_CMD(byte[] data) {
        if (!GameControl.Instance.IsServer) {
            int playerBufferLength = MovementData.SendBufferLenght;
            for (int pindex = 0; pindex < GameControl.Instance.players.Count; pindex++) {
                byte[] playerBuffer = new byte[playerBufferLength];
                for (int i = 0; i < playerBufferLength; i++) {
                    playerBuffer[i] = data[pindex * playerBufferLength + i];
                }
                byte id = playerBuffer[0];
                if (GameControl.Instance.UserExists(id)) {
                    Player user = GameControl.Instance.GetUser(id);
                    if (user.TankExists) {
                        if (!user.Instance.isOwner) {
                            user.Instance.SetPlayerData(new MovementData(playerBuffer));
                        }
                    }
                    else {
                        Debug.LogWarning("Tank no exist: " + user.ID);
                    }
                }
                else
                    Debug.LogError("player id not found: " + id);
            }
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.UpdateTankPosition)]
    public void UpdateTankPosition_CMD(byte[] data) {
        if (data.Length == MovementData.SendBufferLenght + 2) {
            
            short receivedNum = BitConverter.ToInt16(data, 0);
            int playerBufferLength = MovementData.SendBufferLenght;
            byte[] playerBuffer = new byte[playerBufferLength];
            Array.Copy(data, 2, playerBuffer, 0, playerBufferLength);
            MovementData moveData = new MovementData(playerBuffer);
            //Debug.LogFormat("Client: {0} - {1}, {2}\n{3} ", GameControl.Instance.netID, receivedNum, moveData.ToString(), BitConverter.ToString(playerBuffer));
            if (GameControl.Instance.UserExists(moveData.Id)) {
                GameControl.Instance.GetUser(moveData.Id).Instance.SetOwnerData(receivedNum, moveData);
            }
        }
        else {
            Debug.LogError("UpdateTankPosition_CMD: Invalid length!");
        }
        
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.Shoot)]
    public void Clientshoot_CMD(byte[] data) {
        Player player = GameControl.Instance.GetUser(data[0]);
        if (player != null) {
            TankMovement tank = player.Instance;
            tank.turrentInst.ClientShootEffect((TurretScript.ShootMode)data[1]);
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.SetEnabled)]
    public void SetEnabled_CMD(byte[] data) {
        byte id = data[0];
        bool tankEnabled = BitConverter.ToBoolean(data, 1);
        if (GameControl.Instance.UserExists(id))
            GameControl.Instance.players[id].Instance.SetEnabled(tankEnabled);
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.SetMaxSpeed)]
    public void SetMaxSpeed_CMD(byte[] data) {
        float speed = BitConverter.ToSingle(data, 0);
        GameControl.Instance.GetUser().Instance.SetMaxSpeed(speed);
    }*/
}
