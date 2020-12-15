/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity {

  public class EnumEventTableEditor {

    private SerializedProperty _entries;

    private GUIContent _iconToolbarMinus;
    private GUIContent _eventIDName;
    private GUIContent _addButtonContent;

    private GUIContent[] _enumNames;
    private int[] _enumValues;

    /// <summary>
    /// The enum event table is unable to properly display itself in the inspector, it
    /// instead must be constructed and used from within a custom editor.  Pass in 
    /// a serialized property pointing to the EnumEventTable, and the enum type used
    /// by the table.
    /// </summary>
    public EnumEventTableEditor(SerializedProperty tableProperty, Type enumType) {
      _entries = tableProperty.FindPropertyRelative("_entries");

      _addButtonContent = new GUIContent("Add New Event Type");
      _eventIDName = new GUIContent("");
      // Have to create a copy since otherwise the tooltip will be overwritten.
      _iconToolbarMinus = new GUIContent(EditorGUIUtility.IconContent("Toolbar Minus"));
      _iconToolbarMinus.tooltip = "Remove all events in this list.";

      _enumNames = Enum.GetNames(enumType).Query().Select(s => new GUIContent(s)).ToArray();
      _enumValues = (int[])Enum.GetValues(enumType);
    }

    public void DoGuiLayout() {
      if (_entries.serializedObject.isEditingMultipleObjects) {
        return;
      }

      int toBeRemovedEntry = -1;

      EditorGUILayout.Space();

      Vector2 removeButtonSize = GUIStyle.none.CalcSize(_iconToolbarMinus);

      for (int i = 0; i < _entries.arraySize; ++i) {
        SerializedProperty delegateProperty = _entries.GetArrayElementAtIndex(i);
        SerializedProperty enumValueProperty = delegateProperty.FindPropertyRelative("enumValue");
        SerializedProperty callbacksProperty = delegateProperty.FindPropertyRelative("callback");

        int index = Array.IndexOf(_enumValues, enumValueProperty.intValue);
        if (index < 0) {
          _eventIDName.text = "Event " + enumValueProperty.intValue;
        } else {
          _eventIDName.text = _enumNames[index].text;
        }

        EditorGUILayout.PropertyField(callbacksProperty, _eventIDName);
        Rect callbackRect = GUILayoutUtility.GetLastRect();

        Rect removeButtonPos = new Rect(callbackRect.xMax - removeButtonSize.x - 8, callbackRect.y + 1, removeButtonSize.x, removeButtonSize.y);
        if (GUI.Button(removeButtonPos, _iconToolbarMinus, GUIStyle.none)) {
          toBeRemovedEntry = i;
        }

        EditorGUILayout.Space();
      }

      if (toBeRemovedEntry > -1) {
        RemoveEntry(toBeRemovedEntry);
      }

      Rect btPosition = GUILayoutUtility.GetRect(_addButtonContent, GUI.skin.button);
      const float addButonWidth = 200f;
      btPosition.x = btPosition.x + (btPosition.width - addButonWidth) / 2;
      btPosition.width = addButonWidth;
      if (GUI.Button(btPosition, _addButtonContent)) {
        showAddEventMenu();
      }

      _entries.serializedObject.ApplyModifiedProperties();
    }

    public bool HasAnyCallbacks(int enumValue) {
      if (_entries == null) return false;
      for (int i = 0; i < _entries.arraySize; i++) {
        var entryProperty = _entries.GetArrayElementAtIndex(i);
        var enumValueProperty = entryProperty.FindPropertyRelative("enumValue");
        if (enumValueProperty.intValue == enumValue) {
          return true;
        }
      }
      return false;
    }

    private void RemoveEntry(int toBeRemovedEntry) {
      _entries.DeleteArrayElementAtIndex(toBeRemovedEntry);
    }

    private void showAddEventMenu() {
      // Now create the menu, add items and show it
      GenericMenu menu = new GenericMenu();
      for (int i = 0; i < _enumNames.Length; ++i) {
        bool active = true;

        // Check if we already have a Entry for the current enum type, if so, disable it
        for (int p = 0; p < _entries.arraySize; ++p) {
          SerializedProperty entryProperty = _entries.GetArrayElementAtIndex(p);
          SerializedProperty enumValueProperty = entryProperty.FindPropertyRelative("enumValue");
          if (enumValueProperty.intValue == _enumValues[i]) {
            active = false;
          }
        }

        if (active) {
          menu.AddItem(_enumNames[i], false, OnAddNewSelected, _enumValues[i]);
        } else {
          menu.AddDisabledItem(_enumNames[i]);
        }
      }
      menu.ShowAsContext();

      Event.current.Use();
    }

    private void OnAddNewSelected(object enumValue) {
      _entries.arraySize += 1;
      SerializedProperty entryProperty = _entries.GetArrayElementAtIndex(_entries.arraySize - 1);
      SerializedProperty enumValueProperty = entryProperty.FindPropertyRelative("enumValue");
      enumValueProperty.intValue = (int)enumValue;

      _entries.serializedObject.ApplyModifiedProperties();
    }
  }
}
