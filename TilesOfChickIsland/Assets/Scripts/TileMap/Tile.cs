using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public enum TileType { None, Water, Dirt, Grass, Sand, Mountain }
public enum DecorationType { None, Plant, Bush, Tree, BigRock, SmallRock, }
public enum SpecialTileType { None, Start, End }

[Serializable]
public struct TileMaterialPair
{
    public TileType type;
    public Material material;
}

[Serializable]
public struct TileCostPair
{
    public TileType type;
    public int cost;
}

[Serializable]
public struct DecorationTypeObjectsPair
{
    public DecorationType type;
    public List<GameObject> objects;
}

[Serializable]
public struct TileColorPair
{
    public TileType type;
    public Color color;
}

[Serializable]
public struct DecorationColorPair
{
    public DecorationType type;
    public Color color;
}

[Serializable]
public struct SpecialTileColorPair
{
    public SpecialTileType type;
    public Color color;
}

public class Tile : MonoBehaviour
{
    // reference to meshrenderer 
    [SerializeField] private MeshRenderer meshRenderer;
    private Color originalColor;

    // the type of this tile
    TileType type;
    
    // references to all neighbouring tiles
    internal List<Tile> neighbours;

    // whether chick can enter this tile
    internal bool canEnter;

    // cost to enter this tile
    // -1 is infinite
    internal int cost;

    // variables used / changed during pathfinding
    internal float gCost;
    internal float hCost;
    internal float fCost { get { return gCost + hCost; } }
    internal Tile beforeInPath;

    // keep track of decorations added
    private List<GameObject> decorations;

    // height of this tile to place decorations correctly
    private float height;

    /// <summary>
    /// Call to set up this tile.
    /// </summary>
    /// <param name="type">The type of tile</param>
    internal void Initialize(TileType type)
    {
        // remember type
        this.type = type;

        // set material
        meshRenderer.material = GameManager.instance.tileMap.GetMaterialForTileType(type);
        originalColor = meshRenderer.material.color;

        // get cost
        cost = GameManager.instance.tileMap.GetCostForTileType(type);

        // can't enter if cost is -1
        canEnter = cost >= 0;
            
        // start with empty lists for decorations and neighbors
        decorations = new List<GameObject>();
        neighbours = new List<Tile>();
    }

    /// <summary>
    /// Adds and positions decoration on this tile.
    /// </summary>
    /// <param name="go">The new decoration as a GameObject</param>
    /// <param name="canWalkThrough">Whether the chick can walk through this decoration</param>
    internal void AddDecoration(GameObject go, bool canWalkThrough)
    {
        // add it to decorations to keep track of it
        decorations.Add(go);

        // cannot enter this tile if chick
        // cannot walk through this decoration
        if (!canWalkThrough)
            canEnter = false;
    }

    internal void DetermineHeight()
    {
        // height is initially 1
        height = 1;

        // mountains are higher, so set their height
        // nothing to do for the other tile types 
        if (type == TileType.Mountain)
        {
            // the height is based on the percentage of neighbours that are also mountains
            // interpolated between the minimum and maximum height for mountains
            // and floor to the lowest int to get better steps in the mountains
            height = Mathf.Floor(Mathf.Lerp(GameManager.instance.minMountainHeight,
                                                 GameManager.instance.maxMountainHeight,
                                                 (neighbours.FindAll(t => t.type == TileType.Mountain).Count / 8f)));

            // ensure height is at least the min height
            height = Mathf.Max(height, GameManager.instance.minMountainHeight);

            // use the map scale to scale the height
            float mapScale = GameManager.instance.mapScale;

            // set the scaled height and move the mountain up
            transform.localScale = new Vector3(mapScale, height, mapScale);
            transform.localPosition += new Vector3(0, Mathf.Ceil(height - 1f), 0);
        }
    }

    internal void PositionDecorations()
    {
        // position each decoration
        for (int i = 0; i < decorations.Count; i++)
        {
            // randomize the position if it's not the end object
            bool randomize = decorations[i].name != GameManager.instance.endPrefab.name;

            if (randomize)
            {
                // position the object on this tile with a bit of rndm offset
                Vector2 rndmOffset = UnityEngine.Random.insideUnitCircle * 0.3f;
                decorations[i].transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z) + 
                    new Vector3(rndmOffset.x, height - 1, rndmOffset.y);

                // give the object a random y rotation
                decorations[i].transform.localRotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0));

                // give the object a small random change in scale
                float rndmScale = decorations[i].transform.localScale.x + UnityEngine.Random.Range(-0.2f, 0.2f);
                decorations[i].transform.localScale = new Vector3(rndmScale, rndmScale, rndmScale);
            }
            else
            {
                // position the object on this tile 
                decorations[i].transform.localPosition = transform.localPosition;

                // give the object a random y rotation
                decorations[i].transform.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
        }
    }

    /// <summary>
    /// Resets tiles pathfinding variables. 
    /// Readies it for finding new path. 
    /// </summary>
    internal void ClearForPathfinding()
    {
        gCost = 0;
        hCost = 0;
        beforeInPath = null;
    }

    /// <summary>
    /// Call to control the debug visuals.
    /// Used to visualize the open and closed list in pathfinding.
    /// </summary>
    /// <param name="on">Whether the visual should be on or off</param>
    /// <param name="color">The color of the visual</param>
    internal void SetDebugVisual(bool on, Color color)
    {
        meshRenderer.material.SetColor("_SecColor", on ?  color : originalColor);
    }
}
