# Deprecated Modules {#deprecated-modules}

These modules are **still compatible** as of Core 4.3.0, but may become unsupported in the future.
  
- [Interaction Engine 0.3.1 (compatibility release)][ie-beta]

  **Warning:** This version of the Interaction Engine is incompatible with the latest version of the Interaction Engine in the same project. Support for this version of the Interaction Engine is limited. Only existing projects that contained earlier versions of the Interaction Engine beta version (pre-1.0.0) are recommended to upgrade to this module, which will preserve the project's script linkages. Upgrading from pre-1.0.0 versions of the Interaction Engine to 1.0.0 or later will break certain references in your project's scenes and require some work to restore functionality. If you're upgrading to 1.0.0 or higher from this version, be sure to delete all files from this version first.

[ie-beta]: https://developer.leapmotion.com/releases/interaction-engine-031 "(Deprecated) Interaction Engine Beta"

- [UI Input Module 1.2.1][ui-input]

  The UI Input Module features a Leap Event System and a collection of prefabs that allow you to build UGUI (Canvas-based, flat) interfaces for your VR applications. This module's functionality is replaced largely by the Interaction Engine (3D physical interfaces) and the Graphic Renderer (easy curvature, mobile-friendly rendering).
  
[ui-input]: https://developer.leapmotion.com/releases/ui-input-module-121 "(Deprecated) UI Input Module"

- [Graphic Renderer Module 0.1.3][graphic-renderer]
  
  With the Graphic Renderer, you can **render an entire curved, 3D, dynamic interface in 1-3 draw calls**. The Graphic Renderer can be used in tandem with the Interaction Engine; 

[graphic-renderer]: https://developer.leapmotion.com/releases/graphic-renderer-013 "(Deprecated) Graphic Renderer Module"

- [Attachments Module 1.0.5][attachments-module]

  The latest version of Core (since 4.2.0) contains an AttachmentHands.cs script that reproduces the functionality of the Attachment Module hands with a customized inspector interface. Attachment Hands as a concept has migrated to Core to satisfy dependencies for other Modules and examples.

[attachments-module]: https://developer.leapmotion.com/releases/attachments-module "(Deprecated) Attachments Module"

- [Detection Examples 1.0.4][detection-utilities]

  The examples in this module demonstrate scripts in Core that have been deprecated and removed in a future Core release, since most of their functionalities have been superseded by more recent Modules or Core features.

[detection-utilities]: https://developer.leapmotion.com/releases/detection-examples-104 "(Deprecated) Detection Utilities"
