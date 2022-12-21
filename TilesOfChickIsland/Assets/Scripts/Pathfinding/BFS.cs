using UnityEngine;

[CreateAssetMenu(menuName = "Search Algorithms/BFS")]
public class BFS : SearchAlgorithm
{
    protected override void UpdateTileCosts(Tile current, Tile next)
    {
        // TODO: assign correct gCost and hCost
        next.gCost = 0;
        next.hCost = 0;
    }
}
