using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Memories.Gameplay.Reactions;
using Memories.Gameplay.Story;

[InitializeOnLoad]
public static class TempEvansPhotoConnect
{
    static TempEvansPhotoConnect() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        const string result = "Temp/EvansPhotoConnect.result";
        if (File.Exists(result) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            const string path = "Assets/_Project/Configs/Reactions/Hangar/InspectEvansPhoto.asset";
            const string textPath = "Assets/_Project/Configs/StoryTexts/Hangar/EvansPhotoText.asset";
            AssetDatabase.ImportAsset(textPath, ImportAssetOptions.ForceUpdate);
            var text = AssetDatabase.LoadAssetAtPath<StoryTextGraph>(textPath);
            if (!text) throw new Exception("Text asset unavailable");
            var graph = AssetDatabase.LoadAssetAtPath<ReactionGraph>(path);
            var activity = graph.nodes.OfType<ReactionNode>().Single(n => n.Kind == ReactionKind.ObjectActivity);
            var node = AssetDatabase.LoadAllAssetsAtPath(path).OfType<ReactionNode>().SingleOrDefault(n => n.Kind == ReactionKind.StoryText);
            if (node == null)
            {
                node = graph.AddNode<ReactionNode>();
                AssetDatabase.AddObjectToAsset(node, graph);
            }
            else if (!graph.nodes.Contains(node)) graph.nodes.Add(node);
            node.position = activity.position + new Vector2(400, 0);
            var so = new SerializedObject(node);
            so.FindProperty("kind").intValue = (int)ReactionKind.StoryText;
            so.FindProperty("textGraph").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            activity.GetOutputPort("next").Connect(node.GetInputPort("previous"));
            if (!graph.TryGetSequence(out var actions, out var error)) throw new Exception(error);
            foreach (var n in graph.nodes) EditorUtility.SetDirty(n);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            XNodeEditor.NodeEditorWindow.RepaintAll();
            File.WriteAllText(result, "OK: loaded graph validated; activity -> text connected; " + actions.Count + " actions.");
        }
        catch (Exception ex) { File.WriteAllText(result, ex.ToString()); Debug.LogException(ex); }
    }
}
