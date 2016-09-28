using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
#if !UNITY_WEBGL
using WebSocketSharp;
#endif

public class MainServerConnect : MonoBehaviour {
    public delegate void CMD(byte[] data);

    public enum ServerOpCodes : byte {
        None = 0,
        Register = 1,
        Ping = 2,
        Route = 3,
        Close = 4,
    }

    public enum OpCodes : byte {
        None,
        CompleteRegister,
        CompleteConnect,
        routeBack,
        Process,
        UserDisconnect,
        Close,
    }

    public static MainServerConnect Instance;

    public string host = "localhost";
    public int port = 11010;
    public float pingTime = 60;
    public bool IsServer { get; private set; }

    #if !UNITY_WEBGL
    private WebSocket socket;
    #endif
    private string username;
    private string password;
    private string sessionKey;
    private string IP;
    private string salt;

    public Dictionary<byte, CMD> Commands { get; private set; }

    void Awake() {
        Instance = this;
        Commands = new Dictionary<byte, CMD>();
        AddCommands(this);
    }

    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {

    }

    void OnApplicationQuit() {
        Close();
    }

    public void AddCommands(object target) {
        MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++) {
            try {
                NetCommand cmdAttribute = (NetCommand)Attribute.GetCustomAttribute(methods[i], typeof(NetCommand));
                if (cmdAttribute != null) {
                    CMD function = null;
                    if (methods[i].IsStatic)
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), methods[i], true);
                    else
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), target, methods[i], true);
                    if (function != null) {
                        if (CommandExists(cmdAttribute.command))
                            Commands[cmdAttribute.command] = function;
                        else
                            Commands.Add(cmdAttribute.command, function);
                    }
                    else {
                        Debug.LogError("Failed to add main server network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
                    }
                }
            }
            catch (Exception e) {
                if (methods[i] != null) {
                    Debug.LogErrorFormat("Error adding main server network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
                    Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
                }
            }
        }
    }

    public void AddCommand(byte cmd, CMD callback) {
        if (!CommandExists(cmd)) {
            Commands.Add(cmd, callback);
        }
    }

    public void RemoveCommand(byte cmd) {
        if (CommandExists(cmd))
            Commands.Remove(cmd);
    }

    public bool CommandExists(byte Cmd) {
        return Commands.ContainsKey(Cmd);
    }

    
    public void Login(string username, string password, string ip) {
#if UNITY_EDITOR
        TaskQueue.QueueAsync("Net", () => {
            this.username = username;
            this.password = password;
            this.IP = ip;
            socket = new WebSocket(string.Format("ws://{0}:{1}/login", host, port));
            socket.OnMessage += Login_OnMessage;
            socket.OnError += Socket_OnError;
            socket.Connect();
            //socket.Send("l0❤" + username); // send salt request.
            socket.Send(InsertCommand(0, Encoding.UTF8.GetBytes(username)));
            IsServer = false;
        });
#endif
    }


    public void Login(string username, string password, string ip, string sessionKey) {
        TaskQueue.QueueAsync("Net", () => {
            this.username = username;
            this.password = password;
            this.sessionKey = sessionKey;
            this.IP = ip;
            socket = new WebSocket(string.Format("ws://{0}:{1}/login", host, port));
            socket.OnMessage += Login_OnMessage;
            socket.OnError += Socket_OnError;
            socket.Connect();
            //socket.Send("p❤s★" + sessionKey + "★" + IP); // varify session key
            Send(InsertCommand(3, "2★" + sessionKey + "★" + IP));
            IsServer = false;
        });
    }

    public void Register(string username, string passwordHash, string salt, string email) {
        TaskQueue.QueueAsync("Net", () => {
            socket = new WebSocket(string.Format("ws://{0}:{1}/register", host, port));
            socket.OnMessage += Register_OnMessage;
            socket.Connect();
            string sendStr = username + "★" + passwordHash + "★" + salt + "★" + email;
            socket.Send(Encoding.UTF8.GetBytes(sendStr));
        });
    }

    public void Send(byte command, string data) {
        Send(InsertCommand(command, data));
    }

    public void Send(byte command, byte[] data) {
        Send(InsertCommand(command, data));
    }

    public void Send(byte[] data) {
        TaskQueue.QueueAsync("Net", () => {
            if (socket != null) {
                socket.Send(data);
            }
        });
    }

    public void Ping() {
        if (socket != null) {
            if (IsServer)
                SendServer(new Traffic(ServerOpCodes.Ping, Encoding.UTF8.GetBytes(sessionKey)));
            else {
                TaskQueue.QueueAsync("Net", () => {
                    Send(3, "3★" + GameControl.Instance.sessionKey);
                });
            }
        }
    }

    public void GetLobby() {
        if (socket != null) {
            socket.Send(InsertCommand(2, sessionKey));
        }
    }

    public void RegisterServer(string ip, string description = "") {
        TaskQueue.QueueAsync("Net", () => {
            Close();
            socket = new WebSocket(string.Format("ws://{0}:{1}/server", host, port));
            socket.OnMessage += Server_OnMessage;
            socket.OnError += Socket_OnError;
            socket.OnClose += Server_OnClose;
            socket.Connect();
            if (description == string.Empty)
                description = username + "'s football match.";
            SendServer(new Traffic(ServerOpCodes.Register, Encoding.UTF8.GetBytes(sessionKey + "★" + description)));
            IsServer = true;
        });
    }

    public void SendServer(Traffic data) {
        TaskQueue.QueueAsync("Net", () => {
            if (socket != null) {
                //socket.Send(Encoding.UTF8.GetBytes(data.command + "❤" + data.data + "❤" + data.sessionKey));
                socket.Send(BufferEdit.AddFirst(data.byteCommand, data.byteData));
            }
        });
    }

    public void Close() {
        if (socket != null) {
            socket.Close();
            socket = null;
        }
    }

    #if !UNITY_WEBGL
    private void Socket_OnError(object sender, ErrorEventArgs e) {
        SafeDebug.LogError("[NET]: " + e.Exception.GetType().ToString() + ": " + e.Message + "\n" + e.Exception.StackTrace);
    }

    private void Login_OnMessage(object sender, MessageEventArgs e) {
        //SafeDebug.Log(e.Data.Replace("❤", " - "));
        byte command = e.RawData[0];
        byte[] input = BufferEdit.RemoveFirst(e.RawData);
        string inputStr = Encoding.UTF8.GetString(input);
        string sendStr = string.Empty;
        switch (command) {
        #if UNITY_EDITOR
            case 0: // salt received.
                if (input[0] == 0) {
                    GameControl.Instance.errorMsg = "No user named " + username;
                    SafeDebug.LogWarning("no user named " + username);
                    Close();
                    break;
                }
                salt = inputStr;
                string hashedPass = HashHelper.HashPasswordClient(password, salt);
                sendStr = username + "★" + hashedPass + "★" + IP;
                Send(1, sendStr); // send login request
                break;

            case 1: // login result received.
                if (inputStr.Length == 1) {
                    switch (inputStr) {
                        case "0":
                            GameControl.Instance.errorMsg = "No user named " + username;
                            SafeDebug.LogWarning("No user named " + username);
                            break;
                        case "1":
                            GameControl.Instance.errorMsg = "Incorrect password";
                            SafeDebug.LogWarning("Incorrect password");
                            break;
                        case "2":
                            GameControl.Instance.errorMsg = "Inactive account";
                            SafeDebug.LogWarning("Inactive account");
                            break;
                        case "3":
                            GameControl.Instance.errorMsg = "Banned account";
                            SafeDebug.LogWarning("Banned account");
                            break;
                        case "4":
                            GameControl.Instance.errorMsg = "Already Playing";
                            SafeDebug.LogWarning("Already Playing");
                            break;
                        default:
                            GameControl.Instance.errorMsg = "error: " + inputStr;
                            SafeDebug.LogWarning("error: " + inputStr);
                            break;
                    }
                    Close();
                    return;
                }
                //for (int i = 0; i < input.Length; i++)
                //    SafeDebug.Log(input[i]);
                sessionKey = inputStr;
                TaskQueue.QueueMain(() => GameControl.Instance.LoginSuccess(sessionKey));
                TaskQueue.QueueMain(() => InvokeRepeating("Ping", pingTime, pingTime));
                break;
        #endif
            case 2: // login ping
                if (input[0] == 0) {
                    TaskQueue.QueueMain(() => GameControl.Instance.LoginSuccess(sessionKey));
                    TaskQueue.QueueMain(() => InvokeRepeating("Ping", pingTime, pingTime));
                }
                else
                    SafeDebug.Log("Session key not valid");
                break;

            case 3: // ping
                if (input[0] == 1) {
                    SafeDebug.Log("Session key not valid");
                }
                break;
            case 4: // lobby get
                List<LobbyScript.LobbyEntry> entries = new List<LobbyScript.LobbyEntry>();
                string[] entriesStr = inputStr.Split('❤');
                for (int i = 0; i < entriesStr.Length; i++) {
                    string[] parts = entriesStr[i].Split('★');
                    if (parts.Length == 2) {
                        entries.Add(new LobbyScript.LobbyEntry(parts[0], parts[1]));
                    }
                }
                SafeDebug.Log("Lobby get: " + entries.Count + " matches.");
                LobbyScript.Instance.SetLobby(entries.ToArray());
                break;

            case 5: // commands.
                Traffic traffic = new Traffic((NetClient.OpCodes)input[0], BufferEdit.RemoveFirst(input));
                NetClient.Instance.ProcessData(traffic);
                break;

            case 6: // connection Closed.
                if (!GameControl.Instance.IsServer) {
                    SafeDebug.Log("Disconnected from game server: " + inputStr);
                    TaskQueue.QueueMain(() => {
                        NetClient.Instance.Disconnect();
                    });
                }
                break;
        }

    }

    private void Server_OnMessage(object sender, MessageEventArgs e) {
        TaskQueue.QueueMain(() => {
            byte[] dst = BufferEdit.RemoveFirst(e.RawData);
            Traffic traffic = new Traffic((OpCodes)e.RawData[0], dst);
            ProcessData(traffic);
        });
    }

    private void Register_OnMessage(object sender, MessageEventArgs e) {
        if (e.RawData.Length == 1) {
            switch (e.RawData[0]) {
                case 0:
                    SafeDebug.Log("Registration successfull!");
                    break;

                case 1:
                    SafeDebug.LogError("Registration failed: Name invalid!");
                    break;

                case 2:
                    SafeDebug.LogError("Registration failed: email invalid!");
                    break;

                case 3:
                    SafeDebug.LogError("Registration failed: Name already taked!");
                    break;
            }
        }
        Close();
    }

    private void Server_OnClose(object sender, CloseEventArgs e) {
        TaskQueue.QueueMain(() => {
            Destroy(GameLobby.Instance.gameObject);
            if (GamePlay.Instance != null) {
                Destroy(GamePlay.Instance.gameObject);
                GamePlay.Instance = null;
            }
            GameControl.Instance.GameStarted = false;
            GameLobby.Instance = null;
            NetServer.Instance.ClearCommands();
            SceneManager.LoadScene(1);
            Login(username, password, IP, sessionKey);
            Debug.Log("server stopped");
        });
    }
