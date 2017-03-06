﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ReadWriteUtility {

  public static bool EnsureReadWriteEnabled(this Texture texture) {
#if UNITY_EDITOR
    string assetPath = AssetDatabase.GetAssetPath(texture);
    if (string.IsNullOrEmpty(assetPath)) {
      return false;
    }

    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
    if (importer == null) {
      return false;
    }

    if (!importer.isReadable) {
      importer.isReadable = true;
      importer.SaveAndReimport();
      AssetDatabase.Refresh();
    }
#endif

    return true;
  }

  public static bool EnsureReadWriteEnabled(this Mesh mesh) {
#if UNITY_EDITOR
    string assetPath = AssetDatabase.GetAssetPath(mesh);
    if (string.IsNullOrEmpty(assetPath)) {
      return false;
    }

    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
    if (importer == null) {
      return false;
    }

    if (!importer.isReadable) {
      importer.isReadable = true;
      importer.SaveAndReimport();
      AssetDatabase.Refresh();
    }
#endif

    return true;
  }
}
