using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GamePlay : MonoBehaviour, IGamePlayController {
    public class TeamStats {
        public GameLobby.Team Team { get; private set; }
        public Dictionary<byte, Player> Players { get; private set; }
        public string TeamName { get; private set; }
        public int Score { get; set; }
        public bool IsOffense { get; set; }
        public List<byte> idleSpots;

        public TeamStats(GameLobby.Team team, string name) {
            Team = team;
            Players = new Dictionary<byte, Player>();
            idleSpots = new List<byte>();
            TeamName = name;
            Score = 0;
            IsOffense = false;
        }

        public TeamStats(GameLobby.Team team) : this (team, team.ToString()) {
        }
    }

    public enum GameState {
        PreGame,
        KickOff,
    }

    public IGamePlayController instance { get { return Instance; } }
    public static GamePlay Instance;
    public GameLobby.Team offence;
    public GameLobby.Team defence;
    public GameState state;
    public Dictionary<GameLobby.Team, TeamStats> Stats;
    public Dictionary<string, CountDown> Countdowns;

    public float countDownStart = 10;
    private bool doCountDown;
    private float countDownTime = 0;


    void Awake() {
        Instance = this;
        Stats = new Dictionary<GameLobby.Team, TeamStats>();
        Stats.Add(GameLobby.Team.TeamA, new TeamStats(GameLobby.Team.TeamA));
        Stats.Add(GameLobby.Team.TeamB, new TeamStats(GameLobby.Team.TeamB));
        state = GameState.PreGame;
    }

	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
        /*if (Input.GetKeyDown(KeyCode.Z)) {
            GameBall.Instance.GiveBall(0);
        }
        if (Input.GetKeyDown(KeyCode.F)) {
            FeildGoal(PlayerControl.Instance.GetTank(0), GameLobby.Team.TeamA, 0);
        }*/

        /*foreach (CountDown count in Countdowns.Values) {
            count.Update(Time.deltaTime);
        }*/
        
    }

    void OnGUI() {
        GUI.Label(new Rect(Screen.width * 9 / 10, 10, 100, 20), Stats[GameLobby.Team.TeamA].Score + " :TeamA");
        GUI.Label(new Rect(Screen.width * 9 / 10, 30, 100, 20), Stats[GameLobby.Team.TeamB].Score + " :TeamB");
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
        switch (state) {
            case GameState.PreGame:
                CoinFlip();
                break;
        }
    }

    public void GameTimerEnd_Callback() {

    }

    public Vector3 SetTank(Player tank) {
        GameLobby.Team team = tank.Team;
        TeamStats stats = Stats[team];
        stats.Players.Add(tank.ID, tank);

        int side = team == GameLobby.Team.TeamA ? 1 : -1;
        tank.GameObj.transform.position = new Vector3(25 * side, 0, 0);
        //Stats[team].idleSpots.Add(tank.ID);
        //SetSideLine(team);
        return tank.GameObj.transform.position;
    }

    public void Takle(byte playerID, byte VictimID) {
        Player player = GameControl.Instance.GetUser(playerID);
        Player victim = GameControl.Instance.GetUser(VictimID);

        victim.Instance.SetEnabled(false);
        IncreaseScore(player.Team, 1);
        Reset(VictimID, 2);
        /*if (victim.instance.HasBall && lobbyPlayer.team != lobbyEnemy.team) {
            
        }*/
    }

    public void IncreaseScore(GameLobby.Team team, int amount) {
        Stats[team].Score += amount;
        int aScore = Stats[GameLobby.Team.TeamA].Score;
        int bScore = Stats[GameLobby.Team.TeamB].Score;
        NetServer.Instance.Send(NetClient.OpCodes.SetScore, new byte[] { (byte)aScore, (byte)bScore });
    }

    public void BallCatch(byte playerID) {
        Player player = GameControl.Instance.GetUser(playerID);
        GameBall.Instance.GiveBall(playerID);
    }

    public void SetTeams(GameLobby.Team offense, GameLobby.Team defence) {
        if (offence != defence) {
            this.offence = offense;
            this.defence = defence;
            Stats[offense].IsOffense = true;
            Stats[defence].IsOffense = false;
        }
    }

    public void KickOff(GameLobby.Team team) {
        
    }

    public void FeildGoal(Player player, GameLobby.Team team, float xLocation) {
        player.Instance.turrentInst.SetShootMode(TurretScript.ShootMode.Kickoff);
        GameBall.Instance.SpawnTee(xLocation, -1);
    }

    public void SideLineTanks() {
        float offset = 2;
        
    }

    public void SetSideLine(GameLobby.Team team) {
        float offset = 2;
        TeamStats tStats = Stats[team];
        float length = tStats.idleSpots.Count * offset;
        int side = 1;
        if (team == GameLobby.Team.TeamB)
            side = -1;
        Vector3 pos = new Vector3(-length / 2, 0, 25 * side);
        for (int i = 0; i < tStats.idleSpots.Count; i++) {
            byte pid = tStats.idleSpots[i];
            Player player = tStats.Players[pid];
            player.Instance.SetPosition(pos);
            player.Instance.SetEnabled(false);
            pos.x += offset;
        }
    }

    public void Reset(byte id, float time) {
        StartCoroutine(Reset_int(id, time));
    }

    public void CoinFlip() {
        float cointVal = Random.value;
        if (cointVal > .5f) {
            state = GameState.KickOff;
        }
    }

    IEnumerator Reset_int(byte id, float time) {
        yield return new WaitForSeconds(time);
        Player user = GameControl.Instance.GetUser(id);
        int side = 0;
        if (user.Team == GameLobby.Team.TeamA)
            side = 1;
        else
            side = -1;
        PlayerControl.Instance.SetPosition(id, new Vector3(50 * side, 0, 0), new Vector3(0, -side * 90, 0));
        user.Instance.SetEnabled(true);
        
    }
}
