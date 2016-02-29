﻿using UnityEngine;
using System.Collections;
using LeapInternal;
using Leap;

namespace Leap {
  public class LeapProvider :
    MonoBehaviour {
    public IFrame CurrentFrame { get { return _currentFrame; } }
    public Image CurrentImage { get; private set; }
    private Transform providerSpace;
    private Matrix leapMat;

    private TransformedFrame _currentFrame;
    private TransformedFrame _currentFixedFrame;

    protected Controller leap_controller_;

    /** The smoothed offset between the FixedUpdate timeline and the Leap timeline.  
   * Used to provide temporally correct frames within FixedUpdate */
    private SmoothedFloat smoothedFixedUpdateOffset_ = new SmoothedFloat();
    /** The maximum offset calculated per frame */
    public float PerFrameFixedUpdateOffset;
    /** Conversion factor for nanoseconds to seconds. */
    protected const float NS_TO_S = 1e-6f;
    /** Conversion factor for seconds to nanoseconds. */
    protected const float S_TO_NS = 1e6f;
    /** How much smoothing to use when calculating the FixedUpdate offset. */
    protected const float FIXED_UPDATE_OFFSET_SMOOTHING_DELAY = 0.1f;

    /** Set true if the Leap Motion hardware is mounted on an HMD; otherwise, leave false. */
    public bool isHeadMounted = false;

    public bool overrideDeviceType = false;

    /** If overrideDeviceType is enabled, the hand controller will return a device of this type. */
    public LeapDeviceType overrideDeviceTypeWith = LeapDeviceType.Peripheral;

    void Awake() {
      leap_controller_ = new Controller();
      if (leap_controller_.IsConnected) {
        InitializeFlags();
      }
      leap_controller_.Device += HandleControllerConnect;
    }

    // Use this for initialization
    void Start() {
      //set empty frame
        _currentFrame = new TransformedFrame();
        _currentFixedFrame = new TransformedFrame();
    }

    void HandleControllerConnect(object sender, LeapEventArgs args) {
      InitializeFlags();
    }

    protected void OnDisable() {
      leap_controller_.Device -= HandleControllerConnect;
    }

    /** 
* Initializes the Leap Motion policy flags.
* The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
*/
    void InitializeFlags() {
      //Optimize for top-down tracking if on head mounted display.
      if (isHeadMounted)
        leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      else
        leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
    }

    /** Returns the Leap Controller instance. */
    public Controller GetLeapController() {
#if UNITY_EDITOR
      //Do a null check to deal with hot reloading
      if (leap_controller_ == null) {
        leap_controller_ = new Controller();
      }
#endif
      return leap_controller_;
    }
    /** True, if the Leap Motion hardware is plugged in and this application is connected to the Leap Motion service. */
    public bool IsConnected() {
      return GetLeapController().IsConnected;
    }

    /** Returns information describing the device hardware. */
    public LeapDeviceInfo GetDeviceInfo() {
      if (overrideDeviceType) {
        return new LeapDeviceInfo(overrideDeviceTypeWith);
      }

      DeviceList devices = GetLeapController().Devices;
      if (devices.Count == 1) {
        LeapDeviceInfo info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
        // TODO: DeviceList does not tell us the device type. Dragonfly serial starts with "LE" and peripheral starts with "LP"
        if (devices[0].SerialNumber.Length >= 2) {
          switch (devices[0].SerialNumber.Substring(0, 2)) {
            case ("LP"):
              info = new LeapDeviceInfo(LeapDeviceType.Peripheral);
              break;
            case ("LE"):
              info = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
              break;
            default:
              break;
          }
        }

        // TODO: Add baseline & offset when included in API
        // NOTE: Alternative is to use device type since all parameters are invariant
        info.isEmbedded = devices[0].IsEmbedded;
        info.horizontalViewAngle = devices[0].HorizontalViewAngle * Mathf.Rad2Deg;
        info.verticalViewAngle = devices[0].VerticalViewAngle * Mathf.Rad2Deg;
        info.trackingRange = devices[0].Range / 1000f;
        info.serialID = devices[0].SerialNumber;
        return info;
      } else if (devices.Count > 1) {
        return new LeapDeviceInfo(LeapDeviceType.Peripheral);
      }
      return new LeapDeviceInfo(LeapDeviceType.Invalid);
    }

    // Update is called once per frame
    void Update() {

      leapMat = UnityMatrixExtension.GetLeapMatrix(this.transform);

#if CHECK_TRANSFORMED_IMPL
      IFrame frame = leap_controller_.Frame(); // Fetch this only once, otherwise may change between calls
      IFrame previousFrame = leap_controller_.Frame(1); // May actually be Frame(2), but it's ok
      
      _currentFrame.Set(ref leapMat, frame);

      Asserter.CompareAllValues(
          frame.TransformedCopy(ref leapMat), 
          previousFrame.TransformedCopy(ref leapMat), 
          _currentFrame, // == frame.TransformedShallowCopy(ref leapMat)
          previousFrame.TransformedShallowCopy(ref leapMat));
#else
      _currentFrame.Set(ref leapMat, leap_controller_.Frame());
#endif

      //perFrameFixedUpdateOffset_ contains the maximum offset of this Update cycle
      smoothedFixedUpdateOffset_.Update(PerFrameFixedUpdateOffset, Time.deltaTime);
      //float now = leap_controller_.Now();
      //Debug.Log("leap_controller_.Now():" + leap_controller_.Now() + " - CurrentFrame.Timestamp:" + CurrentFrame.Timestamp + " = " + (leap_controller_.Now() - CurrentFrame.Timestamp));
      //Debug.Log("provider.Update().CurrentFrame.Id: " + CurrentFrame.Id);
    }

    void FixedUpdate() {
      //which frame to deliver
    }
    public virtual IFrame GetFixedFrame() {

      //Aproximate the correct timestamp given the current fixed time
      float correctedTimestamp = (Time.fixedTime + smoothedFixedUpdateOffset_.value) * S_TO_NS;

      //Search the leap history for a frame with a timestamp closest to the corrected timestamp
      IFrame closestFrame = leap_controller_.Frame();
      for (int searchHistoryIndex = 0; searchHistoryIndex < 60; searchHistoryIndex++) {

        IFrame historyFrame = leap_controller_.Frame(searchHistoryIndex);//GetTransformedFrame(leapMat, searchHistoryIndex);

        //If we reach an invalid frame, terminate the search
        if (historyFrame.Id < 0) {
          Debug.Log("historyFrame.Id is less than 0");
          break;
        }

        if (Mathf.Abs(historyFrame.Timestamp - correctedTimestamp) < Mathf.Abs(closestFrame.Timestamp - correctedTimestamp)) {
          closestFrame = historyFrame;
        } else {
          //Since frames are always reported in order, we can terminate the search once we stop finding a closer frame
          break;
        }
      }

      leapMat = UnityMatrixExtension.GetLeapMatrix(this.transform);
      _currentFixedFrame.Set(ref leapMat, closestFrame);
      return _currentFixedFrame;
    }
    void OnDestroy() {
      //DestroyAllHands();
      leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
      leap_controller_.StopConnection();
    }
    void OnApplicationPause(bool isPaused) {
      //Debug.Log("Pause " + isPaused);
      if (isPaused)
        leap_controller_.StopConnection();
      else
        leap_controller_.StartConnection();
    }

    void OnApplicationQuit() {
      leap_controller_.StopConnection();
    }
  }
}
