﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine.UI;

namespace Leap.Unity {
  public class WristLeapToIKBlend : HandTransitionBehavior {
    [Leap.Unity.Attributes.AutoFind]
    private Animator animator;
    private HandModel handModel;

    private Vector3 startingPalmPosition;
    private Quaternion startingOrientation;
    private Transform palm;

    private Vector3 armDirection;
    private Vector3 PalmPositionAtLateUpdate;
    private Quaternion PalmRotationAtLateUpdate;
    private float positionIKWeight;
    private float rotationIKWeight;
    private float positionIKTargetWeight;
    private float rotationIKTargetWeight;
    private float elbowIKWeight;
    private float elbowIKTargetWeight;

    private Transform Hips;
    private Transform Scapula;
    private Transform Shoulder;
    private Transform Elbow;
    private Transform Neck;
    private Transform Head;
    private float upperArmLength;
    private float shoulder_up_target_weight;
    private float shoulder_up_weight;
    private float shoulder_forward_weight;
    private float shoulder_forward_target_weight;
    private float shoulder_back_weight;
    private float shoulder_back_target_weight;

    private float shouldersLayerWeight;
    private float shouldersLayerTargetWeight;
    private float spineLayerWeight;
    private float spineLayerTargetWeight;

    private Vector3 UntrackedIKPosition;
    [HideInInspector]
    public bool isTracking;
    private Transform characterRoot;
    private float distanceShoulderToPalm;

    private Vector3 previousPalmPosition;
    private Vector3 iKVelocity;
    public Transform VelocityMarker;
    private Vector3 lastTrackedPosition;
    private Vector3 iKVelocitySnapShot;
    private Queue<Vector3> velocityList = new Queue<Vector3>();
    private Vector3 averageIKVelocity;
    private bool isLerping;

    public Chirality Handedness;
    public GameObject MarkerPrefab;
    public Transform ElbowMarker;
    public Transform ElbowIKTarget;
    public float ElbowOffset = -0.5f;
    public Transform RestIKPosition;
    public Transform ShoulderRestPos;
    public AnimationCurve DropCurveX;
    public AnimationCurve DropCurveY;
    public AnimationCurve DropCurveZ;
    public float ArmDropDuration = 1.5f;

    public IKMarkersAssembly m_IKMarkerAssembly;

    [Leap.Unity.Attributes.AutoFind]
    public LeapHandController leapHandController;
    public Text ZvalueText;
    public Text twistText;
    public Text outText;

    public bool shrugShoulders = false;
    public bool FreezeOnFinish;
    [Range(.01f, .05f)]
    public float ElbowDamp = .01f;

    protected override void Awake() {
      base.Awake();
      animator = transform.root.GetComponentInChildren<Animator>();
      leapHandController = GameObject.FindObjectOfType<LeapHandController>();
      characterRoot = animator.transform;
      handModel = transform.GetComponent<HandModel>();
      palm = GetComponent<HandModel>().palm;
      Neck = animator.GetBoneTransform(HumanBodyBones.Neck);
      Head = animator.GetBoneTransform(HumanBodyBones.Head);
      Hips = animator.GetBoneTransform(HumanBodyBones.Hips);


      if(Handedness == Chirality.Left){
        Scapula = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
      }
      if (Handedness == Chirality.Right) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
      }
      HandFinish();
      upperArmLength = Vector3.Distance(Shoulder.position, Elbow.position);
    }

    public void AssignIKMarkers() {
      if (Handedness == Chirality.Left) {
        Debug.Log(transform.name + " - Left");
        ElbowMarker = m_IKMarkerAssembly.ElbowMarker_L;
        ElbowIKTarget = m_IKMarkerAssembly.ElbowIKTarget_L;
        RestIKPosition = m_IKMarkerAssembly.RestIKPosition_L;
        VelocityMarker = m_IKMarkerAssembly.VelocityMarker_L;
      }
      if (Handedness == Chirality.Right) {
        Debug.Log(transform.name + " - Right");
        ElbowMarker = m_IKMarkerAssembly.ElbowMarker_R;
        ElbowIKTarget = m_IKMarkerAssembly.ElbowIKTarget_R;
        RestIKPosition = m_IKMarkerAssembly.RestIKPosition_R;
        VelocityMarker = m_IKMarkerAssembly.VelocityMarker_R;
      }
      DropCurveX = m_IKMarkerAssembly.DropCurveX;
      DropCurveY = m_IKMarkerAssembly.DropCurveY;
      DropCurveZ = m_IKMarkerAssembly.DropCurveZ;
    }

