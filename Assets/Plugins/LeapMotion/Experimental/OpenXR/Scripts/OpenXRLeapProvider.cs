/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Encoding;
using Microsoft.MixedReality.OpenXR.Preview;

namespace Leap.Unity
{
/// <Summary>
/// This provider sources hand data from OpenXR via the Microsoft Mixed Reality OpenXR Plugin for use with Leap UnityModules
/// </Summary>

public class OpenXRLeapProvider : LeapProvider
{
  private Frame _updateFrame = new Frame();
  private Frame _beforeRenderFrame = new Frame();
  private Frame _fixedUpdateFrame = new Frame();

  private VectorHand _leftVHand = new VectorHand();
  private VectorHand _rightVHand = new VectorHand();

  private Hand _leftHand  = new Hand();
  private Hand _rightHand  = new Hand(); 
  private List<Hand> _hands = new List<Hand>();
  private HandJointLocation[] handJointLocations = new HandJointLocation[HandTracker.JointCount];

  private static readonly HandTracker leftHandTracker = new HandTracker(Handedness.Left);
  private static readonly HandTracker rightHandTracker = new HandTracker(Handedness.Right);

  private void OnEnable()
  {
      Application.onBeforeRender += Application_onBeforeRender;
  }

  private void OnDisable()
  {
      Application.onBeforeRender -= Application_onBeforeRender;
  }

  private void Application_onBeforeRender()
  {
    // Dispatch the frame event just brefore rendering to reduce jitter
    FillLeapFrame(Microsoft.MixedReality.OpenXR.FrameTime.OnBeforeRender, ref _beforeRenderFrame);
    DispatchUpdateFrameEvent(_beforeRenderFrame); 
  }

  void FixedUpdate() {
    FillLeapFrame(Microsoft.MixedReality.OpenXR.FrameTime.OnUpdate, ref _fixedUpdateFrame);
    DispatchFixedFrameEvent(_fixedUpdateFrame); 
  }

  #region LeapProvider 
  
  /// <Summary>
  /// Popuates the given Leap Frame with the most recent hand data
  /// </Summary>
  public void FillLeapFrame(Microsoft.MixedReality.OpenXR.FrameTime frameTime, ref Frame leapFrame)
  {   
      _hands.Clear();
      if (FillLeapHandFromExtension(leftHandTracker, true, frameTime, ref _leftVHand))
      {
          _leftVHand.Decode(_leftHand);
          _hands.Add(_leftHand);
      }
      if (FillLeapHandFromExtension(rightHandTracker, false, frameTime, ref _rightVHand))
      {
          _rightVHand.Decode(_rightHand);
          _hands.Add(_rightHand);
      }

      leapFrame.Hands = _hands;
  }
  
  /// <Summary>
  /// Read the most recent hand data from the OpenXR extension and populate a given VectorHand with joint locations
  /// </Summary>
  private bool FillLeapHandFromExtension(HandTracker handTracker, bool isLeft, Microsoft.MixedReality.OpenXR.FrameTime frameTime, ref VectorHand vHand)
  {
      if (handTracker.TryLocateHandJoints(frameTime, handJointLocations))
      {
          vHand.isLeft = isLeft;

          //Fill the vHand with joint data
          vHand.palmPos = handJointLocations[0].Position;
          vHand.palmRot = handJointLocations[0].Rotation;

          Vector3 localJoint = VectorHand.ToLocal(handJointLocations[2].Position, vHand.palmPos, vHand.palmRot);
          vHand.jointPositions[0] = localJoint;

          for(int j = 0; j < vHand.jointPositions.Length - 1; j++)
          {
              localJoint = VectorHand.ToLocal(handJointLocations[j+2].Position, vHand.palmPos, vHand.palmRot);
              vHand.jointPositions[j+1] = localJoint;
          }

          // 3. Move Hands from relative to the provider
          vHand.palmPos = transform.position + vHand.palmPos;

          return true;
      }
      else
      {
          return false;
      }
  }

  #endregion

  #region Frame Utilities

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
      return _updateFrame;
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
      return _updateFrame;
    }
  }
    

    
  protected virtual void transformFrame(Frame source, Frame dest) {
          dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
  }

  #endregion

  #region Editor Pose Implementation

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
      if (_cachedLeftHands.TryGetValue(editTimePose, out Hand cachedHand)) {
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
      if (_cachedRightHands.TryGetValue(editTimePose, out Hand cachedHand)) {
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
}
}
