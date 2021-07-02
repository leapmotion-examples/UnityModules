# Core {#core}

Core contains the fundamental assets necessary to get Leap hand data into your Unity project. The plugins we package with Core handle all the work of talking to the Ultraleap service that runs on your platform and provides hand data to your application from the sensor.

Core also contains a small set of prefabs to get you prototyping with a VR rig and Leap hands right away. You can create a new scene and drag in the **Leap Rig** prefab to immediately get an XR camera rig with Leap hand support, or you can drag in the LeapHandController prefab to work with a desktop-mounted Leap Motion Controller.

Finally, Core contains some lightweight utilities for a better Unity development experience, XR or otherwise.  These utilities support our Modules and, optionally, your own application: It contains [Query][ref_Query], our garbageless LINQ implementation, some useful data structures, a simple [Tween][ref_Tween]  library, and more!

[ref_Query]: @ref Leap.Unity.Query.Query "Query library"
[ref_Tween]: @ref Leap.Unity.Animation.Tween "Tween library"

Utilizing non-allocating libraries is crucial for VR development, where garbage collection means dropping frames and sickening your users; so our libraries generally eschew surprising memory allocation of any form. If you encounter garbage collection during normal Unity Modules use and it bothers you as much as it bothers us, let us know on our [Developer Forum][devforum].

[devforum]: https://community.leapmotion.com/c/development "Leap Motion Developer Forum"

# Setting up your development environment

Our [developer SDK][devsdk] includes the installer that can get a Leap service running on your platform. You'll need a Leap service to handle the communication between your Leap Motion Controller, your development machine, and your application.

[devsdk]: https://developer.leapmotion.com/get-started/ "Get Started with the Leap Motion SDK"

You'll need **Unity 5.5 or later** to use our Core Assets. _However, if you're intending to use the Interaction Engine, it relies on physics features that are only present in **Unity 5.6 or later**._

Of course, you'll also need the Core package and any Modules you'd like to use imported into your Unity project! Refer to our @ref mainpage guide if you're not sure how to do this.

# The prefabs will get you started

The fastest way to get Hands in your Unity environment is to use the **Leap Rig** prefab (for VR/AR applications) or the **LeapHandController** prefab (for non-VR/AR applications).

If you've gone through the setup process but you aren't seeing hands in your application after adding the LeapHandController prefab or the Leap Rig prefab to your scene, be sure to check out our @ref Troubleshooting section.

# The core Leap components {#core-leap-components}

### The Providers

[LeapProvider][ref_LeapProvider] defines the basic interface our modules expect to use to retrieve [Frame][ref_Frame] data. This abstraction allows you to create your own LeapProviders, which is useful when testing or developing in a context where Leap Controller hardware isn't immediately.

[LeapServiceProvider][ref_LeapServiceProvider] is the class that communicates with the Leap service running on your platform and provides Frame objects containing Leap hands to your application. Generally, any class that needs Hand data from the sensor will need a reference to a LeapServiceProvider to get that data. 

Release 4.7.0. introduces a new tracking model for tracking devices mounted above a screen and facing down towards the user (angled at 30 degress from the vertical), which is currently in **preview only**. Known as Screentop, this can be selected in the LeapServiceProvider's 'Advanced Options' section, under 'Tracking Optimization'.  There is a corresponding Screentop option for viewing the hands in the editor when this tracking mode is selected (Edit Time Pose). 

[LeapXRServiceProvider][ref_LeapXRServiceProvider] is the specialized component you should use for XR applications. Place this component directly on your XR camera, so that it can correctly account for differences in tracking timing between the sensor and your headset's pose tracking.

Finally, if your application needs to manually construct hand data for a standard [Hand][ref_Hand] pipeline or filter hand data coming through a LeapServiceProvider, the [PostProcessProvider][ref_PostProcessProvider] is a handy abstract class you can implement with a single function definition.

[ref_LeapServiceProvider]: @ref Leap.Unity.LeapServiceProvider
[ref_LeapXRServiceProvider]: @ref Leap.Unity.LeapXRServiceProvider
[ref_LeapProvider]: @ref Leap.Unity.LeapProvider
[ref_PostProcessProvider]: @ref Leap.Unity.PostProcessProvider
[ref_Frame]: @ref Leap.Frame
[ref_Hand]: @ref Leap.Hand

### The standard Hand pipeline

[Hand Model Manager][ref_HandModelManager] provides the standard Leap [Hand][ref_Hand] data-flow through [Hand Models][ref_HandModel], which are primarily used to drive visual representations of 3D hand models, such as the [Capsule Hands][ref_CapsuleHand] or the [Rigged Hands][ref_RiggedHand].

In order for the Hand Model Manager to function, you must add groups to its Model Pool -- one for every pair of graphical IHandModels or HandModels that you intend to use in your application.

Our standard rig prefab places the Hand Model Manager as a direct child of the root **Leap Rig** object and as a sibling of the Main Camera. This prevents the hands from drifting if the player's origin space (the **Leap Rig** transform) is moved about the scene.

[ref_HandModelManager]: @ref Leap.Unity.HandModelManager
[ref_HandModel]: @ref Leap.Unity.HandModel
[ref_CapsuleHand]: @ref Leap.Unity.CapsuleHand
[ref_RiggedHand]: @ref Leap.Unity.RiggedHand

# Leap Motion XR rigs {#xr-rig-setup}

![A basic XR rig setup with Capsule Hands.](@ref images/Basic_XR_Rig.png)

