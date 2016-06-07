﻿using UnityEngine;

namespace Leap.Unity.Interaction {

  public class PhysicsDriverKinematic : IPhysicsDriver {

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.Rigidbody.position = solvedPosition;
        _obj.Rigidbody.rotation = solvedRotation;
      } else {
        _obj.Rigidbody.MovePosition(solvedPosition);
        _obj.Rigidbody.MoveRotation(solvedRotation);
      }
    }

    public override void OnGrasp() {
      _obj.Rigidbody.isKinematic = true;
    }

    public override void OnUngrasp() { }
  }
}
