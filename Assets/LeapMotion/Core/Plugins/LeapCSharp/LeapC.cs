/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/


namespace LeapInternal {
  using System;
  using System.Runtime.InteropServices;

  public enum eLeapConnectionStatus : uint {
    /// <summary>
    /// A connection has been established.
    /// </summary>
    eLeapConnectionStatus_NotConnected = 0,
    /// <summary>
    /// The connection has not been completed. Call OpenConnection.
    /// </summary>
    eLeapConnectionStatus_Connected,
    /// <summary>
    /// The connection handshake has not completed.
    /// </summary>
    eLeapConnectionStatus_HandshakeIncomplete,
    /// <summary>
    /// A connection could not be established because the server does not appear to be running.
    /// </summary>
    eLeapConnectionStatus_NotRunning = 0xE7030004
  };

  public enum eLeapDeviceCaps : uint {
    /// <summary>
    /// The device can send color images.
    /// </summary>
    eLeapDeviceCaps_Color = 0x00000001,
  };

  public enum eLeapDeviceType : uint {
    /// <summary>
    /// The Leap Motion consumer peripheral
    /// </summary>
    eLeapDeviceType_Peripheral = 0x0003,
    /// <summary>
    /// Internal research product codename "Dragonfly".
    /// </summary>
    eLeapDeviceType_Dragonfly = 0x1102,
    /// <summary>
    /// Internal research product codename "Nightcrawler".
    /// </summary>
    eLeapDeviceType_Nightcrawler = 0x1201,
    /// <summary>
    /// Research product codename "Rigel".
    /// </summary>
    eLeapDevicePID_Rigel = 0x1202
  };

  public enum eLeapServiceDisposition : uint {
    /// <summary>
    /// The service cannot receive frames fast enough from the underlying hardware.
    /// @since 3.1.3
    /// </summary>
    eLeapServiceState_LowFpsDetected = 0x00000001,

    /// <summary>
    /// The service has paused itself due to an insufficient frame rate from the hardware.
    /// @since 3.1.3
    /// </summary>
    eLeapServiceState_PoorPerformancePause = 0x00000002,
  };

  public enum eDistortionMatrixType {
    /// <summary>
    /// A 64x64 matrix of pairs of points.
    /// </summary>
    eDistortionMatrixType_64x64
  };

  public enum eLeapPolicyFlag : uint {
    /// <summary>
    /// Allows frame receipt even when this application is not the foreground application.
    /// </summary>
    eLeapPolicyFlag_BackgroundFrames = 0x00000001,
    /// <summary>
    /// Allow streaming images
    /// </summary>
    eLeapPolicyFlag_Images = 0x00000002,
    /// <summary>
    /// Optimize HMD Policy Flag.
    /// </summary>
    eLeapPolicyFlag_OptimizeHMD = 0x00000004,
    /// <summary>
    /// Modifies the security token to allow calls to LeapPauseDevice to succeed
    /// </summary>
    eLeapPolicyFlag_AllowPauseResume = 0x00000008,
    /// <summary>
    /// Allows streaming map points.
    /// </summary>
    eLeapPolicyFlag_MapPoints = 0x00000080,
    /// <summary>
    /// The policy specifying whether to optimize tracking for screen-top device.
    /// @since 5.0.0
    /// </summary>
    eLeapPolicyFlag_ScreenTop = 0x00000100,
  };

