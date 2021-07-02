# Interaction Engine {#interaction-engine}

The Interaction Engine allows users to work with your XR application by interacting with *physical* or *pseudo-physical* objects. Whether a baseball, a [block][blocks], a virtual trackball, a button on an interface panel, or a hologram with more complex affordances, if there are objects in your application you need your user to be able to **hover** near, **touch**, or **grasp** in some way, the Interaction Engine can do some or all of that work for you.

You can find latest stable Interaction Engine package [on our developer site][devsite].

For a quick look at what the Interaction Engine can do, we recommend importing the package into your Unity project (don't forget Core as well!) and checking out the included example scenes, documented further down below. For an in-depth review of features in the Interaction Engine, keep reading!

# The basic components of interaction {#ie-basic-components}

- "Interaction objects" are GameObjects with an attached [InteractionBehaviour][ref_InteractionBehaviour]. They require a Rigidbody and at least one Collider.
- The [InteractionManager][ref_InteractionManager] receives FixedUpdate from Unity and handles all the internal logic that makes interactions possible, including updating hand/controller data and interaction object data. **You need one of these in your scene for interaction objects to function!** A good place for the manager is right underneath your player's top-level camera rig transform (that is, not the player's camera itself, but its parent).
- Each [InteractionController][ref_InteractionController] does all the actual _interacting_ with interaction objects, whether by picking them up, touching them, hitting them, or just being near them. This object could be the user's hand by way of the [InteractionHand][ref_InteractionHand] component, or an XR Controller (e.g. Oculus Touch or Vive controller) if it uses the [InteractionXRController][ref_InteractionXRController] component. Interaction controllers **must sit beneath the Interaction Manager in the hierarchy** to function.

[ref_InteractionBehaviour]: @ref Leap.Unity.Interaction.InteractionBehaviour
[ref_InteractionManager]: @ref Leap.Unity.Interaction.InteractionManager
[ref_InteractionController]: @ref Leap.Unity.Interaction.InteractionController
[ref_InteractionHand]: @ref Leap.Unity.Interaction.InteractionHand
[ref_InteractionXRController]: @ref Leap.Unity.Interaction.InteractionXRController

![A basic XR rig with the Interaction Engine.](@ref images/Basic_Components_of_Interaction.png)

Interaction objects can live anywhere in your scene, as long as you have an InteractionManager active. Interaction controllers, on the other hand, always need to live underneath the Interaction Manager in order to function, and the Interaction Manager should always be a sibling of the camera object, so that controllers don't inherit strange velocities if the player's rig is moved around.

# Just add InteractionBehaviour! {#ie-just-add-interactionbehaviour}

When you add an [InteractionBehaviour][ref_InteractionBehaviour] component to an object, a couple of things happen automatically:

- If it didn't have one before, the object will gain a [Rigidbody][rigidbody] component with gravity enabled, making it a physically-simulated object governed by Unity's PhysX engine. If your object doesn't have a [Collider][collider], it will fall through the floor!
- Assuming you have an Interaction Manager with one or more interaction controllers beneath it, you'll be able to pick up, poke, and smack the object with your hands or XR controller.

[rigidbody]: https://docs.unity3d.com/ScriptReference/Rigidbody.html
[collider]: https://docs.unity3d.com/ScriptReference/Collider.html

The first example in the Interaction Engine package showcases the default behavior of a handful of different objects when they first become interaction objects.

[devsite]: https://developer.leapmotion.com/unity "Leap Motion Unity Developer site"
[blocks]: https://www.youtube.com/watch?v=oZ_53T2jBGg&t=1m11s "Ultraleap Blocks demo"
[rigidbody]: https://docs.unity3d.com/ScriptReference/Rigidbody.html
[collider]: https://docs.unity3d.com/ScriptReference/Collider.html

# First steps with the Interaction Engine {#ie-first-steps}

If you haven't already, import @ref core and the Interaction Engine into your Unity project:

- Download the latest Core package from [our developer site][devsite].
- Download the latest Interaction Engine package from [our developer site][devsite].
- Import both packages. To import a `.unitypackage` file, double-click on it while your project is open, or go to `Assets -> Import Package...` and choose the package.
- If you see errors, make sure your project has the latest version of Core (one section up), and that your project was opened using Unity 5.6 or later.

## Update the Physics timestep and gravity!

