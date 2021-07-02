# Ultraleap Unity Modules

To download Ultraleap's latest stable modules as .unitypackages, visit [our Unity developer site][devsite]. Be sure to also check out our [documentation and API reference][um-docs]!

The UnityModules repository is designed to be opened in 2019.2. Our release packages should support 5.6.2 and up, for stable Unity releases. However, this release has been tested with Unity 2019.4 LTS and Unity 2020.2. There is a separate release that supports Unity 2018.4 LTS. If you hit an issue with version support, please submit a ticket with any details!

Since Unity Modules 4.5.0, our [releases page][releases] on this repository is the official source for distribution. This version of UnityModules fixes compilation warnings in Unity 2019 and includes some fixes for newer Unity features like the Scriptable Render Pipeline (in 2019.1 and beyond).

### Notes on SpatialTracking import errors.

* Unity 2019.4 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install [XR Legacy Input Helpers](http://docs.unity3d.com/Packages/com.unity.xr.legacyinputhelpers@2.1/manual/index.html).

* Unity 2020.1 users: If you get errors related to "SpatialTracking" upon importing the Core module, you will need to install the [Oculus XR package](http://docs.unity3d.com/Packages/com.unity.xr.oculus@1.4/manual/index.html).

## License

Use of Ultraleap's UnityModules is subject to the [Apache V2 License Agreement][apache].

## This repository

This repository contains code for Ultraleap's Unity Modules, easy-to-use tools for integrating the Ultraleap camera hardware in Unity projects, and various utilities for VR and AR projects.

**Note that this repository also contains code for work-in-progress modules, tentative modules, or older modules that may be unsupported.** We recommend using the packages available on the [developer site][devsite] unless you're planning on contributing or you are otherwise feeling *particularly hardcore*.

[um-docs]: https://leapmotion.github.io/UnityModules/
[devsite]: https://developer.leapmotion.com/unity/ "Leap Motion Unity Developer site"
[wiki]: https://github.com/leapmotion/UnityModules/wiki "Leap Motion Unity Modules Wiki"
[apache]: http://www.apache.org/licenses/LICENSE-2.0 "Apache V2 License"
[releases]: https://github.com/leapmotion/UnityModules/releases
