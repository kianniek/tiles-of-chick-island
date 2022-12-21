using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SearchAlgorithm : ScriptableObject
{
    // all tiles currently open for evaluation
    private List<Tile> open = new List<Tile>();

    // all tiles previously evaluated
    private List<Tile> closed = new List<Tile>();

    // the path found 
    private List<Tile> path = new List<Tile>();

    // start and end node 
    protected Tile start;
    protected Tile end;

    // currently evaluated tile
    private Tile current;

    // time we started searching
    private float startTime;

    internal delegate void OnFinishFindPath(SearchResult searchResult);

    /// <summary>
    /// Finds a path between the start and end tile if it exists.
    /// </summary>
    /// <param name="start">Start tile</param>
    /// <param name="end">End tile</param>
    /// <param name="onFinishFindPath">Method to call when finished</param>
    internal void FindPath(Tile start, Tile end, OnFinishFindPath onFinishFindPath)
    {
        // set the start time to keep track of time 
        // it takes to find a path;
        startTime = Time.realtimeSinceStartup;

        // if the end tile cannot be entered
        // this cannot be a valid path
        if (!end.canEnter)
        {
            // so don't bother and return and empty search result
            onFinishFindPath(new SearchResult(name, null, start, end, 0, 0, 0, 0));
            return;
        }

        // clears lists and tiles for finding new path
        ClearForPathfinding();

        // keep track of the start and end node
        this.start = start;
        this.end = end;

        // sets start tiles costs and 
        // puts it in the open list as first tile to evaluate
        PrepareStartTile();

        // as long as we have nodes to evaluate, 
        // keep looking for the solution
        while (open.Count > 0)
        {
            // pick the best possible one to analyze
            // and make it the new current tile
            current = PickBestFromOpen();

            // the current node is analyzed, 
            // so move it to closed
            closed.Add(current);
            open.Remove(current);

            // found goal tile?
            if (current == end)
            {
                // traces back and sets path
                TracePath(current);

                // done, create the search result and return
                onFinishFindPath(new SearchResult(name, path, start, end, end.gCost, path.Count, closed.Count, Time.realtimeSinceStartup - startTime));
                return;
            }

            // adds neighbours to the open list if required
            // and updates their costs 
            ProcessCurrentNeighbours();
        }

        // failed making a path, 
        // return an empty search result
        onFinishFindPath(new SearchResult(name, null, start, end, 0, 0, 0, Time.realtimeSinceStartup - startTime));
        return;
    }

    /// <summary>
    /// Finds a path over time between the start and end tile if it exists.
    /// </summary>
    /// <param name="start">Start tile</param>
    /// <param name="end">End tile</param>
    /// <param name="searchResult">The search result</param>
    /// <returns></returns>
    internal IEnumerator FindPathOverTime(Tile start, Tile end, OnFinishFindPath onFinishFindPath)
    {
        // set the start time to keep track of time 
        // it takes to find a path;
        startTime = Time.realtimeSinceStartup;

        // if the end tile cannot be entered
        // this cannot be a valid path
        if (!end.canEnter)
        {
            // so don't bother and give back an empty search result
            onFinishFindPath(new SearchResult(name, null, start, end, 0, 0, 0, 0));
            yield return null;
        }
        else
        {
            // clears lists and tiles for finding new path
            ClearForPathfinding();

            // keep track of the start and end node
            this.start = start;
            this.end = end;

            // sets start tiles costs and 
            // puts it in the open list as first tile to evaluate
            PrepareStartTile();

            // if no path is found, empty result needs
            // to be given back at the end so keep track of that
            bool foundPath = false;

            // as long as we have nodes to evaluate or we don't have a path, 
            // keep looking for the solution
            while (open.Count > 0 && !foundPath)
            {
                // pick the best possible one to analyze
                // and make it the new current tile
                current = PickBestFromOpen();

                // found goal tile?
                if (current == end)
                {
                    // traces back and sets path
                    TracePath(current);

                    // done, give back the succesfull search result
                    foundPath = true;
                    onFinishFindPath(new SearchResult(name, path, start, end, end.gCost, path.Count, closed.Count, Time.realtimeSinceStartup - startTime));
                }
                else
                {
                    // the current node is analyzed, 
                    // so move it to closed
                    closed.Add(current);
                    open.Remove(current);

                    // adds neighbours to the open list if required
                    // and updates their costs 
                    ProcessCurrentNeighbours();

                    // update visuals for debugging
                    VisualizeCurrentSearchIteration();

                    yield return new WaitForSeconds(1f / GameManager.instance.visualizationSpeed);
                }
            }

            // failed making a path, 
            // give back an empty search result
            if (!foundPath)
            {
                onFinishFindPath(new SearchResult(name, null, start, end, 0, 0, 0, Time.realtimeSinceStartup - startTime));
            }

            yield return null;
        }
    }

    /// <summary>
    /// Clears all lists and the tile map 
    /// to ready it for finding a new path.
    /// </summary>
    private void ClearForPathfinding()
    {
        // no start and end anymore
        start = null;
        end = null;

        // clear all lists for start
        open.Clear();
        closed.Clear();
        path.Clear();

        // reset all tiles for pathfinding
        GameManager.instance.tileMap.ClearTilesForPathFinding();
    }

    /// <summary>
    /// Prepares the start tile by setting costs
    /// and adding it to the open list.
    /// </summary>
    private void PrepareStartTile()
    {
        // add from tile to open, 
        // since it's the first tile to analyse
        open.Add(start);

        // set the path values for the start tile
        UpdateTileCosts(start, start);
    }

    /// <summary>
    /// Processes all neighbours of the current node.
    /// Can add them to open and / or update their costs.
    /// </summary>
    private void ProcessCurrentNeighbours()
    {
        // add all neighbouring nodes to the open collection
        // and calculate their costs
        foreach (Tile t in current.neighbours)
        {
            // skip over if this node is already analyzed
            if (closed.Contains(t))
                continue;

            // skip over neighbours that cannot be entered
            if (!t.canEnter)
                continue;

            // calculate cost to get to this tile from the current
            float newGCost = current.gCost + CalculateCostToEnterTile(current, t);

            // if this is a shorter path to this neighbour
            // or if this tile hasn't been evaluated yet
            if (newGCost < t.gCost || !open.Contains(t))
            {
                // set current as the tile before the neighbour in the path
                t.beforeInPath = current;

                UpdateTileCosts(current, t);
            }

            // add tile to the open collection
            if (!open.Contains(t))
                open.Add(t);
        }
    }

    /// <summary>
    /// Calculates the cost to for the next tile 
    /// given it's entered from the current tile.
    /// </summary>
    /// <param name="current">The current tile, the tile from which the next is entered</param>
    /// <param name="next">The tile that is entered</param>
    protected abstract void UpdateTileCosts(Tile current, Tile next);

    /// <summary>
    /// Picks the best tile to evaluate according 
    /// to the current heuristic and the open list.
    /// </summary>
    /// <returns>The best tile to evaluate</returns>
    protected Tile PickBestFromOpen()
    {
        // initially best tile is null
        Tile best = null;

        // go over all tiles in open
        for (int i = 0; i < open.Count; i++)
        {
            // if there is no best yet or
            // if this one is better, 
            // update the best tile
            if (best == null || open[i].fCost < best.fCost)
                best = open[i];
        }

        // return resulting best tile
        return best;
    }

    /// <summary>
    /// Traces back the path starting at the end tile.
    /// </summary>
    /// <param name="tile">The given end tile</param>
    private void TracePath(Tile tile)
    {
        // as long as there is a tile in the path before this one
        // recursively call this method and keep adding this one
        if (tile.beforeInPath == null)
        {
            path.Add(tile);
            return;
        }
        else
        {
            TracePath(tile.beforeInPath);
            path.Add(tile);
        }
    }

    /// <summary>
    /// Shows the debug visualization on current open and closed list.
    /// </summary>
    private void VisualizeCurrentSearchIteration()
    {
        // clear all current visualizations
        GameManager.instance.tileMap.ClearTilesDebugVisual();

        // set all open tiles to show a green debug visual
        open.ForEach(t => t.SetDebugVisual(true, GameManager.instance.openColor));

        // set all closed tiles to show a grey debug visual
        closed.ForEach(t => t.SetDebugVisual(true, GameManager.instance.closedColor));
    }

    /// <summary>
    /// Calculates the cost to enter the next tile from the current.
    /// </summary>
    /// <param name="current">The current tile</param>
    /// <param name="next">The next tile to go to</param>
    /// <returns>The cost to enter the next tile</returns>
    protected float CalculateCostToEnterTile(Tile current, Tile next)
    {
        // if we can't enter the next tile, 
        // return infinite
        if (!next.canEnter)
            return Mathf.Infinity;

        // cost is the cost to enter the next tile
        float cost = next.cost;

        // add extra cost to diagonals, looks better
        if (current.transform.position.x != next.transform.position.x &&
            current.transform.position.z != next.transform.position.z)
            cost *= 1.4f;

        // return calculated cost
        return cost;
    }
}

/// <summary>
/// Stores information about the pathfinding results.
/// </summary>
public class SearchResult 
{
    public string algorithmName;

    public List<Tile> path;

    public Tile start;
    public Tile end;

    public float pathCost;

    public int tilesInPath;
    public int tilesEvaluated;

    public float time;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="algorithmName">Name of the algorithm</param>
    /// <param name="path">Path that has been found</param>
    /// <param name="start">The start of the path</param>
    /// <param name="end">The end of the path</param>
    /// <param name="pathCost">The total cost of the path</param>
    /// <param name="tilesInPath">The amount of tiles the path consists of</param>
    /// <param name="tilesEvaluated">The amount of tiles evaluated</param>
    /// <param name="time">The time it took to find the path</param>
    internal SearchResult(string algorithmName, List<Tile> path, Tile start, Tile end, float pathCost, int tilesInPath, int tilesEvaluated, float time)
    {
        this.algorithmName = algorithmName;
        this.path = path;
        this.start = start;
        this.end = end;
        this.pathCost = pathCost;
        this.tilesInPath = tilesInPath;
        this.tilesEvaluated = tilesEvaluated;
        this.time = time;
    }
}