using System;
using System.Collections.Generic;
using System.Threading;

public class AsyncTask
{
	private ManualResetEvent _resetEvent;
	private List<Action> _actions;
	private List<Action> _currentActions;
	private bool _run;
	
	public Thread thread { get; private set; }
	public string threadName{ get; private set; }
	
	public AsyncTask (string name)
	{
		threadName = name;
		_run = true;
		_resetEvent = new ManualResetEvent (false);
		_actions = new List<Action> ();
		_currentActions = new List<Action> ();
		thread = new Thread (Run);
		thread.Start ();
	}
	
	public void AddTask(Action e){
		lock (_actions) {
			_actions.Add(e);
		}
	}
	
	public void Stop(){
		_run = false;
        thread.Abort();
	}
	
	private void Run(){
		while (_run) {
			_resetEvent.WaitOne(50);
			if (_actions.Count > 0){
				lock(_actions){
					_currentActions.Clear();
					_currentActions.AddRange(_actions);
					_actions.Clear();
				}
				
				for (int i = 0; i < _currentActions.Count; i++){
					try {
						_currentActions[i]();
						_currentActions[i] = null;
					}
					catch(Exception e){
						//Logger.LogError("{0} queue: {1}", threadName, e.Message);
						_currentActions = null;
					}
				}
			}
		}
	}
}


