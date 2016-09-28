using System;
using System.Collections.Generic;

public static class BufferEdit {
    public static byte[] RemoveFirst(byte[] origin) {
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
}
