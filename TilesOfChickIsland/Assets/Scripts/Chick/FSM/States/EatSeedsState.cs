using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EatSeedsState : State
{
    internal override void Enter()
    {
        // start eating animation
        ((ChickFSM)fsm).myChick.Eat(true);
    }

    internal override void Update()
    {
        // no update behavior
    }

    internal override void Exit()
    {
        // end eating animation
        ((ChickFSM)fsm).myChick.Eat(false);
    }
}