Unity's physics engine has a "fixed timestep," and that timestep is not always in sync with the graphics frame rate. It is very important that you set the physics timestep to be the same as the rendering frame rate. If you are building for an Oculus or Vive, this means that your physics timestep should be `0.0111111` (corresponding to 90 frames per second). This is configured via `Edit -> Project Settings -> Time`.

Additionally, we've found that setting your gravity to half its real-world scale (-4.905 on the Y axis instead of -9.81) produces a better feeling when working with physical objects. We strongly recommend setting your gravity in this way; you can change it in `Edit -> Project Settings -> Physics`.

## Get your XR rig ready

If you don't already have a Leap-enabled XR camera rig to your scene, you can follow these steps:
- Open a new scene and delete the `Main Camera` object. (We'll set up our own.)
- Drag the **Leap Rig** prefab into your scene: `LeapMotion/Core/Prefabs`.
- Drag the **Interaction Manager** prefab into your camera rig: `LeapMotion/Modules/InteractionEngine/Prefabs`.

If you aren't familiar with Leap-enabled XR rigs, check out @ref xr-rig-setup.

It is possible to use a custom camera rig in combination with Ultraleap hand tracking. If you'd like to use something other than the **Leap Rig** prefab, you should make sure you have a camera tagged MainCamera in your scene, and that it has children with the same components and linkages that you can find beneath the Camera object in the **Leap Rig** prefab. Note that the Interaction Engine on its own does not render hands, it only instantiates physical representations of hands.

At Leap, we tend to put player-centric scripts in dedicated GameObjects that are siblings of the Main Camera object. For example, the AttachmentHands script offers a convenient way to attach arbitrary objects to any of the joints in a Leap hand representation, and it belongs in such a sibling GameObject. To create AttachmentHands for use in your scene, you would:
- Create a new GameObject in your scene
- Rename it `Attachment Hands`
- Drag it into the Rig object so that it sits beneath your Rig object
- Add the AttachmentHands script to it: drag the `LeapMotion/Core/Scripts/Attachments/AttachmentHands.cs` script onto the object, or use the AddComponent menu with the object selected and type `AttachmentHands`.

## Configure InteractionXRControllers for grasping

If you intend to use the Interaction Engine with Oculus Touch or Vive controllers, you'll need to configure your project's input settings before you'll be able to use the controllers to grasp objects. Input settings are project settings that cannot be changed by imported packages, which is why we can't configure these input settings for you. You can skip this section if you are only interested in using Leap hands with the Interaction Engine.

Go to your Input Manager (`Edit -> Project Settings -> Input`) and set up the joystick axes you'd like to use for left-hand and right-hand grasps. (Controller triggers are still referred to as 'joysticks' in Unity's parlance.) Then make sure each InteractionXRController has its grasping axis set to the corresponding axis you set up. The default prefabs for left and right InteractionXRControllers will look for axes named `LeftXRTriggerAxis` and `RightXRTriggerAxis`, respectively.

Helpful diagrams and axis labels can be found in [Unity's documentation][unity-docs-openvr-controllers].

[unity-docs-openvr-controllers]: https://docs.unity3d.com/Manual/OpenVRControllers.html

# Check out the examples {#ie-examples}

The examples folder (`LeapMotion/Modules/InteractionEngine/Examples`) contains a series of example scenes that demonstrate the features of the Interaction Engine.

Many of the examples can be used with Leap hands via the Leap Motion Controller *or* with any XR controller that Unity provides built-in support for, such as Oculus Touch controllers or Vive controllers.

## Example 1: Interaction Objects 101

\htmlonly
<video class="ie-example-video" src="Example_1_-_Interaction_Objects.webm" autoplay loop></video>
\endhtmlonly

The Interaction Objects example shows the default behavior of interaction objects when they first receive their InteractionBehaviour component.

Reach out with your hands or your XR controller and play around with the objects in front of you to get a sense of how the default physics of interaction objects feels. In particular, you should see that objects don't jitter or explode, even if you attempt to crush them or pull on the constrained objects in various directions.

On the right side of this scene are floating objects that have been marked **kinematic** and that have `ignoreGrasping` and `ignoreContact` set to `true` on their InteractionBehaviours. These objects have a simple script attached to them that causes them to glow when hands are nearby -- but due to their interaction settings, they will only receive hover information, and cannot be grasped. Note that Rigidbodies collide against these objects even though they have `ignoreContact` set to true -- this setting applies only against interaction controllers, not for arbitrary Rigidbodies. In general, we use **Contact** to refer specifically to the contact-handling subsystem in the Interaction Engine between interaction controllers (e.g. hands) and interaction objects (e.g. cubes).

## Example 2: Basic UI in the Interaction Engine

\htmlonly
<video class="ie-example-video" src="Example_2_-_Basic_UI.webm" autoplay loop></video>
\endhtmlonly

Interacting with interface elements is a very particular _kind_ of interaction, but in VR or AR, we find these interactions to make the most sense to users when they are provided physical metaphors and familiar mechanisms. Thus, we've built a small set of fine-tuned InteractionBehaviours (that will continue to grow!) that deal with this extremely common use-case: The [Interaction Button][ref_InteractionButton], and the [Interaction Slider][ref_InteractionSlider].

[ref_InteractionButton]: @ref Leap.Unity.Interaction.InteractionButton
[ref_InteractionSlider]: @ref Leap.Unity.Interaction.InteractionSlider

Try manipulating this interface in various ways, including ways that it doesn't expect to be used. You should find that even clumsy users will be able to push only one button at a time: Fundamentally, _user interfaces in the Interaction Engine only allow the 'primary hovered' interaction object to be manipulated or triggered at any one time_. This is a soft constraint; primary hover data is exposed through the [InteractionBehaviour's API][ref_InteractionBehaviour] for any and all interaction objects for which **hovering** is enabled, and the InteractionButton enforces the constraint by disabling contact when it is not 'the primary hover' of an interaction controller.

