using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Encoding;
using OculusSampleFramework;

namespace Leap.Unity {

    /// <Summary>
    /// A Leap provider that transforms hand data from Quest hand tracking to a Leap hand.
    /// This allows to use Quest hand tracking with interactions built using the Leap Motion Unity Modules.
    /// </Summary>
  public class OVRLeapProvider : LeapProvider {
    protected Frame _updateFrame = new Frame();
    protected Frame _fixedFrame = new Frame();

    private VectorHand _leftVHand = new VectorHand();
    private VectorHand _rightVHand = new VectorHand();

    private Hand _leftHand = new Hand();
    private Hand _rightHand = new Hand();
    private List<Hand> _hands = new List<Hand>();

    private void OnEnable() {
      Application.onBeforeRender += Application_onBeforeRender;
    }

    private void OnDisable() {
      Application.onBeforeRender -= Application_onBeforeRender;
    }

    private void Application_onBeforeRender() {
      // LeapServiceProvider would usually do interpolation here
      FillLeapFrame(-1f, _updateFrame);
      DispatchUpdateFrameEvent(_updateFrame);
    }
  
    void FixedUpdate() {
      // no interpolation here either
      FillLeapFrame(-1f, _fixedFrame);
      DispatchFixedFrameEvent(_fixedFrame);
    }

    /// <Summary>
    /// Popuates the given Leap Frame with the most recent hand data
    /// </Summary>
    public void FillLeapFrame(float frameTime, Frame leapFrame) {
      _hands.Clear();
      if (FillLeapHandFromOvrHand(isLeft: true, frameTime, ref _leftVHand)) {
        _leftVHand.Decode(_leftHand);
        _hands.Add(_leftHand);
      }
      
      if (FillLeapHandFromOvrHand(isLeft: false, frameTime, ref _rightVHand)) {
        _rightVHand.Decode(_rightHand);
        _hands.Add(_rightHand);
      }
      
      leapFrame.Hands = _hands;
    }

    /// <Summary>
    /// Read the most recent hand data from the Oculus hand tracking API and populate a given VectorHand with joint locations
    /// </Summary>
    private bool FillLeapHandFromOvrHand(bool isLeft, float frameTime, ref VectorHand vHand) {
      OVRHand ovrHand = isLeft ? HandsManager.Instance.LeftHand : HandsManager.Instance.RightHand;
      OVRSkeleton ovrSkeleton = isLeft ? HandsManager.Instance.LeftHandSkeleton : HandsManager.Instance.RightHandSkeleton;
      bool isTracked = ovrHand.IsDataHighConfidence;
      IList<OVRBone> bones = ovrSkeleton.Bones;
      
      if (bones.Count != 0 && isTracked) {
        vHand.isLeft = isLeft;
        
        // oddly, Oculus' coordinate systems differ for right/left hand 
        Vector3 rotation = isLeft ? new Vector3(0f, -90f, -180f) : new Vector3(0f, 90f, 0f);
        Vector3 wristOffset = isLeft ? new Vector3(-0.07f, 0f, 0f) : new Vector3(0.07f, 0f, 0f);
        
        // Convert palm position/rotation to Leap coordinate system
        Transform ovrPalm = bones[(int)OVRSkeleton.BoneId.Hand_WristRoot].Transform;
        vHand.palmPos = ovrPalm.position + ovrPalm.TransformVector(wristOffset);
        vHand.palmRot = ovrPalm.rotation * Quaternion.Euler(rotation);
        
        // need to infer carpal bone positions since Oculus doesn't provide them
        Vector3 metaCarpalOffset = isLeft ? new Vector3(0f, 0f, 0.008f) : new Vector3(0f, 0f, -0.008f);
        Vector3 indexMetacarpalOffset   = ovrPalm.TransformVector(metaCarpalOffset    );
        Vector3 middleMetacarpalOffset  = ovrPalm.TransformVector(metaCarpalOffset * 2);
        Vector3 ringMetacarpalOffset    = ovrPalm.TransformVector(metaCarpalOffset * 3);
        Vector3 pinkyMetacarpalOffset   = ovrPalm.TransformVector(metaCarpalOffset * 4);
        
        vHand.jointPositions[ 0] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb0   ].Transform.position                           , vHand.palmPos, vHand.palmRot);   
        vHand.jointPositions[ 1] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb1   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 2] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb2   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 3] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb3   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 4] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_ThumbTip ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 5] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb0   ].Transform.position + indexMetacarpalOffset   , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 6] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Index1   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 7] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Index2   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 8] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Index3   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[ 9] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_IndexTip ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[10] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb0   ].Transform.position + middleMetacarpalOffset  , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[11] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Middle1  ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[12] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Middle2  ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[13] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Middle3  ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[14] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_MiddleTip].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[15] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb0   ].Transform.position + ringMetacarpalOffset    , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[16] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Ring1    ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[17] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Ring2    ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[18] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Ring3    ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[19] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_RingTip  ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[20] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Thumb0   ].Transform.position + pinkyMetacarpalOffset   , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[21] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Pinky1   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[22] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Pinky2   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[23] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_Pinky3   ].Transform.position                           , vHand.palmPos, vHand.palmRot);
        vHand.jointPositions[24] = VectorHand.ToLocal( bones[ (int) OVRSkeleton.BoneId.Hand_PinkyTip ].Transform.position                           , vHand.palmPos, vHand.palmRot);
      
        return true;
      }
      else {
        return false;
      }
    }

    public override Frame CurrentFrame {
      get {
        return _updateFrame;
      }
    }

    public override Frame CurrentFixedFrame {
      get {
        return _fixedFrame;
      }
    }
  }
}