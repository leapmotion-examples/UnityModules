﻿using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// InteractionManager is the core behaviour that manages the IInteractionBehaviours in the scene.
  /// This class allows IInteractionBehaviours to register with it and provides all of the callbacks
  /// needed for operation.  This class also takes care of all bookkeeping to keep track of the objects,
  /// hands, and the internal state of the interaction plugin.
  /// </summary>
  ///
  /// <remarks>
  /// InteractionManager has the following features:
  ///    - Allows instances of IInteractionBehaviour to register or unregister with it.
  ///    - Registered instances stay registered even if this behaviour is disabled.
  ///    - Dispatches events to the interaction plugin and uses the returned data to drive the registered
  ///      behaviours.  Takes care of all bookkeeping needed to maintain the internal state.
  ///    - Supports the concept of 'suspension', where an untracked hand is still allowed to be considered
  ///      grasping an object.  This is to allow an object to not fall when a hand becomes untracked for
  ///      a small amount of time.  This helps with actions such as throwing.
  ///    - Multiple instances of InteractionManager are ALLOWED!  This allows you to have different simulation
  ///      settings and control for different groups of objects.
  ///
  /// InteractionManager has the following requirements:
  ///    - The DataSubfolder property must point to a valid subfolder in the StreamingAssets data folder.
  ///      The subfolder must contain a valid ldat file names IE.
  /// </remarks>
  public class InteractionManager : MonoBehaviour {
    #region SERIALIZED FIELDS
    [SerializeField]
    protected LeapProvider _leapProvider;

    [Tooltip("The streaming asset subolder of the data folder for the engine.")]
    [SerializeField]
    protected string _dataSubfolder = "InteractionEngine";

    [Header("Interaction Settings")]
    [Tooltip("Allow the Interaction Engine to modify object velocities when pushing.")]
    [SerializeField]
    protected bool _contactEnabled = true;

    [Tooltip("Allow the Interaction plugin to modify object positions by grasping.")]
    [SerializeField]
    protected bool _graspingEnabled = true;

    [Tooltip("Depth before collision response becomes as if holding a sphere.")]
    [SerializeField]
    protected float _depthUntilSphericalInside = 0.023f;

    [Header("Layer Settings")]
    [SerializeField]
    protected bool _autoGenerateLayers = false;

    [Tooltip("Layer to use for auto-generation.  The generated interaction layers will have the same collision settings as this layer.")]
    [SerializeField]
    protected SingleLayer _templateLayer = 0;

    [SerializeField]
    protected SingleLayer _interactionLayer = 0;

    [SerializeField]
    protected SingleLayer _brushHandLayer = 0;

    [SerializeField]
    protected SingleLayer _interactionNoClipLayer = 0;

    [Header("Debug")]
    [Tooltip("Allows simulation to be disabled without destroying the scene in any way.")]
    [SerializeField]
    protected bool _pauseSimulation = false;

    [Tooltip("Shows the debug visualization coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugLines = false;

    [Tooltip("Shows the debug messages coming from the internal Interaction plugin.")]
    [SerializeField]
    protected bool _showDebugOutput = false;
    #endregion

    #region INTERNAL FIELDS
    private static UInt32 _expectedVersion = 1;
    protected INTERACTION_SCENE _scene;
    private bool _hasSceneBeenCreated = false;

    protected ShapeDescriptionPool _shapeDescriptionPool;

    protected List<IInteractionBehaviour> _registeredBehaviours = new List<IInteractionBehaviour>();
    protected HashSet<IInteractionBehaviour> _misbehavingBehaviours = new HashSet<IInteractionBehaviour>();

    //Maps the Interaction instance handle to the behaviour
    //A mapping only exists if a shape instance has been created
    protected Dictionary<INTERACTION_SHAPE_INSTANCE_HANDLE, IInteractionBehaviour> _instanceHandleToBehaviour = new Dictionary<INTERACTION_SHAPE_INSTANCE_HANDLE, IInteractionBehaviour>();

    protected Dictionary<int, InteractionHand> _idToInteractionHand = new Dictionary<int, InteractionHand>();
    protected List<IInteractionBehaviour> _graspedBehaviours = new List<IInteractionBehaviour>();

    //A temp list that is recycled.  Used to remove items from _handIdToIeHand.
    private List<int> _handIdsToRemove = new List<int>();
    //A temp list that is recycled.  Used as the argument to OnHandsHold.
    private List<Hand> _holdingHands = new List<Hand>();
    //A temp list that is recycled.  Used to recieve results from InteractionC.
    private List<INTERACTION_SHAPE_INSTANCE_RESULTS> _resultList = new List<INTERACTION_SHAPE_INSTANCE_RESULTS>();
    //A temp list that is recycled.  Used to recieve debug logs from InteractionC.
    private List<string> _debugOutput = new List<string>();
    #endregion

    #region PUBLIC METHODS
    /// <summary>
    /// Gets the current debug flags for this manager.
    /// </summary>
    public virtual DebugFlags DebugFlags {
      get {
        DebugFlags flags = DebugFlags.None;
        if (_showDebugLines) {
          flags |= DebugFlags.Lines;
        }
        if (_showDebugOutput) {
          flags |= DebugFlags.Strings;
          flags |= DebugFlags.Logging;
        }
        return flags;
      }
    }

    /// <summary>
    /// Returns a ShapeDescriptionPool that can be used to allocate shape descriptions
    /// for this manager.  Using the pool can be more efficient since identical shapes
    /// can be automatically combined to save memory.  Shape descriptions aquired from this
    /// pool will be destroyed when this manager is disabled.
    /// </summary>
    public ShapeDescriptionPool ShapePool {
      get {
        return _shapeDescriptionPool;
      }
    }

    /// <summary>
    /// Returns true if any InteractionObject is currently being grasped by at least one Hand.
    /// </summary>
    public bool IsAnyObjectGrasped {
      get {
        return _graspedBehaviours.Count != 0;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionBehaviours that are currently registered with this manager.
    /// </summary>
    public ReadonlyList<IInteractionBehaviour> RegisteredObjects {
      get {
        return _registeredBehaviours;
      }
    }

    /// <summary>
    /// Returns a collection of InteractionBehaviours that are currently being grasped by
    /// at least one hand.
    /// </summary>
    public ReadonlyList<IInteractionBehaviour> GraspedObjects {
      get {
        return _graspedBehaviours;
      }
    }

    /// <summary>
    /// Gets whether or not the Interaction Engine can modify object velocities when pushing.
    /// </summary>
    public bool ContactEnabled {
      get {
        return _contactEnabled;
      }
    }

    /// <summary>
    /// Gets whether or not the Interaction plugin to modify object positions by grasping.
    /// </summary>
    public bool GraspingEnabled {
      get {
        return _graspingEnabled;
      }
    }

    /// <summary>
    /// Gets the layer that interaction objects should be on by default.
    /// </summary>
    public int InteractionLayer {
      get {
        return _interactionLayer;
      }
      set {
        _interactionLayer = value;
      }
    }

    /// <summary>
    /// Gets the layer that interaction objects should be on when they become grasped.
    /// </summary>
    public int InteractionNoClipLayer {
      get {
        return _interactionNoClipLayer;
      }
      set {
        _interactionNoClipLayer = value;
      }
    }

    /// <summary>
    /// Gets the layer that interaction brushes should be on.
    /// </summary>
    public int InteractionBrushLayer {
      get {
        return _brushHandLayer;
      }
      set {
        _brushHandLayer = value;
      }
    }

    /// Force an update of the internal scene info.  This should be called if settings have been changed like
    /// gravity.
    /// </summary>
    public void UpdateSceneInfo() {
      var info = getSceneInfo();
      InteractionC.UpdateSceneInfo(ref _scene, ref info);
    }

    /// <summary>
    /// Tries to find an InteractionObject that is currently being grasped by a Hand with
    /// the given ID.
    /// </summary>
    public bool TryGetGraspedObject(int handId, out IInteractionBehaviour graspedObject) {
      for (int i = 0; i < _graspedBehaviours.Count; i++) {
        var iObj = _graspedBehaviours[i];
        if (iObj.IsBeingGraspedByHand(handId)) {
          graspedObject = iObj;
          return true;
        }
      }

      graspedObject = null;
      return false;
    }

    /// <summary>
    /// Forces the given object to be released by any hands currently holding it.  Will return true
    /// only if there was at least one hand holding the object.
    /// </summary>
    public bool ReleaseObject(IInteractionBehaviour graspedObject) {
      if (!_graspedBehaviours.Remove(graspedObject)) {
        return false;
      }

      INTERACTION_HAND_RESULT result = new INTERACTION_HAND_RESULT();
      result.classification = ManipulatorMode.Contact;
      result.handFlags = HandResultFlags.ManipulatorMode;
      result.instanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();

      foreach (var interactionHand in _idToInteractionHand.Values) {
        if (interactionHand.graspedObject == graspedObject) {
          if (_graspingEnabled) {
            InteractionC.OverrideHandResult(ref _scene, (uint)interactionHand.hand.Id, ref result);
          }
          interactionHand.ReleaseObject();
        }
      }

      return true;
    }

    /// <summary>
    /// Forces a hand with the given id to release an object if it is holding it.  Will return true
    /// only if a hand with the given id releases an object it was holding.
    /// </summary>
    public bool ReleaseHand(int handId) {
      InteractionHand interactionHand;
      if (!_idToInteractionHand.TryGetValue(handId, out interactionHand)) {
        return false;
      }

      if (interactionHand.graspedObject == null) {
        return false;
      }

      if (interactionHand.graspedObject.GraspingHandCount == 1) {
        _graspedBehaviours.Remove(interactionHand.graspedObject);
      }

      interactionHand.ReleaseObject();
      return true;
    }

    /// <summary>
    /// Forces a hand to grasp the given interaction behaviour.  The grasp will only be terminated when 
    /// the hand either times out or the user calls ReleaseHand.
    /// </summary>
    /// <param name="hand"></param>
    public void GraspWithHand(Hand hand, IInteractionBehaviour interactionBehaviour) {
      if (!_registeredBehaviours.Contains(interactionBehaviour)) {
        throw new InvalidOperationException("Cannot grasp " + interactionBehaviour + " because it is not registered with this manager.");
      }

      if (!interactionBehaviour.IsBeingGrasped) {
        _graspedBehaviours.Add(interactionBehaviour);
      }

      InteractionHand interactionHand = _idToInteractionHand[hand.Id];
      interactionHand.GraspObject(interactionBehaviour, isUserGrasp: true);
    }

    /// <summary>
    /// Registers an InteractionObject with this manager, which automatically adds the objects
    /// representation into the internal interaction scene.  If the manager is disabled,
    /// the registration will still succeed and the object will be added to the internal scene
    /// when the manager is next enabled.
    /// </summary>
    public void RegisterInteractionBehaviour(IInteractionBehaviour interactionBehaviour) {
      if (_registeredBehaviours.Contains(interactionBehaviour)) {
        throw new InvalidOperationException("Interaction Behaviour " + interactionBehaviour + " cannot be registered because " +
                                            "it is already registered with this manager.");
      }

      _registeredBehaviours.Add(interactionBehaviour);

      try {
        interactionBehaviour.NotifyRegistered();
      } catch (Exception e) {
        _misbehavingBehaviours.Add(interactionBehaviour);
        throw e;
      }

      //Don't create right away if we are not enabled, creation will be done in OnEnable
      if (_hasSceneBeenCreated) {
        createInteractionShape(interactionBehaviour);
      }
    }

    /// <summary>
    /// Unregisters an InteractionObject from this manager.  This removes it from the internal
    /// scene and prevents any further interaction.
    /// </summary>
    public void UnregisterInteractionBehaviour(IInteractionBehaviour interactionBehaviour) {
      if (!_registeredBehaviours.Contains(interactionBehaviour)) {
        throw new InvalidOperationException("Interaction Behaviour " + interactionBehaviour + " cannot be unregistered because " +
                                            "it is not currently registered with this manager.");
      }

      _registeredBehaviours.Remove(interactionBehaviour);

      if (_graspedBehaviours.Remove(interactionBehaviour)) {
        foreach (var interactionHand in _idToInteractionHand.Values) {
          if (interactionHand.graspedObject == interactionBehaviour) {
            try {
              interactionHand.ReleaseObject();
            } catch (Exception e) {
              //Only log to console
              //We want to continue so we can destroy the shape and dispatch OnUnregister
              Debug.LogException(e);
            }
            break;
          }
        }
      }

      //Don't destroy if we are not enabled, everything already got destroyed in OnDisable
      if (_hasSceneBeenCreated) {
        try {
          destroyInteractionShape(interactionBehaviour);
        } catch (Exception e) {
          //Like above, only log to console so we can dispatch OnUnregister
          Debug.LogException(e);
        }
      }

      interactionBehaviour.NotifyUnregistered();
    }
    #endregion

    #region UNITY CALLBACKS
    protected virtual void Reset() {
      if (_leapProvider == null) {
        _leapProvider = FindObjectOfType<LeapProvider>();
      }
    }

    protected virtual void OnValidate() {
      if (Application.isPlaying && _hasSceneBeenCreated) {
        //Allow the debug lines to be toggled while the scene is playing
        applyDebugSettings();
        //Allow scene info to be updated while the scene is playing
        UpdateSceneInfo();
      }

      if (!Application.isPlaying && _autoGenerateLayers) {
        autoGenerateLayers();
      }
    }

    protected virtual void Awake() {
      if (_autoGenerateLayers) {
        autoGenerateLayers();
        autoSetupCollisionLayers();
      }
    }

    protected virtual void OnEnable() {
      if (_leapProvider == null) {
        enabled = false;
        Debug.LogError("Could not enable Interaction Manager because no Leap Provider was specified.");
        return;
      }

      Assert.IsFalse(_hasSceneBeenCreated, "Scene should not have been created yet");

      try {
        createScene();
        applyDebugSettings();
      } catch (Exception e) {
        enabled = false;
        throw e;
      }

      _shapeDescriptionPool = new ShapeDescriptionPool(_scene);

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "There should not be any instances before the creation step.");

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        IInteractionBehaviour interactionBehaviour = _registeredBehaviours[i];
        try {
          createInteractionShape(interactionBehaviour);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void OnDisable() {
      foreach (var interactionHand in _idToInteractionHand.Values) {
        IInteractionBehaviour graspedBehaviour = interactionHand.graspedObject;
        if (graspedBehaviour != null) {
          try {
            interactionHand.ReleaseObject();
          } catch (Exception e) {
            _misbehavingBehaviours.Add(graspedBehaviour);
            Debug.LogException(e);
          }
        }
      }

      unregisterMisbehavingBehaviours();

      _idToInteractionHand.Clear();
      _graspedBehaviours.Clear();

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        try {
          destroyInteractionShape(_registeredBehaviours[i]);
        } catch (Exception e) {
          Debug.LogException(e);
        }
      }

      Assert.AreEqual(_instanceHandleToBehaviour.Count, 0, "All instances should have been destroyed.");

      if (_shapeDescriptionPool != null) {
        _shapeDescriptionPool.RemoveAllShapes();
        _shapeDescriptionPool = null;
      }

      if (_hasSceneBeenCreated) {
        destroyScene();
      }
    }

    protected virtual void FixedUpdate() {
      if (!_pauseSimulation) {
        simulateFrame(_leapProvider.CurrentFixedFrame);
      }

      if (_showDebugLines) {
        InteractionC.DrawDebugLines(ref _scene);
      }

      if (_showDebugOutput) {
        InteractionC.GetDebugStrings(ref _scene, _debugOutput);
      }
    }

    protected virtual void Update() {
      if (_pauseSimulation) {
        return;
      }

      dispatchOnHandsHolding(_leapProvider.CurrentFrame, isPhysics: false);

      unregisterMisbehavingBehaviours();
    }

    protected virtual void OnGUI() {
      if (_showDebugOutput) {
        for (int i = 0; i < _debugOutput.Count; i++) {
          GUILayout.Label(_debugOutput[i]);
        }
      }
    }
    #endregion

    #region INTERNAL METHODS
    protected void autoGenerateLayers() {
      _interactionLayer = -1;
      _brushHandLayer = -1;
      _interactionNoClipLayer = -1;
      for (int i = 8; i < 32; i++) {
        string layerName = LayerMask.LayerToName(i);
        if (string.IsNullOrEmpty(layerName)) {
          if (_interactionLayer == -1) {
            _interactionLayer = i;
          } else if (_brushHandLayer == -1) {
            _brushHandLayer = i;
          } else {
            _interactionNoClipLayer = i;
            break;
          }
        }
      }

      if (_interactionLayer == -1 || _brushHandLayer == -1 || _interactionNoClipLayer == -1) {
        if (Application.isPlaying) {
          enabled = false;
        }
        Debug.LogError("InteractionManager Could not find enough free layers for auto-setup, manual setup required.");
        _autoGenerateLayers = false;
        return;
      }
    }

    private void autoSetupCollisionLayers() {
      for (int i = 0; i < 32; i++) {
        //Copy ignore settings from template layer
        bool shouldIgnore = Physics.GetIgnoreLayerCollision(_templateLayer, i);

        Physics.IgnoreLayerCollision(_interactionLayer, i, shouldIgnore);
        Physics.IgnoreLayerCollision(_interactionNoClipLayer, i, shouldIgnore);

        //Set brush layer to collide with nothing
        Physics.IgnoreLayerCollision(_brushHandLayer, i, true);
      }

      //After copy and set we specify the interactions between the brush and interaction objects
      Physics.IgnoreLayerCollision(_brushHandLayer, _interactionLayer, false);
    }

    protected virtual void simulateFrame(Frame frame) {
      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        _registeredBehaviours[i].NotifyPreSolve();
      }

      dispatchOnHandsHolding(frame, isPhysics: true);

      updateInteractionRepresentations();

      updateTracking(frame);

      simulateInteraction();

      bool didAnyHandGrasp;
      updateInteractionStateChanges(frame, out didAnyHandGrasp);

      dispatchSimulationResults();

      if (didAnyHandGrasp) {
        dispatchOnHandsHolding(frame, isPhysics: true);
      }

      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        _registeredBehaviours[i].NotifyPostSolve();
      }
    }

    protected virtual void applyDebugSettings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags);
    }

    protected virtual void updateInteractionRepresentations() {
      for (int i = 0; i < _registeredBehaviours.Count; i++) {
        IInteractionBehaviour interactionBehaviour = _registeredBehaviours[i];
        try {
          INTERACTION_SHAPE_INSTANCE_HANDLE shapeInstanceHandle = interactionBehaviour.ShapeInstanceHandle;

          INTERACTION_UPDATE_SHAPE_INFO updateInfo;
          INTERACTION_TRANSFORM updateTransform;
          interactionBehaviour.GetInteractionShapeUpdateInfo(out updateInfo, out updateTransform);

          InteractionC.UpdateShapeInstance(ref _scene, ref updateTransform, ref updateInfo, ref shapeInstanceHandle);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void dispatchOnHandsHolding(Frame frame, bool isPhysics) {
      var hands = frame.Hands;

      //Loop through the currently grasped objects to dispatch their OnHandsHold callback
      for (int i = 0; i < _graspedBehaviours.Count; i++) {
        var interactionBehaviour = _graspedBehaviours[i];

        for (int j = 0; j < hands.Count; j++) {
          var hand = hands[j];
          InteractionHand interactionHand;
          if (_idToInteractionHand.TryGetValue(hand.Id, out interactionHand)) {
            if (interactionHand.graspedObject == interactionBehaviour) {
              _holdingHands.Add(hand);
            }
          }
        }

        try {
          if (isPhysics) {
            interactionBehaviour.NotifyHandsHoldPhysics(_holdingHands);
          } else {
            interactionBehaviour.NotifyHandsHoldGraphics(_holdingHands);
          }
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }

        _holdingHands.Clear();
      }
    }

    protected virtual void updateTracking(Frame frame) {
      int handCount = frame.Hands.Count;
      IntPtr ptr = HandArrayBuilder.CreateHandArray(frame);
      InteractionC.UpdateHands(ref _scene, (uint)handCount, ptr);
      StructAllocator.CleanupAllocations();
    }

    protected virtual void simulateInteraction() {
      var _controllerTransform = new INTERACTION_TRANSFORM();
      _controllerTransform.position = _leapProvider.transform.position.ToCVector();
      _controllerTransform.rotation = _leapProvider.transform.rotation.ToCQuaternion();
      _controllerTransform.wallTime = Time.fixedTime;

      InteractionC.UpdateController(ref _scene, ref _controllerTransform);
    }

    protected virtual void updateInteractionStateChanges(Frame frame, out bool didAnyHandBeginGrasp) {
      didAnyHandBeginGrasp = false;
      var hands = frame.Hands;

      //First loop through all the hands and get their classifications from the engine
      for (int i = 0; i < hands.Count; i++) {
        Hand hand = hands[i];

        bool hasHandResult = false;
        INTERACTION_HAND_RESULT handResult = new INTERACTION_HAND_RESULT();

        //Get the InteractionHand associated with this hand id
        InteractionHand interactionHand;
        if (!_idToInteractionHand.TryGetValue(hand.Id, out interactionHand)) {

          //First we see if there is an untracked interactionHand that can be re-connected using this one
          InteractionHand untrackedInteractionHand = null;
          foreach (var pair in _idToInteractionHand) {
            //If the old ieHand is untracked, and the handedness matches, we re-connect it
            if (pair.Value.isUntracked && pair.Value.hand.IsLeft == hand.IsLeft) {
              untrackedInteractionHand = pair.Value;
              break;
            }
          }

          if (untrackedInteractionHand != null) {
            //If we found an untrackedIeHand, use it!
            interactionHand = untrackedInteractionHand;
            //Remove the old id from the mapping
            _idToInteractionHand.Remove(untrackedInteractionHand.hand.Id);

            try {
              //This also dispatched InteractionObject.OnHandRegainedTracking()
              interactionHand.RegainTracking(hand);
            } catch (Exception e) {
              _misbehavingBehaviours.Add(interactionHand.graspedObject);
              Debug.LogException(e);
              continue;
            }

            //Override the existing classification to force the hand to grab the old object
            hasHandResult = true;
            handResult = new INTERACTION_HAND_RESULT();
            handResult.classification = ManipulatorMode.Grasp;
            handResult.handFlags = HandResultFlags.ManipulatorMode;
            handResult.instanceHandle = interactionHand.graspedObject.ShapeInstanceHandle;

            if (_graspingEnabled) {
              InteractionC.OverrideHandResult(ref _scene, (uint)hand.Id, ref handResult);
            }
          } else {
            //Otherwise just create a new one
            interactionHand = new InteractionHand(hand);
          }

          //In both cases, associate the id with the new ieHand
          _idToInteractionHand[hand.Id] = interactionHand;
        }

        if (!hasHandResult) {
          handResult = getHandResults(interactionHand);
          hasHandResult = true;
        }

        interactionHand.UpdateHand(hand);

        if (!interactionHand.isUserGrasp) {
          switch (handResult.classification) {
            case ManipulatorMode.Grasp:
              {
                IInteractionBehaviour interactionBehaviour;
                if (_instanceHandleToBehaviour.TryGetValue(handResult.instanceHandle, out interactionBehaviour)) {
                  if (interactionHand.graspedObject == null) {
                    didAnyHandBeginGrasp = true;

                    if (!interactionBehaviour.IsBeingGrasped) {
                      _graspedBehaviours.Add(interactionBehaviour);
                    }

                    try {
                      interactionHand.GraspObject(interactionBehaviour, isUserGrasp: false);
                    } catch (Exception e) {
                      _misbehavingBehaviours.Add(interactionBehaviour);
                      Debug.LogException(e);
                      continue;
                    }
                  }
                } else {
                  Debug.LogError("Recieved a hand result with an unkown handle " + handResult.instanceHandle.handle);
                }
                break;
              }
            case ManipulatorMode.Contact:
              {
                if (interactionHand.graspedObject != null) {
                  if (interactionHand.graspedObject.GraspingHandCount == 1) {
                    _graspedBehaviours.Remove(interactionHand.graspedObject);
                  }

                  try {
                    interactionHand.ReleaseObject();
                  } catch (Exception e) {
                    _misbehavingBehaviours.Add(interactionHand.graspedObject);
                    Debug.LogException(e);
                    continue;
                  }
                }
                break;
              }
            default:
              throw new InvalidOperationException("Unexpected classification " + handResult.classification);
          }
        }
      }

      //Loop through all ieHands to check for timeouts and loss of tracking
      foreach (var pair in _idToInteractionHand) {
        var id = pair.Key;
        var ieHand = pair.Value;

        float handAge = Time.unscaledTime - ieHand.lastTimeUpdated;
        //Check to see if the hand is at least 1 frame old
        //We assume it has become untracked if this is the case
        if (handAge > 0) {
          //If the hand isn't grasping anything, just remove it
          if (ieHand.graspedObject == null) {
            _handIdsToRemove.Add(id);
            continue;
          }

          //If is isn't already marked as untracked, mark it as untracked
          if (!ieHand.isUntracked) {
            try {
              //This also dispatches InteractionObject.OnHandLostTracking()
              ieHand.MarkUntracked();
            } catch (Exception e) {
              _misbehavingBehaviours.Add(ieHand.graspedObject);
              Debug.LogException(e);
            }
          }

          //If the age is longer than the timeout, we also remove it from the list
          if (handAge >= ieHand.maxSuspensionTime) {
            _handIdsToRemove.Add(id);

            try {
              if (ieHand.graspedObject.GraspingHandCount == 1) {
                _graspedBehaviours.Remove(ieHand.graspedObject);
              }

              //This also dispatched InteractionObject.OnHandTimeout()
              ieHand.MarkTimeout();
            } catch (Exception e) {
              _misbehavingBehaviours.Add(ieHand.graspedObject);
              Debug.LogException(e);
            }
          }
        }
      }

      //Loop through the stale ids and remove them from the map
      for (int i = 0; i < _handIdsToRemove.Count; i++) {
        _idToInteractionHand.Remove(_handIdsToRemove[i]);
      }
      _handIdsToRemove.Clear();
    }

    protected virtual INTERACTION_HAND_RESULT getHandResults(InteractionHand hand) {
      if (!_graspingEnabled) {
        INTERACTION_HAND_RESULT result = new INTERACTION_HAND_RESULT();
        result.classification = ManipulatorMode.Contact;
        result.handFlags = HandResultFlags.ManipulatorMode;
        result.instanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();
        return result;
      }


      INTERACTION_HAND_RESULT handResult;
      InteractionC.GetHandResult(ref _scene,
                                     (uint)hand.hand.Id,
                                 out handResult);
      return handResult;
    }

    protected virtual void dispatchSimulationResults() {
      InteractionC.GetShapeInstanceResults(ref _scene, _resultList);

      for (int i = 0; i < _resultList.Count; ++i) {
        INTERACTION_SHAPE_INSTANCE_RESULTS result = _resultList[i];
        IInteractionBehaviour interactionBehaviour = _instanceHandleToBehaviour[result.handle];

        try {
          // ShapeInstanceResultFlags.None may be returned if requested when hands are not touching.
          interactionBehaviour.NotifyRecievedSimulationResults(result);
        } catch (Exception e) {
          _misbehavingBehaviours.Add(interactionBehaviour);
          Debug.LogException(e);
        }
      }
    }

    protected virtual void createInteractionShape(IInteractionBehaviour interactionBehaviour) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE descriptionHandle = interactionBehaviour.ShapeDescriptionHandle;
      INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle = new INTERACTION_SHAPE_INSTANCE_HANDLE();

      INTERACTION_CREATE_SHAPE_INFO createInfo;
      INTERACTION_TRANSFORM createTransform;
      interactionBehaviour.GetInteractionShapeCreationInfo(out createInfo, out createTransform);

      InteractionC.CreateShapeInstance(ref _scene, ref descriptionHandle, ref createTransform, ref createInfo, out instanceHandle);

      _instanceHandleToBehaviour[instanceHandle] = interactionBehaviour;

      interactionBehaviour.NotifyInteractionShapeCreated(instanceHandle);
    }

    protected virtual void destroyInteractionShape(IInteractionBehaviour interactionBehaviour) {
      INTERACTION_SHAPE_INSTANCE_HANDLE instanceHandle = interactionBehaviour.ShapeInstanceHandle;

      _instanceHandleToBehaviour.Remove(instanceHandle);

      InteractionC.DestroyShapeInstance(ref _scene, ref instanceHandle);

      interactionBehaviour.NotifyInteractionShapeDestroyed();
    }

    private void unregisterMisbehavingBehaviours() {
      if (_misbehavingBehaviours.Count > 0) {
        foreach (var interactionBehaviour in _misbehavingBehaviours) {
          if (interactionBehaviour != null) {
            try {
              UnregisterInteractionBehaviour(interactionBehaviour);
            } catch (Exception e) {
              Debug.LogException(e);
            }
          }
        }
        _misbehavingBehaviours.Clear();
      }
    }

    protected virtual void createScene() {
#if UNITY_EDITOR
      UInt32 version = InteractionC.GetVersion();
      if (InteractionC.GetVersion() != _expectedVersion) {
        _scene.pScene = (IntPtr)0;
        UnityEditor.EditorUtility.DisplayDialog("Version Error!",
                                                "Leap Interaction dll version expected: " + _expectedVersion + " got version: " + version,
                                                "Ok");
        throw new Exception("Leap Interaction version wrong");
      }
#endif // UNITY_EDITOR

      INTERACTION_SCENE_INFO sceneInfo = getSceneInfo();
      string dataPath = Path.Combine(Application.streamingAssetsPath, _dataSubfolder);
      InteractionC.CreateScene(ref _scene, ref sceneInfo, dataPath);

      _hasSceneBeenCreated = true;
    }

    protected virtual void destroyScene() {
      InteractionC.DestroyScene(ref _scene);
      _hasSceneBeenCreated = false;
    }

    private INTERACTION_SCENE_INFO getSceneInfo() {
      INTERACTION_SCENE_INFO info = new INTERACTION_SCENE_INFO();
      info.gravity = Physics.gravity.ToCVector();

      info.sceneFlags = SceneInfoFlags.HasGravity | SceneInfoFlags.SphericalInside;
      info.depthUntilSphericalInside = _depthUntilSphericalInside;

      if (_contactEnabled) {
        info.sceneFlags |= SceneInfoFlags.ContactEnabled;
      }

      if (_graspingEnabled) {
        info.sceneFlags |= SceneInfoFlags.GraspEnabled;
      }

      return info;
    }

    //A persistant structure for storing useful data about a hand as it interacts with objects
    //TODO: Investigate pooling?
    protected class InteractionHand {
      public Hand hand { get; protected set; }
      public float lastTimeUpdated { get; protected set; }
      public float maxSuspensionTime { get; protected set; }
      public IInteractionBehaviour graspedObject { get; protected set; }
      public bool isUntracked { get; protected set; }
      public bool isUserGrasp { get; protected set; }

      public InteractionHand(Hand hand) {
        this.hand = hand;
        lastTimeUpdated = Time.unscaledTime;
        graspedObject = null;
      }

      public void UpdateHand(Hand hand) {
        this.hand = hand;
        lastTimeUpdated = Time.unscaledTime;
      }

      public void GraspObject(IInteractionBehaviour obj, bool isUserGrasp) {
        this.isUserGrasp = isUserGrasp;
        graspedObject = obj;
        graspedObject.NotifyHandGrasped(hand);
      }

      public void ReleaseObject() {
        graspedObject.NotifyHandReleased(hand);
        graspedObject = null;
        isUntracked = false;
        isUserGrasp = false;
      }

      public void MarkUntracked() {
        isUntracked = true;
        float outTime;
        graspedObject.NotifyHandLostTracking(hand, out outTime);
        maxSuspensionTime = outTime;
      }

      public void MarkTimeout() {
        graspedObject.NotifyHandTimeout(hand);
        graspedObject = null;
        isUntracked = true;
        isUserGrasp = false;
        hand = null;
      }

      public void RegainTracking(Hand newHand) {
        int oldId = hand.Id;
        UpdateHand(newHand);

        isUntracked = false;
        graspedObject.NotifyHandRegainedTracking(newHand, oldId);
      }
    }
    #endregion
  }
}
