using UnityEngine;
using System.Collections;

public interface IGamePlayController {
    IGamePlayController instance { get; }

    void RemoveCountDown(string name);
}
