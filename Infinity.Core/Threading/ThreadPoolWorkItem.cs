﻿namespace Infinity.Core.Threading
{
    public delegate void ThreadedAction(object? state);

    public class ThreadPoolWorkItem
    {
        public ThreadedAction MethodToExecute;
        public ThreadedAction Callback;
        public object State;
    }
}
