using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class SceneScriptComparator : EditorWindow
{
    private Object scene1Asset;
    private Object scene2Asset;
    private Vector2 scrollPosition;
    private List<ScriptInfo> scriptsInScene1Only = new List<ScriptInfo>();
    private List<ScriptInfo> scriptsInScene2Only = new List<ScriptInfo>();
    private List<ScriptInfo> scriptsInBoth = new List<ScriptInfo>();
    private bool hasCompared = false;

    [System.Serializable]
    private class ScriptInfo
    {
        public string scriptName;
        public string guid;
        public string gameObjectName;
        public string fullPath;

        public ScriptInfo(string scriptName, string guid, string gameObjectName, string fullPath)
        {
            this.scriptName = scriptName;
            this.guid = guid;
            this.gameObjectName = gameObjectName;
            this.fullPath = fullPath;
        }
    }

    [MenuItem("Tools/Scene Script Comparator")]
    public static void ShowWindow()
    {
        GetWindow<SceneScriptComparator>("Scene Script Comparator");
    }

    void OnGUI()
    {
        GUILayout.Label("Compare Scripts Between Two Scenes", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scene1Asset = EditorGUILayout.ObjectField("Scene 1", scene1Asset, typeof(Object), false);
        scene2Asset = EditorGUILayout.ObjectField("Scene 2", scene2Asset, typeof(Object), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Compare Scenes", GUILayout.Height(30)))
        {
            if (scene1Asset == null || scene2Asset == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both scenes to compare.", "OK");
                return;
            }

            string scene1Path = AssetDatabase.GetAssetPath(scene1Asset);
            string scene2Path = AssetDatabase.GetAssetPath(scene2Asset);

            if (!scene1Path.EndsWith(".unity") || !scene2Path.EndsWith(".unity"))
            {
                EditorUtility.DisplayDialog("Error", "Please select valid Unity scene files (.unity).", "OK");
                return;
            }

            CompareScenes(scene1Path, scene2Path);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (hasCompared)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Scripts in Scene 1 only
            if (scriptsInScene1Only.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Scripts in Scene 1 ONLY ({scriptsInScene1Only.Count}):", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var script in scriptsInScene1Only.OrderBy(s => s.scriptName))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(script.scriptName, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"on '{script.gameObjectName}'", EditorStyles.miniLabel);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        SelectGameObjectInScene(scene1Asset, script.gameObjectName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            // Scripts in Scene 2 only
            if (scriptsInScene2Only.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Scripts in Scene 2 ONLY ({scriptsInScene2Only.Count}):", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var script in scriptsInScene2Only.OrderBy(s => s.scriptName))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(script.scriptName, GUILayout.Width(200));
                    EditorGUILayout.LabelField($"on '{script.gameObjectName}'", EditorStyles.miniLabel);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        SelectGameObjectInScene(scene2Asset, script.gameObjectName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            // Scripts in both scenes
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Scripts in BOTH scenes ({scriptsInBoth.Count}):", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var script in scriptsInBoth.OrderBy(s => s.scriptName))
            {
                EditorGUILayout.LabelField(script.scriptName);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndScrollView();
        }
    }

    void CompareScenes(string scene1Path, string scene2Path)
    {
        scriptsInScene1Only.Clear();
        scriptsInScene2Only.Clear();
        scriptsInBoth.Clear();

        Dictionary<string, ScriptInfo> scene1Scripts = ExtractScriptsFromScene(scene1Path);
        Dictionary<string, ScriptInfo> scene2Scripts = ExtractScriptsFromScene(scene2Path);

        // Find scripts in scene 1 only
        foreach (var kvp in scene1Scripts)
        {
            if (!scene2Scripts.ContainsKey(kvp.Key))
            {
                scriptsInScene1Only.Add(kvp.Value);
            }
            else
            {
                scriptsInBoth.Add(kvp.Value);
            }
        }

        // Find scripts in scene 2 only
        foreach (var kvp in scene2Scripts)
        {
            if (!scene1Scripts.ContainsKey(kvp.Key))
            {
                scriptsInScene2Only.Add(kvp.Value);
            }
        }

        hasCompared = true;
        Debug.Log($"Comparison complete! Found {scriptsInScene1Only.Count} scripts in Scene 1 only, {scriptsInScene2Only.Count} in Scene 2 only, and {scriptsInBoth.Count} in both.");
    }

    Dictionary<string, ScriptInfo> ExtractScriptsFromScene(string scenePath)
    {
        Dictionary<string, ScriptInfo> scripts = new Dictionary<string, ScriptInfo>();
        Dictionary<string, string> guidToScriptName = new Dictionary<string, string>();
        Dictionary<string, string> gameObjectNames = new Dictionary<string, string>();
        Dictionary<string, string> gameObjectPaths = new Dictionary<string, string>();

        // First, build a map of GUID to script name from .meta files
        string[] allScripts = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        foreach (string scriptPath in allScripts)
        {
            string metaPath = scriptPath + ".meta";
            if (File.Exists(metaPath))
            {
                string metaContent = File.ReadAllText(metaPath);
                Match guidMatch = Regex.Match(metaContent, @"guid:\s*([a-f0-9]{32})");
                if (guidMatch.Success)
                {
                    string guid = guidMatch.Groups[1].Value;
                    string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                    guidToScriptName[guid] = scriptName;
                }
            }
        }

        // Read the scene file
        string sceneContent = File.ReadAllText(scenePath);

        // Extract GameObject names and their fileIDs
        Regex gameObjectRegex = new Regex(@"--- !u!1 &(\d+)\s+GameObject:.*?m_Name: (.+?)(?:\r?\n|$)", RegexOptions.Singleline);
        MatchCollection gameObjectMatches = gameObjectRegex.Matches(sceneContent);
        foreach (Match match in gameObjectMatches)
        {
            string fileID = match.Groups[1].Value;
            string gameObjectName = match.Groups[2].Value.Trim();
            gameObjectNames[fileID] = gameObjectName;
        }

        // Build GameObject hierarchy paths
        BuildGameObjectPaths(sceneContent, gameObjectNames, gameObjectPaths);

        // Extract MonoBehaviour components with their script GUIDs
        Regex monoBehaviourRegex = new Regex(@"--- !u!114 &(\d+)\s+MonoBehaviour:.*?m_GameObject: \{fileID: (\d+)\}.*?m_Script: \{fileID: 11500000, guid: ([a-f0-9]{32})", RegexOptions.Singleline);
        MatchCollection monoBehaviourMatches = monoBehaviourRegex.Matches(sceneContent);

        foreach (Match match in monoBehaviourMatches)
        {
            string fileID = match.Groups[1].Value;
            string gameObjectFileID = match.Groups[2].Value;
            string guid = match.Groups[3].Value;

            if (guidToScriptName.ContainsKey(guid))
            {
                string scriptName = guidToScriptName[guid];
                string gameObjectName = gameObjectNames.ContainsKey(gameObjectFileID) ? gameObjectNames[gameObjectFileID] : "Unknown";
                string fullPath = gameObjectPaths.ContainsKey(gameObjectFileID) ? gameObjectPaths[gameObjectFileID] : gameObjectName;

                // Use script name + game object as unique key to avoid duplicates
                string key = $"{scriptName}_{gameObjectName}";
                if (!scripts.ContainsKey(key))
                {
                    scripts[key] = new ScriptInfo(scriptName, guid, gameObjectName, fullPath);
                }
            }
        }

        return scripts;
    }

    void BuildGameObjectPaths(string sceneContent, Dictionary<string, string> gameObjectNames, Dictionary<string, string> gameObjectPaths)
    {
        // Extract parent-child relationships
        Dictionary<string, string> parentMap = new Dictionary<string, string>();
        Regex transformRegex = new Regex(@"--- !u!224 &(\d+)\s+RectTransform:.*?m_GameObject: \{fileID: (\d+)\}.*?m_Father: \{fileID: (\d+)\}", RegexOptions.Singleline);
        MatchCollection transformMatches = transformRegex.Matches(sceneContent);
        
        foreach (Match match in transformMatches)
        {
            string transformFileID = match.Groups[1].Value;
            string gameObjectFileID = match.Groups[2].Value;
            string parentFileID = match.Groups[3].Value;
            
            if (parentFileID != "0")
            {
                parentMap[gameObjectFileID] = parentFileID;
            }
        }

        // Also check regular Transform components
        Regex regularTransformRegex = new Regex(@"--- !u!4 &(\d+)\s+Transform:.*?m_GameObject: \{fileID: (\d+)\}.*?m_Father: \{fileID: (\d+)\}", RegexOptions.Singleline);
        MatchCollection regularTransformMatches = regularTransformRegex.Matches(sceneContent);
        
        foreach (Match match in regularTransformMatches)
        {
            string transformFileID = match.Groups[1].Value;
            string gameObjectFileID = match.Groups[2].Value;
            string parentFileID = match.Groups[3].Value;
            
            if (parentFileID != "0")
            {
                parentMap[gameObjectFileID] = parentFileID;
            }
        }

        // Build paths for each GameObject
        foreach (var kvp in gameObjectNames)
        {
            string fileID = kvp.Key;
            string name = kvp.Value;
            string path = BuildPath(fileID, parentMap, gameObjectNames);
            gameObjectPaths[fileID] = path;
        }
    }

    string BuildPath(string fileID, Dictionary<string, string> parentMap, Dictionary<string, string> gameObjectNames)
    {
        List<string> pathParts = new List<string>();
        string currentFileID = fileID;

        while (currentFileID != null && gameObjectNames.ContainsKey(currentFileID))
        {
            pathParts.Insert(0, gameObjectNames[currentFileID]);
            if (parentMap.ContainsKey(currentFileID))
            {
                currentFileID = parentMap[currentFileID];
            }
            else
            {
                break;
            }
        }

        return string.Join("/", pathParts);
    }

    void SelectGameObjectInScene(Object sceneAsset, string gameObjectName)
    {
        if (sceneAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != scenePath)
            {
                if (EditorUtility.DisplayDialog("Open Scene", 
                    $"Scene '{sceneAsset.name}' is not currently open. Do you want to open it?", 
                    "Yes", "No"))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }
                else
                {
                    return;
                }
            }

            // Try to find the GameObject in the scene
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            GameObject found = allObjects.FirstOrDefault(go => go.name == gameObjectName);
            
            if (found != null)
            {
                Selection.activeGameObject = found;
                EditorGUIUtility.PingObject(found);
            }
            else
            {
                EditorUtility.DisplayDialog("Not Found", 
                    $"GameObject '{gameObjectName}' not found in the currently open scene.", "OK");
            }
        }
    }
}

