﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [AddComponentMenu("")]
  public partial class LeapGraphicGroup : LeapGraphicComponentBase<LeapGraphicRenderer> {

    #region INSPECTOR FIELDS
    [SerializeField]
    private LeapRenderingMethod _renderingMethod;

    [SerializeField]
    private List<LeapGraphicFeatureBase> _features = new List<LeapGraphicFeatureBase>();
    #endregion

    #region PRIVATE VARIABLES
    [SerializeField, HideInInspector]
    private LeapGraphicRenderer _renderer;

    [SerializeField, HideInInspector]
    private List<LeapGraphic> _graphics = new List<LeapGraphic>();

    [SerializeField, HideInInspector]
    private List<SupportInfo> _supportInfo = new List<SupportInfo>();

    [SerializeField, HideInInspector]
    private bool _addRemoveSupported;

    private HashSet<LeapGraphic> _toAttach = new HashSet<LeapGraphic>();
    private HashSet<LeapGraphic> _toDetach = new HashSet<LeapGraphic>();
    #endregion

    #region PUBLIC RUNTIME API

    public
#if UNITY_EDITOR
  new
#endif
  LeapGraphicRenderer renderer {
      get {
#if UNITY_EDITOR
        if (_renderer == null) {
          _renderer = GetComponent<LeapGraphicRenderer>();

          if (_renderer == null) {
            Debug.LogError("The graphic group still exists but isn't connected to any renderer!", this);
          } else {
            Debug.LogWarning("The _renderer field of the graphic group became null!", this);
          }
        }
#endif

        return _renderer;
      }
    }

    public LeapRenderingMethod renderingMethod {
      get {
#if UNITY_EDITOR
        if (_renderingMethod == null) {
          _renderingMethod = GetComponent<LeapRenderingMethod>();

          if (_renderingMethod == null) {
            Debug.LogError("The graphic group still exists but isn't connected to any rendering method!", this);
          } else {
            Debug.LogWarning("The _renderingMethod field of the graphic group became null!", this);
          }
        }
#endif

        return _renderingMethod;
      }
    }

    public List<LeapGraphicFeatureBase> features {
      get {
        Assert.IsNotNull(_features, "The feature list of graphic group was null!");
        return _features;
      }
    }

    public List<LeapGraphic> graphics {
      get {
        Assert.IsNotNull(_graphics, "The graphic list of graphic group was null!");
        return _graphics;
      }
    }

    /// <summary>
    /// Maps 1-to-1 with the feature list, where each graphic represents the
    /// support that feature currently has.
    /// </summary>
    public List<SupportInfo> supportInfo {
      get {
        Assert.IsNotNull(_supportInfo, "The support info list of graphic group was null!");
        Assert.AreEqual(_features.Count, _supportInfo.Count, "The support info list should have the same length as the feature list.");
        return _supportInfo;
      }
    }

    public bool addRemoveSupported {
      get {
        return _addRemoveSupported;
      }
    }

    public bool TryAddGraphic(LeapGraphic graphic) {
      Assert.IsNotNull(graphic);

      if (!addRemoveSupportedOrEditTime()) {
        return false;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RecordObject(graphic, "Added graphic to group");
        Undo.RecordObject(this, "Added graphic to group");
      }
#endif

      if (_graphics.Contains(graphic)) {
        if (graphic.attachedGroup == null) {
          //detatch and re-add, it forgot it was attached!
          //This can easily happen at edit time due to prefab shenanigans 
          graphic.OnDetachedFromGroup();
        } else {
          return false;
        }
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        int newIndex = _graphics.Count;
        _graphics.Add(graphic);

        LeapSpaceAnchor anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(graphic.transform);

        graphic.OnAttachedToGroup(this, anchor);

        RebuildFeatureData();
        RebuildFeatureSupportInfo();

        _renderer.editor.ScheduleEditorUpdate();
      } else
#endif
      {
        if (_toAttach.Contains(graphic)) {
          return false;
        }

        _toAttach.Add(graphic);
      }

      return true;
    }

    public bool TryRemoveGraphic(LeapGraphic graphic) {
      Assert.IsNotNull(graphic);

      if (!addRemoveSupportedOrEditTime()) {
        return false;
      }

      int graphicIndex = _graphics.IndexOf(graphic);
      if (graphicIndex < 0) {
        return false;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RecordObject(graphic, "Removed graphic from group");
        Undo.RecordObject(this, "Removed graphic from group");

        graphic.OnDetachedFromGroup();
        _graphics.RemoveAt(graphicIndex);

        //TODO: this is gonna need to be optimized
        RebuildFeatureData();
        RebuildFeatureSupportInfo();

        _renderer.editor.ScheduleEditorUpdate();
      } else
#endif
      {
        if (_toDetach.Contains(graphic)) {
          return false;
        }

        _toDetach.Add(graphic);
      }

      return true;
    }

    public bool GetSupportedFeatures<T>(List<T> features) where T : LeapGraphicFeatureBase {
      features.Clear();
      for (int i = 0; i < _features.Count; i++) {
        var feature = _features[i];
        if (!(feature is T)) continue;
        if (_supportInfo[i].support == SupportType.Error) continue;

        features.Add(feature as T);
      }

      return features.Count != 0;
    }

    public void UpdateRenderer() {
#if UNITY_EDITOR
      if (Application.isPlaying)
#endif
      {
        handleRuntimeAddRemove();
      }

      _renderingMethod.OnUpdateRenderer();

      foreach (var feature in _features) {
        feature.isDirty = false;
      }
    }

    public void RebuildFeatureData() {
      using (new ProfilerSample("Rebuild Feature Data")) {
        foreach (var feature in _features) {
          feature.ClearDataObjectReferences();
          feature.isDirty = true;
        }

        for (int i = 0; i < _graphics.Count; i++) {
          var graphic = _graphics[i];

          List<LeapFeatureData> dataList = new List<LeapFeatureData>();
          foreach (var feature in _features) {
            var dataObj = graphic.featureData.Query().OfType(feature.GetDataObjectType()).FirstOrDefault();
            if (dataObj != null) {
              graphic.featureData.Remove(dataObj);
            } else {
              dataObj = feature.CreateFeatureDataForGraphic(graphic);
            }
            feature.AddFeatureData(dataObj);
            dataList.Add(dataObj);
          }

          foreach (var dataObj in graphic.featureData) {
            DestroyImmediate(dataObj);
          }

          graphic.OnAssignFeatureData(dataList);
        }

        //Could be more efficient
        foreach (var feature in _features) {
          feature.AssignFeatureReferences();
        }
      }
    }

    public void RebuildFeatureSupportInfo() {
      using (new ProfilerSample("Rebuild Support Info")) {
        var typeToFeatures = new Dictionary<Type, List<LeapGraphicFeatureBase>>();
        foreach (var feature in _features) {
          Type featureType = feature.GetType();
          List<LeapGraphicFeatureBase> list;
          if (!typeToFeatures.TryGetValue(featureType, out list)) {
            list = new List<LeapGraphicFeatureBase>();
            typeToFeatures[featureType] = list;
          }

          list.Add(feature);
        }


        var featureToInfo = new Dictionary<LeapGraphicFeatureBase, SupportInfo>();

        foreach (var pair in typeToFeatures) {
          var featureType = pair.Key;
          var featureList = pair.Value;
          var infoList = new List<SupportInfo>().FillEach(featureList.Count, () => SupportInfo.FullSupport());

          var castList = Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType)) as IList;
          foreach (var feature in featureList) {
            castList.Add(feature);
          }

          try {
            if (_renderingMethod == null) continue;

            var interfaceType = typeof(ISupportsFeature<>).MakeGenericType(featureType);
            if (!interfaceType.IsAssignableFrom(_renderingMethod.GetType())) {
              infoList.FillEach(() => SupportInfo.Error("This renderer does not support this feature."));
              continue;
            }

            var supportDelegate = interfaceType.GetMethod("GetSupportInfo");

            if (supportDelegate == null) {
              Debug.LogError("Could not find support delegate.");
              continue;
            }

            supportDelegate.Invoke(_renderingMethod, new object[] { castList, infoList });
          } finally {
            for (int i = 0; i < featureList.Count; i++) {
              featureToInfo[featureList[i]] = infoList[i];
            }
          }
        }

        _supportInfo = new List<SupportInfo>();
        foreach (var feature in _features) {
          _supportInfo.Add(feature.GetSupportInfo(this).OrWorse(featureToInfo[feature]));
        }
      }
    }
    #endregion

    #region UNITY CALLBACKS

    protected override void OnValidate() {
      base.OnValidate();

      for (int i = _features.Count; i-- != 0;) {
        if (_features[i] == null) {
          _features.RemoveAt(i);
        }
      }

#if UNITY_EDITOR
      if (!InternalUtility.IsPrefab(this)) {
        editor.OnValidate();
      }
#endif
    }

