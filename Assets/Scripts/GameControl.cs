using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

public class GameControl : MonoBehaviour {
    public enum GuiIndex {
        Login,
        Lobby,
        GameLobby,
        Game,
        Closed,
    }

    public static GameControl Instance;

    public GameObject ballPrefab;
    public Camera mainCam;

    public GuiIndex gui;
    public string mainServerHost;
    public string userName;
    public string password;
    public string sessionKey;
    public byte netID;
    public System.Diagnostics.Stopwatch pingWatch;
    public string errorMsg;
    public bool StartUdp = true;
    public bool Paused = false;

    public bool IsServer { get { return MainServerConnect.Instance.IsServer; } }
    public int PlayerCount { get { return players.Count; } }

    public Dictionary<byte, Player> players { get; private set; }

    public bool GameStarted;

    private bool register = false;
    private bool invalid;

    private string confirmPassword = "";
    private string email = "";
    private bool gameComplete = false;
    private bool win = false;
    private GameObject gameBallObj;

    private string closedReason = "";
    private bool _run = true;

    private int udpCallsCount = 0;
    public int udpCalls = 0;
    private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();


    void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        string[] commandArgs = Environment.GetCommandLineArgs();
        ProcessCommandArgs(commandArgs);
    }

    // Use this for initialization
    void Start() {
        Application.targetFrameRate = 60;
        gui = GuiIndex.Login;
        players = new Dictionary<byte, Player>();
        MainServerConnect.Instance.AddCommands(this);
        NetClient.Instance.AddCommands(this);
        mainServerHost = MainServerConnect.Instance.host;
    }

    void OnEnable() {
        SceneManager.sceneLoaded += LevelLoaded;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= LevelLoaded;
    }

    // Update is called once per frame
    void Update() {
        if (gui == GuiIndex.Game) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (!Paused)
                    Pause();
                else
                    Unpause();
            }

            if (MouseOrbitImproved.Instance.inputEnabled && GameStarted) {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                    SetMouseLocked(true);
            }
        }
    }

    void OnGUI() {
        switch (gui) {
            case GuiIndex.Login:
                return;
                if (sessionKey == string.Empty) {
                    if (register)
                        Register_GUI();
                    else
                        Login_GUI();
                }
                break;

            case GuiIndex.Lobby:
                return;
                LobbyScript.Instance.GUIUpdate();
                break;

            case GuiIndex.GameLobby:
                GameLobby.Instance.GUIUpdate();
                break;

            case GuiIndex.Game:
                if (Paused) {
                    /*float screenWidth = Screen.width;
                    float screenHeight = Screen.height;
                    float boxWidth = screenWidth * (9/10f);
                    float boxHeight = screenHeight * (9 / 10f);
                    GUI.Box(new Rect(screenWidth / 2 - (boxWidth / 2), screenHeight / 2 - (boxHeight / 2), boxWidth, boxHeight), "");*/
                }

                GamePlayClient.Instance.GUIUpdate();

                /*if (gameComplete) {
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
                            NetClient.Instance.Disconnect();
                    }
                }*/
                break;

            case GuiIndex.Closed:
                GUIContent content = new GUIContent("Disconnected: " + closedReason);
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                Vector2 size = style.CalcSize(content);
                GUI.Label(new Rect(Screen.width / 2 - size.x / 2, Screen.height / 2 - size.y / 2, size.x, size.y), content);
                break;
        }
    }

    void Login_GUI() {
        //#if UNITY_EDITOR
        mainServerHost = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 3 - 30, 100, 20), mainServerHost);
        userName = GUI.TextField(new Rect(Screen.width / 2 - 50, Screen.height / 3, 100, 20), userName);
        password = GUI.PasswordField(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 30, 100, 20), password, '*');
        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 60, 100, 20), "Login")) {
            Login();
        }
        if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 3 + 90, 100, 20), "Register")) {
            register = true;
        }
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.red;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(Screen.width / 2 - 75, Screen.height / 3 + 120, 150, 20), errorMsg, style);
        //#endif
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

    void OnApplicationQuit() {
        _run = false;
    }

    ushort _count = 0;
    void LevelLoaded(Scene scene, LoadSceneMode mode) {
        int level = scene.buildIndex;
        if (level == 1) {
            MouseOrbitImproved.Instance.SetInputEnable(false);
            SetMouseLocked(false);
            gui = GuiIndex.Lobby;
            UIControl.Instance.EnableLobby();
            mainCam = FindObjectOfType<Camera>();
            if (mainCam == null)
                Debug.Log("null main camera!");

            TaskQueue.QueueAsync("Test", () => {
                System.Threading.ManualResetEvent reset = new System.Threading.ManualResetEvent(false);
                while(_run) {
                    byte[] countBuff = BitConverter.GetBytes(_count++);
                    MainServerConnect.Instance.Send(ServerCMD.test, BufferEdit.Add(countBuff, new byte[2048]));
                    //reset.WaitOne(1);
                }
            });
        }
        /*if (level == 2) {
            Debug.Log("game lobby scene open");
            if (IsServer) {
                AddUser(0, userName);
                GameLobby.Instance.AddUser(GetUser(0), GameLobby.Team.TeamA);
                NetServer.Instance.Send(0, NetClient.OpCodes.ConnectComplete, new byte[] { 0, 0 });
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
        }*/
    }

    public Player AddUser(byte userID, string name) {
        if (!UserExists(userID)) {
            Player user = new Player(userID, name);
            players.Add(userID, user);
            return user;
        }
        Debug.LogErrorFormat("Failed to add player \"{0}\"({1}): Already Exists!", name, userID);
        return null;
    }

    public void RemoveUser(byte userID) {
        if (UserExists(userID)) {
            players.Remove(userID);
        }
    }

    public void ClearUsers() {
        players.Clear();
    }

    public bool UserExists(byte userID) {
        return players.ContainsKey(userID);
    }

    public Player GetUser() {
        if (UserExists(netID))
            return players[netID];
        return null;
    }

    public Player GetUser(byte user) {
        if (UserExists(user))
            return players[user];
        return null;
    }

    public Player[] GetUsers() {
        return players.Values.ToArray();
    }

    public void DestroyUser(byte user) {
        if (UserExists(user)) {
            if (players[user].TankExists)
                players[user].DestroyTank();
            GameLobby.Instance.RemoveUser(user);
            GamePlayClient.Instance.RemovePlayer(players[user]);
            RemoveUser(user);
        }
    }
    
    public void Register(string user, string password, string email) {
        string salt = HashHelper.RandomKey(32);
        string clientHash = HashHelper.HashPasswordClient(password, salt);
        //MainServerConnect.Instance.Register(user, clientHash, salt, email);
    }

    public void Connect(ushort game) {
        byte[] sendBytes = BitConverter.GetBytes((Int16)game);
        MainServerConnect.Instance.Send(ServerCMD.JoinMatch, sendBytes);
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

    public void Login() {
        // Calls LoginSuccess after successfull login.
        //#if UNITY_EDITOR
        MainServerConnect.Instance.host = mainServerHost;
        MainServerConnect.Instance.Login(userName, password);
        //#endif
    }

    public void LoginSuccess(string sessionKey) {
        Debug.Log("Successfull login.");
        this.sessionKey = sessionKey;
        SceneManager.LoadScene(1, LoadSceneMode.Single);

        return;
        watch.Start();
        TaskQueue.QueueAsync("test", () => {
            while(_run) {
                MainServerConnect.Instance.Send(ServerCMD.test, Protocal.Udp);
                udpCallsCount++;
                if (watch.ElapsedMilliseconds >= 1000) {
                    udpCalls = udpCallsCount;
                    udpCallsCount = 0;
                    watch.Reset();
                    watch.Start();
                }
            }
        });
    }

    // OLD
    /*public void StartServer() {
        gameBallObj = (GameObject)Instantiate(ballPrefab, Vector3.zero + Vector3.up, Quaternion.identity);
        GameStarted = true;
        gameObject.AddComponent<GamePlay>();
        PlayerControl.Instance.SetServer();
        PlayerControl.Instance.AddTank(0);
        EnablePlayCamera();
        gui = GuiIndex.Game;
        NetServer.Instance.Send(NetClient.OpCodes.GameStart);
        MainServerConnect.Instance.SendServer(new Traffic(MainServerConnect.ServerOpCodes.GameStart, new byte[1]));
    }*/

    // Debug command.
    public void StartMatch() {
        MainServerConnect.Instance.Send(ServerCMD.StartMatch);
    }

    public void EndGame() {
        if (GameStarted) {
            Destroy(gameBallObj);
            //if (IsServer)
            //    Destroy(GamePlay.Instance);
            //else
            Destroy(GamePlayClient.Instance);
            GameBall.Instance = null;
            GamePlayClient.Instance = null;
            GamePlay.Instance = null;
        }
        //MainServerConnect.Instance.IsServer = false;
        GameStarted = false;
        GameLobby.Instance.ClearTeams();
        PlayerControl.Instance.Close();
        DisablePlayerCamera();
        LobbyScript.Instance.ClearLobby();
        ClearUsers();
        UIControl.Instance.EnableLobby();
        gui = GuiIndex.Lobby;
        LobbyScript.Instance.Refresh();
    }

    public void EnablePlayCamera() {
        mainCam.clearFlags = CameraClearFlags.Skybox;
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("Default");
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("TransparentFX");
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("Ignore Raycast");
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("Water");
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("Terrain");
        mainCam.cullingMask |= 1 << LayerMask.NameToLayer("player");
        mainCam.GetComponent<MouseOrbitImproved>().enabled = true;
    }

    public void DisablePlayerCamera() {
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Default"));
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("TransparentFX"));
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Water"));
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Terrain"));
        mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("player"));
        mainCam.GetComponent<MouseOrbitImproved>().enabled = false;
    }

    public void SetClientUsers(Player[] users) {
        for (int i = 0; i < users.Length; i++) {
            if (!UserExists(users[i].ID)) {
                players.Add(users[i].ID, users[i]);
                GamePlayClient.Instance.AddPlayer(users[i]);
            }
        }
    }

    // OLD
    /*public string GetLobbyStr() {
        StringBuilder strB = new StringBuilder();
        Player[] users = GetUsers();
        for (int i = 0; i < users.Length; i++) {
            strB.Append(users[i].ID + "★" + users[i].Name + "★" + users[i].Team.ToString() + "❤");
        }
        return strB.ToString();
    }*/

    // OLD
    /*public void CompleteRegister(bool result) {
        if (result) {
            Debug.Log("registration success.");
            gui = GuiIndex.GameLobby;
            NetServer.Instance.AddCommands(this);
            NetServer.Instance.AddCommands(PlayerControl.Instance);
            AddUser(0, userName);
            GameLobby.Instance.AddUser(GetUser(0), GameLobby.Team.TeamA);
            NetServer.Instance.Send(0, NetClient.OpCodes.ConnectComplete, new byte[] { 0, 0});
        }
        else {
            Debug.LogWarning("registration failed.");
        }
    }*/

    public void Pause() {
        SetMouseLocked(false);
        Paused = true;
        MouseOrbitImproved.Instance.SetInputEnable(false);

    }

    public void Unpause() {
        SetMouseLocked(true);
        Paused = false;
        MouseOrbitImproved.Instance.SetInputEnable(true);
    }

    public void SetMouseLocked(bool locked) {
        if (locked) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ClosedScreen(string message) {
        Debug.Log("Opening close screen.");
        closedReason = message;
        gui = GuiIndex.Closed;
        UIControl.Instance.EnableDisconnected();
        SceneManager.LoadScene(2, LoadSceneMode.Single);
    }

    // -------- Main Server commands --------

    [Command(ClientCMD.DoLogin)]
    public void DoLogin_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer);
        if (ent.Count == 2) {
            MainServerConnect.Instance.salt = (string)ent.GetEntryValue(1);
            string hashedPass = HashHelper.HashPasswordClient(password, MainServerConnect.Instance.salt);
            string sendStr = hashedPass; // TODO: mac address
            MainServerConnect.Instance.Send(ServerCMD.Login, sendStr); // send login request
        }
        else if (ent.Count == 1) {
            byte error = (byte)ent.GetEntryValue(0);
            if (error == 2) {
                errorMsg = MainServerConnect.Instance.username + " already connected.";
                SafeDebug.LogWarning(MainServerConnect.Instance.username + " already connected.");
                MainServerConnect.Instance.Close(false);
            }
            else if (error == 3) {
                errorMsg = "No user named " + MainServerConnect.Instance.username;
                SafeDebug.LogWarning("no user named " + MainServerConnect.Instance.username);
                MainServerConnect.Instance.Close(false);
            }
        }
        else {
            MainServerConnect.Instance.Close(false);
        }
    }

    [Command(ClientCMD.LoginResult)]
    public void LoginResult_Cmd(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer);
        if (ent.Count == 1) {
            string input = (string)ent.GetEntryValue(0);
            switch (input) {
                case "1":
                    errorMsg = "Incorrect password";
                    SafeDebug.LogWarning("Incorrect password");
                    break;
                case "2":
                    errorMsg = "Inactive account";
                    SafeDebug.LogWarning("Inactive account");
                    break;
                case "3":
                    errorMsg = "Banned account";
                    SafeDebug.LogWarning("Banned account");
                    break;
                default:
                    errorMsg = "error: " + input;
                    SafeDebug.LogWarning("error: " + input);
                    break;
            }
            MainServerConnect.Instance.Close(false);
            return;
        }
        else if (ent.Count == 2) {
            sessionKey = (string)ent.GetEntryValue(0);
            MainServerConnect.Instance.UdpID = (int)ent.GetEntryValue(1);
            MainServerConnect.Instance.StartTcpPing();
            MainServerConnect.Instance.StartUdp();
            LoginSuccess(sessionKey);
        }
        else
            MainServerConnect.Instance.Close(false);
    }

    [Command(ClientCMD.KeyLoginResult)]
    public void KeyLoginResult_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer);
        if (ent.Count == 2) {
            MainServerConnect.Instance.UdpID = (ushort)ent.GetEntryValue(1);
            MainServerConnect.Instance.StartTcpPing();
            MainServerConnect.Instance.StartUdp();
            LoginSuccess(sessionKey);
        }
        else {
            errorMsg = "Session key not valid";
            SafeDebug.Log("Session key not valid");
        }
    }

    [Command(ClientCMD.SetMatches)]
    public void SetMatches_CMD(Data data) {
        DataEntries ent1 = DataDecoder.Decode(data.Buffer, DataTypePresets.SetMatches);
        ushort count = (ushort)ent1.GetEntryValue(0);
        byte[] matchBuff = (byte[])ent1.GetEntryValue(1);
        DataTypes[] metaData = new DataTypes[count * 2];
        for (int i = 0; i < count * 2; i += 2) {
            metaData[i] = DataTypes.Ushort;
            metaData[i + 1] = DataTypes.String;
        }
        DataEntries ent2 = DataDecoder.Decode(matchBuff, metaData);
        List<LobbyScript.LobbyEntry> entries = new List<LobbyScript.LobbyEntry>();
        for (int i = 0; i < count * 2; i += 2) {
            ushort id = (ushort)ent2.GetEntryValue(i);
            string desc = (string)ent2.GetEntryValue(i + 1);
            entries.Add(new LobbyScript.LobbyEntry(id, desc));
        }
        Debug.Log("Lobby get: " + entries.Count + " matches.");
        LobbyScript.Instance.SetLobby(entries.ToArray());

        /*string input = data.Input;
        List<LobbyScript.LobbyEntry> entries = new List<LobbyScript.LobbyEntry>();
        string[] entriesStr = input.Split('❤');
        for (int i = 0; i < entriesStr.Length; i++) {
            string[] parts = entriesStr[i].Split('★');
            if (parts.Length == 2) {
                entries.Add(new LobbyScript.LobbyEntry(ushort.Parse(parts[0]), parts[1]));
            }
        }
        SafeDebug.Log("Lobby get: " + entries.Count + " matches.");
        LobbyScript.Instance.SetLobby(entries.ToArray());*/
    }

    [Command(ClientCMD.UdpEnabled)]
    public void UdpEnabled_CMD(Data data) {
        if (data.Type == Protocal.Udp)
            Debug.Log("UDP enabled!");
        else
            Debug.Log("Received UDP enabled command on tcp.");
    }

    [Command(ClientCMD.Ping)]
    public void Ping_CMD(Data data) {
        bool pingBack = BitConverter.ToBoolean(data.Buffer, 0);
        if (pingBack) {
            MainServerConnect.Instance.Send(ServerCMD.Ping, BitConverter.GetBytes(false));
        }
    }

    [Command(ClientCMD.EndGame)]
    public void EndGame_CMD(Data data) {
        EndGame();
    }

    [Command(ClientCMD.CompleteRegister)]
    public void CompleteRegister_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer, DataTypePresets.CompleteRegister);
        bool success = (bool)ent.GetEntryValue(0);
        ushort matchID = (ushort)ent.GetEntryValue(1);
        if (success) {
            Debug.LogFormat("Match created - joining match {0}", matchID);
            DataEntries ent2 = new DataEntries();
            ent2.AddEntry(matchID);
            MainServerConnect.Instance.Send(ServerCMD.JoinMatch, ent2.Encode(false));
        }
        else {
            Debug.LogWarning("Server registration failed");
        }
    }

    [Command(ClientCMD.JoinMatchComplete)]
    public void JoinMatchComplete_CMD(Data data) {
        DataEntries ent = DataDecoder.Decode(data.Buffer, DataTypePresets.JoinMatchComplete);
        byte result = (byte)ent.GetEntryValue(0);
        byte id = (byte)ent.GetEntryValue(1);
        byte bt = (byte)ent.GetEntryValue(2);
        if (result == 1) {
            netID = id;
            Player player = AddUser(netID, userName);
            if (player != null) {
                player.Team = (GameLobby.Team)bt;
                gui = GuiIndex.GameLobby;
                UIControl.Instance.EnableGameLobby();
                Debug.Log("joined match successfully! net ID: " + netID);
            }
            else {
                Debug.LogError("CompleteConnect_CMD: player null.");
            }
        }
        else {
            Debug.LogError("CompleteConnect_CMD: player null.");
        }

        /*if (data.Buffer.Length == 3) {
            if (data.Buffer[0] == 0) {
                netID = data.Buffer[1];
                gui = GuiIndex.GameLobby;
                Player player = AddUser(netID, userName);
                if (player != null) {
                    player.Team = (GameLobby.Team)data.Buffer[2];
                    Debug.Log("joined match successfully! net ID: " + netID);
                }
                else {
                    Debug.LogError("JoinMatchComplete_CMD: player null.");
                }
            }
            else
                Debug.LogError("CompleteConnect_CMD: Failed to join match.");
        }
        else
            Debug.LogError("CompleteConnect_CMD: Failed to join match: invalid CompleteConnect data format.");*/

    }

    [Command(ClientCMD.UpdateLobbyUsers)]
    public void UpdateLobbyUsers_CMD(Data data) {
        try {
            DataEntries ent1 = DataDecoder.Decode(data.Buffer, DataTypePresets.UpdateLobbyUsers);
            byte count = (byte)ent1.GetEntryValue(0);
            DataTypes[] metaData = new DataTypes[count * 3];
            for (int i = 0; i < count * 3; i += 3) {
                metaData[i] = DataTypes.Byte;
                metaData[i + 1] = DataTypes.String;
                metaData[i + 2] = DataTypes.Byte;
            }
            byte[] lobbyBuff = (byte[])ent1.GetEntryValue(1);
            DataEntries ent2 = DataDecoder.Decode(lobbyBuff, metaData);
            for (int i = 0; i < count * 3; i += 3) {
                byte id = (byte)ent2.GetEntryValue(i);
                string name = (string)ent2.GetEntryValue(i + 1);
                byte team = (byte)ent2.GetEntryValue(i + 2);
                if (!UserExists(id)) {
                    Player player = AddUser(id, name);
                    if (player != null) {
                        player.Team = (GameLobby.Team)team;
                    }
                }
            }

        }
        catch (Exception e) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
        }


        /*try {
            Debug.Log("Updating game lobby.");
            if (GameLobby.Instance == null) {
                Debug.LogWarning("Game Lobby null! ");
                return;
            }


            string input = data.Input;
            string[] usersStr = input.Split(new char[] { '❤' }, StringSplitOptions.RemoveEmptyEntries);
            List<Player> lobbyUsers = new List<Player>();
            for (int i = 0; i < usersStr.Length; i++) {
                string[] parts = usersStr[i].Split('★');
                if (parts.Length == 3) {
                    byte id = byte.Parse(parts[0]);
                    string name = parts[1];
                    int team = int.Parse(parts[2]);
                    if (!UserExists(id)) {
                        Player player = AddUser(id, name);
                        if (player != null) {
                            player.Team = (GameLobby.Team)team;
                        }
                    }
                    //Player user = new Player(byte.Parse(id), name);
                    //user.Team = (GameLobby.Team)team;
                    //lobbyUsers.Add(user);
                }
                else
                    Debug.LogError("Invalid lobby format!");

            }
            SetClientUsers(lobbyUsers.ToArray());
            Debug.LogFormat("Lobby users set");
        }
        catch (Exception e) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
        }*/
    }

    [Command(ClientCMD.StartMatch)]
    public void StartMatch_CMD(Data data) {
        GameStarted = true;
        SetMouseLocked(true);
        Instantiate(ballPrefab, Vector3.zero + Vector3.up, Quaternion.identity);
        EnablePlayCamera();
        UIControl.Instance.EnableGame();
        gui = GuiIndex.Game;
        MainServerConnect.Instance.Send(ServerCMD.ClientGameOpen);
    }

    // ----------------- OLD ----------------- 

    // OLD
    /*[NetCommand(MainServerConnect.OpCodes.CompleteRegister)]
    public void CompleteRegister_CMD(byte[] data) {
        bool success = BitConverter.ToBoolean(data, 0);
        if (success) {
            MainServerConnect.Instance.IsServer = true;
            CompleteRegister(success);
        }
        else {
            SafeDebug.LogWarning("Server registration failed");
        }
    }*/

    // OLD
    /*[NetCommand(MainServerConnect.OpCodes.CompleteConnect)]
    public void CompleteUserConnect_CMD(byte[] data) {
        try {
            Debug.Log("Connect complete: " + SceneManager.GetActiveScene().buildIndex);
            byte id = data[0];
            byte[] input = BufferEdit.RemoveCmd(data);

            string[] parts = Encoding.UTF8.GetString(input).Split('★');
            string name = parts[0];
            Player user = AddUser(id, name);
            Debug.LogFormat("Player joined: {0}({1})", name, id);
            GameLobby.Instance.AddUser(user, (GameLobby.Team)int.Parse(parts[1]));
            NetServer.Instance.Send(id, NetClient.OpCodes.ConnectComplete, new byte[] { 0, id, Convert.ToByte(GameStarted)});
            //string sendStr = GetLobbyStr();
            //Debug.Log(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(sendStr)));
            //NetServer.Instance.Send(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));

        }
        catch (Exception e) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType().ToString(), e.Message, e.StackTrace);
        }
    }*/

    // OLD
    /*[NetCommand(MainServerConnect.OpCodes.UserDisconnect)]
    public void UserDisconnect_CMD(byte[] data) {
        byte id = data[0];
        if (!UserExists(id))
            return;

        string removedUserName = GetUser(id).Name;
        //NetServer.Instance.RemoveUser(id, "Disconnected", true);
        GameLobby.Instance.RemoveUser(id);

        if (GameStarted) {
            PlayerControl.Instance.RemoveTank(id);
        }
        else
            Debug.Log("Game not running no tanks removed.");

        RemoveUser(id);

        string sendStr = "";
        Player[] users = GetUsers();
        for (int i = 0; i < users.Length; i++) {
            sendStr += users[i].ID + "★" + users[i].Name + "★"+ users[i].Team.ToString() + "❤";
        }
        //Debug.Log(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(sendStr)));
        NetServer.Instance.Send(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));

        Debug.Log("User disconnected: " + removedUserName);
    }*/

    // OLD
    /*[NetCommand(MainServerConnect.OpCodes.Ping)]
    public void Ping_CMD(byte[] data) {
        //Debug.Log("Ping back");
    }*/

    // OLD
    /*[NetCommand(MainServerConnect.OpCodes.EndMatch)]
    public void EndMatch_CMD(byte[] data) {
        NetServer.Instance.ClearCommands();
        EndGame();
    }*/

    // -------- Server commands --------

    // OLD
    /*[ServerCommand(NetServer.OpCodes.GetLobby)]
    public Traffic GetLobby_CMD(Player user, byte[] data) {
        string sendStr = GetLobbyStr();
        return new Traffic(NetClient.OpCodes.UpdateLobbyUsers, Encoding.UTF8.GetBytes(sendStr));
    }*/

    // OLD
    /*[ServerCommand(NetServer.OpCodes.GameOpen)]
    public Traffic GameOpen_CMD(Player user, byte[] data) {
        PlayerControl.Instance.AddTank(user.ID);
        GameBall.Instance.UpdateState(user.ID);
        if (PlayerControl.Instance.TankInstanceCount >= players.Count) {
            PlayerControl.Instance.SetClientTanks();
        }
        return default(Traffic);
    }*/

    // OLD
    /*[ServerCommand(NetServer.OpCodes.Ping)]
    public Traffic Ping_CMD(Player user, byte[] data) {
        Debug.Log("Server: " + pingWatch.Elapsed.ToString());
        NetServer.Instance.Send(user.ID, NetClient.OpCodes.pingComplete);
        return default(Traffic);
    }*/

    // -------- Client commands --------

    // OLD
    /*[ClientCommand(NetClient.OpCodes.ConnectComplete)]
    public void CompleteConnect_CMD(byte[] data) {
        if (data[0] == 0) {
            netID = data[1];
            Debug.Log("Connected to server successfully! net ID: " + netID);
            
            if (!IsServer) {
                gui = GuiIndex.GameLobby;
                NetClient.Instance.Send(NetServer.OpCodes.GetLobby);
            }
            //pingWatch = new System.Diagnostics.Stopwatch();
            //pingWatch.Start();
            //NetClient.Instance.Send(NetServer.OpCodes.Ping);
        }
        else
            Debug.Log("Failed to connect to server");
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.UpdateLobbyUsers)]
    public void UpdateLobbyUsers_CMD(byte[] data) {
        try {
            Debug.Log("Updating game lobby.");
            if (GameLobby.Instance == null) {
                Debug.LogWarning("Game Lobby null! ");
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
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.GameStart)]
    public void StartGame(byte[] data) {
        if (!IsServer) {
            //SceneManager.LoadScene(3);
            GameStarted = true;
            Instantiate(ballPrefab, Vector3.zero + Vector3.up, Quaternion.identity);
            gameObject.AddComponent<GamePlayClient>();
            EnablePlayCamera();
            //NetClient.Instance.Send(NetServer.OpCodes.GameOpen);
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.pingComplete)]
    public void PingComplete_CMD(byte[] data) {
        Debug.Log("Client: " + pingWatch.Elapsed.ToString());
        pingWatch.Stop();
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.SetScore)]
    public void SetScore_CMD(byte[] data) {
        if (!IsServer) {
            GamePlayClient.Instance.SetScore((int)data[0], (int)data[1]);
        }
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.End)]
    public void End_CMD(byte[] data) {
        /*GameObject camObj = GameObject.Find("Camera");
        if (camObj != null) {
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = true;
            cam.transform.position = new Vector3(0, -100, 0);
        }
        MouseOrbitImproved.Instance.SetTarget(null);
        MouseOrbitImproved.Instance.SetInputEnable(false);
        GameLobby.Team team = GetUser(netID).Team;
        GetUser(netID).Instance.SetEnabled(false);
        gameComplete = true;
        win = ((byte)team == data[0]);
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.CountDownStart)]
    public void CountDownStart(byte[] data) {
        GamePlayClient.Instance.StartGameCountDown(BitConverter.ToSingle(data, 0));
    }*/

    // OLD
    /*[ClientCommand(NetClient.OpCodes.Close)]
    public void Close(byte[] data) {
        string reason = Encoding.UTF8.GetString(data);
        NetClient.Instance.Close();
        Debug.Log("Connection closed: " + reason);
    }
    */
    
}
