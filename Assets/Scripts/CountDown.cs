using UnityEngine;
using System.Collections;

public class CountDown {
    public IGamePlayController Controller { get; private set; }
    public string Name { get; private set; }
    public float Time { get; private set; }
    public float Span { get; set; }
    public bool Repeat { get; set; }
    public bool Finished { get; set; }
    public bool TimerPaused { get; set; }
    public System.Action Callback { get; set; }

    public CountDown(IGamePlayController Controller, string name, float timeSpan, System.Action callback, bool repeat = false) {
        Name = name;
        Time = timeSpan;
        Span = timeSpan;
        Callback = callback;
        Repeat = repeat;
        Finished = false;
        TimerPaused = false;
    }

    public void Update(float t) {
        if (!Finished) {
            if (!TimerPaused)
                Time -= t;

            if (Time <= 0) {
                Callback();
                if (Repeat)
                    Reset();
                else {
                    Finished = true;
                    Controller.RemoveCountDown(Name);
                }
            }
        }
    }

    public void Reset() {
        Time = Span;
        Finished = false;
        Resume();
    }

    public void Pause() {
        TimerPaused = false;
    }

    public void Resume() {
        TimerPaused = false;
    }
}
