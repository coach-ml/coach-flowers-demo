using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

[ExecuteInEditMode]
public class Loom : MonoBehaviour
{
	private static Loom _current;
	private int _count;
    private List<Action> _actions = new List<Action>();
    private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

    public static Loom Current
	{
		get {
			return _current;
		}
	}

	public static void Init()
	{
		if (_current == null)
		{
			var g = new GameObject("Loom");
			g.hideFlags = HideFlags.HideAndDontSave;
			_current = g.AddComponent<Loom>();
		}
	}

	public class DelayedQueueItem
	{
		public float time;
		public Action action;
	}
    
	public static void QueueOnMainThread(Action action)
	{
		QueueOnMainThread( action, 0f);
	}

	public static void QueueOnMainThread(Action action, float time)
	{
		if(time != 0)
		{
			lock(Current._delayed)
			{
				Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action});
			}
		}
		else
		{
			lock (Current._actions)
			{
				Current._actions.Add(action);
			}
		}
	}
	
	public static void RunAsync(Action a)
	{
		var t = new Thread(RunAction);
		t.Priority = System.Threading.ThreadPriority.Normal;
		t.Start(a);
	}
	
	private static void RunAction(object action)
	{
		((Action)action)();
	}
		
	void OnDisable()
	{
        if (_current == this)
            _current = null;
	}
	
	void Update()
	{
		var actions = new List<Action>();
		lock (_actions)
		{
			actions.AddRange(_actions);
			_actions.Clear();
			foreach(var a in actions)
			{
				a();
			}
		}
		lock(_delayed)
		{
			foreach(var delayed in _delayed.Where(d=>d.time <= Time.time).ToList())
			{
				_delayed.Remove(delayed);
				delayed.action();
			}
		}
	}
}