  public enum eLeapDeviceStatus : uint {
    /// <summary>
    /// Presently sending frames to all clients that have requested them.
    /// </summary>
    eLeapDeviceStatus_Streaming = 0x00000001,
    /// <summary>
    /// Device streaming has been paused.
    /// </summary>
    eLeapDeviceStatus_Paused = 0x00000002,
    /// <summary>
    /// There are known sources of infrared interference. Device has transitioned to
    /// robust mode in order to compensate.
    /// </summary>
    eLeapDeviceStatus_Robust = 0x00000004,
    /// <summary>
    /// The device's window is smudged, tracking may be degraded.
    /// </summary>
    eLeapDeviceStatus_Smudged = 0x00000008,
    /// <summary>
    /// The device has entered low-resource mode.
    /// </summary>
    eLeapDeviceStatus_LowResource = 0x00000010,
    /// <summary>
    /// The device has failed, but the failure reason is not known.
    /// </summary>
    eLeapDeviceStatus_UnknownFailure = 0xE8010000,
    /// <summary>
    /// Bad calibration, cannot send frames.
    /// </summary>
    eLeapDeviceStatus_BadCalibration = 0xE8010001,
    /// <summary>
    /// Corrupt firmware and/or cannot receive a required firmware update.
    /// </summary>
    eLeapDeviceStatus_BadFirmware = 0xE8010002,
    /// <summary>
    /// Exhibiting USB communications issues.
    /// </summary>
    eLeapDeviceStatus_BadTransport = 0xE8010003,
    /// <summary>
    /// Missing critical control interfaces needed for communication.
    /// </summary>
    eLeapDeviceStatus_BadControl = 0xE8010004,
  };

  public enum eLeapImageType {
    eLeapImageType_Unknown = 0,
    /// <summary>
    /// Default processed IR image
    /// </summary>
    eLeapImageType_Default,
    /// <summary>
    /// Image from raw sensor values
    /// </summary>
    eLeapImageType_Raw
  };

  public enum eLeapImageFormat : uint {
    /// <summary>
    /// An invalid or unknown format.
    /// </summary>
    eLeapImageFormat_UNKNOWN = 0,
    /// <summary>
    /// An infrared image.
    /// </summary>
    eLeapImageType_IR = 0x317249,
    /// <summary>
    /// A Bayer RGBIr image with uncorrected RGB channels
    /// </summary>
    eLeapImageType_RGBIr_Bayer = 0x49425247,
  };

  public enum eLeapPerspectiveType {
    /// <summary>
    /// An unknown or invalid type.
    /// </summary>
    eLeapPerspectiveType_invalid = 0,
    /// <summary>
    /// A canonically left image.
    /// </summary>
    eLeapPerspectiveType_stereo_left = 1,
    /// <summary>
    /// A canonically right image.
    /// </summary>
    eLeapPerspectiveType_stereo_right = 2,
    /// <summary>
    /// Reserved for future use.
    /// </summary>
    eLeapPerspectiveType_mono = 3,
  };

  public enum eLeapHandType {
    eLeapHandType_Left,
    eLeapHandType_Right
  };

  public enum eLeapLogSeverity {
    /// <summary>
    /// The message severity is not known or was not specified.
    /// </summary>
    eLeapLogSeverity_Unknown = 0,
    /// <summary>
    /// A message about a fault that could render the software or device non-functional.
    /// </summary>
    eLeapLogSeverity_Critical,
    /// <summary>
    /// A message warning about a condition that could degrade device capabilities.
    /// </summary>
    eLeapLogSeverity_Warning,
    /// <summary>
    /// A system status message.
    /// </summary>
    eLeapLogSeverity_Information
  };

  public enum eLeapValueType : int {
    /// <summary>
    /// The type is unknown (which is an abnormal condition).
    /// </summary>
    eLeapValueType_Unknown,
    eLeapValueType_Boolean,
    eLeapValueType_Int32,
    eLeapValueType_Float,
    eLeapValueType_String
  };

  public enum eLeapAllocatorType : uint {
    eLeapAllocatorType_Int8 = 0,
    eLeapAllocatorType_Uint8 = 1,
    eLeapAllocatorType_Int16 = 2,
    eLeapAllocatorType_UInt16 = 3,
    eLeapAllocatorType_Int32 = 4,
    eLeapAllocatorType_UInt32 = 5,
    eLeapAllocatorType_Float = 6,
    eLeapAllocatorType_Int64 = 8,
    eLeapAllocatorType_UInt64 = 9,
    eLeapAllocatorType_Double = 10,
  };

