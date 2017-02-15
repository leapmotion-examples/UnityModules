﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsCallbacks : MonoBehaviour {

  public static PhysicsCallbacks _instance;
  public static PhysicsCallbacks Provider {
    get {
      if (_instance == null) {
        _instance = ConstructSingleton();
      }
      return _instance;
    }
  }

  private static PhysicsCallbacks ConstructSingleton() {
    GameObject parent = new GameObject("Physics Callbacks Provider");
    parent.transform.position = new Vector3(-10000F, -10000F, -10000F);

    GameObject trigger0 = new GameObject("OnPostPhysics Trigger 0");
    trigger0.transform.parent = parent.transform;
    trigger0.transform.localPosition = Vector3.zero;
    var body = trigger0.AddComponent<Rigidbody>();
    body.isKinematic = true;
    var box = trigger0.AddComponent<BoxCollider>();
    box.isTrigger = true;

    GameObject trigger1 = Instantiate<GameObject>(trigger0);
    trigger1.name = "OnPostPhysics Trigger 1";
    trigger1.transform.parent = parent.transform;
    trigger1.transform.localPosition = Vector3.zero;

    PhysicsCallbacks postPhysicsTrigger = trigger0.AddComponent<PhysicsCallbacks>();
    return postPhysicsTrigger;
  }

  public Action OnPrePhysics  = () => { };
  public Action OnPostPhysics = () => { };

  void FixedUpdate() {
    OnPrePhysics();
  }

  void OnTriggerStay() {
    OnPostPhysics();
  }

}
