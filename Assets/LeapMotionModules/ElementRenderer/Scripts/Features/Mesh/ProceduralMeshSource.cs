﻿using UnityEngine;
using UnityEngine.Rendering;

public abstract class ProceduralMeshSource : MonoBehaviour {

  // Just to make sure all procedural mesh sources can be enabled/disabled
  protected virtual void Start() { }

  /// <summary>
  /// Try to generate a procedural mesh for the given mesh feature. 
  /// </summary>
  public abstract bool TryGenerateMesh(LeapGuiMeshData meshFeature,
                                   out Mesh mesh,
                                   out UVChannelFlags remappableChannels);
}