  public enum eLeapRS : uint {
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    eLeapRS_Success = 0x00000000,
    /// <summary>
    /// An undetermined error has occurred.
    /// This is usually the result of an abnormal operating condition in LeapC,
    /// the Leap Motion service, or the host computer itself.
    /// </summary>
    eLeapRS_UnknownError = 0xE2010000,
    /// <summary>
    /// An invalid argument was specified.
    /// </summary>
    eLeapRS_InvalidArgument = 0xE2010001,
    /// <summary>
    /// Insufficient resources existed to complete the request.
    /// </summary>
    eLeapRS_InsufficientResources = 0xE2010002,
    /// <summary>
    /// The specified buffer was not large enough to complete the request.
    /// </summary>
    eLeapRS_InsufficientBuffer = 0xE2010003,
    /// <summary>
    /// The requested operation has timed out.
    /// </summary>
    eLeapRS_Timeout = 0xE2010004,
    /// <summary>
    /// The operation is invalid because there is no current connection.
    /// </summary>
    eLeapRS_NotConnected = 0xE2010005,
    /// <summary>
    /// The operation is invalid because the connection is not complete.
    /// </summary>
    eLeapRS_HandshakeIncomplete = 0xE2010006,
    /// <summary>
    /// The specified buffer size is too large.
    /// </summary>
    eLeapRS_BufferSizeOverflow = 0xE2010007,
    /// <summary>
    /// A communications protocol error occurred.
    /// </summary>
    eLeapRS_ProtocolError = 0xE2010008,
    /// <summary>
    /// The server incorrectly specified zero as a client ID.
    /// </summary>
    eLeapRS_InvalidClientID = 0xE2010009,
    /// <summary>
    /// The connection to the service was unexpectedly closed while reading or writing a message.
    /// The server may have terminated.
    /// </summary>
    eLeapRS_UnexpectedClosed = 0xE201000A,
    /// <summary>
    /// The specified request token does not appear to be valid
    /// 
    /// Provided that the token value which identifies the request itself was, at one point, valid, this
    /// error condition occurs when the request to which the token refers has already been satisfied or
    /// is currently being satisfied.
    /// </summary>
    eLeapRS_UnknownImageFrameRequest = 0xE201000B,
    /// <summary>
    /// The specified frame ID is not valid or is no longer valid
    /// 
    /// Provided that frame ID was, at one point, valid, this error condition occurs when the identifier
    /// refers to a frame that occurred further in the past than is currently recorded in the rolling
    /// frame window.
    /// </summary>
    eLeapRS_UnknownTrackingFrameID = 0xE201000C,
    /// <summary>
    /// The specified timestamp references a future point in time
    /// 
    /// The related routine can only operate on time points having occurred in the past, and the
    /// provided timestamp occurs in the future.
    /// </summary>
    eLeapRS_RoutineIsNotSeer = 0xE201000D,
    /// <summary>
    /// The specified timestamp references a point too far in the past
    /// 
    /// The related routine can only operate on time points occurring within its immediate record of
    /// the past.
    /// </summary>
    eLeapRS_TimestampTooEarly = 0xE201000E,
    /// <summary>
    /// LeapPollConnection is called concurrently.
    /// </summary>
    eLeapRS_ConcurrentPoll = 0xE201000F,
    /// <summary>
    /// A connection to the Leap Motion service could not be established.
    /// </summary>
    eLeapRS_NotAvailable = 0xE7010002,
    /// <summary>
    /// The requested operation can only be performed while the device is sending data.
    /// </summary>
    eLeapRS_NotStreaming = 0xE7010004,
    /// <summary>
    /// The specified device could not be opened. It is possible that the device identifier
    /// is invalid, or that the device has been disconnected since being enumerated.
    /// </summary>
    eLeapRS_CannotOpenDevice = 0xE7010005,
  };

