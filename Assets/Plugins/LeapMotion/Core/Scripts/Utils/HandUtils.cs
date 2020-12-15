/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// Static convenience methods and extension methods for getting useful Hand data.
  /// </summary>
  public static class Hands {

    private static LeapProvider s_provider;
    private static GameObject s_leapRig;

    static Hands() {
      InitStatic();
      SceneManager.activeSceneChanged += InitStaticOnNewScene;
    }

    private static void InitStaticOnNewScene(Scene unused, Scene unused2) {
      InitStatic();
    }

    private static void InitStatic() {
      s_provider = Object.FindObjectOfType<LeapServiceProvider>();
      if (s_provider == null) {
        s_provider = Object.FindObjectOfType<LeapProvider>();
        if (s_provider == null) {
          return;
        }
      }

      Camera providerCamera = s_provider.GetComponentInParent<Camera>();
      if (providerCamera == null) return;
      if (providerCamera.transform.parent == null) return;
      s_leapRig = providerCamera.transform.parent.gameObject;
    }

    /// <summary>
    /// Static convenience accessor for the Leap camera rig. This is the parent
    /// of the Camera that contains a LeapProvider in one of its children,
    /// or null if there is no such GameObject.
    /// </summary>
    public static GameObject CameraRig {
      get {
        if (s_leapRig == null) {
          InitStatic();
        }
        return s_leapRig;
      }
    }

    /// <summary>
    /// Static convenience accessor for a LeapProvider in the scene. Preference is given
    /// to a LeapServiceProvider if there is one.
    /// 
    /// If static memory currently has no reference for the provider (or if it was
    /// destroyed), this call will search the scene for a LeapProvider and cache it to be
    /// returned next time.
    /// 
    /// If there is no LeapProvider in your scene, this getter
    /// will return null. Be warned that calling this regularly can be expensive if
    /// LeapProviders often don't exist in your scene or are frequently destroyed.
    /// </summary>
    public static LeapProvider Provider {
      get {
        if (s_provider == null) {
          InitStatic();
        }
        return s_provider;
      }
      set { s_provider = value; }
    }

    /// <summary>
    /// Returns the first hand of the argument Chirality in the current frame,
    /// otherwise returns null if no such hand is found.
    /// </summary>
    public static Hand Get(Chirality chirality) {
      if (chirality == Chirality.Left) return Left;
      else return Right;
    }

    /// <summary>
    /// As Get, but returns the FixedUpdate (physics timestep) hand as opposed to the Update hand.
    /// </summary>
    public static Hand GetFixed(Chirality chirality) {
      if (chirality == Chirality.Left) return FixedLeft;
      else return FixedRight;
    }

    /// <summary>
    /// Returns the first left hand found by Leap in the current frame, otherwise
    /// returns null if no such hand is found.
    /// </summary>
    public static Hand Left {
      get {
        if (Provider == null) return null;
        if (Provider.CurrentFrame == null) return null;
        return Provider.CurrentFrame.Hands.Query().FirstOrDefault(hand => hand.IsLeft);
      }
    }

    /// <summary>
    /// Returns the first right hand found by Leap in the current frame, otherwise
    /// returns null if no such hand is found.
    /// </summary>
    public static Hand Right {
      get {
        if (Provider == null) return null;
        if (Provider.CurrentFrame == null) return null;
        else return Provider.CurrentFrame.Hands.Query().FirstOrDefault(hand => hand.IsRight);
      }
    }

    /// <summary>
    /// Returns the first left hand found by Leap in the current fixed frame, otherwise
    /// returns null if no such hand is found. The fixed frame is aligned with the physics timestep.
    /// </summary>
    public static Hand FixedLeft {
      get {
        if (Provider == null) return null;
        if (Provider.CurrentFixedFrame == null) return null;
        return Provider.CurrentFixedFrame.Hands.Query().FirstOrDefault(hand => hand.IsLeft);
      }
    }

    /// <summary> 
    /// Returns the first right hand found by Leap in the current fixed frame, otherwise
    /// returns null if no such hand is found. The fixed frame is aligned with the physics timestep.
    /// </summary>
    public static Hand FixedRight {
      get {
        if (Provider == null) return null;
        if (Provider.CurrentFixedFrame == null) return null;
        else return Provider.CurrentFixedFrame.Hands.Query().FirstOrDefault(hand => hand.IsRight);
      }
    }

    /// Shorthand for hand.Fingers[(int)Leap.Finger.FingerType.TYPE_THUMB],
    /// or, alternatively, hand.Fingers[0].
    /// </summary>
    public static Finger GetThumb(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];
    }

    /// <summary>
    /// Shorthand for hand.Fingers[(int)Leap.Finger.FingerType.TYPE_INDEX],
    /// or, alternatively, hand.Fingers[1].
    /// </summary>
    public static Finger GetIndex(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
    }

    /// <summary>
    /// Shorthand for hand.Fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE],
    /// or, alternatively, hand.Fingers[2].
    /// </summary>
    public static Finger GetMiddle(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
    }

    /// <summary>
    /// Shorthand for hand.Fingers[(int)Leap.Finger.FingerType.TYPE_RING],
    /// or, alternatively, hand.Fingers[3].
    /// </summary>
    public static Finger GetRing(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_RING];
    }

    /// <summary>
    /// Shorthand for hand.Fingers[(int)Leap.Finger.FingerType.TYPE_PINKY],
    /// or, alternatively, hand.Fingers[4].
    /// </summary>
    public static Finger GetPinky(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_PINKY];
    }

    /// <summary>
    /// Returns a Pose consisting of the tracked hand's palm position and rotation.
    /// </summary>
    public static Pose GetPalmPose(this Hand hand) {
      return new Pose(hand.PalmPosition.ToVector3(), hand.Rotation.ToQuaternion());
    }

    /// <summary>
    /// As Hand.SetTransform(), but takes a Pose as input for convenience.
    /// </summary>
    public static void SetPalmPose(this Hand hand, Pose newPalmPose) {
      hand.SetTransform(newPalmPose.position, newPalmPose.rotation);
    }

    /// <summary>
    /// Returns the direction the Hand's palm is facing. For the  other two palm-basis
    /// directions, see RadialAxis and DistalAxis.
    /// 
    /// The direction out of the back of the hand would be called the dorsal axis.
    /// </summary>
    public static Vector3 PalmarAxis(this Hand hand) {
      return -hand.Basis.yBasis.ToVector3();
    }

    /// <summary>
    /// Returns the the direction towards the thumb that is perpendicular to the palmar
    /// and distal axes. Left and right hands will return opposing directions.
    /// 
    /// The direction away from the thumb would be called the ulnar axis.
    /// </summary>
    public static Vector3 RadialAxis(this Hand hand) {
      if (hand.IsRight) {
        return -hand.Basis.xBasis.ToVector3();
      }
      else {
        return hand.Basis.xBasis.ToVector3();
      }
    }

    /// <summary>
    /// Returns the direction towards the fingers that is perpendicular to the palmar
    /// and radial axes.
    /// 
    /// The direction towards the wrist would be called the proximal axis.
    /// </summary>
    public static Vector3 DistalAxis (this Hand hand) {
      return hand.Basis.zBasis.ToVector3();
    }

    /// <summary>
    /// Returns whether the pinch strength for the hand is greater than 0.8.
    /// For more reliable pinch behavior, try applying hysteresis to the PinchStrength property.
    /// </summary>
    public static bool IsPinching(this Hand hand) {
      return hand.PinchStrength > 0.8F;
    }

    /// <summary>
    /// Returns approximately where the thumb and index finger will be if they are pinched together.
    /// </summary>
    public static Vector3 GetPinchPosition(this Hand hand) {
      Vector indexPosition = hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].TipPosition;
      Vector thumbPosition = hand.Fingers[(int)Finger.FingerType.TYPE_THUMB].TipPosition;
      return (2 * thumbPosition + indexPosition).ToVector3() * 0.333333F;
    }

    /// <summary>
    /// Returns a decent approximation of where the hand is pinching, or where it will pinch,
    /// even if the index and thumb tips are far apart.
    /// 
    /// In general, this will be more stable than GetPinchPosition().
    /// </summary>
    public static Vector3 GetPredictedPinchPosition(this Hand hand) {
      Vector3 indexTip = hand.GetIndex().TipPosition.ToVector3();
      Vector3 thumbTip = hand.GetThumb().TipPosition.ToVector3();

      // The predicted pinch point is a rigid point in hand-space linearly offset by the
      // index finger knuckle position, scaled by the index finger's length, and lightly
      // influenced by the actual thumb and index tip positions.
      Vector3 indexKnuckle = hand.Fingers[1].bones[1].PrevJoint.ToVector3();
      float indexLength = hand.Fingers[1].Length;
      Vector3 radialAxis = hand.RadialAxis();
      float thumbInfluence = Vector3.Dot((thumbTip - indexKnuckle).normalized, radialAxis).Map(0F, 1F, 0.5F, 0F);
      Vector3 predictedPinchPoint = indexKnuckle + hand.PalmarAxis() * indexLength * 0.85F
                                                 + hand.DistalAxis() * indexLength * 0.20F
                                                 + radialAxis        * indexLength * 0.20F;
      predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, thumbTip, thumbInfluence);
      predictedPinchPoint = Vector3.Lerp(predictedPinchPoint, indexTip, 0.15F);

      return predictedPinchPoint;
    }

    /// <summary>
    /// Returns whether this vector faces from a given world position towards another world position within a maximum angle of error.
    /// </summary>
    public static bool IsFacing(this Vector3 facingVector, Vector3 fromWorldPosition, Vector3 towardsWorldPosition, float maxOffsetAngleAllowed) {
      Vector3 actualVectorTowardsWorldPosition = (towardsWorldPosition - fromWorldPosition).normalized;
      return Vector3.Angle(facingVector, actualVectorTowardsWorldPosition) <= maxOffsetAngleAllowed;
    }

    /// <summary>
    /// Returns a confidence value from 0 to 1 indicating how strongly the Hand is making a fist.
    /// </summary>
    public static float GetFistStrength(this Hand hand) {
      return (Vector3.Dot(hand.Fingers[1].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[2].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[3].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[4].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[0].Direction.ToVector3(), -hand.RadialAxis() )
            ).Map(-5, 5, 0, 1);
    }

    /// <summary>
    /// Returns an unsmoothed ray representing the general reaching/interaction intent direction.
    /// </summary>
    public static Ray HandRay(this Hand hand, Transform headTransform) {
      Quaternion shoulderYaw      = Quaternion.Euler(0f, headTransform.rotation.eulerAngles.y, 0f);
      // Approximate shoulder position with magic values.
      Vector3 ProjectionOrigin = headTransform.position
                                  + (shoulderYaw * (new Vector3(0f, -0.13f, -0.1f)
                                  + Vector3.left * 0.15f * (hand.IsLeft ? 1f : -1f)));
      // Compare against this
      //Vector3 ProjectionOrigin    = headTransform.position + shoulderYaw * 
      //                                new Vector3(0.15f * (hand.IsLeft ? -1f : 1f), -0.13f, 0.05f);
      Vector3 ProjectionDirection = hand.Fingers[1].bones[0].NextJoint.ToVector3() - ProjectionOrigin;
      return new Ray(ProjectionOrigin, ProjectionDirection);
    }

    /// <summary>
    /// Transforms a bone by a position and rotation.
    /// </summary>
    public static void Transform(this Bone bone, Vector3 position, Quaternion rotation) {
      bone.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a finger by a position and rotation.
    /// </summary>
    public static void Transform(this Finger finger, Vector3 position, Quaternion rotation) {
      finger.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a hand by a position and rotation.
    /// </summary>
    public static void Transform(this Hand hand, Vector3 position, Quaternion rotation) {
      hand.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a frame by a position and rotation.
    /// </summary>
    public static void Transform(this Frame frame, Vector3 position, Quaternion rotation) {
      frame.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a bone to a position and rotation.
    /// </summary>
    public static void SetTransform(this Bone bone, Vector3 position, Quaternion rotation) {
      bone.Transform(Vector3.zero, (rotation * Quaternion.Inverse(bone.Rotation.ToQuaternion())));
      bone.Transform(position - bone.PrevJoint.ToVector3(), Quaternion.identity);
    }

    /// <summary>
    /// Transforms a finger to a position and rotation by its fingertip.
    /// </summary>
    public static void SetTipTransform(this Finger finger, Vector3 position, Quaternion rotation) {
      finger.Transform(Vector3.zero, (rotation * Quaternion.Inverse(finger.bones[3].Rotation.ToQuaternion())));
      finger.Transform(position - finger.bones[3].NextJoint.ToVector3(), Quaternion.identity);
    }

    /// <summary>
    /// Transforms a hand to a position and rotation.
    /// </summary>
    public static void SetTransform(this Hand hand, Vector3 position, Quaternion rotation) {
      hand.Transform(Vector3.zero, Quaternion.Slerp((rotation * Quaternion.Inverse(hand.Rotation.ToQuaternion())), Quaternion.identity, 0f));
      hand.Transform(position - hand.PalmPosition.ToVector3(), Quaternion.identity);
    }

  }

  /// <summary>
  /// Utility methods for constructing and manipulating Leap hand object data.
  /// </summary>
  public static class HandUtils {

    /// <summary>
    /// Fills the Hand object with the provided hand data. You can pass null for the
    /// fingers input; this will leave the hand's finger data unmodified.
    /// </summary>
    public static void Fill(this Hand toFill,
                            long frameID,
                            int id,
                            float confidence,
                            float grabStrength,
                            float grabAngle,
                            float pinchStrength,
                            float pinchDistance,
                            float palmWidth,
                            bool isLeft,
                            float timeVisible,
                            /* Arm arm,*/
                            List<Finger> fingers,
                            Vector palmPosition,
                            Vector stabilizedPalmPosition,
                            Vector palmVelocity,
                            Vector palmNormal,
                            LeapQuaternion rotation,
                            Vector direction,
                            Vector wristPosition) {
      toFill.FrameId                      = frameID;
      toFill.Id                           = id;
      toFill.Confidence                   = confidence;
      toFill.GrabStrength                 = grabStrength;
      toFill.GrabAngle                    = grabAngle;
      toFill.PinchStrength                = pinchStrength;
      toFill.PinchDistance                = pinchDistance;
      toFill.PalmWidth                    = palmWidth;
      toFill.IsLeft                       = isLeft;
      toFill.TimeVisible                  = timeVisible;
      if (fingers != null) toFill.Fingers = fingers;
      toFill.PalmPosition                 = palmPosition;
      toFill.StabilizedPalmPosition       = stabilizedPalmPosition;
      toFill.PalmVelocity                 = palmVelocity;
      toFill.PalmNormal                   = palmNormal;
      toFill.Rotation                     = rotation;
      toFill.Direction                    = direction;
      toFill.WristPosition                = wristPosition;
    }

    /// <summary>
    /// Fills the Bone object with the provided bone data.
    /// </summary>
    public static void Fill(this Bone toFill,
                            Vector prevJoint,
                            Vector nextJoint,
                            Vector center,
                            Vector direction,
                            float length,
                            float width,
                            Bone.BoneType type,
                            LeapQuaternion rotation) {
      toFill.PrevJoint  = prevJoint;
      toFill.NextJoint  = nextJoint;
      toFill.Center     = center;
      toFill.Direction  = direction;
      toFill.Length     = length;
      toFill.Width      = width;
      toFill.Type       = type;
      toFill.Rotation   = rotation;
    }

    /// <summary>
    /// Fills the Finger object with the provided finger data. You can pass null for
    /// bones; A null bone will not modify the underlying hand's data for that bone.
    /// </summary>
    public static void Fill(this Finger toFill,
                            long frameId,
                            int handId,
                            int fingerId,
                            float timeVisible,
                            Vector tipPosition,
                            Vector direction,
                            float width,
                            float length,
                            bool isExtended,
                            Finger.FingerType type,
                            Bone metacarpal   = null,
                            Bone proximal     = null,
                            Bone intermediate = null,
                            Bone distal       = null) {
      toFill.Id                     = handId;
      toFill.HandId                 = handId;
      toFill.TimeVisible            = timeVisible;
      toFill.TipPosition            = tipPosition;
      toFill.Direction              = direction;
      toFill.Width                  = width;
      toFill.Length                 = length;
      toFill.IsExtended             = isExtended;
      toFill.Type                   = type;

      if (metacarpal   != null) toFill.bones[0] = metacarpal;
      if (proximal     != null) toFill.bones[1] = proximal;
      if (intermediate != null) toFill.bones[2] = intermediate;
      if (distal       != null) toFill.bones[3] = distal;
    }

    /// <summary>
    /// Fills the Arm object with the provided arm data.
    /// </summary>
    public static void Fill(this Arm toFill,
                            Vector elbow,
                            Vector wrist,
                            Vector center,
                            Vector direction,
                            float length,
                            float width,
                            LeapQuaternion rotation) {
      toFill.PrevJoint  = elbow;
      toFill.NextJoint  = wrist;
      toFill.Center     = center;
      toFill.Direction  = direction;
      toFill.Length     = length;
      toFill.Width      = width;
      toFill.Rotation   = rotation;
    }

    /// <summary>
    /// Fills the hand's PalmVelocity data based on the
    /// previous hand object and the provided delta time between the two hands.
    /// </summary>
    public static void FillTemporalData(this Hand toFill,
                                        Hand previousHand, float deltaTime) {
      toFill.PalmVelocity = (toFill.PalmPosition - previousHand.PalmPosition)
                             / deltaTime;
    }
    
    #region Frame Utils

    public static Hand Get(this Frame frame, Chirality whichHand) {
      if (frame.Hands == null) { return null; }
      return frame.Hands.Query().FirstOrDefault(
        h => h.IsLeft == (whichHand == Chirality.Left));
    }

    #endregion

    #region Provider Utils

    public static Hand Get(this LeapProvider provider, Chirality whichHand) {
      Frame frame;
      if (Time.inFixedTimeStep) {
        frame = provider.CurrentFixedFrame;
      }
      else {
        frame = provider.CurrentFrame;
      }

      return frame.Get(whichHand);
    }

    #endregion

  }

}
