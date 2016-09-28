using System;

public class NetCommand : Attribute {
    public byte command;
    public MainServerConnect.OpCodes opcode;

    public NetCommand(byte command) {
        this.command = command;
        this.opcode = (MainServerConnect.OpCodes)command;
    }

    public NetCommand(MainServerConnect.OpCodes opcode) {
        this.opcode = opcode;
        this.command = (byte)opcode;
    }
}