  public enum eLeapEventType {
    /// <summary>
    /// No event has occurred within the timeout period specified when calling LeapPollConnection().
    /// </summary>
    eLeapEventType_None = 0,
    /// <summary>
    /// A connection to the Leap Motion service has been established.
    /// </summary>
    eLeapEventType_Connection,
    /// <summary>
    /// The connection to the Leap Motion service has been lost.
    /// </summary>
    eLeapEventType_ConnectionLost,
    /// <summary>
    /// A device has been detected or plugged-in.
    /// A device event is dispatched after a connection is established for any
    /// devices already plugged in. (The system currently only supports one
    /// streaming device at a time.)
    /// </summary>
    eLeapEventType_Device,
    /// <summary>
    /// Note that unplugging a device generates an eLeapEventType_DeviceLost event
    /// message, not a failure message.
    /// </summary>
    eLeapEventType_DeviceFailure,
    /// <summary>
    /// A policy change has occurred.
    /// This can be due to setting a policy with LeapSetPolicyFlags() or due to changing
    /// or policy-related config settings, including images_mode.
    /// (A user can also change these policies using the Leap Motion Control Panel.)
    /// </summary>
    eLeapEventType_Policy,
    /// <summary>
    /// A tracking frame. The message contains the tracking data for the frame.
    /// </summary>
    eLeapEventType_Tracking = 0x100,
    /// <summary>
    /// The request for an image has failed.
    /// The message contains information about the failure. The client application
    /// will not receive the requested image set.
    /// </summary>
    eLeapEventType_ImageRequestError,
    /// <summary>
    /// The request for an image is complete.
    /// The image data has been completely written to the application-provided
    /// buffer.
    /// </summary>
    eLeapEventType_ImageComplete,
    /// <summary>
    /// A system message.
    /// </summary>
    eLeapEventType_LogEvent,
    /// <summary>
    ///  The device connection has been lost.
    /// 
    /// This event is generally asserted when the device has been detached from the system, when the
    /// connection to the service has been lost, or if the device is closed while streaming. Generally,
    /// any event where the system can conclude no further frames will be received will result in this
    /// message. The DeviceEvent field will be filled with the id of the formerly attached device.
    /// </summary>
    eLeapEventType_DeviceLost,
    /// <summary>
    /// The asynchronous response to a call to LeapRequestConfigValue().
    /// Contains the value of requested configuration item.
    /// </summary>
    eLeapEventType_ConfigResponse,
    /// <summary>
    /// The asynchronous response to a call to LeapSaveConfigValue().
    /// Reports whether the change succeeded or failed.
    /// </summary>
    eLeapEventType_ConfigChange,
    /// <summary>
    /// Notification that a status change has been detected on an attached device.
    /// </summary>
    eLeapEventType_DeviceStatusChange,
    /// <summary>
    /// A tracking frame has been dropped by the service.
    /// </summary>
    eLeapEventType_DroppedFrame,
    /// <summary>
    /// Notification that an unrequested stereo image pair is available.
    /// </summary>
    eLeapEventType_Image,
    /// <summary>
    /// Notification that point mapping has changed.
    /// </summary>
    eLeapEventType_PointMappingChange,
    /// <summary>
    /// An array of system messages.
    /// </summary>
    eLeapEventType_LogEvents,
    /// <summary>
    /// A new head pose is available.
    /// </summary>
    eLeapEventType_HeadPose
  };

  public enum eLeapDeviceFlag : uint {
    /// <summary>
    /// Flag set if the device is presently streaming frames
    /// 
    /// This flag is updated when the user pauses or resumes tracking on the device from the Leap control
    /// panel. Modification of this flag will fail if the AllowPauseResume policy is not set on this device
    /// object.
    /// </summary>
    eLeapDeviceFlag_Stream = 0x00000001
  };

  public enum eLeapDroppedFrameType {
    eLeapDroppedFrameType_PreprocessingQueue,
    eLeapDroppedFrameType_TrackingQueue,
    eLeapDroppedFrameType_Other
  };

