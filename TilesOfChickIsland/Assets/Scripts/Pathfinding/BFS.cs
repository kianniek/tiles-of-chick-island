using UnityEngine;

[CreateAssetMenu(menuName = "Search Algorithms/BFS")]
public class BFS : SearchAlgorithm
{
    protected override void UpdateTileCosts(Tile current, Tile next)
    {
        next.gCost = 0.0f;
        //next.hCost = GameManager.instance.tileMap.GetManhattanDistance(next, end);
        next.hCost = GameManager.instance.tileMap.GetEuclideanDistance(next, end);
    }
}