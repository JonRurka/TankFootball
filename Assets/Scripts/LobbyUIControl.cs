using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIControl : UIPanelControl {
    public GameObject ButtonPrefab;    

    public Button createMatchButton;
    public ScrollRect scrollView;

    private LobbyScript.LobbyEntry[] matches;
    private Button[] matchButtons;

	// Use this for initialization
	void Start () {
        matchButtons = new Button[0];
        LobbyScript.Instance.OnLobbyRefresh += LobbyRefreshed;
        LobbyScript.Instance.Refresh();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void UIEnabled(bool enabled) {
        base.UIEnabled(enabled);
    }

    public override void SetMode(UIControl.UIType type) {
        base.SetMode(type);
    }

    public void LobbyRefreshed(LobbyScript.LobbyEntry[] entries) {
        matches = entries;
        BuildMatchList();
    }

    public void ClearMatchList() {
        for (int i = 0; i < matchButtons.Length; i++) {
            Destroy(matchButtons[i].gameObject);
        }
        matchButtons = new Button[0];
    }

    public void BuildMatchList() {
        ClearMatchList();
        matchButtons = new Button[matches.Length];
        RectTransform panelTrans = Panel.GetComponent<RectTransform>();
        scrollView.content.sizeDelta = new Vector2(scrollView.content.sizeDelta.x, (matches.Length * 30) + 20);
        for (int i = 0; i < matches.Length; i++) {
            GameObject obj = Instantiate(ButtonPrefab, scrollView.content);
            RectTransform rTrans = obj.GetComponent<RectTransform>();
            Button button = obj.GetComponent<Button>();
            Text text = obj.GetComponentInChildren<Text>();
            LobbyScript.LobbyEntry ent = matches[i];
            button.onClick.AddListener(() => LobbyScript.Instance.Connect(ent.hostID, ent.description));
            rTrans.anchorMin = new Vector2(0, 1);
            rTrans.anchorMax = new Vector2(0, 1);
            rTrans.localPosition = new Vector3(304, (i * -30) - 30, 0);
            text.text = matches[i].description;
            matchButtons[i] = button;
        }
    }

    public void OnRefreshClick() {
        LobbyScript.Instance.Refresh();
    }

    public void OnCreateMatchClick() {
        LobbyScript.Instance.CreateMatch();
    }
}
