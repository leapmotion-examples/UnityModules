/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Runtime.InteropServices;
using UnityEngine;

namespace Leap.Unity.Particles {
  
  public class VoxelBinnedParticlesExample : MonoBehaviour {
    public const int MAX_PARTICLES = 1024 * 64;
    public const int MAX_CAPSULES = 1024;

    public const int BOX_SIDE = 64;
    public const int BOX_COUNT = BOX_SIDE * BOX_SIDE * BOX_SIDE;

    [Header("Hands")]
    public LeapProvider _provider;

    [SerializeField]
    public float _handCapsuleRadius = 0.025f;

    [Header("Custom Capsule")]
    public Transform _capsuleA;

    public Transform _capsuleB;

    public float _capsuleRadius;

    [Header("Custom Plane")]
    public Transform _plane;

    [Header("Settings")]
    public Mesh _mesh;

    public ComputeShader _shader;

    public Shader _display, _splatDisplay;

    public bool drawResampledGrid;

    //[SerializeField]
    //public Material _displayMat;

    [StructLayout(LayoutKind.Sequential)]
    private struct Particle {
      public Vector3 position;
      public Vector3 prevPosition;
      public Vector3 color;
      public Vector3 primaryAxis;
      public Vector3 secondaryAxis;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Capsule {
      public Vector3 pointA;
      public Vector3 pointB;
      public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DebugData {
      public uint tests;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SurfaceSplat {
      public Vector3 position;
      public Vector3 normal;
      public Vector3 color;
    }

    // Compute Shader Pass Indices
    private int _integrate, _resolveCollisions, _accumulate_x,
                _accumulate_y, _accumulate_z, _copy, _sort, _spawnSplats;

    // Compute Buffers
    private ComputeBuffer _capsules, _particleFront, _particleBack, _count, 
                          _boxStart, _boxEnd, _debugData, _argBuffer, _surfaceSplats, _splatArgBuffer;

    // The array of collision capsules as supplied by the hand tracking
    private Capsule[] _capsuleArray = new Capsule[MAX_CAPSULES];

    // The surface material of the particles
    private Material _displayMat, _splatDisplayMat;
    private uint[] args = new uint[5];

    void OnEnable() {
      // Construct the Single Triangle Splat Mesh
      Mesh tri = new Mesh();
      tri.vertices = new Vector3[] { 
        new Vector3(-0.5f, 0.0f, 0.0f), 
        new Vector3( 0.5f, 0.0f, 0.0f), 
        new Vector3( 0.0f, 1.0f, 0.0f),
        new Vector3(-0.5f, 0.0f, 0.0f),
        new Vector3( 0.5f, 0.0f, 0.0f),
        new Vector3( 0.0f, 1.0f, 0.0f)};
      tri.uv = new Vector2[] {
        new Vector2(0.0f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(0.5f, 1.0f),
        new Vector2(0.0f, 0.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(0.5f, 1.0f)};
      tri.triangles = new int[] { 2, 1, 0, 3, 4, 5 };
      tri.normals = new Vector3[] { 
        -Vector3.forward, -Vector3.forward, -Vector3.forward,
         Vector3.forward,  Vector3.forward,  Vector3.forward };
      tri.UploadMeshData(false);
      _mesh = tri;

      // Initialize the Compute Buffers
      _capsules      = new ComputeBuffer(MAX_CAPSULES , Marshal.SizeOf(typeof(Capsule)));
      _particleFront = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));
      _particleBack  = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));
      _count         = new ComputeBuffer(BOX_COUNT    , sizeof(uint));
      _boxStart      = new ComputeBuffer(BOX_COUNT    , sizeof(uint));
      _boxEnd        = new ComputeBuffer(BOX_COUNT    , sizeof(uint));
      _debugData     = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(DebugData)));
      _surfaceSplats = new ComputeBuffer(BOX_COUNT    , Marshal.SizeOf(typeof(SurfaceSplat)), ComputeBufferType.Append);
      _surfaceSplats.SetCounterValue(0);

      // Initialize the arg buffers which stores the current number of particles
      _argBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
      args[0] = (uint)_mesh.GetIndexCount(0);
      args[1] = MAX_PARTICLES;
      _argBuffer.SetData(args);

      // Initialize the arg buffers which stores the current number of splats
      _splatArgBuffer = new ComputeBuffer(5, 5*sizeof(uint), ComputeBufferType.IndirectArguments);
      _splatArgBuffer.SetData(new uint[5] { _mesh.GetIndexCount(0), (uint)BOX_COUNT, 0, 0, 0 });

      // Initialize the particle counts in each cell
      uint[] counts = new uint[BOX_COUNT];
      for (int i = 0; i < BOX_COUNT; i++) { counts[i] = 0; }
      _count.SetData(counts);

      // Initialize the particles
      Particle[] particles = new Particle[MAX_PARTICLES];
      Quaternion particleOrientation = Quaternion.Euler(0f, 0f, 60f);
      for (int i = 0; i < MAX_PARTICLES; i++) {
        Vector3 pos = transform.TransformPoint(Random.insideUnitSphere * 0.4f);
        particles[i] = new Particle() {
          position = pos, prevPosition = pos,
          color = new Vector3(Random.value, Random.value, Random.value),
          primaryAxis = particleOrientation * Vector3.right, secondaryAxis = particleOrientation * Vector3.up
        };
      }
      _particleFront.SetData(particles);

