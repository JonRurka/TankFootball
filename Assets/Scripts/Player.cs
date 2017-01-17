using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class Player {
    public byte ID;
    public string Name;
    public GameObject GameObj;
    public TankMovement Instance;
    public GameLobby.Team Team;
    public bool TankExists;

    public Player(byte id, string name) {
        ID = id;
        Name = name;
    }

    public bool SetTank(GameObject tank) {
        if (tank != null) {
            GameObj = tank;
            Instance = tank.GetComponentInChildren<TankMovement>();
            if (Instance != null) {
                TankExists = true;
            }
        }
        else
            Debug.LogFormat("{0}: Tank not set!", Name);
        return TankExists;
    }

    public void DestroyTank() {
        if (TankExists) {
            UnityEngine.Object.Destroy(GameObj);
            GameObj = null;
            Instance = null;
            TankExists = false;
        }
    }
}