  //Note the following LeapC structs are just IntPtrs in C#:
  // LEAP_CONNECTION is an IntPtr
  // LEAP_DEVICE is an IntPtr
  // LEAP_CLOCK_REBASER is an IntPtr

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_CONNECTION_CONFIG {
    public UInt32 size;
    public UInt32 flags;
    public IntPtr server_namespace; //char*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_INFO {
    public UInt32 size;
    public eLeapConnectionStatus status;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_EVENT {
    public eLeapServiceDisposition flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_REF {
    public IntPtr handle; //void *
    public UInt32 id;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_LOST_EVENT {
    public UInt32 flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_ALLOCATOR {
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Allocate allocate;
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public Deallocate deallocate;
    public IntPtr state;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_EVENT {
    public UInt32 flags;
    public LEAP_DEVICE_REF device;
    public eLeapDeviceStatus status;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DEVICE_FAILURE_EVENT {
    public eLeapDeviceStatus status;
    public IntPtr hDevice;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_TRACKING_EVENT {
    public LEAP_FRAME_HEADER info;
    public Int64 tracking_id;
    public UInt32 nHands;
    public IntPtr pHands; //LEAP_HAND*
    public float framerate;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DROPPED_FRAME_EVENT {
    public Int64 frame_id;
    public eLeapDroppedFrameType reason;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_POINT_MAPPING_CHANGE_EVENT {
    public Int64 frame_id;
    public Int64 timestamp;
    public UInt32 nPoints;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_POINT_MAPPING {
    public Int64 frame_id;
    public Int64 timestamp;
    public UInt32 nPoints;
    public IntPtr points;  //LEAP_VECTOR*
    public IntPtr ids;     //uint32*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_HEAD_POSE_EVENT {
    public Int64 timestamp;
    public LEAP_VECTOR head_position;
    public LEAP_QUATERNION head_orientation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONNECTION_MESSAGE {
    public UInt32 size;
    public eLeapEventType type;
    public IntPtr eventStructPtr;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DISCONNECTION_EVENT {
    public UInt32 reserved;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_DEVICE_INFO {
    public UInt32 size;
    public eLeapDeviceStatus status;
    public eLeapDeviceCaps caps;
    public eLeapDeviceType type;
    public UInt32 baseline;
    public UInt32 serial_length;
    public IntPtr serial; //char*
    public float h_fov;
    public float v_fov;
    public UInt32 range;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_FRAME_HEADER {
    public IntPtr reserved;
    public Int64 frame_id;
    public Int64 timestamp;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE_PROPERTIES {
    public eLeapImageType type;
    public eLeapImageFormat format;
    public UInt32 bpp;
    public UInt32 width;
    public UInt32 height;
    public float x_scale;
    public float y_scale;
    public float x_offset;
    public float y_offset;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE {
    public LEAP_IMAGE_PROPERTIES properties;
    public UInt64 matrix_version;

    //LEAP_DISTORTION_MATRIX* 
    //The struct LEAP_DISTORTION_MATRIX cannot exist in c# without using unsafe code
    //This is ok though, since it is just an array of floats
    //so you need to manually marshal this pointer to the correct size and type
    //See LeapC.h for details
    public IntPtr distortionMatrix;
    public IntPtr data; // void* of an allocator-supplied buffer
    public UInt32 offset; // Offset, in bytes, from beginning of buffer to start of image data
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IMAGE_EVENT {
    public LEAP_FRAME_HEADER info;
    public LEAP_IMAGE leftImage;
    public LEAP_IMAGE rightImage;
    public IntPtr calib; //LEAP_CALIBRATION
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_VECTOR {
    public float x;
    public float y;
    public float z;

    public Leap.Vector ToLeapVector() {
      return new Leap.Vector(x, y, z);
    }

    public LEAP_VECTOR(Leap.Vector leap) {
      x = leap.x;
      y = leap.y;
      z = leap.z;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    public float x;
    public float y;
    public float z;
    public float w;

    public Leap.LeapQuaternion ToLeapQuaternion() {
      return new Leap.LeapQuaternion(x, y, z, w);
    }

    public LEAP_QUATERNION(Leap.LeapQuaternion q) {
      x = q.x;
      y = q.y;
      z = q.z;
      w = q.w;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_MATRIX_3x3 {
    public LEAP_VECTOR m1;
    public LEAP_VECTOR m2;
    public LEAP_VECTOR m3;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_BONE {
    public LEAP_VECTOR prev_joint;
    public LEAP_VECTOR next_joint;
    public float width;
    public LEAP_QUATERNION rotation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_DIGIT {
    public Int32 finger_id;
    public LEAP_BONE metacarpal;
    public LEAP_BONE proximal;
    public LEAP_BONE intermediate;
    public LEAP_BONE distal;
    public Int32 is_extended;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_PALM {
    public LEAP_VECTOR position;
    public LEAP_VECTOR stabilized_position;
    public LEAP_VECTOR velocity;
    public LEAP_VECTOR normal;
    public float width;
    public LEAP_VECTOR direction;
    public LEAP_QUATERNION orientation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_HAND {
    public UInt32 id;
    public UInt32 flags;
    public eLeapHandType type;
    public float confidence;
    public UInt64 visible_time;
    public float pinch_distance;
    public float grab_angle;
    public float pinch_strength;
    public float grab_strength;
    public LEAP_PALM palm;
    public LEAP_DIGIT thumb;
    public LEAP_DIGIT index;
    public LEAP_DIGIT middle;
    public LEAP_DIGIT ring;
    public LEAP_DIGIT pinky;
    public LEAP_BONE arm;
  }


  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_TIP {
    public LEAP_VECTOR position;
    public float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_LOG_EVENT {
    public eLeapLogSeverity severity;
    public Int64 timestamp;
    public IntPtr message; //char*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_POLICY_EVENT {
    public UInt32 reserved;
    public UInt32 current_policy;
  }

  [StructLayout(LayoutKind.Explicit, Pack = 1)]
  public struct LEAP_VARIANT_VALUE_TYPE {
    [FieldOffset(0)]
    public eLeapValueType type;
    [FieldOffset(4)]
    public Int32 boolValue;
    [FieldOffset(4)]
    public Int32 intValue;
    [FieldOffset(4)]
    public float floatValue;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_VARIANT_REF_TYPE {
    public eLeapValueType type;
    public string stringValue;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONFIG_RESPONSE_EVENT {
    public UInt32 requestId;
    public LEAP_VARIANT_VALUE_TYPE value;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE {
    public UInt32 requestId;
    public LEAP_VARIANT_REF_TYPE value;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_CONFIG_CHANGE_EVENT {
    public UInt32 requestId;
    public bool status;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
  public struct LEAP_TELEMETRY_DATA {
    public UInt32 threadId;
    public UInt64 startTime;
    public UInt64 endTime;
    public UInt32 zoneDepth;
    public string fileName;
    public UInt32 lineNumber;
    public string zoneName;
  }

  public class LeapC {
    private LeapC() { }
    public static int DistortionSize = 64;

    [DllImport("LeapC", EntryPoint = "LeapGetNow")]
    public static extern long GetNow();

    [DllImport("LeapC", EntryPoint = "LeapCreateClockRebaser")]
    public static extern eLeapRS CreateClockRebaser(out IntPtr phClockRebaser);

    [DllImport("LeapC", EntryPoint = "LeapDestroyClockRebaser")]
    public static extern eLeapRS DestroyClockRebaser(IntPtr hClockRebaser);

    [DllImport("LeapC", EntryPoint = "LeapUpdateRebase")]
    public static extern eLeapRS UpdateRebase(IntPtr hClockRebaser, Int64 userClock, Int64 leapClock);

    [DllImport("LeapC", EntryPoint = "LeapRebaseClock")]
    public static extern eLeapRS RebaseClock(IntPtr hClockRebaser, Int64 userClock, out Int64 leapClock);

    [DllImport("LeapC", EntryPoint = "LeapCreateConnection")]
    public static extern eLeapRS CreateConnection(ref LEAP_CONNECTION_CONFIG pConfig, out IntPtr pConnection);

    //Overrides to allow config to be set to null to use default config
    [DllImport("LeapC", EntryPoint = "LeapCreateConnection")]
    private static extern eLeapRS CreateConnection(IntPtr nulled, out IntPtr pConnection);
    public static eLeapRS CreateConnection(out IntPtr pConnection) {
      return CreateConnection(IntPtr.Zero, out pConnection);
    }

    [DllImport("LeapC", EntryPoint = "LeapGetConnectionInfo")]
    public static extern eLeapRS GetConnectionInfo(IntPtr hConnection, ref LEAP_CONNECTION_INFO pInfo);

    [DllImport("LeapC", EntryPoint = "LeapOpenConnection")]
    public static extern eLeapRS OpenConnection(IntPtr hConnection);

    [DllImport("LeapC", EntryPoint = "LeapSetAllocator")]
    public static extern eLeapRS SetAllocator(IntPtr hConnection, ref LEAP_ALLOCATOR pAllocator);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceList")]
    public static extern eLeapRS GetDeviceList(IntPtr hConnection, [In, Out] LEAP_DEVICE_REF[] pArray, out UInt32 pnArray);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceList")]
    private static extern eLeapRS GetDeviceList(IntPtr hConnection, [In, Out] IntPtr pArray, out UInt32 pnArray);
    //Override to allow pArray argument to be set to null (IntPtr.Zero) in order to get the device count
    public static eLeapRS GetDeviceCount(IntPtr hConnection, out UInt32 deviceCount) {
      return GetDeviceList(hConnection, IntPtr.Zero, out deviceCount);
    }

    [DllImport("LeapC", EntryPoint = "LeapOpenDevice")]
    public static extern eLeapRS OpenDevice(LEAP_DEVICE_REF rDevice, out IntPtr pDevice);

    [DllImport("LeapC", EntryPoint = "LeapGetDeviceInfo", CharSet = CharSet.Ansi)]
    public static extern eLeapRS GetDeviceInfo(IntPtr hDevice, ref LEAP_DEVICE_INFO info);

    [DllImport("LeapC", EntryPoint = "LeapSetPolicyFlags")]
    public static extern eLeapRS SetPolicyFlags(IntPtr hConnection, UInt64 set, UInt64 clear);

    [DllImport("LeapC", EntryPoint = "LeapSetDeviceFlags")]
    public static extern eLeapRS SetDeviceFlags(IntPtr hDevice, UInt64 set, UInt64 clear, out UInt64 prior);

    [DllImport("LeapC", EntryPoint = "LeapPollConnection")]
    public static extern eLeapRS PollConnection(IntPtr hConnection, UInt32 timeout, ref LEAP_CONNECTION_MESSAGE msg);

    [DllImport("LeapC", EntryPoint = "LeapGetFrameSize")]
    public static extern eLeapRS GetFrameSize(IntPtr hConnection, Int64 timestamp, out UInt64 pncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateFrame")]
    public static extern eLeapRS InterpolateFrame(IntPtr hConnection, Int64 timestamp, IntPtr pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateFrameFromTime")]
    public static extern eLeapRS InterpolateFrameFromTime(IntPtr hConnection, Int64 timestamp, Int64 sourceTimestamp, IntPtr pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapInterpolateHeadPose")]
    public static extern eLeapRS InterpolateHeadPose(IntPtr hConnection, Int64 timestamp, ref LEAP_HEAD_POSE_EVENT headPose);

    [DllImport("LeapC", EntryPoint = "LeapPixelToRectilinear")]
    public static extern LEAP_VECTOR LeapPixelToRectilinear(IntPtr hConnection, eLeapPerspectiveType camera, LEAP_VECTOR pixel);

    [DllImport("LeapC", EntryPoint = "LeapRectilinearToPixel")]
    public static extern LEAP_VECTOR LeapRectilinearToPixel(IntPtr hConnection, eLeapPerspectiveType camera, LEAP_VECTOR rectilinear);

    [DllImport("LeapC", EntryPoint = "LeapCloseDevice")]
    public static extern void CloseDevice(IntPtr pDevice);

    [DllImport("LeapC", EntryPoint = "LeapCloseConnection")]
    public static extern eLeapRS CloseConnection(IntPtr hConnection);

    [DllImport("LeapC", EntryPoint = "LeapDestroyConnection")]
    public static extern void DestroyConnection(IntPtr connection);

    [DllImport("LeapC", EntryPoint = "LeapSaveConfigValue")]
    private static extern eLeapRS SaveConfigValue(IntPtr hConnection, string key, IntPtr value, out UInt32 requestId);

    [DllImport("LeapC", EntryPoint = "LeapRequestConfigValue")]
    public static extern eLeapRS RequestConfigValue(IntPtr hConnection, string name, out UInt32 request_id);

    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, bool value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE(); //This is a C# approximation of a C union
      valueStruct.type = eLeapValueType.eLeapValueType_Boolean;
      valueStruct.boolValue = value ? 1 : 0;
      return SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, Int32 value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE();
      valueStruct.type = eLeapValueType.eLeapValueType_Int32;
      valueStruct.intValue = value;
      return SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, float value, out UInt32 requestId) {
      LEAP_VARIANT_VALUE_TYPE valueStruct = new LEAP_VARIANT_VALUE_TYPE();
      valueStruct.type = eLeapValueType.eLeapValueType_Float;
      valueStruct.floatValue = value;
      return SaveConfigWithValueType(hConnection, key, valueStruct, out requestId);
    }
    public static eLeapRS SaveConfigValue(IntPtr hConnection, string key, string value, out UInt32 requestId) {
      LEAP_VARIANT_REF_TYPE valueStruct;
      valueStruct.type = eLeapValueType.eLeapValueType_String;
      valueStruct.stringValue = value;
      return SaveConfigWithRefType(hConnection, key, valueStruct, out requestId);
    }
    private static eLeapRS SaveConfigWithValueType(IntPtr hConnection, string key, LEAP_VARIANT_VALUE_TYPE valueStruct, out UInt32 requestId) {
      IntPtr configValue = Marshal.AllocHGlobal(Marshal.SizeOf(valueStruct));
      eLeapRS callResult = eLeapRS.eLeapRS_UnknownError;
      try {
        Marshal.StructureToPtr(valueStruct, configValue, false);
        callResult = SaveConfigValue(hConnection, key, configValue, out requestId);
      } finally {
        Marshal.FreeHGlobal(configValue);
      }
      return callResult;
    }
    private static eLeapRS SaveConfigWithRefType(IntPtr hConnection, string key, LEAP_VARIANT_REF_TYPE valueStruct, out UInt32 requestId) {
      IntPtr configValue = Marshal.AllocHGlobal(Marshal.SizeOf(valueStruct));
      eLeapRS callResult = eLeapRS.eLeapRS_UnknownError;
      try {
        Marshal.StructureToPtr(valueStruct, configValue, false);
        callResult = SaveConfigValue(hConnection, key, configValue, out requestId);
      } finally {
        Marshal.FreeHGlobal(configValue);
      }
      return callResult;
    }

    [DllImport("LeapC", EntryPoint = "LeapGetPointMappingSize")]
    public static extern eLeapRS GetPointMappingSize(IntPtr hConnection, ref ulong pSize);

    [DllImport("LeapC", EntryPoint = "LeapGetPointMapping")]
    public static extern eLeapRS GetPointMapping(IntPtr hConnection, IntPtr pointMapping, ref ulong pSize);

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LEAP_RECORDING_PARAMETERS {
      public UInt32 mode;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct LEAP_RECORDING_STATUS {
      public UInt32 mode;
    }

    [DllImport("LeapC", EntryPoint = "LeapRecordingOpen")]
    public static extern eLeapRS RecordingOpen(ref IntPtr ppRecording, string userPath, LEAP_RECORDING_PARAMETERS parameters);

    [DllImport("LeapC", EntryPoint = "LeapRecordingClose")]
    public static extern eLeapRS RecordingClose(ref IntPtr ppRecording);

    [DllImport("LeapC", EntryPoint = "LeapRecordingGetStatus")]
    public static extern eLeapRS LeapRecordingGetStatus(IntPtr pRecording, ref LEAP_RECORDING_STATUS status);

    [DllImport("LeapC", EntryPoint = "LeapRecordingReadSize")]
    public static extern eLeapRS RecordingReadSize(IntPtr pRecording, ref UInt64 pncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapRecordingRead")]
    public static extern eLeapRS RecordingRead(IntPtr pRecording, ref LEAP_TRACKING_EVENT pEvent, UInt64 ncbEvent);

    [DllImport("LeapC", EntryPoint = "LeapRecordingWrite")]
    public static extern eLeapRS RecordingWrite(IntPtr pRecording, ref LEAP_TRACKING_EVENT pEvent, ref UInt64 pnBytesWritten);

    [DllImport("LeapC", EntryPoint = "LeapTelemetryProfiling")]
    public static extern eLeapRS LeapTelemetryProfiling(IntPtr hConnection, ref LEAP_TELEMETRY_DATA telemetryData);

    [DllImport("LeapC", EntryPoint = "LeapTelemetryGetNow")]
    public static extern UInt64 TelemetryGetNow();
  }
}