#if UNITY_EDITOR
    protected override void OnDestroyedByUser() {
      editor.OnDestroyedByUser();
    }
#endif

    private void OnEnable() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return;
      }
#endif

      _renderingMethod.OnEnableRenderer();
    }

    private void OnDisable() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return;
      }
#endif

      _renderingMethod.OnDisableRenderer();
    }

    #endregion

    #region PRIVATE IMPLEMENTATION

#if UNITY_EDITOR
    private LeapGraphicGroup() {
      editor = new EditorApi(this);
    }
#endif

    private void handleRuntimeAddRemove() {
      if (_toAttach.Count == 0 && _toDetach.Count == 0) {
        return;
      }

      using (new ProfilerSample("Handle Runtime Add/Remove")) {
        List<int> dirtyIndexes = Pool<List<int>>.Spawn();

        try {
          var attachEnum = _toAttach.GetEnumerator();
          var detachEnum = _toDetach.GetEnumerator();
          bool canAttach = attachEnum.MoveNext();
          bool canDetach = detachEnum.MoveNext();

          //First, we can handle pairs of adds/removes easily by simply placing
          //the new graphic in the same place the old graphic was.
          while (canAttach && canDetach) {
            int toDetatchIndex = _graphics.IndexOf(detachEnum.Current);
            _graphics[toDetatchIndex] = attachEnum.Current;

            var anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(attachEnum.Current.transform);

            detachEnum.Current.OnDetachedFromGroup();
            attachEnum.Current.OnAttachedToGroup(this, anchor);

            dirtyIndexes.Add(toDetatchIndex);

            canAttach = attachEnum.MoveNext();
            canDetach = detachEnum.MoveNext();
          }

          //Then we append all the new graphics if there are any left.  This
          //only happens if more graphics were added than were remove this
          //frame.
          while (canAttach) {
            _graphics.Add(attachEnum.Current);

            var anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(attachEnum.Current.transform);
            attachEnum.Current.OnAttachedToGroup(this, anchor);

            canAttach = attachEnum.MoveNext();
          }

          //Or remove any graphics that did not have a matching add.  This 
          //only happens if more graphics were removed than were added this
          //frame.
          while (canDetach) {
            int toDetachIndex = _graphics.IndexOf(detachEnum.Current);
            dirtyIndexes.Add(toDetachIndex);

            _graphics.RemoveAtUnordered(toDetachIndex);

            detachEnum.Current.OnDetachedFromGroup();

            canDetach = detachEnum.MoveNext();
          }

          attachEnum.Dispose();
          detachEnum.Dispose();
          _toAttach.Clear();
          _toDetach.Clear();

          //Make sure the dirty indexes only point to valid graphics areas.
          //Could potentially be optimized, but hasnt been a bottleneck.
          for (int i = dirtyIndexes.Count; i-- != 0;) {
            if (dirtyIndexes[i] >= _graphics.Count) {
              dirtyIndexes.RemoveAt(i);
            }
          }

          //TODO: this is gonna need to be optimized
          RebuildFeatureData();
          RebuildFeatureSupportInfo();
          if (renderer.space != null) {
            renderer.space.RebuildHierarchy();
            renderer.space.RecalculateTransformers();
          }

          (_renderingMethod as ISupportsAddRemove).OnAddRemoveGraphics(dirtyIndexes);
        } finally {
          dirtyIndexes.Clear();
          Pool<List<int>>.Recycle(dirtyIndexes);
        }
      }
    }

    private bool addRemoveSupportedOrEditTime() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return true;
      }
#endif

      return _addRemoveSupported;
    }
    #endregion
  }
}
