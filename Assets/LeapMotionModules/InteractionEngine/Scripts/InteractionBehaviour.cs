﻿
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction.CApi;
using LeapInternal;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// InteractionBehaviour is the default implementation of IInteractionBehaviour.
  /// </summary>
  ///
  /// <remarks>
  /// It has the following features:
  ///    - Extends from InteractionBehaviourBase to take advantage of it's bookkeeping and callbacks.
  ///    - Supports kinematic movement as well as physical movement.
  ///    - When non-kinematic, supports pushing.
  ///    - Has the concept of a graphical anchor to reduce apparent latency between a hand moving and the object responding.
  ///      This can result in the graphical representation diverging slightly from the physical representation.
  ///    - Utilizes the Kabsch algorithm to determine how the object should rest in the hand when grabbed.
  ///      This allows more fidelity than simple rigid atatchment to the hand, as well as more intuitive multi-hand
  ///      interaction.
  ///
  /// This default implementation has the following requirements:
  ///    - A Rigidbody is required
  ///    - Kinematic movement must still be simulated via Rigidbody kinematic movement, as opposed to rigid movement of the Transform.
  ///    - This behaviour cannot be a child of another InteractionBehaviour.
  ///    - Any non-continuous movement must be noted using the NotifyTeleported() method.
  ///    - Any forces or torques must be applied using the AddLinearAcceleration and AddAngularAcceleration methods instead of
  ///      the Rigidbody AddForce or AddTorque methods.
  ///    - Any update of the kinematic or gravity status of the object must be done through setting the IsKinematic or UseGravity
  ///      properties of this behaviour instead of the properties on the Rigidbody component.
  /// </remarks>
  [SelectionBase]
  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {
    public const int NUM_FINGERS = 5;
    public const int NUM_BONES = 4;

    [SerializeField]
    protected InteractionMaterial _material;

    [SerializeField]
    protected Renderer[] _suspendableRenderers;

    protected Transform[] _childrenArray;
    protected Rigidbody _rigidbody;

    protected bool _isKinematic;
    protected bool _useGravity;
    protected float _drag;
    protected float _angularDrag;
    protected bool _recievedVelocityUpdate = false;
    protected bool _notifiedOfTeleport = false;
    protected bool _ignoringBrushes = false;

    protected Vector3 _solvedPosition;
    protected Quaternion _solvedRotation;

    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;
    protected LEAP_IE_KABSCH _kabsch;

    private Bounds _debugBounds;
    private bool _showDebugRecievedVelocity = false;

    #region PUBLIC METHODS

    /// <summary>
    /// Sets or Gets whether or not this InteractionBehaviour is Kinematic or not.  Always use this instead
    /// of Rigidbody.IsKinematic because InteractionBehaviour overrides the kinematic status of the Rigidbody.
    /// </summary>
    public bool IsKinematic {
      get {
        return _isKinematic;
      }
      set {
        _isKinematic = value;
        if (HasShapeInstance) {
          if (!IsBeingGrasped) {
            _rigidbody.isKinematic = value;
          }
        } else {
          _rigidbody.isKinematic = value;
        }
      }
    }

    /// <summary>
    /// Sets or Gets whether or not this InteractionBehaviour uses Gravity or not.  Always use this instead
    /// of Rigidbody.UseGravity because InteractionBehaviour overrides the gravity status of the Rigidbody.
    /// </summary>
    public bool UseGravity {
      get {
        return _useGravity;
      }
      set {
        _useGravity = value;
        if (!HasShapeInstance) {
          _rigidbody.useGravity = _useGravity;
        }
      }
    }

    /// <summary>
    /// Adds a linear acceleration to the center of mass of this object.  Use this instead of Rigidbody.AddForce()
    /// </summary>
    public void AddLinearAcceleration(Vector3 acceleration) {
      _accumulatedLinearAcceleration += acceleration;
    }

    /// <summary>
    /// Adds an angular acceleration to the center of mass of this object.  Use this instead of Rigidbody.AddTorque()
    /// </summary>
    public void AddAngularAcceleration(Vector3 acceleration) {
      _accumulatedAngularAcceleration += acceleration;
    }

    /// <summary>
    /// This method should always be called if the object is teleported to a new location instead of moving there.  If
    /// this method is not called, it can cause the simulation to become unstable.
    /// </summary>
    public void NotifyTeleported() {
      _notifiedOfTeleport = true;
    }
    #endregion

    #region INTERACTION CALLBACKS

    protected override void OnRegistered() {
      base.OnRegistered();

      _rigidbody = GetComponent<Rigidbody>();
      if (_rigidbody == null) {
        //Should only happen if the user has done some trickery since there is a RequireComponent attribute
        throw new InvalidOperationException("InteractionBehaviour must have a Rigidbody component attached to it.");
      }
      _rigidbody.maxAngularVelocity = float.PositiveInfinity;

      _childrenArray = GetComponentsInChildren<Transform>(true);
      updateLayer();

      //Technically we only need one instance in the entire scene, but easier for each object to have it's own instance for now.
      //TODO: Investigate allowing this to be a singleton?
      KabschC.Construct(ref _kabsch);
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();

      resetState();

      KabschC.Destruct(ref _kabsch);
    }

#if UNITY_EDITOR
    protected override void OnPreSolve() {
      base.OnPreSolve();

      _showDebugRecievedVelocity = false;
    }
#endif

    protected override void OnPostSolve() {
      base.OnPostSolve();

      if (_recievedVelocityUpdate) {
        //If we recieved a velocity update, gravity must always be disabled because the
        //velocity update accounts for gravity.
        if (_rigidbody.useGravity) {
          _rigidbody.useGravity = false;
        }
      } else {
        if (!IsBeingGrasped) {
          //If we did not recieve a velocity update, we set the rigidbody's gravity status
          //to match whatever the user has set.
          if (_rigidbody.useGravity != _useGravity) {
            _rigidbody.useGravity = _useGravity;
          }
        }

        //Only apply if non-zero to prevent waking up the body
        if (_accumulatedLinearAcceleration != Vector3.zero) {
          _rigidbody.AddForce(_accumulatedLinearAcceleration, ForceMode.Acceleration);
        }

        if (_accumulatedAngularAcceleration != Vector3.zero) {
          _rigidbody.AddTorque(_accumulatedAngularAcceleration, ForceMode.Acceleration);
        }
      }

      if (IsBeingGrasped) {
        if (Vector3.Distance(_solvedPosition, _rigidbody.position) > _material.ReleaseDistance) {
          _manager.ReleaseObject(this);
        }
      }

      //Reset so we can accumulate for the next frame
      _accumulatedLinearAcceleration = Vector3.zero;
      _accumulatedAngularAcceleration = Vector3.zero;
      _recievedVelocityUpdate = false;
    }

    public override void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
      createInfo = new INTERACTION_CREATE_SHAPE_INFO();
      createInfo.shapeFlags = ShapeInfoFlags.None;

      if (!_isKinematic) {
        //Kinematic objects do not need velocity simulation
        createInfo.shapeFlags |= ShapeInfoFlags.HasRigidBody;
      }

      createTransform = getRigidbodyTransform();
    }

    protected override void OnInteractionShapeCreated(INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle) {
      base.OnInteractionShapeCreated(instanceHandle);

      //Copy over existing settings for defaults
      _isKinematic = _rigidbody.isKinematic;
      _useGravity = _rigidbody.useGravity;
      _drag = _rigidbody.drag;
      _angularDrag = _rigidbody.angularDrag;

      _solvedPosition = _rigidbody.position;
      _solvedRotation = _rigidbody.rotation;

      updateLayer();

#if UNITY_EDITOR
      Collider[] colliders = GetComponentsInChildren<Collider>();
      if (colliders.Length > 0) {
        _debugBounds = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++) {
          _debugBounds.Encapsulate(colliders[i].bounds);
        }
        _debugBounds.center = transform.InverseTransformPoint(_debugBounds.center);
      }
