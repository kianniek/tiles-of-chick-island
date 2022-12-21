using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State
{
    private const float MINIMUM_TURN_HEAD_TIME = 3f;
    private const float MAXIMUM_TURN_HEAD_TIME = 10f;
    private float turnHeadTimer;

    internal override void Enter()
    {
        // reset turn head timer
        ResetTurnHeadTimer();
    }

    internal override void Update()
    {
        // decrease the timer
        turnHeadTimer -= Time.deltaTime;

        // if timer hits 0, turn head
        if(turnHeadTimer <= 0)
        {
            // turn head
            ((ChickFSM)fsm).myChick.TurnHead();

            // reset the timer to start counting again
            ResetTurnHeadTimer();
        }
    }

    internal override void Exit()
    {
        // no exit behavior
    }

    /// <summary>
    /// Call to give the timer a new random value.
    /// </summary>
    private void ResetTurnHeadTimer() 
    {
        turnHeadTimer = Random.Range(MINIMUM_TURN_HEAD_TIME, MAXIMUM_TURN_HEAD_TIME);
    }
}
