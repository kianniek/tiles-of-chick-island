using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State 
{
    protected FSM fsm;

    /// <summary>
    /// Call to set up the state.
    /// </summary>
    /// <param name="fsm">FSM this state belongs to</param>
    internal virtual void Initialize(FSM fsm)
    {
        this.fsm = fsm;
    }

    /// <summary>
    /// Called when the state is entered.
    /// Becomes the current state of the FSM afterwards.
    /// </summary>
    internal abstract void Enter();

    /// <summary>
    /// Called every frame when this state 
    /// is the current state of the FSM.
    /// </summary>
    internal abstract void Update();

    /// <summary>
    /// Called when the state is exited. 
    /// Cleans up and finishes the state.
    /// </summary>
    internal abstract void Exit();
}
