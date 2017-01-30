using System.Collections;
using System.Collections.Generic;

public static class DataTypePresets {
    public static DataTypes[] SetMatches = new DataTypes[] {
        DataTypes.Ushort, DataTypes.Buffer,
    };

    public static DataTypes[] CompleteRegister = new DataTypes[] {
        DataTypes.Boolean, DataTypes.Ushort,
    };

    public static DataTypes[] UpdateLobbyUsers = new DataTypes[] {
        DataTypes.Byte, DataTypes.Buffer,
    };

    public static DataTypes[] JoinMatchComplete = new DataTypes[] {
        DataTypes.Byte, DataTypes.Byte, DataTypes.Byte,
    };

    public static DataTypes[] TankState = new DataTypes[] {
        DataTypes.Byte, DataTypes.ShortVector2, DataTypes.Short, DataTypes.Byte, DataTypes.Short, DataTypes.ByteVector2, DataTypes.Byte
    };

    public static DataTypes[] UpdateOwnerTankPosition = new DataTypes[] {
        DataTypes.Ushort, DataTypes.Buffer,
    };

    public static DataTypes[] UpdateClientTankPosition = new DataTypes[] {
        DataTypes.Byte, DataTypes.Buffer,
    };

    public static DataTypes[] SetTanks = new DataTypes[] {
        DataTypes.Byte, DataTypes.Buffer,
    };

    public static DataTypes[] SetTanksData = new DataTypes[] {
        DataTypes.Byte, DataTypes.ShortVector2, DataTypes.Byte, DataTypes.Float,
    };

    public static DataTypes[] ChatUpdate = new DataTypes[] {
        DataTypes.Byte, DataTypes.String, DataTypes.String,
    };
}
