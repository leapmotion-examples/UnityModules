/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(SerializableHashSetBase), useForChildren: true)]
  public class SerializableHashSetEditor : PropertyDrawer {

    private ReorderableList _list;
    private SerializedProperty _currProperty;

    private List<Value> _values = new List<Value>();
    private class Value {
      public int index;
      public bool isDuplicate;
      public SerializedProperty value;

      public Value(int index, bool isDuplicate, SerializedProperty value) {
        this.index = index;
        this.isDuplicate = isDuplicate;
        this.value = value;
      }
    }

    public SerializableHashSetEditor() {
      _list = new ReorderableList(_values, typeof(Value),
                                  draggable: true,
                                  displayHeader: true,
                                  displayAddButton: true,
                                  displayRemoveButton: true);

      _list.drawElementCallback = drawElementCallback;
      _list.elementHeightCallback = elementHeightCallback;
      _list.drawHeaderCallback = drawHeader;
      _list.onAddCallback = onAddCallback;
      _list.onRemoveCallback = onRemoveCallback;
      _list.onReorderCallback = onReorderCallback;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      if (property.hasMultipleDifferentValues) {
        GUI.Box(position, "");
        EditorGUI.LabelField(position, "Multi-object editing not supported for Serialized HashSets.", EditorStyles.miniLabel);
      } else {
        _currProperty = property;

        updatePairsFromProperty(property);

        EditorGUIUtility.labelWidth /= 2;
        _list.DoList(position);
        EditorGUIUtility.labelWidth *= 2;
      }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      if (property.hasMultipleDifferentValues) {
        return EditorGUIUtility.singleLineHeight;
      } else {
        updatePairsFromProperty(property);
        return _list.GetHeight();
      }
    }

    private void updatePairsFromProperty(SerializedProperty property) {
      SerializedProperty values = property.FindPropertyRelative("_values");

      var dup = (fieldInfo.GetValue(property.serializedObject.targetObject) as ICanReportDuplicateInformation).GetDuplicationInformation();

      _values.Clear();
      int count = values.arraySize;
      for (int i = 0; i < count; i++) {
        SerializedProperty value = values.GetArrayElementAtIndex(i);

        bool isDup = false;
        if (i < dup.Count) {
          isDup = dup[i] > 1;
        }

        _values.Add(new Value(i, isDup, value));
      }
    }

    private void drawHeader(Rect rect) {
      EditorGUI.LabelField(rect, _currProperty.displayName);
      rect.x += rect.width - 110;
      rect.width = 110;
      if (GUI.Button(rect, "Clear Duplicates")) {
        markDirty(_currProperty);

        Undo.RecordObject(_currProperty.serializedObject.targetObject, "Cleared duplicates");
        (fieldInfo.GetValue(_currProperty.serializedObject.targetObject) as ICanReportDuplicateInformation).ClearDuplicates();

        _currProperty.serializedObject.Update();

        markDirty(_currProperty);
        updatePairsFromProperty(_currProperty);
      }
    }

    private void drawElementCallback(Rect rect, int index, bool isActive, bool isFocused) {
      Value value = _values[index];

      if (value.isDuplicate) {
        GUI.contentColor = new Color(1, 0.7f, 0);
        GUI.color = new Color(1, 0.7f, 0.5f);
      }

      if (value.value.propertyType == SerializedPropertyType.ObjectReference && value.value.objectReferenceValue == null) {
        GUI.contentColor = new Color(1, 0, 0);
        GUI.color = new Color(1, 0, 0);
      }

      drawProp(value.value, rect);

      GUI.contentColor = Color.white;
      GUI.color = Color.white;
      GUI.backgroundColor = Color.white;
    }

    private void onAddCallback(ReorderableList list) {
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      values.arraySize++;

      updatePairsFromProperty(_currProperty);
    }

    private void onRemoveCallback(ReorderableList list) {
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      actuallyDeleteAt(values, list.index);

      updatePairsFromProperty(_currProperty);
    }

    private void onReorderCallback(ReorderableList list) {
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      int startIndex = -1, endIndex = -1;
      bool isForward = true;

      for (int i = 0; i < _values.Count; i++) {
        if (i != _values[i].index) {
          if (_values[i].index - i > 1) {
            isForward = false;
          }
          startIndex = i;
          break;
        }
      }

      for (int i = _values.Count; i-- != 0;) {
        if (i != _values[i].index) {
          endIndex = i;
          break;
        }
      }

      if (isForward) {
        values.MoveArrayElement(startIndex, endIndex);
      } else {
        values.MoveArrayElement(endIndex, startIndex);
      }

      updatePairsFromProperty(_currProperty);
    }

    private float elementHeightCallback(int index) {
      Value value = _values[index];
      float size = getSize(value.value);
      _list.elementHeight = size;
      return size;
    }

    private float getSize(SerializedProperty prop) {
      float size = 0;
      if (prop.propertyType == SerializedPropertyType.Generic) {
        SerializedProperty copy = prop.Copy();
        SerializedProperty endProp = copy.GetEndProperty(false);

        copy.NextVisible(true);
        while (!SerializedProperty.EqualContents(copy, endProp)) {
          size += EditorGUI.GetPropertyHeight(copy);
          copy.NextVisible(false);
        }
      } else {
        size = EditorGUI.GetPropertyHeight(prop, GUIContent.none, false);
      }
      return size;
    }

    private void drawProp(SerializedProperty prop, Rect r) {
      if (prop.propertyType == SerializedPropertyType.Generic) {
        SerializedProperty copy = prop.Copy();
        SerializedProperty endProp = copy.GetEndProperty(false);
        copy.NextVisible(true);
        while (!SerializedProperty.EqualContents(copy, endProp)) {
          r.height = EditorGUI.GetPropertyHeight(copy);
          EditorGUI.PropertyField(r, copy, true);
          r.y += r.height;
          copy.NextVisible(false);
        }
      } else {
        r.height = EditorGUI.GetPropertyHeight(prop);
        EditorGUI.PropertyField(r, prop, GUIContent.none, false);
      }
    }

    private void markDirty(SerializedProperty property) {
      SerializedProperty values = property.FindPropertyRelative("_values");
      int size = values.arraySize;

      values.InsertArrayElementAtIndex(size);
      actuallyDeleteAt(values, size);
    }

    private static void actuallyDeleteAt(SerializedProperty property, int index) {
      int arraySize = property.arraySize;

      while (property.arraySize == arraySize) {
        property.DeleteArrayElementAtIndex(index);
      }
    }
  }
}
