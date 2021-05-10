/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap.Unity;

public class CycleHandPairs : MonoBehaviour {
  public HandModelManager HandPool;
  public string[] GroupNames;
  private int currentGroup;
  public int CurrentGroup {
    get { return currentGroup; }
    set {
      disableAllGroups();
      currentGroup = value;
      HandPool.EnableGroup(GroupNames[value]);
    }
  }

  // Use this for initialization
  void Start () {
    HandPool = GetComponent<HandModelManager>();
    disableAllGroups();
    CurrentGroup = 0;
  }
  
  // Update is called once per frame
  void Update () {
    if (Input.GetKeyUp(KeyCode.RightArrow)) {
      if (CurrentGroup < GroupNames.Length - 1) {
        CurrentGroup++;
      }
    }
    if (Input.GetKeyUp(KeyCode.LeftArrow)) {
      if (CurrentGroup > 0) {
        CurrentGroup--;
      }
    }
  }

  private void disableAllGroups() {
    for (int i = 0; i < GroupNames.Length; i++) {
      HandPool.DisableGroup(GroupNames[i]);
    }
  }

}
