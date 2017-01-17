using UnityEngine;
using System;
using System.Collections;

public class Command : Attribute {
    public readonly byte Value;
    public readonly ClientCMD Opcode;
    public readonly bool Async;

    public Command(byte command, bool async = false) {
        Value = command;
        Opcode = (ClientCMD)command;
        Async = async;
    }

    public Command(ClientCMD opcode, bool async = false) {
        Opcode = opcode;
        Value = (byte)opcode;
        Async = async;
    }

}
