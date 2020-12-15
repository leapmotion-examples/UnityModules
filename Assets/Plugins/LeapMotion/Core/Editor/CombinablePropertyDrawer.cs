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
using System.Reflection;
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity.Attributes {

  [CustomPropertyDrawer(typeof(CombinablePropertyAttribute), true)]
  public class CombinablePropertyDrawer : PropertyDrawer {

    private static Dictionary<FieldInfo, List<CombinablePropertyAttribute>> _cachedAttributes = new Dictionary<FieldInfo, List<CombinablePropertyAttribute>>();

    private List<CombinablePropertyAttribute> attributes = new List<CombinablePropertyAttribute>();
    private void getAttributes(SerializedProperty property) {
      GetAttributes(property, fieldInfo, out attributes);
    }

    public static void GetAttributes(SerializedProperty property,
      FieldInfo fieldInfo, out List<CombinablePropertyAttribute> outAttributes)
    {
      if (!_cachedAttributes.TryGetValue(fieldInfo, out outAttributes)) {
        outAttributes = new List<CombinablePropertyAttribute>();

        foreach (object o in fieldInfo.GetCustomAttributes(typeof(CombinablePropertyAttribute), true)) {
          CombinablePropertyAttribute combinableProperty = o as CombinablePropertyAttribute;
          if (combinableProperty != null) {
            if (combinableProperty.SupportedTypes.Count() != 0 && !combinableProperty.SupportedTypes.Contains(property.propertyType)) {
              Debug.LogError("Property attribute " +
                             combinableProperty.GetType().Name +
                             " does not support property type " +
                             property.propertyType + ".");
              continue;
            }
            outAttributes.Add(combinableProperty);
          }
        }

        _cachedAttributes[fieldInfo] = outAttributes;
      }
    }

    public override float GetPropertyHeight(SerializedProperty property,
      GUIContent label) 
    {
      getAttributes(property);

      var topPanelDrawer = attributes.Query().Cast<ITopPanelDrawer>()
        .Where(o => o != null).FirstOrDefault();
      if (topPanelDrawer != null) {
        return topPanelDrawer.GetHeight() + EditorGUI.GetPropertyHeight(property,
          includeChildren: true);
      }

      return EditorGUI.GetPropertyHeight(property, includeChildren: true);
    }

    public override bool CanCacheInspectorGUI(SerializedProperty property) {
      return false;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      getAttributes(property);

      CombinablePropertyDrawer.OnGUI(this.attributes, this.fieldInfo, position,
        property, label);
    }

    public static void OnGUI(List<CombinablePropertyAttribute> attributes,
      FieldInfo fieldInfo, Rect position, SerializedProperty property,
      GUIContent label)
    {
      float defaultLabelWidth = EditorGUIUtility.labelWidth;
      float fieldWidth = position.width - EditorGUIUtility.labelWidth;

      bool canUseDefaultDrawer = true;
      bool shouldDisable = false;

      RangeAttribute rangeAttribute = null;
      if (fieldInfo != null) {
        rangeAttribute = fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true).FirstOrDefault() as RangeAttribute;
      }

      ISupportDragAndDrop dragAndDropSupport = null;

      IFullPropertyDrawer fullPropertyDrawer = null;
      ITopPanelDrawer topPanelDrawer = null;
      foreach (var a in attributes) {
        if (fieldInfo != null) {
          a.Init(fieldInfo, property.serializedObject.targetObjects);
        }

        if (a is IBeforeLabelAdditiveDrawer) {
          EditorGUIUtility.labelWidth -= (a as IBeforeLabelAdditiveDrawer).GetWidth();
        }

        if (a is IAfterLabelAdditiveDrawer) {
          EditorGUIUtility.labelWidth -= (a as IAfterLabelAdditiveDrawer).GetWidth();
          canUseDefaultDrawer = false;
        }

        if (a is IBeforeFieldAdditiveDrawer) {
          fieldWidth -= (a as IBeforeFieldAdditiveDrawer).GetWidth();
          canUseDefaultDrawer = false;
        }

        if (a is IAfterFieldAdditiveDrawer) {
          fieldWidth -= (a as IAfterFieldAdditiveDrawer).GetWidth();
        }

        if (a is IPropertyDisabler) {
          shouldDisable |= (a as IPropertyDisabler).ShouldDisable(property);
        }

        if (a is IFullPropertyDrawer) {
          if (fullPropertyDrawer != null) {
            Debug.LogError("Cannot have 2 advanced attributes that both override the field drawing");
            return;
          }
          fullPropertyDrawer = a as IFullPropertyDrawer;
        }

        if (a is ISupportDragAndDrop) {
          dragAndDropSupport = (a as ISupportDragAndDrop);
        }

        if (a is ITopPanelDrawer) {
          topPanelDrawer = (a as ITopPanelDrawer);
        }
      }

      if (fullPropertyDrawer != null && !canUseDefaultDrawer) {
        Debug.LogError("Cannot have an advanced attribute drawer that draws a custom field, and also have an advanced attribute drawer that draws between label and field!");
        return;
      }

      Rect r = position;
      Rect topPanel = new Rect();
      if (topPanelDrawer != null) {
        // Y = 0 is the top, so take the Rect's "bottom" for top panel.
        topPanel = r.TakeBottom(topPanelDrawer.GetHeight(), out r);
      }

      if (dragAndDropSupport != null) {
        processDragAndDrop(dragAndDropSupport, ref r, property);
      }

      EditorGUI.BeginChangeCheck();
      EditorGUI.BeginDisabledGroup(shouldDisable);

      if (topPanelDrawer != null) {
        topPanelDrawer.Draw(topPanel, property);
      }

      drawAdditive<IBeforeLabelAdditiveDrawer>(attributes, ref r, property);

      if (canUseDefaultDrawer) {
        r.width = EditorGUIUtility.labelWidth + fieldWidth;

        if (fullPropertyDrawer != null) {
          fullPropertyDrawer.DrawProperty(r, property, label);
        } else {
          if (rangeAttribute != null) {
            if (property.propertyType == SerializedPropertyType.Integer) {
              property.intValue = EditorGUI.IntSlider(r, label, property.intValue, (int)rangeAttribute.min, (int)rangeAttribute.max);
            } else if (property.propertyType == SerializedPropertyType.Float) {
              property.floatValue = EditorGUI.Slider(r, label, property.floatValue, rangeAttribute.min, rangeAttribute.max);
            } else {
              EditorGUI.PropertyField(r, property, label, includeChildren: true);
            }
          } else {
            EditorGUI.PropertyField(r, property, label, includeChildren: true);
          }
        }

        r.x += r.width;
      }
      else {
        r.width = EditorGUIUtility.labelWidth;
        r = EditorGUI.PrefixLabel(r, label);

        drawAdditive<IAfterLabelAdditiveDrawer>(attributes, ref r, property);
        drawAdditive<IBeforeFieldAdditiveDrawer>(attributes, ref r, property);

        r.width = fieldWidth;
        EditorGUI.PropertyField(r, property, GUIContent.none,
          includeChildren: true);
        r.x += r.width;
      }

      drawAdditive<IAfterFieldAdditiveDrawer>(attributes, ref r, property);

      EditorGUI.EndDisabledGroup();

      bool didChange = false;
      try {
        didChange = EditorGUI.EndChangeCheck();
      }
      catch (System.Exception e) {
        Debug.LogWarning("CombinablePropertyDrawer exception getting didChange: " +
          e.ToString());
      }

      if (didChange || !property.hasMultipleDifferentValues) {
        foreach (var a in attributes) {
          if (a is IPropertyConstrainer) {
            (a as IPropertyConstrainer).ConstrainValue(property);
          }
        }
      }

      if (didChange) {
        foreach (var a in attributes) {
          a.OnPropertyChanged(property);
        }
      }

      EditorGUIUtility.labelWidth = defaultLabelWidth;
    }

    private static void drawAdditive<T>(List<CombinablePropertyAttribute> attributes,
                                        ref Rect r, SerializedProperty property)
                          where T : class, IAdditiveDrawer {
      foreach (var a in attributes) {
        if (a is T) {
          T t = a as T;
          r.width = t.GetWidth();
          t.Draw(r, property);
          r.x += r.width;
        }
      }
    }

    private static void processDragAndDrop(ISupportDragAndDrop dragAndDropSupport,
                                           ref Rect r, SerializedProperty property) {
      Event curEvent = Event.current;
      Rect dropArea = dragAndDropSupport.GetDropArea(r, property);

      switch (curEvent.type) {
        case EventType.Repaint:
        case EventType.DragUpdated:
        case EventType.DragPerform:
          if (!dropArea.Contains(curEvent.mousePosition, allowInverse: true)) {
            break;
          }

          bool isValidDrop = dragAndDropSupport.IsDropValid(
                               DragAndDrop.objectReferences, property);

          if (isValidDrop) {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
          } else {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
          }

          if (curEvent.type == EventType.DragPerform && isValidDrop) {
            DragAndDrop.AcceptDrag();

            dragAndDropSupport.ProcessDroppedObjects(
                                 DragAndDrop.objectReferences, property);
          }

          break;
      }
    }
  }
}
