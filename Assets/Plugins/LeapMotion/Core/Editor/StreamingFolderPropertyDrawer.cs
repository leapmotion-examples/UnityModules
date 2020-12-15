/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(StreamingFolder), useForChildren: true)]
  public class StreamingFolderPropertyDrawer : AssetFolderPropertyDrawer {

    protected override bool ValidatePath(string fullPath, string relativePath, out string errorMessage) {
      var fullInfo = new DirectoryInfo(fullPath);
      var streamingInfo = new DirectoryInfo(Application.streamingAssetsPath);

      if (IsInsideOrEqual(fullInfo, streamingInfo)) {
        errorMessage = null;
        return true;
      } else {
        errorMessage = "The specified folder is not a streaming asset folder. Streaming asset folders must be inside project's Assets/StreamingAssets directory.";
        return false;
      }
    }

    private bool IsInsideOrEqual(DirectoryInfo path, DirectoryInfo folder) {
      if (path.Parent == null) {
        return false;
      }

      if (string.Equals(path.FullName, folder.FullName, StringComparison.InvariantCultureIgnoreCase)) {
        return true;
      }

      return IsInsideOrEqual(path.Parent, folder);
    }
  }
}
