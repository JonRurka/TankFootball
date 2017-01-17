using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class DataDecoder {
    public static DataEntries Decode(byte[] buffer, DataTypes[] metaData = null) {
        bool parseMetaData = metaData == null;
        int count = parseMetaData ? buffer[0] : metaData.Length;
        byte[] dataBuff = buffer;
        if (parseMetaData) {
            byte[] metaDataBuff = new byte[count];
            metaData = new DataTypes[count];
            Array.Copy(buffer, 1, metaDataBuff, 0, count);
            for (int i = 0; i < count; i++) {
                metaData[i] = (DataTypes)metaDataBuff[i];
            }
            dataBuff = new byte[buffer.Length - (count + 1)];
            Array.Copy(buffer, count + 1, dataBuff, 0, dataBuff.Length);
        }
        DataEntries.Entry[] entries = new DataEntries.Entry[count];
        for (int i = 0, r = 0; i < count; i++) {
            int length = GetLength(metaData[i]);
            if (metaData[i] == DataTypes.String ||
                metaData[i] == DataTypes.Buffer) {
                length = BitConverter.ToUInt16(dataBuff, r);
                r += 2;
            }
            byte[] entryBuff = new byte[length];
            Array.Copy(dataBuff, r, entryBuff, 0, length);
            entries[i] = new DataEntries.Entry(entryBuff, metaData[i]);
            r += length;
        }
        return new DataEntries(entries);
    }

    private static int GetLength(DataTypes type) {
        int length = 0;
        switch (type) {
            case DataTypes.Boolean: // 1, 8
                length = 1;
                break;
            case DataTypes.Byte: // 1, 8
                length = 1;
                break;
            case DataTypes.Char: // 2, 16
                length = 2;
                break;
            case DataTypes.Decimal: // 16, 128
                length = 16;
                break;
            case DataTypes.Double: // 8, 64
                length = 8;
                break;
            case DataTypes.Float: // 4, 32
                length = 4;
                break;
            case DataTypes.Short: // 2, 16
                length = 2;
                break;
            case DataTypes.Int: // 4, 32
                length = 4;
                break;
            case DataTypes.Long: // 8, 64
                length = 8;
                break;
            case DataTypes.Ushort: // 2, 16
                length = 2;
                break;
            case DataTypes.Uint: // 4, 32
                length = 4;
                break;
            case DataTypes.ULong: // 8, 64
                length = 5;
                break;
            case DataTypes.String: // varries 0 - 65,535
                length = -1;
                break;
            case DataTypes.Buffer: // varries 0 - 65,535
                length = -1;
                break;
            case DataTypes.Vector2: // 8, 64
                length = 8;
                break;
            case DataTypes.ShortVector2: // 4, 32
                length = 4;
                break;
            case DataTypes.Vector3: // 12, 96
                length = 12;
                break;
            case DataTypes.ShortVector3: // 6, 48
                length = 6;
                break;
            case DataTypes.UShortVector2: // 4, 32
                length = 4;
                break;
            case DataTypes.UShortVector3: // 6, 48
                length = 6;
                break;
            case DataTypes.ByteVector2: // 2, 8
                length = 2;
                break;
            case DataTypes.ByteVector3: // 3, 16
                length = 3;
                break;
            case DataTypes.ByteQuaternion: // 4, 32
                length = 4;
                break;
            case DataTypes.ShortQuaternion: // 8, 64
                length = 8;
                break;
            case DataTypes.Quaternion: // 16, 128
                length = 16;
                break;
        }
        return length;
    }
}

