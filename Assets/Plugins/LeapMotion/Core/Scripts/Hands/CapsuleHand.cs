/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;

namespace Leap.Unity {
  /** A basic Leap hand model constructed dynamically vs. using pre-existing geometry*/
  public class CapsuleHand : HandModelBase {
    private const int TOTAL_JOINT_COUNT = 4 * 5;
    private const float CYLINDER_MESH_RESOLUTION = 0.1f; //in centimeters, meshes within this resolution will be re-used
    private const int THUMB_BASE_INDEX = (int)Finger.FingerType.TYPE_THUMB * 4;
    private const int PINKY_BASE_INDEX = (int)Finger.FingerType.TYPE_PINKY * 4;

    private static int _leftColorIndex = 0;
    private static int _rightColorIndex = 0;
    private static Color[] _leftColorList  = { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
    private static Color[] _rightColorList = { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };

    #pragma warning disable 0649
    [SerializeField]
    private Chirality handedness;

    [SerializeField]
    private bool _showArm = true;

    [SerializeField]
    private bool _castShadows = true;

    [SerializeField]
    private Material _material;
    private Material _backing_material;

    [SerializeField]
    private Mesh _sphereMesh;

    private Mesh _cylinderMesh;

    [MinValue(3)]
    [SerializeField]
    private int _cylinderResolution = 12;

    [MinValue(0)]
    [SerializeField]
    private float _jointRadius = 0.008f;

    [MinValue(0)]
    [SerializeField]
    private float _cylinderRadius = 0.006f;

    [MinValue(0)]
    [SerializeField]
    private float _palmRadius = 0.015f;
    #pragma warning restore 0649

    private Material _sphereMat;
    private Hand _hand;
    private Vector3[] _spherePositions;
    private Matrix4x4[] _sphereMatrices   = new Matrix4x4[32], 
                        _cylinderMatrices = new Matrix4x4[32];
    private int _curSphereIndex = 0, _curCylinderIndex = 0;

    public override ModelType HandModelType {
      get {
        return ModelType.Graphics;
      }
    }

    public override Chirality Handedness {
      get {
        return handedness;
      }
      set { }
    }

    public override bool SupportsEditorPersistence() {
      return true;
    }

    public override Hand GetLeapHand() {
      return _hand;
    }

    public override void SetLeapHand(Hand hand) {
      _hand = hand;
    }

    public override void InitHand() {
      if (_material != null && (_backing_material == null || !_backing_material.enableInstancing)) {
        _backing_material = new Material(_material);
        _backing_material.hideFlags = HideFlags.DontSaveInEditor;
        if(!Application.isEditor && !_backing_material.enableInstancing) {
          Debug.LogError("Capsule Hand Material needs Instancing Enabled to render in builds!", this);
        }
        _backing_material.enableInstancing = true;
        _sphereMat = new Material(_backing_material);
        _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
      }
    }

    #if UNITY_EDITOR
    private void OnValidate() {
      _meshMap.Clear();
      if (_material == null || !_material.enableInstancing) {
        Debug.LogWarning("CapsuleHand's Material must have " +
          "instancing enabled in order to work in builds! Replacing " +
          "Material with a Default Material now...", this);
        _material = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(
          System.IO.Path.Combine("Assets", "Plugins", "LeapMotion",
          "Core", "Materials", "InstancedCapsuleHand.mat"), typeof(Material));
      }
    }
    #endif

    public override void BeginHand() {
      base.BeginHand();

      if (_hand.IsLeft) {
        _sphereMat.color = _leftColorList[_leftColorIndex];
        _leftColorIndex = (_leftColorIndex + 1) % _leftColorList.Length;
      } else {
        _sphereMat.color = _rightColorList[_rightColorIndex];
        _rightColorIndex = (_rightColorIndex + 1) % _rightColorList.Length;
      }
    }

    public override void UpdateHand() {
      _curSphereIndex   = 0;
      _curCylinderIndex = 0;

      if (_spherePositions == null || _spherePositions.Length != TOTAL_JOINT_COUNT) {
        _spherePositions = new Vector3[TOTAL_JOINT_COUNT];
      }

      if (_material != null && (_backing_material == null || !_backing_material.enableInstancing)) {
        _backing_material = new Material(_material);
        _backing_material.hideFlags = HideFlags.DontSaveInEditor;
        _backing_material.enableInstancing = true;
        _sphereMat = new Material(_backing_material);
        _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
      }

      //Update all joint spheres in the fingers
      foreach (var finger in _hand.Fingers) {
        for (int j = 0; j < 4; j++) {
          int key = getFingerJointIndex((int)finger.Type, j);

          Vector3 position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
          _spherePositions[key] = position;

          drawSphere(position);
        }
      }

      //Now we just have a few more spheres for the hands
      //PalmPos, WristPos, and mockThumbJointPos, which is derived and not taken from the frame obj

      Vector3 palmPosition = _hand.PalmPosition.ToVector3();
      drawSphere(palmPosition, _palmRadius);

      Vector3 thumbBaseToPalm = _spherePositions[THUMB_BASE_INDEX] - _hand.PalmPosition.ToVector3();
      Vector3 mockThumbJointPos = _hand.PalmPosition.ToVector3() + Vector3.Reflect(thumbBaseToPalm, _hand.Basis.xBasis.ToVector3());
      drawSphere(mockThumbJointPos);

      //If we want to show the arm, do the calculations and display the meshes
      if (_showArm) {
        var arm = _hand.Arm;

        Vector3 right = arm.Basis.xBasis.ToVector3() * arm.Width * 0.7f * 0.5f;
        Vector3 wrist = arm.WristPosition.ToVector3();
        Vector3 elbow = arm.ElbowPosition.ToVector3();

        float armLength = Vector3.Distance(wrist, elbow);
        wrist -= arm.Direction.ToVector3() * armLength * 0.05f;

        Vector3 armFrontRight = wrist + right;
        Vector3 armFrontLeft = wrist - right;
        Vector3 armBackRight = elbow + right;
        Vector3 armBackLeft = elbow - right;

        drawSphere(armFrontRight);
        drawSphere(armFrontLeft);
        drawSphere(armBackLeft);
        drawSphere(armBackRight);

        drawCylinder(armFrontLeft, armFrontRight);
        drawCylinder(armBackLeft, armBackRight);
        drawCylinder(armFrontLeft, armBackLeft);
        drawCylinder(armFrontRight, armBackRight);
      }

      //Draw cylinders between finger joints
      for (int i = 0; i < 5; i++) {
        for (int j = 0; j < 3; j++) {
          int keyA = getFingerJointIndex(i, j);
          int keyB = getFingerJointIndex(i, j + 1);

          Vector3 posA = _spherePositions[keyA];
          Vector3 posB = _spherePositions[keyB];

          drawCylinder(posA, posB);
        }
      }

      //Draw cylinders between finger knuckles
      for (int i = 0; i < 4; i++) {
        int keyA = getFingerJointIndex(i, 0);
        int keyB = getFingerJointIndex(i + 1, 0);

        Vector3 posA = _spherePositions[keyA];
        Vector3 posB = _spherePositions[keyB];

        drawCylinder(posA, posB);
      }

      //Draw the rest of the hand
      drawCylinder(mockThumbJointPos, THUMB_BASE_INDEX);
      drawCylinder(mockThumbJointPos, PINKY_BASE_INDEX);

      // Draw Spheres
      Graphics.DrawMeshInstanced(_sphereMesh, 0, _sphereMat, _sphereMatrices, _curSphereIndex, null, 
        _castShadows?UnityEngine.Rendering.ShadowCastingMode.On: UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);

      // Draw Cylinders
      if(_cylinderMesh == null) { _cylinderMesh = getCylinderMesh(1f); }
      Graphics.DrawMeshInstanced(_cylinderMesh, 0, _backing_material, _cylinderMatrices, _curCylinderIndex, null,
        _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }

    private void drawSphere(Vector3 position) {
      drawSphere(position, _jointRadius);
    }

    private void drawSphere(Vector3 position, float radius) {
      if (isNaN(position)) { return; }

      //multiply radius by 2 because the default unity sphere has a radius of 0.5 meters at scale 1.
      _sphereMatrices[_curSphereIndex++] = Matrix4x4.TRS(position, 
        Quaternion.identity, Vector3.one * radius * 2.0f * transform.lossyScale.x);
    }

    private void drawCylinder(Vector3 a, Vector3 b) {
      if (isNaN(a) || isNaN(b)) { return; }

      float length = (a - b).magnitude;

      if ((a - b).magnitude > 0.001f) {
        _cylinderMatrices[_curCylinderIndex++] = Matrix4x4.TRS(a,
          Quaternion.LookRotation(b - a), new Vector3(transform.lossyScale.x, transform.lossyScale.x, length));
      }
    }

    private bool isNaN(Vector3 v) {
      return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
    }

    private void drawCylinder(int a, int b) {
      drawCylinder(_spherePositions[a], _spherePositions[b]);
    }

    private void drawCylinder(Vector3 a, int b) {
      drawCylinder(a, _spherePositions[b]);
    }

    private int getFingerJointIndex(int fingerIndex, int jointIndex) {
      return fingerIndex * 4 + jointIndex;
    }

    private Dictionary<int, Mesh> _meshMap = new Dictionary<int, Mesh>();
    private Mesh getCylinderMesh(float length) {
      int lengthKey = Mathf.RoundToInt(length * 100 / CYLINDER_MESH_RESOLUTION);

      Mesh mesh;
      if (_meshMap.TryGetValue(lengthKey, out mesh)) {
        return mesh;
      }

      mesh = new Mesh();
      mesh.name = "GeneratedCylinder";
      mesh.hideFlags = HideFlags.DontSave;

      List<Vector3> verts = new List<Vector3>();
      List<Color> colors = new List<Color>();
      List<int> tris = new List<int>();

      Vector3 p0 = Vector3.zero;
      Vector3 p1 = Vector3.forward * length;
      for (int i = 0; i < _cylinderResolution; i++) {
        float angle = (Mathf.PI * 2.0f * i) / _cylinderResolution;
        float dx = _cylinderRadius * Mathf.Cos(angle);
        float dy = _cylinderRadius * Mathf.Sin(angle);

        Vector3 spoke = new Vector3(dx, dy, 0);

        verts.Add(p0 + spoke);
        verts.Add(p1 + spoke);

        colors.Add(Color.white);
        colors.Add(Color.white);

        int triStart = verts.Count;
        int triCap = _cylinderResolution * 2;

        tris.Add((triStart + 0) % triCap);
        tris.Add((triStart + 2) % triCap);
        tris.Add((triStart + 1) % triCap);

        tris.Add((triStart + 2) % triCap);
        tris.Add((triStart + 3) % triCap);
        tris.Add((triStart + 1) % triCap);
      }

      mesh.SetVertices(verts);
      mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
      mesh.RecalculateBounds();
      mesh.RecalculateNormals();
      mesh.UploadMeshData(true);

      _meshMap[lengthKey] = mesh;

      return mesh;
    }
  }
}
