﻿using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Leap.Unity.UI.Constraints {

  [RequireComponent(typeof(Rigidbody))]
  public class RigidbodyParenting : MonoBehaviour, IRuntimeGizmoComponent {

    [Header("Debug")]
    public Text outputText;

    [SerializeField]
    private Rigidbody _parentBody;
    private Rigidbody _childBody;

    void Start() {
      _childBody = GetComponent<Rigidbody>();
      if (_parentBody == null) {
        _parentBody = _childBody.transform.parent.GetComponent<Rigidbody>();
      }
      if (_parentBody == null) { Debug.LogError("[RigidbodyParenting] Must be attached to a Rigidbody that is the child of another Rigidbody."); }

      InitializeBodies();
      PhysicsCallbacks.OnPrePhysics += OnPrePhysics;
      PhysicsCallbacks.OnPostPhysics += OnPostPhysics;
    }

    void Update() {
      if (outputText != null) {
        outputText.text = "Child Local Position: \n" + _childBody.transform.localPosition.ToString("G4") + "      "
                        + "Child Body Velocity: \n" + _childBody.velocity;
      }

      //Debug.Log("Num post physics: " + numPostPhysics);
      //numPostPhysics = 0;
      //Debug.Log("Num fixed update: " + numFixedUpdate);
      //numFixedUpdate = 0;
    }

    //private int numPostPhysics = 0;
    //private int numFixedUpdate = 0;

    private Transform _parentT;
    private Transform _childT;

    //void FixedUpdate() {
    //  numFixedUpdate++;
    //}

    private void InitializeBodies() {
      _parentT = new GameObject("Transform Sim: " + _parentBody.gameObject.name).transform;
      _childT = new GameObject("Transform Sim: " + _childBody.gameObject.name).transform;

      _parentT.transform.position = _parentBody.transform.position;
      _parentT.transform.rotation = _parentBody.transform.rotation;
      _parentT.transform.localScale = _parentBody.transform.localScale;

      _childT.transform.parent = _parentT;
      _childT.transform.position = _childBody.transform.position;
      _childT.transform.rotation = _childBody.transform.rotation;
      _childT.transform.localScale = _childBody.transform.localScale;
    }

    private Vector3 _prePhysicsChildTransformPosition;
    private Quaternion _prePhysicsChildTransformRotation = Quaternion.identity;

    private bool _hasPostPhysics = false;
    private Vector3 _childPosNextPhysicsUpdate = Vector3.zero;
    private Quaternion _childRotNextPhysicsUpdate = Quaternion.identity;

    private void OnPrePhysics() {
      //if (_hasPostPhysics) {
      //  _childBody.position = _childPosNextPhysicsUpdate;
      //  _childBody.rotation = _childRotNextPhysicsUpdate;
      //}

      _childT.position = _childBody.position;
      _childT.rotation = _childBody.rotation;

      _prePhysicsChildTransformPosition = _childT.position;
      _prePhysicsChildTransformRotation = _childT.rotation;
    }

    private void OnPostPhysics() {
      //numPostPhysics++;

      // This implicitly moves _childT via the transform hierarchy.
      _parentT.position = _parentBody.position;
      _parentT.rotation = _parentBody.rotation;

      _childPosNextPhysicsUpdate = _childBody.position + (_childT.position - _prePhysicsChildTransformPosition);
      _childRotNextPhysicsUpdate = _childBody.rotation * (Quaternion.Inverse(_prePhysicsChildTransformRotation) * _childT.rotation);
      _hasPostPhysics = true;

      _childBody.position = _childPosNextPhysicsUpdate;
      _childBody.rotation = _childRotNextPhysicsUpdate;
    }

    #region Gizmos

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (_childBody == null || _parentBody == null) return;

      drawer.PushMatrix();
      drawer.color = Color.red;
      drawer.matrix = _childBody.transform.localToWorldMatrix;
      drawer.DrawWireCube(Vector3.zero, Vector3.one * 0.04F);
      drawer.PopMatrix();

      drawer.color = new Color(0.9F, 0.45F, 0.1F);
      drawer.DrawLine(_childBody.position, _childBody.position + _childBody.rotation * Vector3.up * 0.1F);
    }

    #endregion

  }


}
