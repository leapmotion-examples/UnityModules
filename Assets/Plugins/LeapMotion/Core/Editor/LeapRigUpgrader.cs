/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  using UnityObject = UnityEngine.Object;

  /// <summary>
  /// Can scan the currently open scene and detect instances of an old Leap Rig
  /// hierarchy, render a GUI describing the old rig, and provides functionality for
  /// automatically upgrading the rig.
  /// </summary>
  [InitializeOnLoad]
  public static class LeapRigUpgrader {

    #region Events

    public enum SceneScanStatus { NotScanned, ContainsOldRigs, NoOldRigsFound }
    private static SceneScanStatus _currentSceneScanStatus = SceneScanStatus.NotScanned;
    public static SceneScanStatus currentSceneScanStatus {
      get {
        return _currentSceneScanStatus;
      }
    }

    [InitializeOnLoadMethod]
    private static void Init() {
      EditorSceneManager.sceneOpened -= onSceneOpened;
      EditorSceneManager.sceneOpened += onSceneOpened;

      #if UNITY_2018_1_OR_NEWER
      EditorApplication.hierarchyChanged -= onHierarchyChanged;
      EditorApplication.hierarchyChanged += onHierarchyChanged;
      #else
      EditorApplication.hierarchyWindowChanged -= onHierarchyChanged;
      EditorApplication.hierarchyWindowChanged += onHierarchyChanged;
      #endif

      _currentSceneScanStatus = SceneScanStatus.NotScanned;
    }

    private static void onSceneOpened(Scene scene, OpenSceneMode openSceneMode) {
      _currentSceneScanStatus = SceneScanStatus.NotScanned;
      ClearScanData();
    }
    private static void onHierarchyChanged() {
      _currentSceneScanStatus = SceneScanStatus.NotScanned;
      ClearScanData();
    }

    #endregion

    #region GUI Drawing

    #region Convenience Properties

    public static GUIStyle boldLabel {
      get { return EditorStyles.boldLabel; }
    }

    private static GUIStyle _backingWrapLabel;
    public static GUIStyle wrapLabel {
      get {
        if (_backingWrapLabel == null) {
          _backingWrapLabel = new GUIStyle(EditorStyles.label);
          _backingWrapLabel.wordWrap = true;
        }
        return _backingWrapLabel;
      }
    }

    #endregion

    #region Project Check GUI

    /// <summary>
    /// Called by LeapProjectsChecks to render a box in the project checks tab,
    /// describing the upgrader and linking to it.
    /// </summary>
    [LeapProjectCheck("Core", 0)]
    private static bool drawRigUpgraderCheckGUI() {
      EditorGUILayout.LabelField("Leap Rig Upgrader", boldLabel);
      
      EditorGUILayout.LabelField(
        "If you have upgraded from Core version 4.3.4 or earlier to Core version 4.4 or "
      + "later, you can check the currently-open scene for out-of-date rigs. Instances of "
      + "older Leap Rig prefabs can be automatically upgraded to match the updated rig, "
      + "which is much simpler.",
        wrapLabel);

      EditorGUILayout.Space();

      drawScanCurrentSceneButtonAndStatus();

      EditorGUILayout.Space();

      return true;
    }

    private static void drawScanCurrentSceneButtonAndStatus() {

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button(new GUIContent("Scan Current Scene"),
              GUILayout.MaxWidth(200f))) {
          var oldRigsDetected = ScanCurrentSceneForOldLeapRigs();
          if (oldRigsDetected) {
            _currentSceneScanStatus = SceneScanStatus.ContainsOldRigs;
            if (LeapUnityWindow.isWindowOpen) {
              LeapUnityWindow.ShowTab(1);
            }
          }
          else {
            _currentSceneScanStatus = SceneScanStatus.NoOldRigsFound;
          }
        }

        string scanText;
        switch (_currentSceneScanStatus) {
          case SceneScanStatus.ContainsOldRigs:
            scanText = "Scene contained old Leap rigs as of the last scan.";
            break;
          case SceneScanStatus.NoOldRigsFound:
            scanText = "Scene contained no old Leap rigs as of the last scan.";
            break;
          case SceneScanStatus.NotScanned:
          default:
            scanText = "Scene not yet scanned.";
            break;
        }
        EditorGUILayout.LabelField(scanText);
      }
    }

    #endregion

    #region Upgrader GUI

    private static Vector2 _scrollPosition = Vector2.zero;
    private static Dictionary<OldRigHierarchy, bool> _rigFoldoutStates
      = new Dictionary<OldRigHierarchy, bool>();
    private static OldRigHierarchy.UpgradeOptions _upgradeOptions
      = new OldRigHierarchy.UpgradeOptions();

    public static void DrawUpgraderGUI() {
      var origIndent = EditorGUI.indentLevel;
      EditorGUI.indentLevel = 0;

      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
        drawRigUpgraderCheckGUI();
      }

      EditorGUILayout.Space();
      EditorGUILayout.Space();

      drawRigScanResultsGUI();

      var splitterStyle = new GUIStyle(LeapUnityWindow.windowSkin.box);
      splitterStyle.fixedHeight = 1f;
      splitterStyle.stretchWidth = true;
      splitterStyle.margin = new RectOffset(0, 0, 0, 0);
      GUILayout.Box("", splitterStyle);

      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
        GUILayout.ExpandWidth(true));

      foreach (var oldRig in _oldRigs) {
        EditorGUILayout.Space();
        drawOldRigUpgradeGUI(oldRig);
      }

      EditorGUILayout.EndScrollView();

      EditorGUI.indentLevel = origIndent;
    }

    private static void drawRigScanResultsGUI() {
      var safetyWarning = "To be safe, you should make sure you've backed up your scene "
        + "before upgrading.";
      EditorGUILayout.LabelField(safetyWarning, wrapLabel);
      
      var singleRig = _oldRigs.Count == 1;
      EditorGUILayout.LabelField(
        "Detected " + _oldRigs.Count + " old rig"
        + (singleRig ? "" : "s") + ". "
        , wrapLabel);
    }

    private static void drawOldRigUpgradeGUI(OldRigHierarchy oldRig) {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {

        var rigDescFoldoutState = false;
        if (!_rigFoldoutStates.TryGetValue(oldRig, out rigDescFoldoutState)) {
          _rigFoldoutStates[oldRig] = false;
        }
        string foldoutName = getName(oldRig.rigTransform);
        if (!rigDescFoldoutState) {
          foldoutName += " (click to expand upgrade details)";
        }
        else {
          foldoutName += " (click to hide upgrade details)";
        }
        using (new EditorGUILayout.HorizontalScope()) {
          _rigFoldoutStates[oldRig] = EditorGUILayout.Foldout(_rigFoldoutStates[oldRig],
            foldoutName, toggleOnLabelClick: true);

          EditorGUI.BeginDisabledGroup(true);
          EditorGUILayout.ObjectField(oldRig.rigTransform, typeof(Transform), true,
            GUILayout.ExpandWidth(false));
          EditorGUI.EndDisabledGroup();
        }

        if (_rigFoldoutStates[oldRig]) {
          EditorGUILayout.Space();
          
          drawOldRigCameraGUI(oldRig);
          
          drawOldRigLeapSpaceGUI(oldRig);
          
          drawOldRigLHCGUI(oldRig);
        }

        if (GUILayout.Button("Update Rig")) {
          oldRig.Upgrade(_upgradeOptions);

          EditorApplication.delayCall += () => {
            ScanCurrentSceneForOldLeapRigs();
          };
        }
      }
    }

    #region Old Rig Component Drawing

    private static void drawOldRigCameraGUI(OldRigHierarchy oldRig) {
      EditorGUI.indentLevel = 1;
      drawRigItem("Rig Camera", oldRig.cameraData.cameraComponent,
        typeof(Camera));
      EditorGUI.indentLevel = 2;

      var camera_enableDepthBuffer = oldRig.cameraData.enableDepthBuffer;
      if (camera_enableDepthBuffer != null) {
        EditorGUILayout.Space();
        drawRigItem("Remove Enable Depth Buffer", camera_enableDepthBuffer,
          typeof(EnableDepthBuffer));
        EditorGUILayout.LabelField(
          "The EnableDepthBuffer script used to be a part of Leap camera rigs by "
        + "default but can cause shader issues in certain situations. If you're "
        + "not sure you need this component, you should remove it.",
          wrapLabel);
        drawYesNoToggle(
          yesOption: "Remove the EnableDepthBuffer script from the camera.",
          noOption: "Don't remove the EnableDepthBuffer script, I'm using it for "
                  + "something.",
          yesOptionFlag: ref _upgradeOptions.camera_removeEnableDepthBufferScript
        );
      }


      if (oldRig.isImageRig) {
        EditorGUILayout.Space();
        var rightEyeCamera = oldRig.imageRigData.rightEyeCamData.cameraComponent;
        if (rightEyeCamera != null) {
          drawRigItem("Remove Right Eye Camera", rightEyeCamera, typeof(Camera));
          EditorGUILayout.LabelField(
            "Separate cameras for the left and right eyes are no longer necessary "
          + "for IR viewer rigs.",
            wrapLabel
          );
          drawYesNoToggle(
            yesOption: "Remove this camera object.",
            noOption: "Don't remove this camera, I'm using it for something else.",
            yesOptionFlag: ref _upgradeOptions.camera_removeRightEyeCamera
          );
        }
      }
      else {
        EditorGUILayout.Space();
        var primaryLeapEyeDislocator = oldRig.cameraData.leapEyeDislocator;
        if (primaryLeapEyeDislocator != null) {
          drawRigItem("Remove Leap Eye Dislocator", primaryLeapEyeDislocator,
            typeof(LeapEyeDislocator));
          EditorGUILayout.LabelField(
            "LeapEyeDislocator was found on this object, but it was not detected "
          + "to be an IR viewer rig. LeapEyeDislocator is not necessary "
          + "on non-IR viewer rigs.",
            wrapLabel
          );
          drawYesNoToggle(
            yesOption: "Remove the LeapEyeDislocator script from the camera.",
            noOption: "Don't remove LeapEyeDislocator.",
            yesOptionFlag: ref _upgradeOptions.camera_removeLeapEyeDislocator
          );
        }
      }

      EditorGUILayout.Space();
    }

    private static void drawOldRigLeapSpaceGUI(OldRigHierarchy oldRig) {
      EditorGUILayout.Space();
      EditorGUI.indentLevel = 1;
      drawRigItem("LeapSpace Transform (to be deleted.)",
        oldRig.leapSpaceData.leapSpaceTransform, typeof(Transform));
      EditorGUI.indentLevel = 2;
      EditorGUILayout.LabelField(
        "The LeapSpace transform and its temporal warping functionality have been "
      + "migrated into the LeapXRServiceProvider for XR rigs. As a part of this upgrade, "
      + "this transform (including any components on it) will be deleted.",
        wrapLabel);

      var leapSpace_nonLHCChildren
                = oldRig.leapSpaceData.nonLHCChildTransforms;
      if (leapSpace_nonLHCChildren.Count > 0) {
        EditorGUILayout.Space();
        drawMergeUpwardsSetting(
          originalTransformName: "LeapSpace",
          numExtraChildren: leapSpace_nonLHCChildren.Count,
          mergeExtraChildrenUpwardFlag:
            ref _upgradeOptions.leapSpace_mergeExtraChildrenUpward
        );
      }

      EditorGUILayout.Space();
    }

    private static void drawOldRigLHCGUI(OldRigHierarchy oldRig) {
      EditorGUILayout.Space();
      EditorGUI.indentLevel = 1;
      drawRigItem("LeapHandController Transform (to be deleted.)",
        oldRig.lhcData.leapServiceProvider, typeof(Transform));
      EditorGUI.indentLevel = 2;
      EditorGUILayout.LabelField(
        "The LeapHandController transform and its LeapHandController, "
      + "LeapServiceProvider, and HandPool components have been simplified.",
        wrapLabel);
      
      EditorGUILayout.LabelField(
        "The LeapServiceProvider will be removed. In its place, a "
      + "LeapXRServiceProvider will be added directly to the Camera. The "
      + "LeapXRServiceProvider handles temporal warping (latency correction) "
      + "automatically for XR rigs. Applicable settings will be transferred to "
      + "the LeapXRServiceProvider.", wrapLabel);
      
      EditorGUILayout.LabelField(
        "The HandPool and HandController objects have been combined into a single "
      + "HandModelManager script, which should be moved to the parent transform of the "
      + "hand models. This clarifies its separation from the hand data pipeline "
      + "as one of (potentially many) consumers of hand data from a device.",
      wrapLabel);

      var handModelParent = oldRig.handModelParentTransform;
      var handModelManager = oldRig.lhcData.handModelManager;
      if (handModelParent != null && handModelManager != null) {
        EditorGUILayout.Space();
        drawRigItem("Move the HandModelManager...", handModelManager, typeof(HandModelManager));
        drawRigItem("...to the hand model parent transform", handModelParent, typeof(Transform));
        EditorGUILayout.LabelField(
          "The HandModelManager will be moved to the " + handModelParent.name
        + " transform because it is the parent transform of the hand models.",
          wrapLabel);
      }
      else {
        // decided against adding a HandModelManager if none already exists.
      }

      var lhc_extraComponents = oldRig.lhcData.extraComponents;
      if (lhc_extraComponents != null && lhc_extraComponents.Count > 0) {
        EditorGUILayout.Space();
        drawRigItem("Extra Components Detected", null, null);

        var sb = new StringBuilder();
        sb.Append("The ");
        for (int i = 0; i < lhc_extraComponents.Count; i++) {
          var component = lhc_extraComponents[i];
          sb.Append(component.GetType().Name);
          if (i != lhc_extraComponents.Count - 1) sb.Append(", ");
          if (i == lhc_extraComponents.Count - 2) sb.Append("and ");
        }
        var isPlural = lhc_extraComponents.Count != 1;
        if (isPlural) {
          sb.Append(" components (and references to them) will be migrated to the camera "
            + "transform.");
        }
        else {
          sb.Append(" component (and references to it) will be migrated to the camera "
            + "transform.");
        }

        EditorGUILayout.LabelField(
          sb.ToString(),
          wrapLabel);
      }

      var lhc_extraChildren = oldRig.lhcData.extraChildTransforms;
      if (lhc_extraChildren.Count > 0) {
        EditorGUILayout.Space();
        drawMergeUpwardsSetting(
          originalTransformName: "LeapHandController",
          numExtraChildren: lhc_extraChildren.Count,
          mergeExtraChildrenUpwardFlag: ref _upgradeOptions.lhc_mergeExtraChildrenUpward
        );
      }
    }

    #endregion

    #region Drawing Helper Methods
    
    private static void drawRigItem(string label, UnityObject obj, Type objType) {
      using (new EditorGUILayout.HorizontalScope()) {
        EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(true));
        if (obj != null) {
          EditorGUI.BeginDisabledGroup(true);
          EditorGUILayout.ObjectField(obj, objType, true, GUILayout.ExpandWidth(false));
          EditorGUI.EndDisabledGroup();
        }
      }
    }

    private static void drawMergeUpwardsSetting(string originalTransformName,
                                                int numExtraChildren,
                                                ref bool mergeExtraChildrenUpwardFlag) {
      var isPlural = numExtraChildren == 1;
      EditorGUILayout.LabelField(
        numExtraChildren
      + (isPlural ? " transforms are attached as children "
                  : " transform is attached as a child")
      + " to the " + originalTransformName + " transform, which is deprecated and will be "
      + "removed.",
        wrapLabel);
      drawYesNoToggle(
        yesOption: "Move these objects to be children of the camera.",
        noOption: "Remove these objects.",
        yesOptionFlag: ref mergeExtraChildrenUpwardFlag
      );
    }

    private static void drawYesNoToggle(string yesOption, string noOption,
                                        ref bool yesOptionFlag) {
      yesOptionFlag = EditorGUILayout.ToggleLeft(yesOption, yesOptionFlag);

      bool temp = EditorGUILayout.ToggleLeft(noOption, !yesOptionFlag);
      yesOptionFlag = !temp;
    }

    private static void drawObjField(UnityObject obj, Type objType) {
      EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.ObjectField(obj, objType, true);
      EditorGUI.EndDisabledGroup();
    }

    private static string getName(Transform t) {
      if (t == null) return "";
      return t.name;
    }

    #endregion

    #endregion

    #endregion

    #region Upgrader Memory

    private static List<Transform> _backingTransformsBuffer = null;
    private static List<Transform> _transformsBuffer {
      get {
        if (_backingTransformsBuffer == null)
          _backingTransformsBuffer = new List<Transform>();
        return _backingTransformsBuffer;
      }
    }

    private static List<OldRigHierarchy> _backingOldRigs = null;
    private static List<OldRigHierarchy> _oldRigs {
      get {
        if (_backingOldRigs == null) {
          _backingOldRigs = new List<OldRigHierarchy>();
        }
        return _backingOldRigs;
      }
    }

    #endregion

    #region Scan Methods

    /// <summary>
    /// Clears any data from the most recent scan.
    /// </summary>
    public static void ClearScanData() {
      _oldRigs.Clear();
    }

    /// <summary>
    /// Returns true if old rigs were (potentially) found. Returns false if none were
    /// found. If this method returns true, you can Show() the window instance to display
    /// scanned details and auto-upgrade options to the user.
    /// </summary>
    public static bool ScanCurrentSceneForOldLeapRigs() {
      ClearScanData();

      var currentScene = SceneManager.GetActiveScene();
      var rootObjs = currentScene.GetRootGameObjects();
      foreach (var rootObj in rootObjs) {
        rootObj.transform.GetComponentsInChildren(_transformsBuffer);
        foreach (var transform in _transformsBuffer) {
          var oldRig = OldRigHierarchy.DetectFor(transform);
          if (oldRig != null) {
            _oldRigs.Add(oldRig);
          }
        }
        _transformsBuffer.Clear();
      }

      return _oldRigs.Count > 0;
    }

    #endregion

  }

  /// <summary>
  /// Description of an old Leap VR rig from before Core version 4.4.
  /// 
  /// Use DetectFor(transform) to attempt to detect an
  /// instance of an old Leap rig with its root at that Transform. If the rig is detected
  /// and upgradeable, also provides a call to upgrade the rig in an Undo-able way.
  /// </summary>
  /// <remarks>
  /// This is essentially the hierarchy that we're matching to build this object:
  /// 
  /// Standard VR rig (old)
  /// "LMHeadMountedRig" - soft-contain VRHeightOffset -> XRHeightOffset (automatic)
  ///   |
  ///   |- "CenterEyeAnchor" - soft-contain LeapVRCameraControl -> LeapEyeDislocator,
  ///   |    |                 soft-contain Camera,
  ///   |    |                 soft-contain EnableDepthBuffer -> remove
  ///   |    |
  ///   |    `- "LeapSpace" - soft-contain LeapVRTemporalWarping -> Missing Script
  ///   |         |
  ///   |         `- "LeapHandController" - contain LeapHandController -> Missing Script,
  ///   |                                   contain LeapServiceProvider,
  ///   |                                   soft-contain Hand Model Manager
  ///   `- "HandModels"
  ///        |
  ///        `- X handModelTransforms, unmodified.
  ///        
  /// Alternatively, the old rig could be an old pass-through camera rig, which is
  /// more complicated because it has _two_ separate eye cameras:
  /// 
  /// Standard pass-through rig (old)
  /// "LMHeadMountedRig" - soft-contain VRHeightOffset -> XRHeightOffset (automatic)
  ///   |
  ///   |- "LeftEyeAnchor" - soft-contain LeapVRCameraControl -> LeapEyeDislocator,
  ///   |    |               soft-contain Camera,
  ///   |    |               soft-contain EnableDepthBuffer -> remove,
  ///   |    |               contain LeapImageRetriever
  ///   |    |
  ///   |    `- "LeapSpace" - soft-contain TempWarp -> Missing Script
  ///   |         |
  ///   |         |- "LeapHandController" - contain LeapHandController -> Missing Script,
  ///   |         |                         contain LeapServiceProvider
  ///   |         |                         soft-contain Hand Pool
  ///   |         `- "QuadBackground" -> as 'extra' Transform, moved to child of Camera.
  ///   |
  ///   |- "RightEyeAnchor" - contain LeapVRCameraControl -> LeapEyeDislocator
  ///   |                     ^- this on a sibling of the Leap Image Retriever
  ///   |                        indicates extremely high certainty of Image Rig.
  ///   |                     soft-contain Camera,
  ///   |                     soft-contain Enable Depth Buffer -> Missing Script
  ///   |
  ///   `- "HandModels"
  ///        |
  ///        `- X handModelTransforms, unmodified.
  /// 
  /// </remarks>
  public class OldRigHierarchy {

    #region Status State

    /// <summary>
    /// Whether this rig can be upgraded. Set after a DetectFor(transform) call.
    /// </summary>
    public bool isUpgradeableOldRig = false;

    /// <summary> 
    /// Whether the scan data indicates the scanned transform is potentially an old
    /// Leap rig. Set after a DetectFor(transform) call.
    /// </summary>
    public bool detectedAsPotentialRig = false;

    /// <summary>
    /// If the rig was detected as a potential rig but not upgradeable, this is the
    /// reason the rig cannot be upgraded. Set after a DetectFor(transform) call.
    /// </summary>
    public string reasonRigCannotBeUpgraded
        = "Rig data has not been collected. Call OldRigHierarchy.DetectFor(transform) to "
        + "scan.";

    #endregion

    #region Scanned Rig Data

    /// <summary> The root transform of the old rig hierarchy. </summary>
    public Transform rigTransform;

    /// <summary>
    /// The parent of hand models in this rig that will receive the HandModelManager.
    /// </summary>
    public Transform handModelParentTransform;

    #region Rig Camera Data

    /// <summary>
    /// Old camera objects that weren't modified at all would have had a 
    /// LeapEyeDislocator (formerly LeapVRCameraControl), a Camera component, and an
    /// EnableDepthBuffer component.
    /// </summary>
    public class OldCameraData {
      public Transform cameraTransform;
      public LeapEyeDislocator leapEyeDislocator;
      public EnableDepthBuffer enableDepthBuffer;
      public Camera cameraComponent;

      public bool containsCameraComponent { get { return cameraComponent != null; } }
    }
    /// <summary>
    /// The driven camera transform and component info of the rig hierarchy, or that of
    /// the LEFT eye if this rig was an IR viewer rig specifically.
    /// </summary>
    public OldCameraData cameraData;

    #endregion

    #region Other Rig Data

    public class OldLeapSpaceData {
      public Transform leapSpaceTransform;

      /// <summary>
      /// Some rigs, like the old image rig or modifications to the old standard rig,
      /// may have non-LeapHandController transforms as children of the LeapSpace
      /// object. These objects should be moved to be children of the Camera directly.
      /// </summary>
      public List<Transform> nonLHCChildTransforms;
    }
    public OldLeapSpaceData leapSpaceData;

    public class OldLeapHandControllerData {
      public Transform lhcTransform;
      public LeapServiceProvider leapServiceProvider;
      public HandModelManager handModelManager;
      public List<Transform> extraChildTransforms;
      public List<Component> extraComponents;
    }
    public OldLeapHandControllerData lhcData;

    public class ImageRigData {
      public OldCameraData rightEyeCamData;
    }
    public ImageRigData imageRigData;
    public bool isImageRig { get { return imageRigData != null; } }

    #endregion

    #endregion

    #region Scanning

    /// <summary>
    /// Detects if the argument Transform is the _rig root_ of an old Leap rig,
    /// modifying the passed-in old rig hierarchy description. Returns true if the
    /// rig was detected as an old rig, false otherwise.
    /// </summary>
    public static bool DetectFor(Transform transform, OldRigHierarchy toFill) {
      if (transform == null) return false;

      var didDetectOldRig = false;
      var scannedRig = toFill;
      scannedRig.rigTransform = transform;

      // Scan for the camera transform in immediate children.
      var camDataBuffer = new List<OldCameraData>();
      camDataBuffer.Clear();
      detectPotentialOldRigCameras(transform, camDataBuffer);

      if (camDataBuffer.Count == 0) {
        // No cameras found; this is not an upgradeable old rig.
        return false;
      }
      else {
        // We have a list of children that contain Camera components and potentially
        // other Leap rig-related data, but the check was intentionally loose.

        // Some practical cases to consider, if there was only one camera found:
        // (1) It could be a standard non-image rig, and so is upgradeable!
        // (2) It could be an image rig but with one eye removed (weird).
        //     (2a) If the left eye was removed, the rig is non-upgradeable.
        //     (2b) If the right eye was removed, this rig will look identical to a
        //          non-image rig as in (1) and be upgradeable.
        // (3) It could just be an object with a Camera component beneath it, and be
        //     unrelated to a Leap VR rig. (Looks similar to 2a but with fewer
        //     Leap-related or Missing components.)
        //
        // To confirm it is NOT (3) or (2a), we need to check its children to
        // find the LeapSpace and LeapHandController.

        // Alternatively, if there were two potential cameras found:
        // (1) Optimistically, this could indicate it's an old image rig with a left
        //     and right eye.
        // (2) It could be a stranger construction, like the rig for a custom VR
        //     integration.
        // (3) It could be a standard single-camera non-image rig with another Camera
        //     utilized for some other, distinct purpose.
        // (4) It could just be a transform with two cameras as children and not be
        //     Leap-related!

        // To be an upgradeable Leap Rig, image or otherwise, one of the cameras we've
        // found must have a LeapServiceProvider two transforms down (we don't actually
        // care what transforms are named, since someone could change them without
        // really affecting the structure of the rig):
        //
        // (Camera)
        //   |
        //   `- LeapSpace - Missing Component // optional, formerly LeapVRTemporalWarping
        //        |
        //        `- LeapHandController - LeapServiceProvider // required
        //                                HandModelManager    // optional
        //
        // If there is no LeapServiceProvider two-transforms-down, the rig has been
        // modified too heavily and is no longer auto-upgradeable.
        //
        // Importantly, this condition will hold true if the rig is a non-image rig OR
        // an image rig, and false if we are going to refuse the upgrade. What's more,
        // if this condition holds true, we will be able to collect all the information
        // we need from the rig after scanning through each camera.

        var primaryCamData     = (OldCameraData)null;
        var firstLeapSpaceData = (OldLeapSpaceData)null;
        var firstLHCData       = (OldLeapHandControllerData)null;

        // "Secondary" cameras in the rig are the cameras that don't have LeapSpace or
        // LeapHandController objects (or weren't searched for these if we already
        // found a primary camera).
        // These need to be searched after we find the primary camera to find the
        // right -eye camera in the image rig case.
        var secondaryCamDatas = new List<OldCameraData>();

        for (int i = 0; i < camDataBuffer.Count; i++) {
          var camData = camDataBuffer[i];
          var expectingFirstLeapSpaceData = firstLeapSpaceData == null;

          var camTransform = camData.cameraTransform;
          foreach (var child in camTransform.GetChildren()) {
            var expectingFirstLHCData = firstLHCData == null;

            // We're looking for the child that is the LeapSpace transform, only
            // known implicitly by looking at _its_ children.
            foreach (var grandchild in child.GetChildren()) {
              var leapServiceProvider = grandchild.GetComponent<LeapServiceProvider>();

              // We're going to assume the first LeapServiceProvider we find is
              // indicative of an old, upgradeable rig, and construct out the
              // definition from there.
              if (leapServiceProvider != null) {
                firstLHCData = new OldLeapHandControllerData();
                firstLHCData.lhcTransform = grandchild;
                firstLHCData.leapServiceProvider = leapServiceProvider;

                firstLHCData.extraChildTransforms = new List<Transform>();
                foreach (var greatGrandchild in grandchild.GetChildren()) {
                  firstLHCData.extraChildTransforms.Add(greatGrandchild);
                }

                firstLHCData.handModelManager
                  = grandchild.GetComponent<HandModelManager>();

                var components = grandchild.GetComponents<Component>();
                foreach (var component in components) {
                  if (component == null) continue; // ignore "Missing Script"
                  if (component == firstLHCData.lhcTransform) continue;
                  if (component == firstLHCData.leapServiceProvider) continue;
                  if (component == firstLHCData.handModelManager) continue;

                  if (firstLHCData.extraComponents == null) {
                    firstLHCData.extraComponents = new List<Component>();
                  }
                  firstLHCData.extraComponents.Add(component);
                }

                break;
              }
            }

            if (firstLHCData != null && expectingFirstLHCData) {
              firstLeapSpaceData = new OldLeapSpaceData();
              firstLeapSpaceData.leapSpaceTransform = child;
              firstLeapSpaceData.nonLHCChildTransforms = new List<Transform>();
              foreach (var leapSpaceChild in child.GetChildren()) {
                if (leapSpaceChild == firstLHCData.lhcTransform) continue;
                firstLeapSpaceData.nonLHCChildTransforms.Add(leapSpaceChild);
              }
              break;
            }
          }

          if (firstLeapSpaceData != null && expectingFirstLeapSpaceData) {
            // This is our primary camera.
            primaryCamData = camData;
          }
          else {
            // Either we haven't found a LeapSpace yet or we've already found our first.
            // So this is a secondary camera.
            secondaryCamDatas.Add(camData);
          }
        }

        // At the end of this process, we must have all three of these things in order
        // to be an upgradeable rig:
        // (1) a Primary camera, containing
        // (2) a LeapSpace transform, containing
        // (3) a LeapHandController transform

        // If we don't have these things, we can't upgrade
        // the rig! Not having these things, in fact, indicates it's probably not an
        // old Leap rig at all.
        if (primaryCamData == null) {
          scannedRig.isUpgradeableOldRig = false;
          scannedRig.cameraData = null;
          scannedRig.detectedAsPotentialRig = false;
          scannedRig.leapSpaceData = null;
          scannedRig.lhcData = null;
          scannedRig.handModelParentTransform = null;
          scannedRig.reasonRigCannotBeUpgraded = "Scanned transform does not look like "
            + "an old Leap rig.";
          return false;
        }
        else {
          // OK, we can upgrade this rig. Fill it with any remaining data we need.
          didDetectOldRig = true;

          scannedRig.cameraData = primaryCamData;
          scannedRig.detectedAsPotentialRig = true;
          scannedRig.leapSpaceData = firstLeapSpaceData;
          scannedRig.lhcData = firstLHCData;
          scannedRig.isUpgradeableOldRig = true;
          scannedRig.reasonRigCannotBeUpgraded = "";

          // Since we've come this far, we need to attempt to find the hand model
          // parent transform. This will be the first child transform of the rig that
          // contains one or more HandModelBase components.
          // Not having one of these doesn't preclude updating the rig, since we can
          // just make a new transform for this purpose.
          var firstReasonableHandModelTransform = scannedRig.rigTransform.GetChildren()
              .Query().Where(child =>
                child.GetChildren().Query()
                  .Where(gc => gc.GetComponent<HandModelBase>() != null).Count() > 0)
              .FirstOrDefault();
          scannedRig.handModelParentTransform = firstReasonableHandModelTransform;

          // If there is a LeapImageRetriever on the camera, this could well be an
          // old Leap image rig, with two eyes.
          var leapImageRetriever = primaryCamData.cameraTransform
              .GetComponent<LeapImageRetriever>();
          var rightEyeCamData = (OldCameraData)null;
          if (leapImageRetriever != null) {
            // If there is a secondary camera with a LeapEyeDislocator component, we'll
            // assume that's the other (right) eye.
            rightEyeCamData = secondaryCamDatas.Query()
              .Where(cd => cd.cameraTransform.GetComponent<LeapEyeDislocator>() != null)
              .FirstOrDefault();

            if (rightEyeCamData != null) {
              scannedRig.imageRigData = new ImageRigData();
              scannedRig.imageRigData.rightEyeCamData = rightEyeCamData;
            }
          }
        }
      }

      return didDetectOldRig;
    }

    /// <summary>
    /// Detects **potential** old rig cameras among rigTransform's direct children and
    /// fills the detectedCameras list with data for each possible rig camera detected.
    /// </summary>
    private static void detectPotentialOldRigCameras(Transform rigTransform,
                                                     List<OldCameraData> detectedCameras) {
      foreach (var child in rigTransform.GetChildren()) {
        var eyeDislocator = child.GetComponent<LeapEyeDislocator>();
        var camera = child.GetComponent<Camera>();
        var leapXRProvider = child.GetComponent<LeapXRServiceProvider>();
        var enableDepthBuffer = child.GetComponent<EnableDepthBuffer>();

        if (camera == null) {
          // Definitely no camera detected for this child.
          continue;
        }
        else if (leapXRProvider != null) {
          // A camera with a LeapXRServiceProvider on it is definitely not an old rig!
          // This is probably an up-to-date rig.
          continue;
        }
        else {
          // OK, this could be an old rig camera. Get as much data as possible about it.
          var oldCamData = new OldCameraData();
          oldCamData.cameraTransform = child;
          oldCamData.cameraComponent = camera;
          oldCamData.leapEyeDislocator = eyeDislocator;
          oldCamData.enableDepthBuffer = enableDepthBuffer;

          // Add it to the detected cameras list. We'll process it more later.
          detectedCameras.Add(oldCamData);
        }
      }
    }

    /// <summary>
    /// Detects if the argument Transform is the _rig root_ of an old Leap rig, in
    /// which case an OldRigHierarchy description is returned, otherwise this method
    /// returns null.
    /// </summary>
    public static OldRigHierarchy DetectFor(Transform transform) {
      var scannedRig = new OldRigHierarchy();
      var didDetectOldRig = DetectFor(transform, scannedRig);
      if (!didDetectOldRig) return null;
      return scannedRig;
    }
    
    private static void fillMissingComponentIndices(Transform transform, List<int> toFill) {
      toFill.Clear();
      var tempList = new List<Component>();
      transform.GetComponents(tempList);
      for (int i = 0; i < tempList.Count; i++) {
        var component = tempList[i];
        if (component == null) {
          toFill.Add(i);
        }
      }
    }

    #endregion

    #region Upgrading

    public class UpgradeOptions {
      public bool camera_removeEnableDepthBufferScript = true;
      public bool camera_removeRightEyeCamera = true;
      public bool camera_removeLeapEyeDislocator = true;
      public bool leapSpace_removeMissingScripts = true;
      public bool leapSpace_mergeExtraChildrenUpward = true;
      public bool lhc_removeMissingScripts = true;
      public bool lhc_mergeExtraChildrenUpward = true;
    }

    public void Upgrade(UpgradeOptions options) {
      if (options == null) { options = new UpgradeOptions(); }

      if (!this.isUpgradeableOldRig) {
        throw new InvalidOperationException(
          "Aborting upgrade attempt: This rig is not upgradeable.");
      }

      // Rig transform: unmodified.

      // Camera transform, then LeapSpace transform, then LHC transform.
      if (cameraData == null) {
        Debug.LogError("No camera data available for this rig during upgrade attempt.");
      }
      else {
        var enableDepthBufferScript = this.cameraData.enableDepthBuffer;
        if (options.camera_removeEnableDepthBufferScript
            && enableDepthBufferScript != null) {
          Undo.DestroyObjectImmediate(enableDepthBufferScript);
        }

        var eyeDislocator = this.cameraData.leapEyeDislocator;
        if (options.camera_removeLeapEyeDislocator
            && eyeDislocator != null) {
          Undo.DestroyObjectImmediate(eyeDislocator);
        }
        
        if (this.isImageRig) {
          var rightEyeCamData = this.imageRigData.rightEyeCamData;
          if (rightEyeCamData != null) {
            var rightEyeTransform = rightEyeCamData.cameraTransform;
            if (options.camera_removeRightEyeCamera
                && rightEyeTransform != null) {
              Undo.DestroyObjectImmediate(rightEyeTransform.gameObject);
            }
          }
          if (cameraData.cameraComponent != null
              && cameraData.cameraComponent.stereoTargetEye == StereoTargetEyeMask.Left) {
            cameraData.cameraComponent.stereoTargetEye = StereoTargetEyeMask.Both;
          }
        }

        var cameraTransform = cameraData.cameraTransform;

        if (leapSpaceData == null) {
          Debug.LogError("No LeapSpace data available for this rig during upgrade "
            + "attempt.");
        }
        else {
          var leapSpaceExtraChildren = leapSpaceData.nonLHCChildTransforms;
          if (options.leapSpace_mergeExtraChildrenUpward
              && leapSpaceExtraChildren != null) {
            foreach (var extraChild in leapSpaceExtraChildren.Query().Where(t => t != null)) {
              Undo.SetTransformParent(extraChild, cameraTransform,
                "Move LeapSpace child to Camera");
            }
          }

          var leapSpaceTransform = leapSpaceData.leapSpaceTransform;

          if (lhcData == null) {
            Debug.LogError("No LeapHandController data available for this rig during "
            + "upgrade attempt.");
          }
          else {
            var lhcExtraChildren = lhcData.extraChildTransforms;
            if (options.lhc_mergeExtraChildrenUpward
                && lhcExtraChildren != null) {
              foreach (var extraChild in lhcExtraChildren.Query().Where(t => t != null)) {
                Undo.SetTransformParent(extraChild, cameraTransform,
                  "Move LHC child to Camera");
              }
            }

            // Migrate the HandPool (now HandModelManager) to the hand model parent 
            // transform if we could find one.
            var handModelManager = lhcData.handModelManager;
            var newHandModelManager = (HandModelManager)null;
            if (handModelManager != null && this.handModelParentTransform != null) {
              newHandModelManager
                = Undo.AddComponent<HandModelManager>(handModelParentTransform.gameObject);
              EditorUtility.CopySerialized(handModelManager, newHandModelManager);

              EditorUtils.ReplaceSceneReferences(handModelManager, newHandModelManager);
            }

            // Migrate the LeapServiceProvider on the LHC to a LeapXRServiceProvider on
            // the primary camera, and swap references over to it.
            var leapServiceProvider = lhcData.leapServiceProvider;
            var leapXRServiceProvider = (LeapXRServiceProvider)null;
            if (leapServiceProvider != null) {
              leapXRServiceProvider = Undo.AddComponent<LeapXRServiceProvider>(
                cameraData.cameraTransform.gameObject);

              leapServiceProvider
                .CopySettingsToLeapXRServiceProvider(leapXRServiceProvider);

              EditorUtils.ReplaceSceneReferences(leapServiceProvider,
                leapXRServiceProvider);
            }

            if (newHandModelManager != null && leapXRServiceProvider != null) {
              newHandModelManager.leapProvider = leapXRServiceProvider;
            }

            // Migrate any extra components on the LHC to the camera.
            var extraComponents = lhcData.extraComponents;
            if (extraComponents != null && extraComponents.Count > 0) {
              foreach (var component in extraComponents.Query().Where(c => c != null)) {
                var newComponent
                  = cameraTransform.gameObject.AddComponent(component.GetType());

                EditorUtility.CopySerialized(component, newComponent);

                EditorUtils.ReplaceSceneReferences(component, newComponent);
              }
            }
          }

          // Remove the LeapSpace transform, also destroying the LeapHandController
          // object.
          if (leapSpaceTransform != null) {
            Undo.DestroyObjectImmediate(leapSpaceTransform.gameObject);
          }
        }
      }
    }

    #endregion

  }

}
 