## Example 3: Interaction Callbacks for Handle-type Interfaces

\htmlonly
<video class="ie-example-video" src="Example_3_-_Interaction_Callbacks.webm" autoplay loop></video>
\endhtmlonly

The Interaction Callbacks example features a set of interaction objects that collectively form a basic Transform Tool the user may use at runtime to manipulate the position and rotation of an object. These interaction objects ignore contact, reacting only to grasping controllers and controller proximity through hovering. Instead of allowing themselves to be moved directly by grasping hands, these objects cancel out and report the grasped movement from controllers to their managing TransformTool object, which orchestrates the overall motion of the target object and each handle at the end of every frame.

## Example 4: Attaching Interfaces to the User's Hand

\htmlonly
<video class="ie-example-video" src="Example_4_-_Hand_UI.webm" autoplay loop></video>
\endhtmlonly

Simple applications may want to attach an interface directly to a user's hand so that certain important functionalities are always within arm's reach. This example demonstrates this concept by animating one such interface into view when the user looks at their left palm (or the belly of their XR controller; in the controller case, it may be better to map such a menu to an XR controller button!).

## Example 5: Building on Interaction Objects with Anchors

\htmlonly
<video class="ie-example-video" src="Example_5_-_Anchors.webm" autoplay loop></video>
\endhtmlonly

The AnchorableBehaviour, Anchor, and AnchorGroup components constitute an optional set of scripts that are included with the Interaction Engine that build on the basic interactivity afforded by interaction objects. This example demonstrates all three of these components. AnchorableBehaviours integrate well with InteractionBehaviour components (they are designed to sit on the same GameObject) and allow an interaction object to be placed in Anchor points that can be defined anywhere in your scene.

## Example 6: Dynamic Interfaces: Interaction Objects, AttachmentHands, and Anchors

\htmlonly
<video class="ie-example-video" src="Example_6_-_Dynamic_UI.webm" autoplay loop></video>
\endhtmlonly

InteractionButtons and InteractionSliders are useful on their own, but they become truly powerful tools in your UI toolkit when combined with Anchors, and Core utilities like the AttachmentHands and the Tween library to allow the user to carry around entire physical interfaces on their person in XR spaces. This example combines all of these components to demonstrate using the Interaction Engine to build a set of portable XR interfaces.

## Example 7: Moving Reference Frames

\htmlonly
<video class="ie-example-video" src="Example_7_-_Moving_Reference_Frames.webm" autoplay loop></video>
\endhtmlonly

The Interaction Engine keeps your interfaces working even while the player is being translated and rotated. Make sure your player moves during FixedUpdate, before the Interaction Engine performs its own FixedUpdate. You'll also need to make sure the Interaction Manager object moves with the player -- this is most easily accomplished by placing it beneath the player's rig Transform, as depicted in our [standard rig diagram][ie-basic-components] above.

If you're not sure that your application is set up correctly for moving reference frame support, this example demonstrates a working configuration that you can reference.

