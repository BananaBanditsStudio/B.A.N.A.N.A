using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Randomiserrrrr))]
public class RandomiseUI : Editor
{
    public override void OnInspectorGUI()
    {
        Randomiserrrrr randomrr = (Randomiserrrrr)target;

        GUILayout.Label("Tree Randomization Settings");
        
        GUILayout.Space(5);
        randomrr.multiSize = EditorGUILayout.FloatField("Base Size Multiplier", randomrr.multiSize);
        randomrr.sizeVariation = EditorGUILayout.Slider("Size Variation", randomrr.sizeVariation, 0f, 0.5f);
        
        GUILayout.Space(5);
        GUILayout.Label("Rotation");
        randomrr.xRot = EditorGUILayout.Toggle("Rotate X", randomrr.xRot);
        randomrr.yRot = EditorGUILayout.Toggle("Rotate Y", randomrr.yRot);
        randomrr.zRot = EditorGUILayout.Toggle("Rotate Z", randomrr.zRot);

        GUILayout.Space(10);
        
        var style = new GUIStyle(GUI.skin.button);
        style.normal.textColor = Color.green;
        style.fontSize = 14;
        
        if (GUILayout.Button("Randomise All Children", style))
        {
            randomrr.RandomiseAllChildren();
        }
    }
}