    protected override void HandFinish() {
      if (!FreezeOnFinish) {
        isTracking = false;
      }
      positionIKTargetWeight = 1;
      elbowIKTargetWeight = 1;
      rotationIKWeight = 0;
      shouldersLayerTargetWeight = 0f;
      spineLayerTargetWeight = 1f;

      //snapshot and constrain velocity derived
      iKVelocitySnapShot = averageIKVelocity;
      iKVelocitySnapShot = iKVelocitySnapShot * .3f;// scale the velocity so arm doesn't reach as far;
      VelocityMarker.position = iKVelocitySnapShot;
      //StartCoroutine(DropWithVelocity(palm.position));
      lastTrackedPosition = palm.position;      
    }
    protected override void HandReset() {
      isTracking = true;
      StopAllCoroutines();
      positionIKTargetWeight = 1;
      elbowIKTargetWeight = 1;
      rotationIKWeight = 1;
      shouldersLayerTargetWeight = 1f;
      spineLayerTargetWeight = 1f;
    }


    void LateUpdate() {
      if (isTracking) {
        // Wrist Twists
        CalculateWristTwists();
        //Keep Average Velocity for hand drops 
        CalculateAvergeHandVelocity();
      }
    
      //get Arm Directions and set elbow target position 
      CalculateElbowIKTargetPos();

      // Update Elbow markers 
      ElbowMarker.position = Elbow.position;
      ElbowMarker.rotation = Elbow.rotation;
      //...and palm values for OnAnimatorIK
      PalmPositionAtLateUpdate = palm.position;
      PalmRotationAtLateUpdate = palm.rotation;

      //Lerp all muscle weights
      LerpMuscleWeights();
    }

    public void CalculateWristTwists() {
      Quaternion spacedRot = Quaternion.Inverse(leapHandController.transform.rotation) * (handModel.GetLeapHand().Rotation).ToQuaternion();
      float handRotationZ = spacedRot.z;
      if (ZvalueText != null) {
        ZvalueText.text = "Rot Z: " + handRotationZ.ToString("F2");
      }
      if (Handedness == Chirality.Left) {
        //if (handRotationZ > 0) {
          animator.SetFloat("forearm_twist_out_left",  handRotationZ);
        //}
        if (handRotationZ < 0) {
          animator.SetFloat("forearm_twist_left", (1 - Mathf.Abs(handRotationZ)) * 2f);
        }
        if (twistText != null && outText != null) {
          twistText.text = "Twist: " + animator.GetFloat("forearm_twist_left").ToString("F2");
          outText.text = "Out: " + animator.GetFloat("forearm_twist_out_left").ToString("F2");
        }
      }
      if (Handedness == Chirality.Right) {
        if (handRotationZ > 0) {
          animator.SetFloat("forearm_twist_right", (1 - handRotationZ) * 2f);
        }
        //if (handRotationZ < 0) {
          animator.SetFloat("forearm_twist_out_right", Mathf.Abs(handRotationZ));
        //}
        if (twistText != null && outText != null) {
          twistText.text = "Twist: " + animator.GetFloat("forearm_twist_right").ToString("F2");
          outText.text = "Out: " + animator.GetFloat("forearm_twist_out_right").ToString("F2");
        }
      }
    }
    
