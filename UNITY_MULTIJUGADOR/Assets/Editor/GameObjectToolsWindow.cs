using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// A custom Editor Window that provides two tools:
///  1. Sort selected GameObjects alphabetically within their hierarchy.
///  2. Batch rename selected GameObjects by stripping characters and optional numbering.
/// </summary>
public class GameObjectToolsWindow : EditorWindow
{
    // Which tab is currently selected: 0 = Sort, 1 = Batch Rename
    int _tab = 0;
    readonly string[] _tabs = new[] { "Sort", "Batch Rename" };

    // === Batch-Rename Fields ===

    [Tooltip("When enabled, append sequential numbers after renaming.")]
    bool _enableNumbering = true;

    [Tooltip("When enabled, all characters will be replaced.")]
    bool _fullRename = true;

    [Tooltip("When enabled, all characters will be replaced.")]
    string _newName = "";

    [Tooltip("Number of characters to remove from the start of each name.")]
    int _removeFromStart = 0;

    [Tooltip("Number of characters to remove from the end of each name.")]
    int _removeFromEnd = 0;

    [Tooltip("First number in the appended sequence (if numbering is enabled).")]
    int _startNumber = 1;

    [Tooltip("If true, sort objects alphabetically before applying numbering.")]
    bool _sortBefore = true;

    /// <summary>
    /// Adds a menu item under Tools --> GameObject Tools
    /// </summary>
    [MenuItem("Tools/GameObject Tools")]
    static void OpenWindow()
    {
        GetWindow<GameObjectToolsWindow>("GameObject Tools");
    }

    /// <summary>
    /// Draws the window GUI each frame.
    /// </summary>
    void OnGUI()
    {
        // Draw the tab bar
        _tab = GUILayout.Toolbar(_tab, _tabs);
        GUILayout.Space(8);

        // Draw the selected tab's contents
        if (_tab == 0) DrawSortTab();
        else /* _tab == 1 */ DrawBatchRenameTab();
    }

    #region Sort Tab

    /// <summary>
    /// Renders the "Sort" tab.
    /// </summary>
    void DrawSortTab()
    {
        EditorGUILayout.LabelField("Sort Selected GameObjects Alphabetically",
                                   EditorStyles.boldLabel);

        // Show an info box if fewer than two objects are selected
        if (Selection.transforms.Length < 2)
        {
            EditorGUILayout.HelpBox("Select two or more GameObjects.", MessageType.Info);
            return;
        }

        // Button to trigger sorting
        if (GUILayout.Button("Sort Selected Alphabetically"))
        {
            SortSelected();
        }
    }

    /// <summary>
    /// Sorts all selected GameObjects alphabetically within each parent group.
    /// </summary>
    void SortSelected()
    {
        // Group selected transforms by their parent transform
        var groups = Selection.transforms.GroupBy(t => t.parent);

        foreach (var group in groups)
        {
            Transform parent = group.Key;

            // Register Undo on the parent so the reordering can be undone
            if (parent != null)
                Undo.RegisterCompleteObjectUndo(parent, "Sort Selected GameObjects");

            // Collect all sibling transforms under this parent (or root objects)
            List<Transform> all;
            if (parent != null)
            {
                all = parent.Cast<Transform>().ToList();
            }
            else
            {
                // For root-level objects, fetch from the active scene
                all = SceneManager.GetActiveScene()
                                  .GetRootGameObjects()
                                  .Select(go => go.transform)
                                  .ToList();
            }

            // Extract selected children, sort them by name
            var selectedList = group.ToList();
            selectedList.Sort((a, b) => string.Compare(a.name, b.name));

            // Determine where to insert them (smallest current sibling index)
            int insertAt = group.Min(t => t.GetSiblingIndex());

            // Remove the selected from the full list and insert sorted
            all.RemoveAll(t => selectedList.Contains(t));
            all.InsertRange(insertAt, selectedList);

            // Reassign sibling indices to match the new order
            for (int i = 0; i < all.Count; i++)
            {
                all[i].SetSiblingIndex(i);
            }
        }
    }

