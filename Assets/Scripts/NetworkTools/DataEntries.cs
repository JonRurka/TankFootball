using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class DataEntries {
    public class Entry {
        public DataTypes Type { get; private set; }
        public object Data { get; private set; }
        public byte[] EncodedData { get; private set; }
        public int Length { get; private set; }

        /// <summary>
        /// Constructor for encoding data.
        /// </summary>
        /// <param name="type">Data type of the object.</param>
        /// <param name="data">Data to encode. </param>
        public Entry(DataTypes type, object data) {
            Type = type;
            Data = data;
            Encode();
        }

        /// <summary>
        /// Constructor for decoding data.
        /// </summary>
        /// <param name="buffer">Buffer to decode.</param>
        /// <param name="type">Data type to decode the data to.</param>
        public Entry(byte[] buffer, DataTypes type) {
            Type = type;
            EncodedData = buffer;
            Length = buffer.Length;
            Decode();
        }

        public void Encode() {
            byte[] buffer = new byte[0];
            Vector2 vector2;
            Vector3 vector3;
            Quaternion quat;
            byte[] bx;
            byte[] by;
            byte[] bz;
            byte[] bw;
            switch (Type) {
                case DataTypes.Boolean: // 1, 8
                    buffer = BitConverter.GetBytes((bool)Data);
                    break;
                case DataTypes.Byte: // 1, 8
                    buffer = new byte[] { (byte)Data };
                    break;
                case DataTypes.Char: // 2, 16
                    buffer = BitConverter.GetBytes((char)Data);
                    break;
                case DataTypes.Decimal: // 16, 128
                    Int32[] bits = decimal.GetBits((decimal)Data);
                    List<byte> bytes = new List<byte>(4);
                    foreach (Int32 i in bits)
                        bytes.AddRange(BitConverter.GetBytes(i));
                    buffer = bytes.ToArray();
                    break;
                case DataTypes.Double: // 8, 64
                    buffer = BitConverter.GetBytes((double)Data);
                    break;
                case DataTypes.Float: // 4, 32
                    buffer = BitConverter.GetBytes((float)Data);
                    break;
                case DataTypes.Short: // 2, 16
                    buffer = BitConverter.GetBytes((short)Data);
                    break;
                case DataTypes.Int: // 4, 32
                    buffer = BitConverter.GetBytes((int)Data);
                    break;
                case DataTypes.Long: // 8, 64
                    buffer = BitConverter.GetBytes((long)Data);
                    break;
                case DataTypes.Ushort: // 2, 16
                    buffer = BitConverter.GetBytes((ushort)Data);
                    break;
                case DataTypes.Uint: // 4, 32
                    buffer = BitConverter.GetBytes((uint)Data);
                    break;
                case DataTypes.ULong: // 8, 64
                    buffer = BitConverter.GetBytes((ulong)Data);
                    break;
                case DataTypes.String: // varries 0 - 65,535
                    buffer = Encoding.UTF8.GetBytes((string)Data);
                    if (buffer.Length > ushort.MaxValue)
                        throw new Exception("String too large.");
                    break;
                case DataTypes.Buffer: // varries 0 - 65,535
                    buffer = (byte[])Data;
                    if (buffer.Length > ushort.MaxValue)
                        throw new Exception("Buffer too large.");
                    break;
                case DataTypes.Vector2: // 8, 64
                    vector2 = (Vector2)Data;
                    bx = BitConverter.GetBytes(vector2.x);
                    by = BitConverter.GetBytes(vector2.y);
                    buffer = new byte[] { bx[0], bx[1], bx[2], bx[3],
                                              by[0], by[1], by[2], by[3] };
                    break;
                case DataTypes.ShortVector2: // 4, 32
                    vector2 = (Vector2)Data;
                    bx = BitConverter.GetBytes((short)vector2.x);
                    by = BitConverter.GetBytes((short)vector2.y);
                    buffer = new byte[] { bx[0], bx[1],
                                              by[0], by[1] };
                    break;
                case DataTypes.Vector3: // 12, 96
                    vector3 = (Vector3)Data;
                    bx = BitConverter.GetBytes(vector3.x);
                    by = BitConverter.GetBytes(vector3.y);
                    bz = BitConverter.GetBytes(vector3.z);
                    buffer = new byte[] { bx[0], bx[1], bx[2], bx[3],
                                              by[0], by[1], by[2], by[3],
                                              bz[0], bz[1], bz[2], bz[3] };
                    break;
                case DataTypes.ShortVector3: // 6, 48
                    vector3 = (Vector3)Data;
                    bx = BitConverter.GetBytes((short)vector3.x);
                    by = BitConverter.GetBytes((short)vector3.y);
                    bz = BitConverter.GetBytes((short)vector3.z);
                    buffer = new byte[] { bx[0], bx[1],
                                              by[0], by[1],
                                              bz[0], bz[1] };
                    break;
                case DataTypes.UShortVector2: // 4, 32
                    vector2 = (Vector2)Data;
                    bx = BitConverter.GetBytes((ushort)vector2.x);
                    by = BitConverter.GetBytes((ushort)vector2.y);
                    buffer = new byte[] { bx[0], bx[1],
                                              by[0], by[1] };
                    break;
                case DataTypes.UShortVector3: // 6, 48
                    vector3 = (Vector3)Data;
                    bx = BitConverter.GetBytes((ushort)vector3.x);
                    by = BitConverter.GetBytes((ushort)vector3.y);
                    bz = BitConverter.GetBytes((ushort)vector3.z);
                    buffer = new byte[] { bx[0], bx[1],
                                              by[0], by[1],
                                              bz[0], bz[1] };
                    break;
                case DataTypes.ByteVector2: // 2, 8
                    vector2 = (Vector2)Data;
                    bx = BitConverter.GetBytes((byte)vector2.x);
                    by = BitConverter.GetBytes((byte)vector2.y);
                    buffer = new byte[] { bx[0],
                                              by[0], };
                    break;
                case DataTypes.ByteVector3: // 3, 16
                    vector3 = (Vector3)Data;
                    bx = BitConverter.GetBytes((byte)vector3.x);
                    by = BitConverter.GetBytes((byte)vector3.y);
                    bz = BitConverter.GetBytes((byte)vector3.z);
                    buffer = new byte[] { bx[0],
                                              by[0],
                                              bz[0], };
                    break;
                case DataTypes.ByteQuaternion: // 4, 32
                    quat = (Quaternion)Data;
                    byte brx = (byte)TankState.Scale(quat.x, -1, 1, 0, 255);
                    byte bry = (byte)TankState.Scale(quat.y, -1, 1, 0, 255);
                    byte brz = (byte)TankState.Scale(quat.z, -1, 1, 0, 255);
                    byte brw = (byte)TankState.Scale(quat.w, -1, 1, 0, 255);
                    buffer = new byte[] { brx, bry, brz, brw };
                    break;
                case DataTypes.ShortQuaternion: // 8, 64
                    quat = (Quaternion)Data;
                    ushort srx = (ushort)TankState.Scale(quat.x, -1, 1, 0, ushort.MaxValue);
                    ushort sry = (ushort)TankState.Scale(quat.y, -1, 1, 0, ushort.MaxValue);
                    ushort srz = (ushort)TankState.Scale(quat.z, -1, 1, 0, ushort.MaxValue);
                    ushort srw = (ushort)TankState.Scale(quat.w, -1, 1, 0, ushort.MaxValue);
                    bx = BitConverter.GetBytes(srx);
                    by = BitConverter.GetBytes(sry);
                    bz = BitConverter.GetBytes(srz);
                    bw = BitConverter.GetBytes(srw);
                    buffer = new byte[] { bx[0], bx[1],
                                              by[0], by[1],
                                              bz[0], bz[1],
                                              bw[0], bw[1], };
                    break;
                case DataTypes.Quaternion: // 16, 128
                    quat = (Quaternion)Data;
                    bx = BitConverter.GetBytes(quat.x);
                    by = BitConverter.GetBytes(quat.y);
                    bz = BitConverter.GetBytes(quat.z);
                    bw = BitConverter.GetBytes(quat.w);
                    buffer = new byte[] { bx[0], bx[1], bx[2], bx[3],
                                              by[0], by[1], by[2], by[3],
                                              bz[0], bz[1], bz[2], bz[3],
                                              bw[0], bw[1], bw[2], bw[3] };
                    break;
            }
            EncodedData = buffer;
            Length = buffer.Length;
        }

        public void Decode() {
            object x;
            object y;
            object z;
            object w;
            switch (Type) {
                case DataTypes.Boolean: // 1, 8
                    Data = BitConverter.ToBoolean(EncodedData, 0);
                    break;
                case DataTypes.Byte: // 1, 8
                    Data = EncodedData[0];
                    break;
                case DataTypes.Char: // 2, 16
                    Data = BitConverter.ToBoolean(EncodedData, 0);
                    break;
                case DataTypes.Decimal: // 16, 128
                    Int32[] bits = new Int32[4];
                    for (int i = 0; i <= 15; i += 4) {
                        bits[i / 4] = BitConverter.ToInt32(EncodedData, i);
                    }
                    Data = new decimal(bits);
                    break;
                case DataTypes.Double: // 8, 64
                    Data = BitConverter.ToDouble(EncodedData, 0);
                    break;
                case DataTypes.Float: // 4, 32
                    Data = BitConverter.ToSingle(EncodedData, 0);
                    break;
                case DataTypes.Short: // 2, 16
                    Data = BitConverter.ToInt16(EncodedData, 0);
                    break;
                case DataTypes.Int: // 4, 32
                    Data = BitConverter.ToInt32(EncodedData, 0);
                    break;
                case DataTypes.Long: // 8, 64
                    Data = BitConverter.ToInt64(EncodedData, 0);
                    break;
                case DataTypes.Ushort: // 2, 16
                    Data = BitConverter.ToUInt16(EncodedData, 0);
                    break;
                case DataTypes.Uint: // 4, 32
                    Data = BitConverter.ToUInt32(EncodedData, 0);
                    break;
                case DataTypes.ULong: // 8, 64
                    Data = BitConverter.ToUInt64(EncodedData, 0);
                    break;
                case DataTypes.String: // varries 0 - 65,535
                    Data = Encoding.UTF8.GetString(EncodedData);
                    break;
                case DataTypes.Buffer: // varries 0 - 65,535
                    Data = EncodedData;
                    break;
                case DataTypes.Vector2: // 8, 64
                    x = BitConverter.ToSingle(EncodedData, 0);
                    y = BitConverter.ToSingle(EncodedData, 4);
                    Data = new Vector2((float)x, (float)y);
                    break;
                case DataTypes.ShortVector2: // 4, 32
                    x = BitConverter.ToInt16(EncodedData, 0);
                    y = BitConverter.ToInt16(EncodedData, 2);
                    Data = new Vector2((short)x, (short)y);
                    break;
                case DataTypes.Vector3: // 12, 96
                    x = BitConverter.ToSingle(EncodedData, 0);
                    y = BitConverter.ToSingle(EncodedData, 4);
                    z = BitConverter.ToSingle(EncodedData, 8);
                    Data = new Vector3((float)x, (float)y, (float)z);
                    break;
                case DataTypes.ShortVector3: // 6, 48
                    x = BitConverter.ToInt16(EncodedData, 0);
                    y = BitConverter.ToInt16(EncodedData, 2);
                    z = BitConverter.ToInt16(EncodedData, 4);
                    Data = new Vector3((short)x, (short)y, (short)z);
                    break;
                case DataTypes.UShortVector2: // 4, 32
                    x = BitConverter.ToUInt16(EncodedData, 0);
                    y = BitConverter.ToUInt16(EncodedData, 2);
                    Data = new Vector2((ushort)x, (ushort)y);
                    break;
                case DataTypes.UShortVector3: // 6, 48
                    x = BitConverter.ToUInt16(EncodedData, 0);
                    y = BitConverter.ToUInt16(EncodedData, 2);
                    z = BitConverter.ToUInt16(EncodedData, 4);
                    Data = new Vector3((ushort)x, (ushort)y, (ushort)z);
                    break;
                case DataTypes.ByteVector2: // 2, 8
                    x = EncodedData[0];
                    y = EncodedData[1];
                    Data = new Vector2((byte)x, (byte)y);
                    break;
                case DataTypes.ByteVector3: // 3, 16
                    x = EncodedData[0];
                    y = EncodedData[1];
                    z = EncodedData[2];
                    Data = new Vector3((byte)x, (byte)y, (byte)z);
                    break;
                case DataTypes.ByteQuaternion: // 4, 32
                    x = EncodedData[0];
                    y = EncodedData[1];
                    z = EncodedData[2];
                    w = EncodedData[3];
                    x = TankState.Scale((byte)x, 0, 255, -1, 1);
                    y = TankState.Scale((byte)y, 0, 255, -1, 1);
                    z = TankState.Scale((byte)z, 0, 255, -1, 1);
                    w = TankState.Scale((byte)w, 0, 255, -1, 1);
                    Data = new Quaternion((float)x, (float)y, (float)z, (float)w);
                    break;
                case DataTypes.ShortQuaternion: // 8, 64
                    x = BitConverter.ToUInt16(EncodedData, 0);
                    y = BitConverter.ToUInt16(EncodedData, 2);
                    z = BitConverter.ToUInt16(EncodedData, 4);
                    w = BitConverter.ToUInt16(EncodedData, 6);
                    x = TankState.Scale((ushort)x, 0, ushort.MaxValue, -1, 1);
                    y = TankState.Scale((ushort)y, 0, ushort.MaxValue, -1, 1);
                    z = TankState.Scale((ushort)z, 0, ushort.MaxValue, -1, 1);
                    w = TankState.Scale((ushort)w, 0, ushort.MaxValue, -1, 1);
                    Data = new Quaternion((float)x, (float)y, (float)z, (float)w);
                    break;
                case DataTypes.Quaternion: // 16, 128
                    x = BitConverter.ToSingle(EncodedData, 0);
                    y = BitConverter.ToSingle(EncodedData, 4);
                    z = BitConverter.ToSingle(EncodedData, 8);
                    w = BitConverter.ToSingle(EncodedData, 12);
                    Data = new Quaternion((float)x, (float)y, (float)z, (float)w);
                    break;
            }
        }
    }

    public int Count { get { return _entries.Count; } }

    private List<Entry> _entries;

    public DataEntries() {
        _entries = new List<Entry>();
    }

    public DataEntries(params Entry[] entries) : this() {
        foreach (Entry ent in entries) {
            _entries.Add(ent);
        }
    }

    public void AddEntry(object value, DataTypes type = DataTypes.Unsupported) {
        DataTypes detected = GetDataType(value);
        if (detected == DataTypes.Unsupported)
            throw new Exception("Unsupported data type: " + value.GetType().ToString());
        if (type != DataTypes.Unsupported)
            _entries.Add(new Entry(type, value));
        else
            _entries.Add(new Entry(detected, value));
    }

    public object GetEntryValue(int index) {
        if (index < _entries.Count)
            return _entries[index].Data;
        return null;
    }

    public Entry GetEntry(int index) {
        if (index < _entries.Count)
            return _entries[index];
        return null;
    }

    public Entry[] GetEntries() {
        return _entries.ToArray();
    }

    public byte[] Encode(bool addMetaData = true) {
        return DataEncoder.Encode(this, addMetaData);
    }

    private DataTypes GetDataType(object t) {
        DataTypes type = DataTypes.Unsupported;
        if (t is bool) { type = DataTypes.Boolean; }
        if (t is byte) { type = DataTypes.Byte; }
        if (t is char) { type = DataTypes.Char; }
        if (t is decimal) { type = DataTypes.Decimal; }
        if (t is double) { type = DataTypes.Double; }
        if (t is float) { type = DataTypes.Float; }
        if (t is short) { type = DataTypes.Short; }
        if (t is int) { type = DataTypes.Int; }
        if (t is long) { type = DataTypes.Long; }
        if (t is ushort) { type = DataTypes.Ushort; }
        if (t is uint) { type = DataTypes.Uint; }
        if (t is ulong) { type = DataTypes.ULong; }
        if (t is string) { type = DataTypes.String; }
        if (t is byte[]) { type = DataTypes.Buffer; }
        if (t is Vector2) { type = DataTypes.Vector2; }
        if (t is Vector3) { type = DataTypes.Vector3; }
        if (t is Quaternion) { type = DataTypes.Quaternion; }
        return type;
    }
}

