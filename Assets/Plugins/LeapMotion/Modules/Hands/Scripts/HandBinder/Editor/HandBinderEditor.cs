/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.HandsModule {
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandBinder))]
    public class HandBinderEditor : Editor {
        private HandBinder myTarget;

        private Color handModelDebugCol = Color.green;
        private Color leapHandDebugCol = Color.black;
        private Color previousCol = Color.white;

        private Texture buttonTexture;
        private Texture downstate;
        private Texture dividerLine;
        private Texture subButton;

        private SerializedProperty handedness;
        private SerializedProperty debugLeapHand;
        private SerializedProperty DebugLeapRotationAxis;
        private SerializedProperty gizmoSize;
        private SerializedProperty debugModelTransforms;
        private SerializedProperty DebugModelRotationAxis;
        private SerializedProperty setPositions;
        private SerializedProperty useMetaBones;
        private SerializedProperty setEditorPose;
        private SerializedProperty globalFingerRotationOffset;
        private SerializedProperty wristRotationOffset;
        private SerializedProperty boundHand;
        private SerializedProperty offsets;
        private SerializedProperty fineTuning;
        private SerializedProperty debugOptions;
        public Rect windowRect0 = new Rect(20, 20, 120, 50);
        private GUIStyle buttonStyle;
        private GUIStyle subButtonStyle;
        private Color green = new Color32(140, 234, 40, 255);

        /// <summary>
        /// Assign the serialized properties
        /// </summary>
        private void SerializedProperties() {
            handedness = serializedObject.FindProperty("handedness");
            debugLeapHand = serializedObject.FindProperty("DebugLeapHand");
            DebugLeapRotationAxis = serializedObject.FindProperty("DebugLeapRotationAxis");
            gizmoSize = serializedObject.FindProperty("GizmoSize");
            debugModelTransforms = serializedObject.FindProperty("DebugModelTransforms");
            DebugModelRotationAxis = serializedObject.FindProperty("DebugModelRotationAxis");
            setPositions = serializedObject.FindProperty("SetPositions");
            useMetaBones = serializedObject.FindProperty("UseMetaBones");
            setEditorPose = serializedObject.FindProperty("SetEditorPose");
            globalFingerRotationOffset = serializedObject.FindProperty("GlobalFingerRotationOffset");
            wristRotationOffset = serializedObject.FindProperty("wristRotationOffset");
            handedness = serializedObject.FindProperty("handedness");
            fineTuning = serializedObject.FindProperty("fineTuning");
            debugOptions = serializedObject.FindProperty("debugOptions");
            boundHand = serializedObject.FindProperty("boundHand");
            offsets = serializedObject.FindProperty("offsets");

            buttonTexture = Resources.Load<Texture>("EditorDocumentationGreenUpstate");
            downstate = Resources.Load<Texture>("EditorDocumentationGreenDownstate");
            dividerLine = Resources.Load<Texture>("EditorDividerLine");
            subButton = Resources.Load<Texture>("SecondaryButton");
        }

        private void OnEnable() {
            serializedObject.Update();
            myTarget = (HandBinder)target;

            if(myTarget.gameObject.name.ToUpper().Contains("Left".ToUpper())) {
                myTarget.handedness = Chirality.Left;
            }
            if(myTarget.gameObject.name.ToUpper().Contains("Right".ToUpper())) {
                myTarget.handedness = Chirality.Right;
            }

            SerializedProperties();
        }

        /// <summary>
        /// Set the GUIStyle of the varius GUI elements we render
        /// </summary>
        private void SetUp() {
            buttonStyle = new GUIStyle(GUI.skin.button) {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() {
                    textColor = Color.black,
                    background = (Texture2D)buttonTexture,
                },
                active = new GUIStyleState() {
                    textColor = Color.white,
                    background = (Texture2D)downstate,
                },
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                stretchHeight = true,
                fixedHeight = 50,
            };

            subButtonStyle = new GUIStyle(GUI.skin.button) {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() {
                    textColor = Color.white,
                    background = (Texture2D)subButton,
                },
                stretchHeight = true,
                fixedHeight = 20,
            };

            previousCol = GUI.color;

        }

        /// <summary>
        /// Draw the inspector GUI
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            SetUp();
            GUIHandGraphic.DrawHandGraphic(myTarget.handedness, GUIHandGraphic.FlattenHandBinderTransforms(myTarget));
            DrawAutoBindButton();
            ShowBindingOptions();
            ShowDebugOptions();
            ShowFineTuningOptions();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAutoBindButton() {
            //Draw the Auto Bind Button
            if(Selection.gameObjects.Length == 1 && GUILayout.Button("Bind Hand", buttonStyle)) {
                var window = (BindHandWindow)EditorWindow.GetWindow(typeof(BindHandWindow));
                window.SetUp(ref myTarget);
                window.titleContent = new GUIContent("Binding Window");
                window.autoRepaintOnSceneChange = true;
                window.Show();
                window.minSize = new Vector2(600, 900);
            }
            EditorGUILayout.Space();
        }

        private void ShowBindingOptions() {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(handedness, new GUIContent("Hand Type", "Which hand does this binder target?"));
            EditorGUILayout.Space();
            setEditorPose.boolValue = GUILayout.Toggle(setEditorPose.boolValue, new GUIContent("Set Leap Editor Pose", "Should the Leap Editor Pose be used during Edit mode?"));
            useMetaBones.boolValue = GUILayout.Toggle(useMetaBones.boolValue, new GUIContent("Use Metacarpal Bones", "Does this binding require Metacarpal Bones?"));
            setPositions.boolValue = GUILayout.Toggle(setPositions.boolValue, new GUIContent("Set Bone Positions", "Does this binding require the positional leap data to be applied to the 3D model?"));
            EditorGUILayout.Space();

            EditorGUILayout.Space();
            GUILayout.Label(dividerLine);
            EditorGUILayout.Space();
        }

        private void ShowDebugOptions() {
            //Draw the debugging options
            debugOptions.boolValue = GUILayout.Toggle(debugOptions.boolValue, !debugOptions.boolValue ? "Show Debug Options" : "Hide Debug Options", subButtonStyle);
            EditorGUILayout.Space();
            if(debugOptions.boolValue) {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugLeapHand);
                if(debugLeapHand.boolValue) {
                    leapHandDebugCol = EditorGUILayout.ColorField(GUIContent.none, leapHandDebugCol, false, false, false);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(DebugLeapRotationAxis);
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(debugModelTransforms);
                if(debugModelTransforms.boolValue) {
                    handModelDebugCol = EditorGUILayout.ColorField(GUIContent.none, handModelDebugCol, false, false, false);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(DebugModelRotationAxis);
                EditorGUILayout.PropertyField(gizmoSize);
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        private void ShowFineTuningOptions() {
            //Draw the fine tuning options
            fineTuning.boolValue = GUILayout.Toggle(fineTuning.boolValue, !fineTuning.boolValue ? "Show Fine Tuning Options" : "Hide Fine Tuning Options", subButtonStyle);
            EditorGUILayout.Space();
            if(fineTuning.boolValue) {
                EditorGUILayout.Space();
                GUI.color = Color.white;

                //Draw the Calculated Offsets for the wrist and Fingers
                GUILayout.BeginVertical("Box");
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(wristRotationOffset, new GUIContent("Wrist Rotation Offset"));
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(globalFingerRotationOffset, new GUIContent("Fingers Rotation Offset"));
                GUI.color = previousCol;
                GUILayout.EndVertical();

                EditorGUILayout.Space();
                if(Selection.gameObjects.Length == 1 && GUILayout.Button("Auto Calculate Offsets")) {
                    if(EditorUtility.DisplayDialog("Auto Calculate Rotation Offsets",
                       "Are you sure you want to recalculate the rotation offsets?", "Yes", "No")) {

                    }
                    Undo.RegisterFullObjectHierarchyUndo(myTarget.gameObject, "Recalculate Offsets");
                    HandBinderAutoBinder.EstimateWristRotationOffset(myTarget);
                }
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();

                for(int offsetIndex = 0; offsetIndex < offsets.arraySize; offsetIndex++) {
                    SerializedProperty boundType = offsets.GetArrayElementAtIndex(offsetIndex);
                    BoundTypes previousBoundType = myTarget.offsets[offsetIndex];
                    SerializedProperty offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                    SerializedProperty offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                    SerializedProperty offsetPosition = offsetProperty.FindPropertyRelative("position");

                    GUILayout.BeginVertical("Box");
                    GUILayout.BeginHorizontal("Box");
                    EditorGUILayout.PropertyField(boundType, GUIContent.none);

                    if(GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"))) {
                        if(boundType.intValue != (int)BoundTypes.WRIST) {
                            offsetRotation.vector3Value = Vector3.zero;
                            offsetPosition.vector3Value = Vector3.zero;
                        }
                        offsets.DeleteArrayElementAtIndex(offsetIndex);
                        break;
                    }
                    GUILayout.EndHorizontal();

                    //Check to see if the user has changed the value
                    if((int)previousBoundType != boundType.intValue) {
                        //Check to see if any of the offsets are the same as this one
                        if(myTarget.offsets.Any(x => (int)x == boundType.intValue)) {
                            boundType.intValue = (int)previousBoundType;
                        }
                        else {
                            offsetRotation.vector3Value = Vector3.zero;
                            offsetPosition.vector3Value = Vector3.zero;
                            offsetProperty = BoundTypeToOffsetProperty((BoundTypes)boundType.intValue);
                            offsetRotation = offsetProperty.FindPropertyRelative("rotation");
                            offsetPosition = offsetProperty.FindPropertyRelative("position");
                        }
                    }

                    EditorGUILayout.PropertyField(offsetPosition);
                    EditorGUILayout.PropertyField(offsetRotation);
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();
                    GUILayout.Label(dividerLine);
                    EditorGUILayout.Space();
                }

                GUILayout.BeginHorizontal("Box");
                GUILayout.Label(new GUIContent("Add Bone Offset", "Add an extra offset for any bone"));
                if(GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"))) {
                    if(offsets.arraySize < 22) {
                        offsets.InsertArrayElementAtIndex(offsets.arraySize);
                        var offset = offsets.GetArrayElementAtIndex(offsets.arraySize - 1);

                        var enumList = new List<int>();

                        for(int i = 0; i < 22; i++) {
                            enumList.Add(i);
                        }

                        var result = enumList.Where(typeA => myTarget.offsets.All(typeB => (int)typeB != typeA)).FirstOrDefault();

                        offset.intValue = result;
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();
                GUILayout.Label(dividerLine);
                EditorGUILayout.Space();
            }
        }

        /// <summary>
        /// Convert a BoundType to the offsetProperty for the bound transform
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private SerializedProperty BoundTypeToOffsetProperty(BoundTypes boundType) {
            if(boundType == BoundTypes.WRIST) {
                return boundHand.FindPropertyRelative("wrist").FindPropertyRelative("offset");
            }
            else if(boundType == BoundTypes.ELBOW) {
                return boundHand.FindPropertyRelative("elbow").FindPropertyRelative("offset");
            }
            else if(!HandBinderUtilities.boundTypeMapping.ContainsKey(boundType)) {
                return null;
            }

            (Finger.FingerType fingerType, Bone.BoneType boneType) fingerBoneType = HandBinderUtilities.boundTypeMapping[boundType];
            return FingerOffsetPropertyFromLeapTypes(fingerBoneType.fingerType, fingerBoneType.boneType);
        }

        private SerializedProperty FingerOffsetPropertyFromLeapTypes(Finger.FingerType fingerType, Bone.BoneType boneType) {
            return boundHand.FindPropertyRelative("fingers").GetArrayElementAtIndex((int)fingerType).FindPropertyRelative("boundBones").GetArrayElementAtIndex((int)boneType).FindPropertyRelative("offset");
        }

        /// <summary>
        /// Draw extra gizmos in the scene to help the user while they edit variables
        /// </summary>
        private void OnSceneGUI() {
            //Update the editor pose, this will only get called when the object is selected.
            myTarget = (HandBinder)target;

            if(myTarget.LeapHand == null) {
                return;
            }

            //Draw the leap hand in the scene
            if(myTarget.DebugLeapHand) {

                Handles.color = leapHandDebugCol;
                foreach(var finger in myTarget.LeapHand.Fingers) {
                    var index = 0;

                    foreach(var bone in finger.bones) {
                        Handles.SphereHandleCap(-1, bone.PrevJoint.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
                        if((index + 1) <= finger.bones.Length - 1) {
                            Handles.DrawLine(finger.bones[index].PrevJoint.ToVector3(), finger.bones[index + 1].PrevJoint.ToVector3());
                        }

                        if(DebugLeapRotationAxis.boolValue) {
                            DrawLeapBasis(bone, myTarget.GizmoSize * 4);
                        }
                        index++;
                    }
                }
                Handles.SphereHandleCap(-1, myTarget.LeapHand.WristPosition.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Fingers[0].bones[0].PrevJoint.ToVector3());
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Fingers[1].bones[0].PrevJoint.ToVector3());
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Fingers[2].bones[0].PrevJoint.ToVector3());
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Fingers[3].bones[0].PrevJoint.ToVector3());
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Fingers[4].bones[0].PrevJoint.ToVector3());
                Handles.DrawLine(myTarget.LeapHand.WristPosition.ToVector3(), myTarget.LeapHand.Arm.PrevJoint.ToVector3());
                Handles.SphereHandleCap(-1, myTarget.LeapHand.Arm.PrevJoint.ToVector3(), Quaternion.identity, myTarget.GizmoSize, EventType.Repaint);
            }

            //Draw the bound Gameobjects
            if(myTarget.DebugModelTransforms) {
                Handles.color = handModelDebugCol;
                for(int finger = 0; finger < myTarget.boundHand.fingers.Length; finger++) {
                    for(int bone = 0; bone < myTarget.boundHand.fingers[finger].boundBones.Length; bone++) {
                        var target = myTarget.boundHand.fingers[finger].boundBones[bone].boundTransform;
                        if(target != null) {
                            if(myTarget.DebugModelTransforms) {
                                Handles.DrawWireDisc(target.position, target.right, myTarget.GizmoSize);
                                Handles.DrawWireDisc(target.position, target.up, myTarget.GizmoSize);
                                Handles.DrawWireDisc(target.position, target.forward, myTarget.GizmoSize);
                            }

                            if(DebugModelRotationAxis.boolValue) {
                                DrawTransformBasis(target, myTarget.GizmoSize * 4);
                            }
                        }
                    }
                }

                //Draw the wrist Gizmo
                if(myTarget.boundHand.wrist.boundTransform != null) {
                    var target = myTarget.boundHand.wrist.boundTransform;
                    Handles.DrawWireDisc(target.position, target.right, myTarget.GizmoSize);
                    Handles.DrawWireDisc(target.position, target.up, myTarget.GizmoSize);
                    Handles.DrawWireDisc(target.position, target.forward, myTarget.GizmoSize);
                }

                //Draw the wrist Gizmo
                if(myTarget.boundHand.elbow.boundTransform != null) {
                    var target = myTarget.boundHand.elbow.boundTransform;
                    Handles.DrawWireDisc(target.position, target.right, myTarget.GizmoSize);
                    Handles.DrawWireDisc(target.position, target.up, myTarget.GizmoSize);
                    Handles.DrawWireDisc(target.position, target.forward, myTarget.GizmoSize);
                }
            }
        }

        private void DrawLeapBasis(Leap.Bone bone, float size) {
            Vector3 middle, y, x, z;

            middle = bone.PrevJoint.ToVector3();
            y = bone.Basis.xBasis.ToVector3();
            x = bone.Basis.yBasis.ToVector3();
            z = bone.Basis.zBasis.ToVector3();

            Handles.color = Color.green;
            Handles.DrawLine(middle, middle + y.normalized * size);
            Handles.color = Color.red;
            Handles.DrawLine(middle, middle + x.normalized * size);
            Handles.color = Color.blue;
            Handles.DrawLine(middle, middle + z.normalized * size);
            Handles.color = leapHandDebugCol;
        }

        private void DrawTransformBasis(Transform bone, float size) {
            Vector3 middle, y, x, z;

            var prevCol = Handles.color;
            middle = bone.position;
            y = bone.up;
            x = bone.right;
            z = bone.forward;

            Handles.color = green;
            Handles.DrawLine(middle, middle + y.normalized * size);
            Handles.color = Color.red;
            Handles.DrawLine(middle, middle + x.normalized * size);
            Handles.color = Color.blue;
            Handles.DrawLine(middle, middle + z.normalized * size);
            Handles.color = prevCol;
        }

        public class BindHandWindow : EditorWindow {
            Texture mainButtonTexture, dividerLine;
            HandBinder handBinder;
            float spaceSize = 30f;
            Vector2 scrollPosition;
            GUISkin editorSkin;
            string previousUndoName;

            string message1 = "Reference the GameObjects you wish to use from the scene into the fields below, once assigned the dots above will appear green to show they are bound to tracking data.";
            string message2 = "Once you have assigned the bones you wish to use, the button below will attempt to calculate the rotational offsets needed to line the 3D Model hand with the tracking data.";
            public void SetUp(ref HandBinder handBinderRef) {
                handBinder = handBinderRef;
                mainButtonTexture = Resources.Load<Texture>("EditorDocumentationGreenUpstate");
                dividerLine = Resources.Load<Texture>("EditorDividerline");

                editorSkin = new GUISkin() {
                    label = new GUIStyle() {
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = true,
                        normal = new GUIStyleState() {
                            textColor = Color.white,
                        },
                        padding = new RectOffset(10, 10, 10, 10),
                    },
                    button = new GUIStyle("Button") {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = true,
                        normal = new GUIStyleState() {
                            background = (Texture2D)mainButtonTexture,
                        },
                        fontStyle = FontStyle.Bold,
                        fontSize = 20,
                    }
                };
            }

            //Closes the window if the user selects another gameobject with the Hand Binder on it
            private void OnSelectionChange() {

                if(Selection.activeTransform != null) {
                    var selectedHandBinder = Selection.activeTransform.GetComponent<HandBinder>();
                    if(selectedHandBinder != null && selectedHandBinder != handBinder) {
                        Close();
                    }
                }

                Repaint();
            }

            void OnGUI() {
                GUIHandGraphic.DrawHandGraphic(handBinder.handedness, GUIHandGraphic.FlattenHandBinderTransforms(handBinder));
                DrawAutoBindButton();
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                DrawObjectFields();
                GUILayout.EndScrollView();
                DrawRotationOffsets();
            }

            void DrawAutoBindButton() {
                if(GUILayout.Button(new GUIContent("Auto Bind", "Automatically try to search and bind the hand"), editorSkin.button, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth), GUILayout.MinHeight(spaceSize))) {
                    if(EditorUtility.DisplayDialog("Auto Bind",
                        "Are you sure you want to discard all your changes and run the Auto Bind process?", "Yes", "No")) {

                        Undo.RegisterFullObjectHierarchyUndo(handBinder, "AutoBind");
                        Undo.undoRedoPerformed += AutoRigUndo;
                        HandBinderAutoBinder.AutoBind(handBinder);
                        handBinder.UpdateHand();
                    }
                }

                GUILayout.Label(message1, editorSkin.label);
                GUILayout.Label(dividerLine);
            }

            public void AutoRigUndo() {
                Close();
                handBinder.ResetHand(true);
                Undo.undoRedoPerformed -= AutoRigUndo;
            }

            void DrawObjectFields() {
                //Draw a list of all the points of the hand that can be bound too
                GUILayout.Space(spaceSize);
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.BeginVertical();

                //Draw the wrist bone object field
                DrawObjectField("WRIST : ", ref handBinder.boundHand.wrist);
                GUILayout.Space(spaceSize);

                for(int fingerID = 0; fingerID < handBinder.boundHand.fingers.Length; fingerID++) {
                    for(int boneID = 0; boneID < handBinder.boundHand.fingers[fingerID].boundBones.Length; boneID++) {
                        if((Finger.FingerType)fingerID == Finger.FingerType.TYPE_THUMB && (Bone.BoneType)boneID == Bone.BoneType.TYPE_METACARPAL) {
                            continue;
                        }
                        var fingerType = ((Finger.FingerType)fingerID).ToString().Remove(0, 5).ToString();
                        var boneType = ((Bone.BoneType)boneID).ToString().Remove(0, 5).ToString();
                        //var boneType = (fingerID == 0 ? boneID - 1: boneID).ToString();

                        var objectFieldName = ((fingerType + " " + boneType + " :").ToString());
                        DrawObjectField(objectFieldName, ref handBinder.boundHand.fingers[fingerID].boundBones[boneID], true, fingerID, boneID);

                    }
                    GUILayout.Space(spaceSize);
                }

                //Draw the Elbow bone object field
                DrawObjectField("Elbow : ", ref handBinder.boundHand.elbow);
                GUILayout.Space(spaceSize);

                GUILayout.EndVertical();
                GUILayout.Space(20);
                GUILayout.EndHorizontal();
            }

            void DrawObjectField(string name, ref BoundBone boundBone, bool autoAssignChildren = false, int fingerID = 0, int boneID = 0) {
                GUILayout.BeginHorizontal();
                GUI.color = boundBone.boundTransform != null ? Color.green : Color.white;
                GUILayout.Label(name);
                GUI.color = Color.white;
                var newTransform = (Transform)EditorGUILayout.ObjectField(boundBone.boundTransform, typeof(Transform), true, GUILayout.MaxWidth(EditorGUIUtility.labelWidth * 2));
                if(newTransform != boundBone.boundTransform) {
                    Undo.RegisterFullObjectHierarchyUndo(handBinder, "Bound Object");
                    boundBone = HandBinderAutoBinder.AssignBoundBone(newTransform);

                    if(boundBone.boundTransform != null) {
                        if(autoAssignChildren) {
                            AutoAssignChildrenBones(newTransform, fingerID, boneID);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }


            private void AutoAssignChildrenBones(Transform newT, int fingerID, int boneID) {
                var firstChildList = new List<Transform>() { newT };
                firstChildList = GetFirstChildren(newT, ref firstChildList);
                for(int i = 0; i < firstChildList.Count; i++) {
                    if(boneID + i <= 3) {
                        handBinder.boundHand.fingers[fingerID].boundBones[boneID + i] = HandBinderAutoBinder.AssignBoundBone(firstChildList[i]);
                    }
                }
            }

            List<Transform> GetFirstChildren(Transform child, ref List<Transform> firstChildren) {
                if(child.childCount > 0) {
                    firstChildren.Add(child.GetChild(0));
                    return GetFirstChildren(child.GetChild(0), ref firstChildren);
                }

                else {
                    return firstChildren;
                }
            }

            void DrawRotationOffsets() {
                GUILayout.Label(dividerLine);
                GUILayout.Label(message2, editorSkin.label);
                if(GUILayout.Button("Calculate Rotation Offsets", editorSkin.button, GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth), GUILayout.MinHeight(spaceSize))) {
                    if(EditorUtility.DisplayDialog("Auto Calculate Rotation Offsets",
                       "Are you sure you want to recalculate the rotation offsets?", "Yes", "No")) {
                        Undo.RegisterFullObjectHierarchyUndo(handBinder.gameObject, "Recalculate Offsets");
                        HandBinderAutoBinder.EstimateWristRotationOffset(handBinder);
                        HandBinderAutoBinder.CalculateElbowLength(handBinder);
                        handBinder.SetEditorPose = true;
                        handBinder.UpdateHand();
                    }
                }
            }
        }

        public class GUIHandGraphic {

            static public Texture handTexture, dotTexture;
            static public Vector2[] handPoints = new Vector2[]
            {
                //Thumb
                new Vector2(-20.9F, 51),
                new Vector2(-25.6f, 53.9f),
                new Vector2(-60.3f, 100.9f),
                new Vector2(-94.2f, 146.9f),
                
                //Index
                new Vector2(-7.1f, 89.37f),
                new Vector2(-2, 151.19f),
                new Vector2(-0.2f, 190.37f),
                new Vector2(0.9f, 229.8f),

                //Middle
                new Vector2(17.5f, 99.4f),
                new Vector2(32.2f, 149.5f),
                new Vector2(41.3f, 185.7f),
                new Vector2(51.6f, 229.2f),

                //Ring
                new Vector2(33.2f, 82.3f),
                new Vector2(58.6f, 132.6f),
                new Vector2(76.7f, 166.2f),
                new Vector2(91.3f, 200f),

                //Pinky
                new Vector2(39.6f, 53.9f),
                new Vector2(75.4f, 98.6f),
                new Vector2(103, 119),
                new Vector2(125, 138.01f),

                //Wrist
                new Vector2(0, 0),
            };

            //Turn the bound handBinder bones into a flattened array
            static public Transform[] FlattenHandBinderTransforms(HandBinder handBinder) {
                var bones = new List<Transform>();
                int index = 0;
                for(int FINGERID = 0; FINGERID < handBinder.boundHand.fingers.Length; FINGERID++) {
                    for(int BONEID = 0; BONEID < handBinder.boundHand.fingers[FINGERID].boundBones.Length; BONEID++) {
                        var BONE = handBinder.boundHand.fingers[FINGERID].boundBones[BONEID];
                        bones.Add(BONE.boundTransform);
                        index++;
                    }
                    index++;
                }
                bones.Add(handBinder.boundHand.wrist.boundTransform);
                return bones.ToArray();

            }
            static public void SetUp() {
                handTexture = Resources.Load<Texture>("EditorHand");
                dotTexture = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo").image;
            }

            static public void DrawHandGraphic(Chirality handedness) {
                if(handTexture == null || dotTexture == null) {
                    SetUp();
                }

                var midPoint = Screen.width / 2;
                var middleYOffset = 50;

                //Draw the hand texture
                var handTextureRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);
                if(handedness == Chirality.Left) {
                    handTextureRect.x -= handTexture.width / 2;
                }
                else {
                    handTextureRect.x += handTexture.width / 2;
                    handTextureRect.size = new Vector2(-handTextureRect.size.x, handTextureRect.size.y);
                }

                GUI.DrawTextureWithTexCoords(handTextureRect, handTexture, new Rect(0, 0, 1, 1));

                var index = 0;
                for(int FINGERID = 0; FINGERID < 5; FINGERID++) {
                    for(int BONEID = 0; BONEID < 4; BONEID++) {
                        if(BONEID == 0) {
                            index++;
                            continue;
                        }

                        GUI.color = Color.green;
                        var pointRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);

                        if(handedness == Chirality.Left) {
                            pointRect.center -= handPoints[index];
                        }
                        else {
                            var offset = handPoints[index] + Vector2.left * 25;
                            pointRect.center += new Vector2(offset.x, -offset.y);
                        }

                        GUI.DrawTextureWithTexCoords(pointRect, dotTexture, new Rect(0, 0, 11f, 11f));
                        GUI.color = Color.white;
                        index++;
                    }
                }

                //Draw the wrist point
                GUI.color = Color.green;
                var pRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);
                ;
                if(handedness == Chirality.Left) {
                    pRect.center -= handPoints[index];
                }
                else {
                    var offset = handPoints[index] + Vector2.left * 25;
                    pRect.center += new Vector2(offset.x, -offset.y);
                }

                pRect.center -= (handPoints[index]);
                GUI.DrawTextureWithTexCoords(pRect, dotTexture, new Rect(-.05f, 0, 11f, 11f));
                GUI.color = Color.white;
                GUILayout.Space(handTexture.height * 1.25f);
            }

            static public void DrawHandGraphic(Chirality handedness, Transform[] bones = null) {
                if(handTexture == null || dotTexture == null) {
                    SetUp();
                }

                var midPoint = Screen.width / 2;
                var middleYOffset = 50;

                //Draw the hand texture
                var handTextureRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);
                if(handedness == Chirality.Left) {
                    handTextureRect.x -= handTexture.width / 2;
                }
                else {
                    handTextureRect.x += handTexture.width / 2;
                    handTextureRect.size = new Vector2(-handTextureRect.size.x, handTextureRect.size.y);
                }

                GUI.DrawTextureWithTexCoords(handTextureRect, handTexture, new Rect(0, 0, 1, 1));

                for(int boneID = 0; boneID < bones.Length; boneID++) {
                    if(boneID == 0) {
                        continue;
                    }

                    var bone = bones[boneID];
                    var isSelectedOrHovered = Selection.activeTransform == bone;

                    var pointRect = new Rect(midPoint, middleYOffset, handTexture.width, handTexture.height);

                    if(handedness == Chirality.Left) {
                        pointRect.center -= handPoints[boneID];
                    }
                    else {
                        var offset = handPoints[boneID] + Vector2.left * 25;
                        pointRect.center += new Vector2(offset.x, -offset.y);
                    }

                    GUI.color = bone != null ? Color.green : Color.grey;
                    GUI.DrawTextureWithTexCoords(pointRect, isSelectedOrHovered ? EditorGUIUtility.IconContent("DotFrameDotted").image : dotTexture, new Rect(0, 0, 11f, 11f));
                    GUI.color = Color.white;
                }

                GUILayout.Space(handTexture.height * 1.25f);
            }
        }
    }
}