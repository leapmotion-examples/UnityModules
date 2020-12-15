/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity {

  public static class Hotkeys {

    [MenuItem("GameObject/Make Group %g")]
    public static void MakeGroup() {
      if (!CorePreferences.allowGroupObjectsHotkey) {
        return;
      }

      GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
      if (objs.Length == 0) {
        return;
      }

      Transform first = objs[0].transform;

      List<Transform> hierarchy = new List<Transform>();

      Transform parent = first.parent;
      while (parent != null) {
        hierarchy.Add(parent);
        parent = parent.parent;
      }

      int index = 0;
      parent = hierarchy.FirstOrDefault();

      if (parent != null) {
        foreach (var obj in objs) {
          Transform t = obj.transform;
          while (!t.IsChildOf(parent) || t == parent) {
            index++;
            if (index >= hierarchy.Count) {
              parent = null;
              break;
            } else {
              parent = hierarchy[index];
            }
          }
          if (parent == null) {
            break;
          }
        }
      }

      GameObject root = new GameObject("Group");
      root.transform.SetParent(parent);
      root.transform.localPosition = Vector3.zero;
      root.transform.localRotation = Quaternion.identity;
      root.transform.localScale = Vector3.one;
      Undo.RegisterCreatedObjectUndo(root, "Created group object.");

      List<Transform> allTransforms = new List<Transform>();
      if (parent == null) {
        var sceneRoots = root.scene.GetRootGameObjects();
        foreach (var sceneRoot in sceneRoots) {
          allTransforms.AddRange(sceneRoot.GetComponentsInChildren<Transform>());
        }
      } else {
        allTransforms.AddRange(parent.GetComponentsInChildren<Transform>());
      }

      foreach (var obj in allTransforms) {
        if (objs.Contains(obj.gameObject)) {
          Transform originalParent = obj.transform.parent;
          obj.transform.SetParent(root.transform, worldPositionStays: true);

          Vector3 newPos = obj.transform.localPosition;
          Quaternion newRot = obj.transform.localRotation;
          Vector3 newScale = obj.transform.localScale;

          obj.transform.SetParent(originalParent, worldPositionStays: true);
          Undo.SetTransformParent(obj.transform, root.transform, "Moved " + obj.name + " into group.");
          Undo.RecordObject(obj.transform, "Set new transform for " + obj.name + ".");

          obj.transform.localPosition = newPos;
          obj.transform.localRotation = newRot;
          obj.transform.localScale = newScale;
        }
      }

      Selection.activeGameObject = root;
      Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }

    [MenuItem("GameObject/Reset Local Transform %e")]
    public static void ResetAll() {
      if (!CorePreferences.allowClearTransformHotkey) {
        return;
      }

      GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
      foreach (var obj in objs) {
        Undo.RecordObject(obj.transform, "Cleared transform for " + obj.name + ".");
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
      }
    }

    [MenuItem("GameObject/Reset Local Position and Rotation %#e")]
    public static void ResetPositionRotation() {
      if (!CorePreferences.allowClearTransformHotkey) {
        return;
      }

      GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
      foreach (var obj in objs) {
        Undo.RecordObject(obj.transform, "Cleared local position and rotation for " + obj.name + ".");
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
      }
    }

    [MenuItem("GameObject/Deselect All %#d")]
    static void DeselectAll() {
      if (!CorePreferences.allowClearTransformHotkey) {
        return;
      }

      Selection.objects = new Object[0];
    }

  }
}
