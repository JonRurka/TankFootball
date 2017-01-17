using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
//#if !UNITY_WEBGL
//using WebSocketSharp;
//#endif

public class NetClient : MonoBehaviour {
    public delegate void CMD(byte[] data);

    public enum OpCodes : byte {
        None,
        Close,
        ConnectComplete,
        UpdateLobbyUsers,
        GameStart,
        SetTanks,
        AddTank,
        RemoveTank,
        UpdatePositions,
        UpdateTankPosition,
        UpdateBallPos,
        SetMaxSpeed,
        GiveBall,
        FreeBall,
        Shoot,
        SetEnabled,
        SetScore,
        End,
        CountDownStart,
        pingComplete,
    }

    public static NetClient Instance { get; private set; }
    public string userName;
    public string domain;
    public bool connected;
    //public int DownBps;
    //public int UpBps;
    //public int ReceivedPs;
    //public int SentPs;

    #if !UNITY_WEBGL
    //private WebSocket socket;
    #endif
    private bool _run;
    private int _receivedBytes;
    private int _sentBytes;
    private int _received;
    private int _sent;

    private string _encryptionPass = "QH5SnB7eXckcAqa8yUGPbqEsQ1XL9eo";

    private Dictionary<byte, CMD> _commands { get; set; }

    void Awake() {
        Instance = this;
        _commands = new Dictionary<byte, CMD>();
    }

    /*IEnumerator Start() {
        WebSocket echoSocket = new WebSocket(new Uri("ws://localhost:11010/echo"));
        yield return StartCoroutine(echoSocket.Connect());
        string sendstr = JsonConvert.SerializeObject(new Traffic("test", "hello"));
        echoSocket.Send(Encoding.UTF8.GetBytes(sendstr));
        while(true) {
            string reply = echoSocket.RecvString();
            if (reply != null) {
                Debug.Log(reply);
                break;
            }
            if (echoSocket.error != null) {
                Debug.LogErrorFormat("Error: {0}", echoSocket.error);
            }
            yield return 0;
        }
        echoSocket.Close();
    }*/

    public void Start() {
        /*string text = "{000:000:000:000:000:000:000:000}";
        //string text = "test";
        Debug.Log(text);
        string encrypted = HashHelper.Encrypt(text, "012345678");
        Debug.Log(encrypted);
        Debug.Log(HashHelper.Decrypt(encrypted, "012345678"));*/
    }

    void Update() {
        /*DownBps = _receivedBytes;
        _receivedBytes = 0;

        UpBps = _sentBytes;
        _sentBytes = 0;

        ReceivedPs = _received;
        _received = 0;

        SentPs = _sent;
        _sent = 0;*/
    }

    public void Connect(ushort host) {
        connected = true;
        GameControl.Instance.Connect(host);

        return;
        /*this.host = host;
        socket = new WebSocket(string.Format("ws://{0}:{1}/server", host, port));
        socket.OnMessage += Socket_OnMessage;
        socket.OnError += Socket_OnError;
        socket.Connect();
        connected = true;
        string sendStr = GameControl.Instance.userName + "★" + GameControl.Instance.sessionKey;*/
        //Send(new Traffic(NetServer.OpCodes.Connect, Encoding.UTF8.GetBytes(sendStr)));

    }

