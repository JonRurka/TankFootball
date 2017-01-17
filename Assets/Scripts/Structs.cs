using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ClientCMD {
    DoLogin,
    LoginResult,
    KeyLoginResult,
    SetMatches,
    UdpEnabled,
    Ping,
    EndGame,
    CompleteRegister,
    JoinMatchComplete,
    UpdateLobbyUsers,
    StartMatch,
    SetTanks,
    UpdateOwnerTankPosition,
    UpdateClientTankPosition,
    RemovePlayer,
    UpdateBallPos,
    SetBallState,
    SetShootMode,
    ShootFx,
}

public enum ServerCMD {
    GetSalt,
    Login,
    GetMatches, // debug
    KeyLogin,
    StartUdp,
    Ping,
    CreateMatch, // debug command
    JoinMatch, // TODO: change to join random match
    GetLobby,
    StartMatch, // debug command
    ClientGameOpen,
    ClientClose,
    SubmitInput,
    SubmitPosition,
    Shoot,
    test,
    GetBall, // debug
    SetKick, // debug
}

public struct Data {
    public Protocal Type { get; private set; }
    public byte command { get; private set; }
    public byte[] Buffer { get; set; }
    public string Input { get; set; }

    public Data(Protocal type, byte cmd, byte[] data) {
        Type = type;
        command = cmd;
        Buffer = data;
        Input = Encoding.UTF8.GetString(Buffer);
    }
}

public enum PermissionLevel {
    Banned = 0,
    Inactive = 1,
    Active = 2,
    Moderator = 3,
    Admin = 4
}

public enum Protocal {
    Tcp,
    Udp
}

public struct Traffic {
    public byte byteCommand;
    public NetServer.OpCodes serverOpCode;
    public NetClient.OpCodes clientOpCode;
    public MainServerConnect.OpCodes mainOpCodes;
    public MainServerConnect.ServerOpCodes mainServerOpCodes;
    public byte[] byteData;

    public Traffic(NetClient.OpCodes command, byte[] data) {
        this.byteCommand = (byte)command;
        this.clientOpCode = command;
        this.serverOpCode = NetServer.OpCodes.None;
        this.mainOpCodes = MainServerConnect.OpCodes.None;
        this.mainServerOpCodes = MainServerConnect.ServerOpCodes.None;
        this.byteData = data;
    }

    public Traffic(NetServer.OpCodes command, byte[] data) {
        this.byteCommand = (byte)command;
        this.clientOpCode = NetClient.OpCodes.None;
        this.serverOpCode = command;
        this.mainOpCodes = MainServerConnect.OpCodes.None;
        this.mainServerOpCodes = MainServerConnect.ServerOpCodes.None;
        this.byteData = data;
    }

    public Traffic(MainServerConnect.OpCodes command, byte[] data) {
        this.byteCommand = (byte)command;
        this.clientOpCode = NetClient.OpCodes.None;
        this.serverOpCode = NetServer.OpCodes.None;
        this.mainOpCodes = command;
        this.mainServerOpCodes = MainServerConnect.ServerOpCodes.None;
        this.byteData = data;
    }

    public Traffic(MainServerConnect.ServerOpCodes command, byte[] data) {
        this.byteCommand = (byte)command;
        this.clientOpCode = NetClient.OpCodes.None;
        this.serverOpCode = NetServer.OpCodes.None;
        this.mainOpCodes = MainServerConnect.OpCodes.None;
        this.mainServerOpCodes = command;
        this.byteData = data;
    }
}

public class TankState {
    public const int SendBufferLenght = 14;

    public byte Id { get; private set; }
    public Vector2 Location { get; private set; }
    public float Velocity { get; private set; }
    public float Rotation { get; private set; }
    public float AngularVelecity { get; private set; }
    public Vector2 TurretRotation { get; private set; }
    public float Power { get; private set; }
    public byte[] EncodedData { get; private set; }

