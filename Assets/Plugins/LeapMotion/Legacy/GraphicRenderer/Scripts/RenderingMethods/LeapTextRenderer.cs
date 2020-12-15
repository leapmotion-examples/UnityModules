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
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Text")]
  [Serializable]
  public class LeapTextRenderer : LeapRenderingMethod<LeapTextGraphic>, ISupportsAddRemove {
    public const string DEFAULT_FONT = "Arial.ttf";
    public const string DEFAULT_SHADER = "LeapMotion/GraphicRenderer/Text/Dynamic";
    public const float SCALE_CONSTANT = 0.001f;

    [EditTimeOnly, SerializeField]
    private Font _font = default(Font);

    [EditTimeOnly, SerializeField]
    private float _dynamicPixelsPerUnit = 1.0f;

    [EditTimeOnly, SerializeField]
    public bool _useColor = true;

    [EditTimeOnly, SerializeField]
    public Color _globalTint = Color.white;

    [Header("Rendering Settings")]
    [EditTimeOnly, SerializeField]
    private Shader _shader = default(Shader);

    [EditTimeOnly, SerializeField]
    private float _scale = 1f;

    #pragma warning disable 0649
    [SerializeField]
    private RendererMeshData _meshData;

    [SerializeField]
    private Material _material;
    #pragma warning restore 0649

    //Curved space
    private const string CURVED_PARAMETERS = LeapGraphicRenderer.PROPERTY_PREFIX + "Curved_GraphicParameters";
    private List<Matrix4x4> _curved_worldToAnchor = new List<Matrix4x4>();
    private List<Vector4> _curved_graphicParameters = new List<Vector4>();

    public override SupportInfo GetSpaceSupportInfo(LeapSpace space) {
      return SupportInfo.FullSupport();
    }

    public void OnAddRemoveGraphics(List<int> dirtyIndexes) {
      while (_meshData.Count > group.graphics.Count) {
        _meshData.RemoveMesh(_meshData.Count - 1);
      }

      while (_meshData.Count < group.graphics.Count) {
        group.graphics[_meshData.Count].isRepresentationDirty = true;
        _meshData.AddMesh(new Mesh());
      }
    }

    public override void OnEnableRenderer() {
      foreach (var graphic in group.graphics) {
        var textGraphic = graphic as LeapTextGraphic;
        _font.RequestCharactersInTexture(textGraphic.text);
      }

      generateMaterial();

      Font.textureRebuilt += onFontTextureRebuild;
    }

    public override void OnDisableRenderer() {
      Font.textureRebuilt -= onFontTextureRebuild;
    }

    public override void OnUpdateRenderer() {
      using (new ProfilerSample("Update Text Renderer")) {
        ensureFontIsUpToDate();

        for (int i = 0; i < group.graphics.Count; i++) {
          var graphic = group.graphics[i] as LeapTextGraphic;

          if (graphic.isRepresentationDirtyOrEditTime || graphic.HasRectChanged()) {
            generateTextMesh(i, graphic, _meshData[i]);
          }
        }

        if (renderer.space == null) {
          using (new ProfilerSample("Draw Meshes")) {
            for (int i = 0; i < group.graphics.Count; i++) {
              var graphic = group.graphics[i];
              if (graphic.isActiveAndEnabled) {
                Graphics.DrawMesh(_meshData[i], graphic.transform.localToWorldMatrix, _material, 0);
              }
            }
          }
        } else if (renderer.space is LeapRadialSpace) {
          var curvedSpace = renderer.space as LeapRadialSpace;

          using (new ProfilerSample("Build Material Data And Draw Meshes")) {
            _curved_worldToAnchor.Clear();
            _curved_graphicParameters.Clear();
            for (int i = 0; i < _meshData.Count; i++) {
              var graphic = group.graphics[i];
              if (!graphic.isActiveAndEnabled) {
                _curved_graphicParameters.Add(Vector4.zero);
                _curved_worldToAnchor.Add(Matrix4x4.identity);
                continue;
              }

              var transformer = graphic.anchor.transformer;

              Vector3 localPos = renderer.transform.InverseTransformPoint(graphic.transform.position);

              Matrix4x4 mainTransform = renderer.transform.localToWorldMatrix * transformer.GetTransformationMatrix(localPos);
              Matrix4x4 deform = renderer.transform.worldToLocalMatrix * Matrix4x4.TRS(renderer.transform.position - graphic.transform.position, Quaternion.identity, Vector3.one) * graphic.transform.localToWorldMatrix;
              Matrix4x4 total = mainTransform * deform;

              _curved_graphicParameters.Add((transformer as IRadialTransformer).GetVectorRepresentation(graphic.transform));
              _curved_worldToAnchor.Add(mainTransform.inverse);

              Graphics.DrawMesh(_meshData[i], total, _material, 0);
            }
          }

          using (new ProfilerSample("Upload Material Data")) {
            _material.SetFloat(SpaceProperties.RADIAL_SPACE_RADIUS, curvedSpace.radius);
            _material.SetMatrixArraySafe("_GraphicRendererCurved_WorldToAnchor", _curved_worldToAnchor);
            _material.SetMatrix("_GraphicRenderer_LocalToWorld", renderer.transform.localToWorldMatrix);
            _material.SetVectorArraySafe("_GraphicRendererCurved_GraphicParameters", _curved_graphicParameters);
          }
        }
      }
    }

#if UNITY_EDITOR
    public override void OnEnableRendererEditor() {
      base.OnEnableRendererEditor();

      _font = Resources.GetBuiltinResource<Font>(DEFAULT_FONT);
      _shader = Shader.Find(DEFAULT_SHADER);
    }

    public override void OnUpdateRendererEditor() {
      base.OnUpdateRendererEditor();

      if (_font == null) {
        _font = Resources.GetBuiltinResource<Font>(DEFAULT_FONT);
      }

      if (_shader == null) {
        _shader = Shader.Find(DEFAULT_SHADER);
      }

      _meshData.Validate(this);

      //Make sure we have enough meshes to render all our graphics
      while (_meshData.Count > group.graphics.Count) {
        UnityEngine.Object.DestroyImmediate(_meshData[_meshData.Count - 1]);
        _meshData.RemoveMesh(_meshData.Count - 1);
      }

      while (_meshData.Count < group.graphics.Count) {
        _meshData.AddMesh(new Mesh());
      }

      generateMaterial();

      PreventDuplication(ref _material);
    }
#endif

    private void onFontTextureRebuild(Font font) {
      if (font != _font) {
        return;
      }

      foreach (var graphic in group.graphics) {
        graphic.isRepresentationDirty = true;
      }
    }

    private void generateMaterial() {
      if (_material == null) {
        _material = new Material(_font.material);
      }

#if UNITY_EDITOR
      Undo.RecordObject(_material, "Touched material");
#endif

      _material.mainTexture = _font.material.mainTexture;
      _material.name = "Font material";
      _material.shader = _shader;

      foreach (var keyword in _material.shaderKeywords) {
        _material.DisableKeyword(keyword);
      }

      if (renderer.space != null) {
        if (renderer.space is LeapCylindricalSpace) {
          _material.EnableKeyword(SpaceProperties.CYLINDRICAL_FEATURE);
        } else if (renderer.space is LeapSphericalSpace) {
          _material.EnableKeyword(SpaceProperties.SPHERICAL_FEATURE);
        }
      }

      if (_useColor) {
        _material.EnableKeyword(LeapGraphicRenderer.FEATURE_PREFIX + "VERTEX_COLORS");
      }
    }

    private void ensureFontIsUpToDate() {
      CharacterInfo info;
      bool doesNeedRebuild = false;

      for (int i = 0; i < group.graphics.Count; i++) {
        var graphic = group.graphics[i] as LeapTextGraphic;
        int scaledFontSize = Mathf.RoundToInt(graphic.fontSize * _dynamicPixelsPerUnit);

        if (graphic.isRepresentationDirtyOrEditTime) {
          for (int j = 0; j < graphic.text.Length; j++) {
            char character = graphic.text[j];

            if (!_font.GetCharacterInfo(character, out info, scaledFontSize, graphic.fontStyle)) {
              doesNeedRebuild = true;
              break;
            }
          }

          if (doesNeedRebuild) {
            break;
          }
        }
      }

      if (!doesNeedRebuild) {
        return;
      }

      for (int i = 0; i < group.graphics.Count; i++) {
        var graphic = group.graphics[i] as LeapTextGraphic;
        int scaledFontSize = Mathf.RoundToInt(graphic.fontSize * _dynamicPixelsPerUnit);

        graphic.isRepresentationDirty = true;
        _font.RequestCharactersInTexture(graphic.text,
                                         scaledFontSize,
                                         graphic.fontStyle);
      }
    }

    private List<TextWrapper.Line> _tempLines = new List<TextWrapper.Line>();
    private List<Vector3> _verts = new List<Vector3>();
    private List<Vector4> _uvs = new List<Vector4>();
    private List<Color> _colors = new List<Color>();
    private List<int> _tris = new List<int>();
    private void generateTextMesh(int index, LeapTextGraphic graphic, Mesh mesh) {
      using (new ProfilerSample("Generate Text Mesh")) {
        mesh.Clear(keepVertexLayout: false);

        graphic.isRepresentationDirty = false;

        int scaledFontSize = Mathf.RoundToInt(graphic.fontSize * _dynamicPixelsPerUnit);

        //Check for characters not found in the font
        {
          HashSet<char> unfoundCharacters = null;
          CharacterInfo info;
          foreach (var character in graphic.text) {
            if (character == '\n') {
              continue;
            }

            if (unfoundCharacters != null && unfoundCharacters.Contains(character)) {
              continue;
            }

            if (!_font.GetCharacterInfo(character, out info, scaledFontSize, graphic.fontStyle)) {
              if (unfoundCharacters == null) unfoundCharacters = new HashSet<char>();
              unfoundCharacters.Add(character);
              Debug.LogError("Could not find character [" + character + "] in font " + _font + "!");
            }
          }
        }
        
        var text = graphic.text;

        float _charScale = this._scale * SCALE_CONSTANT / _dynamicPixelsPerUnit;
        float _scale = _charScale * graphic.fontSize / _font.fontSize;
        float lineHeight = _scale * graphic.lineSpacing * _font.lineHeight * _dynamicPixelsPerUnit;

        RectTransform rectTransform = graphic.transform as RectTransform;
        float maxWidth;
        if (rectTransform != null) {
          maxWidth = rectTransform.rect.width;
        } else {
          maxWidth = float.MaxValue;
        }

        _widthCalculator.font = _font;
        _widthCalculator.charScale = _charScale;
        _widthCalculator.fontStyle = graphic.fontStyle;
        _widthCalculator.scaledFontSize = scaledFontSize;
        TextWrapper.Wrap(text, graphic.tokens, _tempLines, _widthCalculator.func, maxWidth);

        float textHeight = _tempLines.Count * lineHeight;

        Vector3 origin = Vector3.zero;
        origin.y -= _font.ascent * _scale * _dynamicPixelsPerUnit;

        if (rectTransform != null) {
          origin.y -= rectTransform.rect.y;

          switch (graphic.verticalAlignment) {
            case LeapTextGraphic.VerticalAlignment.Center:
              origin.y -= (rectTransform.rect.height - textHeight) / 2;
              break;
            case LeapTextGraphic.VerticalAlignment.Bottom:
              origin.y -= (rectTransform.rect.height - textHeight);
              break;
          }
        }

        foreach (var line in _tempLines) {

          if (rectTransform != null) {
            origin.x = rectTransform.rect.x;
            switch (graphic.horizontalAlignment) {
              case LeapTextGraphic.HorizontalAlignment.Center:
                origin.x += (rectTransform.rect.width - line.width) / 2;
                break;
              case LeapTextGraphic.HorizontalAlignment.Right:
                origin.x += (rectTransform.rect.width - line.width);
                break;
            }
          } else {
            switch (graphic.horizontalAlignment) {
              case LeapTextGraphic.HorizontalAlignment.Left:
                origin.x = 0;
                break;
              case LeapTextGraphic.HorizontalAlignment.Center:
                origin.x = -line.width / 2;
                break;
              case LeapTextGraphic.HorizontalAlignment.Right:
                origin.x = -line.width;
                break;
            }
          }

          for (int i = line.start; i < line.end; i++) {
            char c = text[i];

            CharacterInfo info;
            if (!_font.GetCharacterInfo(c, out info, scaledFontSize, graphic.fontStyle)) {
              continue;
            }

            int offset = _verts.Count;
            _tris.Add(offset + 0);
            _tris.Add(offset + 1);
            _tris.Add(offset + 2);

            _tris.Add(offset + 0);
            _tris.Add(offset + 2);
            _tris.Add(offset + 3);

            _verts.Add(_charScale * new Vector3(info.minX, info.maxY, 0) + origin);
            _verts.Add(_charScale * new Vector3(info.maxX, info.maxY, 0) + origin);
            _verts.Add(_charScale * new Vector3(info.maxX, info.minY, 0) + origin);
            _verts.Add(_charScale * new Vector3(info.minX, info.minY, 0) + origin);

            _uvs.Add(info.uvTopLeft);
            _uvs.Add(info.uvTopRight);
            _uvs.Add(info.uvBottomRight);
            _uvs.Add(info.uvBottomLeft);

            if (_useColor) {
              _colors.Append(4, _globalTint * graphic.color);
            }

            origin.x += info.advance * _charScale;
          }
          origin.y -= lineHeight;
        }

        for (int i = 0; i < _uvs.Count; i++) {
          Vector4 uv = _uvs[i];
          uv.w = index;
          _uvs[i] = uv;
        }

        mesh.SetVertices(_verts);
        mesh.SetTriangles(_tris, 0);
        mesh.SetUVs(0, _uvs);

        if (_useColor) {
          mesh.SetColors(_colors);
        }

        _verts.Clear();
        _uvs.Clear();
        _tris.Clear();
        _colors.Clear();
        _tempLines.Clear();
      }
    }

    private CharWidthCalculator _widthCalculator = new CharWidthCalculator();
    private class CharWidthCalculator {
      public Font font;
      public int scaledFontSize;
      public FontStyle fontStyle;
      public float charScale;

      public Func<char, float> func;

      public CharWidthCalculator() {
        func = funcMethod;
      }

      private float funcMethod(char c) {
        CharacterInfo info;
        font.GetCharacterInfo(c, out info, scaledFontSize, fontStyle);
        return info.advance * charScale;
      }
    }
  }
}
