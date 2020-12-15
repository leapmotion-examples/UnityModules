/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Leap.Unity.Splines {

  [ExecuteInEditMode]
  public class BSpline : MonoBehaviour {

    public Transform pointsParent = null;

    [Range(5, 120)]
    public int numDivisions = 5;

    [Tooltip("Must have at least one more control point than this value.")]
    public int polyDegree = 3;

    public List<float> _weightsBuffer = new List<float>();

    public bool isUniform = true;

    [Range(0f, 1f)]
    public float renderFraction = 1f;
    public bool reverseRenderOrder = false;

    private List<Vector3> _pointsBuffer = new List<Vector3>();
    private List<float> _knotVectorBuffer = new List<float>();

    private List<Vector3> _outputBuffer = new List<Vector3>();

    private void Update() {
      _pointsBuffer.Clear();
      if (pointsParent != null) {
        foreach (var child in pointsParent.GetChildren()) {
          _pointsBuffer.Add(child.position);
        }
      }

      if (_weightsBuffer == null) {
        _weightsBuffer = new List<float>();
      }
      while (_weightsBuffer.Count < _pointsBuffer.Count) {
        _weightsBuffer.Add(1f);
      }
      while (_weightsBuffer.Count > _pointsBuffer.Count) {
        _weightsBuffer.RemoveLast();
      }
      for (int i = 0; i < _weightsBuffer.Count; i++) {
        if (_weightsBuffer[i] < 0.01f) {
          _weightsBuffer[i] = 0.01f;
        }
      }

      if (_pointsBuffer.Count > 1) {
        //var minPolyDegree = 3;
        //while (_pointsBuffer.Count < 4) {
        //  _pointsBuffer.Add(_pointsBuffer[0]);
        //}
        var usePolyDegree = Mathf.Min(polyDegree, _pointsBuffer.Count - 1);

        _knotVectorBuffer.Clear();
        calculateKnotVector(usePolyDegree, _pointsBuffer.Count, isUniform: true,
          fillKnotVector: _knotVectorBuffer);

        var step = 1f / numDivisions;
        _outputBuffer.Clear();
        for (float t = 0; t < 1f; t += step) {
          _outputBuffer.Add(interpolateRationalBSpline(_pointsBuffer,
            _weightsBuffer, usePolyDegree, _knotVectorBuffer, t));
        }
      }
    }

    protected void OnDrawGizmos() {
      if (_pointsBuffer.Count > 1) {
        var currFraction = 0f;
        var fractionStep = 1f / (_outputBuffer.Count - 1);
        var start = 0;
        var end = _outputBuffer.Count - 1;
        var dir = 1;
        if (reverseRenderOrder) {
          start = _outputBuffer.Count - 1;
          end = 0;
          dir = -1;
        }
        for (int i = start; i != end; i += dir) {
          var curr = _outputBuffer[i];
          var next = _outputBuffer[i + dir];
          Drawer.UnityGizmoDrawer.Line(curr, next);
          currFraction += fractionStep;
          if (currFraction > renderFraction) { break; }
        }
      }
    }

    public void SetRenderFraction(float t) {
      renderFraction = t;
    }

    // B-Spline code from:
    // https://www.codeproject.com/Articles/1095142/Generate-and-understand-NURBS-curves

    private void calculateKnotVector(int polyDegree, int numControlPoints, 
      bool isUniform, List<float> fillKnotVector = null)
    {
      if (polyDegree + 1 > numControlPoints || numControlPoints == 0) {
        return;
      }

      StringBuilder outText = new StringBuilder();

      int n = numControlPoints;
      int m = n + polyDegree + 1;

      if (isUniform) {
        int divisor = m - 1;
        fillKnotVector.Add(0);
        outText.Append("0");
        for (int i = 1; i < m; i++) {
          if (i >= m - 1) {
            fillKnotVector.Add(1);
            outText.Append(", 1");
          }
          else {
            fillKnotVector.Add(i / (float)divisor);
            outText.Append(", " + i.ToString() + "/" + divisor.ToString());
          }
        }
      }
      else {
        int divisor = m - 1 - 2 * polyDegree;
        outText.Append("0");
        for (int i = 0; i < m; i++) {
          if (i < polyDegree) {
            fillKnotVector.Add(0);
            outText.Append(", 0");
          }
          else if (i >= m - polyDegree - 1) {
            fillKnotVector.Add(1);
            outText.Append(", 1");
          }
          else {
            int numerator = i - polyDegree;
            fillKnotVector.Add(numerator / (float)divisor);
            outText.Append(", " + numerator.ToString() + "/" +
              divisor.ToString());
          }
        }
      }
    }

    private Vector3 interpolateBSpline(List<Vector3> controlPoints,
      int polyDegree, List<float> knotVector, float t)
    {
      float x = 0, y = 0, z = 0;
      for (int i = 0; i < controlPoints.Count; i++) {
        var knotWeight = calcKnotWeight(i, polyDegree, knotVector, t);
        x += controlPoints[i].x * knotWeight;
        y += controlPoints[i].y * knotWeight;
        z += controlPoints[i].z * knotWeight;
      }
      return new Vector3(x, y, z);
    }

    private Vector3 interpolateRationalBSpline(List<Vector3> controlPoints,
      List<float> controlPointWeights, int polyDegree, List<float> knotVector,
      float t)
    {
      float x = 0, y = 0, z = 0;
      float totalWeight = 0f;

      for (int i = 0; i < controlPoints.Count; i++) {
        var knotWeight = calcKnotWeight(i, polyDegree, knotVector, t) *
          controlPointWeights[i];
        totalWeight += knotWeight;
      }

      for (int i = 0; i < controlPoints.Count; i++) {
        var knotWeight = calcKnotWeight(i, polyDegree, knotVector, t);
        x += controlPoints[i].x * controlPointWeights[i] * knotWeight /
          totalWeight;
        y += controlPoints[i].y * controlPointWeights[i] * knotWeight /
          totalWeight;
        z += controlPoints[i].z * controlPointWeights[i] * knotWeight /
          totalWeight;
      }

      return new Vector3(x, y, z);
    }
    
    private float[] N = new float[128];
    private float calcKnotWeight(int i, int polyDegree, List<float> knotVector,
      float t)
    {
      //var lenN = polyDegree + 1;
      float saved, temp;

      int maxKnotVectorIndex = knotVector.Count - 1;
      if ((i == 0 && t == knotVector[0]) ||
        (i == (maxKnotVectorIndex - polyDegree - 1) &&
        t == knotVector[maxKnotVectorIndex])) 
      {
        return 1;
      }
      if (t < knotVector[i] || t >= knotVector[i + polyDegree + 1]) {
        return 0;
      }

      for (int j = 0; j <= polyDegree; j++) {
        if (t >= knotVector[i + j] && t < knotVector[i + j + 1]) {
          N[j] = 1;
        }
        else {
          N[j] = 0;
        }
      }

      for (int k = 1; k <= polyDegree; k++) {
        if (N[0] == 0) {
          saved = 0;
        }
        else {
          saved = ((t - knotVector[i]) * N[0]) /
            (knotVector[i + k] - knotVector[i]);
        }

        for (int j = 0; j < polyDegree - k + 1; j++) {
          float knotVectorLeft = knotVector[i + j + 1];
          float knotVectorRight = knotVector[i + j + k + 1];

          if (N[j + 1] == 0) {
            N[j] = saved;
            saved = 0;
          }
          else {
            temp = N[j + 1] / (knotVectorRight - knotVectorLeft);
            N[j] = saved + (knotVectorRight - t) * temp;
            saved = (t - knotVectorLeft) * temp;
          }
        }
      }
      return N[0];
    }

  }

}
