using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FSM 
{
    // keep track of our current state
    private State currentState;

    /// <summary>
    /// Call to initialize the state machine.
    /// </summary>
    internal virtual void Initialize()
    {
        // base class only makes the machine go to the first state
        // other initialize actions are handled by classes that inherit from this class.
        GotoState(GetInitialState());
    }

    /// <summary>
    /// Called once per frame.
    /// </summary>
    internal void Update()
    {
        // update the current state
        currentState?.Update();
    }

    /// <summary>
    /// Call to go to another state.
    /// </summary>
    /// <param name="state">The state to go to</param>
    internal void GotoState(State state)
    {
        // exit our current state if we have any
        currentState?.Exit();
        
        // set the new current state
        currentState = state;

        // enter the current state if we have any
        currentState?.Enter();
    }

    /// <summary>
    /// Get the initial state of this state machine.
    /// </summary>
    /// <returns>The initial state</returns>
    protected abstract State GetInitialState();
}
