using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum DataTypes {
    Unsupported,
    Boolean, // 1, 8
    Byte, // 1, 8
    Char, // 2, 16
    Decimal, // 16, 128
    Double, // 8, 64
    Float, // 4, 32
    Short, // 2, 16
    Int, // 4, 32
    Long, // 8, 64
    Ushort, // 2, 16
    Uint, // 4, 32
    ULong, // 8, 64

    String, // varries 0 - 65,535
    Buffer, // varries 0 - 65,535

    Vector2, // 8, 64
    ShortVector2, // 4, 32
    Vector3, // 12, 96
    ShortVector3, // 6, 48
    UShortVector2, // 4, 32
    UShortVector3, // 6, 48
    ByteVector2, // 2, 8
    ByteVector3, // 3, 16

    ByteQuaternion, // 4, 32
    ShortQuaternion, // 8, 64
    Quaternion, // 16, 128
}

