/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {
  using Attributes;

  /// <summary>
  /// The LeapServiceProvider provides tracked Leap Hand data and images from the device
  /// via the Leap service running on the client machine.
  /// </summary>
  public class LeapServiceProvider : LeapProvider {

    #region Constants

    /// <summary>
    /// Converts nanoseconds to seconds.
    /// </summary>
    protected const double NS_TO_S = 1e-6;

    /// <summary>
    /// Converts seconds to nanoseconds.
    /// </summary>
    protected const double S_TO_NS = 1e6;

    /// <summary>
    /// The transform array used for late-latching.
    /// </summary>
    protected const string HAND_ARRAY_GLOBAL_NAME = "_LeapHandTransforms";

    /// <summary>
    /// The maximum number of times the provider will 
    /// attempt to reconnect to the service before giving up.
    /// </summary>
    protected const int MAX_RECONNECTION_ATTEMPTS = 5;

    /// <summary>
    /// The number of frames to wait between each
    /// reconnection attempt.
    /// </summary>
    protected const int RECONNECTION_INTERVAL = 180;

    #endregion

    #region Inspector

    public enum InteractionVolumeVisualization {
      None,
      LeapMotionController,
      StereoIR170,
      Automatic
    }
    [Tooltip("Displays a representation of the interaction volume in the scene view")]
    [SerializeField]
    protected InteractionVolumeVisualization _interactionVolumeVisualization = InteractionVolumeVisualization.LeapMotionController;

    public InteractionVolumeVisualization SelectedInteractionVolumeVisualization => _interactionVolumeVisualization;

    public enum FrameOptimizationMode {
      None,
      ReuseUpdateForPhysics,
      ReusePhysicsForUpdate,
    }
    [Tooltip("When enabled, the provider will only calculate one leap frame instead of two.")]
    [SerializeField]
    protected FrameOptimizationMode _frameOptimization = FrameOptimizationMode.None;

    public enum PhysicsExtrapolationMode {
      None,
      Auto,
      Manual
    }
    [Tooltip("The mode to use when extrapolating physics.\n" +
             " None - No extrapolation is used at all.\n" +
             " Auto - Extrapolation is chosen based on the fixed timestep.\n" +
             " Manual - Extrapolation time is chosen manually by the user.")]
    [SerializeField]
    protected PhysicsExtrapolationMode _physicsExtrapolation = PhysicsExtrapolationMode.Auto;

    [Tooltip("The amount of time (in seconds) to extrapolate the physics data by.")]
    [SerializeField]
    protected float _physicsExtrapolationTime = 1.0f / 90.0f;

    public enum TrackingOptimizationMode {
      Desktop,
      ScreenTop,
      HMD
    }
    [Tooltip("[Service must be >= 4.9.2!] " +
      "Which tracking mode to request that the service optimize for. " +
      "(Use the LeapXRServiceProvider for HMD Mode instead of this option!)")]
    [SerializeField]
    [EditTimeOnly]
    protected TrackingOptimizationMode _trackingOptimization = TrackingOptimizationMode.Desktop;

#if UNITY_2017_3_OR_NEWER
    [Tooltip("When checked, profiling data from the LeapCSharp worker thread will be used to populate the UnityProfiler.")]
    [EditTimeOnly]
#else
    [Tooltip("Worker thread profiling requires a Unity version of 2017.3 or greater.")]
    [Disable]
#endif
    [SerializeField]
    protected bool _workerThreadProfiling = false;

    [Tooltip("Which Leap Service API Endpoint to connect to.  This is configured on the service with the 'api_namespace' argument.")]
    [SerializeField]
    [EditTimeOnly]
    protected string _serverNameSpace = "Leap Service";

    #endregion

    #region Internal Settings & Memory
    protected bool _useInterpolation = true;

    // Extrapolate on Android to compensate for the latency introduced by its graphics
    // pipeline.
#if UNITY_ANDROID && !UNITY_EDITOR
    protected int ExtrapolationAmount = 15;
    protected int BounceAmount = 70;
#else
    protected int ExtrapolationAmount = 0;
    protected int BounceAmount = 0;
#endif

    protected Controller _leapController;
    protected bool _isDestroyed;

    protected SmoothedFloat _fixedOffset = new SmoothedFloat();
    protected SmoothedFloat _smoothedTrackingLatency = new SmoothedFloat();
    protected long _unityToLeapOffset;

    protected Frame _untransformedUpdateFrame;
    protected Frame _transformedUpdateFrame;
    protected Frame _untransformedFixedFrame;
    protected Frame _transformedFixedFrame;

    #endregion

    #region Edit-time Frame Data
 
    private Action<Device> _onDeviceSafe;
    /// <summary>
    /// A utility event to get a callback whenever a new device is connected to the service.
    /// This callback will ALSO trigger a callback upon subscription if a device is already
    /// connected.
    /// 
    /// For situations with multiple devices OnDeviceSafe will be dispatched once for each device.
    /// </summary>
    public event Action<Device> OnDeviceSafe {
      add {
        if (_leapController != null && _leapController.IsConnected) {
          foreach (var device in _leapController.Devices) {
            value(device);
          }
        }
        _onDeviceSafe += value;
      }
      remove {
        _onDeviceSafe -= value;
      }
    }

    #if UNITY_EDITOR
    private Frame _backingUntransformedEditTimeFrame = null;
    private Frame _untransformedEditTimeFrame {
      get {
        if (_backingUntransformedEditTimeFrame == null) {
          _backingUntransformedEditTimeFrame = new Frame();
        }
        return _backingUntransformedEditTimeFrame;
      }
    }
    private Frame _backingEditTimeFrame = null;
    private Frame _editTimeFrame {
      get {
        if (_backingEditTimeFrame == null) {
          _backingEditTimeFrame = new Frame();
        }
        return _backingEditTimeFrame;
      }
    }

    private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedLeftHands
      = new Dictionary<TestHandFactory.TestHandPose, Hand>();
    private Hand _editTimeLeftHand {
      get {
        Hand cachedHand = null;
        if (_cachedLeftHands.TryGetValue(editTimePose, out cachedHand)) {
          return cachedHand;
        }
        else {
          cachedHand = TestHandFactory.MakeTestHand(isLeft: true, pose: editTimePose);
          _cachedLeftHands[editTimePose] = cachedHand;
          return cachedHand;
        }
      }
    }

    private Dictionary<TestHandFactory.TestHandPose, Hand> _cachedRightHands
      = new Dictionary<TestHandFactory.TestHandPose, Hand>();
    private Hand _editTimeRightHand {
      get {
        Hand cachedHand = null;
        if (_cachedRightHands.TryGetValue(editTimePose, out cachedHand)) {
          return cachedHand;
        }
        else {
          cachedHand = TestHandFactory.MakeTestHand(isLeft: false, pose: editTimePose);
          _cachedRightHands[editTimePose] = cachedHand;
          return cachedHand;
        }
      }
    }

    #endif

    #endregion

    #region LeapProvider Implementation

    public override Frame CurrentFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          _editTimeFrame.Hands.Clear();
          _untransformedEditTimeFrame.Hands.Clear();
          _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
          _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
          transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
          return _editTimeFrame;
        }
        #endif
        if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
          return _transformedFixedFrame;
        } else {
          return _transformedUpdateFrame;
        }
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        #if UNITY_EDITOR
        if (!Application.isPlaying) {
          _editTimeFrame.Hands.Clear();
          _untransformedEditTimeFrame.Hands.Clear();
          _untransformedEditTimeFrame.Hands.Add(_editTimeLeftHand);
          _untransformedEditTimeFrame.Hands.Add(_editTimeRightHand);
          transformFrame(_untransformedEditTimeFrame, _editTimeFrame);
          return _editTimeFrame;
        }
        #endif
        if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics) {
          return _transformedUpdateFrame;
        } else {
          return _transformedFixedFrame;
        }
      }
    }

    #endregion

    #region Unity Events

    protected virtual void Reset() {
      editTimePose = TestHandFactory.TestHandPose.DesktopModeA;
    }

    protected virtual void Awake() {
      _fixedOffset.delay = 0.4f;
      _smoothedTrackingLatency.SetBlend(0.99f, 0.0111f);
    }

    protected virtual void Start() {
      createController();
      _transformedUpdateFrame = new Frame();
      _transformedFixedFrame = new Frame();
      _untransformedUpdateFrame = new Frame();
      _untransformedFixedFrame = new Frame();
    }

    protected virtual void Update() {
      if (_workerThreadProfiling) {
        LeapProfiling.Update();
      }

      if (!checkConnectionIntegrity()) { return; }

#if UNITY_EDITOR
      if (UnityEditor.EditorApplication.isCompiling) {
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.LogWarning("Unity hot reloading not currently supported. Stopping Editor Playback.");
        return;
      }
#endif

      _fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);

      if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate) {
        DispatchUpdateFrameEvent(_transformedFixedFrame);
        return;
      }

      if (_useInterpolation) {
#if !UNITY_ANDROID || UNITY_EDITOR
        _smoothedTrackingLatency.value = Mathf.Min(_smoothedTrackingLatency.value, 30000f);
        _smoothedTrackingLatency.Update((float)(_leapController.Now() - _leapController.FrameTimestamp()), Time.deltaTime);
#endif
        long timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
        _unityToLeapOffset = timestamp - (long)(Time.time * S_TO_NS);

        _leapController.GetInterpolatedFrameFromTime(_untransformedUpdateFrame, timestamp, CalculateInterpolationTime() - (BounceAmount * 1000));
      }
      else {
        _leapController.Frame(_untransformedUpdateFrame);
      }

      if (_untransformedUpdateFrame != null) {
        transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);

        DispatchUpdateFrameEvent(_transformedUpdateFrame);
      }
    }

    protected virtual void FixedUpdate() {
      if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics) {
        DispatchFixedFrameEvent(_transformedUpdateFrame);
        return;
      }

      if (_useInterpolation) {

        long timestamp;
        switch (_frameOptimization) {
          case FrameOptimizationMode.None:
            // By default we use Time.fixedTime to ensure that our hands are on the same
            // timeline as Update.  We add an extrapolation value to help compensate
            // for latency.
            float extrapolatedTime = Time.fixedTime + CalculatePhysicsExtrapolation();
            timestamp = (long)(extrapolatedTime * S_TO_NS) + _unityToLeapOffset;
            break;
          case FrameOptimizationMode.ReusePhysicsForUpdate:
            // If we are re-using physics frames for update, we don't even want to care
            // about Time.fixedTime, just grab the most recent interpolated timestamp
            // like we are in Update.
            timestamp = CalculateInterpolationTime() + (ExtrapolationAmount * 1000);
            break;
          default:
            throw new System.InvalidOperationException(
              "Unexpected frame optimization mode: " + _frameOptimization);
        }
        _leapController.GetInterpolatedFrame(_untransformedFixedFrame, timestamp);

      }
      else {
        _leapController.Frame(_untransformedFixedFrame);
      }

      if (_untransformedFixedFrame != null) {
        transformFrame(_untransformedFixedFrame, _transformedFixedFrame);

        DispatchFixedFrameEvent(_transformedFixedFrame);
      }
    }

    protected virtual void OnDestroy() {
      destroyController();
      _isDestroyed = true;
    }

    protected virtual void OnApplicationPause(bool isPaused) {
      if (_leapController != null) {
        if (isPaused) {
          _leapController.StopConnection();
        }
        else {
          _leapController.StartConnection();
        }
      }
    }

    protected virtual void OnApplicationQuit() {
      destroyController();
      _isDestroyed = true;
    }

    public float CalculatePhysicsExtrapolation() {
      switch (_physicsExtrapolation) {
        case PhysicsExtrapolationMode.None:
          return 0;
        case PhysicsExtrapolationMode.Auto:
          return Time.fixedDeltaTime;
        case PhysicsExtrapolationMode.Manual:
          return _physicsExtrapolationTime;
        default:
          throw new System.InvalidOperationException(
            "Unexpected physics extrapolation mode: " + _physicsExtrapolation);
      }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns the Leap Controller instance.
    /// </summary>
    public Controller GetLeapController() {
      #if UNITY_EDITOR
      // Null check to deal with hot reloading.
      if (!_isDestroyed && _leapController == null) {
        createController();
      }
      #endif
      return _leapController;
    }

    /// <summary>
    /// Returns true if the Leap Motion hardware is plugged in and this application is
    /// connected to the Leap Motion service.
    /// </summary>
    public bool IsConnected() {
      return GetLeapController().IsConnected;
    }

    /// <summary>
    /// Retransforms hand data from Leap space to the space of the Unity transform.
    /// This is only necessary if you're moving the LeapServiceProvider around in a
    /// custom script and trying to access Hand data from it directly afterward.
    /// </summary>
    public void RetransformFrames() {
      transformFrame(_untransformedUpdateFrame, _transformedUpdateFrame);
      transformFrame(_untransformedFixedFrame, _transformedFixedFrame);
    }

    /// <summary>
    /// Copies property settings from this LeapServiceProvider to the target
    /// LeapXRServiceProvider where applicable. Does not modify any XR-specific settings
    /// that only exist on the LeapXRServiceProvider.
    /// </summary>
    public void CopySettingsToLeapXRServiceProvider(
        LeapXRServiceProvider leapXRServiceProvider) {
      leapXRServiceProvider._interactionVolumeVisualization = _interactionVolumeVisualization;
      leapXRServiceProvider._frameOptimization = _frameOptimization;
      leapXRServiceProvider._physicsExtrapolation = _physicsExtrapolation;
      leapXRServiceProvider._physicsExtrapolationTime = _physicsExtrapolationTime;
      leapXRServiceProvider._workerThreadProfiling = _workerThreadProfiling;
    }

    #endregion

    #region Internal Methods

    protected virtual long CalculateInterpolationTime(bool endOfFrame = false) {
      #if UNITY_ANDROID && !UNITY_EDITOR
      return _leapController.Now() - 16000;
      #else
      if (_leapController != null) {
        return _leapController.Now() - (long)_smoothedTrackingLatency.value;
      } else {
        return 0;
      }
      #endif
    }
 
    /// <summary>
    /// Initializes Leap Motion policy flags.
    /// </summary>
    protected virtual void initializeFlags() {
      if (_leapController == null) {
        return;
      }

      if (_trackingOptimization == TrackingOptimizationMode.Desktop) {
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      } else if (_trackingOptimization == TrackingOptimizationMode.ScreenTop) {
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
        _leapController.SetPolicy  (Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
      } else if (_trackingOptimization == TrackingOptimizationMode.HMD) {
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
        _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
        _leapController.SetPolicy  (Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      }
    }

    /// <summary>
    /// Creates an instance of a Controller, initializing its policy flags and
    /// subscribing to its connection event.
    /// </summary>
    protected void createController() {
      if (_leapController != null) {
        return;
      }

      _leapController = new Controller(0, _serverNameSpace);
      _leapController.Device += (s, e) => {
        if (_onDeviceSafe != null) {
          _onDeviceSafe(e.Device);
        }
      };

      if (_leapController.IsConnected) {
        initializeFlags();
      } else {
        _leapController.Device += onHandControllerConnect;
      }

      if (_workerThreadProfiling) {
        //A controller will report profiling statistics for the duration of it's lifetime
        //so these events will never be unsubscribed from.
        _leapController.EndProfilingBlock += LeapProfiling.EndProfilingBlock;
        _leapController.BeginProfilingBlock += LeapProfiling.BeginProfilingBlock;

        _leapController.EndProfilingForThread += LeapProfiling.EndProfilingForThread;
        _leapController.BeginProfilingForThread += LeapProfiling.BeginProfilingForThread;
      }
    }
 
    /// <summary>
    /// Stops the connection for the existing instance of a Controller, clearing old
    /// policy flags and resetting the Controller to null.
    /// </summary>
    protected void destroyController() {
      if (_leapController != null) {
        if (_leapController.IsConnected) {
          _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
          _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
        }
        _leapController.StopConnection();
        _leapController.Dispose();
        _leapController = null;
      }
    }

    private int _framesSinceServiceConnectionChecked = 0;
    private int _numberOfReconnectionAttempts = 0;
    /// <summary>
    /// Checks whether this provider is connected to a service;
    /// If it is not, attempt to reconnect at regular intervals
    /// for MAX_RECONNECTION_ATTEMPTS
    /// </summary>
    protected bool checkConnectionIntegrity() {
      if (_leapController.IsServiceConnected) {
        _framesSinceServiceConnectionChecked = 0;
        _numberOfReconnectionAttempts = 0;
        return true;
      } else if (_numberOfReconnectionAttempts < MAX_RECONNECTION_ATTEMPTS) {
        _framesSinceServiceConnectionChecked++;

        if (_framesSinceServiceConnectionChecked > RECONNECTION_INTERVAL) {
          _framesSinceServiceConnectionChecked = 0;
          _numberOfReconnectionAttempts++;

          Debug.LogWarning("Leap Service not connected; attempting to reconnect for try " +
                           _numberOfReconnectionAttempts + "/" + MAX_RECONNECTION_ATTEMPTS +
                           "...", this);
          using (new ProfilerSample("Reconnection Attempt")) {
            destroyController();
            createController();
          }
        }
      }
      return false;
    }

    protected void onHandControllerConnect(object sender, LeapEventArgs args) {
      initializeFlags();

      if (_leapController != null) {
        _leapController.Device -= onHandControllerConnect;
      }
    }

    protected virtual void transformFrame(Frame source, Frame dest) {
      dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
    }

    #endregion

  }

}
