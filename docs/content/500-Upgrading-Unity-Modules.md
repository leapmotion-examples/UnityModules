# Upgrading Unity Modules {#upgrading-unity-modules}

If your project only contains modules from June 1st, 2017 or onwards, the modules in your project can all be found under the following file structure:

- Core: `Assets/LeapMotion/Core`
- Modules: `Assets/LeapMotion/<module-name>`

To upgrade a module, simply delete the relevant folder in `LeapMotion` and import the latest module package. With rare exception for updates containing significant and breaking changes, your scene files should regain any linkages with Leap Motion assets after you import the latest module.

Note that if you are upgrading Core, it is recommended that you also ensure your modules are up-to-date, especially if you encounter build errors after the upgrade.

# Special upgrade: Old XR Rigs (before Core 4.4.0) {#upgrades-prior-to-4-4-0}

Core `4.4.0` simplifies the Rig Hierarchy, so projects that used older versions of Leap Motion VR rigs in their scenes are likely out-of-date. After pulling in the new Core and modules via the normal upgrade process, you'll want to run the Leap Rig Upgrader tool to detect and automatically upgrade old Leap rigs in these projects.

To upgrade a rig, load the scene containing the rig and run the scanner found in the Ultraleap Unity SDK window (`Window/Leap Motion`). Any rigs that can be automatically upgraded will be detected in the current scene, and you can upgrade them with one click, or review the pending changes and make them yourself. If you encounter any issues with the auto-upgrader, please report them in the [Developer Forum][devforum].

The new rigs should be much easier to understand on their own. For a detailed description of the new standard rig, see the @ref xr-rig-setup section.

[devforum]: https://forums.leapmotion.com/c/development

# Special upgrade: Files (before June 1st, 2017) {#upgrades-prior-to-2017-06-01}

If your project contains modules released prior to June 1st, 2017, your project's Ultraleap files are slightly more spread out, so you'll want to make sure you delete them all before pulling anything in from a more recent Unity Modules package.

Ultraleap's Unity Modules prior to June 1st placed its files in the following locations in your Assets folder:

- Core: `Assets/LeapMotion` and `Assets/Plugins`
- Modules: `Assets/LeapMotionModules`

To upgrade to the latest version of Core and the latest Modules:

1. Delete the Core module's scripts.
  - Delete `Assets/LeapMotion`.
2. Delete other modules' scripts.
  - Delete `Assets/LeapMotionModules`
3. Delete Leap plugins from their old locations.
  - Delete `Assets/Plugins/LeapCSharp.NET3.5.dll`
  - Delete `Assets/Plugins/libLeapC.so`
  - Delete `Assets/Plugins/x86/LeapC.dll`
  - Delete `Assets/Plugins/x86_64/LeapC.dll`
  - Delete `donotdelete.txt`. Who put that there, anyway?

With these steps completed, you're ready to import the latest version of Core and any desired modules into your project.
