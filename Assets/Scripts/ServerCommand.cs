using UnityEngine;
using System;
using System.Collections;

public class ServerCommand : Attribute {
    public byte byteCommand;
    public NetServer.OpCodes opCode;

    public ServerCommand(byte cmd) {
        byteCommand = cmd;
        opCode = (NetServer.OpCodes)cmd;
    }

    public ServerCommand(NetServer.OpCodes cmd) {
        byteCommand = (byte)cmd;
        opCode = cmd;
    }
}
