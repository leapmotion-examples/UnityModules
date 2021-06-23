# OVRProvider

This is an experimental compatibility layer that allows to use Quest hand tracking with interactions built using the Leap Motion Unity Modules.

## Requirements

### Oculus Integration

* Download the [Oculus Integration] (https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022) from the Asset Store

### Setup

* Create a new Unity Project
* Import the Unity Modules
* Follow [these instructions] (https://developer.oculus.com/documentation/unity/unity-handtracking/) provided by Oculus to set up hand tracking in Unity 
* Open one of the example scenes included with the Unity Modules and replace the traditional LeapServiceProvider/LeapXRServiceProvider with an OVRProvider
* Assign the OVRProvider as to components that reference a LeapProvider (e.g. InteractioHands and HandModelManager)