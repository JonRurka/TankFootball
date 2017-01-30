using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LobbyChatUIControl : UIPanelControl {
    public InputField messageInput;
    public Text chatBoxText;
    public RectTransform contentTrans;
    public ScrollRect scrollView;

    public List<string> Messages = new List<string>();

    public string messageText;

    public Color userColor;
    public Color modColor;
    public Color adminColor;
    public Color systemColor;

    private string boxText;

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Return)) {
            string input = messageText;
            messageText = string.Empty;
            messageInput.text = string.Empty;
            GameControl.Instance.SubmitChat(input);
            messageInput.Select();
            messageInput.ActivateInputField();
        }
	}

    public void ChatEnabled(bool enabled) {
        Messages.Clear();
        chatBoxText.text = string.Empty;
        chatBoxText.rectTransform.sizeDelta = new Vector2(chatBoxText.rectTransform.sizeDelta.x, 0);
        if (enabled) {
            GameControl.Instance.OnChatMessageReceived += OnMessageReceived;
        }
        else {
            GameControl.Instance.OnChatMessageReceived -= OnMessageReceived;
        }
    }

    public void OnMessageReceived(UserNameColors color, string user, string message) {
        Color uColor = userColor;
        switch (color) {
            case UserNameColors.White:
                uColor = userColor;
                break;
            case UserNameColors.Green:
                uColor = modColor;
                break;
            case UserNameColors.Red:
                uColor = adminColor;
                break;
            case UserNameColors.Yellow:
                uColor = systemColor;
                break;
        }

        string messageStr = string.Format("<color=#{0}>{1}</color>: {2}", 
                                          ColorUtility.ToHtmlStringRGB(uColor), user, message);
        Messages.Add(messageStr);
        UpdateBoxText();
    }

    public void OnValueChanged(string text) {
        messageText = text;
    }

    public void UpdateBoxText() {

        boxText = string.Empty;
        for (int i = 0; i < Messages.Count; i++) {
            boxText += Messages[i] + "\n";
        }
        TextGenerator textGen = new TextGenerator();
        TextGenerationSettings genSettings = chatBoxText.GetGenerationSettings(chatBoxText.rectTransform.rect.size);
        float height = textGen.GetPreferredHeight(boxText, genSettings);
        contentTrans.sizeDelta = new Vector2(contentTrans.sizeDelta.x, height);
        chatBoxText.text = boxText;
        if (scrollView.verticalScrollbar != null) {
            if (scrollView.verticalScrollbar.value == 0)
                StartCoroutine(ScrollToBottom());
        }
    }

    IEnumerator ScrollToBottom() {
        yield return new WaitForEndOfFrame();
        if (scrollView.verticalScrollbar != null)
            scrollView.verticalScrollbar.value = 0;
    }
}