    public TankState(byte id, Vector2 location, float velocity, float rotation, float angularVelecity, Vector2 turretRotation, float power) {
        this.Id = id;
        this.Location = location;
        this.Velocity = velocity;
        this.Rotation = rotation;
        this.AngularVelecity = angularVelecity;
        this.TurretRotation = turretRotation;
        this.Power = Mathf.Clamp01(power);
        this.EncodedData = Encode();
    }

    public TankState(byte[] encodedData) {
        this.EncodedData = encodedData;
        Decode();
    }

    public byte[] Encode() {
        float x = Scale(Location.x, -63, 63, Int16.MinValue, Int16.MaxValue);
        float z = Scale(Location.y, -63, 63, Int16.MinValue, Int16.MaxValue);
        float vel = Scale(Velocity, -15, 15, Int16.MinValue, Int16.MaxValue);
        float rot = Scale(Repeat(Rotation, 360), 0, 360, 0, 255);
        float aVel = Scale(AngularVelecity, -3, 3, Int16.MinValue, Int16.MaxValue);
        float tx = Scale(Repeat(TurretRotation.x, 360), 0, 360, 0, 255);
        float ty = Scale(Repeat(TurretRotation.y, 360), 0, 360, 0, 255);
        float p = Scale(Power, 0, 1, 0, 255);

        DataEntries ent = new DataEntries();
        ent.AddEntry(Id);
        ent.AddEntry(new Vector2(x, z), DataTypes.ShortVector2);
        ent.AddEntry((short)vel);
        ent.AddEntry((byte)rot);
        ent.AddEntry((short)aVel);
        ent.AddEntry(new Vector2(tx, ty), DataTypes.ByteVector2);
        ent.AddEntry((byte)p);

        return ent.Encode(false);


        /*
        Int16 psx = (Int16)Scale(Location.x, -63, 63, Int16.MinValue, Int16.MaxValue);
        Int16 psz = (Int16)Scale(Location.y, -63, 63, Int16.MinValue, Int16.MaxValue);
        Int16 vs = (Int16)Scale(Velocity, -15, 15, Int16.MinValue, Int16.MaxValue);
        UInt16 rsy = (UInt16)Scale(Repeat(Rotation, 360), 0, 360, UInt16.MinValue, UInt16.MaxValue);
        Int16 avy = (Int16)Scale(AngularVelecity, -3, 3, Int16.MinValue, Int16.MaxValue);
        byte tbx = (byte)MovementData.Scale(Repeat(TurretRotation.x, 360), 0, 360, 0, 255);
        byte tby = (byte)MovementData.Scale(Repeat(TurretRotation.y, 360), 0, 360, 0, 255);
        byte pb = (byte)Scale(Power, 0, 1, 0, 255);

        byte[] pbx = BitConverter.GetBytes((Int16)psx); // 2
        byte[] pby = BitConverter.GetBytes((Int16)psz); // 2
        byte[] vb = BitConverter.GetBytes((Int16)vs); // 2
        byte[] rby = BitConverter.GetBytes((UInt16)rsy); // 2
        byte[] avyb = BitConverter.GetBytes((Int16)avy); // 2


        byte[] sendBuffer = new byte[SendBufferLenght];
        sendBuffer[0] = Id;
        sendBuffer[1] = pbx[0];
        sendBuffer[2] = pbx[1];
        sendBuffer[3] = pby[0];
        sendBuffer[4] = pby[1];
        sendBuffer[5] = vb[0];
        sendBuffer[6] = vb[1];
        sendBuffer[7] = rby[0];
        sendBuffer[8] = rby[1];
        sendBuffer[9] = avyb[0];
        sendBuffer[10] = avyb[1];
        sendBuffer[11] = tbx;
        sendBuffer[12] = tby;
        sendBuffer[13] = pb;

        return sendBuffer;
        */
    }