    public void CalculateElbowIKTargetPos() {
      armDirection = handModel.GetArmDirection();
      Vector3 ElbowTargetPosition = characterRoot.InverseTransformPoint(palm.position + (armDirection * ElbowOffset));
      Vector3 palmInAnimatorSpace = characterRoot.InverseTransformPoint(PalmPositionAtLateUpdate);
      Vector3 shoulderInAnimatorSpace = characterRoot.InverseTransformDirection(Shoulder.position);
      distanceShoulderToPalm = (palm.position - Shoulder.transform.position).magnitude;
      //Rule 0: pull down targets slightly if head is looking down
      if (Head.transform.localRotation.z < .15) {
        ElbowTargetPosition.y -= (.15f - Head.transform.localRotation.z) * 1f;
      }
      //turn off elbow hint if hand close to shoulder
      if (distanceShoulderToPalm < .1f) {
        elbowIKTargetWeight = 0;
      }
      if (Handedness == Chirality.Left) {
        //Rule 1: Move elbow target out as palm approaches shoulder to control flipping
        ElbowTargetPosition.x -= distanceShoulderToPalm * .2f;
        //Rule 2: If palm goes behind controller move elbow target out (happens when hand drops)
        if (palmInAnimatorSpace.z < 0) {
          ElbowTargetPosition.x += palmInAnimatorSpace.z * 10;
          //Rule 2.5: 
          if (ElbowTargetPosition.y > shoulderInAnimatorSpace.y) {
            ElbowTargetPosition.y = shoulderInAnimatorSpace.y;
          }
        }
      }
      if (Handedness == Chirality.Right) {
        //Rule 1: Move elbow target out as palm approaches shoulder to control flipping
        ElbowTargetPosition.x += distanceShoulderToPalm * .2f;
        //Rule 2: If palm goes behind controller move elbow target out (happens when hand drops)
        if (palmInAnimatorSpace.z < 0) {
          ElbowTargetPosition.x -= palmInAnimatorSpace.z * 10;
          //Rule 2.5: 
          if (ElbowTargetPosition.y > shoulderInAnimatorSpace.y) {
            ElbowTargetPosition.y = shoulderInAnimatorSpace.y;
          }
        }
      }
      //Rule 3:  Prevent elbow targets from crossing body midpoint
      if (Handedness == Chirality.Left && ElbowTargetPosition.x > -.05f) {
        ElbowTargetPosition.x = -.1f;
      }
      if (Handedness == Chirality.Right && ElbowTargetPosition.x < .05f) {
        ElbowTargetPosition.x = .1f;
      }
      if (isTracking) {
        ElbowIKTarget.position = characterRoot.TransformPoint(ElbowTargetPosition);
      }
      else ElbowIKTarget.position = Vector3.Lerp(ElbowIKTarget.position, characterRoot.TransformPoint(ElbowTargetPosition), ElbowDamp);
    }

    public void CalculateAvergeHandVelocity() {
      iKVelocity = (palm.position - previousPalmPosition) / Time.deltaTime;
      if (velocityList.Count >= 3) {
        velocityList.Dequeue();
      }
      if (velocityList.Count < 3) {
        velocityList.Enqueue(iKVelocity);
      }
      averageIKVelocity = new Vector3(0, 0, 0);
      foreach (Vector3 v in velocityList) {
        averageIKVelocity += v;
      }
      averageIKVelocity = (averageIKVelocity / 3);
      previousPalmPosition = palm.position;

    }

    public void LerpMuscleWeights() {
      shoulder_up_weight = Mathf.Lerp(shoulder_up_weight, shoulder_up_target_weight, .3f);
      shoulder_forward_weight = Mathf.Lerp(shoulder_forward_weight, shoulder_forward_target_weight, .05f);
      shoulder_back_weight = Mathf.Lerp(shoulder_back_weight, shoulder_back_target_weight, .05f);
      positionIKWeight = Mathf.Lerp(positionIKWeight, positionIKTargetWeight, .4f);
      rotationIKWeight = Mathf.Lerp(rotationIKWeight, rotationIKTargetWeight, .4f);
      elbowIKWeight = Mathf.Lerp(elbowIKWeight, elbowIKTargetWeight, .4f);
      shouldersLayerWeight = Mathf.Lerp(shouldersLayerWeight, shouldersLayerTargetWeight, .1f);

      if (Handedness == Chirality.Left) {
        animator.SetLayerWeight(2, shouldersLayerWeight);
      }
      if (Handedness == Chirality.Right) {
        animator.SetLayerWeight(3, shouldersLayerWeight);
      }
    }

