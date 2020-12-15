/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Sprites;
#endif
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  public abstract class LeapMesherBase : LeapRenderingMethod<LeapMeshGraphicBase>,
  ISupportsFeature<LeapTextureFeature>,
  ISupportsFeature<LeapSpriteFeature>,
  ISupportsFeature<LeapRuntimeTintFeature>,
  ISupportsFeature<LeapBlendShapeFeature>,
  ISupportsFeature<CustomFloatChannelFeature>,
  ISupportsFeature<CustomVectorChannelFeature>,
  ISupportsFeature<CustomColorChannelFeature>,
  ISupportsFeature<CustomMatrixChannelFeature> {

    [Serializable]
    private class JaggedRects : JaggedArray<Rect> {
      public JaggedRects(int length) : base(length) { }
    }

    public const string MESH_ASSET_NAME = "Mesh Data";
    public const string TEXTURE_ASSET_NAME = "Texture Data";

    public const string UV_0_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_UV_0";
    public const string UV_1_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_UV_1";
    public const string UV_2_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_UV_2";
    public const string UV_3_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_UV_3";
    public const string COLORS_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_COLORS";
    public const string NORMALS_FEATURE = LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_NORMALS";

    public static string GetUvFeature(UVChannelFlags flags) {
      switch (flags) {
        case UVChannelFlags.UV0:
          return UV_0_FEATURE;
        case UVChannelFlags.UV1:
          return UV_1_FEATURE;
        case UVChannelFlags.UV2:
          return UV_2_FEATURE;
        case UVChannelFlags.UV3:
          return UV_3_FEATURE;
        default:
          throw new InvalidOperationException();
      }
    }

    public Action<Texture2D, AtlasUvs> OnPostProcessAtlas;

    #region INSPECTOR FIELDS
    //BEGIN MESH SETTINGS
    [Tooltip("Should this renderer include uv0 coordinates in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useUv0 = true;

    [Tooltip("Should this renderer include uv1 coordinates in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useUv1 = false;

    [Tooltip("Should this renderer include uv2 coordinates in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useUv2 = false;

    [Tooltip("Should this renderer include uv3 coordinates in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useUv3 = false;

    [Tooltip("Should this renderer include vertex colors in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useColors = true;

    [Tooltip("Multiply all vertex colors for all graphics by this color.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private Color _bakedTint = Color.white;

    [Tooltip("Should this renderer include normals in the generated meshes.")]
    [EditTimeOnly, SerializeField, HideInInspector]
    private bool _useNormals = false;

    //BEGIN RENDERING SETTINGS
    [Tooltip("Shader to use to display the graphics.")]
    [EditTimeOnly, SerializeField]
    protected Shader _shader;

    [Tooltip("The layer that the visual representation of the graphics.  It is independent from the layer the graphics occupy.")]
    [SerializeField]
    private SingleLayer _visualLayer = 0;

    [Tooltip("The atlas combines multiple textures into a single texture automatically, allowing each graphic to have a different " +
             "texture but still allow the graphics as a whole to render efficiently.")]
    [SerializeField]
    private AtlasBuilder _atlas = new AtlasBuilder();
    #endregion

    protected GenerationState _generation = GenerationState.GetGenerationState();

    //Internal cache of requirements 
    protected bool _doesRequireColors;
    protected bool _doesRequireNormals;
    protected bool _doesRequireVertInfo;
    protected List<UVChannelFlags> _requiredUvChannels = new List<UVChannelFlags>();

    //Feature lists
    protected List<LeapTextureFeature> _textureFeatures = new List<LeapTextureFeature>();
    protected List<LeapSpriteFeature> _spriteFeatures = new List<LeapSpriteFeature>();
    protected List<LeapRuntimeTintFeature> _tintFeatures = new List<LeapRuntimeTintFeature>();
    protected List<LeapBlendShapeFeature> _blendShapeFeatures = new List<LeapBlendShapeFeature>();
    protected List<CustomFloatChannelFeature> _floatChannelFeatures = new List<CustomFloatChannelFeature>();
    protected List<CustomVectorChannelFeature> _vectorChannelFeatures = new List<CustomVectorChannelFeature>();
    protected List<CustomColorChannelFeature> _colorChannelFeatures = new List<CustomColorChannelFeature>();
    protected List<CustomMatrixChannelFeature> _matrixChannelFeatures = new List<CustomMatrixChannelFeature>();

    //### Generated Data ###
    [SerializeField]
    protected Material _material;
    [SerializeField]
    protected RendererMeshData _meshes;
    [SerializeField]
    protected RendererTextureData _packedTextures;

    //#### Sprite/Texture Remapping ####
    [SerializeField]
    private AtlasUvs _atlasUvs = default(AtlasUvs);
    [NonSerialized]
    protected MaterialPropertyBlock _spriteTextureBlock;

    //#### Tinting ####
    protected const string TINTS_PROPERTY = LeapGraphicRenderer.PROPERTY_PREFIX + "Tints";
    protected List<Vector4> _tintColors = new List<Vector4>();

    //#### Blend Shapes ####
    protected const string BLEND_SHAPE_AMOUNTS_PROPERTY = LeapGraphicRenderer.PROPERTY_PREFIX + "BlendShapeAmounts";
    protected List<float> _blendShapeAmounts = new List<float>();

    //#### Custom Channels ####
    public const string CUSTOM_CHANNEL_KEYWORD = LeapGraphicRenderer.FEATURE_PREFIX + "ENABLE_CUSTOM_CHANNELS";
    protected List<float> _customFloatChannelData = new List<float>();
    protected List<Vector4> _customVectorChannelData = new List<Vector4>();
    protected List<Color> _customColorChannelData = new List<Color>();
    protected List<Matrix4x4> _customMatrixChannelData = new List<Matrix4x4>();

    public Material material {
      get {
        return _material;
      }
    }

#if UNITY_EDITOR
    public virtual bool IsAtlasDirty {
      get {
        return _atlas.isDirty;
      }
    }

    public virtual void RebuildAtlas(ProgressBar progress) {
      Undo.RecordObject(renderer, "Rebuilt atlas");

      progress.Begin(2, "Rebuilding Atlas", "", () => {
        Texture2D[] packedTextures;

        group.GetSupportedFeatures(_textureFeatures);
        _atlas.UpdateTextureList(_textureFeatures);
        _atlas.RebuildAtlas(progress, out packedTextures, out _atlasUvs);

        progress.Begin(3, "", "", () => {
          progress.Step("Post-Process Atlas");
          if (OnPostProcessAtlas != null) {
            for (int i = 0; i < packedTextures.Length; i++) {
              try {
                OnPostProcessAtlas(packedTextures[i], _atlasUvs);
              } catch (Exception e) {
                Debug.LogException(e);
              }
            }
          }
          progress.Step("Saving To Asset");

          //Now that all post-process has finished, finally mark no longer readable
          //Don't change the mipmaps because a post process might have uploaded custom data
          for (int i = 0; i < packedTextures.Length; i++) {
            packedTextures[i].Apply(updateMipmaps: false, makeNoLongerReadable: true);
          }

          _packedTextures.AssignTextures(packedTextures, _textureFeatures.Query().Select(f => f.propertyName).ToArray());

          progress.Begin(_textureFeatures.Count, "", "Loading Into Scene", () => {
            foreach (var feature in _textureFeatures) {
              _material.SetTexture(feature.propertyName, _packedTextures.GetTexture(feature.propertyName));
            }
          });
        });
      });
    }
#endif

    public virtual void GetSupportInfo(List<LeapTextureFeature> features, List<SupportInfo> info) {
      for (int i = 0; i < features.Count; i++) {
        var feature = features[i];
        if (group.features.Query().OfType<LeapSpriteFeature>().Any(s => s.channel == feature.channel)) {
          info[i] = info[i].OrWorse(SupportInfo.Error("Texture features cannot currently share uvs with sprite features."));
        }
      }
    }

    public virtual void GetSupportInfo(List<LeapSpriteFeature> features, List<SupportInfo> info) {
      SupportUtil.OnlySupportFirstFeature<LeapSpriteFeature>(info);

#if UNITY_EDITOR
      #if !UNITY_2020_1_OR_NEWER
      if (!Application.isPlaying) {
        Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);
      }
      #endif

      for (int i = 0; i < features.Count; i++) {
        var feature = features[i];

        if (!feature.AreAllSpritesPacked()) {
          info[i] = SupportInfo.Error("Not all sprites are packed.");
        }

        if (!feature.AreAllSpritesOnSameTexture()) {
          info[i] = SupportInfo.Error("Not all sprites are packed into same atlas.");
        }
      }
#endif

      for (int i = 0; i < features.Count; i++) {
        var feature = features[i];
        if (group.features.Query().OfType<LeapTextureFeature>().Any(s => s.channel == feature.channel)) {
          info[i] = info[i].OrWorse(SupportInfo.Error("Sprite features cannot currently share uvs with texture features."));
        }
      }
    }

    public virtual void GetSupportInfo(List<LeapRuntimeTintFeature> features, List<SupportInfo> info) {
      SupportUtil.OnlySupportFirstFeature<LeapRuntimeTintFeature>(info);
    }

    public virtual void GetSupportInfo(List<LeapBlendShapeFeature> features, List<SupportInfo> info) {
      SupportUtil.OnlySupportFirstFeature<LeapBlendShapeFeature>(info);
    }

    //Full unconditional support for all custom channels
    public virtual void GetSupportInfo(List<CustomFloatChannelFeature> features, List<SupportInfo> info) { }
    public virtual void GetSupportInfo(List<CustomVectorChannelFeature> features, List<SupportInfo> info) { }
    public virtual void GetSupportInfo(List<CustomColorChannelFeature> features, List<SupportInfo> info) { }
    public virtual void GetSupportInfo(List<CustomMatrixChannelFeature> features, List<SupportInfo> info) { }

    public override void OnEnableRenderer() {
      loadAllSupportedFeatures();
      prepareMaterial();

      //Sprite textures cannot be accessed at edit time, so we need to make sure
      //to upload them to the material right as the renderer is enabled
      if (_spriteFeatures != null) {
        uploadSpriteTextures();
      }
    }

    public override void OnDisableRenderer() { }

    public override void OnUpdateRenderer() {
      if (_material == null) {
        prepareMaterial();
      }

      updateTinting();
      updateBlendShapes();
      updateCustomChannels();
    }

#if UNITY_EDITOR
    public override void OnDisableRendererEditor() {
      base.OnDisableRendererEditor();
    }

    public override void OnUpdateRendererEditor() {
      base.OnUpdateRendererEditor();

      _meshes.Validate(this);
      _packedTextures.Validate(this);

      setupForBuilding();

      PreventDuplication(ref _material);

      buildMesh();
    }
#endif

    #region UPDATE
    protected virtual void updateTinting() {
      foreach (var tintFeature in _tintFeatures) {
        if (!tintFeature.isDirtyOrEditTime) continue;

        using (new ProfilerSample("Update Tinting")) {
          if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
            tintFeature.featureData.Query().Select(d => (Vector4)d.color.linear).FillList(_tintColors);
          } else {
            tintFeature.featureData.Query().Select(d => (Vector4)d.color).FillList(_tintColors);
          }
          _material.SetVectorArraySafe(TINTS_PROPERTY, _tintColors);
        }
      }
    }

    protected virtual void updateBlendShapes() {
      foreach (var blendShapeFeature in _blendShapeFeatures) {
        if (!blendShapeFeature.isDirtyOrEditTime) continue;

        using (new ProfilerSample("Update Blend Shapes")) {
          blendShapeFeature.featureData.Query().Select(d => d.amount).FillList(_blendShapeAmounts);
          _material.SetFloatArraySafe(BLEND_SHAPE_AMOUNTS_PROPERTY, _blendShapeAmounts);
        }
      }
    }

    protected virtual void updateCustomChannels() {
      using (new ProfilerSample("Update Custom Channels")) {
        foreach (var floatChannelFeature in _floatChannelFeatures) {
          if (!floatChannelFeature.isDirtyOrEditTime) continue;

          floatChannelFeature.featureData.Query().Select(d => d.value).FillList(_customFloatChannelData);
          _material.SetFloatArraySafe(floatChannelFeature.channelName, _customFloatChannelData);
        }

        foreach (var vectorChannelFeature in _vectorChannelFeatures) {
          if (!vectorChannelFeature.isDirtyOrEditTime) continue;

          vectorChannelFeature.featureData.Query().Select(d => d.value).FillList(_customVectorChannelData);
          _material.SetVectorArraySafe(vectorChannelFeature.channelName, _customVectorChannelData);
        }

        foreach (var colorChannelFeature in _colorChannelFeatures) {
          if (!colorChannelFeature.isDirtyOrEditTime) continue;

          colorChannelFeature.featureData.Query().Select(d => d.value).FillList(_customColorChannelData);
          _material.SetColorArraySafe(colorChannelFeature.channelName, _customColorChannelData);
        }

        foreach (var matrixChannelFeature in _matrixChannelFeatures) {
          if (!matrixChannelFeature.isDirty) continue;

          matrixChannelFeature.featureData.Query().Select(d => d.value).FillList(_customMatrixChannelData);
          _material.SetMatrixArraySafe(matrixChannelFeature.channelName, _customMatrixChannelData);
        }
      }
    }
    #endregion

    #region GENERATION SETUP
    protected virtual void setupForBuilding() {
      using (new ProfilerSample("Mesh Setup")) {
        MeshCache.Clear();
        loadAllSupportedFeatures();
        prepareMeshes();
        prepareMaterial();

        if (_textureFeatures.Count != 0) {
          _atlas.UpdateTextureList(_textureFeatures);
        }

        if (_spriteFeatures.Count != 0) {
#if UNITY_EDITOR && !UNITY_2020_1_OR_NEWER
          Packer.RebuildAtlasCacheIfNeeded(EditorUserBuildSettings.activeBuildTarget);
#endif
          extractSpriteRects();
          uploadSpriteTextures();
        }
      }
    }

    protected virtual void loadAllSupportedFeatures() {
      using (new ProfilerSample("Load All Supported Features")) {
        group.GetSupportedFeatures(_textureFeatures);
        group.GetSupportedFeatures(_spriteFeatures);
        group.GetSupportedFeatures(_tintFeatures);
        group.GetSupportedFeatures(_blendShapeFeatures);

        group.GetSupportedFeatures(_floatChannelFeatures);
        group.GetSupportedFeatures(_vectorChannelFeatures);
        group.GetSupportedFeatures(_colorChannelFeatures);
        group.GetSupportedFeatures(_matrixChannelFeatures);

        _doesRequireColors = doesRequireMeshColors();
        _doesRequireNormals = doesRequireMeshNormals();
        _doesRequireVertInfo = doesRequireVertInfo();

        _requiredUvChannels.Clear();
        foreach (var channel in MeshUtil.allUvChannels) {
          if (channel == UVChannelFlags.UV3 && _doesRequireVertInfo) continue;

          if (doesRequireUvChannel(channel)) {
            _requiredUvChannels.Add(channel);
          }
        }
      }
    }

    protected virtual void prepareMeshes() {
      _meshes.Clear();
      _generation.Reset();
    }

    protected virtual void prepareMaterial() {
      if (_material == null) {
        _material = new Material(_shader);
      }

#if UNITY_EDITOR
      Undo.RecordObject(_material, "Touched material");
#endif

      _material.shader = _shader;
      _material.name = "Procedural Graphic Material";

      foreach (var keyword in _material.shaderKeywords) {
        _material.DisableKeyword(keyword);
      }

      if (_spriteTextureBlock == null) {
        _spriteTextureBlock = new MaterialPropertyBlock();
      }
      _spriteTextureBlock.Clear();

      if (_doesRequireColors) {
        _material.EnableKeyword(COLORS_FEATURE);
      }

      if (_doesRequireNormals) {
        _material.EnableKeyword(NORMALS_FEATURE);
      }

      foreach (var channel in _requiredUvChannels) {
        _material.EnableKeyword(GetUvFeature(channel));
      }

      if (_customColorChannelData.Count > 0 ||
         _customFloatChannelData.Count > 0 ||
         _customVectorChannelData.Count > 0 ||
         _customMatrixChannelData.Count > 0) {
        _material.EnableKeyword(CUSTOM_CHANNEL_KEYWORD);
      }

      if (_textureFeatures.Count != 0) {
        foreach (var feature in _textureFeatures) {
          _material.SetTexture(feature.propertyName, _packedTextures.GetTexture(feature.propertyName));
        }
      }

      if (_tintFeatures.Count != 0) {
        _material.EnableKeyword(LeapRuntimeTintFeature.FEATURE_NAME);
      }

      if (_blendShapeFeatures.Count != 0) {
        _material.EnableKeyword(LeapBlendShapeFeature.FEATURE_NAME);
      }
    }

    protected virtual void extractSpriteRects() {
      using (new ProfilerSample("Extract Sprite Rects")) {
        foreach (var spriteFeature in _spriteFeatures) {
          for (int i = 0; i < spriteFeature.featureData.Count; i++) {
            var dataObj = spriteFeature.featureData[i];
            var sprite = dataObj.sprite;
            if (sprite == null) continue;

            Rect rect;
            if (SpriteAtlasUtil.TryGetAtlasedRect(sprite, out rect)) {
              _atlasUvs.SetRect(spriteFeature.channel.Index(), sprite, rect);
            } else {
              _atlasUvs.SetRect(spriteFeature.channel.Index(), sprite, default(Rect));
            }
          }
        }
      }
    }

    protected virtual void uploadSpriteTextures() {
      using (new ProfilerSample("Upload Sprite Textures")) {
        foreach (var spriteFeature in _spriteFeatures) {
          var tex = spriteFeature.featureData.Query().
                                       Select(d => d.sprite).
                                       Where(s => s != null).
#if UNITY_EDITOR
                                       Select(s => SpriteUtility.GetSpriteTexture(s, getAtlasData: true)).
#else
                                       Select(s => s.texture).
#endif
                                       FirstOrDefault();

          if (tex != null) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
              _spriteTextureBlock.SetTexture(spriteFeature.propertyName, tex);
            } else
#endif
            {
              _material.SetTexture(spriteFeature.propertyName, tex);
            }
          }
        }
      }
    }
    #endregion

    #region MESH GENERATION
    protected virtual void buildMesh() {
      using (new ProfilerSample("Build Mesh")) {
        beginMesh();

        for (_generation.graphicIndex = 0;
             _generation.graphicIndex < group.graphics.Count;
             _generation.graphicIndex++) {
          //For re-generation of everything, graphicIndex == graphicId
          _generation.graphicId = _generation.graphicIndex;
          _generation.graphic = group.graphics[_generation.graphicIndex] as LeapMeshGraphicBase;
          buildGraphic();
        }

        if (_generation.isGenerating) {
          finishAndAddMesh();
        }
      }

      _meshes.ClearPool();
    }

    protected virtual void buildGraphic() {
      using (new ProfilerSample("Build Graphic")) {
        refreshMeshData();
        if (_generation.graphic.mesh == null) return;

        buildTopology();

        if (_doesRequireColors) {
          buildColors();
        }

        foreach (var channel in _requiredUvChannels) {
          buildUvs(channel);
        }

        if (_doesRequireVertInfo) {
          buildVertInfo();
        }

        foreach (var blendShapeFeature in _blendShapeFeatures) {
          buildBlendShapes(blendShapeFeature.featureData[_generation.graphicIndex]);
        }

        _generation.graphic.isRepresentationDirty = false;
      }
    }

    protected virtual void refreshMeshData() {
      using (new ProfilerSample("Refresh Mesh Data")) {
        _generation.graphic.RefreshMeshData();
      }
    }

    protected virtual void buildTopology() {
      using (new ProfilerSample("Build Topology")) {
        var topology = MeshCache.GetTopology(_generation.graphic.mesh);

        int vertOffset = _generation.verts.Count;
        for (int i = 0; i < topology.tris.Length; i++) {
          _generation.tris.Add(topology.tris[i] + vertOffset);
        }

        if (_doesRequireNormals) {
          var normals = MeshCache.GetNormals(_generation.graphic.mesh);
          for (int i = 0; i < topology.verts.Length; i++) {
            Vector3 meshVert;
            Vector3 meshNormal;
            graphicVertNormalToMeshVertNormal(topology.verts[i], normals[i], out meshVert, out meshNormal);

            _generation.verts.Add(meshVert);
            _generation.normals.Add(meshNormal);
          }
        } else {
          for (int i = 0; i < topology.verts.Length; i++) {
            _generation.verts.Add(graphicVertToMeshVert(topology.verts[i]));
          }
        }
      }
    }

    protected virtual void buildColors() {
      using (new ProfilerSample("Build Colors")) {
        Color totalTint;
        if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
          totalTint = _bakedTint.linear * _generation.graphic.vertexColor.linear;
        } else {
          totalTint = _bakedTint * _generation.graphic.vertexColor;
        }

        var colors = MeshCache.GetColors(_generation.graphic.mesh);
        if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
          for (int i = 0; i < colors.Length; i++) {
            _generation.colors.Add(colors[i].linear * totalTint);
          }
        } else {
          for (int i = 0; i < colors.Length; i++) {
            _generation.colors.Add(colors[i] * totalTint);
          }
        }
      }
    }

    protected virtual void buildUvs(UVChannelFlags channel) {
      using (new ProfilerSample("Build Uvs")) {
        var uvs = MeshCache.GetUvs(_generation.graphic.mesh, channel);
        var targetList = _generation.uvs[channel.Index()];

        targetList.AddRange(uvs);

        //If we cannot remap this channel, just return
        if ((_generation.graphic.remappableChannels & channel) == 0) {
          return;
        }

        UnityEngine.Object key;
        if (_textureFeatures.Count > 0) {
          key = _textureFeatures[0].featureData[_generation.graphicIndex].texture;
        } else if (_spriteFeatures.Count > 0) {
          key = _spriteFeatures[0].featureData[_generation.graphicIndex].sprite;
        } else {
          return;
        }

        Rect rect = _atlasUvs.GetRect(channel.Index(), key);
        MeshUtil.RemapUvs(targetList, rect, uvs.Count);
      }
    }

    protected virtual void buildVertInfo() {
      using (new ProfilerSample("Build Special Uv3")) {
        _generation.vertInfo.Append(_generation.graphic.mesh.vertexCount,
                                    new Vector4(0, 0, 0, _generation.graphicId));
      }
    }

    protected virtual void buildBlendShapes(LeapBlendShapeData blendShapeData) {
      using (new ProfilerSample("Build Blend Shapes")) {
        List<Vector3> blendVerts = Pool<List<Vector3>>.Spawn();

        try {
          if (!blendShapeData.TryGetBlendShape(blendVerts)) {
            return;
          }

          int offset = _generation.verts.Count - blendVerts.Count;
          for (int i = 0; i < blendVerts.Count; i++) {
            Vector3 shapeVert = blendVerts[i];
            Vector3 delta = blendShapeDelta(shapeVert, _generation.verts[i + offset]);

            Vector4 currVertInfo = _generation.vertInfo[i + offset];
            currVertInfo.x = delta.x;
            currVertInfo.y = delta.y;
            currVertInfo.z = delta.z;
            _generation.vertInfo[i + offset] = currVertInfo;
          }
        } finally {
          blendVerts.Clear();
          Pool<List<Vector3>>.Recycle(blendVerts);
        }
      }
    }

    protected virtual Vector3 blendShapeDelta(Vector3 shapeVert, Vector3 originalVert) {
      return graphicVertToMeshVert(shapeVert) - originalVert;
    }

    protected virtual void beginMesh(Mesh mesh = null) {
      Assert.IsNull(_generation.mesh, "Cannot begin a new mesh without finishing the current mesh.");

      _generation.Reset();

      if (mesh == null) {
        mesh = _meshes.GetMeshFromPoolOrNew();
      } else {
        mesh.Clear(keepVertexLayout: false);
      }

      mesh.name = "Procedural Graphic Mesh";
      mesh.hideFlags = HideFlags.None;

      _generation.mesh = mesh;
    }

    protected virtual void finishMesh(bool deleteEmptyMeshes = true) {
      using (new ProfilerSample("Finish Mesh")) {

        if (_generation.verts.Count == 0 && deleteEmptyMeshes) {
          if (!Application.isPlaying) {
            UnityEngine.Object.DestroyImmediate(_generation.mesh, allowDestroyingAssets: true);
          }
          _generation.mesh = null;
          return;
        }

        _generation.mesh.SetVertices(_generation.verts);
        _generation.mesh.SetTriangles(_generation.tris, 0);

        if (_generation.normals.Count == _generation.verts.Count) {
          _generation.mesh.SetNormals(_generation.normals);
        }

        if (_generation.vertInfo.Count == _generation.verts.Count) {
          _generation.mesh.SetTangents(_generation.vertInfo);
        }

        if (_generation.colors.Count == _generation.verts.Count) {
          _generation.mesh.SetColors(_generation.colors);
        }

        foreach (var channel in _requiredUvChannels) {
          _generation.mesh.SetUVs(channel.Index(), _generation.uvs[channel.Index()]);
        }

        postProcessMesh();
      }
    }

    protected virtual void finishAndAddMesh(bool deleteEmptyMeshes = true) {
      finishMesh(deleteEmptyMeshes);

      if (_generation.mesh != null) {
        _meshes.AddMesh(_generation.mesh);
        _generation.mesh = null;
      }
    }

    protected virtual void postProcessMesh() { }

    protected struct GenerationState {
      public LeapMeshGraphicBase graphic;
      public int graphicIndex;
      public int graphicId;
      public Mesh mesh;

      public List<Vector3> verts;
      public List<Vector3> normals;
      public List<Vector4> vertInfo;
      public List<int> tris;
      public List<Color> colors;
      public List<Vector4>[] uvs;

      public bool isGenerating {
        get {
          return mesh != null;
        }
      }

      public static GenerationState GetGenerationState() {
        GenerationState state = new GenerationState();

        state.verts = new List<Vector3>();
        state.normals = new List<Vector3>();
        state.vertInfo = new List<Vector4>();
        state.tris = new List<int>();
        state.colors = new List<Color>();
        state.uvs = new List<Vector4>[4] {
          new List<Vector4>(),
          new List<Vector4>(),
          new List<Vector4>(),
          new List<Vector4>()
        };

        return state;
      }

      public void Reset() {
        verts.Clear();
        normals.Clear();
        vertInfo.Clear();
        tris.Clear();
        colors.Clear();
        for (int i = 0; i < 4; i++) {
          uvs[i].Clear();
        }
      }
    }
    #endregion

    #region UTILITY

    protected IEnumerable<UVChannelFlags> enabledUvChannels {
      get {
        if (_useUv0) yield return UVChannelFlags.UV0;
        if (_useUv1) yield return UVChannelFlags.UV1;
        if (_useUv2) yield return UVChannelFlags.UV2;
        if (_useUv3) yield return UVChannelFlags.UV3;
      }
    }

    protected virtual bool doesRequireMeshColors() {
      return _useColors;
    }

    protected virtual bool doesRequireMeshNormals() {
      return _useNormals;
    }

    protected virtual bool doesRequireUvChannel(UVChannelFlags channel) {
      switch (channel) {
        case UVChannelFlags.UV0:
          return _useUv0;
        case UVChannelFlags.UV1:
          return _useUv1;
        case UVChannelFlags.UV2:
          return _useUv2;
        case UVChannelFlags.UV3:
          return _useUv3;
        default:
          throw new ArgumentException("Invalid channel argument.");
      }
    }

    protected void drawMesh(Mesh mesh, Matrix4x4 transform) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Graphics.DrawMesh(mesh, transform, _material, _visualLayer, null, 0, _spriteTextureBlock);
      } else
#endif
      {
        Graphics.DrawMesh(mesh, transform, _material, _visualLayer);
      }
    }

    protected virtual bool doesRequireVertInfo() {
      return _tintFeatures.Count != 0 ||
             _blendShapeFeatures.Count != 0 ||
             _customFloatChannelData.Count != 0 ||
             _customVectorChannelData.Count != 0 ||
             _customMatrixChannelData.Count != 0 ||
             _customColorChannelData.Count != 0;
    }

    protected abstract Vector3 graphicVertToMeshVert(Vector3 vertex);
    protected abstract void graphicVertNormalToMeshVertNormal(Vector3 vertex, Vector3 normal, out Vector3 meshVert, out Vector3 meshNormal);
    #endregion
  }
}
