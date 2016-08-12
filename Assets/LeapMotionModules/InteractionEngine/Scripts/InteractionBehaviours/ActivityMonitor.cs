﻿using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Interaction {

  public abstract class IActivityMonitor : MonoBehaviour {
    public abstract void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager);
    public abstract void Revive();
    public abstract void UpdateState();

    public int arrayIndex;
  }

  public class ActivityMonitorLite : IActivityMonitor, IRuntimeGizmoComponent {
    public static GizmoType gizmoType = GizmoType.ActivityDepth;

    public const float EXPLOSION_VELOCITY = 100;
    public const int HYSTERESIS_TIMEOUT = 5;

    protected Rigidbody _rigidbody;
    protected IInteractionBehaviour _interactionBehaviour;
    protected ActivityManager _manager;

    // For explosion protection
    protected Vector3 _prevPosition;
    protected Vector3 _prevVelocity;
    protected Vector3 _prevAngularVelocity;

    protected int _timeToLive = 1;
    protected int _timeToDie = 0;  // Timer after _timeToLive goes negative before deactivation.

    public override void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();

      _rigidbody = GetComponent<Rigidbody>();
    }

    public override void Revive() {
      _timeToLive = 1;
    }

    public override void UpdateState() {
      if (_rigidbody.isKinematic) {
        if ((_rigidbody.position - _prevPosition).sqrMagnitude / Time.fixedDeltaTime >= EXPLOSION_VELOCITY * EXPLOSION_VELOCITY) {
          _rigidbody.velocity = _prevVelocity;
          _rigidbody.angularVelocity = _prevAngularVelocity;
          _rigidbody.position = _rigidbody.position + _rigidbody.velocity * Time.fixedDeltaTime;
          _rigidbody.rotation = _rigidbody.rotation;
        }
      }

      _prevPosition = _rigidbody.position;
      _prevVelocity = _rigidbody.velocity;
      _prevAngularVelocity = _rigidbody.angularVelocity;

      // Grasped objects do not intersect the brush layer but are still touching hands.
      if (_interactionBehaviour.IsBeingGrasped) {
        Revive();
        return;
      }

      if (_timeToLive > 0) {
        --_timeToLive;
        _timeToDie = 0;
      } else {
        if (_interactionBehaviour.IsAbleToBeDeactivated() && ++_timeToDie >= HYSTERESIS_TIMEOUT) {
          _manager.Deactivate(_interactionBehaviour);
        }
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      switch (gizmoType) {
        case GizmoType.InteractionStatus:
          if (_interactionBehaviour.IsBeingGrasped) {
            drawer.color = Color.green;
          } else if (GetComponent<Rigidbody>().IsSleeping()) {
            drawer.color = Color.gray;
          } else {
            drawer.color = Color.blue;
          }
          break;
        case GizmoType.ActivityDepth:
          drawer.color = Color.HSVToRGB(Mathf.Max(0, _timeToLive) / (_manager.MaxDepth * 2.0f), 1, 1);
          break;
      }

      drawer.DrawColliders(gameObject);
    }

    public enum GizmoType {
      InteractionStatus,
      ActivityDepth
    }
  }

  public class ActivityMonitor : ActivityMonitorLite {

    public override void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      base.Init(interactionBehaviour, manager);

      bool wasSleeping = _rigidbody.IsSleeping();

      //We need to do this in order to force Unity to reconsider collision callbacks for this object
      //Otherwise scripts added in the middle of a collision never recieve the Stay callbacks.
      Collider singleCollider = GetComponentInChildren<Collider>();
      if (singleCollider != null) {
        Physics.IgnoreCollision(singleCollider, singleCollider, true);
        Physics.IgnoreCollision(singleCollider, singleCollider, false);
      }

      if (wasSleeping) {
        _rigidbody.Sleep();
      }
    }

    void OnCollisionEnter(Collision collision) {
      handleCollision(collision);
    }

    void OnCollisionStay(Collision collision) {
      handleCollision(collision);
    }

    private void handleCollision(Collision collision) {
      IInteractionBehaviour otherBehaviour = null;
      ActivityMonitor neighbor = collision.gameObject.GetComponent<ActivityMonitor>();
      if (neighbor != null) {
        if (arrayIndex > neighbor.arrayIndex) {
          return; // Only need to do this on one side of a pair.
        }

        otherBehaviour = neighbor._interactionBehaviour;
      } else {
        if (_timeToLive <= 1) {
          return; // Do not activate neighbor.
        }

        otherBehaviour = collision.gameObject.GetComponent<IInteractionBehaviour>();
        if (otherBehaviour == null) {
          return;
        }

        // Unregistered behaviours will fail to activate.
        neighbor = _manager.Activate(otherBehaviour) as ActivityMonitor;
        if (neighbor != null) {
          neighbor._timeToLive = _timeToLive - 1;
        }
        return;
      }

      // Allow different managers.
      if (!_manager.IsRegistered(otherBehaviour)) {
        return;
      }

      // propagate both ways
      int nextTime = ((_timeToLive > neighbor._timeToLive) ? _timeToLive : neighbor._timeToLive) - 1;
      if (_timeToLive < nextTime) {
        _timeToLive = nextTime;
      } else if (neighbor._timeToLive < nextTime) {
        neighbor._timeToLive = nextTime;
      }
    }
  }
}
