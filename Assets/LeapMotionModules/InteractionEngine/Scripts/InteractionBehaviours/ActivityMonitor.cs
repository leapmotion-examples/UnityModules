﻿using UnityEngine;
using Leap.Unity.RuntimeGizmos;
using System;

namespace Leap.Unity.Interaction {

  public interface IActivityMonitor {
    void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager);
    void Revive();
    void UpdateState();

    int arrayIndex { get; set; }
  }

  public class ActivityMonitorLite : IActivityMonitor {
    public const int HYSTERESIS_TIMEOUT = 5;

    public int arrayIndex { get; set; }

    private IInteractionBehaviour _interactionBehaviour;
    private ActivityManager _manager;
    private int _timeToLive = 1;

    public void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
    }

    public void Revive() {
      _timeToLive = 1;
    }

    public void UpdateState() {
      _timeToLive--;
      if (_interactionBehaviour.IsAbleToBeDeactivated() && _timeToLive == -HYSTERESIS_TIMEOUT) {
        _manager.Deactivate(_interactionBehaviour);
      }
    }
  }

  public class ActivityMonitor : MonoBehaviour, IActivityMonitor, IRuntimeGizmoComponent {
    public const int HYSTERESIS_TIMEOUT = 5;

    public enum GizmoType {
      InteractionStatus,
      ActivityDepth
    }

    public static GizmoType gizmoType = GizmoType.ActivityDepth;

    // Caches index into ActivityManager array.
    public int arrayIndex { get; set; }

    private IInteractionBehaviour _interactionBehaviour;
    private ActivityManager _manager;
    private int _timeToLive = 0; // Converges to remaining allowed distance to hands across contact graph.
    private int _timeToDie = 0;  // Timer after _timeToLive goes negative before deactivation.

    public void Init(IInteractionBehaviour interactionBehaviour, ActivityManager manager) {
      _interactionBehaviour = interactionBehaviour;
      _manager = manager;
      Revive();

      Rigidbody rigidbody = GetComponent<Rigidbody>();
      bool wasSleeping = rigidbody.IsSleeping();

      //We need to do this in order to force Unity to reconsider collision callbacks for this object
      //Otherwise scripts added in the middle of a collision never recieve the Stay callbacks.
      Collider singleCollider = GetComponentInChildren<Collider>();
      if (singleCollider != null) {
        Physics.IgnoreCollision(singleCollider, singleCollider, true);
        Physics.IgnoreCollision(singleCollider, singleCollider, false);
      }

      if (wasSleeping) {
        rigidbody.Sleep();
      }
    }

    public void Revive() {
      // This has a contact graph distance of 0 from the hands.
      _timeToLive = _manager.MaxDepth;
    }

    public void UpdateState() {

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
  }
}
