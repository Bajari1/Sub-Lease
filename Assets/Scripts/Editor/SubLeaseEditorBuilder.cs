#if UNITY_EDITOR
using System.IO;
using SubLease.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
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

            // Allow background execution so 2 clients (Host + Sandboxie Client) run simultaneously on the same PC!
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[SubLeaseEditorBuilder] Successfully created and saved playable scene at: {scenePath}");
        }

        [MenuItem("SubLease/Build Windows Standalone Exe (For 2-Client Sandboxie Test)")]
        public static void BuildWindowsStandaloneExe()
        {
            BuildAndSaveDefaultScene();

            string buildDir = "Builds/Windows";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "SubLease.exe");
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SubLease_DropHatch_Main.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[SubLeaseEditorBuilder] Standalone Build Result: {report.summary.result} | Output: {exePath}");
        }

        [MenuItem("SubLease/Build Meta Quest 3 Standalone APK (ARM64)")]
        public static void BuildQuest3StandaloneApk()
        {
            BuildAndSaveDefaultScene();

            string buildDir = "Builds/Quest3";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

            string apkPath = Path.Combine(buildDir, "SubLease_Quest3.apk");
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SubLease_DropHatch_Main.unity" },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[SubLeaseEditorBuilder] Meta Quest 3 APK Build Result: {report.summary.result} | Output: {apkPath}");
        }
    }
}
#endif
