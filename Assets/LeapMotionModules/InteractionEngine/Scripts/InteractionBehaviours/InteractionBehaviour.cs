﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

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

    [Tooltip("A transform that acts as the parent of all renderers for this object.  By seperating out the graphical " +
             "representation from the physical, interaction fidelity can be improved and latency reduced.")]
    [SerializeField]
    protected Transform _graphicalAnchor;

    [Tooltip("How long it takes for the graphical anchor to return to the origin after a release.")]
    [SerializeField]
    protected float _graphicalReturnTime = 0.25f;

    [Tooltip("Should a hand be able to impart pushing forces to this object.")]
    [SerializeField]
    protected bool _enableContact = true;

    [Space]
    [Tooltip("Should advanved throwing settings be enabled.")]
    [SerializeField]
    protected bool _advancedThrowing = false;

    [Tooltip("A curve used to calculate a multiplier of the throwing velocity.  Maps original velocity to multiplier.")]
    [SerializeField]
    protected AnimationCurve _throwingVelocityCurve;

    [Tooltip("Measured in Meters per Second.  If the object is thrown faster than this speed, contact is disabled for a period of time.")]
    [SerializeField]
    protected float _contactDisableSpeed = 0.4f;

    [Tooltip("How much time after contact is disabled after a throw before it is re-enabled.")]
    [SerializeField]
    protected float _contactEnableDelay = 0.1f;

    protected Renderer[] _renderers;
    protected Rigidbody _rigidbody;

    protected bool _isKinematic;
    protected bool _useGravity;
    protected bool _recievedVelocityUpdate = false;
    protected bool _notifiedOfTeleport = false;

    protected Vector3 _solvedPosition;
    protected Quaternion _solvedRotation;

    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    protected Dictionary<int, HandPointCollection> _handIdToPoints;
    protected LEAP_IE_KABSCH _kabsch;

    private Coroutine _graphicalLerpCoroutine = null;

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
    /// Sets or Gets the transform used as the graphical anchor of this InteractionBehaviour.
    /// </summary>
    public Transform GraphicalAnchor {
      get {
        return _graphicalAnchor;
      }
      set {
        if (!value.IsChildOf(transform) || value == transform) {
          throw new ArgumentException("Cannot have a graphical anchor that is not a child of the InteractionBehaviour");
        }

        if (_graphicalLerpCoroutine != null) {
          StopCoroutine(_graphicalLerpCoroutine);
          _graphicalLerpCoroutine = null;
        }

        if (_graphicalAnchor != null) {
          _graphicalAnchor.gameObject.SetActive(true);
        }

        _graphicalAnchor = value;

        updateRendererStatus();
      }
    }

    /// <summary>
    /// Sets or Gets the amount of time the graphical anchor takes to return to it's neutral offset after
    /// a release has occured.
    /// </summary>
    public float GraphicalReturnTime {
      get {
        return _graphicalReturnTime;
      }
      set {
        _graphicalReturnTime = value;
      }
    }

    /// <summary>
    /// Sets or Gets whether or not pushing is enabled for this object.  Pushing will only be used if this is set to True
    /// AND if pushing is enabled for the InteractionManager that this behaviour is registered with.
    /// </summary>
    public bool PushingEnabled {
      get {
        return _enableContact;
      }
      set {
        _enableContact = value;
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
    /// <param name="acceleration"></param>
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

      //Technically we only need one instance in the entire scene, but easier for each object to have it's own instance for now.
      //TODO: Investigate allowing this to be a singleton?
      KabschC.Construct(ref _kabsch);
    }

    protected override void OnUnregistered() {
      base.OnUnregistered();

      _rigidbody = null;

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
        //If we did not recieve a velocity update, we set the rigidbody's gravity status
        //to match whatever the user has set.
        if (_rigidbody.useGravity != _useGravity) {
          _rigidbody.useGravity = _useGravity;
        }

        //Only apply if non-zero to prevent waking up the body
        if (_accumulatedLinearAcceleration != Vector3.zero) {
          _rigidbody.AddForce(_accumulatedLinearAcceleration, ForceMode.Acceleration);
        }

        if (_accumulatedAngularAcceleration != Vector3.zero) {
          _rigidbody.AddTorque(_accumulatedAngularAcceleration, ForceMode.Acceleration);
        }
      }

      //Reset so we can accumulate for the next frame
      _accumulatedLinearAcceleration = Vector3.zero;
      _accumulatedAngularAcceleration = Vector3.zero;
      _notifiedOfTeleport = false;
      _recievedVelocityUpdate = false;
    }

    public override void GetInteractionShapeCreationInfo(out INTERACTION_CREATE_SHAPE_INFO createInfo, out INTERACTION_TRANSFORM createTransform) {
      createInfo = new INTERACTION_CREATE_SHAPE_INFO();
      createInfo.gravity = Physics.gravity.ToCVector();
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

      //When the shape is destroyed, revert rigidbody status to user settings
      _rigidbody.isKinematic = _isKinematic;
      _rigidbody.useGravity = _useGravity;
    }

    public override void GetInteractionShapeUpdateInfo(out INTERACTION_UPDATE_SHAPE_INFO updateInfo, out INTERACTION_TRANSFORM interactionTransform) {
      updateInfo = new INTERACTION_UPDATE_SHAPE_INFO();

      updateInfo.updateFlags = UpdateInfoFlags.None;
      if (!_notifiedOfTeleport) {
        updateInfo.updateFlags |= UpdateInfoFlags.VelocityEnabled;
      }

      if (_enableContact && !_isKinematic && !IsBeingGrasped) {
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
          _enableContact) {
        //Use Sleep() to clear any forces that might have been applied by the user.
        _rigidbody.Sleep();
        _rigidbody.velocity = results.linearVelocity.ToVector3();
        _rigidbody.angularVelocity = results.angularVelocity.ToVector3();
        _recievedVelocityUpdate = true;

#if UNITY_EDITOR
        _showDebugRecievedVelocity = true;
#endif
      }
    }

    protected override void OnHandGrasped(Hand hand) {
      base.OnHandGrasped(hand);

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

    protected override void OnHandsHoldPhysics(List<Hand> hands) {
      base.OnHandsHoldPhysics(hands);

      //Get new transform
      Vector3 newPosition;
      Quaternion newRotation;
      getSolvedTransform(hands, out newPosition, out newRotation);

      _solvedPosition = newPosition;
      _solvedRotation = newRotation;

      //Apply new transform to object
      if (_notifiedOfTeleport) {
        _rigidbody.position = newPosition;
        _rigidbody.rotation = newRotation;
      } else {
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
      }
    }

    protected override void OnHandsHoldGraphics(List<Hand> hands) {
      base.OnHandsHoldGraphics(hands);

      if (_graphicalAnchor != null) {
        Vector3 newPosition;
        Quaternion newRotation;
        getSolvedTransform(hands, out newPosition, out newRotation);

        _graphicalAnchor.position = newPosition;
        _graphicalAnchor.rotation = newRotation;
      }
    }

    protected override void OnHandReleased(Hand hand) {
      base.OnHandReleased(hand);

      removeHandPointCollection(hand.Id);
    }

    protected override void OnHandLostTracking(Hand oldHand) {
      base.OnHandLostTracking(oldHand);

      updateRendererStatus();
    }

    protected override void OnHandRegainedTracking(Hand newHand, int oldId) {
      base.OnHandRegainedTracking(newHand, oldId);

      updateRendererStatus();

      //Associate the collection with the new id
      var collection = _handIdToPoints[oldId];
      _handIdToPoints.Remove(oldId);
      _handIdToPoints[newHand.Id] = collection;

      NotifyTeleported();
    }

    protected override void OnHandTimeout(Hand oldHand) {
      base.OnHandTimeout(oldHand);

      updateRendererStatus();
      removeHandPointCollection(oldHand.Id);
    }

    protected override void OnGraspBegin() {
      base.OnGraspBegin();

      //Stop an existing lerp coroutine if it exists to prevent conflict
      if (_graphicalLerpCoroutine != null) {
        StopCoroutine(_graphicalLerpCoroutine);
      }

      //A grasp always causes the object to become kinematic
      _rigidbody.isKinematic = true;
    }

    protected override void OnGraspEnd() {
      base.OnGraspEnd();

      //If there is a graphical anchor, we are going to lerp it back to match the 
      //position and rotation of the rigidbody, since it might have diverged.
      if (_graphicalAnchor != null) {
        _graphicalLerpCoroutine = StartCoroutine(lerpGraphicalToOrigin());
      }

      //Revert the kinematic status of the Rigidbody to the user setting once the grasp is finished.
      _rigidbody.isKinematic = _isKinematic;

      if (_advancedThrowing) {
        float speed = _rigidbody.velocity.magnitude;
        float multiplier = _throwingVelocityCurve.Evaluate(speed);
        _rigidbody.velocity *= multiplier;

        if (_enableContact && speed >= _contactDisableSpeed) {
          _enableContact = false;
          StartCoroutine(enableContactAfterDelay());
        }
      }
    }
    #endregion

    #region UNITY CALLBACKS
    protected override void Reset() {
      base.Reset();

      _throwingVelocityCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                  new Keyframe(1.0f, 1.0f, 0.0f, 0.0f),
                                                  new Keyframe(2.0f, 1.5f, 0.0f, 0.0f));
    }

    protected virtual void Awake() {
      _handIdToPoints = new Dictionary<int, HandPointCollection>();
    }

    protected IEnumerator enableContactAfterDelay() {
      yield return new WaitForSeconds(_contactEnableDelay);
      _enableContact = true;
    }

    protected IEnumerator lerpGraphicalToOrigin() {
      //We lerp position in world space instead of local space
      //This helps remove wobbles when the object is rotating
      Vector3 globalPosOffset = _graphicalAnchor.position - transform.position;
      Quaternion startRot = _graphicalAnchor.localRotation;
      float startTime = Time.time;

      while (true) {
        yield return null;

        //Using sigmoid to help hide the lerp
        float t = Mathf.InverseLerp(startTime, startTime + _graphicalReturnTime, Time.time);
        float percent = t * t * (3 - 2 * t);

        //Lerp based on transform.position instead of rigidbody.position to reduce stutter
        _graphicalAnchor.position = transform.position + Vector3.Lerp(globalPosOffset, Vector3.zero, percent);
        _graphicalAnchor.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, percent);

        if (percent >= 1.0f) {
          break;
        }
      }

      //Null out coroutine reference when finished
      _graphicalLerpCoroutine = null;
    }

