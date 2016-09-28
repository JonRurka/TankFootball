using UnityEngine;
using System;
using System.Collections;

public class ClientCommand : Attribute {
    public byte byteCommand;
    public NetClient.OpCodes opCode;

    public ClientCommand(byte cmd) {
        byteCommand = cmd;
        opCode = (NetClient.OpCodes)cmd;
    }

    public ClientCommand(NetClient.OpCodes cmd) {
        byteCommand = (byte)cmd;
        opCode = cmd;
    }
}