[ie-basic-components]: @ref ie-basic-components

## Example 8: Swap Grasp

\htmlonly
<video class="ie-example-video" src="Example_8_-_Swap_Grasp.webm" autoplay loop></video>
\endhtmlonly

This example scene demonstrates the use of the [InteractionController][ref_InteractionController]'s SwapGrasp() method, which allows you to instantly swap an object that the user is holding for another. This is especially useful if you need objects to morph while the user is holding them.

[ref_InteractionController]: @ref Leap.Unity.Interaction.InteractionController

# Working with PhysX objects in Unity {#ie-working-with-physx}

Before scripting behavior with the Interaction Engine, you should know the basics of working with PhysX Rigidbodies in Unity. Most importantly, you should understand Unity's physics scripting execution order:

1. FixedUpdate (user physics logic) **sometimes, but always with PhysX**
2. PhysX updates Rigidbodies and resolves collisions **sometimes, but always with FixedUpdate**
3. Update (user graphics logic) **once every frame**

Source: [this helpful chart from Unity][unity script callback order], via [the execution order page][execution order].

[unity script callback order]: https://docs.unity3d.com/uploads/Main/monobehaviour_flowchart.svg
[execution order]: https://docs.unity3d.com/Manual/ExecutionOrder.html

**FixedUpdate** happens just before the physics engine "PhysX" updates and is where user physics logic goes! This is where you should modify the positions, rotations, velocities, and angular velocities of your Rigidbodies to your liking before the physics engine *does physics to them*.

**FixedUpdate may happen 0 or more times per Update.** XR applications usually run at 90 frames per second to avoid sickening the user. Update runs once before the Camera in your scene renders what it sees to the screen or your XR headset. Unity's physics engine has a "fixed timestep" that is configured via `Edit -> Project Settings -> Time`. At Leap, we build applications with a fixed timestep of `0.0111111` to try and get a FixedUpdate to run once a frame, and this is the setting we recommend. But do note that FixedUpdate is **not** guaranteed to fire before every rendered frame, if your time-per-frame is less that your fixed timestep. Additionally, FixedUpdate may happen two or more times before a rendered frame. This will happen if you spend more than two fixed timesteps' worth of time on any one render frame (i.e. if you "drop a frame" because you tried to do too much work during one Update or FixedUpdate).

Naturally, because the Interaction Engine deals entirely in physics objects, **all interaction object callbacks occur during FixedUpdate**. While we're on the subject of potential gotchas, here are a few more gotchas when working with physics:

- The update order (FixedUpdate, PhysX, Update) implies that if you move physics objects via their Rigidbodies during Update and not during FixedUpdate, the new positions/rotations will not be visible until the *next* update cycle, after the physics engine manipulates objects' Transforms via their Rigidbodies.
- When you move a PhysX object (Rigidbody) via its Transform (`transform.position` or `transform.rotation`) _instead of its Rigidbody_ (`rigidbody.position` or `rigidbody.rotation`), you **force PhysX to immediately do some heavy recalculations internally**, so if you do this a lot to a bunch of physics objects a frame, it could be bad news for your framerate. Generally, we don't recommend doing this! (But sometimes it's necessary.)

# Custom layers for interaction objects {#ie-custom-layers}

Have a custom object layer setup? No problem. Interaction objects need to switch between two layers at runtime:
- The "Interaction" layer, used when the object **can** collide with your hands.
- The "No Contact" layer, used when the object **can't** collide with your hands. This is the case when the object is **grasped**, or when`ignoreContact` is set to `true`.