#endif
    }

    protected override void OnInteractionShapeDestroyed() {
      base.OnInteractionShapeDestroyed();

      resetState();
    }

    public override void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();

      updateInfo.updateFlags = UpdateInfoFlags.VelocityEnabled;

      // Request notification of when hands are no longer touching (or influencing.)
      if (_ignoringBrushes) {
        updateInfo.updateFlags |= UpdateInfoFlags.ReportNoResult;
      }

      if (_material.ContactEnabled && !_isKinematic && !IsBeingGrasped) {
        updateInfo.updateFlags |= UpdateInfoFlags.ApplyAcceleration;
      }

      updateInfo.linearAcceleration = _accumulatedLinearAcceleration.ToCVector();
      updateInfo.angularAcceleration = _accumulatedAngularAcceleration.ToCVector();
      updateInfo.linearVelocity = _rigidbody.velocity.ToCVector();
      updateInfo.angularVelocity = _rigidbody.angularVelocity.ToCVector();

      if (_useGravity) {
        updateInfo.updateFlags |= UpdateInfoFlags.GravityEnabled;
      }

      interactionTransform = getRigidbodyTransform();
    }

    protected override void OnRecievedSimulationResults(INTERACTION_SHAPE_INSTANCE_RESULTS results) {
      base.OnRecievedSimulationResults(results);

      if ((results.resultFlags & ShapeInstanceResultFlags.Velocities) != 0 &&
          !IsBeingGrasped &&
          _material.ContactEnabled) {
        //Use Sleep() to clear any forces that might have been applied by the user.
        _rigidbody.Sleep();
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;
      }
#if UNITY_EDITOR
      _showDebugRecievedVelocity = _recievedVelocityUpdate;
#endif

      if ((results.resultFlags & ShapeInstanceResultFlags.MaxHand) != 0) {
        if (!_ignoringBrushes && results.maxHandDepth > _material.BrushDisableDistance) {
          _ignoringBrushes = true;
        }
      } else if (_ignoringBrushes) {
        _ignoringBrushes = false;
      }

      updateLayer();
    }

    protected override void OnHandGrasped(Hand hand) {
      base.OnHandGrasped(hand);

      updateState();

      var newCollection = HandPointCollection.Create(_rigidbody);
      _handIdToPoints[hand.Id] = newCollection;

      newCollection.UpdateTransform();

      for (int f = 0; f < NUM_FINGERS; f++) {
        Finger finger = hand.Fingers[f];
        Finger.FingerType fingerType = finger.Type;

        for (int j = 0; j < NUM_BONES; j++) {
          Bone.BoneType boneType = (Bone.BoneType)j;
          Bone bone = finger.Bone(boneType);

          Vector3 bonePos = bone.NextJoint.ToVector3();

          //Global position of the point is just the position of the joint itself
          newCollection.SetGlobalPosition(bonePos, fingerType, boneType);
        }
      }
    }

    protected override void OnHandsHoldPhysics(ReadonlyList<Hand> hands) {
      base.OnHandsHoldPhysics(hands);

      float distanceToSolved = Vector3.Distance(_rigidbody.position, _solvedPosition);

      //Get new transform
      Vector3 newPosition;
      Quaternion newRotation;
      getSolvedTransform(hands, out newPosition, out newRotation);

      _solvedPosition = newPosition;
      _solvedRotation = newRotation;

      //Apply new transform to object
      switch (_material.GraspMethod) {
        case InteractionMaterial.GraspMethodEnum.Kinematic:
          if (_notifiedOfTeleport) {
            _rigidbody.position = newPosition;
            _rigidbody.rotation = newRotation;
          } else {
            _rigidbody.MovePosition(newPosition);
            _rigidbody.MoveRotation(newRotation);
          }
          break;
        case InteractionMaterial.GraspMethodEnum.Velocity:
          if (_notifiedOfTeleport) {
            _rigidbody.position = newPosition;
            _rigidbody.rotation = newRotation;
          } else {
            Vector3 deltaPos = newPosition - _rigidbody.position;
            Quaternion deltaRot = newRotation * Quaternion.Inverse(_rigidbody.rotation);

            Vector3 deltaAxis;
            float deltaAngle;
            deltaRot.ToAngleAxis(out deltaAngle, out deltaAxis);

            Vector3 targetVelocity = deltaPos / Time.fixedDeltaTime;
            Vector3 targetAngularVelocity = deltaAxis * deltaAngle * Mathf.Deg2Rad / Time.fixedDeltaTime;

            if (targetVelocity.sqrMagnitude > float.Epsilon) {
              float targetSpeed = targetVelocity.magnitude;
              float actualSpeed = Mathf.Min(_material.MaxVelocity, targetSpeed);
              float targetPercent = actualSpeed / targetSpeed;

              targetVelocity *= targetPercent;
              targetAngularVelocity *= targetPercent;
            }

            float followStrength = _material.StrengthByDistance.Evaluate(distanceToSolved);
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetVelocity, followStrength);
            _rigidbody.angularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, targetAngularVelocity, followStrength);
          }
          break;
        default:
          throw new InvalidOperationException("Unexpected grasp method");
      }

      _notifiedOfTeleport = false;
    }

    protected override void OnHandReleased(Hand hand) {
      base.OnHandReleased(hand);

      updateState();

      removeHandPointCollection(hand.Id);
    }

    protected override void OnHandLostTracking(Hand oldHand, out float maxSuspensionTime) {
      base.OnHandLostTracking(oldHand, out maxSuspensionTime);

      maxSuspensionTime = _material.SuspensionEnabled ? _material.MaxSuspensionTime : 0;

      updateState();
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      updateState();

      //Associate the collection with the new id
      var collection = _handIdToPoints[oldId];
      _handIdToPoints.Remove(oldId);
      _handIdToPoints[newHand.Id] = collection;

      NotifyTeleported();
    }

    protected override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      updateState();

      removeHandPointCollection(oldHand.Id);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      updateState();
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();

      updateState();

      float speed = _rigidbody.velocity.magnitude;
      float multiplier = _material.ThrowingVelocityCurve.Evaluate(speed);
      _rigidbody.velocity *= multiplier;
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();

      Vector3 scale = transform.lossyScale;
      if (!Mathf.Approximately(scale.x, scale.y) || !Mathf.Approximately(scale.x, scale.z)) {
        enabled = false;
        Debug.LogError("Interaction Behaviour cannot have a non-uniform scale!");
        return;
      }
    }

