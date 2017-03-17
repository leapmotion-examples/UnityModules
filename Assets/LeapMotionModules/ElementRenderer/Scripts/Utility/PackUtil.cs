﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public static class PackUtil {

  [Serializable]
  public class Settings {
    [EditTimeOnly]
    [MinValue(0)]
    public int border = 1;

    [EditTimeOnly]
    [MinValue(0)]
    public int padding = 0;

    [EditTimeOnly]
    [MinValue(16)]
    [MaxValue(8192)]
    public int maxAtlasSize = 4096;

    [EditTimeOnly]
    public FilterMode filterMode = FilterMode.Bilinear;
  }

  public static void DoPack(List<LeapGuiTextureFeature> textureFeatures,
                            Settings settings,
                        out Texture2D[] packedTextures,
                        out Rect[][] channelMapping) {
    packedTextures = new Texture2D[textureFeatures.Count];
    channelMapping = new Rect[4][];

    Texture2D[] textureArray = new Texture2D[textureFeatures[0].data.Count];

    foreach (var channel in MeshUtil.allUvChannels) {
      var mainTexture = textureFeatures.Query().FirstOrDefault(f => f.channel == channel);
      if (mainTexture == null) continue;

      Texture2D defaultTexture, packedTexture;
      prepareForPacking(mainTexture, settings, textureArray, out defaultTexture, out packedTexture);

      var packedRects = packedTexture.PackTextures(textureArray,
                                                   padding: settings.padding,
                                                   maximumAtlasSize: settings.maxAtlasSize,
                                                   makeNoLongerReadable: true);

      //Correct uvs to account for the added border
      for (int i = 0; i < packedRects.Length; i++) {
        float dx = 1.0f / packedTexture.width;
        float dy = 1.0f / packedTexture.height;
        Rect r = packedRects[i];

        if (textureArray[i] != defaultTexture) {
          dx *= settings.border;
          dy *= settings.border;
        }

        r.x += dx;
        r.y += dy;
        r.width -= dx * 2;
        r.height -= dy * 2;
        packedRects[i] = r;
      }

      packedTextures[textureFeatures.IndexOf(mainTexture)] = packedTexture;
      channelMapping[channel.Index()] = packedRects;

      //All texture features that are NOT the main texture do not get their own atlas step
      //They are simply copied into a new texture
      //var nonMainTextures = textureFeatures.Query().Where(f => f.channel == channel).Skip(1).ToList();
      //TODO: much lower priority ;)
    }

    //Clear cache
    foreach (var texture in _cachedProcessedTextures.Values) {
      UnityEngine.Object.DestroyImmediate(texture);
    }
    _cachedProcessedTextures.Clear();
  }

  private static void prepareForPacking(LeapGuiTextureFeature feature,
                                        Settings settings,
                                        Texture2D[] textureArray,
                                    out Texture2D defaultTexture,
                                    out Texture2D packedTexture) {
    feature.data.Query().Select(dataObj => processTexture(dataObj.texture, settings.border)).FillArray(textureArray);

    var nonNullTexture = feature.data.Query().Select(d => d.texture).FirstOrDefault(t => t != null);
    TextureFormat format;
    if (nonNullTexture == null) { //welp
      format = TextureFormat.ARGB32;
    } else {
      format = nonNullTexture.format;
    }

    defaultTexture = getDefaultTexture(format);
    for (int i = 0; i < textureArray.Length; i++) {
      if (textureArray[i] == null) {
        textureArray[i] = defaultTexture;
      }
    }

    packedTexture = new Texture2D(1, 1, format, mipmap: false);
    packedTexture.filterMode = settings.filterMode;
  }

  private static Dictionary<ProcessKey, Texture2D> _cachedProcessedTextures = new Dictionary<ProcessKey, Texture2D>();
  private static Texture2D processTexture(Texture2D source, int border) {
    if (source == null) {
      return null;
    }

    Texture2D bordered;
    ProcessKey key = new ProcessKey() { texture = source, border = border };
    if (!_cachedProcessedTextures.TryGetValue(key, out bordered)) {

      if (source.IsReadable()) {
        bordered = source.GetBordered(border);
      } else {
        var tempReadable = source.GetReadableTexture();
        bordered = tempReadable.GetBordered(border);
        UnityEngine.Object.DestroyImmediate(tempReadable);
      }

      _cachedProcessedTextures[key] = bordered;
    }
    return bordered;
  }

  private static Dictionary<TextureFormat, Texture2D> _cachedDefaultTextures = new Dictionary<TextureFormat, Texture2D>();
  private static Texture2D getDefaultTexture(TextureFormat format) {
    Texture2D texture;
    if (!_cachedDefaultTextures.TryGetValue(format, out texture)) {
      texture = new Texture2D(3, 3, TextureFormat.ARGB32, mipmap: false);
      texture.SetPixels(new Color[3 * 3].Fill(Color.white));
      _cachedDefaultTextures[format] = texture;
    }
    return texture;
  }

  private struct ProcessKey {
    public Texture2D texture;
    public int border;
  }
}
