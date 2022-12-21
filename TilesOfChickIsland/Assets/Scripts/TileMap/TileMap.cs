using PhantomGrammar.GrammarCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileMap : MonoBehaviour
{
    // width and height of current map
    internal int width { get; private set; }
    internal int height { get; private set; }

    // all tiles in this map,
    // can be null if there is no tile at that coordinate
    private Tile[,] tiles;

    // the start and end tile on this map
    internal Tile startTile { get; private set; }
    internal Tile endTile { get; private set; }

    // the center of the map
    internal Vector3 centerMap { get; private set; }

    /// <summary>
    /// Creates the map from expression.
    /// </summary>
    /// <param name="lvlExpressionFile">The level to load</param>
    /// <returns>True if the level was loaded succesfully</returns>
    internal bool CreateTileMap(string lvlExpressionFile)
    {
        // load expression from file
        Expression lvlExpression = new Expression(null);
        lvlExpression.OpenFile("Assets/Resources/Levels/Expressions/" + lvlExpressionFile + ".xpr");

        // setup the new map, setting width, height,
        // the tiles array and the center of the map
        SetupNewTileMap(lvlExpression.Width, lvlExpression.Height);

        // loop over all pixels in the map expression
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // get the symbol at this position
                Symbol symbol = lvlExpression.Symbols[x + y * lvlExpression.Width];

                // define the tile and optional decoration
                Symbol tileSymbol = symbol.Container == null ? symbol : symbol.Container;
                Symbol decorationSymbol = symbol.Container == null ? null : symbol;

                // create the tile
                Tile tile = CreateTile(x, y, ToEnum<TileType>(tileSymbol.Label, true, TileType.Water), 
                    tileSymbol.GetMemberValue<bool>("start", false), tileSymbol.GetMemberValue<bool>("end", false));

                // tile can be null, e.g. if it's a water tile
                // in that case continue
                if (tile == null)
                    continue;

                // if there is a decoration symbol, resolve it
                if (decorationSymbol != null)
                    CreateDecoration(tile, ToEnum<DecorationType>(decorationSymbol.Label, true, DecorationType.None));
            }
        }

        // post process the level, 
        // checking for validity, setting neighbours, etc.
        // returns true if succesful
        return PostProcessLevel();
    }

    /// <summary>
    /// Creates the map from bitmaps
    /// </summary>
    /// <param name="lvlBase">The base / tiles of the level</param>
    /// <param name="lvlDecorations">The decorations of the level</param>
    /// <returns>True if the level was loaded succesfully</returns>
    internal bool CreateTileMap(Texture2D lvlBase, Texture2D lvlDecorations)
    {
        // setup the new map, setting width, height,
        // the tiles array and the center of the map
        SetupNewTileMap(lvlBase.width, lvlBase.height);

        // loop over all pixels in the map image
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // get the colors at this position, 
                // which represents the tile, special tile and decoration type
                Color tileColor = lvlBase.GetPixel(x, y);
                Color decorationColor = lvlDecorations.GetPixel(x, y);

                // and get the types based on those colors
                TileType tileType = GetTileTypeForColor(tileColor);
                DecorationType decorationType = GetDecorationTypeForColor(decorationColor);
                SpecialTileType specialTileType = GetSpecialTileTypeForColor(decorationColor);

                // create the tile
                Tile tile = CreateTile(x, y, tileType,
                    specialTileType == SpecialTileType.Start, specialTileType == SpecialTileType.End);

                // tile can be null, e.g. if it's a water tile
                // in that case continue
                if (tile == null)
                    continue;

                // if there is a decoration color, resolve it
                if (decorationColor != Color.white && decorationType != DecorationType.None)
                    CreateDecoration(tile, decorationType);
            }
        }

        // post process the level, 
        // checking for validity, setting neighbours, etc.
        // returns true if succesful
        return PostProcessLevel();
    }

    /// <summary>
    /// Sets up for a new tile map with given width and height.
    /// Creates the tile array and determines the center of the map.
    /// </summary>
    /// <param name="width">The width of the level</param>
    /// <param name="height">The height of the level</param>
    private void SetupNewTileMap(int width, int height)
    {
        // get the width and height
        this.width = width;
        this.height = height;

        // determine the center of the map
        centerMap = new Vector3(width * 0.5f * GameManager.instance.mapScale, 0, height * 0.5f * GameManager.instance.mapScale);

        // create the 2D array with the same size as the expression
        tiles = new Tile[width, height];
    }

    /// <summary>
    /// Creates a tile.
    /// </summary>
    /// <param name="x">The x position of the tile</param>
    /// <param name="y">The y position of the tile</param>
    /// <param name="tileType">The tile type of the tile</param>
    /// <param name="isStart">Whether this tile is the start tile</param>
    /// <param name="isEnd">Whether this tile is the end tile</param>
    /// <returns>The newly created tile or null on failure</returns>
    private Tile CreateTile(int x, int y, TileType tileType, bool isStart = false, bool isEnd = false) 
    {
        // water doesn't spawn a tile, 
        // there is already a plane representing water
        // also none doesn't spawn a tile
        // so nothing else to do for this tile
        if (tileType == TileType.Water || tileType == TileType.None)
            return null;

        // instantiate a new tile object
        Tile tile = Instantiate(GameManager.instance.tilePrefab, transform).GetComponent<Tile>();

        // give it a recognizable name
        tile.name = string.Format("Tile[{0},{1}]", x, y);

        // position tile
        tile.transform.localPosition = new Vector3(x * GameManager.instance.mapScale, 0, y * GameManager.instance.mapScale);

        // initialize it with given type
        tile.Initialize(tileType);

        // resolve start and end tiles
        if (isStart)
            startTile = tile;
        else if (isEnd)
        {
            endTile = tile;

            // instantiate the end object (an arrow) on the end tile
            GameObject endObject = Instantiate(GameManager.instance.endPrefab, transform);
            endTile.AddDecoration(endObject, true);
        }

        // set tile in tiles array
        tiles[x, y] = tile;

        // return the newly created tile
        return tile;
    }

    /// <summary>
    /// Creates a decoration.
    /// </summary>
    /// <param name="onTile">The tile on which the decoration is to be placed</param>
    /// <param name="decorationType">The type of decoration</param>
    private void CreateDecoration(Tile onTile, DecorationType decorationType)
    {
        // don't spawn any on start / end
        if (onTile == startTile || onTile == endTile)
            return;

        // determine whether we can walk through this type
        bool canWalkThrough = decorationType == DecorationType.None ||
                              decorationType == DecorationType.Plant ||
                              decorationType == DecorationType.SmallRock;

        // get an object that matches this type of decoration
        // and instantiate it
        GameObject decorationObject = GameObject.Instantiate(GetRandomDecorationForType(decorationType), transform);

        // add the decoration to the tile, positions it as well
        onTile.AddDecoration(decorationObject, canWalkThrough);
    }

    /// <summary>
    /// Call to post process the newly created level.
    /// Checking validity of the level, setting neighbours, etc.
    /// </summary>
    /// <returns>True if the level was succesfully processed</returns>
    private bool PostProcessLevel()
    {
        // check if the map is valid
        if (!IsMapValid())
            return false;

        // find all neighbours and set them
        ConnectNeighbours();

        // determine the height for each tile
        // since heights are set based on neighbours
        // we couldn't do this earlier
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null)
                    tiles[x, y].DetermineHeight();
            }
        }

        // position all decorations now that tiles know there heights
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null)
                    tiles[x, y].PositionDecorations();
            }
        }

        // make sure visuals and variables are cleared
        ClearTilesForPathFinding();
        ClearTilesDebugVisual();

        // valid map build, return true
        return true;
    }

    /// <summary>
    /// Determines whether the current map is valid
    /// </summary>
    /// <returns>True if the map is valid</returns>
    private bool IsMapValid()
    {
        // not a valid map if we don't have a start and end tile
        if (startTile == null || endTile == null)
        {
            Debug.LogError("Not a valid map!");
            return false;
        }

        // valid map, return true
        return true;
    }

    /// <summary>
    /// Connects neighbours by finding all neighbours for each tile
    /// and setting them.
    /// </summary>
    private void ConnectNeighbours()
    {
        // 8-way connected map
        // find neighbours for all tiles
        Tile currentTile = null;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                currentTile = tiles[x, y];

                // skip over tiles that are null
                if (currentTile == null)
                    continue;

                if (y > 0 && tiles[x, y - 1] != null)                       // up
                    currentTile.neighbours.Add(tiles[x, y - 1]);

                if (y < height - 1 && tiles[x, y + 1] != null)              // down
                    currentTile.neighbours.Add(tiles[x, y + 1]);

                if (x > 0 && tiles[x - 1, y] != null)                       // left
                    currentTile.neighbours.Add(tiles[x - 1, y]);

                if (x < width - 1 && tiles[x + 1, y] != null)               // right
                    currentTile.neighbours.Add(tiles[x + 1, y]);

                if (x > 0 && y > 0 && tiles[x - 1, y - 1] != null)                      // diagonal left-up
                    currentTile.neighbours.Add(tiles[x - 1, y - 1]);

                if (x > 0 && y < height - 1 && tiles[x - 1, y + 1] != null)             // diagonal left-down
                    currentTile.neighbours.Add(tiles[x - 1, y + 1]);

                if (x < width - 1 && y > 0 && tiles[x + 1, y - 1] != null)              // diagonal right-up
                    currentTile.neighbours.Add(tiles[x + 1, y - 1]);

                if (x < width - 1 && y < height - 1 && tiles[x + 1, y + 1] != null)     // diagonal right-down
                    currentTile.neighbours.Add(tiles[x + 1, y + 1]);
            }
        }
    }

    /// <summary>
    /// Clears all pathfinding variables for finding a new path.
    /// </summary>
    internal void ClearTilesForPathFinding()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // skip over tiles that are null
                if (tiles[x, y] == null)
                    continue;

                tiles[x, y].ClearForPathfinding();
            }
        }
    }

    /// <summary>
    /// Clears the debug visuals on all tiles. 
    /// </summary>
    internal void ClearTilesDebugVisual()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // skip over tiles that are null
                if (tiles[x, y] == null)
                    continue;

                tiles[x, y].SetDebugVisual(false, Color.white);
            }
        }
    }

    /// <summary>
    /// Get the tile at coordinates.
    /// </summary>
    /// <param name="x">X coordinate of tile</param>
    /// <param name="y">Y coordinate of tile</param>
    /// <returns>Tile at coordinates if it exists</returns>
    internal Tile GetTileAt(int x, int y)
    {
        // return null if out of bounds
        if (x < 0 || x >= width ||
            y < 0 || y >= height)
            return null;

        // return the tile 
        return tiles[x, y];
    }

    /// <summary>
    /// Get the tile at given position.
    /// </summary>
    /// <param name="position">The position to find the closest tile for</param>
    /// <returns>The closest tile if it exists</returns>
    internal Tile GetTileAt(Vector3 position)
    {
        // divide the position by mapscale
        // to get them in the same space
        position /= GameManager.instance.mapScale;

        // clamp the position
        position.x = Mathf.Clamp(position.x, 0, width);
        position.y = 0;
        position.z = Mathf.Clamp(position.z, 0, height);

        // return the tile at the rounded position
        return tiles[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)];
    }

    /// <summary>
    /// Get the tile at given transforms position with a raycast.
    /// Is more accurate than the GetTileAt method.
    /// </summary>
    /// <param name="transform">The positions transform is used to get the tile</param>
    /// <returns>The tile at the transforms position</returns>
    internal Tile GetTileAtWithRaycast(Transform transform)
    {
        // declare the raycast hit to pass 
        RaycastHit hit;

        // do the raycast and either return the tile component or null
        if (Physics.Raycast(transform.position + transform.up, -transform.up, out hit, Mathf.Infinity, 1 << 6))
            return hit.collider.gameObject.GetComponent<Tile>();
        else
            return null;
    }

    /// <summary>
    /// Call to get the Manhattan distance between two tiles.
    /// </summary>
    /// <param name="tile1">The tile from</param>
    /// <param name="tile2">The tile to</param>
    /// <returns>Returns the Manhattan distance between the two tiles</returns>
    internal float GetManhattanDistance(Tile tile1, Tile tile2)
    {
        // calculate the minimal distance walking horizontally / vertically and diagonally
        float distanceX = Mathf.Abs(tile1.transform.position.x - tile2.transform.position.x);
        float distanceY = Mathf.Abs(tile1.transform.position.z - tile2.transform.position.z);
        float distance;

        if (distanceX >= distanceY)
            distance = (distanceX - distanceY) + distanceY * 1.4f;
        else
            distance = (distanceY - distanceX) + distanceX * 1.4f;

        // return the heuristic
        return distance * 10;
    }

    /// <summary>
    /// Get the requested amount of free tiles around the start tile. 
    /// </summary>
    /// <param name="startTile">The tile to start from</param>
    /// <param name="amountOfRequestedNeighbours">The amount of requested neighbours</param>
    /// <returns>List with free tiles around the start tile</returns>
    internal List<Tile> GetRandomFreeNeighbours(Tile startTile, int amountOfRequestedNeighbours)
    {
        // declare list to return
        List<Tile> returnTiles = new List<Tile>();
        
        // get all free neighbour tiles
        List<Tile> freeNeighbourTiles = startTile.neighbours.FindAll(t => t.canEnter);

        // as long as we have new neighbours to look at, 
        // and we have not found the amount of neighbours requested 
        while(freeNeighbourTiles.Count > 0 && amountOfRequestedNeighbours > 0)
        {
            // do we have enough free neighbours already?
            if (freeNeighbourTiles.Count > amountOfRequestedNeighbours)
            {
                // for as many neighbours as are requested
                for (int i = 0; i < amountOfRequestedNeighbours; i++)
                {
                    // get a random tile,
                    // add it to the return tiles,
                    // remove it from the options 
                    returnTiles.Add(freeNeighbourTiles[UnityEngine.Random.Range(0, freeNeighbourTiles.Count - 1)]);
                    freeNeighbourTiles.Remove(returnTiles[i]);
                }

                // no new neighbours to find
                amountOfRequestedNeighbours = 0;
            }
            else
            {
                // add the tiles that are free to the to be returned list
                for (int i = 0; i < freeNeighbourTiles.Count; i++)
                {
                    returnTiles.Add(freeNeighbourTiles[i]);
                    freeNeighbourTiles.Remove(returnTiles[i]);
                }

                // not that much neighbours required anymore
                amountOfRequestedNeighbours -= freeNeighbourTiles.Count;

                // look through all tiles we currently have found
                // and add their neighbours that aren't found yet and can be entered
                for (int i = 0; i < returnTiles.Count; i++)
                {
                    freeNeighbourTiles.AddRange(returnTiles[i].neighbours.FindAll
                        (t => t.canEnter && !returnTiles.Contains(t)));
                }
            }
        }

        // return the found tiles
        return returnTiles;
    }

    /// <summary>
    /// Call to get the tile type for a given color.
    /// </summary>
    /// <param name="color">The color to get the tile type for</param>
    /// <returns>The tile type for the given color</returns>
    internal TileType GetTileTypeForColor(Color color)
    {
        return GameManager.instance.tileColors.Find(t => t.color.IsSimilarTo(color, 0.1f)).type;
    }

    /// <summary>
    /// Call to get the decoration type for a given color.
    /// </summary>
    /// <param name="color">The color to get the decoration type for</param>
    /// <returns>The decoration type for the given color</returns>
    internal DecorationType GetDecorationTypeForColor(Color color)
    {
        return GameManager.instance.decorationColors.Find(t => t.color.IsSimilarTo(color, 0.1f)).type;
    }

    /// <summary>
    /// Call to get the special tile type for a given color.
    /// </summary>
    /// <param name="color">The color to get the special tile type for</param>
    /// <returns>The special tile type for the given color</returns>
    internal SpecialTileType GetSpecialTileTypeForColor(Color color)
    {
        return GameManager.instance.specialTileColors.Find(t => t.color.IsSimilarTo(color, 0.1f)).type;
    }

    /// <summary>
    /// Call to get the material for a given tile type.
    /// </summary>
    /// <param name="tileType">The tile type to get the material for</param>
    /// <returns>The material for the given tile type</returns>
    internal Material GetMaterialForTileType(TileType tileType)
    {
        return GameManager.instance.tileMaterials.Find(t => t.type == tileType).material;
    }

    /// <summary>
    /// Call to get the cost for a given tile type.
    /// </summary>
    /// <param name="tileType">The tile type to get the cost for</param>
    /// <returns>The cost for the given tile type</returns>
    internal int GetCostForTileType(TileType tileType)
    {
        return GameManager.instance.tileCosts.Find(t => t.type == tileType).cost;
    }

    /// <summary>
    /// Call to get the possible decorations for a given decoration type.
    /// </summary>
    /// <param name="decorationType">The decoration type to get the possible decorations for</param>
    /// <returns>The possible decorations for the given decoration type</returns>
    private List<GameObject> GetDecorationsForType(DecorationType decorationType)
    {
        return GameManager.instance.decorationTypeObjects.Find(d => d.type == decorationType).objects;
    }

    /// <summary>
    /// Call to get a random valid decorations for a given decoration type.
    /// </summary>
    /// <param name="decorationType">The decoration type to get a random valid decoration for</param>
    /// <returns>A random valid decorations for the given decoration type</returns>
    private GameObject GetRandomDecorationForType(DecorationType decorationType)
    {
        List<GameObject> gos = GetDecorationsForType(decorationType);
        return gos[UnityEngine.Random.Range(0, gos.Count - 1)];
    }

    /// <summary>
    /// Parses a string to its enum value.
    /// </summary>
    /// <typeparam name="TEnum">The type of enum</typeparam>
    /// <param name="strEnumValue">The string to parse</param>
    /// <param name="ignoreCase">Whether to ignore cases</param>
    /// <param name="defaultValue">The value to default to</param>
    /// <returns>The enum matching to the given string</returns>
    internal TEnum ToEnum<TEnum>(string strEnumValue, bool ignoreCase, TEnum defaultValue)
    {
        var values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        if (ignoreCase)
        {
            string compareTo = strEnumValue.ToLower();

            foreach (TEnum e in values)
            {
                if (compareTo == e.ToString().ToLower())
                    return e;
            }
        }
        else
        {
            foreach (TEnum e in values)
            {
                if (strEnumValue == e.ToString())
                    return e;
            }
        }

        return defaultValue;
    }
}
