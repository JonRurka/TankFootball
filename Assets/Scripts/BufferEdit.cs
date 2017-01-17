using System;
using System.Collections.Generic;

public static class BufferEdit {
    public static byte[] RemoveLength(byte[] origin) {
        List<byte> dst = new List<byte>(origin);
        dst.RemoveAt(0);
        dst.RemoveAt(0);
        return dst.ToArray();
    }

    public static byte[] RemoveCmd(byte[] origin) {
        List<byte> dst = new List<byte>(origin);
        dst.RemoveAt(0);
        return dst.ToArray();
    }

    public static byte[] AddFirst(byte byteToAdd, byte[] origin) {
        List<byte> dst = new List<byte>();
        dst.Add(byteToAdd);
        dst.AddRange(origin);
        return dst.ToArray();
    }

    public static byte[] Add(params byte[][] buffers) {
        List<byte> dst = new List<byte>();
        for (int i = 0; i < buffers.GetLength(0); i++) {
            dst.AddRange(buffers[i]);
        }
        return dst.ToArray();
    }

    public static bool IsFlagSet(byte value, int flag) {
        if (flag < 0 || flag > 7)
            throw new ArgumentOutOfRangeException("pos", "Index must be in the range of 0-7.");
        return (value & (1 << flag)) != 0;
    }

    public static byte SetFlag(byte value, int flag) {
        if (flag < 0 || flag > 7)
            throw new ArgumentOutOfRangeException("pos", "Index must be in the range of 0-7.");
        return (byte)(value | (1 << flag));
    }

    public static byte UnsetFlage(byte value, int flag) {
        if (flag < 0 || flag > 7)
            throw new ArgumentOutOfRangeException("pos", "Index must be in the range of 0-7.");
        return  (byte)(value & ~(1 << flag));
    }

    public static byte ToggleFlag(byte value, int flag) {
        if (flag < 0 || flag > 7)
            throw new ArgumentOutOfRangeException("pos", "Index must be in the range of 0-7.");
        return (byte)(value ^ (1 << flag));
    }

    public static string FlagString(byte value) {
        return Convert.ToString(value, 2).PadLeft(8, '0');
    }
}
