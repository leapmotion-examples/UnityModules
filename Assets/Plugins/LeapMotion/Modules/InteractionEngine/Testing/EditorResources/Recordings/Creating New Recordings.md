

Set up your Scene
=================

1. Open up the 'test' scene; this is just an empty scene.
2. Drag in the stage prefab for your recording (usually the Default Stage).
3. Drag in IE Test Recording Rig prefab from the Recordings folder.


Create your Recording
=====================

4. The RecorderBuddy script allows you to make timed recordings when you press Space. Confirm that it's on your Recording Rig prefab.
5. Press Play, and be sure to click in the game window to give it input focus.
6. Use the 'R' key to reset your rig to the desired position in space.
7. Press Space and perform your desired action. If you need more than a second, you can adjust the RecorderBuddy's recording duration, but try to keep recordings short. Stop Play mode when done.


Post-process your Recording
===========================

8. Your recording will be saved into the Recordings folder in a new subfolder, probably Hierarchy Recordings 01 unless there are other folders or you changed the target recording name.
9. Drag the "Hierarchy Recorder Raw" prefab into your scene; this is a prefab containing the raw data of your recording.
10. AFTER dragging the "Raw" recording prefab into the scene, use its inspector to adjust post-processing settings on the post-process component.
11. It's almost never a good idea to use the RawLeapRecording setting on the post-process object for Leap hand data. Use the VectorHandRecording setting to save a bunch of disk space.
12. Hit the 'Build Playback Prefab' button when the post-process settings are to your liking. This will produce another prefab that uses the Unity Playables API to be playable by Unity. It'll have a Director attached and everything.
13. Delete the LeapVRTemporalWarping script on the Leap Space object in the rig. Yeah, this step sucks. But the script will annoyingly send null reference exceptions to the console otherwise. Also delete the RecorderBuddy. Ugh.
14. Rename this prefab to something descriptive; put the timeline object in the Recordings folder and the prefab in the EditorResources folder.


Use your Recording
==================

15. You can drag this Playable prefab into a scene at any time and use its Director to play it. It's a full Leap rig and produces real Leap hands; it also has an Interaction Manager and Interaction Hands so it can interact with IE objects in a scene.
16. For IE tests, create a const string with the name of this prefab and you'll be able to load it using utility functions in the Testing scripts to define new tests that use this recording.