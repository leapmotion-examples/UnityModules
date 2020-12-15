/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

#if UNITY_5_6 && UNITY_EDITOR
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using Leap.Unity.Animation;

public class TweenCoreTests {

  [UnityTest]
  public IEnumerator BasicSingleTweenLifecycle() {
    bool didHitEnd = false;

    var tween = Tween.Single().
                      Value(0, 1, p => {
                        if (p == 1) {
                          didHitEnd = true;
                        }
                      }).
                      OverTime(1.0f).
                      Play();

    Assert.IsTrue(tween.isRunning);
    Assert.IsTrue(tween.isValid);

    yield return tween.Yield();

    Assert.IsFalse(tween.isValid);
    Assert.IsTrue(didHitEnd);
  }

  [UnityTest]
  public IEnumerator BasicPersistentTweenLifecycle() {
    bool didHitEnd = false;

    var tween = Tween.Persistent().
                      Value(0, 1, p => {
                        if (p == 1) {
                          didHitEnd = true;
                        }
                      }).
                      Play();

    Assert.IsTrue(tween.isRunning);
    Assert.IsTrue(tween.isValid);

    yield return tween.Yield();

    Assert.IsTrue(tween.isValid);
    Assert.IsFalse(tween.isRunning);
    Assert.IsTrue(didHitEnd);
  }

  [UnityTest]
  public IEnumerator OverTimeTest([Values]TweenType type,
                                  [Values(0.0f, 0.1f, 1.0f, 2.0f)] float time) {
    var tween = Create(type).OverTime(time).Play();

    float startTime = Time.time;
    yield return tween.Yield();
    float endTime = Time.time;

    float delta = endTime - startTime;
    Assert.That(delta, Is.EqualTo(time).Within(0.1f));
  }

  public Tween Create(TweenType type) {
    switch (type) {
      case TweenType.Single:
        return Tween.Single();
      case TweenType.Persistent:
        return Tween.Persistent();
      default:
        throw new Exception();
    }
  }

  public enum TweenType {
    Single,
    Persistent
  }
}
#endif
