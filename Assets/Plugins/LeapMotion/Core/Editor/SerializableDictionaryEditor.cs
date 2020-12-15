/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Leap.Unity {

  [CustomPropertyDrawer(typeof(SerializableDictionaryBase), useForChildren: true)]
  public class SerializableDictionaryEditor : PropertyDrawer {

    private ReorderableList _list;
    private SerializedProperty _currProperty;

    private List<Pair> _pairs = new List<Pair>();
    private class Pair {
      public int index;
      public bool isDuplicate;
      public SerializedProperty a;
      public SerializedProperty b;

      public Pair(int index, bool isDuplicate, SerializedProperty a, SerializedProperty b) {
        this.index = index;
        this.isDuplicate = isDuplicate;
        this.a = a;
        this.b = b;
      }
    }

    public SerializableDictionaryEditor() {
      _list = new ReorderableList(_pairs, typeof(Pair),
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
        EditorGUI.LabelField(position, "Multi-object editing not supported for Serialized Dictionaries.", EditorStyles.miniLabel);
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
      SerializedProperty keys = property.FindPropertyRelative("_keys");
      SerializedProperty values = property.FindPropertyRelative("_values");

      var dup = (fieldInfo.GetValue(property.serializedObject.targetObject) as ICanReportDuplicateInformation).GetDuplicationInformation();

      _pairs.Clear();
      int count = keys.arraySize;
      for (int i = 0; i < count; i++) {
        SerializedProperty key = keys.GetArrayElementAtIndex(i);
        SerializedProperty value = values.GetArrayElementAtIndex(i);

        bool isDup = false;
        if (i < dup.Count) {
          isDup = dup[i] > 1;
        }

        _pairs.Add(new Pair(i, isDup, key, value));
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
      Rect leftRect = rect;
      leftRect.width *= (fieldInfo.GetValue(_currProperty.serializedObject.targetObject) as ISerializableDictionary).KeyDisplayRatio();

      Rect rightRect = rect;
      rightRect.x += leftRect.width;
      rightRect.width -= leftRect.width;

      Pair pair = _pairs[index];

      if (pair.isDuplicate) {
        GUI.contentColor = new Color(1, 0.7f, 0);
        GUI.color = new Color(1, 0.7f, 0.5f);
      }

      if (pair.a.propertyType == SerializedPropertyType.ObjectReference && pair.a.objectReferenceValue == null) {
        GUI.contentColor = new Color(1, 0, 0);
        GUI.color = new Color(1, 0, 0);
      }

      drawProp(pair.a, leftRect);

      GUI.contentColor = Color.white;
      GUI.color = Color.white;
      GUI.backgroundColor = Color.white;

      drawProp(pair.b, rightRect);
    }

    private void onAddCallback(ReorderableList list) {
      SerializedProperty keys = _currProperty.FindPropertyRelative("_keys");
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      keys.arraySize++;
      values.arraySize++;

      updatePairsFromProperty(_currProperty);
    }

    private void onRemoveCallback(ReorderableList list) {
      SerializedProperty keys = _currProperty.FindPropertyRelative("_keys");
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      actuallyDeleteAt(keys, list.index);
      actuallyDeleteAt(values, list.index);

      updatePairsFromProperty(_currProperty);
    }

    private void onReorderCallback(ReorderableList list) {
      SerializedProperty keys = _currProperty.FindPropertyRelative("_keys");
      SerializedProperty values = _currProperty.FindPropertyRelative("_values");

      int startIndex = -1, endIndex = -1;
      bool isForward = true;

      for (int i = 0; i < _pairs.Count; i++) {
        if (i != _pairs[i].index) {
          if (_pairs[i].index - i > 1) {
            isForward = false;
          }
          startIndex = i;
          break;
        }
      }

      for (int i = _pairs.Count; i-- != 0;) {
        if (i != _pairs[i].index) {
          endIndex = i;
          break;
        }
      }

      if (isForward) {
        keys.MoveArrayElement(startIndex, endIndex);
        values.MoveArrayElement(startIndex, endIndex);
      } else {
        keys.MoveArrayElement(endIndex, startIndex);
        values.MoveArrayElement(endIndex, startIndex);
      }

      updatePairsFromProperty(_currProperty);
    }

    private float elementHeightCallback(int index) {
      Pair pair = _pairs[index];
      float size = Mathf.Max(getSize(pair.a), getSize(pair.b));
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
      SerializedProperty keys = property.FindPropertyRelative("_keys");
      int size = keys.arraySize;

      keys.InsertArrayElementAtIndex(size);
      actuallyDeleteAt(keys, size);
    }

    private static void actuallyDeleteAt(SerializedProperty property, int index) {
      int arraySize = property.arraySize;

      while (property.arraySize == arraySize) {
        property.DeleteArrayElementAtIndex(index);
      }
    }
  }
}