The **Leap Rig** prefab is a ready-made XR rig to get you started building an XR application with Ultraleap hand tracking. It consists of the following objects:

The **Leap Rig** is the root object. This is the Transform you should manipulate if you want to move your player in the XR space. This object contains the _optional_ XRHeightOffset script by default, which offers a simple way to get your player's head at a consistent height across the different XR platforms built into Unity.

The **Main Camera** is a direct child of the Leap Rig. Its local position and rotation are controlled directly by Unity's XR integration; you won't be able to move it manually (for that, you should move its parent, the Leap Rig, sometimes called your camera rig or just your "rig").

The [LeapXRServiceProvider][ref_LeapXRServiceProvider] component is attached directly to the Main Camera. It is a specialized type of LeapServiceProvider that retrieves hand data from the sensor and also accounts for latency differences between the sensor and the headset's pose tracking. (This prevents tracked hands from drifting if you quickly look around in your headset.)

The **Hand Model Manager object** is a sibling of the Main Camera and is the parent object for all of your HandModels, such as [Capsule Hands][ref_CapsuleHand] and [Rigged Hands][ref_RiggedHand].

Any other player-centric objects, such as the **Attachment Hands** prefab, the **Interaction Manager** prefab in the @ref interaction-engine, or your own custom player objects, are also well-placed as siblings of the Main Camera.

# A basic set of Leap hands {#a-basic-set-of-leap-hands}

These hands will get you prototyping right away, and may even serve all the needs of a simple XR application.

## HandModel Implementations

HandModel implementations get automatically pooled by the Hand Pool, but must be added manually as groups in your Hand Pool in order to function. Drop in the prefabs and then add references to the prefabs in your HandPool component to get these hands to render.

![Rigged Hands](@ref images/RiggedHands.png)

A recent addition to the Core assets, [Rigged Hands][ref_RiggedHand] are the standard hands we use at Leap when building demos or VR content. They are implemented as HandModels, which means they need to be added as an group in your LeapHandController object's HandPool in order to function. If you're interested in using a custom hand mesh with a SkinnedMeshRenderer similarly to the Rigged Hands, you'll want to check out the @ref hands-module.

**Note:** For Rigged Hands to look correct, you need to either set your `Quality Settings/Other/Blend Weights` to "4 Bones" (global quality setting) or override the `Skinned Mesh Renderer`s' Quality setting to "4 Bones" (override quality setting for the Rigged Hands, which should be on by default). If you don't, the rigged hands will exhibit a strange stretch in the palm.

![Capsule Hands](@ref images/CapsuleHands.png)

The [Capsule Hands][ref_CapsuleHand] generate a set of spheres and cylinders to render hands using Leap hand data. They render all of the raw data available in a Leap hand in a procedural way. With relatively minor changes to the CapsuleHand. script, you can quickly create a set of hands that match your application's visual style.

# Attaching objects to hand joints {#attaching-objects-to-hand-joints}

![Attachment Hands](@ref images/AttachmentHands.png)

[Attachment Hands][ref_AttachmentHands] aren't usually used to render hands _per se_; rather, you can drag in the AttachmentHands prefab as a sibling of your Main Camera and use the Transforms that the script automatically generates to easily attach objects to Leap hands or to refer to specific target joints on a hand. To attach an object to a hand, just drag it to be a child of the joint you want to attach it to and align it as desired. **Attachment Hands aren't Hand Models**, so they don't need to be added to the Hand Model Manager's model pool to function, they only require an implementation of [LeapProvider][ref_LeapProvider] (e.g. a [LeapServiceProvider][ref_LeapServiceProvider]) to be in your scene.

[ref_AttachmentHands]: @ref Leap.Unity.Attachments.AttachmentHands

# FAQ {#core-faq}

**Q: I'm working on a custom experience/headset integration and hand alignment needs to be _totally perfect._ I have control over the head rig that will be used for my experience. How can I make sure the hand aligns with the user's real hands perfectly?**

A: Check your LeapXRServiceProvider object in the Leap Rig prefab. We've included an `Allow Manual Device Offset` checkbox in the Advanced section that will allow you to adjust where your application expects the Leap Motion Controller to be relative to the tracked headset. (If you don't see this checkbox, make sure you've upgraded to the latest version of the Core module.)

Because most Leap Motion VR rigs utilize a custom VR developer mount attachment, not all Leap Motion Controllers are mounted in the same place relative to the tracked positions of VR headsets. In order for hands in VR space to align perfectly with hands in the real world, your application needs to know _exactly_ where the Leap Motion Controller is mounted relative to your tracked headset position and orientation. While the default values will usually produce an acceptable experience for VR, in passthrough or mixed-reality situations, a mismatch between the real world Leap position and the VR world Leap position -- even of just a few degrees of tilt, or a centimeter of displacement -- can shift hands too much to produce a plausible tracking experience.

Naturally, this solution isn't viable if you intend for your application to be run on a wide variety of headsets with a Leap Motion Controller attached. Under these circumstances, we recommend you keep the device offsets to their default values (uncheck the checkbox to revert them).

**Q: How do I convert a VR example into a desktop example?

First, add your own camera to the scene so you can film from whatever perspective makes sense for your user.

The basic setup of a VR-less system is to use a LeapServiceProvider component (not a LeapXRServiceProvider) and a linked Hand Model Manager (with hands underneath it in the hierarchy and registered to it, much like how you see implemented in the VR prefabs).
Also remember to disable VR mode from your player settings (if you’re using a cloned version of UnityModules). Upon pressing play and holding your hands over the device, you should see the hands in the proper orientation.
