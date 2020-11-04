/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;
  using System.Threading;

  using LeapInternal;

  /// <summary>
  /// The Controller class is your main interface to the Leap Motion Controller.
  /// 
  /// Create an instance of this Controller class to access frames of tracking
  /// data and configuration information.Frame data can be polled at any time
  /// using the Controller.Frame() function.Call frame() or frame(0) to get the
  /// most recent frame.Set the history parameter to a positive integer to access
  /// previous frames.A controller stores up to 60 frames in its frame history.
  /// 
  /// 
  /// Polling is an appropriate strategy for applications which already have an
  /// intrinsic update loop, such as a game. You can also subscribe to the FrameReady
  /// event to get tracking frames through an event delegate.
  /// 
  /// If the current thread implements a SynchronizationContext that contains a message
  /// loop, events are posted to that threads message loop. Otherwise, events are called
  /// on an independent thread and applications must perform any needed synchronization
  /// or marshalling of data between threads. Note that Unity3D does not create an
  /// appropriate SynchronizationContext object. Typically, event handlers cannot access
  /// any Unity objects.
  /// 
  /// @since 1.0
  /// </summary>
  public class Controller :
    IController {
    Connection _connection;
    bool _disposed = false;
    Config _config;

    /// <summary>
    /// The SynchronizationContext used for dispatching events.
    /// 
    /// By default the synchronization context of the thread creating the controller
    /// instance is used. You can change the context if desired.
    /// </summary>
    public SynchronizationContext EventContext {
      get {
        return _connection.EventContext;
      }
      set {
        _connection.EventContext = value;
      }
    }

    /// <summary>
    /// Dispatched when the connection is initialized (but not necessarily connected).
    /// 
    /// Can be dispatched more than once, if connection is restarted.
    /// @since 3.0
    /// </summary>
    public event EventHandler<LeapEventArgs> Init {
      add {
        if (_hasInitialized)
          value(this, new LeapEventArgs(LeapEvent.EVENT_INIT));
        _init += value;
      }
      remove { _init -= value; }
    }

    private bool _hasInitialized = false;
    private EventHandler<LeapEventArgs> _init;

    /// <summary>
    /// Dispatched when the connection to the service is established.
    /// @since 3.0
    /// </summary>
    public event EventHandler<ConnectionEventArgs> Connect {
      add {
        if (_hasConnected)
          value(this, new ConnectionEventArgs());
        _connect += value;
      }
      remove { _connect -= value; }
    }

    private bool _hasConnected = false;
    private EventHandler<ConnectionEventArgs> _connect;

    /// <summary>
    /// Dispatched if the connection to the service is lost.
    /// @since 3.0
    /// </summary>
    public event EventHandler<ConnectionLostEventArgs> Disconnect {
      add {
        _connection.LeapConnectionLost += value;
      }
      remove {
        _connection.LeapConnectionLost -= value;
      }
    }

    /// <summary>
    /// Dispatched when a tracking frame is ready.
    /// @since 3.0
    /// </summary>
    public event EventHandler<FrameEventArgs> FrameReady {
      add {
        _connection.LeapFrame += value;
      }
      remove {
        _connection.LeapFrame -= value;
      }
    }

    /// <summary>
    /// Dispatched when an internal tracking frame is ready.
    /// @since 3.0
    /// </summary>
    public event EventHandler<InternalFrameEventArgs> InternalFrameReady {
      add {
        _connection.LeapInternalFrame += value;
      }
      remove {
        _connection.LeapInternalFrame -= value;
      }
    }

    /// <summary>
    /// Dispatched when a Leap Motion device is connected.
    /// @since 3.0
    /// </summary>
    public event EventHandler<DeviceEventArgs> Device {
      add {
        _connection.LeapDevice += value;
      }
      remove {
        _connection.LeapDevice -= value;
      }
    }

    /// <summary>
    /// Dispatched when a Leap Motion device is disconnected.
    /// @since 3.0
    /// </summary>
    public event EventHandler<DeviceEventArgs> DeviceLost {
      add {
        _connection.LeapDeviceLost += value;
      }
      remove {
        _connection.LeapDeviceLost -= value;
      }
    }

    /// <summary>
    /// Dispatched when a Leap device fails to initialize.
    /// @since 3.0
    /// </summary>
    public event EventHandler<DeviceFailureEventArgs> DeviceFailure {
      add {
        _connection.LeapDeviceFailure += value;
      }
      remove {
        _connection.LeapDeviceFailure -= value;
      }
    }

    /// <summary>
    /// Dispatched when the system generates a loggable event.
    /// @since 3.0
    /// </summary>
    public event EventHandler<LogEventArgs> LogMessage {
      add {
        _connection.LeapLogEvent += value;
      }
      remove {
        _connection.LeapLogEvent -= value;
      }
    }

    /// <summary>
    /// Dispatched when a policy changes.
    /// @since 3.0
    /// </summary>
    public event EventHandler<PolicyEventArgs> PolicyChange {
      add {
        _connection.LeapPolicyChange += value;
      }
      remove {
        _connection.LeapPolicyChange -= value;
      }
    }

    /// <summary>
    /// Dispatched when a configuration setting changes.
    /// @since 3.0
    /// </summary>
    public event EventHandler<ConfigChangeEventArgs> ConfigChange {
      add {
        _connection.LeapConfigChange += value;
      }
      remove {
        _connection.LeapConfigChange -= value;
      }
    }

    /// <summary>
    /// Dispatched when the image distortion map changes.
    /// The distortion map can change when the Leap device switches orientation,
    /// or a new device becomes active.
    /// @since 3.0
    /// </summary>
    public event EventHandler<DistortionEventArgs> DistortionChange {
      add {
        _connection.LeapDistortionChange += value;
      }
      remove {
        _connection.LeapDistortionChange -= value;
      }
    }

    /// <summary>
    /// Dispatched when the service drops a tracking frame.
    /// </summary>
    public event EventHandler<DroppedFrameEventArgs> DroppedFrame {
      add {
        _connection.LeapDroppedFrame += value;
      }
      remove {
        _connection.LeapDroppedFrame -= value;
      }
    }

    /// <summary>
    /// Dispatched when an unrequested image is ready.
    /// @since 4.0
    /// </summary>
    public event EventHandler<ImageEventArgs> ImageReady {
      add {
        _connection.LeapImage += value;
      }
      remove {
        _connection.LeapImage -= value;
      }
    }

    /// <summary>
    /// Dispatched whenever a thread wants to start profiling for a custom thread.
    /// The event is always dispatched from the thread itself.
    /// 
    /// The event data will contain the name of the thread, as well as an array of
    /// all possible profiling blocks that could be entered on that thread.
    /// 
    /// @since 4.0
    /// </summary>
    public event Action<BeginProfilingForThreadArgs> BeginProfilingForThread {
      add {
        _connection.LeapBeginProfilingForThread += value;
      }
      remove {
        _connection.LeapBeginProfilingForThread -= value;
      }
    }

    /// <summary>
    /// Dispatched whenever a thread is finished profiling.  The event is always
    /// dispatched from the thread itself.
    /// 
    /// @since 4.0
    /// </summary>
    public event Action<EndProfilingForThreadArgs> EndProfilingForThread {
      add {
        _connection.LeapEndProfilingForThread += value;
      }
      remove {
        _connection.LeapEndProfilingForThread -= value;
      }
    }

    /// <summary>
    /// Dispatched whenever a thread enters a profiling block.  The event is always
    /// dispatched from the thread itself.
    /// 
    /// The event data will contain the name of the profiling block.
    /// 
    /// @since 4.0
    /// </summary>
    public event Action<BeginProfilingBlockArgs> BeginProfilingBlock {
      add {
        _connection.LeapBeginProfilingBlock += value;
      }
      remove {
        _connection.LeapBeginProfilingBlock -= value;
      }
    }

    /// <summary>
    /// Dispatched whenever a thread ends a profiling block.  The event is always
    /// dispatched from the thread itself.
    /// 
    /// The event data will contain the name of the profiling block.
    /// 
    /// @since 4.0
    /// </summary>
    public event Action<EndProfilingBlockArgs> EndProfilingBlock {
      add {
        _connection.LeapEndProfilingBlock += value;
      }
      remove {
        _connection.LeapEndProfilingBlock -= value;
      }
    }

    /// <summary>
    /// Dispatched when point mapping change events are generated by the service.
    /// @since 4.0
    /// </summary>
    public event EventHandler<PointMappingChangeEventArgs> PointMappingChange {
      add {
        _connection.LeapPointMappingChange += value;
      }
      remove {
        _connection.LeapPointMappingChange -= value;
      }
    }

    /// <summary>
    /// Dispatched when a new HeadPose is available.
    /// </summary>
    public event EventHandler<HeadPoseEventArgs> HeadPoseChange {
      add {
        _connection.LeapHeadPoseChange += value;
      }
      remove {
        _connection.LeapHeadPoseChange -= value;
      }
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing) {
      if (_disposed) {
        return;
      }

      _disposed = true;
    }

    /// <summary>
    /// Constructs a Controller object.
    /// 
    /// The default constructor uses a connection key of 0.
    /// 
    /// @since 1.0
    /// </summary>
    public Controller() : this(0) { }

    /// <summary>
    /// Constructs a Controller object using the specified connection key.
    /// 
    /// All controller instances using the same key will use the same connection
    /// to the service. In general, an application should not use more than one connection
    /// for all its controllers. Each connection keeps its own cache of frames and images.
    /// 
    /// @param connectionKey An identifier specifying the connection to use. If a
    /// connection with the specified key already exists, that connection is used.
    /// Otherwise, a new connection is created.
    /// @since 3.0
    /// </summary>
    public Controller(int connectionKey) {
      _connection = Connection.GetConnection(connectionKey);
      _connection.EventContext = SynchronizationContext.Current;

      _connection.LeapInit += OnInit;
      _connection.LeapConnection += OnConnect;
      _connection.LeapConnectionLost += OnDisconnect;

      _connection.Start();
    }

    /// <summary>
    /// Starts the connection.
    /// 
    /// A connection starts automatically when created, but you can
    /// use this function to restart the connection after stopping it.
    /// 
    /// @since 3.0
    /// </summary>
    public void StartConnection() {
      _connection.Start();
    }

    /// <summary>
    /// Stops the connection.
    /// 
    /// No more frames or other events are received from a stopped connection. You can
    /// restart with StartConnection().
    /// 
    /// @since 3.0
    /// </summary>
    public void StopConnection() {
      _connection.Stop();
    }

    /// <summary>
    /// Reports whether your application has a connection to the Leap Motion
    /// daemon/service. Can be true even if the Leap Motion hardware is not available. 
    /// @since 1.2 
    /// </summary>
    public bool IsServiceConnected {
      get {
        return _connection.IsServiceConnected;
      }
    }

    /// <summary>
    /// Requests setting a policy.
    ///  
    /// A request to change a policy is subject to user approval and a policy 
    /// can be changed by the user at any time (using the Leap Motion settings dialog). 
    /// The desired policy flags must be set every time an application runs. 
    ///  
    /// Policy changes are completed asynchronously and, because they are subject 
    /// to user approval or system compatibility checks, may not complete successfully. Call 
    /// Controller.IsPolicySet() after a suitable interval to test whether 
    /// the change was accepted. 
    /// @since 2.1.6 
    /// </summary>
    public void SetPolicy(PolicyFlag policy) {
      _connection.SetPolicy(policy);
    }

    /// <summary>
    /// Requests clearing a policy.
    /// 
    /// Policy changes are completed asynchronously and, because they are subject
    /// to user approval or system compatibility checks, may not complete successfully. Call
    /// Controller.IsPolicySet() after a suitable interval to test whether
    /// the change was accepted.
    /// @since 2.1.6
    /// </summary>
    public void ClearPolicy(PolicyFlag policy) {
      _connection.ClearPolicy(policy);
    }

    /// <summary>
    /// Gets the active setting for a specific policy.
    /// 
    /// Keep in mind that setting a policy flag is asynchronous, so changes are
    /// not effective immediately after calling setPolicyFlag(). In addition, a
    /// policy request can be declined by the user. You should always set the
    /// policy flags required by your application at startup and check that the
    /// policy change request was successful after an appropriate interval.
    /// 
    /// If the controller object is not connected to the Leap Motion software, then the default
    /// state for the selected policy is returned.
    ///
    /// @since 2.1.6
    /// </summary>
    public bool IsPolicySet(PolicyFlag policy) {
      return _connection.IsPolicySet(policy);
    }

    /// <summary>
    /// In most cases you should get Frame objects using the LeapProvider.CurrentFrame
    /// property. The data in Frame objects taken directly from a Leap.Controller instance
    /// is still in the Leap Motion frame of reference and will not match the hands
    /// displayed in a Unity scene.
    /// 
    /// Returns a frame of tracking data from the Leap Motion software. Use the optional
    /// history parameter to specify which frame to retrieve. Call frame() or
    /// frame(0) to access the most recent frame; call frame(1) to access the
    /// previous frame, and so on. If you use a history value greater than the
    /// number of stored frames, then the controller returns an empty frame.
    /// 
    /// @param history The age of the frame to return, counting backwards from
    /// the most recent frame (0) into the past and up to the maximum age (59).
    /// @returns The specified frame; or, if no history parameter is specified,
    /// the newest frame. If a frame is not available at the specified history
    /// position, an invalid Frame is returned.
    /// @since 1.0
    /// </summary>
    public Frame Frame(int history = 0) {
      Frame frame = new Frame();
      Frame(frame, history);
      return frame;
    }

    /// <summary>
    /// Identical to Frame(history) but instead of constructing a new frame and returning
    /// it, the user provides a frame object to be filled with data instead.
    /// </summary>
    public void Frame(Frame toFill, int history = 0) {
      LEAP_TRACKING_EVENT trackingEvent;
      _connection.Frames.Get(out trackingEvent, history);
      toFill.CopyFrom(ref trackingEvent);
    }

    /// <summary>
    /// Returns the timestamp of a recent tracking frame.  Use the
    /// optional history parameter to specify how many frames in the past
    /// to retrieve the timestamp.  Leave the history parameter as
    /// it's default value to return the timestamp of the most recent
    /// tracked frame.
    /// </summary>
    public long FrameTimestamp(int history = 0) {
      LEAP_TRACKING_EVENT trackingEvent;
      _connection.Frames.Get(out trackingEvent, history);
      return trackingEvent.info.timestamp;
    }

    /// <summary>
    /// Returns the frame object with all hands transformed by the specified
    /// transform matrix.
    /// </summary>
    public Frame GetTransformedFrame(LeapTransform trs, int history = 0) {
      return new Frame().CopyFrom(Frame(history)).Transform(trs);
    }

    /// <summary>
    /// Returns the Frame at the specified time, interpolating the data between existing frames, if necessary.
    /// </summary>
    public Frame GetInterpolatedFrame(Int64 time) {
      return _connection.GetInterpolatedFrame(time);
    }

    /// <summary>
    /// Fills the Frame with data taken at the specified time, interpolating the data between existing frames, if necessary.
    /// </summary>
    public void GetInterpolatedFrame(Frame toFill, Int64 time) {
      _connection.GetInterpolatedFrame(toFill, time);
    }

    /// <summary>
    /// Returns the Head pose at the specified time, interpolating the data between existing frames, if necessary.
    /// </summary>
    public LEAP_HEAD_POSE_EVENT GetInterpolatedHeadPose(Int64 time) {
      return _connection.GetInterpolatedHeadPose(time);
    }

    public void GetInterpolatedHeadPose(ref LEAP_HEAD_POSE_EVENT toFill, Int64 time) {
      _connection.GetInterpolatedHeadPose(ref toFill, time);
    }

    public void TelemetryProfiling(ref LEAP_TELEMETRY_DATA telemetryData) {
      _connection.TelemetryProfiling(ref telemetryData);
    }

    public UInt64 TelemetryGetNow() {
      return LeapC.TelemetryGetNow();
    }

    public void GetPointMapping(ref PointMapping pointMapping) {
      _connection.GetPointMapping(ref pointMapping);
    }

    /// <summary>
    /// This is a special variant of GetInterpolatedFrameFromTime, for use with special
    /// features that only require the position and orientation of the palm positions, and do
    /// not care about pose data or any other data.
    /// 
    /// You must specify the id of the hand that you wish to get a transform for.  If you specify
    /// an id that is not present in the interpolated frame, the output transform will be the
    /// identity transform.
    /// </summary>
    public void GetInterpolatedLeftRightTransform(Int64 time,
                                                  Int64 sourceTime,
                                                  int leftId,
                                                  int rightId,
                                              out LeapTransform leftTransform,
                                              out LeapTransform rightTransform) {
      _connection.GetInterpolatedLeftRightTransform(time, sourceTime, leftId, rightId, out leftTransform, out rightTransform);
    }

    public void GetInterpolatedFrameFromTime(Frame toFill, Int64 time, Int64 sourceTime) {
      _connection.GetInterpolatedFrameFromTime(toFill, time, sourceTime);
    }

    /// <summary>
    /// Returns a timestamp value as close as possible to the current time.
    /// Values are in microseconds, as with all the other timestamp values.
    /// 
    /// @since 2.2.7
    /// </summary>
    public long Now() {
      return LeapC.GetNow();
    }

    /// <summary>
    /// Reports whether this Controller is connected to the Leap Motion service and
    /// the Leap Motion hardware is plugged in.
    /// 
    /// When you first create a Controller object, isConnected() returns false.
    /// After the controller finishes initializing and connects to the Leap Motion
    /// software and if the Leap Motion hardware is plugged in, isConnected() returns true.
    /// 
    /// You can either handle the onConnect event using a Listener instance or
    /// poll the isConnected() function if you need to wait for your
    /// application to be connected to the Leap Motion software before performing some other
    /// operation.
    /// 
    /// @since 1.0
    /// </summary>
    public bool IsConnected {
      get {
        return IsServiceConnected && Devices.Count > 0;
      }
    }

    /// <summary>
    /// Returns a Config object, which you can use to query the Leap Motion system for
    /// configuration information.
    /// 
    /// @since 1.0
    /// </summary>
    public Config Config {
      get {
        if (_config == null)
          _config = new Config(this._connection.ConnectionKey);
        return _config;
      }
    }

    /// <summary>
    /// The list of currently attached and recognized Leap Motion controller devices.
    /// 
    /// The Device objects in the list describe information such as the range and
    /// tracking volume.
    /// 
    /// Currently, the Leap Motion Controller only allows a single active device at a time,
    /// however there may be multiple devices physically attached and listed here.  Any active
    /// device(s) are guaranteed to be listed first, however order is not determined beyond that.
    /// 
    /// @since 1.0
    /// </summary>
    public DeviceList Devices {
      get {
        return _connection.Devices;
      }
    }

    /// <summary>
    /// A list of any Leap Motion hardware devices that are physically connected to
    /// the client computer, but are not functioning correctly. The list contains
    /// FailedDevice objects containing the pnpID and the reason for failure. No
    /// other device information is available.
    /// 
    /// @since 3.0
    /// </summary>
    public FailedDeviceList FailedDevices() {
      return _connection.FailedDevices;
    }

    /// <summary>
    /// The supported controller policies.
    /// 
    /// The supported policy flags are:
    /// 
    /// **POLICY_BACKGROUND_FRAMES** -- requests that your application receives frames
    ///   when it is not the foreground application for user input.
    /// 
    ///   The background frames policy determines whether an application
    ///   receives frames of tracking data while in the background. By
    ///   default, the Leap Motion  software only sends tracking data to the foreground application.
    ///   Only applications that need this ability should request the background
    ///   frames policy. The "Allow Background Apps" checkbox must be enabled in the
    ///   Leap Motion Control Panel or this policy will be denied.
    /// 
    /// **POLICY_OPTIMIZE_HMD** -- request that the tracking be optimized for head-mounted
    ///   tracking.
    /// 
    ///   The optimize HMD policy improves tracking in situations where the Leap
    ///   Motion hardware is attached to a head-mounted display. This policy is
    ///   not granted for devices that cannot be mounted to an HMD, such as
    ///   Leap Motion controllers embedded in a laptop or keyboard.
    /// 
    /// Some policies can be denied if the user has disabled the feature on
    /// their Leap Motion control panel.
    /// 
    /// @since 1.0
    /// </summary>
    public enum PolicyFlag {
      /// <summary>
      /// The default policy.
      /// </summary>
      POLICY_DEFAULT = 0,
      /// <summary>
      /// Receive background frames.
      /// </summary>
      POLICY_BACKGROUND_FRAMES = (1 << 0),
      /// <summary>
      /// Allow streaming images.
      /// </summary>
      POLICY_IMAGES = (1 << 1),
      /// <summary>
      /// Optimize the tracking for head-mounted device.
      /// </summary>
      POLICY_OPTIMIZE_HMD = (1 << 2),
      /// <summary>
      /// Allow pausing and unpausing of the Leap Motion service.
      /// </summary>
      POLICY_ALLOW_PAUSE_RESUME = (1 << 3),
      /// <summary>
      /// Allow streaming map point
      /// </summary>
      POLICY_MAP_POINTS = (1 << 7),
      /// <summary>
      /// Optimize the tracking for screen-top device.
      /// @since 5.0.0
      /// </summary>
      POLICY_OPTIMIZE_SCREENTOP = (1 << 8),
    }

    protected virtual void OnInit(object sender, LeapEventArgs eventArgs) {
      _hasInitialized = true;
    }

    protected virtual void OnConnect(object sender, ConnectionEventArgs eventArgs) {
      _hasConnected = true;
    }

    protected virtual void OnDisconnect(object sender, ConnectionLostEventArgs eventArgs) {
      _hasInitialized = false;
      _hasConnected = false;
    }
  }
}
