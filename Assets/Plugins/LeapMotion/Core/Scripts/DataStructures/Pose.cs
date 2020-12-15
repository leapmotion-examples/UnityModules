/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using Leap.Unity.Encoding;
using Leap.Unity.Splines;

namespace Leap.Unity {

  /// <summary>
  /// A position and rotation. You can multiply two poses; this acts like Matrix4x4
  /// multiplication, but Poses always have unit scale.
  /// </summary>
  [System.Serializable]
  public struct Pose : IEquatable<Pose>, IInterpolable<Pose> {

    public Vector3    position;
    public Quaternion rotation;

    public Pose(Vector3 position)
      : this(position, Quaternion.identity) { }
    public Pose(Quaternion rotation)
      : this(Vector3.zero, rotation) { }
    public Pose(Vector3 position, Quaternion rotation) {
      this.position = position;
      this.rotation = rotation;
    }

    public static readonly Pose identity = new Pose(Vector3.zero, Quaternion.identity);

    public Pose inverse {
      get {
        Quaternion invQ = Quaternion.Inverse(this.rotation);
        return new Pose(invQ * -this.position, invQ.ToNormalized()); // Normalize
      }
    }

    /// <summary>
    /// Returns a Matrix4x4 corresponding to this pose's translation and
    /// rotation, with unit scale.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        this.rotation = this.rotation.ToNormalized(); // Normalize
        return Matrix4x4.TRS(this.position, this.rotation, Vector3.one);
      }
    }

    /// <summary>
    /// Returns Pose B transformed by Pose A, like a transform hierarchy with A as the
    /// parent of B.
    /// </summary>
    public static Pose operator *(Pose A, Pose B) {
      return new Pose(A.position + (A.rotation * B.position),
                      A.rotation * B.rotation);
    }

    /// <summary>
    /// Returns the accumulation of the two poses: The positions summed, and with
    /// rotation A.rotation * B.rotation. Note that this accumulates the poses without
    /// interpreting either pose as a parent space of the other; but also beware that
    /// rotations are noncommutative, so this operation is also noncommutative.
    /// </summary>
    public static Pose operator +(Pose A, Pose B) {
      return new Pose(A.position + B.position,
                      A.rotation * B.rotation);
    }

    /// <summary>
    /// Transforms the right-hand-side Vector3 as a local-space position into
    /// world space as if this Pose were its reference frame or parent.
    /// </summary>
    public static Pose operator *(Pose pose, Vector3 localPosition) {
      return new Pose(pose.position + pose.rotation * localPosition,
                      pose.rotation);
    }

    public static Pose operator *(Pose pose, Quaternion localRotation) {
      return pose * new Pose(Vector3.zero, localRotation);
    }

    public static Pose operator *(Quaternion parentRotation, Pose localPose) {
      return new Pose(Vector3.zero, parentRotation) * localPose;
    }

    /// <summary> Non-projective matrices only (MultiplyPoint3x4). </summary>
    public static Pose operator *(Matrix4x4 matrix, Pose localPose) {
      return new Pose(matrix.MultiplyPoint3x4(localPose.position),
        matrix.rotation * localPose.rotation);
    }

    public bool ApproxEquals(Pose other) {
      return position.ApproxEquals(other.position) && rotation.ApproxEquals(other.rotation);
    }

    /// <summary>
    /// Returns a pose interpolated (Lerp for position, Slerp, NOT Lerp for rotation)
    /// between a and b by t from 0 to 1. This method clamps t between 0 and 1; if
    /// extrapolation is desired, see Extrapolate.
    /// </summary>
    public static Pose Lerp(Pose a, Pose b, float t) {
      if (t >= 1f) return b;
      if (t <= 0f) return a;
      return new Pose(Vector3.Lerp(a.position, b.position, t),
                      Quaternion.Lerp(Quaternion.Slerp(a.rotation, b.rotation, t), Quaternion.identity, 0f));
    }

    /// <summary>
    /// As Lerp, but doesn't clamp t between 0 and 1. Values above one extrapolate
    /// forwards beyond b, while values less than zero extrapolate backwards past a.
    /// </summary>
    public static Pose LerpUnclamped(Pose a, Pose b, float t) {
      return new Pose(Vector3.LerpUnclamped(a.position, b.position, t),
                      Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));
    }

    /// <summary>
    /// As LerpUnclamped, but extrapolates using time values for a and b, and a target
    /// time at which to determine the extrapolated pose.
    /// </summary>
    public static Pose LerpUnclampedTimed(Pose a, float aTime,
                                          Pose b, float bTime,
                                          float extrapolateTime) {
      return LerpUnclamped(a, b, extrapolateTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    public override string ToString() {
      return "[Pose | Position: " + this.position.ToString()
           + ", Rotation: " + this.rotation.ToString() + "]";
    }

    public string ToString(string format) {
      return "[Pose | Position: " + this.position.ToString(format)
           + ", Rotation: " + this.rotation.ToString(format) + "]";
    }

    public override bool Equals(object obj) {
      if (!(obj is Pose)) return false;
      else return this.Equals((Pose)obj);
    }
    public bool Equals(Pose other) {
      return other.position == this.position && 
             other.rotation == this.rotation;
    }

    public override int GetHashCode() {
      return new Hash() {
        position,
        rotation
      };
    }

    public static bool operator ==(Pose a, Pose b) {
      return a.Equals(b);
    }

    public static bool operator !=(Pose a, Pose b) {
      return !(a.Equals(b));
    }


    // IInterpolable Implementation
    public Pose CopyFrom(Pose h) {
      position = h.position;
      rotation = h.rotation;
      return this;
    }
    public bool FillLerped(Pose a, Pose b, float t) {
      this = LerpUnclamped(a, b, t);
      return true;
    }
    public bool FillSplined(Pose a, Pose b, Pose c, Pose d, float t) {
      position = CatmullRom.ToCHS(a.position, b.position, c.position, d.position,
        centripetal: false).PositionAt(t);
      rotation = Quaternion.SlerpUnclamped(b.rotation, c.rotation, t);
      return true;
    }

  }

  public static class PoseExtensions {

    /// <summary>
    /// Creates a Pose using the transform's localPosition and localRotation.
    /// </summary>
    public static Pose ToLocalPose(this Transform t) {
      return new Pose(t.localPosition, t.localRotation);
    }

    /// <summary>
    /// Creates a Pose using the transform's position and rotation.
    /// </summary>
    public static Pose ToPose(this Transform t) {
      return new Pose(t.position, t.rotation);
    }

    /// <summary>
    /// Creates a Pose using the transform's position and rotation.
    /// </summary>
    public static Pose GetPose(this Transform t) {
      return new Pose(t.position, t.rotation);
    }

    /// <summary>
    /// Creates a Pose using the transform's position and rotation.
    /// </summary>
    public static Pose ToWorldPose(this Transform t) {
      return t.ToPose();
    }

    /// <summary>
    /// Sets the localPosition and localRotation of this transform to the argument pose's
    /// position and rotation.
    /// </summary>
    public static void SetLocalPose(this Transform t, Pose localPose) {
      t.localPosition = localPose.position;
      t.localRotation = localPose.rotation;
    }
    /// <summary>
    /// Sets the position and rotation of this transform to the argument pose's
    /// position and rotation. Identical to SetWorldPose.
    /// </summary>
    public static void SetPose(this Transform t, Pose worldPose) {
      t.position = worldPose.position;
      t.rotation = worldPose.rotation;
    }

    /// <summary>
    /// Sets the position and rotation of this transform to the argument pose's
    /// position and rotation. Identical to SetPose.
    /// </summary>
    public static void SetWorldPose(this Transform t, Pose worldPose) {
      t.SetPose(worldPose);
    }

    /// <summary>
    /// Returns the pose (position and rotation) described by a Matrix4x4.
    /// </summary>
    public static Pose GetPose(this Matrix4x4 m) {
      return new Pose(position: m.MultiplyPoint3x4(Vector3.zero),
        rotation: m.GetQuaternion());
      // return new Pose(m.GetColumn(3),
      //                 m.GetColumn(2) == m.GetColumn(1) ? Quaternion.identity
      //                                                  : Quaternion.LookRotation(
      //                                                      m.GetColumn(2),
      //                                                      m.GetColumn(1)));
    }

    /// <summary>
    /// Returns a new Pose with the argument rotation instead of the Pose's current
    /// rotation.
    /// </summary>
    public static Pose WithRotation(this Pose pose, Quaternion newRotation) {
      return new Pose(pose.position, newRotation);
    }

    /// <summary>
    /// Returns a new Pose with the argument position instead of the Pose's current
    /// position.
    /// </summary>
    public static Pose WithPosition(this Pose pose, Vector3 newPosition) {
      return new Pose(newPosition, pose.rotation);
    }

    public const float EPSILON = 0.0001f;

    public static bool ApproxEquals(this Vector3 v0, Vector3 v1) {
      return (v0 - v1).magnitude < EPSILON;
    }

    public static bool ApproxEquals(this Quaternion q0, Quaternion q1) {
      return (q0.ToAngleAxisVector() - q1.ToAngleAxisVector()).magnitude < EPSILON;
    }

  }

}
