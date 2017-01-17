using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DataEncoder {
    /*
     - first byte is number of entries if metadata is included.
     - Every entry gets a 1 byte entry at beginning to specify data type if metadata is included.
     - next section is the all the data. If the data if of varrying length, length is included in front in either 1 byte or 2
        depending on length.
     - The metadata can be omitted if the metadata is known by the other decoder.
     */

    public static byte[] Encode(DataEntries dataEntries, bool addMetaData = true) {
        List<byte> result = new List<byte>();
        List<byte> metaList = new List<byte>();
        List<byte> data = new List<byte>();
        if (addMetaData)
            metaList.Add((byte)dataEntries.Count);
        DataEntries.Entry[] entries = dataEntries.GetEntries();
        for (int i = 0; i < entries.Length; i++) {
            DataTypes type = entries[i].Type;
            if (addMetaData)
                metaList.Add((byte)type);
            if (type == DataTypes.String || type == DataTypes.Buffer) {
                byte[] buff = BitConverter.GetBytes((ushort)entries[i].Length);
                data.AddRange(buff);
            }
            data.AddRange(entries[i].EncodedData);
        }
        result.AddRange(metaList);
        result.AddRange(data);
        return result.ToArray();
    }
}

