﻿using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class GuiRectUtil {

  public static void SplitHorizontally(this Rect rect, out Rect left, out Rect right) {
    left = rect;
    left.width /= 2;
    right = left;
    right.x += right.width;
  }

  public static void SplitHorizontallyWithRight(this Rect rect, out Rect left, out Rect right, float rightWidth) {
    left = rect;
    left.width -= rightWidth;
    right = left;
    right.x += right.width;
    right.width = rightWidth;
  }

  public static Rect NextLine(this Rect rect) {
    rect.y += rect.height;
    return rect;
  }

  public static Rect FromRight(this Rect rect, float width) {
    rect.x = rect.width - width;
    rect.width = width;
    return rect;
  }

#if UNITY_EDITOR
  public static Rect SingleLine(this Rect rect) {
    rect.height = EditorGUIUtility.singleLineHeight;
    return rect;
  }

  public static Rect Indent(this Rect rect) {
    rect.x += EditorGUIUtility.singleLineHeight;
    rect.width -= EditorGUIUtility.singleLineHeight;
    return rect;
  }
#endif
}
