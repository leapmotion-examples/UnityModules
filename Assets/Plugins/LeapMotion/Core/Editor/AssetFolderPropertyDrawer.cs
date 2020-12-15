/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(AssetFolder), useForChildren: true)]
  public class AssetFolderPropertyDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      Rect left, right;
      position.SplitHorizontallyWithRight(out left, out right, position.height);
      left.width -= 2;

      Object folderAsset = null;
      string folderPath = "";

      SerializedProperty folderProp = property.FindPropertyRelative("_assetFolder");
      if (folderProp.hasMultipleDifferentValues) {
        EditorGUI.showMixedValue = true;
      } else {
        folderAsset = folderProp.objectReferenceValue;
        if (folderAsset != null) {
          folderPath = AssetDatabase.GetAssetPath(folderAsset);
        }
      }

      EditorGUI.TextField(left, label, folderPath);

      var content = EditorGUIUtility.IconContent("Folder Icon");

      if (GUI.Button(right, content, GUIStyle.none)) {
        string resultPath = PromptUserForPath(folderPath);
        if (!string.IsNullOrEmpty(resultPath)) {
          string relativePath = Utils.MakeRelativePath(Application.dataPath, resultPath);
          var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);

          string errorMessage;
          if (!ValidatePath(resultPath, relativePath, out errorMessage)) {
            EditorUtility.DisplayDialog("Invalid selection.", errorMessage, "OK");
          } else {
            folderProp.objectReferenceValue = asset;
          }
        }
      }

      EditorGUI.showMixedValue = false;

      if (position.Contains(Event.current.mousePosition)) {
        var draggedObject = DragAndDrop.objectReferences.FirstOrDefault();
        string errorMessage;
        if (draggedObject != null) {
          switch (Event.current.type) {
            case EventType.DragUpdated:
              if (ValidateObject(draggedObject, out errorMessage)) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
              } else {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
              }
              break;
            case EventType.DragPerform:
              if (ValidateObject(draggedObject, out errorMessage)) {
                DragAndDrop.AcceptDrag();
                folderProp.objectReferenceValue = draggedObject;
              }
              break;
          }
        }
      }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      return EditorGUIUtility.singleLineHeight;
    }

    protected virtual string PromptUserForPath(string currentPath) {
      return EditorUtility.OpenFolderPanel("Select Folder", currentPath, "");
    }

    protected virtual bool ValidateObject(Object asset, out string errorMessage) {
      string relativePath = AssetDatabase.GetAssetPath(asset);
      string fullPath = Path.GetFullPath(relativePath);
      return ValidatePath(fullPath, relativePath, out errorMessage);
    }

    protected virtual bool ValidatePath(string fullPath, string relativePath, out string errorMessage) {
      if (!Directory.Exists(fullPath)) {
        errorMessage = "The specified folder does not exist!";
        return false;
      }

      var asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(relativePath);
      if (asset != null) {
        errorMessage = null;
        return true;
      } else {
        errorMessage = "The specified folder is not an asset folder. Asset folders must be inside project's Assets directory.";
        return false;
      }
    }
  }
}
