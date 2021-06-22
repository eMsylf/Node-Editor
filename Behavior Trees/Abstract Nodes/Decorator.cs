using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BobJeltes.AI.BehaviorTree
{
    public abstract class Decorator : BTNode
    {
        public BTNode child;
    }
}