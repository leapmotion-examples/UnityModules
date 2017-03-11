﻿using UnityEditor;
using Leap.Unity;

[CustomEditor(typeof(LeapGuiBakedRenderer))]
public class LeapGuiBakedRendererEditor : CustomEditorBase {

  protected override void OnEnable() {
    base.OnEnable();

    specifyConditionalDrawing("_createMeshRenderers", "_enableLightmapping");
    specifyConditionalDrawing("_enableLightmapping", "_lightmapUnwrapSettings",
                                                     "_giFlags");
  }
}
