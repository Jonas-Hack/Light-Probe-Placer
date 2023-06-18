/*
*  Written by Jonas H.
*  
*   An editor window for automatically placing light probes within a rectengular area.
*   Offers the option to avoid the placement of probes within scene geometry.
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

/// <summary>
/// An editor window for automatically placing light probes within a rectengular area.
/// Offers the option to avoid the placement of probes within scene geometry.
/// </summary>
public class LightProbePlacer : EditorWindow
{
#region Parameters
    // Keep track if I'm doing light probe work right now
    bool probeSelected = false;
    LightProbeGroup group;

    // Bounds of the area to be filled with probes
    Vector3 probePos;
    Vector3 size = Vector3.one;

    float probeSpacing = 1.0f;

    // Scene intersection
    bool avoidIntersection = true;
    float intersectionRange = 0.2f;
    LayerMask intersectionMask;
    #endregion

#region Widow Management
    [MenuItem("Custom Tools/Light Probe Placer")]
    static void Init()
    {
        //Get existing open window or if none, make a new one:
        LightProbePlacer window = (LightProbePlacer)EditorWindow.GetWindow(typeof(LightProbePlacer));
        window.Show();
    }
    #endregion

#region Selection
    // When selecting something, gather all needed data
    private void OnSelectionChange()
    {
        // Find out what I have even selected

        // Get Light Probe Group Object From the Scene
        group = null;
        Transform selected = Selection.activeTransform;
        if (selected != null) group = selected.gameObject.GetComponent<LightProbeGroup>();

        // Am I doing any lightprobe work?
        probeSelected = (group != null);
        if (probeSelected)
        {
            // Try to infer appropiate parameter values
            // Center of my generation area
            probePos = group.transform.position;
            // Bounds of my generation area (local space)
            Vector3[] probePositions = group.probePositions;
            if (probePositions.Length != 0)
            {
                Vector3 bounds = new Vector3();
                foreach (Vector3 pos in probePositions)
                {
                    bounds.x = Mathf.Max(bounds.x, Mathf.Abs(pos.x));
                    bounds.y = Mathf.Max(bounds.y, Mathf.Abs(pos.y));
                    bounds.z = Mathf.Max(bounds.z, Mathf.Abs(pos.z));
                }
                size = bounds * 2.0f;
            }
        }

        // More resposive window
        Repaint();
    }
#endregion

    // On (immideate mode) UI redraw
    private void OnGUI()
    {
        // Title
        GUILayout.Label("Light Probe Placer", EditorStyles.whiteLargeLabel);
        // Abort if we're not currently doing any lightprobe work
        if (!probeSelected)
        {
            GUILayout.Label("No Light Probe Group Selected", EditorStyles.label);
            return;
        }


#region Parameter GUI
        // Get maximum extent along each axis in local space
        GUILayout.Label("Size:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("X:", EditorStyles.label);
        size.x = EditorGUILayout.FloatField(size.x);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Y:", EditorStyles.label);
        size.y = EditorGUILayout.FloatField(size.y);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Z:", EditorStyles.label);
        size.z = EditorGUILayout.FloatField(size.z);
        EditorGUILayout.EndHorizontal();

        // Get Probe "Density"
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Probe Spacing", EditorStyles.label);
        probeSpacing = EditorGUILayout.FloatField(probeSpacing);
        EditorGUILayout.EndHorizontal();

        // Optionally Avoid Intersecting Any (Physics) Objects
        GUILayout.Label("Intersection Avoidance", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Avoid Intersection", EditorStyles.label);
        avoidIntersection = EditorGUILayout.Toggle(avoidIntersection);
        EditorGUILayout.EndHorizontal();
        
        // Only draw intersection parameters if needed
        if(avoidIntersection)
        {
            // Min Distance to Scene Objects
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Margin", EditorStyles.label);
            intersectionRange = EditorGUILayout.FloatField(intersectionRange);
            EditorGUILayout.EndHorizontal();

            // Which Objects to Avoid
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mask", EditorStyles.label);
            // https://answers.unity.com/questions/42996/how-to-create-layermask-field-in-a-custom-editorwi.html
            LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(intersectionMask), InternalEditorUtility.layers);
            intersectionMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            EditorGUILayout.EndHorizontal();
        }

        // Doesn't hurt to know the number of probes of the group
        GUILayout.Label("Current Probe Count: " + group.probePositions.Length, EditorStyles.helpBox);

        GUILayout.Space(10);

        // Trigger Generation of Probe Positions
        bool generate = GUILayout.Button("Generate", GUILayout.Height(25));

        #endregion

#region Generation
        // If I need to replace the probes of this group
        if (generate)
        {
            // Support Ctrl + Z
            Undo.RecordObject(group, "Generate Lightprobe Probe Positions");
            // Make it possible to save changes
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // Let's explicitly state our borders
            float minX = -size.x  / 2.0f;
            float maxX = size.x / 2.0f;

            float minY = -size.y / 2.0f;
            float maxY = size.y / 2.0f;

            float minZ = -size.z / 2.0f;
            float maxZ = size.z / 2.0f;

            // Store probe positions here
            List<Vector3> positions = new List<Vector3>();
            
            // Hold temp position values
            Vector3 pos = new Vector3(minX, minY, minZ);
            
            // Go Through Every Possible Position
            for(pos.x = minX; pos.x <= maxX; pos.x += probeSpacing)
            for(pos.y = minY; pos.y <= maxY; pos.y += probeSpacing)
            for(pos.z = minZ; pos.z <= maxZ; pos.z += probeSpacing)
            {
                // Test For Intersections of the scenery in world space
                Vector3 worldPos = group.transform.TransformPoint(pos);
                bool intersecting = Physics.CheckSphere(worldPos, intersectionRange, intersectionMask);
                if (!avoidIntersection || !intersecting)
                {   // Use This position
                    positions.Add(pos);
                }
            }

            // Apply to Light Probe Group
            group.probePositions = positions.ToArray();
        }
    #endregion
    }

#region Scene View Bound Rendering
    // Needed to draw Scene View Handles (Gizmos) from an Editor Window

    private void OnFocus()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView view)
    {
        if (!probeSelected) return;

        Handles.color = Color.yellow;
        // Transform Handles to fit lightprobe group transform
        Handles.matrix = group.transform.localToWorldMatrix;
        Handles.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, size.z));
    }
 #endregion
}

#endif