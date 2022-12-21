using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhantomGrammar.GrammarCore;

public class GameManager : MonoBehaviour
{
    // scene references
    [SerializeField] internal TileMap tileMap;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private new Light light;
    [SerializeField] private SettingsUI settingsUI;

    // prefab references
    [SerializeField] internal GameObject tilePrefab;
    [SerializeField] internal GameObject endPrefab;
    [SerializeField] private GameObject chickPrefab;
    internal Chick chick;

    // tilemap settings
    [SerializeField] internal float mapScale = 1f;
    [SerializeField] internal float minMountainHeight = 2f;
    [SerializeField] internal float maxMountainHeight = 5f;
    [SerializeField] internal List<TileColorPair> tileColors;
    [SerializeField] internal List<DecorationColorPair> decorationColors;
    [SerializeField] internal List<SpecialTileColorPair> specialTileColors;
    [SerializeField] internal List<TileMaterialPair> tileMaterials;
    [SerializeField] internal List<TileCostPair> tileCosts;
    [SerializeField] internal List<DecorationTypeObjectsPair> decorationTypeObjects;

    // level to load
    [SerializeField] private LoadingType loadingType = LoadingType.Bitmap;
    [SerializeField] private string lvlExpressionFileName;
    [SerializeField] private Texture2D lvlBaseBitmap;
    [SerializeField] private Texture2D lvlDecorationsBitmap;
    public enum LoadingType { Bitmap, Expression }

    // the possible algorithms to use in pathfinding
    [SerializeField] internal List<SearchAlgorithm> availableSearchAlgorithms;
    [SerializeField] private SearchAlgorithm currentSearchAlgorithm;

    // helper methods for search algorithms, used in editor script
    public int AmountOfSearchAlgorithms() { return availableSearchAlgorithms.Count; }
    public SearchAlgorithm GetSearchAlgorithm(int i) { return availableSearchAlgorithms[i]; }
    public int GetIndexCurrentSearchAlgorithm() { return currentSearchAlgorithm == null ? -1 : availableSearchAlgorithms.IndexOf(currentSearchAlgorithm); }
    
    // visualization pathfinding variables
    [SerializeField] public bool visualizeSearch;
    [SerializeField] internal float visualizationSpeed;

    // colors for debug visuals
    [SerializeField] internal Color pathColor;
    [SerializeField] internal Color openColor;
    [SerializeField] internal Color closedColor;

    // the results from finding a path
    [HideInInspector] public SearchResult searchResult;
    public bool IsSearching { get; private set; }
    public bool HasPath { get { return searchResult != null && searchResult.path != null; } }

    // semi singleton
    internal static GameManager instance;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        // setup singleton
        instance = this;

        // initialize setting ui
        settingsUI.Initialize();

        // settings ui is only used in a build
#if UNITY_EDITOR
        settingsUI.gameObject.SetActive(false);
#else
        settingsUI.gameObject.SetActive(true);
#endif

        // set global shader vars
        Shader.SetGlobalColor("_LightColor", light.color);
        Shader.SetGlobalColor("_BackgroundColor", Camera.main.backgroundColor);

        // init post processing effect
        Camera.main.GetComponent<PostProcessingEffect>().Initialize();

        // load the level 
        LoadLevel();

        // not searching rn
        IsSearching = false;
    }

    /// <summary>
    /// Call to load the level set in the inspector.
    /// </summary>
    private void LoadLevel()
    {
        bool loadSuccesful = false;

        switch (loadingType)
        {
            case LoadingType.Bitmap:
                // cannot load the level if it's null
                if (lvlBaseBitmap == null || lvlDecorationsBitmap == null)
                    return;

                // load the level
                loadSuccesful = tileMap.CreateTileMap(lvlBaseBitmap, lvlDecorationsBitmap);
                break;

            case LoadingType.Expression:
                // cannot load the level if it's empty
                if (lvlExpressionFileName == null || lvlExpressionFileName == string.Empty)
                    return;

                // load the level
                loadSuccesful = tileMap.CreateTileMap(lvlExpressionFileName);
                break;
        }

        if (loadSuccesful)
        {
            // center camera on map
            cameraController.CenterOnTileMap();

            // spawn the chick at start
            chick = Instantiate(chickPrefab).GetComponent<Chick>();
            chick.transform.position = tileMap.startTile.transform.position;
            chick.Initialize();
        }
    }

    /// <summary>
    /// Call to set the current search algorithm.
    /// </summary>
    /// <param name="newSearchAlgorithm">The new search algorithm</param>
    internal void SetCurrentSearchAlgorithm(SearchAlgorithm newSearchAlgorithm)
    {
        currentSearchAlgorithm = newSearchAlgorithm;
    }

    /// <summary>
    /// Called from UI to find a path if it exists with the current algorithm.
    /// </summary>
    public void FindPath()
    {
        // make sure path is reset
        ResetPath();

        // we're searching rn!
        IsSearching = true;

        // if we need to visualize the search, 
        // it's executed over time with intervals
        // else, find path instant
        if (visualizeSearch)
            StartCoroutine(currentSearchAlgorithm.FindPathOverTime(tileMap.startTile, tileMap.endTile, OnFinishPath));
        else
            currentSearchAlgorithm.FindPath(tileMap.startTile, tileMap.endTile, OnFinishPath);
    }

    /// <summary>
    /// Called when the search algorithm has finished.
    /// </summary>
    /// <param name="searchResult"></param>
    internal void OnFinishPath(SearchResult searchResult)
    {
        // save the result
        this.searchResult = searchResult;

        // done searching
        IsSearching = false;

        // throw error and return if no path was found
        if (searchResult.path == null || searchResult.path.Count == 0)
        {
            Debug.LogError("No valid path found!");
            return;
        }

        // draw path if we show debug visuals
        if (visualizeSearch)
            DrawCurrentPath();

        // update interactive state of buttons in setting ui
        settingsUI.SetInteractableStateButtons();
    }

    /// <summary>
    /// Draws the current path with a line renderer.
    /// </summary>
    private void DrawCurrentPath()
    {
        // cant draw the path if it doesn't exist!
        if (searchResult.path == null || searchResult.path.Count <= 0)
            return;

        // set debug visuals for the tiles in the path
        for (int i = 0; i < searchResult.path.Count; i++)
            searchResult.path[i].SetDebugVisual(true, pathColor);
    }

    /// <summary>
    /// Called from UI to make the chick start following the path.
    /// </summary>
    public void FollowPath()
    {
        // give the path to chick and make him follow it
        chick.FollowPath(searchResult.path);
    }

    /// <summary>
    /// Called from UI to reset chick and path.
    /// </summary>
    public void ResetPath()
    {
        // reset the search results
        searchResult = null;

        // dont show debug visuals anymore
        tileMap.ClearTilesDebugVisual();

        // reset the chick
        chick.transform.position = tileMap.startTile.transform.position;
        chick.ResetPath();

        // update interactive state of buttons in setting ui
        settingsUI.SetInteractableStateButtons();
    }
}
