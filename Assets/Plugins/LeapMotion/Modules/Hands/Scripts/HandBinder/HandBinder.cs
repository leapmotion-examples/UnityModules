/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.HandsModule {

    [DisallowMultipleComponent]
    public class HandBinder : HandModelBase {
        public Hand LeapHand;

        [Tooltip("The size of the debug gizmos")]
        public float GizmoSize = 0.004f;
        [Tooltip("The length of the elbow to maintain the correct offset from the wrist")]
        public float elbowLength;

        [Tooltip("The Rotation offset that will be assigned to all the Fingers")]
        public Vector3 GlobalFingerRotationOffset;
        [Tooltip("The Rotation offset that will be assigned to the wrist")]
        public Vector3 wristRotationOffset;

        [Tooltip("Set the assigned transforms to the leap hand during editor")]
        public bool SetEditorPose;
        [Tooltip("Set the assigned transforms to the same position as the Leap Hand")]
        public bool SetPositions;
        [Tooltip("Use metacarpal bones")]
        public bool UseMetaBones;
        [Tooltip("Show the Leap Hand in the scene")]
        public bool DebugLeapHand = true;
        [Tooltip("Show the Leaps rotation axis in the scene")]
        public bool DebugLeapRotationAxis = false;
        [Tooltip("Show the assigned gameobjects as gizmos in the scene")]
        public bool DebugModelTransforms = true;
        [Tooltip("Show the assigned gameobjects rotation axis in the scene")]
        public bool DebugModelRotationAxis;

        //Used by Editor Script
        public bool fineTuning;
        public bool debugOptions;
        public bool needsResetting = false;

        //The data structure that contains transforms that get bound to the leap data
        public BoundHand boundHand = new BoundHand();

        //User defines offsets in editor script
        public List<BoundTypes> offsets = new List<BoundTypes>();

        //Stores all the childrens default pose
        public SerializedTransform[] defaultHandPose;

        public override Chirality Handedness { get { return handedness; } set { } }
        public Chirality handedness;
        public override ModelType HandModelType { get { return ModelType.Graphics; } }

        public override Hand GetLeapHand() {
            return LeapHand;
        }

        public override void SetLeapHand(Hand hand) {
            LeapHand = hand;
        }

        public override bool SupportsEditorPersistence() {

            bool editorPersistance = SetEditorPose;

            if(SetEditorPose == false) {
                ResetHand();
            }

            if(DebugLeapHand) {
                editorPersistance = true;
            }

            return editorPersistance;
        }

        private void OnDestroy() {
            ResetHand();
        }

        //Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time.
        private void Reset() {

            //Return if we already have assigned base transforms
            if(defaultHandPose != null) {
                ResetHand();
                return;
            }

            else {
                //Store all children transforms so the user has the ability to reset back to a default pose
                var allChildren = new List<Transform>();
                allChildren.Add(transform);
                allChildren.AddRange(HandBinderAutoBinder.GetAllChildren(transform));

                var baseTransforms = new List<SerializedTransform>();
                foreach(var child in allChildren) {
                    var serializedTransform = new SerializedTransform();
                    serializedTransform.reference = child.gameObject;
                    serializedTransform.transform = new TransformStore();
                    serializedTransform.transform.position = child.localPosition;
                    serializedTransform.transform.rotation = child.localRotation.eulerAngles;
                    baseTransforms.Add(serializedTransform);
                }
                defaultHandPose = baseTransforms.ToArray();
            }
        }

        /// <summary>
        /// Update the BoundGameobjects so that the positions and rotations match that of the leap hand
        /// </summary>
        public override void UpdateHand() {

            if(!SetEditorPose && !Application.isPlaying) {
                return;
            }

            if(LeapHand == null) {
                ResetHand();
                return;
            }

            //Calculate the elbows position and rotation making sure to maintain the models forearm length
            if(boundHand.elbow.boundTransform != null && boundHand.wrist.boundTransform != null && elbowLength > 0) {
                //Calculate the position of the elbow based on the calcualted elbow length
                var elbowPosition = LeapHand.WristPosition.ToVector3() -
                                        ((LeapHand.Arm.Basis.zBasis.ToVector3() * elbowLength) + boundHand.elbow.offset.position);
                if(!elbowPosition.ContainsNaN()) {
                    boundHand.elbow.boundTransform.transform.position = elbowPosition;
                    boundHand.elbow.boundTransform.transform.rotation = LeapHand.Arm.Rotation.ToQuaternion() * Quaternion.Euler(boundHand.elbow.offset.rotation);
                }
            }

            //Update the wrists position and rotation to leap data
            if(boundHand.wrist.boundTransform != null) {

                //Calculate the position of the wrist to the leap position + offset defined by the user
                var wristPosition = LeapHand.WristPosition.ToVector3() + boundHand.wrist.offset.position;

                //Calculate rotation offset needed to get the wrist into the same rotation as the leap based on the calculated wrist offset
                var leapRotationOffset = ((Quaternion.Inverse(boundHand.wrist.boundTransform.transform.rotation) * LeapHand.Rotation.ToQuaternion()) * Quaternion.Euler(wristRotationOffset)).eulerAngles;

                //Set the wrist bone to the calculated values
                boundHand.wrist.boundTransform.transform.position = wristPosition;
                boundHand.wrist.boundTransform.transform.rotation *= Quaternion.Euler(leapRotationOffset);
            }

            //Loop through all the leap fingers and update the bound fingers to the leap data
            if(LeapHand != null) {
                for(int fingerIndex = 0; fingerIndex < LeapHand.Fingers.Count; fingerIndex++) {
                    for(int boneIndex = 0; boneIndex < LeapHand.Fingers[fingerIndex].bones.Length; boneIndex++) {

                        //The transform that the user has defined
                        var boundTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].boundTransform;

                        //Continue if the user has not defined a transform for this finger
                        if(boundTransform == null) {
                            continue;
                        }

                        //Get the start transform that was stored for each assigned transform
                        var startTransform = boundHand.fingers[fingerIndex].boundBones[boneIndex].startTransform;

                        if(boneIndex == 0 && !UseMetaBones) {
                            boundTransform.transform.localRotation = Quaternion.Euler(startTransform.rotation);
                            boundTransform.transform.localPosition = startTransform.position;
                            continue;
                        }

                        //Get the leap bone to extract the position and rotation values
                        var leapBone = LeapHand.Fingers[fingerIndex].bones[boneIndex];
                        //Get any offsets the user has set up
                        var boneOffset = boundHand.fingers[fingerIndex].boundBones[boneIndex].offset;

                        //Only update the finger position if the user has defined this behaviour
                        if(SetPositions) {
                            boundTransform.transform.position = leapBone.PrevJoint.ToVector3();
                            boundTransform.transform.localPosition += boneOffset.position;
                        }

                        else {
                            boundTransform.transform.localPosition = startTransform.position + boneOffset.position;
                        }

                        //Update the bound transforms rotation to the leaps rotation * global rotation offset * any further offsets the user has defined
                        boundTransform.transform.rotation = leapBone.Rotation.ToQuaternion() * Quaternion.Euler(GlobalFingerRotationOffset) * Quaternion.Euler(boneOffset.rotation);
                    }
                }
            }

            needsResetting = true;
        }

        /// <summary>
        /// Reset the boundGameobjects back to the default pose
        /// </summary>
        public void ResetHand(bool forceReset = false) {

            if(defaultHandPose == null || !needsResetting && forceReset != true) {
                return;
            };

            for(int i = 0; i < defaultHandPose.Length; i++) {

                var baseTransform = defaultHandPose[i];
                if(baseTransform != null && baseTransform.reference != null) {
                    baseTransform.reference.transform.localPosition = baseTransform.transform.position;
                    baseTransform.reference.transform.localRotation = Quaternion.Euler(baseTransform.transform.rotation);
                }
            }
            needsResetting = false;
        }
    }
}