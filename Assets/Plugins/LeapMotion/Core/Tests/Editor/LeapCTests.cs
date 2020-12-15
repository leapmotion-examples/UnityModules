/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if !UNITY_EDITOR_LINUX

using System;
using System.Threading;
using System.Runtime.InteropServices;

using NUnit.Framework;
using LeapInternal;

namespace Leap.LeapCSharp.Tests {

  [TestFixture]
  public class LeapCTests {

    [Test]
    public void TestNow() {
      long start = LeapC.GetNow();
      Thread.Sleep(1);

      long stop = LeapC.GetNow();
      long delta = stop - start;

      Assert.Greater(delta, 200);
      Assert.Less(delta, 1800);
    }

    [Test]
    public void TestRebaserLifeCycle() {
      IntPtr rebaser = IntPtr.Zero;
      eLeapRS result = LeapC.CreateClockRebaser(out rebaser);
      Assert.True(result == eLeapRS.eLeapRS_Success);
      Assert.AreNotEqual(IntPtr.Zero, rebaser, "Handle no longer zero");

      Int64 sysNow = DateTime.Now.Millisecond;
      Int64 leapNow = LeapC.GetNow();
      result = LeapC.UpdateRebase(rebaser, sysNow, leapNow);
      Assert.True(result == eLeapRS.eLeapRS_Success);

      Int64 rebasedTime;
      result = LeapC.RebaseClock(rebaser, sysNow + 10, out rebasedTime);
      Assert.True(result == eLeapRS.eLeapRS_Success);

      Logger.Log("Rebased: " + rebasedTime);
      LeapC.DestroyClockRebaser(rebaser);
    }

    [Test]
    public void TestCreateConnection() {
      eLeapRS result;
      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      config.server_namespace = Marshal.StringToHGlobalAnsi("Leap Service");
      config.flags = 0;
      config.size = (uint)Marshal.SizeOf(config);
      IntPtr connHandle = IntPtr.Zero;
      result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Assert.False(connHandle == IntPtr.Zero, "Configured connection failed.");

      IntPtr defConn = IntPtr.Zero;
      result = LeapC.CreateConnection(out defConn);
      Assert.False(defConn == IntPtr.Zero, "Default config connection failed.");
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Marshal.FreeHGlobal(config.server_namespace);
    }

