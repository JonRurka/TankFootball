using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

public class MainServerConnect : MonoBehaviour {
    public delegate void CMD(Data data);

    public class CommandInfo {
        public readonly CMD Cmd;
        public readonly byte Value;
        public readonly ClientCMD Opcode;
        public readonly bool Async;

        public CommandInfo(CMD callback, Command att) {
            Cmd = callback;
            Value = att.Value;
            Opcode = att.Opcode;
            Async = att.Async;
        }

        public CommandInfo(byte value, CMD callback, bool async) {
            Cmd = callback;
            Value = value;
            Opcode = (ClientCMD)value;
            Async = async;
        }
    }

    public enum HostType {
        local,
        remote
    }

    // OLD
    public enum ServerOpCodes : byte {
        None,
        Register,
        Ping,
        Route,
        GameStart,
        Close,
    }

    // OLD
    public enum OpCodes : byte {
        None,
        CompleteRegister,
        CompleteConnect,
        routeBack,
        Process,
        UserDisconnect,
        EndMatch,
        Ping,
    }


    public static MainServerConnect Instance;

    public HostType hostType = HostType.local;
    public string remoteHost = "tankfootball.com";
    public string debugHost = "10.0.1.17";
    public string host;
    public int TcpPort = 11010;
    public int UdpPort = 11011;
    public int UdpLocalPort = 0;
    public float pingTime = 60;
    public bool udpEnabled = false;
    public bool udpStarted = false;
    public int UdpID { get; set; }
    public bool IsServer { get; set; }
    public bool Run { get; private set; }
    public bool Connected { get; private set; }
    public int BytesSent_PS { get; private set; }
    public int PacketsSent_PS { get; private set; }
    public int BytesReceived_PS { get; private set; }
    public int PacketsReceived_PS { get; private set; }

#if !UNITY_WEBGL
    //private WebSocket socket;
    private TcpClient tcpClient;
    private UdpClient udpClient;
    private Socket tcpSocket;

    private IPEndPoint endPoint;
#endif
    public string username;
    public string password;
    public string sessionKey;
    public string salt;
    private int _receivedBytes;
    private int _received;
    private int _sentBytes;
    private int _sent;
    private Thread mainThread;

    private bool _udpPingEnabled;

    private System.Diagnostics.Stopwatch watch;
    private System.Diagnostics.Stopwatch timeOutWatch;

    public Dictionary<byte, CommandInfo> Commands { get; private set; }

    void Awake() {
        Instance = this;
        Commands = new Dictionary<byte, CommandInfo>();
        watch = new System.Diagnostics.Stopwatch();
        timeOutWatch = new System.Diagnostics.Stopwatch();
        AddCommands(this);
        mainThread = Thread.CurrentThread;
        if (hostType == HostType.local) {
            host = debugHost;
        }
        else {
            host = remoteHost;
        }
    }

    // Use this for initialization
    void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {

    }

    void OnApplicationQuit() {
        DoSend(InsertCommand((byte)ServerCMD.ClientClose, new byte[1]));
        Close(false);
    }

