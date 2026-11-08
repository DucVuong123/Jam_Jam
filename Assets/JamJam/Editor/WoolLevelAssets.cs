using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamJam.Editor
{
    public static class WoolLevelAssets
    {
        public const string Folder = WoolAssetsBuilder.Root + "/Levels";
        public const string Expected = WoolLevelGenerator.ExpectedFingerprint;

        [MenuItem("JamJam/Generate Levels")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before generating levels");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before generating levels");
            var active = SceneManager.GetActiveScene().path;
            Write(out var levels);
            Assign(WoolArtBuilder.ScenePath, levels);
            Assign(WoolLobbyBuilder.ScenePath, levels);
            if (!string.IsNullOrEmpty(active) && SceneManager.GetActiveScene().path != active) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
            Debug.Log("Generated " + levels.Length + " Wool levels");
        }

        public static void Write(out WoolLevel[] levels)
        {
            var results = new WoolLevelResult[WoolLevelGenerator.Count];
            for (var i = 0; i < results.Length; i++)
                if (!WoolLevelGenerator.TryGenerate(i + 1, out results[i], out var error)) throw new InvalidOperationException(error);
            Directory.CreateDirectory(Folder);
            levels = new WoolLevel[results.Length];
            for (var i = 0; i < results.Length; i++)
            {
                var path = Folder + "/Level_" + (i + 1) + ".asset";
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<WoolLevel>();
                    AssetDatabase.CreateAsset(level, path);
                }
                var result = results[i];
                level.Title = result.Recipe.Title;
                level.Data = result.Data;
                var points = new Vector3[result.Path.Count];
                for (var p = 0; p < points.Length; p++) points[p] = new Vector3(result.Path[p].X, 0, result.Path[p].Z);
                level.Path = points;
                EditorUtility.SetDirty(level);
                levels[i] = level;
            }
            AssetDatabase.SaveAssets();
        }

        static void Assign(string scenePath, WoolLevel[] levels)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            var lobby = UnityEngine.Object.FindFirstObjectByType<WoolLobby>();
            if (game == null && lobby == null) throw new InvalidOperationException("No WoolScene or WoolLobby in " + scenePath);
            if (game != null) { game.Levels = (WoolLevel[])levels.Clone(); EditorUtility.SetDirty(game); }
            if (lobby != null) { lobby.Levels = (WoolLevel[])levels.Clone(); EditorUtility.SetDirty(lobby); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static string Check()
        {
            var memory = 2166136261u;
            var stored = 2166136261u;
            var guids = new List<string>();
            for (var i = 1; i <= WoolLevelGenerator.Count; i++)
            {
                if (!WoolLevelGenerator.TryGenerate(i, out var result, out var error)) throw new InvalidOperationException(error);
                var path = Folder + "/Level_" + i + ".asset";
                var level = AssetDatabase.LoadAssetAtPath<WoolLevel>(path);
                if (level == null) throw new InvalidOperationException("Missing " + path);
                if (level.Title != result.Recipe.Title) throw new InvalidOperationException("Level " + i + " title is " + level.Title);
                unchecked
                {
                    memory = (memory ^ WoolLevelGenerator.Fingerprint(result.Data)) * 16777619u;
                    stored = (stored ^ WoolLevelGenerator.Fingerprint(level.Data)) * 16777619u;
                }
                guids.Add(AssetDatabase.AssetPathToGUID(path));
            }
            if (memory.ToString("X8") != Expected) throw new InvalidOperationException("Unity generator fingerprint " + memory.ToString("X8") + " differs from harness " + Expected);
            if (stored.ToString("X8") != Expected) throw new InvalidOperationException("Level assets fingerprint " + stored.ToString("X8") + " differs from " + Expected);
            ExpectList(WoolArtBuilder.ScenePath, guids);
            ExpectList(WoolLobbyBuilder.ScenePath, guids);
            return "LEVEL ASSETS PASS / " + guids.Count + " levels / fingerprint " + Expected + " / both scenes list Level_1..30";
        }

        static void ExpectList(string scenePath, IReadOnlyList<string> guids)
        {
            var lines = File.ReadAllLines(scenePath);
            var found = new List<string>();
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i] != "  Levels:") continue;
                for (var j = i + 1; j < lines.Length && lines[j].StartsWith("  - {fileID: 11400000, guid: ", StringComparison.Ordinal); j++)
                    found.Add(lines[j].Substring(29, 32));
                break;
            }
            if (found.Count != guids.Count) throw new InvalidOperationException(scenePath + " lists " + found.Count + " levels");
            for (var i = 0; i < guids.Count; i++)
                if (found[i] != guids[i]) throw new InvalidOperationException(scenePath + " level " + (i + 1) + " is not Level_" + (i + 1));
        }
    }
}