    public void OnAnimatorIK(int layerIndex) {
      if (isTracking) {
        CalculateShoulderMuscles();
        TrackedIKHandling();
      }
      else {
        UntrackedIKHandling();
      }
    }
    float shrugWeight = 0;

    //Apply Shoulder Rigging Rules
    public void CalculateShoulderMuscles() {
      Vector3 elbow = characterRoot.InverseTransformPoint(ElbowMarker.position);
      Vector3 scapula = characterRoot.InverseTransformPoint(Scapula.position);
      Vector3 shoulder = characterRoot.InverseTransformPoint(Shoulder.position);
      float elbowToShoulder = 0;
      if (ShoulderRestPos) {
        elbowToShoulder = Vector3.Distance(handModel.GetElbowPosition(), ShoulderRestPos.position);
      }

      //raise shoulder as elbow goes above shoulder
      if (elbow.y > scapula.y) {
        //if (((elbow.y - shoulder.y) * 10f) > shoulder_up_target_weight)
        if (((elbow.y - shoulder.y) * 10f > shrugWeight)) {
          shoulder_up_target_weight = (elbow.y - shoulder.y) * 10f;
        }
      }
      //Shrug shoulders if elbow closer to should than length of upperArm
      else if (ShoulderRestPos && elbowToShoulder < upperArmLength * .7f && shrugShoulders) {
        shrugWeight = Mathf.Lerp(shrugWeight, (upperArmLength - elbowToShoulder) * 8.0f, .2f);
        if (shrugWeight > shoulder_up_target_weight) {
          shoulder_up_target_weight = shrugWeight;
        }
      }
      else {
        shrugWeight = Mathf.Lerp(shrugWeight, 0f, .05f);
        shoulder_up_target_weight = Mathf.Lerp(shoulder_up_target_weight, 0f, .05f);
      }
      //move shoulder back when hand close to shoulder
      if (distanceShoulderToPalm < .2f) {
        shoulder_back_target_weight = 5 - distanceShoulderToPalm * 2;
      }
      else shoulder_back_target_weight = 0;
      //bring shouler forward as elbow comes close to center
      if (Handedness == Chirality.Left && elbow.x > shoulder.x || Handedness == Chirality.Right && elbow.x < shoulder.x) {
        shoulder_forward_target_weight = Mathf.Abs(elbow.x - shoulder.x * 20f);
      }
      else {
        shoulder_forward_target_weight = 0.0f;
      }
    }

