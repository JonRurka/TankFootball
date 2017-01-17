using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Login : UIPanelControl {
    public InputField hostInput;
    public InputField usernameInput;
    public InputField passwordInput;

    public string userName;
    public string password;
    public string sessionKey;
    public string host;

    // Use this for initialization
    void Start () {
        userName = GameControl.Instance.userName;
        password = GameControl.Instance.password;
        sessionKey = GameControl.Instance.password;
        host = MainServerConnect.Instance.host;
        hostInput.text = host;

        if (sessionKey != string.Empty)
            MainServerConnect.Instance.Login(userName, password, sessionKey);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnHostChanged(string value) {
        host = value;
    }

    public void OnUserChanged(string value) {
        userName = value;
    }

    public void OnPasswordChanged(string value) {
        password = value;
    }

    public void OnLoginPressed() {
        GameControl.Instance.userName = userName;
        GameControl.Instance.password = password;
        MainServerConnect.Instance.host = host;
        MainServerConnect.Instance.Login(userName, password);
    }
}