#endif

    private void ProcessData(Traffic traffic) {
        //string decryptStr = HashHelper.Decrypt(data, _encryptionPass);
        if (CommandExists(traffic.byteCommand)) {
            Commands[traffic.byteCommand](traffic.byteData);
        }
        else
            Debug.Log("Command " + traffic.byteCommand + " doesn't exist.");
    }

    [NetCommand(OpCodes.routeBack)]
    private void RouteBack_CMD(byte[] data) {
        Traffic traffic = new Traffic((NetClient.OpCodes)data[0], BufferEdit.RemoveFirst(data));
        NetClient.Instance.ProcessData(traffic);
    }

    [NetCommand(OpCodes.Process)]
    private void Process_CMD(byte[] data) {
        byte user = data[0];
        byte[] sentBuffer = BufferEdit.RemoveFirst(data);
        Traffic traffic = new Traffic((NetServer.OpCodes)sentBuffer[0], BufferEdit.RemoveFirst(sentBuffer));
        NetServer.Instance.ProcessData(user, traffic, (tr) => { NetServer.Instance.Send(user, tr); });
    }

    private byte[] InsertCommand(byte command, string data) {
        return InsertCommand(command, Encoding.UTF8.GetBytes(data));
    }

    private byte[] InsertCommand(byte command, byte[] data) {
        return BufferEdit.AddFirst(command, data);
    }
    
}
