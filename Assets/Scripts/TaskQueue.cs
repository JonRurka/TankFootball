using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


public class TaskQueue : MonoBehaviour
{
	private static List<Action> _actions = new List<Action>();
	private static List<Action> _currentActions = new List<Action>();
	private static Dictionary<string, AsyncTask> _asyncTasks = new Dictionary<string, AsyncTask> ();
	private static TaskQueue _instance;
	
	void Awake ()
	{
		DontDestroyOnLoad (this);
        if (_instance == null) {
            _instance = this;
        }
        else {
            Destroy(gameObject);
            Debug.Log("Only one task queue permitted.");
        }
	}
	
	void Update(){
		if (_actions.Count > 0) {
			lock (_actions) {
				_currentActions.Clear ();
				_currentActions.AddRange (_actions);
				_actions.Clear ();
			}
			for (int i = 0; i < _currentActions.Count; i++) {
				try {
					_currentActions [i] ();
					_currentActions [i] = null;
				}
				catch(Exception e){
					SafeDebug.LogError("Queue: " + e.GetType().ToString() + ": " + e.Message + "\n" + e.StackTrace);
					_currentActions[i] = null;
				}
			}
		}
	}

    void OnApplicationQuit() {
        if (_instance != null && _asyncTasks != null)
        {
            foreach (AsyncTask task in new List<AsyncTask>(_asyncTasks.Values))
            {
                task.Stop();
            }
            _asyncTasks.Clear();
        }
    }

    public static void QueueMain(Action action) {
		if (_actions != null) {
			lock (_actions){
				_actions.Add(action);
			}
		}
	}
	
	public static void QueueAsync(string thread, Action e){
		if (_asyncTasks != null) {
			lock(_asyncTasks){
				if (_asyncTasks.ContainsKey(thread)){
					_asyncTasks[thread].AddTask(e);
				}
				else{
					AddAsyncQueue(thread);
					QueueAsync(thread, e);
				}
			}
		}
	}
	
	public static void AddAsyncQueue(string thread){
		if (_asyncTasks != null) {
			lock (_asyncTasks){
				if (!_asyncTasks.ContainsKey(thread)){
					AsyncTask task = new AsyncTask(thread);
					_asyncTasks.Add(thread, task);
				}
			}
		}
	}
	
	public static bool ThreadExists(string thread) {
		return _asyncTasks.ContainsKey(thread);
	}
	
	public static Thread GetThreadRef(string thread){
		if (_asyncTasks != null) {
			lock (_asyncTasks){
				if  (_asyncTasks.ContainsKey(thread)){
					return _asyncTasks[thread].thread;
				}
				else return null;
			}
		}
		return null;
	}
}

