/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using Leap.Unity.Infix;
using UnityEngine;

namespace Leap.Unity {

  /// <summary> Simple drawing interface abstraction (intended for debug drawing,
  /// not production!) with statically-accessible backend implementations via 
  /// HyperMegaLines, Unity Debug drawing, and Unity Gizmo drawing. </summary>
  public class Drawer {

    // Minimum Drawer Functions.
    public System.Action<Vector3, Vector3> implDrawLine;
    public System.Action<Color> implSetColor;

    /// <summary> Calls the `setColor` delegate. </summary>
    public Color color { set { implSetColor(value); }}

    public Stack<Matrix4x4> _matrices;
    public Matrix4x4 _currMatrix = Matrix4x4.identity;
    public bool MaybePushMatrix(Matrix4x4? m) {
      if (m != null) { PushMatrix(m.Value); return true; } else { return false; }
    }
    public void PushMatrix(Matrix4x4 m) {
      _matrices.Push(m);
      _currMatrix = m;
    }
    public void PopMatrix() {
      _currMatrix = _matrices.Pop();
    }

    // Extended Drawer Functions.
    public System.Action<Matrix4x4> implDrawUnitSphere;

    public bool isActiveAndEnabled = true;

    public void MaybeSetColor(Color? maybeColor) {
      if (maybeColor != null) { implSetColor(maybeColor.Value); }
    }

    #region Implementations

    //private static Drawer _hyperMegaDrawer;
    //private static Leap.Unity.HyperMegaStuff.HyperMegaLines _hyperMegaLines;
    //public static Drawer HyperMegaDrawer { get {
    //  var drawer = Utils.Require(ref _hyperMegaDrawer);
    //  if (_hyperMegaLines == null) {
    //    _hyperMegaLines = HyperMegaStuff.HyperMegaLines.drawer;
    //  }
    //  drawer.isActiveAndEnabled = _hyperMegaLines.isActiveAndEnabled;
    //  drawer.implDrawLine = (a, b) => {
    //    _hyperMegaLines.DrawLine(a, b);
    //  };
    //  drawer.implSetColor = (c) => {
    //    _hyperMegaLines.color = c;
    //  };
    //  return drawer;
    //}}

    private static Drawer _unityDebugDrawer;
    private static Color _unityDebugColor = Color.white;
    public static Drawer UnityDebugDrawer { get {
      var drawer = Utils.Require(ref _unityDebugDrawer);
      // Debug Lines are cleared on Update, don't use in edit mode
      drawer.isActiveAndEnabled = Application.isPlaying;
      drawer.implDrawLine = (a, b) => {
        var color = Drawer._unityDebugColor;
        UnityEngine.Debug.DrawLine(a, b, color);
      };
      drawer.implSetColor = (c) => {
        _unityDebugColor = c;
      };
      return drawer;
    }}

    private static Drawer _unityGizmoDrawer;
    /// <summary> For use in OnDrawGizmos and OnDrawGizmosSelected. </summary>
    public static Drawer UnityGizmoDrawer { get {
      var drawer = Utils.Require(ref _unityGizmoDrawer);
      drawer.isActiveAndEnabled = true;
      drawer.implDrawLine = (a, b) => {
        Gizmos.DrawLine(a, b);
      };
      drawer.implSetColor = (c) => {
        Gizmos.color = c;
      };
      drawer.implDrawUnitSphere = (m) => {
        var origM = Gizmos.matrix;
        Gizmos.matrix = m;
        Gizmos.DrawSphere(Vector3.zero, 1f);
        Gizmos.matrix = origM;
      };
      return drawer;
    }}