    public void AddCommands(object target) {
        MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++) {
            try {
                Command cmdAttribute = (Command)Attribute.GetCustomAttribute(methods[i], typeof(Command));
                if (cmdAttribute != null) {
                    CMD function = null;
                    if (methods[i].IsStatic)
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), methods[i], true);
                    else
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), target, methods[i], true);
                    if (function != null) {
                        if (CommandExists(cmdAttribute.Value))
                            Commands[cmdAttribute.Value] = new CommandInfo(function, cmdAttribute);
                        else
                            Commands.Add(cmdAttribute.Value, new CommandInfo(function, cmdAttribute));
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

    public void AddCommand(byte cmd, CMD callback, bool async = false) {
        if (!CommandExists(cmd)) {
            Commands.Add(cmd, new CommandInfo(cmd, callback, async));
        }
    }

    public void RemoveCommand(byte cmd) {
        if (CommandExists(cmd))
            Commands.Remove(cmd);
    }

    public bool CommandExists(byte Cmd) {
        return Commands.ContainsKey(Cmd);
    }

    public void Login(string username, string password) {
        //#if UNITY_EDITOR
        TaskQueue.QueueAsync("Net", () => {
            this.username = username;
            this.password = password;
            IsServer = false;
            Run = true;
            tcpClient = new TcpClient(host, TcpPort);
            tcpSocket = tcpClient.Client;
            ListenLoop();
            Send(ServerCMD.GetSalt, Encoding.UTF8.GetBytes(username));
        });
//#endif
    }

    public void Login(string username, string password, string sessionKey) {
        TaskQueue.QueueAsync("Net", () => {
            this.username = username;
            this.password = password;
            IsServer = false;
            Run = true;
            tcpClient = new TcpClient(host, TcpPort);
            tcpSocket = tcpClient.Client;
            ListenLoop();
            Send(ServerCMD.KeyLogin, sessionKey);
        });
    }

    public void StartUdp() {
        try {
            if (GameControl.Instance.StartUdp) {
                udpClient = new UdpClient();
                udpClient.Connect(host, UdpPort);
                endPoint = udpClient.Client.RemoteEndPoint as IPEndPoint;
                UdpLocalPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
                Debug.LogFormat("UDP client starting: {0}, {1}, {2}", UdpLocalPort, endPoint.Port, endPoint.Address.ToString());
                udpStarted = true;
                BegineReceiveUDP();
                Send(ServerCMD.StartUdp, BitConverter.GetBytes(UdpLocalPort), Protocal.Udp);
                StartUdpPing();
            }
            else {
                Debug.Log("UDP not started.");
            }
        }
        catch (Exception ex) {
            Debug.Log(ex.ToString());
        }
    }

    public void StartTcpPing() {
        InvokeRepeating("TcpPing", 0.1f, 0.1f);
    }

    public void StartUdpPing() {
        _udpPingEnabled = true;
        InvokeRepeating("UdpPing", 0.1f, 0.1f);
    }

    public void Send(ServerCMD cmd, Protocal type = Protocal.Tcp) {
        Send((byte)cmd, new byte[1], type);
    }

    public void Send(ServerCMD cmd, string data, Protocal type = Protocal.Tcp) {
        Send((byte)cmd, Encoding.UTF8.GetBytes(data), type);
    }

    public void Send(ServerCMD cmd, byte[] data, Protocal type = Protocal.Tcp) {
        Send((byte)cmd, data, type);
    }

    public void Send(byte command, Protocal type = Protocal.Tcp) {
        Send(command, new byte[1], type);
    }

    public void Send(byte command, string data, Protocal type = Protocal.Tcp) {
        Send(InsertCommand(command, data), type);
    }

    public void Send(byte command, byte[] data, Protocal type = Protocal.Tcp) {
        Send(InsertCommand(command, data), type);
    }

    public void Send(byte[] data, Protocal type = Protocal.Tcp) {
        if ((data.Length + 2) >= 65536) {
            SafeDebug.LogError(string.Format("Send data length exceeds 65,536: {0} - {1}", data.Length + 2, BitConverter.ToString(data, 0, 4)));
            return;
        }

        //data = BufferEdit.Add(BitConverter.GetBytes(IsServer), data);

        if (Thread.CurrentThread == mainThread) {
            TaskQueue.QueueAsync("Net", () => {
                DoSend(data, type);
            });
        }
        else {
            DoSend(data, type);
        }
    }

    public void GetLobby() {
        Send(ServerCMD.GetMatches);
    }

    public void SendServer(Traffic data, Protocal type = Protocal.Tcp) {
        Send(BufferEdit.AddFirst(data.byteCommand, data.byteData), type);
    }

    public bool IsConnected() {
        if (tcpSocket == null || tcpClient == null)
            return false;
        return !(tcpSocket.Poll(1, SelectMode.SelectRead) && tcpClient.Available == 0);
    }

    public void Close(bool closeScreen, string reason = "") {
        if (tcpClient != null) {
            //CloseServer();
            CancelInvoke();
            Run = false;
            udpEnabled = false;
            tcpClient.Close();
            tcpClient = null;
            tcpSocket = null;
            if (udpClient != null) {
                udpClient.Close();
                udpClient = null;
            }
            watch.Stop();
            if (closeScreen) {
                GameControl.Instance.ClosedScreen(reason);
            }
        }
        Debug.Log("Server connection closed.");
    }

    public void CloseServer() {
        if (IsServer) {
            /*Destroy(GameLobby.Instance.gameObject);
            if (GamePlay.Instance != null) {
                Destroy(GamePlay.Instance.gameObject);
                GamePlay.Instance = null;
            }
            GameControl.Instance.GameStarted = false;
            GameLobby.Instance = null;
            NetServer.Instance.ClearCommands();
            SceneManager.LoadScene(1);
            //Login(username, password, IP, sessionKey);
            Debug.Log("server stopped");
            IsServer = false;*/
        }
    }

    // debug function.
    public void RegisterServer(string description = "") {
        if (description == string.Empty)
            description = username + "'s football match.";
        Send(ServerCMD.CreateMatch, Encoding.UTF8.GetBytes(description));
    }

    public string GetPhysicalAddress(string ipAddress) {
        IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        //Debug.LogFormat("Interface information for {0}.{1}     ",
        //    computerProperties.HostName, computerProperties.DomainName);
        if (nics == null || nics.Length < 1) {
            Debug.LogErrorFormat("  No network interfaces found.");
            return "";
        }

        //Debug.LogFormat("  Number of interfaces .................... : {0}", nics.Length);
        foreach (NetworkInterface adapter in nics) {
            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet) {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                //Debug.LogFormat(String.Empty.PadLeft(adapter.Description.Length, '='));
                //Debug.LogFormat(adapter.Description);
                //Debug.LogFormat("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) {
                    sb.Append(bytes[i].ToString("X2"));
                    if (i != bytes.Length - 1)
                        sb.Append("-");
                }
                //Debug.LogFormat("  Physical address ........................ : " + sb.ToString());
                foreach (UnicastIPAddressInformation ip in adapter.GetIPProperties().UnicastAddresses) {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
                        string adapterAddress = ip.Address.ToString();
                        if (adapterAddress == ipAddress) {
                            return sb.ToString();
                        }
                    }
                }
            }
        }
        Debug.LogError("Physical Address not found!");
        return "";
    }

    public string GetLocalIPAddress() {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                return ip.ToString();
            }
        }
        throw new Exception("Local IP Address Not Found!");
    }

    /*
    
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

    */

    private void DoSend(byte[] data, Protocal type = Protocal.Tcp) {
        if (type == Protocal.Tcp || !udpStarted) {
            if (tcpClient != null && tcpSocket != null) {
                //if ((ServerCMD)data[0] != ServerCMD.Ping)
                //    SafeDebug.Log("SENDING: " + (ServerCMD)data[0]);
                data = AddLength(data);
                tcpSocket.Send(data);
            }
        }
        else {
            if (udpClient != null) {
                byte[] udpIdBuff = BitConverter.GetBytes((UInt16)UdpID);
                byte[] buffer = BufferEdit.Add(udpIdBuff, data);
                udpClient.Send(buffer, buffer.Length);
            }
            else
                SafeDebug.Log("udp null");
        }
    }

    private void BegineReceiveUDP() {
        udpClient.BeginReceive(new AsyncCallback(UdpReadCallback), null);
    }

    private void UdpReadCallback(IAsyncResult ar) {
        byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
        if (receivedBytes.Length > 0) {
            udpEnabled = true;
            Process(receivedBytes, Protocal.Udp);
            //bool isServer = BitConverter.ToBoolean(receivedBytes, 0);
            //receivedBytes = BufferEdit.RemoveCmd(receivedBytes);

            /*if (isServer) {
                Process_Server(receivedBytes, Protocal.Udp);
            }
            else
                Process_User(receivedBytes);*/
        }
        BegineReceiveUDP();
    }

    private void UdpPing() {
        Ping(Protocal.Udp);
    }

    private void TcpPing() {
        Ping(Protocal.Tcp);
    }

    private void Ping(Protocal type) {
        if (type == Protocal.Udp && !udpStarted)
            return;
        // sending "true" to tell server to ping back.
        Send(ServerCMD.Ping, BitConverter.GetBytes(true), type);
    }

    private void ListenLoop() {
        TaskQueue.QueueAsync("Net_loop", () => {
            watch.Start();
            Socket socket = tcpClient.Client;
            ManualResetEvent reset = new ManualResetEvent(false);
            timeOutWatch.Start();
            while (Run) {
                if (watch.Elapsed.Seconds >= 1) {
                    BytesReceived_PS = _receivedBytes;
                    PacketsReceived_PS = _received;
                    BytesSent_PS = _sentBytes;
                    PacketsSent_PS = _sent;

                    _receivedBytes = 0;
                    _received = 0;
                    _sentBytes = 0;
                    _sent = 0;

                    watch.Reset();
                    watch.Start();
                }

                if (timeOutWatch.ElapsedMilliseconds >= 10000) {
                    Debug.Log("Connection timed out!");
                    TaskQueue.QueueMain(() => Close(true, "timed out"));
                    break;
                }

                if (socket.Available >= 1) {
                    byte[] lengthBuff = new byte[2];
                    socket.Receive(lengthBuff, 0, 2, SocketFlags.None);
                    int bufferLength = BitConverter.ToUInt16(lengthBuff, 0) - 2;
                    byte[] dataBuff = new byte[bufferLength];
                    List<byte> listBuff = new List<byte>();
                    int bytesNeeded = bufferLength;
                    while (bytesNeeded > 0) {
                        byte[] partialReceiveBuff = new byte[65536];
                        int rx = socket.Receive(partialReceiveBuff, 0, bytesNeeded, SocketFlags.None);
                        byte[] partialBuff = new byte[rx];
                        Array.Copy(partialReceiveBuff, 0, partialBuff, 0, rx);
                        listBuff.AddRange(partialBuff);
                        bytesNeeded -= rx;
                    }
                    dataBuff = listBuff.ToArray();
                    timeOutWatch.Reset();
                    timeOutWatch.Start();
                    Connected = true;
                    Process(dataBuff, Protocal.Tcp);
                    //bool isServer = BitConverter.ToBoolean(dataBuff, 0);
                    //dataBuff = BufferEdit.RemoveCmd(dataBuff);
                    /*if (isServer)
                        Process_Server(dataBuff, Protocal.Tcp);
                    else
                        Process_User(dataBuff);*/
                }
                reset.WaitOne(1);
            }
        });
    }

    private void Process(byte[] data, Protocal type) {
        byte command = data[0];
        byte[] dst = BufferEdit.RemoveCmd(data);
        if (CommandExists(command)) {
            if (Commands[command].Async)
                Commands[command].Cmd(new Data(type, command, dst));
            else {
                TaskQueue.QueueMain(() => Commands[command].Cmd(new Data(type, command, dst)));
            }
        }
        //Traffic traffic = new Traffic((OpCodes)command, dst);
        //ProcessData(traffic, type);
    }

    // OLD
    private void Process_User(byte[] data) {
        byte command = data[0];
        byte[] input = BufferEdit.RemoveCmd(data);
        string inputStr = Encoding.UTF8.GetString(input);
        string sendStr = string.Empty;
        string[] parts;
        switch (command) {
#if UNITY_EDITOR
            case 0: // salt received.
                if (input[0] == 1) {
                    salt = Encoding.UTF8.GetString(BufferEdit.RemoveCmd(input));
                    string hashedPass = HashHelper.HashPasswordClient(password, salt);
                    sendStr = hashedPass; // TODO: mac address
                    Send(1, sendStr); // send login request
                    break;
                }
                else if (input[0] == 2) {
                    GameControl.Instance.errorMsg = username + " already connected.";
                    SafeDebug.LogWarning(username + " already connected.");
                    Close(false);
                    break;
                }
                else if (input[0] == 3) {
                    GameControl.Instance.errorMsg = "No user named " + username;
                    SafeDebug.LogWarning("no user named " + username);
                    Close(false);
                    break;
                }
                break;

            case 1: // login result received.
                if (inputStr.Length == 1) {
                    switch (inputStr) {
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
                        default:
                            GameControl.Instance.errorMsg = "error: " + inputStr;
                            SafeDebug.LogWarning("error: " + inputStr);
                            break;
                    }
                    Close(false);
                    return;
                }
                //for (int i = 0; i < input.Length; i++)
                //    SafeDebug.Log(input[i]);
                parts = inputStr.Split('|');
                sessionKey = parts[0];
                UdpID = int.Parse(parts[1]);
                Debug.Log("UDP id: " + UdpID);
                GameControl.Instance.LoginSuccess(sessionKey);
                StartUdp();
                //TaskQueue.QueueMain(() => InvokeRepeating("Ping", pingTime, pingTime));
                break;
#endif
            case 2: // session key login result
                if (input[0] == 1) {
                    input = BufferEdit.RemoveCmd(input);
                    UdpID = BitConverter.ToUInt16(input, 0);
                    GameControl.Instance.LoginSuccess(sessionKey);
                    StartUdp();
                }
                else {
                    GameControl.Instance.errorMsg = "Session key not valid";
                    SafeDebug.Log("Session key not valid");
                }
                break;

            case 3: // ping
                //Debug.Log("Ping back: " + (input[0] == 0 ? Protocal.Tcp : Protocal.Udp));
                break;

            case 4: // lobby get
                List<LobbyScript.LobbyEntry> entries = new List<LobbyScript.LobbyEntry>();
                string[] entriesStr = inputStr.Split('❤');
                for (int i = 0; i < entriesStr.Length; i++) {
                    parts = entriesStr[i].Split('★');
                    if (parts.Length == 2) {
                        entries.Add(new LobbyScript.LobbyEntry(ushort.Parse(parts[0]), parts[1]));
                    }
                }
                SafeDebug.Log("Lobby get: " + entries.Count + " matches.");
                LobbyScript.Instance.SetLobby(entries.ToArray());
                break;

            case 5: // commands.
                Traffic traffic = new Traffic((NetClient.OpCodes)input[0], BufferEdit.RemoveCmd(input));
                NetClient.Instance.ProcessData(traffic);
                break;

            case 6: // connection Closed.
                // close dialoge.
                break;

            case 7: // notice
                SafeDebug.Log("NOTCE: " + inputStr);
                break;

            case 8: // end match
                GameControl.Instance.EndGame();
                break;

            case 100:
                udpEnabled = true;
                Debug.Log("UDP enabled.");
                break;

            default:
                Debug.LogError("No network user command: " + command);
                break;
                
        }
    }

    // OLD
    private void Process_Server(byte[] data, Protocal type) {
        byte command = data[0];
        byte[] dst = BufferEdit.RemoveCmd(data);
        Traffic traffic = new Traffic((OpCodes)command, dst);
        ProcessData(traffic, type);
    }

    // OLD
    private void ProcessData(Traffic traffic, Protocal type) {
        //string decryptStr = HashHelper.Decrypt(data, _encryptionPass);

        // NetCommands
        //if (type == Protocal.Udp) ;
        //    Debug.Log("Server Command: " + traffic.mainOpCodes);
        if (CommandExists(traffic.byteCommand)) {
            //Commands[traffic.byteCommand](traffic.byteData);
        }
        else
            Debug.Log("NetCommand " + traffic.byteCommand + " doesn't exist.");
    }

    // OLD
    [NetCommand(OpCodes.routeBack)]
    private void RouteBack_CMD(byte[] data) {
        Traffic traffic = new Traffic((NetClient.OpCodes)data[0], BufferEdit.RemoveCmd(data));
        NetClient.Instance.ProcessData(traffic);
    }

    // OLD
    [NetCommand(OpCodes.Process)]
    private void Process_CMD(byte[] data) {
        byte user = data[0];
        byte[] sentBuffer = BufferEdit.RemoveCmd(data);
        Traffic traffic = new Traffic((NetServer.OpCodes)sentBuffer[0], BufferEdit.RemoveCmd(sentBuffer));
        NetServer.Instance.ProcessData(user, traffic, (tr) => { NetServer.Instance.Send(user, tr); });
    }

    private byte[] InsertCommand(byte command, string data) {
        return InsertCommand(command, Encoding.UTF8.GetBytes(data));
    }

    private byte[] InsertCommand(byte command, byte[] data) {
        return BufferEdit.AddFirst(command, data);
    }

    private byte[] AddLength(byte[] data) {
        byte[] lengthBuff = BitConverter.GetBytes((UInt16)(data.Length));
        return BufferEdit.Add(lengthBuff, data);
    }
}
