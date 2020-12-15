/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity {

  [CustomEditor(typeof(PostProcessProvider), editorForChildClasses: true)]
  public class PostProcessProviderEditor : CustomEditorBase<PostProcessProvider> {

    protected override void OnEnable() {
      base.OnEnable();

      // Edit-time pose is only relevant for providers that generate hands.
      // Post-process Providers are a special case and don't generate their own hands.
      specifyConditionalDrawing(() => false, "editTimePose");
    }

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      drawNotificationsGUI();
    }

    private void drawNotificationsGUI() {
      var provider = this.target;

      if (!provider.enabled) {
        EditorGUILayout.HelpBox(
          message: "This post-process provider is disabled, so it will not output any "
                 + "hand data. Use pass-through mode if you only want to disable its "
                 + "post-processing and still output hands.",
          type: MessageType.Info
        );
      }
      else if (provider.passthroughOnly) {
        EditorGUILayout.HelpBox(
          message: "This post-process provider is set to pass-through only, so it will "
                 + "pass its input unmodified to its output.",
          type: MessageType.Info
        );
      }
    }

  }

}