    private static Drawer _unityGizmoHandlesDrawer;
    /// <summary> For use in OnDrawGizmos and OnDrawGizmosSelected via the Handles API. By default, draws on _top_ of any scene geometry. </summary>
    public static Drawer UnityGizmoHandlesDrawer { get {
      var drawer = Utils.Require(ref _unityGizmoHandlesDrawer);
      drawer.isActiveAndEnabled = true;
      drawer.implDrawLine = (a, b) => {
        #if UNITY_EDITOR
        UnityEditor.Handles.DrawLine(a, b);
        #endif
      };
      drawer.implSetColor = (c) => {
        #if UNITY_EDITOR
        UnityEditor.Handles.color = c;
        #endif
      };
      drawer.implDrawUnitSphere = (m) => {
        #if UNITY_EDITOR
        var origM = UnityEditor.Handles.matrix;
        var origColor = UnityEditor.Handles.color;
        float h, s, v;
        Color.RGBToHSV(origColor, out h, out s, out v);
        UnityEditor.Handles.color = origColor.WithHSV(h, s * 1f, v * 3f);
        UnityEditor.Handles.matrix = m;
        // UnityEditor.Handles.SphereHandleCap
        UnityEditor.Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 1f, EventType.Repaint);
        UnityEditor.Handles.matrix = origM;
        #endif
      };
      return drawer;
    }}

    #endregion // Implementations

    #region Line Drawing

    public void Line(Vector3 a, Vector3 b) {
      this.implDrawLine(
        _currMatrix.MultiplyPoint3x4(a),
        _currMatrix.MultiplyPoint3x4(b)
      );
    }
    public void Line(Vector3 a, Vector3 b, Color? color) {
      MaybeSetColor(color);
      Line(a, b);
    }
    private Action<Vector3, Vector3> drawLineAction { get {
      return Utils.Require(ref _b_drawLineAction, () => (a, b) => Line(a, b));
    }}
    private Action<Vector3, Vector3> _b_drawLineAction;

    public void Lines(
      System.Action<System.Action<Vector3, Vector3>> drawLineFuncFunc,
      Color? color = null)
    {
      MaybeSetColor(color);
      drawLineFuncFunc(drawLineAction);
    }

    #endregion

    #region Sphere Drawing