    private void TrackedIKHandling() {
      if (Handedness == Chirality.Left) {
        animator.SetFloat("shoulder_up_left", shoulder_up_weight);
        shoulder_forward_target_weight += distanceShoulderToPalm * .5f;
        //animator.SetFloat("shoulder_forward_left", shoulder_forward_weight);
        animator.SetFloat("shoulder_back_left", shoulder_back_weight);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.LeftHand, PalmPositionAtLateUpdate);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight * .25f);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
      }
      if (Handedness == Chirality.Right) {
        animator.SetFloat("shoulder_up_right", shoulder_up_weight);
        shoulder_forward_target_weight += distanceShoulderToPalm * .5f;
        //animator.SetFloat("shoulder_forward_right", shoulder_forward_weight);
        animator.SetFloat("shoulder_back_right", shoulder_back_weight);
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
        animator.SetIKPosition(AvatarIKGoal.RightHand, PalmPositionAtLateUpdate);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight * .25f);
        animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, elbowIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
      }
    }

    private void UntrackedIKHandling() {
      if (Handedness == Chirality.Left) {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, positionIKWeight);
        if (isLerping) {
          animator.SetIKPosition(AvatarIKGoal.LeftHand, UntrackedIKPosition);
        }
        else {
          animator.SetIKPosition(AvatarIKGoal.LeftHand, RestIKPosition.position);
        }
        animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.LeftHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, elbowIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.LeftElbow, ElbowIKTarget.position);
        animator.SetFloat("forearm_twist_left", 0);
        animator.SetFloat("forearm_twist_out_left", 0);
      }
      if (Handedness == Chirality.Right) {
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, positionIKWeight);
        if (isLerping) {
          animator.SetIKPosition(AvatarIKGoal.RightHand, UntrackedIKPosition);
        }
        else {
          animator.SetIKPosition(AvatarIKGoal.RightHand, RestIKPosition.position);
        }        
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rotationIKWeight);
        animator.SetIKRotation(AvatarIKGoal.RightHand, PalmRotationAtLateUpdate);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, elbowIKWeight);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow, ElbowIKTarget.position);
        animator.SetFloat("forearm_twist_right", 0);
        animator.SetFloat("forearm_twist_out_right", 0);
      }
      if (!isTracking && Handedness == Chirality.Left) {
        Debug.DrawRay(lastTrackedPosition, iKVelocitySnapShot, Color.blue);
      }
      if (!isTracking && Handedness == Chirality.Right) {
        Debug.DrawRay(lastTrackedPosition, iKVelocitySnapShot, Color.green);
      }
    }


    private IEnumerator DropWithVelocity(Vector3 startPosition){
      float magnitude = averageIKVelocity.magnitude;
      Ray velocityRay = new Ray(startPosition, iKVelocitySnapShot);
      Vector3 projectedVelocity = velocityRay.GetPoint(magnitude * .75f);
      Vector3 localVelocity = characterRoot.InverseTransformPoint(projectedVelocity);

      isLerping = true;
      UntrackedIKPosition = startPosition;
      float startTime = Time.time;
      float speed = magnitude * .01f;
      if (speed < .01f ) {
        speed = .01f;
      }
      //push targets forward if tracking lost over head to avoid arm flipping
      if (localVelocity.z < .2f && localVelocity.y > 1.5f) {
        projectedVelocity = projectedVelocity + (characterRoot.forward * 2f);
        ArmDropDuration += .2f;
      }
      float endTime = startTime + ArmDropDuration * magnitude;

      while (Time.time <= endTime) {
        float t = (Time.time - startTime) / ArmDropDuration;
        float lerpedPositionX = Mathf.Lerp(projectedVelocity.x, RestIKPosition.position.x, DropCurveX.Evaluate(t * 2));
        float lerpedPositionY = Mathf.Lerp(projectedVelocity.y, RestIKPosition.position.y, DropCurveY.Evaluate(t * 2));
        float lerpedPositionZ = Mathf.Lerp(projectedVelocity.z, RestIKPosition.position.z, DropCurveZ.Evaluate(t * 2));
        Vector3 newMarkerPosition = new Vector3(lerpedPositionX, lerpedPositionY, lerpedPositionZ);
        //Keep target in front of hips
        if (characterRoot.InverseTransformPoint(newMarkerPosition).z < characterRoot.InverseTransformPoint(Hips.position).z + .5f) {
          if (Handedness == Chirality.Left && characterRoot.InverseTransformPoint(newMarkerPosition).x > characterRoot.InverseTransformPoint(Hips.position).x + .2f) {
            newMarkerPosition.z = characterRoot.InverseTransformPoint(Hips.position).z + .5f;
          }
          if (Handedness == Chirality.Right && characterRoot.InverseTransformPoint(newMarkerPosition).x < characterRoot.InverseTransformPoint(Hips.position).x + .2f) {
            newMarkerPosition.z = characterRoot.InverseTransformPoint(Hips.position).z + .5f;
          }
        }
        VelocityMarker.position = newMarkerPosition;
        UntrackedIKPosition = Vector3.MoveTowards(UntrackedIKPosition, VelocityMarker.position, speed);
        yield return null;
      }
      isLerping = false;
    }

    public override void OnSetup() {
      Awake();
      MarkerPrefab = Resources.Load("RuntimeGizmoMarker") as GameObject;
      Debug.Log("MarkerPrefab: " + MarkerPrefab);
      Handedness = GetComponent<IHandModel>().Handedness;
      if (Handedness == Chirality.Left) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
      }
      if (Handedness == Chirality.Right) {
        Scapula = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
        Shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        Elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
      }
      AssignIKMarkers();
    }
  }
}