#if UNITY_EDITOR
    private void OnCollisionEnter(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      if (otherObj.GetComponentInParent<IHandModel>() != null
        && otherObj.GetComponentInParent<InteractionBrushHand>() == null) {
        string thisLabel = gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
        string otherLabel = otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";

        UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                "For interaction to work properly please prevent collision between IHandModel "
                                                + "and InteractionBehavior. " + thisLabel + ", " + otherLabel,
                                                "Ok");
        Debug.Break();
      }
    }
#endif

    protected virtual void OnDrawGizmos() {
      if (IsRegisteredWithManager) {
        Matrix4x4 gizmosMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(_rigidbody.position, _rigidbody.rotation, Vector3.one);

        if (_rigidbody.IsSleeping()) {
          Gizmos.color = Color.gray;
        } else if (IsBeingGrasped) {
          Gizmos.color = Color.green;
        } else if (_showDebugRecievedVelocity) {
          Gizmos.color = Color.yellow;
        } else if (_ignoringBrushes) {
          Gizmos.color = Color.red;
        } else {
          Gizmos.color = Color.blue;
        }

        Gizmos.DrawWireCube(_debugBounds.center, _debugBounds.size);

        Gizmos.matrix = gizmosMatrix;
      }
    }
    #endregion

    #region INTERNAL

    protected void updateLayer() {
      int layer;
      if (_ignoringBrushes || !_manager.ContactEnabled || !_material.ContactEnabled) {
        if (_material.UseCustomLayers) {
          layer = _material.InteractionNoClipLayer;
        } else {
          layer = _manager.InteractionNoClipLayer;
        }
      } else {
        if (_material.UseCustomLayers) {
          layer = _material.InteractionLayer;
        } else {
          layer = _manager.InteractionLayer;
        }
      }

      if (gameObject.layer != layer) {
        for (int i = 0; i < _childrenArray.Length; i++) {
          _childrenArray[i].gameObject.layer = layer;
        }
      }
    }

    protected INTERACTION_TRANSFORM getRigidbodyTransform() {
      INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();

      if (IsBeingGrasped) {
        interactionTransform.position = _solvedPosition.ToCVector();
        interactionTransform.rotation = _solvedRotation.ToCQuaternion();
      } else {
        interactionTransform.position = _rigidbody.position.ToCVector();
        interactionTransform.rotation = _rigidbody.rotation.ToCQuaternion();
      }

      interactionTransform.wallTime = Time.fixedTime;
      return interactionTransform;
    }

    protected void resetState() {
      _rigidbody.useGravity = _useGravity;
      _rigidbody.isKinematic = _isKinematic;
    }

    protected virtual void updateState() {
      //Renderers are visible if there are no grasping hands
      //or if there is at least one tracked grasping hand
      int trackedGraspingHandCount = GraspingHandCount - UntrackedHandCount;
      bool shouldBeVisible = GraspingHandCount == 0 || trackedGraspingHandCount > 0 || !_material.HideObjectOnSuspend;

      for (int i = 0; i < _suspendableRenderers.Length; i++) {
        var renderer = _suspendableRenderers[i];
        if (renderer != null) {
          renderer.enabled = shouldBeVisible;
        }
      }

      //Update kinematic status of body
      if (IsBeingGrasped) {
        _rigidbody.useGravity = false;
        _rigidbody.drag = 0;
        _rigidbody.angularDrag = 0;

        switch (_material.GraspMethod) {
          case InteractionMaterial.GraspMethodEnum.Kinematic:
            _rigidbody.isKinematic = true;
            break;
          case InteractionMaterial.GraspMethodEnum.Velocity:
            if (UntrackedHandCount > 0) {
              _rigidbody.isKinematic = true;
            } else {
              _rigidbody.isKinematic = false;
            }
            break;
          default:
            throw new InvalidOperationException("Unexpected grasp method");
        }
      } else {
        _rigidbody.useGravity = _useGravity;
        _rigidbody.isKinematic = _isKinematic;
        _rigidbody.drag = _drag;
        _rigidbody.angularDrag = _angularDrag;
      }
    }

    protected void removeHandPointCollection(int handId) {
      var collection = _handIdToPoints[handId];
      _handIdToPoints.Remove(handId);

      //Return the collection to the pool so it can be re-used
      HandPointCollection.Return(collection);
    }

    protected void getSolvedTransform(ReadonlyList<Hand> hands, out Vector3 newPosition, out Quaternion newRotation) {
      KabschC.Reset(ref _kabsch);

      for (int h = 0; h < hands.Count; h++) {
        Hand hand = hands[h];

        var collection = _handIdToPoints[hand.Id];
        collection.UpdateTransform();

        for (int f = 0; f < NUM_FINGERS; f++) {
          Finger finger = hand.Fingers[f];
          Finger.FingerType fingerType = finger.Type;

          for (int j = 0; j < NUM_BONES; j++) {
            Bone.BoneType boneType = (Bone.BoneType)j;
            Bone bone = finger.Bone(boneType);

            Vector3 objectPos = collection.GetGlobalPosition(fingerType, boneType);
            Vector3 bonePos = bone.NextJoint.ToVector3();

            //Do the solve such that the objects positions are matched to the new bone positions
            LEAP_VECTOR point1 = (objectPos - _rigidbody.position).ToCVector();
            LEAP_VECTOR point2 = (bonePos - _rigidbody.position).ToCVector();

            KabschC.AddPoint(ref _kabsch, ref point1, ref point2, 1.0f);
          }
        }
      }

      KabschC.Solve(ref _kabsch);

      LEAP_VECTOR leapTranslation;
      LEAP_QUATERNION leapRotation;
      KabschC.GetTranslation(ref _kabsch, out leapTranslation);
      KabschC.GetRotation(ref _kabsch, out leapRotation);

      Vector3 solvedTranslation = leapTranslation.ToVector3();
      Quaternion solvedRotation = leapRotation.ToQuaternion();

      //Calculate new transform using delta
      newPosition = _rigidbody.position + solvedTranslation;
      newRotation = solvedRotation * _rigidbody.rotation; ;
    }

    protected class HandPointCollection {
      //Without a pool, you might end up with 2 instances per object
      //With a pool, likely there will only ever be 2 instances!
      private static Stack<HandPointCollection> _handPointCollectionPool = new Stack<HandPointCollection>();

      private Rigidbody _rigidbody;
      private Vector3[] _localPositions;

      private Matrix4x4 _transformMatrix;

      private bool _hasInverse = false;
      private Matrix4x4 _inverseTransformMatrix;

      public static HandPointCollection Create(Rigidbody rigidbody) {
        HandPointCollection collection;
        if (_handPointCollectionPool.Count != 0) {
          collection = _handPointCollectionPool.Pop();
        } else {
          collection = new HandPointCollection();
        }

        collection.init(rigidbody);
        return collection;
      }

      public static void Return(HandPointCollection handPointCollection) {
        handPointCollection.reset();
        _handPointCollectionPool.Push(handPointCollection);
      }

      private HandPointCollection() {
        _localPositions = new Vector3[NUM_FINGERS * NUM_BONES];
      }

      private void init(Rigidbody rigidbody) {
        _rigidbody = rigidbody;
      }

      private void reset() {
        _rigidbody = null;
        _hasInverse = false;
      }

      public void UpdateTransform() {
        Vector3 interactionPosition = _rigidbody.position;
        Quaternion interactionRotation = _rigidbody.rotation;

        _hasInverse = false;
        _transformMatrix = Matrix4x4.TRS(interactionPosition, interactionRotation, Vector3.one);
      }

      public void SetGlobalPosition(Vector3 globalPosition, Finger.FingerType fingerType, Bone.BoneType boneType) {
        if (!_hasInverse) {
          _inverseTransformMatrix = _transformMatrix.inverse;
          _hasInverse = true;
        }

        _localPositions[getIndex(fingerType, boneType)] = _inverseTransformMatrix.MultiplyPoint3x4(globalPosition);
      }

      public Vector3 GetGlobalPosition(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return _transformMatrix.MultiplyPoint3x4(_localPositions[getIndex(fingerType, boneType)]);
      }

      private int getIndex(Finger.FingerType fingerType, Bone.BoneType boneType) {
        return (int)fingerType * 4 + (int)boneType;
      }
    }
    #endregion
  }
}
