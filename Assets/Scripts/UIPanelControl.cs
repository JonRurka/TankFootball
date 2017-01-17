using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelControl : MonoBehaviour {
    public GameObject Panel;
    public UIControl.UIType Type { get; protected set; }

    public virtual void UIEnabled(bool enabled) {
        Panel.SetActive(enabled);
    }

    public virtual void SetMode(UIControl.UIType type) {
        Type = type;
    }
}