#if UNITY_EDITOR
    private void OnCollisionEnter(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      if (otherObj.GetComponentInParent<IHandModel>() != null) {
        UnityEditor.EditorUtility.DisplayDialog("Collision Detected!",
                                                "A collision between an InteractionBehaviour and a Hand was detected!  " +
                                                "For interaction to work properly please disable collision between interaction.",
                                                "Ok");
        Debug.Break();
      }
    }
#endif

    protected virtual void OnDrawGizmos() {
      if (IsRegisteredWithManager) {
        Matrix4x4 gizmosMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        if (_rigidbody.IsSleeping()) {
          Gizmos.color = Color.gray;
        } else if (IsBeingGrasped) {
          Gizmos.color = Color.green;
        } else if (_showDebugRecievedVelocity) {
          Gizmos.color = Color.yellow;
        } else {
          Gizmos.color = Color.blue;
        }

        Gizmos.DrawWireCube(_debugBounds.center, _debugBounds.size);

        Gizmos.matrix = gizmosMatrix;
      }
    }
    #endregion

    #region INTERNAL
    protected INTERACTION_TRANSFORM getRigidbodyTransform() {
      INTERACTION_TRANSFORM interactionTransform = new INTERACTION_TRANSFORM();
      interactionTransform.position = _solvedPosition.ToCVector();
      interactionTransform.rotation = _solvedRotation.ToCQuaternion();
      interactionTransform.wallTime = Time.fixedTime;
      return interactionTransform;
    }

    protected virtual void updateRendererStatus() {
      //Renderers are visible if there are no grasping hands
      //or if there is at least one tracked grasping hand
      int trackedGraspingHandCount = GraspingHandCount - UntrackedHandCount;
      bool shouldBeVisible = GraspingHandCount == 0 || trackedGraspingHandCount > 0;

      if (_graphicalAnchor != null) {
        _graphicalAnchor.gameObject.SetActive(shouldBeVisible);
      }
    }

    protected void removeHandPointCollection(int handId) {
      var collection = _handIdToPoints[handId];
      _handIdToPoints.Remove(handId);

      //Return the collection to the pool so it can be re-used
      HandPointCollection.Return(collection);
    }

    protected void getSolvedTransform(List<Hand> hands, out Vector3 newPosition, out Quaternion newRotation) {
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
