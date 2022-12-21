using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    // define the property
    public SerializedProperty
        tileMap,
        cameraController,
        light,
        settingsUI,

        tilePrefab,
        endPrefab,
        chickPrefab,

        searchAlgorithms,
        currentSearchAlgorithm,

        mapScale,
        minMountainHeight,
        maxMountainHeight,
        tileColors,
        decorationColors,
        specialTileColors,
        tileMaterials,
        tileCosts,
        decorationTypeObjects,

        loadingType,
        lvlExpressionFileName,
        lvlBaseBitmap,
        lvlDecorationsBitmap, 
        
        visualizeSearch,
        visualizationSpeed,
        pathColor,
        openColor,
        closedColor;

    private GUIStyle horizontalLineStyle;
    private GUIStyle titleStyle;
    private GUIStyle debugInfoAreaStyle;
    private GUIStyle debugInfoTextStyle;

    /// <summary>
    /// Called when the object is enabled.
    /// </summary>
    private void OnEnable()
    {
        // setup the properties
        tileMap = serializedObject.FindProperty("tileMap");
        cameraController = serializedObject.FindProperty("cameraController");
        light = serializedObject.FindProperty("light");
        settingsUI = serializedObject.FindProperty("settingsUI");

        tilePrefab = serializedObject.FindProperty("tilePrefab");
        endPrefab = serializedObject.FindProperty("endPrefab");
        chickPrefab = serializedObject.FindProperty("chickPrefab");

        loadingType = serializedObject.FindProperty("loadingType");
        lvlExpressionFileName = serializedObject.FindProperty("lvlExpressionFileName");
        lvlBaseBitmap = serializedObject.FindProperty("lvlBaseBitmap");
        lvlDecorationsBitmap = serializedObject.FindProperty("lvlDecorationsBitmap");

        mapScale = serializedObject.FindProperty("mapScale");
        minMountainHeight = serializedObject.FindProperty("minMountainHeight");
        maxMountainHeight = serializedObject.FindProperty("maxMountainHeight");
        tileColors = serializedObject.FindProperty("tileColors");
        decorationColors = serializedObject.FindProperty("decorationColors");
        specialTileColors = serializedObject.FindProperty("specialTileColors");
        tileMaterials = serializedObject.FindProperty("tileMaterials");
        tileCosts = serializedObject.FindProperty("tileCosts");
        decorationTypeObjects = serializedObject.FindProperty("decorationTypeObjects");

        searchAlgorithms = serializedObject.FindProperty("availableSearchAlgorithms");
        currentSearchAlgorithm = serializedObject.FindProperty("currentSearchAlgorithm");

        visualizeSearch = serializedObject.FindProperty("visualizeSearch");
        visualizationSpeed = serializedObject.FindProperty("visualizationSpeed");
        pathColor = serializedObject.FindProperty("pathColor");
        openColor = serializedObject.FindProperty("openColor");
        closedColor = serializedObject.FindProperty("closedColor");

        horizontalLineStyle = new GUIStyle();
        horizontalLineStyle.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLineStyle.margin = new RectOffset(0, 0, 4, 4);
        horizontalLineStyle.fixedHeight = 1;

        titleStyle = new GUIStyle();
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        debugInfoAreaStyle = new GUIStyle();
        debugInfoAreaStyle.padding = new RectOffset(10, 10, 8, 8);
        Color[] pix = new Color[2 * 2];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = new Color(0.3f, 0.3f, 0.3f);
        Texture2D result = new Texture2D(2, 2);
        result.SetPixels(pix);
        result.Apply();
        debugInfoAreaStyle.normal.background = result;

        debugInfoTextStyle = new GUIStyle();
        debugInfoTextStyle.normal.textColor = Color.white;
        debugInfoTextStyle.wordWrap = true;
    }

    public override void OnInspectorGUI()
    {
        // update the GM
        serializedObject.Update();

        // get the target as a GM object
        GameManager gameManager = (GameManager)target;

        // ----- FINDING PATHS PART -----
        Title("Pathfinding");
        // show property for current algorithm
        int selectedPF = Mathf.Max(0, gameManager.GetIndexCurrentSearchAlgorithm());
        string[] optionsPF = new string[gameManager.AmountOfSearchAlgorithms()];
        for (int i = 0; i < gameManager.AmountOfSearchAlgorithms(); i++)
            optionsPF[i] = gameManager.GetSearchAlgorithm(i).name;
        selectedPF = EditorGUILayout.Popup("Current pathfinding algorithm", selectedPF, optionsPF);
        currentSearchAlgorithm.objectReferenceValue = gameManager.GetSearchAlgorithm(selectedPF);
        
        EditorGUILayout.Space(1);
        EditorGUILayout.BeginHorizontal();

        // if we have a current algorithms selected
        // show button to find path
        GUI.enabled = gameManager.GetIndexCurrentSearchAlgorithm() >= 0 && Application.isPlaying;
        if (GUILayout.Button("Find Path"))
            gameManager.FindPath();

        // if we have a path,
        // show button to follow path + reset path
        GUI.enabled = gameManager.HasPath;
        if (GUILayout.Button("Follow Path"))
            gameManager.FollowPath();
        if (GUILayout.Button("Reset Path"))
            gameManager.ResetPath();
     
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical(debugInfoAreaStyle, GUILayout.ExpandWidth(true));

        // display debug information
        if (gameManager.IsSearching)
        {
            EditorGUILayout.LabelField("Searching ...", debugInfoTextStyle);
        }
        else if (gameManager.searchResult == null)
        {
            EditorGUILayout.LabelField("No results yet.", debugInfoTextStyle);
        }
        else
        {
            string text = string.Empty;
            text += "Algorithm: " + gameManager.searchResult.algorithmName + "\n";
            text += "Path cost: " + gameManager.searchResult.pathCost + "\n";
            text += "Tiles in path: " + gameManager.searchResult.tilesInPath + "\n";
            text += "Tiles evaluated: " + gameManager.searchResult.tilesEvaluated + "\n";
            text += "Time: " + String.Format("{0:0.000}", gameManager.searchResult.time) + " sec";

            EditorGUILayout.LabelField(text, debugInfoTextStyle);
        }
        EditorGUILayout.EndVertical();

        // ----- SEARCH ALGORITHMS PART -----

        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("Search Algorithms");

        // property for all possible algorithms
        EditorGUILayout.PropertyField(searchAlgorithms);

        EditorGUILayout.Space(1);

        // ----- LEVEL TO LOAD PART -----

        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("Level to Load");

        // show property for loading type
        EditorGUILayout.PropertyField(loadingType);

        // what is the current loading type?
        GameManager.LoadingType currLoadingType = (GameManager.LoadingType)loadingType.enumValueIndex;

        // switch on the loading type
        switch (currLoadingType)
        {
            case GameManager.LoadingType.Bitmap:
                // and show either the bit map property
                EditorGUILayout.PropertyField(lvlBaseBitmap);
                EditorGUILayout.PropertyField(lvlDecorationsBitmap);
                break;

            case GameManager.LoadingType.Expression:
                // or the expression property
                EditorGUILayout.PropertyField(lvlExpressionFileName);
                break;
        }
        EditorGUILayout.Space(1);

        // ----- DEBUG PART -----

        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("Debug Settings");

        EditorGUILayout.PropertyField(visualizeSearch);

        // if search will be visualized, show settings for it
        if (gameManager.visualizeSearch)
        {
            EditorGUILayout.Slider(visualizationSpeed, 0, 100);
            EditorGUILayout.PropertyField(pathColor);
            EditorGUILayout.PropertyField(openColor);
            EditorGUILayout.PropertyField(closedColor);
        }

        EditorGUILayout.Space(1);

        // ----- REFERENCES PART -----
        // scene references
        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("References to Scene Object");
        EditorGUILayout.PropertyField(tileMap);
        EditorGUILayout.PropertyField(cameraController);
        EditorGUILayout.PropertyField(light);
        EditorGUILayout.PropertyField(settingsUI);
        EditorGUILayout.Space(1);

        // prefabs
        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("References to Prefabs");
        EditorGUILayout.PropertyField(tilePrefab);
        EditorGUILayout.PropertyField(endPrefab);
        EditorGUILayout.PropertyField(chickPrefab);
        EditorGUILayout.Space(1);

        // ----- TILE MAP PART -----
        HorizontalLine(Color.grey);
        EditorGUILayout.Space(1);
        Title("Tile Map");

        EditorGUILayout.PropertyField(mapScale);
        EditorGUILayout.PropertyField(minMountainHeight);
        EditorGUILayout.PropertyField(maxMountainHeight);
        EditorGUILayout.PropertyField(tileColors);
        EditorGUILayout.PropertyField(decorationColors);
        EditorGUILayout.PropertyField(specialTileColors);
        EditorGUILayout.PropertyField(tileMaterials);
        EditorGUILayout.PropertyField(tileCosts);
        EditorGUILayout.PropertyField(decorationTypeObjects);

        EditorGUILayout.Space(1);

        // apply the changes to the object
        serializedObject.ApplyModifiedProperties();
    }

    private void HorizontalLine(Color color)
    {
        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLineStyle);
        GUI.color = c;
    }

    private void Title(string text)
    {
        EditorGUILayout.LabelField(text, titleStyle);
    }
}
