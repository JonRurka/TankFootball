using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

public class GameControl : MonoBehaviour {
    public static GameControl Instance;

    public GameObject ballPrefab;

    public string mainServerHost;
    public string userName;
    public string password;
    public string sessionKey;
    public byte netID;
    public string publicIp;
    public System.Diagnostics.Stopwatch pingWatch;
    public string errorMsg;

    public Dictionary<byte, Player> players { get; private set; }

    public bool GameStarted;
    public bool IsServer;

    private bool register = false;
    private bool invalid;

    private string confirmPassword = "";
    private string email = "";
    private bool gameComplete = false;
    private bool win = false;
    

    void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start() {
        players = new Dictionary<byte, Player>();
        string[] commandArgs = Environment.GetCommandLineArgs();
        ProcessCommandArgs(commandArgs);
        MainServerConnect.Instance.AddCommands(this);
        NetClient.Instance.AddCommands(this);
        mainServerHost = MainServerConnect.Instance.host;
        GetPublicIP();
    }

    // Update is called once per frame
    void Update() {
        
    }

    void OnGUI() {
        int index = SceneManager.GetActiveScene().buildIndex;
        switch (index) {
            case 0:
                if (sessionKey == string.Empty) {
                    if (!register)
                        Login_GUI();
                    else
                        Register_GUI();
                }
                break;

            case 3:
                if (gameComplete) {
                    string result = string.Empty;
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.fontSize = 32;
                    style.alignment = TextAnchor.MiddleCenter;
                    if (win) {
                        result = "WIN";
                        style.normal.textColor = Color.green;
                    }
                    else {
                        result = "LOOSE";
                        style.normal.textColor = Color.red;
                    }
                    GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2, 300, 100), "YOU "+result+"!!", style);
                    if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 100, 100, 30), "Exit")) {
                        Application.Quit();
                        /*if (IsServer)
                            NetServer.Instance.Stop("Match ended");
                        else
                            NetClient.Instance.Disconnect();*/
                    }
                }
                break;
        }
    }

    void Login_GUI() {
        #if UNITY_EDITOR
        mainServerHost = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 3 - 30, 100, 20), mainServerHost);
        userName = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 3, 100, 20), userName);
        password = GUI.PasswordField(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 30, 100, 20), password, '*');
        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 60, 100, 20), "Login") && publicIp != string.Empty) {
            Login();
        }
        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 90, 100, 20), "Register")) {
            register = true;
        }
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.red;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2 - 75, Screen.height / 3 + 120, 150, 20), errorMsg, style);
        #endif
    }

    void Register_GUI() {
        #if UNITY_EDITOR
        GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 3, 100, 20), "Username: ");
        userName = GUI.TextField(new Rect(Screen.width / 2 + 20, Screen.height / 3, 100, 20), userName);

        GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 30, 100, 20), "Password: ");
        password = GUI.PasswordField(new Rect(Screen.width / 2 + 20, Screen.height / 3 + 30, 100, 20), password, '*');

        GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 60, 100, 20), "Confirm: ");
        confirmPassword = GUI.PasswordField(new Rect(Screen.width / 2 + 20, Screen.height / 3 + 60, 100, 20), confirmPassword, '*');

        GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 90, 100, 20), "Email: ");
        email = GUI.TextField(new Rect(Screen.width / 2 + 20, Screen.height / 3 + 90, 100, 20), email);

        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 120, 100, 20), "Register")) {
            userName = userName.Trim();
            password = password.Trim();
            confirmPassword = confirmPassword.Trim();
            email = email.Trim();


            Regex r = new Regex("^[\x20-\x7F]*$");
            Regex email_reg = new Regex(@"^[^@\s]+@[^@\s]+(\.[^@\s]+)+$");
            bool valid = true;

            if (userName == string.Empty || !r.IsMatch(userName)) {
                Debug.Log("username is NOT valid!");
                valid = false;
            }

            if (password == string.Empty || !r.IsMatch(password)) {
                Debug.Log("password is NOT valid!");
                valid = false;
            }

            if (confirmPassword != password) {
                Debug.Log("confirm password must match password!");
                valid = false;
            }

            if (email == string.Empty || !email_reg.IsMatch(email)) {
                Debug.Log("email is NOT valid!");
                valid = false;
            }

            if (valid) {
                Register(userName, password, email);
            }
        }

        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 150, 100, 20), "Back")) {
            userName = "";
            password = "";
            confirmPassword = "";
            email = "";
            register = false;
        }
        #endif
    }

    void OnLevelWasLoaded(int level) {
        if (level == 2) {
            Debug.Log("game lobby scene open");
            if (IsServer) {
                IsServer = true;
                AddUser(0, userName);
                GameLobby.Instance.AddUser(GetUser(0), GameLobby.Team.TeamA);
                NetServer.Instance.Send(0, NetClient.OpCodes.ConnectComplete, new byte[] { 0, 0, Convert.ToByte(false) });
            }
            else {
                NetClient.Instance.Send(NetServer.OpCodes.GetLobby);
            }
        }
        if (level == 3) {
            Debug.Log("Game scene opened");
            Instantiate(ballPrefab, Vector3.zero + Vector3.up, Quaternion.identity);
            if (IsServer) {
                GameStarted = true;
                NetServer.Instance.Send(NetClient.OpCodes.GameStart);
                gameObject.AddComponent<GamePlay>();
                PlayerControl.Instance.AddTank(0);
            }
            else {
                gameObject.AddComponent<GamePlayClient>();
                NetClient.Instance.Send(NetServer.OpCodes.GameOpen);
            }
        }
    }

    public Player AddUser(byte userID, string name) {
        if (!UserExists(userID)) {
            Player user = new Player(userID, name);
            players.Add(userID, user);
            return user;
        }
        return null;
    }

    public void RemoveUser(byte userID) {
        if (UserExists(userID)) {
            players.Remove(userID);
        }
    }

    public bool UserExists(byte userID) {
        return players.ContainsKey(userID);
    }

    public Player GetUser(byte user) {
        if (UserExists(user))
            return players[user];
        return null;
    }

    public Player[] GetUsers() {
        return players.Values.ToArray();
    }

    public void Register(string user, string password, string email) {
        string salt = HashHelper.RandomKey(32);
        string clientHash = HashHelper.HashPasswordClient(password, salt);
        MainServerConnect.Instance.Register(user, clientHash, salt, email);
    }

    public void Connect(string game) {
        byte[] sendBytes = Encoding.UTF8.GetBytes(game);
        MainServerConnect.Instance.Send(4, sendBytes);
    }

    public void ProcessCommandArgs(string[] args) {
        for (int i = 0; i < args.Length; i++) {
            if (args[i].Contains("=")) {
                string[] cmd = args[i].Split('=');
                switch (cmd[0]) {
                    case "user":
                        userName = cmd[1];
                        break;
                    case "pass":
                        password = cmd[1];
                        break;
                    case "key":
                        sessionKey = cmd[1];
                        break;
                }
            }
        }
    }

    public void GetPublicIP() {
        StartCoroutine(GetPublicIP_Int());
    }

    private IEnumerator GetPublicIP_Int() {
        WWW ipGet = new WWW(@"http://icanhazip.com");
        if (ipGet == null) {
            Debug.Log("IP get failed.");
            yield return 0;
        }
        else {
            yield return ipGet;
            string data = ipGet.text;
            publicIp = data.Trim();
            Debug.Log(publicIp);
            if (sessionKey != string.Empty)
                MainServerConnect.Instance.Login(userName, password, publicIp, sessionKey);
        }
    }

    public void Login() {
        // Calls LoginSuccess after successfull login.
        #if UNITY_EDITOR
        MainServerConnect.Instance.host = mainServerHost;
        MainServerConnect.Instance.Login(userName, password, publicIp);
        #endif
    }

    public void LoginSuccess(string sessionKey) {
        Debug.Log("Successfull login.");
        this.sessionKey = sessionKey;
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void StartGame() {
        SceneManager.LoadScene(3);
    }

    public void RefreshLobby() {
        if (SceneManager.GetActiveScene().buildIndex == 1) {
            MainServerConnect.Instance.GetLobby();
        }

    }

    public void SetClientUsers(Player[] users) {
        players.Clear();
        for (int i = 0; i < users.Length; i++) {
            players.Add(users[i].ID, users[i]);
        }
    }

    public string GetLobbyStr() {
        StringBuilder strB = new StringBuilder();
        Player[] users = GetUsers();
        for (int i = 0; i < users.Length; i++) {
            strB.Append(users[i].ID + "★" + users[i].Name + "★" + users[i].Team.ToString() + "❤");
        }
        return strB.ToString();
    }

    // -------- Main Server commands --------

    [NetCommand(MainServerConnect.OpCodes.CompleteRegister)]
    public void CompleteRegister_CMD(byte[] data) {
        if (data[0] == 0) {
            Debug.Log("registration success.");
            NetServer.Instance.AddCommands(this);
            IsServer = true;
            SceneManager.LoadScene(2, LoadSceneMode.Single);
        }
        else {
            Debug.Log("registration failed.");
        }
    }

    [NetCommand(MainServerConnect.OpCodes.CompleteConnect)]
    public void CompleteUserConnect_CMD(byte[] data) {
        try {
            Debug.Log("Connect complete: " + SceneManager.GetActiveScene().buildIndex);
            byte id = data[0];
            byte[] input = BufferEdit.RemoveFirst(data);

            string[] parts = Encoding.UTF8.GetString(input).Split('★');
            string name = parts[0];
            Player user = AddUser(id, name);
            GameLobby.Instance.AddUser(user, (GameLobby.Team)int.Parse(parts[1]));
            NetServer.Instance.Send(id, NetClient.OpCodes.ConnectComplete, new byte[] { 0, id, Convert.ToByte(GameStarted)});
            string sendStr = GetLobbyStr();
            //Debug.Log(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(sendStr)));
            //NetServer.Instance.Send(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));

        }
        catch (Exception e) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType().ToString(), e.Message, e.StackTrace);
        }
    }

    [NetCommand(MainServerConnect.OpCodes.UserDisconnect)]
    public void UserDisconnect_CMD(byte[] data) {
        byte id = data[0];
        if (!UserExists(id))
            return;

        string removedUserName = GetUser(id).Name;
        NetServer.Instance.RemoveUser(id, "Disconnected", true);
        GameLobby.Instance.RemoveUser(id);

        if (GameStarted) {
            PlayerControl.Instance.RemoveTank(id);
        }
        else
            Debug.Log("Game not running no tanks removed.");

        string sendStr = "";
        Player[] users = GetUsers();
        for (int i = 0; i < users.Length; i++) {
            sendStr += users[i].ID + "★" + users[i].Name + "★"+ users[i].Team.ToString() + "❤";
        }
        //Debug.Log(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(sendStr)));
        NetServer.Instance.Send(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));

        Debug.Log("User disconnected: " + removedUserName);
    }

    [NetCommand(MainServerConnect.OpCodes.Close)]
    public void Close_CMD(byte[] data) {
        string reason = Encoding.UTF8.GetString(data);
        Debug.Log("Closing server: " + reason);
        MainServerConnect.Instance.Close();
    }

    // -------- Server commands --------

    [ServerCommand(NetServer.OpCodes.GetLobby)]
    public Traffic GetLobby_CMD(Player user, byte[] data) {
        string sendStr = GetLobbyStr();
        return new Traffic(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));
    }

    [ServerCommand(NetServer.OpCodes.GameOpen)]
    public Traffic GameOpen_CMD(Player user, byte[] data) {
        PlayerControl.Instance.AddTank(user.ID);
        GameBall.Instance.UpdateState(user.ID);
        return default(Traffic);
    }

    [ServerCommand(NetServer.OpCodes.Ping)]
    public Traffic Ping_CMD(Player user, byte[] data) {
        Debug.Log("Server: " + pingWatch.Elapsed.ToString());
        NetServer.Instance.Send(user.ID, NetClient.OpCodes.pingComplete);
        return default(Traffic);
    }

    // -------- Client commands --------

    [ClientCommand(NetClient.OpCodes.ConnectComplete)]
    public void CompleteConnect_CMD(byte[] data) {
        if (data[0] == 0) {
            netID = data[1];
            Debug.Log("Connected to server successfully! net ID: " + netID);
            
            if (!IsServer) {
                GameStarted = BitConverter.ToBoolean(data, 2);
                SceneManager.LoadScene(2);
            }
            //pingWatch = new System.Diagnostics.Stopwatch();
            //pingWatch.Start();
            //NetClient.Instance.Send(NetServer.OpCodes.Ping);
        }
        else
            Debug.Log("Failed to connect to server");
    }

    [ClientCommand(NetClient.OpCodes.UpdateLobbyUsers)]
    public void UpdateLobbyUsers_CMD(byte[] data) {
        try {
            Debug.Log("Updating game lobby.");
            if (GameLobby.Instance == null) {
                Debug.LogWarning("Game Lobby null! " + SceneManager.GetActiveScene().buildIndex);
                return;
            }
            

            string input = Encoding.UTF8.GetString(data);
            string[] usersStr = input.Split(new char[] { '❤' }, StringSplitOptions.RemoveEmptyEntries);
            List<Player> lobbyUsers = new List<Player>();
            for (int i = 0; i < usersStr.Length; i++) {
                string[] parts = usersStr[i].Split('★');
                if (parts.Length == 3) {
                    string id = parts[0];
                    string name = parts[1];
                    string team = parts[2];
                    Player user = new Player(byte.Parse(id), name);
                    user.Team = (GameLobby.Team)Enum.Parse(typeof(GameLobby.Team), team, true);
                    lobbyUsers.Add(user);
                }
                else
                    Debug.LogError("Invalid lobby format!");

            }
            SetClientUsers(lobbyUsers.ToArray());
        }
        catch(Exception e) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
        }
    }

    [ClientCommand(NetClient.OpCodes.GameStart)]
    public void StartGame(byte[] data) {
        if (!IsServer) {
            SceneManager.LoadScene(3);
        }
    }

    [ClientCommand(NetClient.OpCodes.pingComplete)]
    public void PingComplete_CMD(byte[] data) {
        Debug.Log("Client: " + pingWatch.Elapsed.ToString());
        pingWatch.Stop();
    }

    [ClientCommand(NetClient.OpCodes.SetScore)]
    public void SetScore_CMD(byte[] data) {
        if (!IsServer) {
            GamePlayClient.Instance.SetScore((int)data[0], (int)data[1]);
        }
    }

    [ClientCommand(NetClient.OpCodes.End)]
    public void End_CMD(byte[] data) {
        GameObject camObj = GameObject.Find("Camera");
        if (camObj != null) {
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, -100, 0);
            MouseOrbitImproved.Instance.SetTarget(null);
            MouseOrbitImproved.Instance.SetInputEnable(false);
        }
        GameLobby.Team team = GetUser(netID).Team;
        GetUser(netID).Instance.SetEnabled(false);
        gameComplete = true;
        win = ((byte)team == data[0]);
    }

    [ClientCommand(NetClient.OpCodes.CountDownStart)]
    public void CountDownStart(byte[] data) {
        GamePlayClient.Instance.StartGameCountDown(BitConverter.ToSingle(data, 0));
    }

    [ClientCommand(NetClient.OpCodes.Close)]
    public void Close(byte[] data) {
        string reason = Encoding.UTF8.GetString(data);
        NetClient.Instance.Close();
        Debug.Log("Connection closed: " + reason);
    }

    
}