On a specific Interaction Behaviour under its **Layer Overrides** header, check `Override Interaction Layer` and `Override No Contact Layer` in its inspector to specify custom layers to use for the object when contact is enabled or disabled (e.g. due to being grasped). These layers must follow collision rules with respect to the **contact bone layer**, which is the layer that contains the Colliders that make up the bones in Interaction Hands or Interaction Controllers. (The contact bone layer is usually automatically generated, but you can specify a custom layer to use for Interaction Controllers in the Interaction Manager's inspector.) The rules are as follows:

- The Interaction Layer should have collision enabled with the contact bone layer.
- The No Contact layer should **not** have collision enabled with the contact bone layer.
- (Any collision configuration is allowed for these layers with respect to any other, non-contact-bone layers.)

You can override both or only one of the layers for interaction objects as long as these rules are followed. You can also name these layers anything you want, although we usually put "Interaction" and "No Contact" in the layer names to make their purposes clear.

# Custom behaviors for interaction objects {#ie-custom-interaction-behaviors}

Be sure to take a look at examples 2 through 6 to see how interaction objects can have their behavior fine-tuned to meet the specific needs of your application. The standard workflow for writing custom scripts for interaction objects goes something like this:

- Be sure your object has an [InteractionBehaviour][ref_InteractionBehaviour] component (or an [InteractionButton][ref_InteractionButton] or [InteractionSlider][ref_InteractionSlider] component, each of which inherit from @ref InteractionBehaviour).
- Add your custom script to the interaction object and initialize a reference to the InteractionBehaviour component.
```{.cs}
using Leap.Unity.Interaction;
using UnityEngine;

[RequireComponent(typeof(InteractionBehaviour))]
public class CustomInteractionScript : MonoBehaviour {

  private InteractionBehaviour _intObj;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();
  }

}
```
- Check out the API documentation (or take advantage of IntelliSense!) for the @ref InteractionBehaviour class to get a sense of what behavior you can control through scripting, or look at the examples below.

## Disabling/enabling interaction types at runtime

Disabling and enabling **hover**, **contact**, or **grasping** at or before runtime is a first-class feature of the Interaction Engine. You have two ways to do this:

### Option 1: Using controller interaction types

The [InteractionController][ref_InteractionController] class provides the `enableHovering`, `enableContact`, and `enableGrasping` properties. Setting any of these properties to false will immediately fire "End" events for the corresponding interaction type and prevent the corresponding interactions from occurring between this controller and any interaction object.

### Option 2: Using object interaction overrides

The [InteractionBehaviour][ref_InteractionBehaviour] class provides the `ignoreHover`, `ignoreContact`, and `ignoreGrasping` properties. Setting any of these properties to true will immediately fire "End" events for the corresponding interaction type (for this object only) and prevent the corresponding interactions from occurring between this interaction object and any controller.

## Constraining an object's held position and rotation

### Option 1: Use PhysX constraints (joints)

The Interaction Engine will obey the constraints you impose on interaction objects whose Rigidbodies you constrain using [Joint][joint] components. If you grasp a *non-kinematic* interaction object that has a Joint attached to it, the object will obey the constraints imposed by that joint.

[joint]: https://docs.unity3d.com/Manual/Joints.html

If you add or remove an interaction object's Joints at runtime and your object is graspable, you should call `_intObj.RefreshPositionLockedState()` to have the object check whether any attached Joints or Rigidbody state lock the object's position. Under these circumstances, the object must choose a different grasp orientation solver to give intuitively correct behavior. Check [the API documentation on this method][ref_InteractionBehaviour_RefreshPositionLockedState] for more details.

[ref_InteractionBehaviour_RefreshPositionLockedState]: @ref Leap.Unity.Interaction.InteractionBehaviour@RefresionPositionLockedState()

### Option 2: Use the OnGraspedMovement callback

When grasped, objects fire their OnGraspedMovement callback right after the Interaction Engine moves them with the grasping controller. That means you can take advantage of this callback to modify the Rigidbody position and/or rotation just before PhysX performs its physics update. Setting up this callback will look something like this:

```{.cs}

private InteractionBehaviour _intObj;

private void OnEnable() {
  _intObj = GetComponent<InteractionBehaviour>();

  _intObj.OnGraspedMovement -= onGraspedMovement; // Prevent double-subscription.
  _intObj.OnGraspedMovement += onGraspedMovement;
}

private void OnDisable() {
  _intObj.OnGraspedMovement -= onGraspedMovement;
}

private void onGraspedMovement(Vector3 presolvedPos, Quaternion presolvedRot,
                               Vector3 solvedPos,    Quaternion solvedRot,
                               List<InteractionController> graspingControllers) {
  // Project the vector of the motion of the object due to grasping along the world X axis.
  Vector3 movementDueToGrasp = solvedPos - presolvedPos;
  float xAxisMovement = movementDueToGrasp.x;

  // Move the object back to its position before the grasp solve this frame,
  // then add just its movement along the world X axis.
  _intObj.rigidbody.position = presolvedPos;
  _intObj.rigidbody.position += Vector3.right * xAxisMovement;
}

```

## Constraining an interaction object's position and rotation generally

The principles explained above for constraining a grasped interaction object's position and rotation also apply to constraining the interaction object's position and rotation even when it is not grasped. Of course, Rigidbody Joints will work as expected.

When scripting a custom constraint, however, instead of using the OnGraspedMovement callback, the Interaction Manager provides an OnPostPhysicalUpdate event that fires just after its FixedUpdate, in which it updates interaction controllers and interaction objects. This is a good place to apply your physical constraints.

```{.cs}

private InteractionBehaviour _intObj;

void OnEnable() {
  _intObj = GetComponent<InteractionBehaviour>();

  // Prevent double subscription.
  _intObj.manager.OnPostPhysicalUpdate -= applyXAxisWallConstraint;
  _intObj.manager.OnPostPhysicalUpdate += applyXAxisWallConstraint;
}

void OnDisable() {
  _intObj.manager.OnPostPhysicalUpdate -= applyXAxisWallConstraint;
}

private void applyXAxisWallConstraint() {
  // This constraint forces the interaction object to have a positive X coordinate.
  Vector3 objPos = _intObj.rigidbody.position;
  if (objPos.x < 0F) {
    objPos.x = 0F;
    _intObj.rigidbody.position = objPos;

    // Zero out any negative-X velocity when the constraint is applied.
    Vector3 objVel = _intObj.rigidbody.velocity;
    if (objVel.x < 0F) {
      objVel = 0F;
      _intObj.rigidbody.velocity = objVel;
    }
  }
}

```

## Applying forces to an interaction object

If your interaction object is not actively being touched by an Interaction Hand or an Interaction XR Controller, you may apply forces to your Rigidbody using the standard API provided by Unity. However, when an object experiences external forces that press it into the user's controller or the user's hand, the "soft contact" system provided by the Interaction Engine requires special knowledge of those external forces to properly account for them. In any gameplay-critical circumstances involving forces of this nature, you should use the Forces API provided by interaction objects:

```{.cs}
_intObj.AddLinearAcceleration(myAccelerationAmount);
_intObj.AddAngularAcceleration(myAngularAccelerationAmount);
```

These accelerations are ultimately applied using the Rigidbody forces API, but are also recorded by the "soft contact" subsystem, to prevent the object from nudging its way _through_ interaction controllers due to repeated application of these forces.

# Interaction types in-depth {#ie-in-depth}

## Hovering

Hover functionality in the Interaction Engine consists of two inter-related subsystems, referred to as 'Hover' and 'Primary Hover' respectively.

### Proximity feedback ("Hover")

Any interaction object within the Hover Activity Radius (defined in your [Interaction Manager][ref_InteractionManager]) around an interaction controller's hover point will receive the OnHoverBegin, OnHoverStay, and OnHoverEnd callbacks and have its `isHovered` state set to true, as long as both the hovering controller and the interaction object have their hover settings enabled. Interaction objects provide a public getter for getting the closest hovering interaction controller as well. In general, hover information is useful when scripting visual and audio feedback related to proximity.

### Primary Hover

Interaction controllers define one or more "primary hover points," and the closest interaction object (that is currently hovered by an interaction controller) to any of the interaction controller's primary hover points will become the primarily hovered object of that controller. For example, in [InteractionHand][ref_InteractionHand]'s inspector, you can specify which of the hand's fingertips you'd like tracked as primary hover points. The primary hover status of an interaction object can be queried at any time using a controller's `primaryHoveredObject` property or the object's `isPrimaryHovered` property.

Fundamentally, primary hover is the feature that turns unreliable interfaces into reliable ones. When _only the primary hovered object_ of a given interaction controller can be depressed or otherwise interacted-with by that controller, even the coarsest motions are guaranteed to only ever interact with a single UI element at a time. This is why the button panel in [[Example 2 (Basic UI) | Getting-Started-(Interaction-Engine)#example-2-basic-ui-in-the-interaction-engine]] will only depress one button per hand at any given time, even if you clumsily throw your whole hand into the panel. The InteractionButton, InteractionToggle, and InteractionSlider classes all implement this primary-hover-only strategy in order to produce more reliable interfaces.

Because it constraints interactions down to a single controller/object pair, "primary hover" tends to most closely resemble the concept of "hover" in 2D interfaces utilizing a mouse pointer.

## Contact

Contact in the Interaction Engine consists of two subsystems:
- **Contact Bones**, which are Rigidbodies with a single Collider and [ContactBone][ref_ContactBone] component that holds additional contact data for hands and controllers, and
- **Soft Contact**, which activates when Contact Bones get dislocated from their target positions and rotations -- in other words, when a hand or interaction controller jams itself too far "inside" an interaction object.

[ref_ContactBone]: @ref Leap.Unity.Interaction.ContactBone

### Contact Bones

Interaction controller implementations are responsible for constructing and updating a set of GameObjects with Rigidbodies, Colliders, and ContactBone components, referred to as contact bones. The controller is also responsible for defining the "ideal" or target position and rotation for a given contact bone at all times. During the FixedUpdate, the [InteractionController][ref_InteractionController] base class will set each of its contact bones' velocities and angular velocities such that the contact bone will reach its ideal position and rotation by the _next_ FixedUpdate. These velocities then propagate through the Unity's physics engine (PhysX) update and the contact bones may collide against objects in the scene, which will apply forces to them -- and potentially dislocate the contact bones, preventing them from reaching their destination.

Additionally, at the beginning of every FixedUpdate, an interaction controller checks how dislocated a contact bone is from its intended position and rotation. If this dislocation becomes too large, the interaction controller will switch into Soft Contact mode, which effectively disables its contact bones, by converting them into [Trigger][triggers] colliders.

[triggers]: https://docs.unity3d.com/ScriptReference/Collider-isTrigger.html

### Soft Contact

Soft Contact is essentially an alternative to the standard physical paradigm in physics engines of treating Rigidbodies as, well, perfectly rigid bodies. Instead, relative positions, rotations, velocities, and angular velocities are calculated as the trigger colliders of contact bones pass through the colliders of interaction objects, and custom velocities and angular velocities are applied each frame to any interaction objects that are colliding with the bones of an interaction controller in soft contact mode so that the controller and object will resist motions _deeper into_ the object but freely allow motions _out of_ the object.

If debug drawing is enabled on your Interaction Manager, you can tell when an interaction controller is in Soft Contact mode because its contact bones (by default) will render as white instead of green.

## Grasping

When working with XR controllers, grasping is a pretty basic feature to implement: simply define which button should be used to grab objects, and use the motion of the grasp point to move any grasped object. However, when working with Leap hands, we no longer have the simplicity of dealing in digital buttons. Instead, we've implemented a finely-tuned heuristic for detecting when a user has intended to grasp an interaction object. Whether you're working with XR controllers or hands, the grasping API in the Interaction Engine provides a common interface for constructing logic around grasping, releasing, and throwing.

### Grasped pose & object movement

When an interaction controller picks up an object, the default implementation of all interaction controllers assumes that the intended behavior is for the object to follow the grasp point. Grasp points are explicitly defined for InteractionXRControllers (as Transforms) and are implicit for Interaction Hands (depending on how the hand's fingers grasp the object), but the resulting behavior is the same in either case.

While grasped, interaction objects are moved under one of two mutually-exclusive modes: Kinematic or Nonkinematic. By default, kinematic interaction objects will move [kinematically][ref_isKinematic] when grasped, and nonkinematic interaction objects will move nonkinematically when grasped. When moving kinematically, an interaction object's rigidbody position and rotation _are set explicitly_, effectively teleporting the object to the new position and rotation. This allows the grasped object to clip through colliders it otherwise would not be able to penetrate. Nonkinematic grasping motions, however, cause an interaction object to instead _receive a velocity and angular velocity_ that will move it to its new target position and rotation on the next physics engine update, which allows the object to collide against objects in the scene before reaching its target grasped position.

[ref_isKinematic]: https://docs.unity3d.com/ScriptReference/Rigidbody-isKinematic.html

When an object is moved because it is being grapsed by a moving controller, the [OnGraspedMovement][ref_InteractionBehaviour_OnGraspedMovement] is fired right after the object is moved, which you should subscribe to if you wish to modify how the object moves while it is grasped. Alternatively, you can disable the `moveObjectWhenGrasped` setting on interaction objects to prevent their grasped motion entirely (which will no longer cause the callback to fire).

[ref_InteractionBehaviour_OnGraspedMovement]: @ref Leap.Unity.Interaction.InteractionBehaviour#OnGraspedMovement

### Throwing

When a grasped object is released, its velocity and angular velocity are controlled by an object whose class implements the [IThrowHandler][ref_IThrowHandler] interface. IThrowHandlers receive updates every frame during a grab so that they can accumulate velocity and angular velocity data about the object. Usually, only the latest few frames of data are necessary. When the object is finally released, they get an OnThrow call, which in the default implementation ([SlidingWindowThrow][ref_SlidingWindowThrow]) sets the velocity of the object based on a recent historical average of the object's velocity while grasped. In practice, this results in greater accuracy in users' throws.

[ref_IThrowHandler]: @ref Leap.Unity.Interaction.IThrowHandler
[ref_SlidingWindowThrow]: @ref Leap.Unity.Interaction.SlidingWindowThrow

However, if you'd like to create a different implementation of a throw, you can implement a new IThrowHandler and set the public `throwHandler` property on any interaction object to change how it behaves when it is thrown.

# FAQ {#interaction-engine-faq}

**Q: Can I translate and rotate my XR rig (player), say, on a moving ship, and still have Interaction Engine user interfaces work?**

A: Yes, we support this via code in the [InteractionManager][ref_InteractionManager] that watches how its own Transform moves between each FixedUpdate and translates the colliders in the player's Interaction Controllers accordingly. Refer to Example 7 to see a working implementation of this functionality. In general, make sure to:
- Translate and/or rotate the player during FixedUpdate, **before the InteractionManager performs its own FixedUpdate**. You can ensure your movement script occurs before the InteractionManager by setting its Script Execution Order to run before Default Time (`Edit -> Project Settings -> Script Execution Order`). Alternatively, an easy way to receive a callback to execute just before the Interaction Manager's FixedUpdate is to subscribe to the [OnPrePhysicalUpdate][ref_InteractionManager_OnPrePhysicalUpdate] event.
- Make sure the Interaction Manager moves with the player when the player is translated or rotated. The easiest way to do this is to have the Interaction Manager be a child object of your player rig object, e.g., the **Leap Rig** object.

[ref_InteractionManager_OnPrePhysicalUpdate]: @ref Leap.Unity.Interaction.InteractionManager#OnPrePhysicalUpdate

**Q: Will the Interaction Engine work at arbitrary player scales?**

A: Currently, no. The Interaction Engine works best at "real-world" scale: 1 Unity distance unit = 1 real-world meter. All of Ultraleaps's Unity assets follow this rule, so you're fine if you keep our prefabs at unit scale. If you scale the player too far away from unit scale, certain interactions may stop functioning properly. We would like to support arbitrary interaction scales, but there is no timeline for this feature currently.

**Q: How can I effectively grasp very small objects?**

A: If you need to be able to grasp a very small object and the object's physical colliders don't produce good grasping behaviors, try adding a new primitive collider for the object, such as a SphereCollider, with a larger radius than the object itself, and with isTrigger set to true. As long as the [InteractionBehaviour][ref_InteractionBehaviour] is not ignoring grasping, you will be able to grasp the object using this trigger volume.

However, you don't want a grasping-only trigger collider to be _too_ much larger than the object itself. In general, the larger the grasping volume is around an object, the more likely the user is to accidentally grasp objects when they don't intend to. Additionally, having overlapping grasping-only trigger colliders from multiple objects will prevent the grasp classifier from correctly picking which object to grasp.

**Q: Can I add colliders to my interaction object that are ignored by the Interaction Engine, so that I can, for example, detect ray-casts against the object when it is far away from the player?**

A: You can use the [IgnoreColliderForInteraction][ref_IgnoreColliderForInteraction] component on any Colliders on an InteractionBehaviour that you'd like the Interaction Engine to ignore. Any such colliders should also be [trigger][triggers] colliders to prevent strange behavior if interaction controllers collide with those ignored colliders.

[ref_IgnoreColliderForInteraction]: @ref IgnoreCollidersForInteraction

**Q: Can I hold an interaction object with both hands?**

A: You can check a checkbox named 'Allow Multi Grasp' that is located on the interaction object itself to enable two-handed grasping for that object. If you want to know if there are two hands currently grasping an object, you can use the `graspingHands` property of the [InteractionBehaviour][ref_InteractionBehaviour]. This is a set of all hands currently grasping the object, so it will have a count of two if it's currently being grasped by two hands.

Two-hand-grasping interactions are not fully realistic yet with out-of-the-box Interaction Engine components, but we'd like to support this in the future!

# Have a question not answered here?

Head over to [the developer forum][devforum] and post your question! We'll see how we can help.

[devforum]: https://community.leapmotion.com/c/development "Leap Motion Developer Forums"