    public void Decode() {
        DataEntries ent = DataDecoder.Decode(EncodedData, DataTypePresets.TankState);
        Id = (byte)ent.GetEntryValue(0);
        Vector2 pos = (Vector2)ent.GetEntryValue(1);
        short vel = (short)ent.GetEntryValue(2);
        byte rot = (byte)ent.GetEntryValue(3);
        short aVel = (short)ent.GetEntryValue(4);
        Vector2 t = (Vector2)ent.GetEntryValue(5);
        byte p = (byte)ent.GetEntryValue(6);

        Location = new Vector3(Scale(pos.x, Int16.MinValue, Int16.MaxValue, -63, 63),
                               Scale(pos.y, Int16.MinValue, Int16.MaxValue, -63, 63));
        Velocity = Scale(vel, Int16.MinValue, Int16.MaxValue, -15, 15);
        Rotation = Scale(rot, 0, 255, 0, 360);
        AngularVelecity = Scale(aVel, Int16.MinValue, Int16.MaxValue, -3, 3);
        TurretRotation = new Vector3(Scale(t.x, 0, 255, 0, 360),
                                     Scale(t.y, 0, 255, 0, 360), 0);
        Power = Scale(p, 0, 255, 0, 1);

        /*Id = EncodedData[0];
        Int16 psx = BitConverter.ToInt16(EncodedData, 1);
        Int16 psz = BitConverter.ToInt16(EncodedData, 3);
        Int16 vs = BitConverter.ToInt16(EncodedData, 5);
        UInt16 rsy = BitConverter.ToUInt16(EncodedData, 7);
        Int16 avy = BitConverter.ToInt16(EncodedData, 9);

        Location = new Vector2(Scale(psx, Int16.MinValue, Int16.MaxValue, -63, 63), 
                               Scale(psz, Int16.MinValue, Int16.MaxValue, -63, 63));
        Velocity = Scale(vs, Int16.MinValue, Int16.MaxValue, -15, 15);
        Rotation = Scale(rsy, UInt16.MinValue, UInt16.MaxValue, 0, 360);
        AngularVelecity = Scale(avy, Int16.MinValue, Int16.MaxValue, -3, 3);
        TurretRotation = new Vector2(Scale(EncodedData[11], 0, 255, 0, 360), 
                                     Scale(EncodedData[12], 0, 255, 0, 360));
        Power = Scale(EncodedData[13], 0, 255, 0, 1);*/
    }

    public override string ToString() {
        return string.Format("{0}: pos:{1}, Vel:{2}, Rot:{3}, TurretRot:{4}",
                             Id, Location, Velocity, Rotation, TurretRotation);
    }

    public static float Repeat(float value, float mod) {
        float result = value % mod;
        if (result < 0)
            result += mod;
        return result;
    }

    public static float Scale(float value, float oldMin, float oldMax, float newMin, float newMax) {
        return newMin + (value - oldMin) * (newMax - newMin) / (oldMax - oldMin);
    }

    public static short Scale(float value, float oldMin, float oldMax) {
        return (short)Scale(value, oldMin, oldMax, short.MinValue, short.MaxValue);
    }
}

public class BallStateData {
    public const int SendBufferLenght = 10;

    public Vector3 Location { get; private set; }
    public Quaternion Rotation { get; private set; }
    public byte[] EncodedData { get; private set; }

    public BallStateData(Vector3 location, Quaternion rotation) {
        this.Location = location;
        this.Rotation = rotation;
        this.EncodedData = Encode();
    }

    public BallStateData(byte[] encodedData) {
        this.EncodedData = encodedData;
        Decode();
    }

