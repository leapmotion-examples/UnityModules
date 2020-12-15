/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Packaging {
  using Attributes;

  using DiagDebug = System.Diagnostics.Debug;
  using Debug = UnityEngine.Debug;
  using System.Collections.Generic;

  [CreateAssetMenu(fileName = "Build", menuName = "Build Definition", order = 201)]
  public class BuildDefinition : DefinitionBase {
    private const string BUILD_EXPORT_FOLDER_KEY = "LeapBuildDefExportFolder";
    private const string DEFAULT_BUILD_NAME = "Build.asset";

    [SerializeField]
    protected bool _trySuffixWithGitHash = false;

    public bool useLocalBuildFolderPath = false;
    [DisableIf("useLocalBuildFolderPath", isEqualTo: false)]
    [Tooltip("Relative to Application.dataPath, AKA the Assets folder.")]
    public string localBuildFolderPath = "../Builds/";

#if UNITY_EDITOR
    [Tooltip("The options to enable for this build.")]
    [EnumFlags]
    [SerializeField]
    protected BuildOptions _options = BuildOptions.None;

    [Tooltip("If disabled, the editor's current Player Settings will be used. " +
      "Not all Player Settings are supported. Any unspecified settings are " +
      "inherited from the editor's current Player Settings.")]
    [SerializeField]
    protected bool _useSpecificPlayerSettings = true;

    [System.Serializable]
    public struct BuildPlayerSettings {
      #if UNITY_2018_3_OR_NEWER
      public FullScreenMode fullScreenMode;
      #endif
      public int defaultScreenWidth;
      public int defaultScreenHeight;
      public bool resizableWindow;
      #if !UNITY_2019_1_OR_NEWER
      public ResolutionDialogSetting resolutionDialogSetting;
      #endif
      public static BuildPlayerSettings Default() {
        return new BuildPlayerSettings() {
          #if UNITY_2018_3_OR_NEWER
          fullScreenMode = FullScreenMode.Windowed,
          #endif
          defaultScreenWidth = 800,
          defaultScreenHeight = 500,
          resizableWindow = true,
          #if !UNITY_2019_1_OR_NEWER
          resolutionDialogSetting = ResolutionDialogSetting.HiddenByDefault
          #endif
        };
      }
    }
    [Tooltip("Only used when Use Specific Player Settings is true. " +
      "Not all Player Settings are supported. Any unspecified settings " +
      "are inherited from the editor's current Player Settings.")]
    [SerializeField]
    protected BuildPlayerSettings _playerSettings =
      BuildPlayerSettings.Default();

    [Tooltip("The scenes that should be included in this build, " + "" +
             "in the order they should be included.")]
    [SerializeField]
    protected SceneAsset[] _scenes;

    [Tooltip("The build targets to use for this build definition.")]
    [SerializeField]
    protected BuildTarget[] _targets = { BuildTarget.StandaloneWindows64 };

    public BuildDefinition() {
      _definitionName = "Build";
    }

    public static void BuildFromGUID(string guid) {
      var path = AssetDatabase.GUIDToAssetPath(guid);
      var def = AssetDatabase.LoadAssetAtPath<BuildDefinition>(path);
      def.Build();
    }

    public enum ExitCode {
      NoExportFolderSpecified = 5,
      ExceptionInPostBuildMethod = 6
    }

    public bool crashWithCodeIfBuildFails = false;

    public void Build(string overrideExportFolder = null,
      bool crashWithCodeOnFail = false, bool openFileWindowWhenDone = true)
    {
      crashWithCodeIfBuildFails = crashWithCodeOnFail;

      string exportFolder;
      if (overrideExportFolder != null) {
        exportFolder = overrideExportFolder;
      }
      else if (useLocalBuildFolderPath) {
        exportFolder = Path.Combine(Application.dataPath, localBuildFolderPath);
        if (!Directory.Exists(exportFolder)) {
          try {
            // Simplify any ".." relative-ness.
            exportFolder = new DirectoryInfo(exportFolder).FullName;
          } catch (Exception) {
            UnityEngine.Debug.Log("Could not build " + DefinitionName + " because " +
              "localBuildFolderPath was used and directory " + exportFolder +
              " could not be created.");
          }
        } else {
          // Simplify any ".." relative-ness.
          exportFolder = new DirectoryInfo(exportFolder).FullName;
        }
      }
      else if (!TryGetPackageExportFolder(out exportFolder, promptIfNotDefined: true)) {
        UnityEngine.Debug.LogError("Could not build " + DefinitionName + " because no export folder was chosen.");
        if (crashWithCodeOnFail) {
          EditorApplication.Exit((int)ExitCode.NoExportFolderSpecified);
        }
        return;
      }

      if (_trySuffixWithGitHash) {
        string hash;
        if (tryGetGitCommitHash(out hash)) {
          exportFolder = Path.Combine(exportFolder, DefinitionName + "_" + hash);
        } else {
          UnityEngine.Debug.LogWarning("Failed to get git hash.");
        }
      }

      var fullBuildPath = Path.Combine(exportFolder, DefinitionName);
      string fullExecutablePath = Path.Combine(
        fullBuildPath,
        DefinitionName
      );

      var buildOptions = new BuildPlayerOptions() {
        scenes = _scenes.Where(s => s != null).
                                    Select(s => AssetDatabase.GetAssetPath(s)).
                                    ToArray(),
        options = _options,
      };

      foreach (var target in _targets) {
        buildOptions.target = target;
        buildOptions.locationPathName = fullExecutablePath +
          getFileSuffix(target);
        
        if (_useSpecificPlayerSettings) {
          #if UNITY_2018_3_OR_NEWER
          var origFullscreenMode = PlayerSettings.fullScreenMode;
          #endif
          var origDefaultWidth = PlayerSettings.defaultScreenWidth;
          var origDefaultHeight = PlayerSettings.defaultScreenHeight;
          var origResizableWindow = PlayerSettings.resizableWindow;
          #if !UNITY_2019_1_OR_NEWER
          var origResDialogSetting = PlayerSettings.displayResolutionDialog;
          #endif
          try {
            #if UNITY_2018_3_OR_NEWER
            PlayerSettings.fullScreenMode = _playerSettings.fullScreenMode;
            #endif
            PlayerSettings.defaultScreenWidth = _playerSettings.defaultScreenWidth;
            PlayerSettings.defaultScreenHeight =
            _playerSettings.defaultScreenHeight;
            PlayerSettings.resizableWindow = _playerSettings.resizableWindow;
            #if !UNITY_2019_1_OR_NEWER
            PlayerSettings.displayResolutionDialog = 
              _playerSettings.resolutionDialogSetting;
            #endif

            callPreBuildExtensions(fullBuildPath, crashWithCodeOnFail);
            BuildPipeline.BuildPlayer(buildOptions);
            callPostBuildExtensions(fullBuildPath, crashWithCodeOnFail);
          }
          finally {
            #if UNITY_2018_3_OR_NEWER
            PlayerSettings.fullScreenMode = origFullscreenMode;
            #endif
            PlayerSettings.defaultScreenWidth = origDefaultWidth;
            PlayerSettings.defaultScreenHeight = origDefaultHeight;
            PlayerSettings.resizableWindow = origResizableWindow;
            #if !UNITY_2019_1_OR_NEWER
            PlayerSettings.displayResolutionDialog = origResDialogSetting;
            #endif
          }
        }
        else {
          callPreBuildExtensions(fullBuildPath, crashWithCodeOnFail);
          BuildPipeline.BuildPlayer(buildOptions);
          callPostBuildExtensions(fullBuildPath, crashWithCodeOnFail);
        }

        if (_options.HasFlag(BuildOptions.EnableHeadlessMode)) {
          // The -batchmode flag is the only important part of headless mode
          // for Windows. The EnableHeadlessMode build option only actually has
          // an effect on Linux standalone builds.
          // Here, it's being used to mark the _intention_ of a headless build
          // for Windows builds.
          var text = "\"" + _definitionName + ".exe" + "\" -batchmode";
          var headlessModeBatPath = Path.Combine(Path.Combine(exportFolder,
            _definitionName), "Run Headless Mode.bat");
          File.WriteAllText(headlessModeBatPath, text);
        }
      }

      if (openFileWindowWhenDone) {
        Process.Start(exportFolder);
      }
    }

    private void callPreBuildExtensions(string exportFolder,
      bool crashWithCodeOnFail = false) {
      try {
        foreach (var preBuildKeyValue in BuildExtensions.preBuildExtensionMethods) {
          var attribute = preBuildKeyValue.Key;
          var methodInfo = preBuildKeyValue.Value;
          //Debug.Log("Calling " + methodInfo.Name);
          var methodArgs = new object[] { this, exportFolder };
          methodInfo.Invoke(null, methodArgs);
        }
      }
      catch (System.Exception e) {
        Debug.LogError("Caught exception while calling a pre-build " +
          "extension methods: " + e.ToString());
        if (crashWithCodeOnFail) {
          EditorApplication.Exit((int)ExitCode.ExceptionInPostBuildMethod);
        }
      }
    }

    private void callPostBuildExtensions(string exportFolder,
      bool crashWithCodeOnFail = false)
    {
      try {
        foreach (var postBuildKeyValue in BuildExtensions.postBuildExtensionMethods) {
          var attribute = postBuildKeyValue.Key;
          var methodInfo = postBuildKeyValue.Value;
          var methodArgs = new object[] { this, exportFolder };
          methodInfo.Invoke(null, methodArgs);
        }
      }
      catch (System.Exception e) {
        Debug.LogError("Caught exception while calling post-build " +
          "extension methods: " + e.ToString());
        if (crashWithCodeOnFail) {
          EditorApplication.Exit((int)ExitCode.ExceptionInPostBuildMethod);
        }
      }
    }

    private static string getFileSuffix(BuildTarget target) {
      switch (target) {
        case BuildTarget.StandaloneWindows:
        case BuildTarget.StandaloneWindows64:
          return ".exe";
        case BuildTarget.Android:
          return ".apk";
        case BuildTarget.StandaloneLinux64:
          return ".x86_64";
        default:
          return "";
      }
    }

    private static bool tryGetGitCommitHash(out string hash) {
      try {
        Process process = new Process();

        ProcessStartInfo startInfo = new ProcessStartInfo() {
          WindowStyle = ProcessWindowStyle.Hidden,
          FileName = "cmd.exe",
          Arguments = "/C git log -1",
          WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
          RedirectStandardOutput = true,
          CreateNoWindow = true,
          UseShellExecute = false
        };

        process.StartInfo = startInfo;
        process.Start();

        string result = process.StandardOutput.ReadToEnd();

        var match = Regex.Match(result, @"^commit (\w{40})");
        if (match.Success) {
          hash = match.Groups[1].Value;
          return true;
        } else {
          hash = "";
          return false;
        }
      } catch (Exception e) {
        UnityEngine.Debug.LogException(e);
        hash = "";
        return false;
      }
    }

    [MenuItem("Build/All Apps", priority = 1)]
    public static void BuildAll() {
      foreach (var item in EditorResources.FindAllAssetsOfType<BuildDefinition>()) {
        item.Build();
      }
    }

    [MenuItem("Build/All Apps", priority = 1, validate = true)]
    public static bool ValidateBuildAll() {
      return EditorResources.FindAllAssetsOfType<BuildDefinition>().Length > 0;
    }
#endif

    [System.Serializable]
    public class BuildExtensionData : SerializableDictionary<string, string> { }

    [Header("Extension Data")]
    public BuildExtensionData extensionData;

  }

  [AttributeUsage(validOn: AttributeTargets.Method)]
  public class PreBuildExtension : Attribute { }

  [AttributeUsage(validOn: AttributeTargets.Method)]
  public class PostBuildExtension : Attribute { }

  public static class BuildExtensions {

    private static Dictionary<Attribute, MethodInfo>
      _backingPreBuildExtensionMethods = null;
    public static Dictionary<Attribute, MethodInfo> preBuildExtensionMethods {
      get {
        if (_backingPreBuildExtensionMethods == null) {
          _backingPreBuildExtensionMethods =
            new Dictionary<Attribute, MethodInfo>();
        }
        return _backingPreBuildExtensionMethods;
      }
    }

    private static Dictionary<Attribute, MethodInfo>
      _backingPostBuildExtensionMethods = null;
    public static Dictionary<Attribute, MethodInfo> postBuildExtensionMethods {
      get {
        if (_backingPostBuildExtensionMethods == null) {
          _backingPostBuildExtensionMethods =
            new Dictionary<Attribute, MethodInfo>();
        }
        return _backingPostBuildExtensionMethods;
      }
    }

    #if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    #endif
    private static void InitializeOnLoad() {
      // Scan the assembly for PreBuildExtensions and PostBuildExtensions.
      // var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      // foreach (var assembly in assemblies) {
      //   foreach (var type in assembly.GetTypes()) {
      //     foreach (var method in type.GetMethods()) {
      //       var preBuildExtensionAttr = method.GetCustomAttribute(
      //         typeof(PreBuildExtension));
      //       var postBuildExtensionAttr = method.GetCustomAttribute(
      //         typeof(PostBuildExtension));

      //       if (preBuildExtensionAttr != null) {
      //         preBuildExtensionMethods[preBuildExtensionAttr] = method;
      //       }

      //       if (postBuildExtensionAttr != null) {
      //         postBuildExtensionMethods[postBuildExtensionAttr] = method;
      //       }
      //     }
      //   }
      // }
    }

  }

}