      // Find the indices of each pass in the compute shader
      _integrate         = _shader.FindKernel("Integrate");
      _resolveCollisions = _shader.FindKernel("ResolveCollisions");
      _accumulate_x      = _shader.FindKernel("Accumulate_X");
      _accumulate_y      = _shader.FindKernel("Accumulate_Y");
      _accumulate_z      = _shader.FindKernel("Accumulate_Z");
      _copy              = _shader.FindKernel("Copy");
      _sort              = _shader.FindKernel("Sort");
      _spawnSplats       = _shader.FindKernel("SpawnSplats");


      // Bind the buffers to the memory of each pass of the compute shader
      foreach (var index in new int[] { _integrate,
                                      _resolveCollisions,
                                      _accumulate_x, _accumulate_y, _accumulate_z,
                                      _copy, _sort, _spawnSplats }
      ) {
        _shader.SetBuffer(index, "_Capsules",         _capsules);
        _shader.SetBuffer(index, "_ParticleFront",    _particleFront);
        _shader.SetBuffer(index, "_ParticleBack",     _particleBack);
        _shader.SetBuffer(index, "_BinParticleCount", _count);
        _shader.SetBuffer(index, "_BinStart",         _boxStart);
        _shader.SetBuffer(index, "_BinEnd",           _boxEnd);
        _shader.SetBuffer(index, "_DebugData",        _debugData);
      }
      // Bind the Surface Splats Buffer as well
      _shader  .SetBuffer(_spawnSplats, "_SurfaceSplats", _surfaceSplats);

      // Set the Particle Display Material
      _displayMat = new Material(_display);
      _displayMat.SetBuffer("_Particles", _particleFront);

      // Set the Splat Display Material
      _splatDisplayMat = new Material(_splatDisplay);
      _splatDisplayMat.SetBuffer("_SurfaceSplats", _surfaceSplats);
    }

    void OnDisable() {
      // Deallocate the GPU Compute Buffers
      if (_particleFront  != null) _particleFront .Release();
      if (_particleBack   != null) _particleBack  .Release();
      if (_count          != null) _count         .Release();
      if (_boxStart       != null) _boxStart      .Release();
      if (_boxEnd         != null) _boxEnd        .Release();
      if (_argBuffer      != null) _argBuffer     .Release();
      if (_capsules       != null) _capsules      .Release();
      if (_debugData      != null) _debugData     .Release();
      if (_surfaceSplats  != null) _surfaceSplats .Release(); 
      if (_splatArgBuffer != null) _splatArgBuffer.Release();
    }

    void Update() {
      // Update the capsule structs with the latest hand tracking data
      int index = 0;
      if (_provider != null) {
        Frame frame = _provider.CurrentFrame;
        foreach (var hand in frame.Hands) {
          foreach (var finger in hand.Fingers) {
            foreach (var bone in finger.bones) {
              _capsuleArray[index++] = new Capsule() {
                pointA = bone.PrevJoint.ToVector3(),
                pointB = bone.NextJoint.ToVector3(),
                radius = _handCapsuleRadius
              };
            }
          }
        }
      }

      // Copy the capsules and the ground plane data to the compute shader
      _capsules.SetData  (_capsuleArray);
      _shader  .SetInt   ("_CapsuleCount" , index);
      _shader  .SetVector("_PlanePosition", _plane.position);
      _shader  .SetVector("_PlaneNormal"  , _plane.up);

      // Run the simulation multiple times for extra speed!
      for (int i = 0; i < 2; i++) {
        _shader.SetVector("_Center", transform.position);

        using (new ProfilerSample("Integrate")) {
          _shader.Dispatch(_integrate, MAX_PARTICLES / 64, 1, 1);
        }

        using (new ProfilerSample("Accumulate")) {
          _shader.Dispatch(_accumulate_x, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
          _shader.Dispatch(_accumulate_y, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
          _shader.Dispatch(_accumulate_z, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
        }

        using (new ProfilerSample("Copy")) {
          _shader.Dispatch(_copy, BOX_COUNT / 64, 1, 1);
        }
        using (new ProfilerSample("Sort")) {
          _shader.Dispatch(_sort, MAX_PARTICLES / 64, 1, 1);
        }

        using (new ProfilerSample("Resolve Collisions")) {
          _shader.Dispatch(_resolveCollisions, MAX_PARTICLES / 64, 1, 1);
        }
      }

      //DebugData[] data = new DebugData[MAX_PARTICLES];
      //_debugData.GetData(data);
      //Debug.Log("##########");
      //Debug.Log(data[1000].tests);

      // Update Splats
      _surfaceSplats.SetCounterValue(0);
      using (new ProfilerSample("Spawn Surface Splats")) {
        _shader.Dispatch(_spawnSplats, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
      }

      if (Input.GetKeyDown(KeyCode.Space)) {
        drawResampledGrid = !drawResampledGrid;
      }
    }

    void LateUpdate() {


      // Draw Splats
      if (drawResampledGrid) {
        ComputeBuffer.CopyCount(_surfaceSplats, _splatArgBuffer, 4);
        Graphics.DrawMeshInstancedIndirect(_mesh,
                                           0,
                                           _splatDisplayMat,
                                           new Bounds(Vector3.zero, Vector3.one * 10000),
                                           _splatArgBuffer);//, layer: 1, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
      } else {
        Graphics.DrawMeshInstancedIndirect(_mesh,
                                           0,
                                           _displayMat,
                                           new Bounds(Vector3.zero, Vector3.one * 10000),
                                           _argBuffer);//, layer: 1, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
      }
    }

  }
  
}
