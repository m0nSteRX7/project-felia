using System;
using System.IO;
using ProjectFelia.DesktopStandalone;
using ProjectFelia.EditorXR.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectFeliaBuildPipeline
{
    private const string LauncherScenePath = "Assets/Scenes/Launcher.unity";

    private const string EditorScenePath = "Assets/Scenes/DesktopEditor.unity";

    private const string BuildFolderPath = "Builds/macOS";

    private const string BuildAppPath = "Builds/macOS/ProjectFelia.app";

    [MenuItem("Project Felia/Build macOS Standalone")]
    public static void BuildMacOSStandalone()
    {
        EnsureStarterScenes();
        AssetDatabase.Refresh();
        var report = BuildStandaloneApp();
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"macOS build failed with result: {report.summary.result}");
        }
    }

    [MenuItem("Project Felia/Build and Open macOS Standalone")]
    public static void BuildAndOpenMacOSStandalone()
    {
        BuildMacOSStandalone();

        if (File.Exists(BuildAppPath))
        {
            EditorUtility.RevealInFinder(BuildAppPath);
        }
    }

    [MenuItem("Project Felia/Generate Starter Scenes")]
    public static void GenerateStarterScenes()
    {
        EnsureStarterScenes();
        AssetDatabase.Refresh();
    }

    private static BuildReport BuildStandaloneApp()
    {
        Directory.CreateDirectory(BuildFolderPath);

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] { LauncherScenePath, EditorScenePath },
            locationPathName = BuildAppPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        return BuildPipeline.BuildPlayer(buildPlayerOptions);
    }

    private static void EnsureStarterScenes()
    {
        EnsureLauncherScene();
        EnsureEditorScene();
    }

    private static void EnsureLauncherScene()
    {
        if (!SceneExists(LauncherScenePath))
        {
            CreateLauncherScene(LauncherScenePath);
        }
    }

    private static void EnsureEditorScene()
    {
        if (!SceneExists(EditorScenePath))
        {
            CreateEditorScene(EditorScenePath);
        }
    }

    private static bool SceneExists(string scenePath)
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null;
    }

    private static void CreateEmptyScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void CreateLauncherScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var launcherObject = new GameObject("DesktopLauncher");
        launcherObject.AddComponent<DesktopLauncherBootstrap>();

        Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void CreateEditorScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightObject = new GameObject("Directional Light");
        lightObject.AddComponent<Light>().type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
        cameraObject.AddComponent<DesktopEditorCameraController>();

        var bootstrapObject = new GameObject("DesktopEditorBootstrap");
        bootstrapObject.AddComponent<DesktopStandaloneEditorBootstrap>();
        bootstrapObject.AddComponent<DesktopHierarchyPanel>();

        CreateSampleObjects();

        Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void CreateSampleObjects()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Sample Cube";
        cube.transform.position = new Vector3(0f, 0.5f, 0f);

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sample Sphere";
        sphere.transform.position = new Vector3(2f, 0.5f, 0f);

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Sample Capsule";
        capsule.transform.position = new Vector3(-2f, 1f, 0f);
    }
}