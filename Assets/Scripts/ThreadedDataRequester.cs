using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ThreadedDataRequester : MonoBehaviour
{
	private static ThreadedDataRequester instance;

	Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

	void Awake()
	{
		instance = FindObjectOfType<ThreadedDataRequester>();
	}

	// Request data function, pass through func assigning an object labelled generateData and pass callback function to pass on the action function
	public static void RequestData(Func<object> generateData, Action<object> callback)
	{
		// Create threadstart, setting the delegate
		ThreadStart threadStart = delegate {
			// Call instance of threaded data requester, passing through the data thread calling generateData and callback
			instance.DataThread(generateData, callback);
		};
		// Start thread
		new Thread(threadStart).Start();
	}

	// DataThread function pass through func assigning an object labelled generateData and callback function to pass on the action function
	void DataThread(Func<object> generateData, Action<object> callback)
	{
		object data = generateData();

		// Lock allows it so that when one thread reaches this point, while it's executing this code no other thread can execute as well, it will have to wait its turn	
		lock (dataQueue)
		{
			// Threadinfo passing through callback and object data
			dataQueue.Enqueue(new ThreadInfo(callback, data));
		}
	}

	void Update()
	{
		// If mapDataThreadInfoQueue has something in it then loop through all the queue elements
		if (dataQueue.Count > 0)
		{
			// Loop through all the thread elements
			for (int i = 0; i < dataQueue.Count; i++)
			{
				// Thread info is equal to the next thing in the queue
				ThreadInfo threadInfo = dataQueue.Dequeue();

				// Pass in thread info parameter to callback
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}

	struct ThreadInfo
	{
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo(Action<object> callback, object parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}
}