    public void Sphere(Vector3 center = default(Vector3), float radius = 1f,
      Color? color = null, Matrix4x4? matrix = null)
    {
      try {
        if (implDrawUnitSphere != null) {
          var useMatrix = (matrix ?? Matrix4x4.identity) * Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * radius);
          MaybeSetColor(color);
          implDrawUnitSphere(useMatrix);
        } else {
          // Fall back to line drawing.
          this.WireSphere(center, radius, color, matrix);
        }
      } catch (System.Exception e) { Debug.LogError(e); }
    }

    #endregion

  }

  public static class DrawerExtensions {

    public static void WireSphere(this Drawer drawer,
      Vector3 center = default(Vector3), float radius = 1f, Color? color = null,
      Matrix4x4? matrix = null)
    {
      drawer.Lines(drawLineFunc =>
        new Geometry.Sphere(center, radius, overrideMatrix: matrix)
          .DrawLines(drawLineFunc), color);
    }

    public static void Lines(this Drawer drawer, Vector3 a, Vector3 b,
      Vector3 c)
    {
      drawer.Line(a, b); drawer.Line(b, c); 
    }

    public static void Lines(this Drawer drawer, Vector3 a, Vector3 b,
      Vector3 c, Vector3 d)
    {
      drawer.Line(a, b); drawer.Line(b, c); drawer.Line(c, d);
    }

    public static void Lines(this Drawer drawer, Vector3 a, Vector3 b,
      Vector3 c, Vector3 d, Vector3 e)
    {
      drawer.Line(a, b); drawer.Line(b, c); drawer.Line(c, d);
      drawer.Line(d, e);
    }

    private static Color[] _basisColors;
    public static void DrawBasis(this Transform t, Drawer drawer,
      float? length = null,
      Color? overrideAxesColor = null,
      float? axesHueShift = null,
      Matrix4x4? overrideMatrix = null)
    {
      var useLength = length.UnwrapOr(0.5f);
      var useMatrix = overrideMatrix.UnwrapOr(t.localToWorldMatrix);
      var a = Vector3.zero;
      Color xColor = Color.red, yColor = Color.green, zColor = Color.blue;
      if (overrideAxesColor != null) {
        xColor = yColor = zColor = overrideAxesColor.Value;
      }
      if (axesHueShift != null) {
        var hueShift = axesHueShift.Value;
        xColor = xColor.ShiftHue(hueShift);
        yColor = yColor.ShiftHue(hueShift);
        zColor = zColor.ShiftHue(hueShift);
      }
      Utils.Require(ref _basisColors, 3);
      _basisColors[0] = xColor;
      _basisColors[1] = yColor;
      _basisColors[2] = zColor;

      var octahedron = new Geometry.Bipyramid(a: a, b: Vector3.zero,
        polySegments: 4, lengthFraction: 0.5f,
        overrideMatrix: useMatrix);
      for (var bIdx = 0; bIdx < 3; bIdx++) {
        var b = Vector3.zero; b[bIdx] = useLength;
        octahedron.b = b;
        drawer.color = _basisColors[bIdx];
        octahedron.DrawLines(drawer.implDrawLine);
      }
    }

    /// <summary> Draws a bipyramidal basis axis using red, green, and blue for
    /// the X, Y, and Z axes at the argument pose.
    ///
    /// You can override the axis color with a single value (`overrideAxesColor`),
    /// or you can pass a hue shift value to apply to the axis colors
    /// (`axesHueShift`). </summary>
    public static void Draw(this Pose pose, Drawer drawer,
      float length, Color? overrideAxesColor = null,
      float? hueShift = null)
    {
      Color xColor = Color.red, yColor = Color.green, zColor = Color.blue;
      if (overrideAxesColor != null) {
        xColor = yColor = zColor = overrideAxesColor.Value;
      }
      if (hueShift != null) {
        var useHueShift = hueShift.Value;
        xColor = xColor.ShiftHue((float)useHueShift);
        yColor = yColor.ShiftHue((float)useHueShift);
        zColor = zColor.ShiftHue((float)useHueShift);
      }

      Utils.Require(ref _basisColors, 3);
      _basisColors[0] = xColor;
      _basisColors[1] = yColor;
      _basisColors[2] = zColor;

      var bipyramid = new Geometry.Bipyramid(a: Vector3.zero, b: Vector3.zero,
        polySegments: 16, lengthFraction: 0.5f, overrideMatrix: pose.matrix);
      for (var bIdx = 0; bIdx < 3; bIdx++) {
        var b = Vector3.zero; b[bIdx] = length;
        bipyramid.b = b;
        drawer.color = _basisColors[bIdx];
        bipyramid.DrawLines(drawer.implDrawLine);
      }
    }

    public static void Draw(this Matrix4x4 m, Drawer drawer,
      float axisLengths, float? hueShift = null,
      Color? color = null,
      Color? xColor = null, Color? yColor = null, Color? zColor = null,
      bool drawBips = false)
    {
      var useXColor = xColor ?? color ?? Color.red;
      var useYColor = yColor ?? color ?? Color.green;
      var useZColor = zColor ?? color ?? Color.blue;
      if (hueShift != null) {
        useXColor = useXColor.ShiftHue(hueShift.Value);
        useYColor = useYColor.ShiftHue(hueShift.Value);
        useZColor = useZColor.ShiftHue(hueShift.Value);
      }
      if (drawBips) {
        var bipyramid = new Geometry.Bipyramid(a: Vector3.zero, b: Vector3.zero,
          polySegments: 16, lengthFraction: 0.5f, overrideMatrix: m);
        for (var bIdx = 0; bIdx < 3; bIdx++) {
          var b = Vector3.zero; b[bIdx] = axisLengths;
          bipyramid.b = b;
          if (bIdx == 0) { drawer.color = useXColor; }
          if (bIdx == 1) { drawer.color = useYColor; }
          if (bIdx == 2) { drawer.color = useZColor; }
          bipyramid.DrawLines(drawer.implDrawLine);
        }
      } else {
        for (var i = 0; i < 3; i++) {
          var pos = m.GetPosition();
          if (i == 0) { drawer.color = useXColor; }
          if (i == 1) { drawer.color = useYColor; }
          if (i == 2) { drawer.color = useZColor; }
          drawer.implDrawLine(pos, pos + m.GetAxis(i) * axisLengths);
        }
      }
    }

  }

}