    [Ignore("info.status is not returning true even though the result was returned "
          + "as successful. See the commented-out assert at the bottom of the test.")]
    [Test]
    public void TestOpenConnection() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      LEAP_CONNECTION_INFO info = new LEAP_CONNECTION_INFO();
      info.size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_INFO));
      result = LeapC.GetConnectionInfo(connHandle, ref info);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      // Assert.AreEqual(eLeapConnectionStatus.eLeapConnectionStatus_Connected, info.status, "Status: " + info.status);
    }

    [Test]
    public void TestGetConnectionInfo() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result;
      LEAP_CONNECTION_INFO info = new LEAP_CONNECTION_INFO();
      info.status = eLeapConnectionStatus.eLeapConnectionStatus_HandshakeIncomplete;
      info.size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_INFO));
      result = LeapC.GetConnectionInfo(connHandle, ref info);
      Assert.AreEqual(eLeapRS.eLeapRS_InvalidArgument, result);

      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      result = LeapC.GetConnectionInfo(connHandle, ref info);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Assert.AreEqual(eLeapConnectionStatus.eLeapConnectionStatus_NotConnected, info.status, "Not connected before OpenConnection");

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      int polls = 10;
      for (int i = 1; i <= polls; i++) {
        LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
        uint timeout = 1;
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (result == eLeapRS.eLeapRS_Success) break;
      }

      result = LeapC.GetConnectionInfo(connHandle, ref info);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Assert.AreEqual(eLeapConnectionStatus.eLeapConnectionStatus_Connected, info.status, "Connection info says we are connected");

    }

    [Test]
    public void TestPollConnection() {
      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Created connection");

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection");

      int polls = 10;
      for (int i = 1; i <= polls; i++) {
        LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
        uint timeout = 1;
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        Console.WriteLine("Poll #" + i + " Msg type: " + msg.type + " result type: " + result);

        Assert.True(result == eLeapRS.eLeapRS_Success || result == eLeapRS.eLeapRS_Timeout, "Poll #" + i + " of " + polls);
        //Thread.Sleep (54);
      }
    }

    [Test]
    public void TestInterpolateFrames() {
      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Created connection");

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      //Get 6 frames
      int polls = 100;
      int frameCount = 0;
      LEAP_TRACKING_EVENT firstBefore = new LEAP_TRACKING_EVENT(); // Prevent
      LEAP_TRACKING_EVENT first = new LEAP_TRACKING_EVENT();       // 'unassigned'
      LEAP_TRACKING_EVENT firstAfter = new LEAP_TRACKING_EVENT();  // errors.
      LEAP_TRACKING_EVENT tenthBefore;
      LEAP_TRACKING_EVENT tenth;
      LEAP_TRACKING_EVENT tenthAfter;
      int selected = 5;
      for (int i = 1; i <= polls; i++) {
        LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
        uint timeout = 100;
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Tracking) {
          if (frameCount == selected - 1)
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out firstBefore);
          if (frameCount == selected)
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out first);
          if (frameCount == selected + 1)
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out firstAfter);
          if (frameCount == selected + 2)
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out tenthBefore);
          if (frameCount == selected + 3)
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out tenth);
          if (frameCount == selected + 4) {
            StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(msg.eventStructPtr, out tenthAfter);
            break;
          }
          frameCount++;
        }
      }
      //Int64 halfTime = (first.info.timestamp - firstBefore.info.timestamp)/2;
      Int64 testTime = first.info.timestamp;// + halfTime;
      UInt64 size;
      Logger.Log("PrevF : " + firstBefore.info.timestamp);
      Logger.Log("Test  : " + testTime);
      Logger.Log("NextF : " + firstAfter.info.timestamp);

      result = LeapC.GetFrameSize(connHandle, testTime, out size);
      Logger.Log("Size: " + size);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Frame size call succeeded " + result.indexOf());

      IntPtr trackingBuffer = Marshal.AllocHGlobal((Int32)size);
      result = LeapC.InterpolateFrame(connHandle, testTime, trackingBuffer, size);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Frame interpolation succeeded.");

      LEAP_TRACKING_EVENT tracking_evt;
      StructMarshal<LEAP_TRACKING_EVENT>.PtrToStruct(trackingBuffer, out tracking_evt);
      Assert.AreEqual(first.info.frame_id, tracking_evt.info.frame_id, "Interpolated frame has ID");
      Marshal.FreeHGlobal(trackingBuffer);

    }

    [Test]
    public void TestGetDeviceList() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Get device count
      UInt32 deviceCount = 0;
      result = LeapC.GetDeviceCount(connHandle, out deviceCount);
      Logger.Log("DC: " + deviceCount);
      //Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Device count call successful");
      Assert.Greater(deviceCount, 0, deviceCount + " devices exist");

      UInt32 validDeviceHandles = deviceCount;
      LEAP_DEVICE_REF[] deviceList = new LEAP_DEVICE_REF[deviceCount];
      result = LeapC.GetDeviceList(connHandle, deviceList, out validDeviceHandles);
      Assert.AreEqual(deviceCount, validDeviceHandles, validDeviceHandles + " existing devices are valid");
      Logger.Log("VDHC: " + validDeviceHandles);

      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Device list call successful");
    }

    [Test]
    public void TestOpenDevice() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Get device count
      UInt32 deviceCount = 0;
      result = LeapC.GetDeviceCount(connHandle, out deviceCount);
      Assert.True(eLeapRS.eLeapRS_Success == result || eLeapRS.eLeapRS_InsufficientBuffer == result, "Device count call successful ");
      Assert.Greater(deviceCount, 0, "Devices exist");

      UInt32 validDeviceHandles = deviceCount;
      LEAP_DEVICE_REF[] deviceList = new LEAP_DEVICE_REF[deviceCount];
      result = LeapC.GetDeviceList(connHandle, deviceList, out validDeviceHandles);
      Assert.AreEqual(deviceCount, validDeviceHandles, "Existing devices are valid");

      foreach (LEAP_DEVICE_REF deviceRef in deviceList) {
        IntPtr device;
        Logger.LogStruct(deviceRef);
        result = LeapC.OpenDevice(deviceRef, out device);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Call successful");
        Assert.AreNotEqual(IntPtr.Zero, device, "Device handle not zero");
      }
    }

    [Test]
    public void TestGetDeviceInfo() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Get device count
      UInt32 deviceCount = 0;
      result = LeapC.GetDeviceCount(connHandle, out deviceCount);
      Assert.True(eLeapRS.eLeapRS_Success == result || eLeapRS.eLeapRS_InsufficientBuffer == result, "Device count call successful ");
      Assert.Greater(deviceCount, 0, "Devices exist");

      UInt32 validDeviceHandles = deviceCount;
      LEAP_DEVICE_REF[] deviceList = new LEAP_DEVICE_REF[deviceCount];
      result = LeapC.GetDeviceList(connHandle, deviceList, out validDeviceHandles);
      Assert.AreEqual(deviceCount, validDeviceHandles, "Existing devices are valid");

      foreach (LEAP_DEVICE_REF deviceRef in deviceList) {
        IntPtr device;
        Logger.LogStruct(deviceRef);
        result = LeapC.OpenDevice(deviceRef, out device);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Call successful");
        LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
        int defaultLength = 1;
        deviceInfo.serial = Marshal.AllocCoTaskMem(defaultLength);
        deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
        Logger.Log("DeviceInfo size: " + deviceInfo.size);
        deviceInfo.serial_length = (uint)defaultLength;
        Logger.LogStruct(deviceInfo, "Before: ");
        result = LeapC.GetDeviceInfo(device, ref deviceInfo);
        Assert.AreEqual(eLeapRS.eLeapRS_InsufficientBuffer, result, "not enough buffer");
        if (deviceInfo.serial_length != (uint)defaultLength) {
          deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length);
          deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
          result = LeapC.GetDeviceInfo(device, ref deviceInfo);
          Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "2nd Call successful");
        }
        Logger.LogStruct(deviceInfo, "After: ");
        string serialnumber = Marshal.PtrToStringAnsi(deviceInfo.serial);
        Logger.Log(serialnumber);
      }
    }

    [Test]
    public void TestGetOneDeviceInfo() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      LEAP_DEVICE_EVENT device_evt;
      StructMarshal<LEAP_DEVICE_EVENT>.PtrToStruct(msg.eventStructPtr, out device_evt);
      IntPtr device;
      result = LeapC.OpenDevice(device_evt.device, out device);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Call successful");
      LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
      int defaultLength = 1;
      deviceInfo.serial = Marshal.AllocCoTaskMem(defaultLength);
      deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
      Logger.Log("DeviceInfo size: " + deviceInfo.size);
      deviceInfo.serial_length = (uint)defaultLength;
      Logger.LogStruct(deviceInfo, "Before: ");
      result = LeapC.GetDeviceInfo(device, ref deviceInfo);
      Assert.AreEqual(eLeapRS.eLeapRS_InsufficientBuffer, result, "not enough buffer");
      if (deviceInfo.serial_length != (uint)defaultLength) {
        Marshal.FreeCoTaskMem(deviceInfo.serial);
        deviceInfo.serial = Marshal.AllocCoTaskMem((int)deviceInfo.serial_length);
        deviceInfo.size = (uint)Marshal.SizeOf(deviceInfo);
        result = LeapC.GetDeviceInfo(device, ref deviceInfo);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "2nd Call successful");
      }
      Logger.LogStruct(deviceInfo, "After: ");
      string serialnumber = Marshal.PtrToStringAnsi(deviceInfo.serial);
      Marshal.FreeCoTaskMem(deviceInfo.serial);
      Logger.Log(serialnumber);
    }

    [Test]
    public void TestSetPolicyFlags() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      UInt64 setFlags = 0;
      UInt64 clearFlags = 0;
      result = LeapC.SetPolicyFlags(connHandle, setFlags, clearFlags);
      Logger.Log(result);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "SetDevicePolicyFlags Call");
    }

    //public  static extern eLeapRS  LeapSetDeviceFlags (LEAP_DEVICE hDevice, UInt64 set, UInt64 clear, out UInt64* prior);
    [Test]
    public void TestSetDeviceFlags() {
      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Get device count
      UInt32 deviceCount = 0;
      result = LeapC.GetDeviceCount(connHandle, out deviceCount);
      Assert.AreEqual(eLeapRS.eLeapRS_InsufficientBuffer, result, "GetDeviceCount Call ");
      Assert.Greater(deviceCount, 0, "Devices exist");

      UInt32 validDeviceHandles = deviceCount;
      LEAP_DEVICE_REF[] deviceList = new LEAP_DEVICE_REF[deviceCount];
      result = LeapC.GetDeviceList(connHandle, deviceList, out validDeviceHandles);
      Assert.AreEqual(deviceCount, validDeviceHandles, "Existing devices are valid");

      foreach (LEAP_DEVICE_REF deviceRef in deviceList) {
        IntPtr device;
        result = LeapC.OpenDevice(deviceRef, out device);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "OpenDevice Call successful");
        //UInt64 setFlags = 0, clearFlags = 0, priorFlags = 0;
        //result = LeapC.SetDeviceFlags(device, setFlags, clearFlags, out priorFlags);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "SetDeviceFlags Call successful");
      }
    }

    //public static extern void LeapCloseDevice (LEAP_DEVICE pDevice);
    [Test]
    public void TestCloseDevice() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Get device count
      UInt32 deviceCount = 0;
      result = LeapC.GetDeviceCount(connHandle, out deviceCount);
      Assert.Greater(deviceCount, 0, "Devices exist: " + deviceCount);
      //            Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "GetDeviceCount Call successful");

      UInt32 validDeviceHandles = deviceCount;
      LEAP_DEVICE_REF[] deviceList = new LEAP_DEVICE_REF[deviceCount];
      result = LeapC.GetDeviceList(connHandle, deviceList, out validDeviceHandles);
      Assert.AreEqual(deviceCount, validDeviceHandles, "Existing devices are valid");

      foreach (LEAP_DEVICE_REF deviceRef in deviceList) {
        IntPtr device;
        //Assert.True (false, "This test is blocked by a bug -- device handle always 0");
        result = LeapC.OpenDevice(deviceRef, out device);
        Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "OpenDevice Call successful");
        LeapC.CloseDevice(device); //TODO How to verify?
      }
    }

    //public static extern void LeapDestroyConnection (LEAP_CONNECTION connection);
    [Test]
    public void TestDestroyConnection() {
      LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG();
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(ref config, out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result);
      Assert.False(connHandle == IntPtr.Zero, "configured connection failed.");
      LeapC.DestroyConnection(connHandle); //TODO How to verify?
    }

    [Test]
    public void TestBoolConfigReadWrite() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      UInt32 requestId = 1;
      result = LeapC.SaveConfigValue(connHandle, "image_processing_auto_flip", false, out requestId);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      int attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      LEAP_CONFIG_CHANGE_EVENT response;
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestId, ReturnedRequestID, "Request ID is the same");
      UnityEngine.Debug.Log("Response status: " + response.status);
      Assert.True(response.status == true, "Save successful");

      //read the value back
      UInt32 requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "image_processing_auto_flip", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        //Logger.Log ("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigResponse, configMsg.type);
      LEAP_CONFIG_RESPONSE_EVENT set_response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out set_response);
      Logger.LogStruct(configMsg);
      Logger.LogStruct(set_response);
      Logger.LogStruct(set_response.value);
      ReturnedRequestID = set_response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Boolean, set_response.value.type, "Got a Boolean value");
      Assert.False(set_response.value.boolValue == 0, "Auto-flip is disabled");

      //Set to opposite boolean
      result = LeapC.SaveConfigValue(connHandle, "image_processing_auto_flip", (set_response.value.boolValue == 0 ? true : false), out requestId);

      //read the value back again
      requestID = 2;
      result = LeapC.RequestConfigValue(connHandle, "image_processing_auto_flip", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        //Logger.Log ("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out set_response);
      Logger.LogStruct(configMsg);
      Logger.LogStruct(set_response);
      Logger.LogStruct(set_response.value);
      ReturnedRequestID = set_response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Boolean, set_response.value.type, "Got a Boolean value");
      Assert.True(set_response.value.boolValue == 1, "Auto-flip is enabled again");
    }

    [Ignore("There are no public-facing float config settings. (The LeapC gestures API is "
          + "deprecated.")]
    [Test]
    public void TestReadFloatConfig() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }
      UInt32 requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "Gesture.Swipe.MinVelocity", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      int attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      LEAP_CONFIG_RESPONSE_EVENT response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Float, response.value.type, "Got a Float value");
      Assert.True(response.value.floatValue != float.NaN, "Is a float");
      Assert.AreEqual(1000, response.value.floatValue, "Swipe min velocity is 3mm, the default value");
    }

    [Ignore("LeapC does not document any float config settings to test this with.")]
    [Test]
    public void TestFloatConfigReadWrite() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Set to something
      UInt32 requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "tool_radius_filtering", 3.0f, out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      int attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      LEAP_CONFIG_CHANGE_EVENT change;
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read first
      requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "tool_radius_filtering", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      LEAP_CONFIG_RESPONSE_EVENT response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Float, response.value.type, "Got a Float value");
      Assert.True(response.value.floatValue != float.NaN, "Is a float");
      Assert.AreEqual(3, response.value.floatValue, "Keytap min distance is 3mm, the default value");

      //Set to something else
      requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "tool_radius_filtering", 6.0f, out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read again to verify write
      requestID = 2;
      result = LeapC.RequestConfigValue(connHandle, "tool_radius_filtering", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Third Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Float, response.value.type, "Got a Float value 2nd time, too");
      Assert.True(response.value.floatValue != float.NaN, "Is a float");
      Assert.AreEqual(6.0f, response.value.floatValue, "Keytap min distance is 6mm, the changed value");

    }

    [Ignore("LeapC appears to have a problem returning status: true for a successful "
          + "config change event for images_mode.")]
    [Test]
    public void TestInt32ConfigReadWrite() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      //Set to something
      UInt32 requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "images_mode", 1, out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      int attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      LEAP_CONFIG_CHANGE_EVENT change;
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read first
      requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "images_mode", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      LEAP_CONFIG_RESPONSE_EVENT response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Int32, response.value.type, "Got an Int32 value");
      Assert.AreEqual(1, response.value.intValue, "images_mode should be 1");

      // Set to something else.
      requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "images_mode", 2, out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read again to verify write
      requestID = 2;
      result = LeapC.RequestConfigValue(connHandle, "images_mode", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out response);
      ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_Int32, response.value.type, "Got a Int32 value the second time, too");
      Assert.AreEqual(2, response.value.intValue, "images_mode is 2, the changed value");
    }

    [Ignore("No known public settings return a string.")]
    [Test]
    public void TestGetStringConfigValue() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }

      UInt32 requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "tracking_version", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      int attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_String, response.value.type, "Got a String value");
      Assert.AreEqual("v2", response.value.stringValue, "Got expected string");

    }

    [Ignore("No public config settings exist for strings.")]
    [Test]
    public void TestStringConfigReadWrite() {
      IntPtr connHandle = IntPtr.Zero;
      eLeapRS result = LeapC.CreateConnection(out connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Connection created");

      //Open connection
      result = LeapC.OpenConnection(connHandle);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Opened connection.");

      //Wait for device event
      LEAP_CONNECTION_MESSAGE msg = new LEAP_CONNECTION_MESSAGE();
      uint timeout = 100;
      int tries = 100;
      for (int t = 0; t < tries; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref msg);
        if (msg.type == eLeapEventType.eLeapEventType_Device)
          break;
      }
      Assert.AreNotEqual(tries, 100, "PollConnection timed out trying to get "
        + "a ConfigChange event.");

      //Set to something
      UInt32 requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "tilt_axis", "y", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      LEAP_CONNECTION_MESSAGE configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      int attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreNotEqual(attempts, 1000, "PollConnection timed out trying to get "
        + "a ConfigChange event.");
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      LEAP_CONFIG_CHANGE_EVENT change;
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read first
      requestID = 1;
      result = LeapC.RequestConfigValue(connHandle, "tilt_axis", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      Assert.AreNotEqual(attempts, 100, "PollConnection timed out trying to get "
        + "a ConfigResponse event.");
      LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE response;
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(configMsg.eventStructPtr, out response);
      UInt32 ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_String, response.value.type, "Got an String value");
      Assert.AreEqual("y", response.value.stringValue, "Got y.");

      //Set to something else
      requestID = 5;
      result = LeapC.SaveConfigValue(connHandle, "tilt_axis", "x", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config save requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 100;
      attempts = 1000;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigChange)
          break;
      }
      Assert.AreNotEqual(attempts, 1000, "PollConnection timed out trying to get "
        + "a ConfigChange event.");
      Assert.AreEqual(eLeapEventType.eLeapEventType_ConfigChange, configMsg.type);
      StructMarshal<LEAP_CONFIG_CHANGE_EVENT>.PtrToStruct(configMsg.eventStructPtr, out change);
      Assert.AreEqual(requestID, change.requestId, "Request ID is the same");
      Assert.True(change.status == true, "Save successful");

      //Read again to verify write
      requestID = 2;
      result = LeapC.RequestConfigValue(connHandle, "tilt_axis", out requestID);
      Assert.AreEqual(eLeapRS.eLeapRS_Success, result, "Config value requested");
      configMsg = new LEAP_CONNECTION_MESSAGE();

      timeout = 10;
      attempts = 100;
      for (int t = 0; t < attempts; t++) {
        result = LeapC.PollConnection(connHandle, timeout, ref configMsg);
        Logger.Log("Msg type: " + configMsg.type);
        if (configMsg.type == eLeapEventType.eLeapEventType_ConfigResponse)
          break;
      }
      Assert.AreNotEqual(attempts, 100, "PollConnection timed out trying to get "
        + "a ConfigResponse event.");
      StructMarshal<LEAP_CONFIG_RESPONSE_EVENT_WITH_REF_TYPE>.PtrToStruct(configMsg.eventStructPtr, out response);
      ReturnedRequestID = response.requestId;
      Assert.AreEqual(requestID, ReturnedRequestID, "Request ID is the same");
      Assert.AreEqual(eLeapValueType.eLeapValueType_String, response.value.type, "Got a String value the second time, too");
      Assert.AreEqual("x", response.value.stringValue, "Got x");
    }

  }
}

#endif
