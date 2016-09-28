using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyScript : MonoBehaviour {
    [System.Serializable]
    public struct LobbyEntry {
        public string host;
        public string description;

        public LobbyEntry(string host, string description) {
            this.host = host;
            this.description = description;
        }
    }

    public static LobbyScript Instance;

    public List<LobbyEntry> Matches;

    void Awake() {
        Instance = this;
        Matches = new List<LobbyEntry>();
    }

	// Use this for initialization
	void Start () {
        Debug.Log("Lobby started!");
        MainServerConnect.Instance.GetLobby();
    }
	
	// Update is called once per frame
	void Update () {
	    
	}

    void OnGUI() {
        if (GUI.Button(new Rect(10, 10, 100, 20), "create game")) {
            MainServerConnect.Instance.RegisterServer(GameControl.Instance.publicIp);
        }

        if (GUI.Button(new Rect(Screen.width * 9/10, 10, 100, 20), "Refresh")) {
            MainServerConnect.Instance.GetLobby();
        }

        for (int i = 0; i < Matches.Count; i++) {
            if (GUI.Button(new Rect(110, 10 + 20 * i, 500, 20), Matches[i].description)) {
                Connect(Matches[i].host);
            }
        }
        
    }

    public void Connect(string host) {
        Debug.Log("Connecting to " + host);
        NetClient.Instance.Connect(host);
    }

    public void SetLobby(LobbyEntry[] entries) {
        Matches = new List<LobbyEntry>(entries);
    }
}
