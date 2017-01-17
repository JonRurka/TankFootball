using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyScript : MonoBehaviour {
    [System.Serializable]
    public struct LobbyEntry {
        public ushort hostID;
        public string description;

        public LobbyEntry(ushort hostID, string description) {
            this.hostID = hostID;
            this.description = description;
        }
    }

    public static LobbyScript Instance;

    public List<LobbyEntry> Matches;
    public event System.Action<LobbyEntry[]> OnLobbyRefresh;

    void Awake() {
        Instance = this;
        Matches = new List<LobbyEntry>();
    }

	// Use this for initialization
	void Start () {
        Debug.Log("Lobby started!");
    }
	
	// Update is called once per frame
	void Update () {
	    
	}

    public void GUIUpdate() {
        if (GUI.Button(new Rect(10, 10, 100, 20), "create game")) {
            CreateMatch();
        }

        if (GUI.Button(new Rect(Screen.width * 9/10, 10, 100, 20), "Refresh")) {
            Refresh();
        }

        for (int i = 0; i < Matches.Count; i++) {
            if (GUI.Button(new Rect(110, 10 + 20 * i, 500, 20), Matches[i].description)) {
                Connect(Matches[i].hostID, Matches[i].description);
            }
        }

    }

    public void CreateMatch() {
        MainServerConnect.Instance.RegisterServer();
    }

    public void Refresh() {
        MainServerConnect.Instance.GetLobby();
    }

    public void Connect(ushort host, string desc) {
        Debug.LogFormat("joining game {0}:{1}", host, desc);
        GameControl.Instance.Connect(host);
    }

    public void SetLobby(LobbyEntry[] entries) {
        Matches = new List<LobbyEntry>(entries);
        if (OnLobbyRefresh != null) {
            /*entries = new LobbyEntry[] {
                new LobbyEntry(0, "wubuwubuwubuwubuwubu"),
                new LobbyEntry(1, "hhhhhhhhhhhhhhiiiiiii"),
            };
            */
            OnLobbyRefresh.Invoke(entries);
        }
    }

    public void ClearLobby() {
        Matches.Clear();
        if (OnLobbyRefresh != null)
            OnLobbyRefresh.Invoke(new LobbyEntry[0]);
    }
}
