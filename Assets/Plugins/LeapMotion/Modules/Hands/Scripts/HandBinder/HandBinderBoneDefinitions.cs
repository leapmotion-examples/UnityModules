/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.HandsModule {

    /// <summary>
    /// Used to define what bones names are valid for each finger
    /// </summary>
    [System.Serializable]
    public class BoneDefinitions {
        public string[] DefinitionThumb = { "thumb" };
        public string[] DefinitionIndex = { "index" };
        public string[] DefinitionMiddle = { "middle" };
        public string[] DefinitionRing = { "ring" };
        public string[] DefinitionPinky = { "pinky", "little" };
        public string[] DefinitionWrist = { "wrist", "hand", "palm" };
        public string[] DefinitionElbow = { "elbow", "upperArm" };
    }
}