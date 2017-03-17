/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  public static class Utils {

    #region Math Utils
    public static int Repeat(int x, int m) {
      int r = x % m;
      return r < 0 ? r + m : r;
    }

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector will have the same length as the
    /// input vector.
    /// </summary>
    public static Vector2 Perpendicular(this Vector2 vector) {
      return new Vector2(vector.y, -vector.x);
    }

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector is not guaranteed to be a unit vector,
    /// nor is its length guaranteed to be the same as the source
    /// vector's.
    /// </summary>
    public static Vector3 Perpendicular(this Vector3 vector) {
      float x2 = vector.x * vector.x;
      float y2 = vector.y * vector.y;
      float z2 = vector.z * vector.z;

      float mag0 = z2 + x2;
      float mag1 = y2 + x2;
      float mag2 = z2 + y2;

      if (mag0 > mag1) {
        if (mag0 > mag2) {
          return new Vector3(-vector.z, 0, vector.x);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      } else {
        if (mag1 > mag2) {
          return new Vector3(vector.y, -vector.x, 0);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      }
    }
    
    #endregion

    #region Orientation Utils

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Useful for billboarding Quads and UI elements whose forward vectors should match
    /// rather than oppose the Main Camera.
    /// </summary>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, Vector3.up);
    }

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Allows specifying an upwards parameter; this is passed as the upwards vector to the Quaternion.LookRotation.
    /// </summary>
    /// <param name="thisTransform"></param>
    /// <param name="transform"></param>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform, Vector3 upwards) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, upwards);
    }

    #endregion

    #region Physics Utils

    public static void IgnoreCollisions(GameObject first, GameObject second, bool ignore = true) {
      if (first == null || second == null)
        return;

      Collider[] first_colliders = first.GetComponentsInChildren<Collider>();
      Collider[] second_colliders = second.GetComponentsInChildren<Collider>();

      for (int i = 0; i < first_colliders.Length; ++i) {
        for (int j = 0; j < second_colliders.Length; ++j) {
          if (first_colliders[i] != second_colliders[j] &&
              first_colliders[i].enabled && second_colliders[j].enabled) {
            Physics.IgnoreCollision(first_colliders[i], second_colliders[j], ignore);
          }
        }
      }
    }

    #endregion

    #region Gizmo Utils

    public static void DrawCircle(Vector3 center,
                           Vector3 normal,
                           float radius,
                           Color color,
                           int quality = 32,
                           float duration = 0,
                           bool depthTest = true) {
      Vector3 planeA = Vector3.Slerp(normal, -normal, 0.5f);
      DrawArc(360, center, planeA, normal, radius, color, quality);
    }

    /* Adapted from: Zarrax (http://math.stackexchange.com/users/3035/zarrax), Parametric Equation of a Circle in 3D Space?, 
     * URL (version: 2014-09-09): http://math.stackexchange.com/q/73242 */
    public static void DrawArc(float arc,
                           Vector3 center,
                           Vector3 forward,
                           Vector3 normal,
                           float radius,
                           Color color,
                           int quality = 32) {

      Gizmos.color = color;
      Vector3 right = Vector3.Cross(normal, forward).normalized;
      float deltaAngle = arc / quality;
      Vector3 thisPoint = center + forward * radius;
      Vector3 nextPoint = new Vector3();
      for (float angle = 0; Mathf.Abs(angle) <= Mathf.Abs(arc); angle += deltaAngle) {
        float cosAngle = Mathf.Cos(angle * Constants.DEG_TO_RAD);
        float sinAngle = Mathf.Sin(angle * Constants.DEG_TO_RAD);
        nextPoint.x = center.x + radius * (cosAngle * forward.x + sinAngle * right.x);
        nextPoint.y = center.y + radius * (cosAngle * forward.y + sinAngle * right.y);
        nextPoint.z = center.z + radius * (cosAngle * forward.z + sinAngle * right.z);
        Gizmos.DrawLine(thisPoint, nextPoint);
        thisPoint = nextPoint;
      }
    }

    public static void DrawCone(Vector3 origin,
                           Vector3 direction,
                           float angle,
                           float height,
                           Color color,
                           int quality = 4,
                           float duration = 0,
                           bool depthTest = true) {

      float step = height / quality;
      for (float q = step; q <= height; q += step) {
        DrawCircle(origin + direction * q, direction, Mathf.Tan(angle * Constants.DEG_TO_RAD) * q, color, quality * 8, duration, depthTest);
      }
    }

    #endregion

  }

}
