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

  [CustomPropertyDrawer(typeof(SingleLayer))]
  public class SingleLayerEditor : PropertyDrawer {
    private GUIContent[] _layerNames;
    private List<int> _layerValues;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      ensureLayersInitialized();

      SerializedProperty layerProperty = property.FindPropertyRelative("layerIndex");
      if (layerProperty == null) {
        Debug.LogWarning("Could not find the layer index property, was it renamed or removed?");
        return;
      }

      int index = _layerValues.IndexOf(layerProperty.intValue);
      if (index < 0) {
        if (Application.isPlaying) {
          //If application is playing we dont want to change the layers on the fly
          //Instead, just display them as an int value
          layerProperty.intValue = EditorGUI.IntField(position, property.displayName, layerProperty.intValue);
          return;
        } else {
          //If the application is not running, reset the layer to the default layer
          layerProperty.intValue = 0;
          index = 0;
        }
      }

      var tooltipAttribute = fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true).
                                       Cast<TooltipAttribute>().
                                       FirstOrDefault();

      if (tooltipAttribute != null) {
        label.tooltip = tooltipAttribute.tooltip;
      }

      bool originalMixedValue = EditorGUI.showMixedValue;
      if (layerProperty.hasMultipleDifferentValues) {
        EditorGUI.showMixedValue = true;
      }

      EditorGUI.BeginChangeCheck();
      index = EditorGUI.Popup(position, label, index, _layerNames);
      if (EditorGUI.EndChangeCheck()) {
        layerProperty.intValue = _layerValues[index];
      }

      EditorGUI.showMixedValue = originalMixedValue;
    }

    private void ensureLayersInitialized() {
      if (_layerNames == null) {
        Dictionary<int, GUIContent> valueToLayer = new Dictionary<int, GUIContent>();
        for (int i = 0; i < 32; i++) {
          string layerName = LayerMask.LayerToName(i);
          if (!string.IsNullOrEmpty(layerName)) {
            valueToLayer[i] = new GUIContent(layerName);
          }
        }

        _layerValues = valueToLayer.Keys.ToList();
        _layerNames = valueToLayer.Values.ToArray();
      }
    }
  }
}
