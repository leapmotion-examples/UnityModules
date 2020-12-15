/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct Box {

    public Transform transform;
    public Vector3 center;
    public Vector3 radii;
    public Matrix4x4? overrideMatrix;

    private Box(LocalBox localBox) {
      this.center = localBox.center;
      this.radii = localBox.radii;
      this.transform = null;
      this.overrideMatrix = null;
    }

    public Box(LocalBox localBox, Transform withTransform) 
           : this(localBox) {
      this.transform = withTransform;
    }

    public Box(Vector3 center, Vector3 radii, Component transformSource = null) {
      this.center = center;
      this.radii = radii;
      if (transformSource != null) { this.transform = transformSource.transform; }
      else { this.transform = null; }
      this.overrideMatrix = null;
    }

    public Box(Vector3 radii) : this(Vector3.zero, radii) { }

    public Box(Vector3 radii, Component transformSource) :
      this(Vector3.zero, radii, transformSource) { }

    public Box(float radius) : this(Vector3.one * radius) { }

    public Box(float radius, Component transformSource) : 
      this(Vector3.one * radius, transformSource) { }

    public static Box unit { get { return new Box(1f); }}

    /// <summary>
    /// Local-to-world matrix for this Box.
    /// </summary>
    public Matrix4x4 matrix {
      get {
        if (overrideMatrix != null) {
          return overrideMatrix.Value * Matrix4x4.Translate(center);
        }
        if (transform == null) {
          return Matrix4x4.Translate(center);
        }
        return transform.localToWorldMatrix * Matrix4x4.Translate(center);
      }
    }

    private Vector3 corner000 {
      get { return matrix.MultiplyPoint3x4(-radii); }
    }
    private Vector3 corner100 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        radii.x, -radii.y, -radii.z
      )); }
    }
    private Vector3 corner010 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        -radii.x, radii.y, -radii.z
      )); }
    }
    private Vector3 corner110 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        radii.x, radii.y, -radii.z
      )); }
    }
    private Vector3 corner001 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        -radii.x, -radii.y, radii.z
      )); }
    }
    private Vector3 corner101 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        radii.x, -radii.y, radii.z
      )); }
    }
    private Vector3 corner011 {
      get { return matrix.MultiplyPoint3x4(new Vector3(
        -radii.x, radii.y, radii.z
      )); }
    }
    private Vector3 corner111 {
      get { return matrix.MultiplyPoint3x4(radii); }
    }

    /// <summary> Returns the world-space position of the normalized coordinates
    /// (X, Y, Z each from 0 to 1) given this box's radii and attached transform.
    /// </summary>
    public Vector3 Sample(Vector3 coords01) {
      var coordsN1to1 = coords01.CompWise(f => Mathf.Clamp01(f) * 2 - 1);
      return matrix.MultiplyPoint3x4(this.radii.CompMul(coordsN1to1));
    }

    public void DrawLines(System.Action<Vector3, Vector3> drawLineFunc,
      int divisions = 0)
    {
      divisions = Mathf.Max(1, divisions);
      var frac = 1f / divisions;

      drawDividedLines(draw: drawLineFunc, step: frac, a: corner000, b: corner100);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner000, b: corner010);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner110, b: corner100);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner110, b: corner010);

      drawDividedLines(draw: drawLineFunc, step: frac, a: corner000, b: corner001);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner100, b: corner101);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner010, b: corner011);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner110, b: corner111);

      drawDividedLines(draw: drawLineFunc, step: frac, a: corner001, b: corner101);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner001, b: corner011);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner111, b: corner101);
      drawDividedLines(draw: drawLineFunc, step: frac, a: corner111, b: corner011);
    }

    private void drawDividedLines(System.Action<Vector3, Vector3> draw,
      float step, Vector3 a, Vector3 b)
    {
      step = Mathf.Max(0.01f, step);
      var a_t = a;
      for (var t = step; t <= 1f; t += step) {
        var b_t = Vector3.Lerp(a, b, t);
        draw(a_t, b_t);
        a_t = b_t;
      }
    }

    public void Draw(Drawer drawer, Color? color = null) {
      if (color != null) { drawer.color = color.Value; }
      DrawLines(drawer.Line);
    }

    public void DrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (transform != null) {
        drawer.PushMatrix();

        drawer.matrix = Matrix4x4.TRS(transform.TransformPoint(center),
                                      transform.rotation,
                                      Quaternion.Inverse(transform.rotation)
                                        * transform.TransformVector(radii));

        drawBoxGizmos(drawer, Vector3.zero, Vector3.one);

        drawer.PopMatrix();
      }
      else {
        drawBoxGizmos(drawer, center, radii);
      }
    }

    private void drawBoxGizmos(RuntimeGizmoDrawer drawer, Vector3 center, Vector3 radii) {
      drawer.DrawWireCube(center, radii * 2f);

      drawer.color = drawer.color.WithAlpha(0.05f);
      int div = 3;
      float invDiv = 1f / div;
      for (int i = 0; i < div; i++) {
        for (int j = 0; j < div; j++) {
          for (int k = 0; k < div; k++) {
            drawer.DrawWireCube((center + (new Vector3(i, j, k) - radii) * invDiv * 2),
                                radii * invDiv * 2f);
          }
        }
      }

      drawer.color = drawer.color.WithAlpha(0.04f);
      drawer.DrawCube(center, radii * 2f);
    }

  }

}
