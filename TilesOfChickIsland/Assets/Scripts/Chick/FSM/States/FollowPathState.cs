using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPathState : State
{
    private List<Tile> MyPath { get { return MyChick.path; } }
    private Chick MyChick { get { return ((ChickFSM)fsm).myChick; } }

    private float previousDistance;
    private float currentDistance;

    internal override void Enter()
    {
        // start at the beginning of the path
        MyChick.currentTileIndex = 0;
        previousDistance = Mathf.Infinity;
    }

    internal override void Update()
    {
        // as long as there is a next tile to go to... 
        if (MyChick.currentTileIndex < MyPath.Count)
        {
            // get the current distance tot the tile
            currentDistance = MyChick.DistanceToCurrentTile();

            // check to see if the tile is reached, which is true if the chick
            // will reach that tile in this frame or if it overshot
            if (currentDistance <= MyChick.CurrentSpeed * Time.deltaTime ||
                currentDistance > previousDistance)
            {
                // if this is the last tile, make the chick walk the last part
                if (MyChick.currentTileIndex == MyPath.Count - 1)
                    MyChick.Move(MyChick.DirectionToCurrentTile());

                // go to next tile 
                MyChick.GotoNextTile();

                // update the previous distance
                previousDistance = Mathf.Infinity;

                // stop this update
                return;
            }

            // update the previous distance
            previousDistance = currentDistance;

            // make chick walk towards current tile
            MyChick.Move(MyChick.DirectionToCurrentTile());
        }
    }

    internal override void Exit()
    {
        // stop moving
        MyChick.Move(Vector3.zero);
    }
}
