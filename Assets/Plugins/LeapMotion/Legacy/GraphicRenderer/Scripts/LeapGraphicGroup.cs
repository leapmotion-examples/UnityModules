/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
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

  [Serializable]
  public partial class LeapGraphicGroup : ISerializationCallbackReceiver, ILeapInternalGraphicGroup {

    #region INSPECTOR FIELDS
    [SerializeField]
    private string _groupName = default(string);

    [SerializeField]
    private RenderingMethodReference _renderingMethod = new RenderingMethodReference();

    [SerializeField]
    private FeatureList _features = new FeatureList();
    #endregion

    #region PRIVATE VARIABLES
    [SerializeField, HideInInspector]
    private LeapGraphicRenderer _renderer;

    [SerializeField, HideInInspector]
    private List<LeapGraphic> _graphics = new List<LeapGraphic>();

    [SerializeField, HideInInspector]
    private List<SupportInfo> _supportInfo = new List<SupportInfo>();

    [SerializeField, HideInInspector]
    private bool _addRemoveSupported = default(bool);

    private HashSet<LeapGraphic> _toAttach = new HashSet<LeapGraphic>();
    private HashSet<LeapGraphic> _toDetach = new HashSet<LeapGraphic>();
    #endregion

    #region PUBLIC RUNTIME API

    public string name {
      get {
        return _groupName;
      }
    }

    /// <summary>
    /// Gets the renderer this group is attached to.
    /// </summary>
    public LeapGraphicRenderer renderer {
      get {
        return _renderer;
      }
    }

    /// <summary>
    /// Sets the renderer this group is attached to.
    /// </summary>
    LeapGraphicRenderer ILeapInternalGraphicGroup.renderer {
      set {
        _renderer = value;
      }
    }

    /// <summary>
    /// Gets the rendering method used for this group.  This can only be changed
    /// at edit time using either the inspector interface, or the editor method
    /// ChangeRenderingMethod.
    /// </summary>
    public LeapRenderingMethod renderingMethod {
      get {
        return _renderingMethod.Value;
      }
    }

    /// <summary>
    /// Returns the list of features attached to this group.  This can only be 
    /// changed at edit time using either the inspector interface, or the editor
    /// methods AddFeature and RemoveFeature.
    /// </summary>
    public IList<LeapGraphicFeatureBase> features {
      get {
        Assert.IsNotNull(_features, "The feature list of graphic group was null!");
        return _features;
      }
    }

    /// <summary>
    /// Returns the list of graphics attached to this group.  This getter returns
    /// a regular mutable list for simplicity and efficiency, but the user is 
    /// still not allowed to mutate this list in any way.
    /// </summary>
    public List<LeapGraphic> graphics {
      get {
        Assert.IsNotNull(_graphics, "The graphic list of graphic group was null!");
        return _graphics;
      }
    }

    /// <summary>
    /// Returns the total number of graphics that will be part of this group after
    /// the next update cycle.  Since attachments are delayed, this number can be
    /// larger than graphics.Count.
    /// </summary>
    public int toBeAttachedCount {
      get {
        return _graphics.Count + _toAttach.Count;
      }
    }

    /// <summary>
    /// Maps 1-to-1 with the feature list, where each element represents the
    /// support that feature currently has.
    /// </summary>
    public List<SupportInfo> supportInfo {
      get {
        Assert.IsNotNull(_supportInfo, "The support info list of graphic group was null!");
        Assert.AreEqual(_features.Count, _supportInfo.Count, "The support info list should have the same length as the feature list.");
        return _supportInfo;
      }
    }

    /// <summary>
    /// Returns whether or not add/remove operations are supported at runtime by
    /// this group.  If this returns false, TryAddGraphic and TryRemoveGraphic will
    /// always fail at runtime.
    /// </summary>
    public bool addRemoveSupported {
      get {
        return _addRemoveSupported;
      }
    }

    /// <summary>
    /// Tries to add the given graphic to this group.  This can safely be called
    /// during runtime or edit time.  This method can fail under the following 
    /// conditions:
    ///    - The graphic is already attached to this group.
    ///    - The graphic is already attached to a different group.
    ///    - It is runtime and add/remove is not supported by this group.
    ///    
    /// At runtime the actual attachment is delayed until LateUpdate for efficiency
    /// reasons.  Expect that even if this method returns true that the graphic will
    /// not actually be attached until the end of LateUpdate.
    /// </summary>
    public bool TryAddGraphic(LeapGraphic graphic) {
      Assert.IsNotNull(graphic);

      if (graphic.willbeAttached || (graphic.isAttachedToGroup && !graphic.willbeDetached)) {
        return false;
      }

      if (!addRemoveSupportedOrEditTime()) {
        return false;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RecordObject(graphic, "Added graphic to group");
      } else
#endif
      {
        if (_toAttach.Contains(graphic)) {
          return false;
        }
        if (_toDetach.Contains(graphic)) {
          graphic.CancelWillBeDetached();
          graphic.isRepresentationDirty = true;
          _toDetach.Remove(graphic);
          return true;
        }
      }

      if (_graphics.Contains(graphic)) {
        if (graphic.attachedGroup == null) {
          //detatch and re-add, it forgot it was attached!
          //This can easily happen at edit time due to prefab shenanigans 
          graphic.OnDetachedFromGroup();
          _graphics.Remove(graphic);
        } else {
          Debug.LogWarning("Could not add graphic because it was already a part of this group.");
          return false;
        }
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        int newIndex = _graphics.Count;
        _graphics.Add(graphic);

        LeapSpaceAnchor anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(graphic.transform);

        RebuildFeatureData();
        RebuildFeatureSupportInfo();

        graphic.OnAttachedToGroup(this, anchor);

        if (_renderer.space != null) {
          _renderer.space.RebuildHierarchy();
          _renderer.space.RecalculateTransformers();
        }

        _renderer.editor.ScheduleRebuild();
      } else
#endif
      {
        if (_toAttach.Contains(graphic)) {
          return false;
        }

        graphic.NotifyWillBeAttached(this);
        _toAttach.Add(graphic);
      }

      return true;
    }

    public void RefreshGraphicAnchors() {
      foreach (var graphic in _graphics) {
        var anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(graphic.transform);
        graphic.OnUpdateAnchor(anchor);
      }
    }

    /// <summary>
    /// Tries to remove the given graphic from this group.  This can safely be called
    /// during runtime or edit time.  This method can fail under the following 
    /// conditions:
    ///    - The graphic is not attached to this group.
    ///    - It is runtime and add/remove is not supported by this group.
    ///    
    /// At runtime the actual detachment is delayed until LateUpdate for efficiency
    /// reasons.  Expect that even if this method returns true that the graphic will
    /// not actually be detached until the end of LateUpdate.
    /// </summary>
    public bool TryRemoveGraphic(LeapGraphic graphic) {
      Assert.IsNotNull(graphic);

      if (!addRemoveSupportedOrEditTime()) {
        return false;
      }

#if UNITY_EDITOR
      if (Application.isPlaying)
#endif
      {
        if (_toDetach.Contains(graphic)) {
          return false;
        }
        if (_toAttach.Contains(graphic)) {
          graphic.CancelWillBeAttached();
          graphic.isRepresentationDirty = true;
          _toAttach.Remove(graphic);
          return true;
        }
      }

      int graphicIndex = _graphics.IndexOf(graphic);
      if (graphicIndex < 0) {
        return false;
      }

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RecordObject(graphic, "Removed graphic from group");
        Undo.RecordObject(_renderer, "Removed graphic from group");

        graphic.OnDetachedFromGroup();
        _graphics.RemoveAt(graphicIndex);

        RebuildFeatureData();
        RebuildFeatureSupportInfo();

        if (_renderer.space != null) {
          _renderer.space.RebuildHierarchy();
          _renderer.space.RecalculateTransformers();
        }

        _renderer.editor.ScheduleRebuild();
      } else
#endif
      {
        if (_toDetach.Contains(graphic)) {
          return false;
        }

        graphic.NotifyWillBeDetached(this);
        _toDetach.Add(graphic);
      }

      return true;
    }

    /// <summary>
    /// Fills the argument list with all of the currently supported features 
    /// of type T.  Returns true if there are any supported features, and 
    /// returns false if there are no supported features.
    /// </summary>
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

      _renderingMethod.Value.OnUpdateRenderer();

      foreach (var feature in _features) {
        feature.isDirty = false;
      }
    }

    public void RebuildFeatureData() {
      using (new ProfilerSample("Rebuild Feature Data")) {
        foreach (var feature in _features) {
          feature.ClearDataObjectReferences();
        }

        for (int i = 0; i < _graphics.Count; i++) {
          var graphic = _graphics[i];
#if UNITY_EDITOR
          EditorUtility.SetDirty(graphic);
          Undo.RecordObject(graphic, "modified feature data on graphic.");
#endif

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
            if (_renderingMethod.Value == null) continue;

            var interfaceType = typeof(ISupportsFeature<>).MakeGenericType(featureType);
            if (!interfaceType.IsAssignableFrom(_renderingMethod.Value.GetType())) {
              infoList.FillEach(() => SupportInfo.Error("This renderer does not support this feature."));
              continue;
            }

            var supportDelegate = interfaceType.GetMethod("GetSupportInfo");

            if (supportDelegate == null) {
              Debug.LogError("Could not find support delegate.");
              continue;
            }

            supportDelegate.Invoke(_renderingMethod.Value, new object[] { castList, infoList });
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

    #region LIFECYCLE CALLBACKS

    /// <summary>
    /// Specifically called during the OnEnable callback during RUNTIME ONLY
    /// </summary>
    public void OnEnable() {
      for (int i = 0; i < _features.Count; i++) {
        _features[i].AssignFeatureReferences();
        _features[i].ClearDataObjectReferences();
        _features[i].isDirty = true;
        foreach (var graphic in _graphics) {
          _features[i].AddFeatureData(graphic.featureData[i]);
        }
      }

      _renderingMethod.Value.OnEnableRenderer();
    }

    /// <summary>
    /// Specifically called during the OnDisable callback during RUNTIME ONLY
    /// </summary>
    public void OnDisable() {
      _renderingMethod.Value.OnDisableRenderer();
    }

    #endregion

    #region PRIVATE IMPLEMENTATION

#if UNITY_EDITOR
    public LeapGraphicGroup() {
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

          int newGraphicStart = _graphics.Count;

          //Then we append all the new graphics if there are any left.  This
          //only happens if more graphics were added than were remove this
          //frame.
          while (canAttach) {
            _graphics.Add(attachEnum.Current);
            canAttach = attachEnum.MoveNext();
          }

          //We remove any graphics that did not have a matching add.  This 
          //only happens if more graphics were removed than were added this
          //frame.
          while (canDetach) {
            int toDetachIndex = _graphics.IndexOf(detachEnum.Current);
            dirtyIndexes.Add(toDetachIndex);

            _graphics[_graphics.Count - 1].isRepresentationDirty = true;
            _graphics.RemoveAtUnordered(toDetachIndex);

            detachEnum.Current.OnDetachedFromGroup();

            canDetach = detachEnum.MoveNext();
          }

          //TODO: this is gonna need to be optimized
          //Make sure to call this before OnAttachedToGroup or else the graphic
          //will not have the correct feature data when it gets attached!
          RebuildFeatureData();
          RebuildFeatureSupportInfo();

          //newGraphicStart is either less than _graphics.Count because we added 
          //new graphics, or it is greater than _graphics.Count because we removed
          //some graphics.
          for (int i = newGraphicStart; i < _graphics.Count; i++) {
            var anchor = _renderer.space == null ? null : LeapSpaceAnchor.GetAnchor(attachEnum.Current.transform);
            _graphics[i].OnAttachedToGroup(this, anchor);
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

          if (renderer.space != null) {
            renderer.space.RebuildHierarchy();
            renderer.space.RecalculateTransformers();
          }

          foreach (var feature in _features) {
            feature.isDirty = true;
          }

          (_renderingMethod.Value as ISupportsAddRemove).OnAddRemoveGraphics(dirtyIndexes);
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

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() {
      if (_renderingMethod.Value == null || renderer == null) {
        Debug.LogWarning("The rendering group did not find the needed data!  If you have a variable of type " +
                         "LeapGraphicGroup make sure to annotate it with a [NonSerialized] attribute, or else " +
                         "Unity will automatically create invalid instances of the class.");
      } else {
        ILeapInternalRenderingMethod renderingMethodInternal = _renderingMethod.Value;
        renderingMethodInternal.group = this;
        renderingMethodInternal.renderer = renderer;
      }
    }

    [Serializable]
    public class FeatureList : MultiTypedList<LeapGraphicFeatureBase, LeapTextureFeature,
                                                                      LeapSpriteFeature,
                                                                      LeapRuntimeTintFeature,
                                                                      LeapBlendShapeFeature,
                                                                      CustomFloatChannelFeature,
                                                                      CustomVectorChannelFeature,
                                                                      CustomColorChannelFeature,
                                                                      CustomMatrixChannelFeature> { }

    [Serializable]
    public class RenderingMethodReference : MultiTypedReference<LeapRenderingMethod, LeapBakedRenderer,
                                                                                     LeapDynamicRenderer,
                                                                                     LeapTextRenderer> { }
    #endregion
  }
}
