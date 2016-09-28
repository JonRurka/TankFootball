using System;
using System.Text;

public enum PermissionLevel {
    Banned = 0,
    Inactive = 1,
    Active = 2,
    Moderator = 3,
    Admin = 4
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

