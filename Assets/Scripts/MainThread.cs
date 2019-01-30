using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThread : MonoBehaviour
{
    public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();     // Essentially just a fifo list for thread management 

    public void Update()
    {
        while (ExecuteOnMainThread.Count > 0)
        {
            ExecuteOnMainThread.Dequeue().Invoke(); // cleares everything queued on main thread
        }
    }
}