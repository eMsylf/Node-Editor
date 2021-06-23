using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BobJeltes.AI.BehaviorTree
{
    public class Repeat : Decorator
    {
        public override void OnStart()
        {
            
        }

        public override void OnStop()
        {
            
        }

        public override Result Tick()
        {
            child.Tick();
            return Result.Running;
        }
    }
}
