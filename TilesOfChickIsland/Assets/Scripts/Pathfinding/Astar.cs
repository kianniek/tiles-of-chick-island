using UnityEngine;

[CreateAssetMenu(menuName = "Search Algorithms/A*")]
public class Astar : SearchAlgorithm
{
    protected override void UpdateTileCosts(Tile current, Tile next)
    {
        next.gCost = current.gCost + CalculateCostToEnterTile(current, next);
        next.hCost = GameManager.instance.tileMap.GetManhattanDistance(next, this.end);
    }
}
