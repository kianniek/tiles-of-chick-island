using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chick : MonoBehaviour
{
    // component references
    [SerializeField] private Animator animator;

    // movement variables
    [SerializeField] private float walkSpeed = 1f;
    [SerializeField] private float runSpeed = 2f;
    [SerializeField] private float maxRunCost = 10f;
    internal float CurrentSpeed { get { return running ? runSpeed : walkSpeed; } }
    private bool running = false;
    internal Vector3 wantedDirection;
    private Vector3 currentDirection;

    // path variables
    internal List<Tile> path;
    internal int currentTileIndex;
    private Tile currentTile;

    // the fsm of this chick
    private ChickFSM fsm;

    /// <summary>
    /// Initializes the chick, makes it ready for updates.
    /// </summary>
    internal void Initialize()
    {
        // initially chick doesn't have a path
        path = null;
        currentTile = null;

        // no initial velocities
        wantedDirection = currentDirection = Vector3.zero;

        // setup the state machine
        fsm = new ChickFSM();
        fsm.Initialize(this);
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    private void Update()
    {
        // update the state machine
        fsm.Update();
    }

    /// <summary>
    /// Sets the path and starts following it.
    /// </summary>
    /// <param name="path">The path to follow</param>
    internal void FollowPath(List<Tile> path)
    {
        // remember the path
        this.path = path;

        // goto the follow path state
        fsm.GotoState(fsm.followPathState);
    }

    /// <summary>
    /// Resets the path.
    /// </summary>
    internal void ResetPath()
    {
        // reset the path to null
        path = null;
        currentTile = null;

        // stop moving
        Move(Vector3.zero);

        // go back to the idle state
        fsm.GotoState(fsm.idleState);
    }

    /// <summary>
    /// Call to make the chick move in given direction.
    /// </summary>
    /// <param name="direction">Direction to move in</param>
    internal void Move(Vector3 direction)
    {
        // determine the current tile
        currentTile = GameManager.instance.tileMap.GetTileAtWithRaycast(transform);

        // determine whether to move or run based on
        // the tile the chick is currently on and its cost
        running = currentTile == null ? false : currentTile.cost <= maxRunCost;

        // stop moving if no direction is provided
        if (direction == Vector3.zero)
        {
            animator.SetBool("Run", false);
            animator.SetBool("Walk", false);
            return;
        }

        // make sure the direction is normalized
        direction.Normalize();

        // determine wanted velocity
        wantedDirection = direction * Time.deltaTime;

        // smooth the wanted velocity to get the velocity we'll actually apply
        Vector3 smoothVelocity = Vector3.zero;
        currentDirection = Vector3.SmoothDamp(currentDirection, wantedDirection, ref smoothVelocity, 0.03f);

        // move in the direction of the current velocity
        transform.position += currentDirection * CurrentSpeed;

        // look in that direction as well
        transform.LookAt(transform.position + currentDirection);

        // set the animator variables
        animator.SetBool("Walk", !running);
        animator.SetBool("Run", running);
    }

    /// <summary>
    /// Get the distance to the current tile.
    /// </summary>
    /// <returns>The distance</returns>
    internal float DistanceToCurrentTile()
    {
        // get the positions
        Vector3 tilePos = path[currentTileIndex].transform.position;
        Vector3 chickPos = transform.position;

        // ignore y axis
        tilePos.y = chickPos.y = 0;

        // return the length of the vector from tile to chick
        return (tilePos - chickPos).magnitude;
    }

    /// <summary>
    /// Get the direction to the current tile.
    /// </summary>
    /// <returns>The normalized direction</returns>
    internal Vector3 DirectionToCurrentTile()
    {
        // get the positions
        Vector3 tilePos = path[currentTileIndex].transform.position;
        Vector3 chickPos = transform.position;

        // ignore y axis
        tilePos.y = chickPos.y = 0;

        // return the normalized vector from chick to tile
        // to get the direction
        return (tilePos - chickPos).normalized;
    }

    /// <summary>
    /// Iterates to the next tile. 
    /// </summary>
    internal void GotoNextTile()
    {
        // increase the tile index
        currentTileIndex++;

        // if we reached the end of the path
        // we're done following the path, goto eat seeds state
        if (currentTileIndex >= path.Count)
        {
            fsm.GotoState(((ChickFSM)fsm).eatSeedsState);
            return;
        }
    }

    /// <summary>
    /// Call to trigger the turn head animation.
    /// </summary>
    internal void TurnHead()
    {
        animator.SetTrigger("TurnHead");
    }

    /// <summary>
    /// Call to turn the eat animation on/off.
    /// </summary>
    /// <param name="on">Whether to turn the animation on or off</param>
    internal void Eat(bool on)
    {
        animator.SetBool("Eat", on);
    }
}
