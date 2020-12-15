/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  using UnityObject = UnityEngine.Object;

  public static class EditorUtils {

    private class SceneReference<T> where T : UnityObject {
      SerializedObject objectContainingReference;
      SerializedProperty reference;
    }

    /// <summary>
    /// Scans the currently-open scene for references of a and replaces them with b.
    /// These swaps are performed with Undo.RecordObject before an object's references
    /// are changed, so they can be undone.
    /// </summary>
    public static void ReplaceSceneReferences<T>(T a, T b) where T : UnityObject {
      var aId = a.GetInstanceID();
      var refType = typeof(T);
      
      var curScene = SceneManager.GetActiveScene();
      var rootObjs = curScene.GetRootGameObjects();
      foreach (var rootObj in rootObjs) {
        var transforms = rootObj.GetComponentsInChildren<Transform>();
        foreach (var transform in transforms) {
          var components = transform.GetComponents<Component>();

          var objectChanges = new List<Action>();
          foreach (var component in components) {
            var compType = typeof(Component);
            var fieldInfos = compType.GetFields(BindingFlags.Instance
              | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic
              | BindingFlags.Public);
            foreach (var fieldInfo in fieldInfos.Query()
              .Where(fi => fi.FieldType.IsAssignableFrom(refType))) {
              var refValue = fieldInfo.GetValue(component) as T;
              if (refValue.GetInstanceID() == aId) {
                objectChanges.Add(() => {
                  fieldInfo.SetValue(fieldInfo, b);
                });
              }
            }
          }
          if (objectChanges.Count > 0) {
            Undo.RecordObject(transform.gameObject,
              "Swap " + transform.name + " references from " + a.name + " to " + b.name);
            foreach (var change in objectChanges) {
              change();
            }
          }
        }
      }
    }

  }

}
