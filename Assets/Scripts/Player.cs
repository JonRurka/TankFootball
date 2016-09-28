using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class Player {
    public byte ID { get; private set; }
    public string Name { get; private set; }
    public GameObject GameObj { get; set; }
    public TankMovement Instance { get; set; }
    public GameLobby.Team Team { get; set; }
    public bool TankExists { get; private set; }

    public Player(byte id, string name) {
        ID = id;
        Name = name;
    }

    public bool SetTank(GameObject tank) {
        if (tank != null) {
            GameObj = tank;
            Instance = tank.GetComponent<TankMovement>();
            if (Instance != null) {
                TankExists = true;
            }
        }
        return TankExists;
    }

    public void DestroyTank() {
        if (TankExists) {
            GameObject.Destroy(GameObj);
            GameObj = null;
            Instance = null;
            TankExists = false;
        }
    }
}
