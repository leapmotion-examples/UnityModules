# Getting Started {#mainpage}

The UnityModules repository is designed to be opened in 2019.2. However, the most recent release (4.8.0) has been tested with Unity 2019.4 LTS and Unity 2020.2. If you are sourcing UnityModules directly from this repository, your mileage may vary with earlier versions of Unity.

# Installing the modules {#installing-unity-modules}

Core is the only dependency for any Module. If you are installing any Unity Modules for a new project, you need only to download the latest stable versions of Core and any modules of interest from [our developer page][devpage]. You can import new modules into your project at any time; to upgrade a module, simply delete its folder from your project to get rid of all of the files in that module, then import the latest module version.

[devpage]: https://developer.leapmotion.com/unity/ "Ultraleap Unity Developer Page"

This documentation is updated on a rolling basis, generally to match module releases. It is likely out-of-sync with the `develop` branch in the UnityModules [repository][unitymodules-repo].

[unitymodules-repo]: https://github.com/leapmotion/UnityModules

For more details on upgrading modules, or if your project already contains older versions of our modules, check out our reference for @ref upgrading-unity-modules (it's easy).

# Which modules are right for you? {#choosing-modules}

- @ref core is the dependency for all other modules. Ultraleap's Core Assets provide the foundation for VR applications with a minimal interface between Unity and the Leap Motion Controller and a collection of garbageless C# utilities. With Core, you can render [a basic set of Leap hands][basicset], [attach objects to hand joints][attachmenthands], and find generally-useful utilities like a non-allocating LINQ implementation and a Tween library.

[basicset]: @ref a-basic-set-of-leap-hands
[attachmenthands]: @ref attaching-objects-to-hand-joints

- The @ref interaction-engine provides physics representations of hands and VR controllers fine-tuned with interaction heuristics to provide a fully-featured interaction API: grasping, throwing, stable 'soft' collision feedback, and proximity. It also comes with with a suite of examples and prefabs to power **reliable, stable 3D user interfaces** as well as any physics-critical experiences akin to Ultraleap's [Blocks][] demo.
  
[Blocks]: https://www.youtube.com/watch?v=oZ_53T2jBGg "Ultraleap Blocks Demo"

- The @ref hands-module provides the tools you need to rig your 3D hand assets to work with Leap hands, including the powerful @ref #hands-auto-rigging-tool.

# Troubleshooting {#troubleshooting}

If you're not seeing hands in your Unity application, check the following first:

- If you aren't using a custom camera rig, make sure you have the Leap Rig (XR rig) prefab or the LeapHandController (desktop-mounted controller) prefab in your scene.

- If you're using a custom camera or controller rig, check your application's scene hierarchy while your hand is in front of the sensor. Make sure the hand isn't simply being rendered in the wrong place relative to your camera. Additionally, double-check you have your rig is set up correct: @ref xr-rig-setup.

- Check the Ultraleap icon in your task bar, which you'll have if you've installed the service from the [developer SDK][devsdk] for your platform. If it's dark, the Leap service isn't sending any data. Double-check your Ultraleap hardware is plugged in, and check that the Ultraleap service is running.

- Open the Ultraleap Visualizer from the right-click menu of the Ultraleap icon in your task bar. If you see hands in the Visualizer, the problem is mostly likely somewhere in Unity; otherwise, the problem is outside Unity.

[devsdk]: https://developer.leapmotion.com/get-started/ "Ultraleap Developer SDK"
