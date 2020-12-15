/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Attachments {

  [CustomEditor(typeof(AttachmentHands))]
  public class AttachmentHandsEditor : CustomEditorBase<AttachmentHands> {

    private Texture _handTex;
    private Rect _handTexRect;

    protected override void OnEnable() {
      base.OnEnable();

      _handTex = EditorResources.Load<Texture2D>("HandTex");

      this.specifyCustomDrawer("_attachmentPoints", drawAttachmentPointsEditor);
    }

    private void drawAttachmentPointsEditor(SerializedProperty property) {

      // Set up the draw rect space based on the image and available editor space.

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Attachment Transforms", EditorStyles.boldLabel);

      // Determine whether the target object is a prefab. AttachmentPoints cannot be edited on prefabs.
      var isTargetPrefab = Utils.IsObjectPartOfPrefabAsset(target.gameObject);

      if (isTargetPrefab) {
        EditorGUILayout.HelpBox("Drag the prefab into the scene to make changes to attachment points.", MessageType.Info, true);
      }

      _handTexRect = EditorGUILayout.BeginVertical(GUILayout.MinWidth(EditorGUIUtility.currentViewWidth),
                                                   GUILayout.MinHeight(EditorGUIUtility.currentViewWidth * (_handTex.height / (float)_handTex.width)),
                                                   GUILayout.MaxWidth(_handTex.width),
                                                   GUILayout.MaxHeight(_handTex.height));
      
      Rect imageContainerRect = _handTexRect; imageContainerRect.width = EditorGUIUtility.currentViewWidth - 30F;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));
      imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.6F, 0.6F, 0.6F));
      imageContainerRect.x += 1; imageContainerRect.y += 1; imageContainerRect.width -= 2; imageContainerRect.height -= 2;
      EditorGUI.DrawRect(imageContainerRect, new Color(0.2F, 0.2F, 0.2F));

      _handTexRect = new Rect(_handTexRect.x + (imageContainerRect.center.x - _handTexRect.center.x),
                              _handTexRect.y,
                              _handTexRect.width,
                              _handTexRect.height);
      EditorGUI.DrawTextureTransparent(_handTexRect, _handTex);
      EditorGUILayout.Space();


      // Draw the toggles for the attachment points.

      EditorGUI.BeginDisabledGroup(isTargetPrefab);

      makeAttachmentPointsToggle("Palm",                new Vector2( 0.100F,  0.160F));
      makeAttachmentPointsToggle("Wrist",               new Vector2( 0.080F,  0.430F));

      makeAttachmentPointsToggle("ThumbProximalJoint",  new Vector2(-0.190F,  0.260F));
      makeAttachmentPointsToggle("ThumbDistalJoint",    new Vector2(-0.310F,  0.170F));
      makeAttachmentPointsToggle("ThumbTip",            new Vector2(-0.390F,  0.110F));

      makeAttachmentPointsToggle("IndexKnuckle",        new Vector2(-0.040F, -0.040F));
      makeAttachmentPointsToggle("IndexMiddleJoint",    new Vector2(-0.060F, -0.170F));
      makeAttachmentPointsToggle("IndexDistalJoint",    new Vector2(-0.075F, -0.280F));
      makeAttachmentPointsToggle("IndexTip",            new Vector2(-0.080F, -0.380F));

      makeAttachmentPointsToggle("MiddleKnuckle",       new Vector2( 0.080F, -0.050F));
      makeAttachmentPointsToggle("MiddleMiddleJoint",   new Vector2( 0.080F, -0.190F));
      makeAttachmentPointsToggle("MiddleDistalJoint",   new Vector2( 0.080F, -0.310F));
      makeAttachmentPointsToggle("MiddleTip",           new Vector2( 0.090F, -0.420F));

      makeAttachmentPointsToggle("RingKnuckle",         new Vector2( 0.195F, -0.020F));
      makeAttachmentPointsToggle("RingMiddleJoint",     new Vector2( 0.220F, -0.150F));
      makeAttachmentPointsToggle("RingDistalJoint",     new Vector2( 0.230F, -0.270F));
      makeAttachmentPointsToggle("RingTip",             new Vector2( 0.245F, -0.380F));

      makeAttachmentPointsToggle("PinkyKnuckle",        new Vector2( 0.295F,  0.040F));
      makeAttachmentPointsToggle("PinkyMiddleJoint",    new Vector2( 0.340F, -0.050F));
      makeAttachmentPointsToggle("PinkyDistalJoint",    new Vector2( 0.380F, -0.130F));
      makeAttachmentPointsToggle("PinkyTip",            new Vector2( 0.410F, -0.210F));

      EditorGUI.EndDisabledGroup();

      EditorGUILayout.EndVertical();
    }

    private void makeAttachmentPointsToggle(string attachmentFlagName, Vector2 offCenterPosImgSpace) {
      AttachmentPointFlags attachmentPoints = target.attachmentPoints;

      AttachmentPointFlags flag = (AttachmentPointFlags)System.Enum.Parse(typeof(AttachmentPointFlags), attachmentFlagName, true);

      if (EditorGUI.Toggle(makeToggleRect(_handTexRect.center
                                          + new Vector2(offCenterPosImgSpace.x * _handTexRect.width,
                                                        offCenterPosImgSpace.y * _handTexRect.height)),
                           (attachmentPoints & flag) == flag)) {

        target.attachmentPoints = attachmentPoints | flag; // Set flag bit to 1.
      }
      else {
        if (!wouldFlagDeletionDestroyData(target, flag)
            || EditorUtility.DisplayDialog("Delete " + flag + " Attachment Point?",
                                           "Deleting the " + flag + " attachment point will destroy "
                                         + "its GameObject and any of its non-Attachment-Point children, "
                                         + "and will remove any components attached to it.",
                                           "Delete " + flag + " Attachment Point", "Cancel")) {

          target.attachmentPoints = attachmentPoints & (~flag); // Set flag bit to 0.
        }
      }
    }

    private const float TOGGLE_SIZE = 15.0F;
    private Rect makeToggleRect(Vector2 centerPos) {
      return new Rect(centerPos.x - TOGGLE_SIZE / 2F, centerPos.y - TOGGLE_SIZE / 2F, TOGGLE_SIZE, TOGGLE_SIZE);
    }

    private static bool wouldFlagDeletionDestroyData(AttachmentHands target, AttachmentPointFlags flag) {
      if (target.attachmentHands == null) return false;

      foreach (var attachmentHand in target.attachmentHands) {
        var point = attachmentHand.GetBehaviourForPoint(flag);

        if (point == null) return false;
        else {
          // Data will be destroyed if this AttachmentPointBehaviour's Transform contains any children
          // that are not themselves AttachmentPointBehaviours.
          foreach (var child in point.transform.GetChildren()) {
            if (child.GetComponent<AttachmentPointBehaviour>() == null) return true;
          }

          // Data will be destroyed if this AttachmentPointBehaviour contains any components
          // that aren't constructed automatically.
          foreach (var component in point.gameObject.GetComponents<Component>()) {
            if (component is Transform) continue;
            if (component is AttachmentPointBehaviour) continue;

            return true;
          }
        }
      }
      return false;
    }

  }

}
