/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;

namespace Leap.Unity {

  [CustomEditor(typeof(Comment))]
  public class CommentEditor : CustomEditorBase<Comment> {

    private SerializedProperty _isEditing;
    private GUIStyle _editStyle;
    private GUIStyle _displayStyle;

    protected override void OnEnable() {
      base.OnEnable();

      _isEditing = serializedObject.FindProperty("_isEditing");

      dontShowScriptField();

      specifyCustomDrawer("_comment", drawComment);
    }

    private void drawComment(SerializedProperty commentProp) {
      string text = commentProp.stringValue;

      if (_editStyle == null) {
        _editStyle = new GUIStyle(EditorStyles.textArea);
        _editStyle.wordWrap = true;
      }

      if (_displayStyle == null) {
        _displayStyle = new GUIStyle(EditorStyles.label);
        _displayStyle.wordWrap = true;
        _displayStyle.richText = true;
      }

      if (string.IsNullOrEmpty(text)) {
        _isEditing.boolValue = true;
      }

      if (_isEditing.boolValue) {
        if (GUILayout.Button("Finish")) {
          _isEditing.boolValue = false;
        }

        commentProp.stringValue = EditorGUILayout.TextArea(text, _editStyle);
      } else {
        EditorGUILayout.Space();
        var rect = GUILayoutUtility.GetRect(new GUIContent(text), _displayStyle);
        EditorGUI.SelectableLabel(rect, text, _displayStyle);
      }
    }

  }
}