    public void AddCommands(object target) {
        MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++) {
            try {
                ClientCommand cmdAttribute = (ClientCommand)Attribute.GetCustomAttribute(methods[i], typeof(ClientCommand));
                if (cmdAttribute != null) {
                    CMD function = null;
                    if (methods[i].IsStatic)
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), methods[i]);
                    else
                        function = (CMD)Delegate.CreateDelegate(typeof(CMD), target, methods[i]);
                    if (function != null) {
                        if (CommandExists(cmdAttribute.byteCommand))
                            _commands[cmdAttribute.byteCommand] = function;
                        else
                            _commands.Add(cmdAttribute.byteCommand, function);
                    }
                }
            }
            catch (Exception e) {
                if (methods[i] != null) {
                    Debug.LogErrorFormat("Error adding client network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
                    Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
                }
            }
        }
    }

    public void AddCommand(byte cmd, CMD callback) {
        if (!CommandExists(cmd)) {
            _commands.Add(cmd, callback);
        }
    }

    public void RemoveCommand(byte cmd) {
        if (CommandExists(cmd))
            _commands.Remove(cmd);
    }

    public bool CommandExists(byte Cmd) {
        return _commands.ContainsKey(Cmd);
    }

    public void Send(NetServer.OpCodes command, Protocal type = Protocal.Tcp) {
        Send(command, new byte[0], type);
    }

    public void Send(NetServer.OpCodes command, string data, Protocal type = Protocal.Tcp) {
        Send(command, Encoding.UTF8.GetBytes(data), type);
    }

    public void Send(NetServer.OpCodes command, byte[] data, Protocal type = Protocal.Tcp) {
        Send(new Traffic(command, data), type);
    }

    public void Send(Traffic traffic, Protocal type) {
        if (GameControl.Instance.IsServer) {
            //Debug.LogWarning("Processed by local server: " + traffic.serverOpCode);
            NetServer.Instance.ProcessData(0, traffic, (tr) => { NetServer.Instance.Send(0, tr); });
            return;
        }

        if (connected) {
            //_sentBytes += traffic.data.Length;
            //_sent++;
            //Debug.Log("pre-encrypt");
            //string dataToSend = HashHelper.Encrypt(JsonConvert.SerializeObject(traffic), _encryptionPass);
            //Debug.Log("encrypt finished");
            //socket.Send(Convert.FromBase64String(dataToSend));
            //Debug.Log("pre-serialize");
            //string sendStr = traffic.command + Delimiter + traffic.data;

            //Debug.Log("serialize finished.");
            //socket.Send(sendStr);

            try {
                //byte[] sendBuffer = BufferEdit.AddFirst(traffic.byteCommand, traffic.byteData);
                List<byte> sendBuffer = new List<byte>();
                sendBuffer.AddRange(new byte[] { 5, traffic.byteCommand });
                sendBuffer.AddRange(traffic.byteData);
                MainServerConnect.Instance.Send(sendBuffer.ToArray(), type);
            }
            catch(Exception e) {
                Debug.LogErrorFormat("{0}: {1}\n{2}", e.GetType(), e.Message, e.StackTrace);
            }
        }
        else
            Debug.LogError("Not connected!");
    }

    /*public string GetDownRate() {
        return GetDataRate(DownBps);
    }

    public string GetUpRate() {
        return GetDataRate(UpBps);
    }*/

    public string GetDataRate(float bytes) {
        float kBytes = bytes / 1024f;
        float mBytes = kBytes / 1024f;
        float gBytes = mBytes / 1024f;

        if (bytes < 1024)
            return bytes + " B/s";
        else if (kBytes < 1024)
            return kBytes.ToString("0.000") + " KB/s";
        else if (mBytes < 1024)
            return mBytes.ToString("0.000") + " MB/s";
        else
            return gBytes.ToString("0.000") + " GB/s";
    }

    public void Close() {
    #if !UNITY_WEBGL
        //if (socket != null)
        //    socket.Close();
        _run = false;
        connected = false;
        Debug.Log("network client closed");
    #endif
    }

    /*private void Socket_OnError(object sender, ErrorEventArgs e) {
        SafeDebug.LogError("[NET CLIENT]: " + e.Exception.GetType().ToString() + ": " + e.Message);
    }

    private void Socket_OnMessage(object sender, MessageEventArgs e) {
        try {
            Traffic traffic = new Traffic((OpCodes)e.RawData[0], BufferEdit.RemoveFirst(e.RawData));
            ProcessData(traffic);
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("{0}: {1}\n{2}", ex.GetType(), ex.Message, ex.StackTrace);
        }
    }*/

    /*IEnumerator Connect_int() {
        string hostStr = string.Format("ws://{0}:{1}/echo", host, serverPort);
        WebSocket connectSocket = new WebSocket(new Uri(hostStr));
        yield return StartCoroutine(connectSocket.Connect());
        connectSocket.SendString("echotest");
        while (true) {
            string reply = connectSocket.RecvString();
            if (reply != null) {
                if (reply == "echotest") {
                    Debug.Log("echo success");
                    connected = true;
                    StartCoroutine(Loop_int());
                    NetworkController.Instance.Login();
                    break;
                }
                /*if (reply.Contains("domain=") && reply.Contains("key=")) {
                    string[] parts = reply.Split('&');
                    string[] domainStr = parts[0].Split('=');
                    string[] keyStr = parts[1].Split('=');
                    domain = domainStr[1];
                    //_encryptionPass = keyStr[1];
                    connected = true;
                    StartCoroutine(Loop_int());
                }
            }
            if (connectSocket.error != null) {
                Debug.LogErrorFormat("Error: {0}", connectSocket.error);
                break;
            }
            yield return 0;
        }
        Debug.Log("Echo Socket closing...");
        connectSocket.Close();
    }*/

    /*IEnumerator Loop_int() {
        _run = true;
        string hostStr = string.Format("ws://{0}:{1}/server", host, serverPort);
        socket = new WebSocket(new Uri(hostStr));
        yield return StartCoroutine(socket.Connect());
        while (_run) {
            string reply = socket.RecvString();
            if (reply != null) {
                ProcessData(reply);
            }
            if (socket.error != null) {
                Debug.LogErrorFormat("Error: {0}", socket.error);
                _run = false;
            }
            yield return 0;
        }

        if (socket != null)
            socket.Close();
        connected = false;
    }*/

    public void ProcessData(Traffic traffic) {
        //_receivedBytes += data.Length * sizeof(char);
        //_received++;
        //string decryptStr = HashHelper.Decrypt(data, _encryptionPass);
        TaskQueue.QueueMain(() => {
            if (CommandExists(traffic.byteCommand)) {
                _commands[traffic.byteCommand](traffic.byteData);
            }
        });
    }

    public const char Delimiter = '★';
	private const char endStr = '❤';

}