    #endregion

    #region Batch Rename Tab

    /// <summary>
    /// Renders the "Batch Rename & Number" tab.
    /// </summary>
    void DrawBatchRenameTab()
    {
        EditorGUILayout.LabelField("Batch Rename & Number", EditorStyles.boldLabel);

        // No selection? show info and return early
        if (Selection.transforms.Length == 0)
        {
            EditorGUILayout.HelpBox("Select one or more GameObjects.", MessageType.Info);
            return;
        }

        // Toggle numbering on/off
        _enableNumbering = EditorGUILayout.Toggle(
            new GUIContent("Enable Numbering", "If unchecked, only character removal is applied."), _enableNumbering);

        // Toggle full rename on/off
        _fullRename = EditorGUILayout.Toggle(
    new GUIContent("Full Rename", "When enabled, all existing characters will be replaced with a new name."), _fullRename);

        if (_fullRename)
        {
            _newName = EditorGUILayout.TextField(new GUIContent("New Name", "Name to completely replace existing names."), _newName);
        }
        else
        {
            _removeFromStart = EditorGUILayout.IntField(
                new GUIContent("Remove From Start", "Number of characters to strip off the front of the name."), _removeFromStart);

            _removeFromEnd = EditorGUILayout.IntField(
                new GUIContent("Remove From End", "Number of characters to strip off the end of the name."), _removeFromEnd);
        }

        // Only show numbering options if enabled
        if (_enableNumbering)
        {
            _startNumber = EditorGUILayout.IntField(
                new GUIContent("Start Number", "First number in the appended sequence."), _startNumber);

            _sortBefore = EditorGUILayout.Toggle(
                new GUIContent("Sort Before Numbering","Order items alphabetically before numbering."), _sortBefore);
        }

        GUILayout.Space(8);

        // Button to apply the batch rename (and numbering if enabled)
        string buttonLabel = _enableNumbering ? "Apply Batch Rename & Number" : "Apply Batch Rename";
        if (GUILayout.Button(buttonLabel))
        {
            ApplyBatchRename();
        }
    }

    /// <summary>
    /// Performs the batch rename and optional numbering on the selected GameObjects.
    /// </summary>
    void ApplyBatchRename()
    {
        // Optionally sort the selection before renaming
        IEnumerable<Transform> items = Selection.transforms.AsEnumerable();
        if (_enableNumbering && _sortBefore)
        {
            items = items.OrderBy(t => t.name);
        }

        var list = items.ToList();
        int count = list.Count;

        // Determine padding width if numbering is on
        int numberWidth = 0;
        if (_enableNumbering)
        {
            int maxNumber = _startNumber + count - 1;
            numberWidth = maxNumber.ToString().Length;
        }

        // Record Undo for all selected objects at once
        Undo.RecordObjects(list.ToArray(), _enableNumbering ? "Batch Rename & Number" : "Batch Rename");

        // Process each object
        for (int i = 0; i < count; i++)
        {
            Transform t = list[i];
            string originalName = t.name;

            // Clamp removal counts to valid ranges
            int startIdx = Mathf.Clamp(_removeFromStart, 0, originalName.Length);
            int endCount = Mathf.Clamp(_removeFromEnd, 0, originalName.Length - startIdx);

            // Compute the base name after stripping
            string baseName;
            if (_fullRename)
            {
                baseName = _newName;
            }
            else
            {
                baseName = originalName.Substring(
                    startIdx,
                    originalName.Length - startIdx - endCount
                );
            }

            // Append numbered suffix if needed
            if (_enableNumbering)
            {
                int number = _startNumber + i;
                string padded = number.ToString().PadLeft(numberWidth, '0');
                t.name = baseName + (" (") + padded + (")");
            }
            else
            {
                t.name = baseName;
            }
        }
    }

    #endregion
}
