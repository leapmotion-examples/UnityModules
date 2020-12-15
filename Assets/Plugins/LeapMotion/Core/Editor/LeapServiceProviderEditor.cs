/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider> {

    internal const float INTERACTION_VOLUME_MODEL_IMPORT_SCALE_FACTOR = 0.001f;

    protected Quaternion deviceRotation = Quaternion.identity;
    protected bool isVRProvider = false;

    protected Vector3 controllerOffset = Vector3.zero;

    private const float LMC_BOX_RADIUS = 0.45f;
    private const float LMC_BOX_WIDTH = 0.965f;
    private const float LMC_BOX_DEPTH = 0.6671f;

    private Mesh _stereoIR170InteractionZoneMesh;
    private Material _stereoIR170InteractionMaterial;
    private readonly Vector3 _stereoIR170InteractionZoneMeshOffset = new Vector3(0.0523f, 0, 0.005f);

    private LeapServiceProvider _leapServiceProvider;
    private Controller _leapController;


    protected override void OnEnable() {

      base.OnEnable();

      ParseStereoIR170InteractionMeshData();

      specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);

      specifyConditionalDrawing("_frameOptimization",
                                (int)LeapServiceProvider.FrameOptimizationMode.None,
                                "_physicsExtrapolation",
                                "_physicsExtrapolationTime");

      specifyConditionalDrawing("_physicsExtrapolation",
                                (int)LeapServiceProvider.PhysicsExtrapolationMode.Manual,
                                "_physicsExtrapolationTime");

      deferProperty("_serverNameSpace");
      deferProperty("_workerThreadProfiling");

      if (!(LeapServiceProvider is LeapXRServiceProvider)) {
        addPropertyToFoldout("_trackingOptimization", "Advanced Options");
      }
      addPropertyToFoldout("_workerThreadProfiling", "Advanced Options");
      addPropertyToFoldout("_serverNameSpace"      , "Advanced Options");
    }

    private void frameOptimizationWarning(SerializedProperty property) {
      var mode = (LeapServiceProvider.FrameOptimizationMode)property.intValue;
      string warningText;

      switch (mode) {
        case LeapServiceProvider.FrameOptimizationMode.ReuseUpdateForPhysics:
          warningText = "Reusing update frames for physics introduces a frame of latency "
                      + "for physics interactions.";
          break;
        case LeapServiceProvider.FrameOptimizationMode.ReusePhysicsForUpdate:
          warningText = "This optimization REQUIRES physics framerate to match your "
                      + "target framerate EXACTLY.";
          break;
        default:
          return;
      }

      EditorGUILayout.HelpBox(warningText, MessageType.Warning);
    }

    public override void OnInspectorGUI() {

#if UNITY_2019_3_OR_NEWER
      // Easily tracking VR-enabled-or-not requires an XR package installed, so remove this warning for now.
#else
      if (UnityEditor.PlayerSettings.virtualRealitySupported && !isVRProvider) {
        EditorGUILayout.HelpBox(
          "VR support is enabled. If your Leap is mounted to your headset, you should be "
          + "using LeapXRServiceProvider instead of LeapServiceProvider. (If your Leap "
          + "is not mounted to your headset, you can safely ignore this warning.)",
          MessageType.Warning);
      }
#endif

      base.OnInspectorGUI();
    }

    public virtual void OnSceneGUI() {

      switch (GetSelectedInteractionVolume()) {
        case LeapServiceProvider.InteractionVolumeVisualization.None:
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.LeapMotionController:
          DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.StereoIR170:
          DrawStereoIR170InteractionZoneMesh();
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
          DetectConnectedDevice();
          break;
        default:
          break;
      }

    }

    private void ParseStereoIR170InteractionMeshData() {

      if (_stereoIR170InteractionZoneMesh == null) {
        _stereoIR170InteractionZoneMesh = (Mesh)AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", "Plugins", "LeapMotion", "Core", "Models", "StereoIR170-interaction-cone.obj"), typeof(Mesh));
      }

      if (_stereoIR170InteractionMaterial == null) {
        _stereoIR170InteractionMaterial = (Material)AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", "Plugins", "LeapMotion", "Core", "Materials", "StereoIR170InteractionVolume.mat"), typeof(Material));
      }

    }

    private LeapServiceProvider LeapServiceProvider {
      get {

        if (this._leapServiceProvider != null) { 
          return this._leapServiceProvider;
        }
        else {
          this._leapServiceProvider = this.target.GetComponent<LeapServiceProvider>();

          return this._leapServiceProvider;
        }
      }
    }

    private Controller LeapController {
      get {

        if (this._leapController != null) {
          return this._leapController;
        }
        else
        {
          this._leapController = LeapServiceProvider?.GetLeapController();

          if (this._leapController != null) {
            this._leapController.Device += _leapController_DeviceChanged;
            this._leapController.DeviceLost += _leapController_DeviceChanged;
          }

          return this._leapController;
        }
      }
    }

    private void _leapController_DeviceChanged(object sender, DeviceEventArgs e) {
      EditorWindow view = EditorWindow.GetWindow<SceneView>();
      view.Repaint();
    }

    private void DetectConnectedDevice() {

      if (LeapController?.Devices?.Count == 1)
      {
        if (LeapController.Devices.First().Type == Device.DeviceType.TYPE_RIGEL) {
          DrawStereoIR170InteractionZoneMesh();
        }
        else if (LeapController.Devices.First().Type == Device.DeviceType.TYPE_PERIPHERAL) {
          DrawLeapMotionControllerInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
        }
      }
    }

    private LeapServiceProvider.InteractionVolumeVisualization? GetSelectedInteractionVolume() {

      return LeapServiceProvider?.SelectedInteractionVolumeVisualization;
    }

    private void DrawStereoIR170InteractionZoneMesh() {

      if (_stereoIR170InteractionMaterial != null && _stereoIR170InteractionZoneMesh != null) {
        _stereoIR170InteractionMaterial.SetPass(0);

        Graphics.DrawMeshNow(_stereoIR170InteractionZoneMesh,
           target.transform.localToWorldMatrix * 
           Matrix4x4.TRS(controllerOffset + _stereoIR170InteractionZoneMeshOffset, deviceRotation * Quaternion.Euler(-90, 0, 0), Vector3.one * 0.001f));
      }
    }

    private void DrawLeapMotionControllerInteractionZone(float box_width, float box_depth, float box_radius, Color interactionZoneColor) {

      Color previousColor = Handles.color;
      Handles.color = interactionZoneColor;

      Vector3 origin = target.transform.TransformPoint(controllerOffset);
      Vector3 local_top_left, top_left, local_top_right, top_right, local_bottom_left, bottom_left, local_bottom_right, bottom_right;
      getLocalGlobalPoint(-1, 1,  1, box_width, box_depth, box_radius, out local_top_left    , out top_left);
      getLocalGlobalPoint( 1, 1,  1, box_width, box_depth, box_radius, out local_top_right   , out top_right);
      getLocalGlobalPoint(-1, 1, -1, box_width, box_depth, box_radius, out local_bottom_left , out bottom_left);
      getLocalGlobalPoint( 1, 1, -1, box_width, box_depth, box_radius, out local_bottom_right, out bottom_right);

      Handles.DrawAAPolyLine(origin, top_left);
      Handles.DrawAAPolyLine(origin, top_right);
      Handles.DrawAAPolyLine(origin, bottom_left);
      Handles.DrawAAPolyLine(origin, bottom_right);

      drawControllerEdge(origin, local_top_left, local_top_right, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_top_left, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_bottom_right, box_radius);
      drawControllerEdge(origin, local_bottom_right, local_top_right, box_radius);

      drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right,
                        local_bottom_right, box_radius);
      drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left,
                        local_bottom_right, box_radius);

      Handles.color = previousColor;
    }

    private void getLocalGlobalPoint(int x, int y, int z, float box_width, float box_depth, float box_radius, out Vector3 local, out Vector3 global) {

      local = deviceRotation * new Vector3(x * box_width, y * box_radius, z * box_depth);
      global = target.transform.TransformPoint(controllerOffset
                                               + box_radius * local.normalized);
    }

    private void drawControllerEdge(Vector3 origin,
                                    Vector3 edge0, Vector3 edge1,
                                    float box_radius) {
      Vector3 right_normal = target.transform
                                   .TransformDirection(Vector3.Cross(edge0, edge1));
      float right_angle = Vector3.Angle(edge0, edge1);

      Handles.DrawWireArc(origin, right_normal, target.transform.TransformDirection(edge0),
                          right_angle, target.transform.lossyScale.x * box_radius);
    }

    private void drawControllerArc(Vector3 origin,
                                   Vector3 edgeA0, Vector3 edgeA1,
                                   Vector3 edgeB0, Vector3 edgeB1,
                                   float box_radius) {

      Vector3 faceA = target.transform.rotation * Vector3.Lerp(edgeA0, edgeA1, 0.5f);
      Vector3 faceB = target.transform.rotation * Vector3.Lerp(edgeB0, edgeB1, 0.5f);

      float resolutionIncrement = 1f / 50f;
      for (float i = 0f; i < 1f; i += resolutionIncrement) {
        Vector3 begin = Vector3.Lerp(faceA, faceB, i).normalized
                        * target.transform.lossyScale.x * box_radius;
        Vector3 end = Vector3.Lerp(faceA, faceB, i + resolutionIncrement).normalized
                      * target.transform.lossyScale.x * box_radius;

        Handles.DrawAAPolyLine(origin + begin, origin + end);
      }
    }
  }
}


