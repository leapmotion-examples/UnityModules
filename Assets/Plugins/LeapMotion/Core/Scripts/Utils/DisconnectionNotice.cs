/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity{
  /**
   * Tracks the connection state of the Leap Motion hardware. If the device is unplugged
   * or otherwise not detected, the script fades in a GUITexture object which should communicate
   * the problem to the user.
   *
   * @deprecated Use ConnectionMonitor in new code.
   */
  public class DisconnectionNotice : MonoBehaviour {
  
    /** The speed to fade the object alpha from 0 to 1. */
    public float fadeInTime = 1.0f;
    /** The speed to fade the object alpha from 1 to 0. */
    public float fadeOutTime = 1.0f;
    /** The easing curve. */
    public AnimationCurve fade;
    /** A delay before beginning the fade-in effect. */
    public int waitFrames = 10;
    /** The fully on texture tint color. */
    public Color onColor = Color.white;
  
    private Controller leap_controller_;
    private float fadedIn = 0.0f;
    private int frames_disconnected_ = 0;
    private UnityEngine.UI.Image _image;
  
    void Start() {
      leap_controller_ = new Controller();
      if (_image == null) { _image = GetComponent<UnityEngine.UI.Image>(); }
      SetAlpha(0.0f);
    }
  
    void SetAlpha(float alpha) {
      if (_image != null) { _image.color = Color.Lerp(Color.clear, onColor, alpha); }
    }
  
    /** The connection state of the controller. */
    bool IsConnected() {
      return leap_controller_.IsConnected;
    }
              
    void Update() {
  
      if (IsConnected())
        frames_disconnected_ = 0;
      else
        frames_disconnected_++;
  
      if (frames_disconnected_ < waitFrames)
        fadedIn -= Time.deltaTime / fadeOutTime;
      else
        fadedIn += Time.deltaTime / fadeInTime;
      fadedIn = Mathf.Clamp(fadedIn, 0.0f, 1.0f);
  
      SetAlpha(fade.Evaluate(fadedIn));
    }
  }
}
