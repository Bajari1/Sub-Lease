#if UNITY_EDITOR
using System.IO;
using SubLease.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubLease.EditorTools
{
    public static class SubLeaseEditorBuilder
    {
        [MenuItem("SubLease/Generate & Save Playable Drop Hatch Scene")]
        public static void BuildAndSaveDefaultScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootObj = new GameObject("_SUB_LEASE_GameBootstrapper");
            bootObj.AddComponent<GameBootstrapper>();

            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
            }

            string scenePath = "Assets/Scenes/SubLease_DropHatch_Main.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log($"[SubLeaseEditorBuilder] Successfully created and saved playable scene at: {scenePath}");
        }
    }
}
#endif
