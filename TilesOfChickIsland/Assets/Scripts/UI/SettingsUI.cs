using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingsUI : MonoBehaviour
{
    // references to the dropdown and buttons in this menu
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Button findPathButton;
    [SerializeField] private Button followPathButton;
    [SerializeField] private Button resetPathButton;

    /// <summary>
    /// Call to initialize this UI element.
    /// </summary>
    internal void Initialize()
    {
        // make sure no test options are in the dropdown anymore
        dropdown.ClearOptions();

        // set the current pathfinding algorithms as the dropdown options
        List<string> dropdownOptions = new List<string>();
        for (int i = 0; i < GameManager.instance.availableSearchAlgorithms.Count; i++)
            dropdownOptions.Add(GameManager.instance.availableSearchAlgorithms[i].name);
        dropdown.AddOptions(dropdownOptions);

        // call a dropdown changed manually, since setting its options
        // doesn't count as a change
        DropdownChanged();

        // set the interactable state of the buttons
        SetInteractableStateButtons();
    }

    /// <summary>
    /// Checks for each button whether it should be interactable rn.
    /// </summary>
    internal void SetInteractableStateButtons()
    {
        // can always find the path again
        findPathButton.interactable = true;

        // can only follow or reset the path if there is a current path
        followPathButton.interactable = resetPathButton.interactable = GameManager.instance.HasPath;
    }

    /// <summary>
    /// Called on dropdown changed.
    /// </summary>
    public void DropdownChanged()
    {
        // set the current search algorithm equal
        // to the current value of the dropdown
        GameManager.instance.SetCurrentSearchAlgorithm(GameManager.instance.availableSearchAlgorithms[dropdown.value]);
    }

    /// <summary>
    /// Called on press find path button.
    /// </summary>
    public void PressFindPath()
    {
        GameManager.instance.FindPath();
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Called on press follow path button.
    /// </summary>
    public void PressFollowPath()
    {
        GameManager.instance.FollowPath();
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Called on press reset path button.
    /// </summary>
    public void PressResetPath()
    {
        GameManager.instance.ResetPath();
        EventSystem.current.SetSelectedGameObject(null);
    }
}
