# OpenXR Hand Tracking: Preview

This is an experimental example of using OpenXR hand tracking with the Ultraleap UnityModules in Unity 2020.2 using the Microsoft Mixed Reality OpenXR Plugin Preview package.

It demonstrates how you can use OpenXR hand data in Unity to drive the hand visualisation and interactions through the existing UnityModule features.

Note: This does rely on preview packages that may change outside our control and is work in progress so may not work as expected.

## Requirements

### Unity 2020.2

This can be downloaded from the Unity Hub.

### OpenXR Tracking API Layer
Adds `XR_EXT_hand_tracking` support to an existing OpenXR runtime.

* [Ultraleap OpenXR Hand Tracking API Layer] (https://github.com/ultraleap/OpenXRHandTracking/releases/tag/1.0.0-beta2)

* Download the package and install as per the included instructions

### OpenXR Plugin for Unity

* [Microsoft Mixed Reality OpenXR Plugin] (https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/openxr-getting-started)

Note: Tested with V0.1.4 of the Package

### Setup

* Create a new Unity Project
* Import these Unity Modules into your project
* Follow the instructions provided with the Microsoft Mixed Reality Plugin to add the required package to your project and enable the plugin
* Open the included example scene to test.



## Known Issues

* The timing of hand updates between rendering and physics for interations is not perfect, so some jitter can occur. Set the Fixed Timestep to 0.01111 to minimise the impact.

* Not all features of the Leap.Hand are populated by the provider. All joint transforms for the hand are updated, but additional features such as pinch strength are not.

* The OpenXR data in the Microsoft plugin is designed around an eye level experience, whereas Unity by default uses a floor level. Ensure there is an  `EyeLevelOverride` component applied to an object in your scene to ensure hands appear in the correct location for your head.

