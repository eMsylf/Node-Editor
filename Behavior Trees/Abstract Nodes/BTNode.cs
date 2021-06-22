﻿namespace BobJeltes.AI.BehaviorTree
{
    public abstract class BTNode
    {
        public abstract Result Tick();
    }

    public enum Result
    {
        Success,
        Failure,
        Running
    }
}