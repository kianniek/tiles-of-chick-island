using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickFSM : FSM
{
    internal Chick myChick;

    internal IdleState idleState;
    internal FollowPathState followPathState;
    internal EatSeedsState eatSeedsState;

    internal void Initialize(Chick myChick) 
    {
        this.myChick = myChick;

        idleState = new IdleState();
        idleState.Initialize(this);

        followPathState = new FollowPathState();
        followPathState.Initialize(this);

        eatSeedsState = new EatSeedsState();
        eatSeedsState.Initialize(this);

        base.Initialize();
    }

    protected override State GetInitialState()
    {
        return idleState;
    }
}