    public byte[] Encode() {
        Int16 sx = (Int16)TankState.Scale(Location.x, -100, 100, Int16.MinValue, Int16.MaxValue);
        Int16 sy = (Int16)TankState.Scale(Location.y, -100, 100, Int16.MinValue, Int16.MaxValue);
        Int16 sz = (Int16)TankState.Scale(Location.z, -100, 100, Int16.MinValue, Int16.MaxValue);

        byte[] bx = BitConverter.GetBytes(sx);
        byte[] by = BitConverter.GetBytes(sy);
        byte[] bz = BitConverter.GetBytes(sz);

        byte brx = (byte)TankState.Scale(Rotation.x, -1, 1, 0, 255);
        byte bry = (byte)TankState.Scale(Rotation.y, -1, 1, 0, 255);
        byte brz = (byte)TankState.Scale(Rotation.z, -1, 1, 0, 255);
        byte brw = (byte)TankState.Scale(Rotation.w, -1, 1, 0, 255);

        byte[] sendBuffer = new byte[SendBufferLenght];

        sendBuffer[0] = bx[0];
        sendBuffer[1] = bx[1];
        sendBuffer[2] = by[0];
        sendBuffer[3] = by[1];
        sendBuffer[4] = bz[0];
        sendBuffer[5] = bz[1];
        sendBuffer[6] = brx;
        sendBuffer[7] = bry;
        sendBuffer[8] = brz;
        sendBuffer[9] = brw;

        return sendBuffer;
    }

    public void Decode() {
        Int16 sx = BitConverter.ToInt16(EncodedData, 0);
        Int16 sy = BitConverter.ToInt16(EncodedData, 2);
        Int16 sz = BitConverter.ToInt16(EncodedData, 4);

        byte brx = EncodedData[6];
        byte bry = EncodedData[7];
        byte brz = EncodedData[8];
        byte brw = EncodedData[9];

        Location = new Vector3(TankState.Scale(sx, Int16.MinValue, Int16.MaxValue, -100, 100),
                               TankState.Scale(sy, Int16.MinValue, Int16.MaxValue, -100, 100),
                               TankState.Scale(sz, Int16.MinValue, Int16.MaxValue, -100, 100));

        Rotation = new Quaternion(TankState.Scale(brx, 0, 255, -1, 1),
                                  TankState.Scale(bry, 0, 255, -1, 1),
                                  TankState.Scale(brz, 0, 255, -1, 1),
                                  TankState.Scale(brw, 0, 255, -1, 1));
    }
}

public class InputData {
    public bool Forward;
    public bool Backwards;
    public bool Left;
    public bool Right;

    public InputData() {
        Forward = false;
        Backwards = false;
        Left = false;
        Right = false;
    }

    public InputData(float vert, float horz) {
        if (vert > 0.01) {
            Forward = true;
        }
        if (vert < -0.01) {
            Backwards = true;
        }
        if (horz > 0.01) {
            Right = true;
        }
        if (horz < -0.01) {
            Left = true;
        }
    }

    public InputData(byte value) {
        Forward = BufferEdit.IsFlagSet(value, 0);
        Backwards = BufferEdit.IsFlagSet(value, 1);
        Left = BufferEdit.IsFlagSet(value, 2);
        Right = BufferEdit.IsFlagSet(value, 3);
    }

    public byte GetByte() {
        byte value = 0;
        if (Forward)
            value = BufferEdit.SetFlag(value, 0);
        if (Backwards)
            value = BufferEdit.SetFlag(value, 1);
        if (Left)
            value = BufferEdit.SetFlag(value, 2);
        if (Right)
            value = BufferEdit.SetFlag(value, 3);
        return value;
    }
}

public class TeamStats {
    public GameLobby.Team Team { get; private set; }
    public Dictionary<byte, Player> Players { get; private set; }
    public string TeamName { get; private set; }
    public int Score { get; set; }
    public bool IsOffense { get; set; }
    public List<byte> idleSpots;

    public TeamStats(GameLobby.Team team, string name) {
        Team = team;
        Players = new Dictionary<byte, Player>();
        idleSpots = new List<byte>();
        TeamName = name;
        Score = 0;
        IsOffense = false;
    }

    public TeamStats(GameLobby.Team team) : this(team, team.ToString()) {
    }
}

public enum GameState {
    PreGame,
    KickOff,
}

public enum RotationDir {
    None,
    Left,
    Right,
}

[Serializable]
public class ServerState {
    public Vector3 Position;
    public Vector3 CurPosition;
    public Quaternion Rotation;
    public Quaternion CurRotation;
    public float Velocity;
    public float Power;
    public RotationDir RotDir;
}