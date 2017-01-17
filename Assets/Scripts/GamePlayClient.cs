using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GamePlayClient : MonoBehaviour, IGamePlayController {

    public IGamePlayController instance { get { return Instance; } }
    public static GamePlayClient Instance;

    public Dictionary<GameLobby.Team, TeamStats> Stats;
    public Dictionary<string, CountDown> Countdowns;


    void Awake() {
        Instance = this;
    }

    // Use this for initialization
    void Start () {
        Countdowns = new Dictionary<string, CountDown>();
        Stats = new Dictionary<GameLobby.Team, TeamStats>();
        Stats.Add(GameLobby.Team.TeamA, new TeamStats(GameLobby.Team.TeamA));
        Stats.Add(GameLobby.Team.TeamB, new TeamStats(GameLobby.Team.TeamB));
    }
	
	// Update is called once per frame
	void Update () {
	    foreach(CountDown count in Countdowns.Values) {
            count.Update(Time.deltaTime);
        }
	}

    public void GUIUpdate() {
        //GUI.Label(new Rect(Screen.width * 9 / 10, 10, 100, 20), Stats[GameLobby.Team.TeamA].Score + " :TeamA");
        //GUI.Label(new Rect(Screen.width * 9 / 10, 30, 100, 20), Stats[GameLobby.Team.TeamB].Score + " :TeamB");
    }

    public void SetScore(int teamA, int teamB) {
        Stats[GameLobby.Team.TeamA].Score = teamA;
        Stats[GameLobby.Team.TeamB].Score = teamB;
    }

    public void AddPlayer(Player player) {
        if (!Stats[player.Team].Players.ContainsKey(player.ID))
            Stats[player.Team].Players.Add(player.ID, player);
    }

    public void RemovePlayer(Player player) {
        if (Stats[player.Team].Players.ContainsKey(player.ID))
            Stats[player.Team].Players.Remove(player.ID);
    }

    public void StartGameCountDown(float time) {
        CreateCountDown("StartGame", time, GameStarted_Callback);
    }

    public void StartGameTimer(float time) {
        CreateCountDown("GameTimer", time, GameTimerEnd_Callback);
    }

    public void CreateCountDown(string name, float time, System.Action callback, bool repeat = false) {
        if (!Countdowns.ContainsKey(name)) {
            Countdowns.Add(name, new CountDown(this, name, time, callback, repeat));
        }
    }

    public void RemoveCountDown(string name) {
        if (Countdowns.ContainsKey(name))
            Countdowns.Remove(name);
    }

    public void GameStarted_Callback() {
        // remove GUI
    }

    public void GameTimerEnd_Callback() {
        
    }
}
