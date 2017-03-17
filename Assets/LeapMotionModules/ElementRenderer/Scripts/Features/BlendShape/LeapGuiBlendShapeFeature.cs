﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

[AddComponentMenu("")]
[LeapGuiTag("Blend Shape")]
public class LeapGuiBlendShapeFeature : LeapGuiFeature<LeapGuiBlendShapeData> {
  public const string FEATURE_NAME = LeapGui.FEATURE_PREFIX + "BLEND_SHAPES";

  [EditTimeOnly]
  [SerializeField]
  private BlendShapeSpace _space;

  public BlendShapeSpace space {
    get {
      return _space;
    }
    set {
      isDirty = true;
      _space = value;
    }
  }

  public enum BlendShapeSpace {
    Local,
    World
  }

  public override SupportInfo GetSupportInfo(LeapGuiGroup group) {
    if (!group.renderer.IsValidElement<LeapGuiMeshElementBase>()) {
      return SupportInfo.Error("Blend shapes a renderer that supports mesh elements.");
    } else {
      return SupportInfo.FullSupport();
    }
  }

#if UNITY_EDITOR
  public override void DrawFeatureEditor(Rect rect, bool isActive, bool isFocused) {
    _space = (BlendShapeSpace)EditorGUI.EnumPopup(rect, "Blend Space", _space);
  }

  public override float GetEditorHeight() {
    return EditorGUIUtility.singleLineHeight;
  }
#endif
}
