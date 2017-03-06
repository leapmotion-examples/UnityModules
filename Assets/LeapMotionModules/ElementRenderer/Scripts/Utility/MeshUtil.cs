﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Query;

public static class MeshUtil {
  public const int MAX_VERT_COUNT = 65535;

  public static List<UVChannelFlags> allUvChannels;
  static MeshUtil() {
    allUvChannels = new List<UVChannelFlags>();
    allUvChannels.Add(UVChannelFlags.UV0);
    allUvChannels.Add(UVChannelFlags.UV1);
    allUvChannels.Add(UVChannelFlags.UV2);
    allUvChannels.Add(UVChannelFlags.UV3);
  }

  public static void RemapUvs(List<Vector4> uvs, Rect mapping) {
    RemapUvs(uvs, mapping, uvs.Count);
  }

  public static void RemapUvs(List<Vector4> uvs, Rect mapping, int lastCount) {
    for (int i = uvs.Count - lastCount; i < uvs.Count; i++) {
      Vector4 uv = uvs[i];
      uv.x = mapping.x + uv.x * mapping.width;
      uv.y = mapping.y + uv.y * mapping.height;
      uvs[i] = uv;
    }
  }

  public static void GetUVsOrDefault(this Mesh mesh, int channel, List<Vector4> uvs) {
    mesh.GetUVs(channel, uvs);
    if (uvs.Count != mesh.vertexCount) {
      uvs.Fill(mesh.vertexCount, Vector4.zero);
    }
  }

  public static int Index(this UVChannelFlags flags) {
    switch (flags) {
      case UVChannelFlags.UV0:
        return 0;
      case UVChannelFlags.UV1:
        return 1;
      case UVChannelFlags.UV2:
        return 2;
      case UVChannelFlags.UV3:
        return 3;
    }
    throw new InvalidOperationException();
  }
}
