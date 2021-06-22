using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BobJeltes.AI.BehaviorTree
{
    public class RootNode : Decorator
    {
        public override Result Tick()
        {
            return child.Tick();
        }

        public override Node Clone()
        {
            RootNode node = (RootNode)MemberwiseClone();
            node.child = child.Clone();
            return node;
        }
    }
}