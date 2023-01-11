using UnityEngine;

[CreateAssetMenu(menuName = "Search Algorithms/Dijkstra")]
public class Dijkstra : SearchAlgorithm
{
    protected override void UpdateTileCosts(Tile current, Tile next)
    {
        next.gCost = current.gCost + this.CalculateCostToEnterTile(current, next);
        next.hCost = 0.0f;
    }
}