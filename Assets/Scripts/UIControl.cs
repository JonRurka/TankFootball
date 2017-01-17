using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIControl : MonoBehaviour {
    public enum UIType {
        Standalone,
        Mobile,
    }

    public static UIControl Instance;

    public UIPanelControl Lobby;
    public UIPanelControl GameLobby;
    public UIPanelControl Game;

    private UIPanelControl[] panels;

    void Awake() {
        Instance = this;
        panels = new UIPanelControl[] {
            Lobby,
            GameLobby,
            Game,
        };
    }

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DisableAll() {
        for (int i = 0; i < panels.Length; i++) {
            panels[i].UIEnabled(false);
        }
    }

    public void EnableLobby() {
        DisableAll();
        Lobby.UIEnabled(true);
    }

    public void EnableGameLobby() {
        DisableAll();
        GameLobby.UIEnabled(true);
    }

    public void EnableGame() {
        DisableAll();
        Game.UIEnabled(true);
    }

    public void EnableDisconnected() {
        
    }
}
