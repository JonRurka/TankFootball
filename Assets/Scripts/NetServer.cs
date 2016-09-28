using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

public class NetServer : MonoBehaviour {
    public delegate Traffic CMD(Player user, byte[] data);

    public enum OpCodes : byte {
        None,
        GetLobby,
        GameOpen,
        SubmitInput,
        Shoot,
        Ping,
        Close
    }

    public static NetServer Instance;

    private Dictionary<byte, CMD> _commands;

    void Awake() {
        Instance = this;
        _commands = new Dictionary<byte, CMD>();
    }

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void AddCommands(object target) {
        MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++) {
            try {
                ServerCommand cmdAttribute = (ServerCommand)Attribute.GetCustomAttribute(methods[i], typeof(ServerCommand));
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
                    Debug.LogErrorFormat("Error adding server network function: " + methods[i].DeclaringType.Name + "." + methods[i].Name);
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

    public void ClearCommands() {
        _commands.Clear();
    }

    public bool CommandExists(byte Cmd) {
        return _commands.ContainsKey(Cmd);
    }

    public void RemoveUser(byte userID, string reason, bool removed) {
        if (!removed)
            Send(userID, NetClient.OpCodes.Close, reason);
        GameControl.Instance.RemoveUser(userID);
    }

    public void Stop() {
        Stop("Server stopped.");
    }

    public void Stop(string reason) {
        byte[] sendBuffer = Encoding.UTF8.GetBytes(reason);
        MainServerConnect.Instance.SendServer(new Traffic(MainServerConnect.ServerOpCodes.Close, sendBuffer));
    }

    public void Send(NetClient.OpCodes command) {
        Send(command, new byte[0]);
    }

    public void Send(NetClient.OpCodes command, string data) {
        Send(command, Encoding.UTF8.GetBytes(data));
    }

    public void Send(NetClient.OpCodes command, byte[] data) {
        Send(new Traffic(command, data));
    }

    public void Send(Traffic traffic) {
        byte[] sendBuffer = BufferEdit.AddFirst(255, BufferEdit.AddFirst(traffic.byteCommand, traffic.byteData));
        MainServerConnect.Instance.SendServer(new Traffic(MainServerConnect.ServerOpCodes.Route, sendBuffer));
    }

    public void Send(byte[] users, NetClient.OpCodes command) {
        Send(users, command, new byte[0]);
    }

    public void Send(byte[] users, NetClient.OpCodes command, string data) {
        Send(users, command, Encoding.UTF8.GetBytes(data));
    }

    public void Send(byte[] users, NetClient.OpCodes command, byte[] data) {
        Send(users, new Traffic(command, data));
    }

    public void Send(byte[] users, Traffic traffic) {
        List<byte> sendBuffer = new List<byte>();
        sendBuffer.Add((byte)users.Length);
        sendBuffer.AddRange(users);
        sendBuffer.AddRange(BufferEdit.AddFirst(traffic.byteCommand, traffic.byteData));
        MainServerConnect.Instance.SendServer(new Traffic(MainServerConnect.ServerOpCodes.Route, sendBuffer.ToArray()));
    }

    public void Send(byte user, NetClient.OpCodes command) {
        Send(user, command, new byte[0]);
    }

    public void Send(byte user, NetClient.OpCodes command, string data) {
        Send(user, command, Encoding.UTF8.GetBytes(data));
    }

    public void Send(byte user, NetClient.OpCodes command, byte[] data) {
        Send(user, new Traffic(command, data));
    }

    public void Send(byte user, Traffic traffic) {
        if (GameControl.Instance.UserExists(user)) {
            List<byte> sendBuffer = new List<byte>();
            sendBuffer.AddRange(new byte[] { 1, user });
            sendBuffer.AddRange(BufferEdit.AddFirst(traffic.byteCommand, traffic.byteData));
            MainServerConnect.Instance.SendServer(new Traffic(MainServerConnect.ServerOpCodes.Route, sendBuffer.ToArray()));
        }
    }

    public void ProcessData(byte userID, Traffic traffic, Action<Traffic> onComplete) {
        //string decryptStr = HashHelper.Decrypt(data, _encryptionPass);

        TaskQueue.QueueMain(() => {
            try {
                if (CommandExists(traffic.byteCommand)) {
                    Player user = GameControl.Instance.GetUser(userID);
                    Traffic result = _commands[traffic.byteCommand](user, traffic.byteData);
                    if (onComplete != null && result.clientOpCode != NetClient.OpCodes.None)
                        onComplete(result);
                }
                else
                    SafeDebug.LogWarning("Net Server: Command not found: " + traffic.serverOpCode + ", " + traffic.byteCommand);
            }
            catch (Exception e) {
                Debug.LogErrorFormat("{0}: {1}: {2}\n{3}", traffic.serverOpCode, e.GetType().ToString(), e.Message, e.StackTrace);
            }
        });
    }
}

