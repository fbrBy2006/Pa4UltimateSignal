using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BlueBoxWindowsBuild
{
    private const string OutputDirectory = "Builds/Windows";
    private const string ExecutableName = "BlueBoxPA4.exe";

    [MenuItem("Blue Box/Build Windows EXE")]
    public static void BuildWindowsExecutable()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play Mode before building the Windows version.");
            return;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes were found in Build Settings. Add BlueBoxLab.unity and try again.");
            return;
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, OutputDirectory);
        Directory.CreateDirectory(outputDirectory);
        string executablePath = Path.Combine(outputDirectory, ExecutableName);

        PlayerSettings.productName = "BlueBoxPA4";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = executablePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log("Building BlueBoxPA4 for Windows x64...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Windows build succeeded: {executablePath} ({summary.totalSize / (1024f * 1024f):F1} MB). Submit the complete Builds/Windows folder, not only the EXE.");
            if (!Application.isBatchMode)
                EditorUtility.RevealInFinder(outputDirectory);
        }
        else
        {
            Debug.LogError($"Windows build failed: {summary.result}. Check the Console for details.");
        }
    }
}
