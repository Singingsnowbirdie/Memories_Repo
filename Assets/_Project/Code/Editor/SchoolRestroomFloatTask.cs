using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Memories.Gameplay;

[InitializeOnLoad]
internal static class SchoolRestroomFloatTask
{
    static SchoolRestroomFloatTask() { EditorApplication.delayCall += Run; }
    static void Run()
    {
        if (!File.Exists("Temp/school-restroom-float.request")) return;
        File.Delete("Temp/school-restroom-float.request");
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/_Project/Locations/School/School.unity");
            if (!scene.isLoaded || EditorApplication.isPlaying) throw new Exception("Школа должна быть открыта вне Play Mode");
            var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var names = new[] { "SM_vantus Variant", "SM_brush Variant", "SprayBottle" };
            var targets = names.Select(n => all.Single(t => t.name == n)).ToArray();
            var group = roots.SelectMany(r => r.GetComponentsInChildren<FloatingScenery>(true)).Single(f => f.name == "Парящие предметы школы");
            var settings = new SerializedObject(group);
            var objects = settings.FindProperty("objects");
            if (objects.arraySize != 22) throw new Exception("Неожиданный размер группы: " + objects.arraySize);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Не удалось сохранить исходную сцену");
            File.Copy(scene.path, "Temp/School.before-restroom-floating.unity", true);
            Undo.RecordObject(group, "Анимация предметов школьного двора");
            int start = objects.arraySize;
            objects.arraySize += targets.Length;
            for (int i = 0; i < targets.Length; i++) objects.GetArrayElementAtIndex(start + i).objectReferenceValue = targets[i];
            settings.ApplyModifiedProperties();
            var report = new StringBuilder();
            foreach (var target in targets)
            {
                report.AppendLine("ADDED " + target.name + " " + target.localPosition);
                foreach (var collider in target.GetComponentsInChildren<Collider>(true))
                {
                    report.AppendLine("REMOVED " + collider.name + " " + collider.GetType().Name);
                    Undo.DestroyObjectImmediate(collider);
                }
            }
            int total = 0;
            foreach (var f in roots.SelectMany(r => r.GetComponentsInChildren<FloatingScenery>(true)))
            {
                var a = new SerializedObject(f).FindProperty("objects");
                for (int i = 0; i < a.arraySize; i++)
                {
                    var t = a.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (t == null || t.GetComponentsInChildren<Collider>(true).Length != 0) throw new Exception("Ошибка проверки анимированного объекта");
                    total++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Не удалось сохранить результат");
            report.AppendLine("SUCCESS: " + total + " объектов без коллайдеров");
            File.WriteAllText("Temp/school-restroom-float-report.txt", report.ToString());
        }
        catch (Exception e) { File.WriteAllText("Temp/school-restroom-float-report.txt", e.ToString()); }
    }
}
