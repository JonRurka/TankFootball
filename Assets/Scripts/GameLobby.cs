using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class GameLobby : MonoBehaviour {
    public enum Team {
        TeamA = 1,
        TeamB = 2,
    }

    public static GameLobby Instance;

    private Dictionary<Team, List<Player>> _teams;
    
    void Awake() {
        Instance = this;
        _teams = new Dictionary<Team, List<Player>>();
        _teams.Add(Team.TeamA, new List<Player>());
        _teams.Add(Team.TeamB, new List<Player>());
    }

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void GUIUpdate() {
        int i = 0;
        foreach (Player usr in GameControl.Instance.players.Values.ToArray()) {
            GUI.Label(new Rect(10, 10 + i++ * 20, 100, 20), usr.Name + " (" + usr.Team + ")");
        }

        if (!GameControl.Instance.GameStarted && GUI.Button(new Rect(110, 10, 100, 20), "Start")) {
            GameControl.Instance.StartMatch();
        }
        /*if (GameControl.Instance != null && GameControl.Instance.IsServer) {
            foreach (Player usr in GameControl.Instance.players.Values.ToArray()) {
                GUI.Label(new Rect(10, 10 + i++ * 20, 100, 20), usr.Name + " (" + usr.Team + ")");
            }

            if (!GameControl.Instance.GameStarted && GUI.Button(new Rect(110, 10, 100, 20), "Start")) {
                GameControl.Instance.StartServer();
            }
        }
        else {
            foreach (Player usr in GameControl.Instance.players.Values.ToArray()) {
                GUI.Label(new Rect(10, 10 + i++ * 20, 100, 20), usr.Name + " (" + usr.Team + ")");
            }
        }*/
    }

    public void AddUser(Player user, Team team) {
        user.Team = team;
        _teams[team].Add(user);
    }

    public void RemoveUser(Player user) {
        RemoveUser(user.ID);
    }

    public void RemoveUser(byte id) {
        if (GameControl.Instance.UserExists(id)) {
            Player lobbyUser = GameControl.Instance.players[id];
            _teams[lobbyUser.Team].Remove(lobbyUser);
        }
    }

    public void ClearTeams() {
        _teams[Team.TeamA].Clear();
        _teams[Team.TeamB].Clear();
    }
}
