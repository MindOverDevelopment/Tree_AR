﻿using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Pipe {
	/// <summary>
	/// Container and manager for branch descriptors.
	/// </summary>
	[System.Serializable]
	public class BranchDescriptorCollection : ISerializationCallbackReceiver {
		#region Subclasses
		[System.Serializable]
		public class SproutStyle {
			[FormerlySerializedAs("colorSaturation")]
			public float minColorSaturation = 1f;
			[FormerlySerializedAs("colorSaturation")]
			public float maxColorSaturation = 1f;
			public enum SproutSaturationMode {
				Uniform = 0,
				Hierarchy = 1,
				Branch = 2
			}
			public SproutSaturationMode sproutSaturationMode = SproutSaturationMode.Uniform;
			public bool invertSproutSaturationMode = false;
			public float sproutSaturationVariance = 0f;
			public float minColorShade = 0.75f;
			public float maxColorShade = 1f;
			public float minColorTint = 0f;
			public float maxColorTint = 0.2f;
			public Color colorTint = Color.white;
			public enum SproutTintMode {
				Uniform = 0,
				Hierarchy = 1,
				Branch = 2
			}
			public SproutTintMode sproutTintMode = SproutTintMode.Uniform;
			public bool invertSproutTintMode = false;
			public float sproutTintVariance = 0f;
			public float metallic = 0f;
			public float glossiness = 0f;
			[FormerlySerializedAs("subsurfaceMul")]
			public float subsurface = 1f;
			public List<float> sproutMapAlphas = new List<float> ();
			public List<SproutMap.SproutMapArea> sproutMapAreas = new List<SproutMap.SproutMapArea> ();
			public SproutStyle Clone () {
				SproutStyle clone = new SproutStyle ();
				clone.minColorSaturation = minColorSaturation;
				clone.maxColorSaturation = maxColorSaturation;
				clone.sproutSaturationMode = sproutSaturationMode;
				clone.invertSproutSaturationMode = invertSproutSaturationMode;
				clone.sproutSaturationVariance = sproutSaturationVariance;
				clone.minColorShade = minColorShade;
				clone.maxColorShade = maxColorShade;
				clone.minColorTint = minColorTint;
				clone.maxColorTint = maxColorTint;
				clone.colorTint = colorTint;
				clone.sproutTintMode = sproutTintMode;
				clone.invertSproutTintMode = invertSproutTintMode;
				clone.sproutTintVariance = sproutSaturationVariance;
				clone.metallic = metallic;
				clone.glossiness = glossiness;
				clone.subsurface = subsurface;
				for (int i = 0; i < sproutMapAlphas.Count; i++) {
					clone.sproutMapAlphas.Add (sproutMapAlphas [i]);
				}
				for (int i = 0; i < sproutMapAreas.Count; i++) {
					clone.sproutMapAreas.Add (sproutMapAreas [i].Clone ());
				}
				return clone;
			}
		}
		#endregion

		#region Constants
		public const int IMPL_NONE = -1;
		#endregion
		
		#region Vars
		/// <summary>
		/// Type of descriptor for this collection.
		/// </summary>
		[SerializeField]
		public int descriptorImplId = IMPL_NONE;
		/// <summary>
		/// Name for the type of descriptor.
		/// </summary>
		[SerializeField]
		public string descriptorImplName = "Undefined Descriptor";
		/// <summary>
		/// The snapshots.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("branchDescriptors")]
		public List<BranchDescriptor> snapshots = new List<BranchDescriptor> ();
		/// <summary>
		/// The variations.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("variationDescriptors")]
		public List<VariationDescriptor> variations = new List<VariationDescriptor> ();
		[SerializeField]
		public int snapshotIndex = -1;
		[SerializeField]
		public int lastSnapshotIndex = -1;
		[SerializeField]
		public int variationIndex = -1;
		[SerializeField]
		public int lastVariationIndex = -1;
		#endregion

		#region Constructor
		public BranchDescriptorCollection () {}
		#endregion

		#region Map Vars
        /// <summary>
        /// Main texture for branches.
        /// </summary>
        public Texture2D branchAlbedoTexture = null;
        /// <summary>
        /// Normal map texture for branches.
        /// </summary>
        public Texture2D branchNormalTexture = null;
        public float branchTextureYDisplacement = 0f;
		public SproutStyle sproutStyleA = new SproutStyle ();
		public SproutStyle sproutStyleB = new SproutStyle ();
		public SproutStyle sproutStyleCrown = new SproutStyle ();
		public float branchColorShade = 1f;
		public float branchColorSaturation = 1f;
		public bool showVariationInScene = false;
		public bool enableAO = false;
		public int samplesAO = 5;
		public float strengthAO = 0.5f;
		public bool enablePreviewAO = false;
        #endregion

		#region Canvas Settings Vars
		/// <summary>
		/// Background color to use on the preview canvas.
		/// </summary>
		public Color bgColor = new Color (0.28f, 0.38f, 0.47f);
		/// <summary>
		/// Size for the preview base plane.
		/// </summary>
		public float planeSize = 1f;
		/// <summary>
		/// Color tint for the preview base plane.
		/// </summary>
		public Color planeTint = Color.white;
		/// <summary>
		/// Display a ruler as reference for the mesh height.
		/// </summary>
		public bool showRuler = true;
		/// <summary>
		/// Color for the ruler.
		/// </summary>
		public Color rulerColor = Color.white;
		/// <summary>
		/// Color to use for the gizmos on the canvas view.
		/// </summary>
		public Color gizmosColor = Color.yellow;
		/// <summary>
		/// Line width for the gizmos.
		/// </summary>
		public float gizmosLineWidth = 0.5f;
		/// <summary>
		/// Size for the unit point gizmos.
		/// </summary>
		public float gizmosUnitSize = 0.3f;
		/// <summary>
		/// Flag to display the 3D world vectors.
		/// </summary>
		public bool showAxisGizmo = true;
		/// <summary>
		/// AXis gizmo size.
		/// </summary>
		public float axisGizmoSize = 0.15f;
		/// <summary>
		/// Outline selection width.
		/// </summary>
		public float gizmosOutlineWidth = 1f;
		/// <summary>
		/// Outline selection alpha.
		/// </summary>
		public float gizmosOutlineAlpha = 0.5f;
		#endregion

		#region Export Settings Vars
		/// <summary>
		/// Available texture export modes.
		/// </summary>
		public enum ExportMode {
			SelectedSnapshot,
			Atlas
		}
		/// <summary>
		/// Export mode selected.
		/// </summary>
		public ExportMode exportMode = ExportMode.Atlas;
		/// <summary>
		/// Path to save the textures relative to the data application path (without the Assets folder).
		/// </summary>
		public string exportPath = "";
		public string exportPrefix = "branch";
		public string exportPrefixVariation = "variation";
		public int exportTake = 0;
		/// <summary>
		/// Texture size.
		/// </summary>
		public enum TextureSize
		{
			_128px,
			_256px,
			_512px,
			_1024px,
			_2048px
		}
		public TextureSize exportTextureSize = TextureSize._1024px;
		public int exportAtlasPadding = 5;
		public bool exportAlbedoEnabled = true;
		public int exportTexturesFlags = 15;
		public Texture2D atlasAlbedoTexture = null;
		public Texture2D atlasNormalsTexture = null;
		public Texture2D atlasExtrasTexture = null;
		public Texture2D atlasSubsurfaceTexture = null;
		#endregion

		#region Events
		public delegate void OnSnapshotEvent (BranchDescriptor snapshot);
		public delegate void OnVariationEvent (VariationDescriptor variation);
		public OnSnapshotEvent onBeforeAddSnapshot;
		public OnSnapshotEvent onAddSnapshot;
		public OnSnapshotEvent onBeforeRemoveSnapshot;
		public OnSnapshotEvent onRemoveSnapshot;
		public OnVariationEvent onBeforeAddVariation;
		public OnVariationEvent onAddVariation;
		public OnVariationEvent onBeforeRemoveVariation;
		public OnVariationEvent onRemoveVariation;
		#endregion

		#region Management
		/// <summary>
		/// Adds a snapshot instance to this collection, assigning it an id.
		/// </summary>
		/// <param name="snapshot">Branch Descriptor instance.</param>
		public void AddSnapshot (BranchDescriptor snapshot) {
			snapshot.id = GetSnapshotId ();
			onBeforeAddSnapshot?.Invoke (snapshot);
			snapshots.Add (snapshot);
			BuildIdToSnapshot ();
			onAddSnapshot?.Invoke (snapshot);
		}
		/// <summary>
		/// Adds a variation instance to this collection, assigning it an id.
		/// </summary>
		/// <param name="variation">Variation Descriptor instance.</param>
		public void AddVariation (VariationDescriptor variation) {
			variation.id = GetVariationId ();
			onBeforeAddVariation?.Invoke (variation);
			variations.Add (variation);
			onAddVariation?.Invoke (variation);
		}
		/// <summary>
		/// Removes a snapshot from this collection.
		/// </summary>
		/// <param name="snapshotId">Branch descriptor id.</param>
		/// <returns><c>True</c> if the snapshot is removed.</returns>
		public bool RemoveSnapshot (int snapshotId) {
			for (int i = 0; i < snapshots.Count; i++) {
				if (snapshots [i].id == snapshotId) {
					onBeforeRemoveSnapshot?.Invoke (snapshots [i]);
					BranchDescriptor snapshotToRemove = snapshots [i];
					snapshots.RemoveAt (i);
					BuildIdToSnapshot ();
					onRemoveSnapshot?.Invoke (snapshotToRemove);
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Removes a variation from this collection.
		/// </summary>
		/// <param name="variationId">Variation descriptor id.</param>
		/// <returns><c>True</c> if the variation is removed.</returns>
		public bool RemoveVariation (int variationId) {
			for (int i = 0; i < variations.Count; i++) {
				if (variations [i].id == variationId) {
					onBeforeRemoveVariation?.Invoke (variations [i]);
					VariationDescriptor variationToRemove = variations [i];
					variations.RemoveAt (i);
					onRemoveVariation?.Invoke (variationToRemove);
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Removes a variation from this collection.
		/// </summary>
		/// <param name="variation">Variation descriptor instance.</param>
		/// <returns><c>True</c> if the variation is removed.</returns>
		public bool RemoveVariation (VariationDescriptor variation) {
			for (int i = 0; i < variations.Count; i++) {
				if (variations [i] == variation) {
					onBeforeRemoveVariation?.Invoke (variation);
					variations.RemoveAt (i);
					onRemoveVariation?.Invoke (variation);
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Gets the next id for the snapshots in this collection.
		/// </summary>
		/// <returns>Next id for snapshots.</returns>
		private int GetSnapshotId () {
			int _lastBranchDescriptorId = 0;
			for (int i = 0; i < snapshots.Count; i++) {
				if (snapshots [i].id >= _lastBranchDescriptorId) {
					_lastBranchDescriptorId = snapshots [i].id + 1;
				}
			}
			return _lastBranchDescriptorId++;
		}
		/// <summary>
		/// Gets the next id for the variations in this collection.
		/// </summary>
		/// <returns>Next id for variations.</returns>
		private int GetVariationId () {
			int _lastVariationId = 0;
			for (int i = 0; i < variations.Count; i++) {
				if (variations [i].id >= _lastVariationId) {
					_lastVariationId = variations [i].id + 1;
				}
			}
			return _lastVariationId++;
		}
		#endregion

		#region Query
		Dictionary<int, BranchDescriptor> _idToSnapshot = new Dictionary<int, BranchDescriptor> ();
		/// <summary>
		/// Build the snpashot id to snapshot instance dictionary.
		/// </summary>
		public void BuildIdToSnapshot () {
			_idToSnapshot.Clear ();
			for (int i = 0; i < snapshots.Count; i++) {
				if (!_idToSnapshot.ContainsKey (snapshots[i].id)) {
					_idToSnapshot.Add (snapshots [i].id, snapshots [i]);
				}
			}
		}
		/// <summary>
		/// Returns a BranchDescriptor instance (snapshot) by id.
		/// </summary>
		/// <param name="id">Id for the snapshot.</param>
		/// <returns>Snapshot instance.</returns>
		public BranchDescriptor GetSnapshot (int id) {
			if (_idToSnapshot.ContainsKey (id)) {
				return _idToSnapshot [id];
			}
			return null;
		}
		#endregion

		#region Serializable
        /// <summary>
        /// Before serialization method.
        /// </summary>
		public void OnBeforeSerialize() {}
        /// <summary>
        /// After serialization method.
        /// </summary>
		public void OnAfterDeserialize() {
			// Get the last id and set the id on snapshots with id = 0.
			bool hasUnassigned = false;
			int _lastId = 0;
			for (int i = 0; i < snapshots.Count; i++) {
				if (snapshots [i].id <= 0) hasUnassigned = true;
				else if (snapshots [i].id > _lastId) {
					_lastId = snapshots [i].id;
				}
			}
			// Assign missing ids on snapshots.
			if (hasUnassigned) {
				for (int i = 0; i < snapshots.Count; i++) {
					_lastId++;
					if (snapshots [i].id <= 0) snapshots [i].id = _lastId;
				}
			}

			// Build dictionary.
			BuildIdToSnapshot ();
		}
		#endregion

		#region Clone
		public BranchDescriptorCollection Clone () {
			BranchDescriptorCollection clone = new BranchDescriptorCollection ();
			clone.descriptorImplId = descriptorImplId;
			clone.descriptorImplName = descriptorImplName;
			
			for (int i = 0; i < snapshots.Count; i++) {
				clone.snapshots.Add (snapshots [i].Clone ());
			}
			clone.snapshotIndex = snapshotIndex;
			clone.lastSnapshotIndex = lastSnapshotIndex;

			for (int i = 0; i < variations.Count; i++) {
				clone.variations.Add (variations [i].Clone ());
			}
			clone.variationIndex = variationIndex;
			clone.lastVariationIndex = lastVariationIndex;

			clone.branchTextureYDisplacement = branchTextureYDisplacement;
            clone.branchAlbedoTexture = branchAlbedoTexture;
            clone.branchNormalTexture = branchNormalTexture;
			clone.branchColorShade = branchColorShade;
			clone.branchColorSaturation = branchColorSaturation;
			clone.showVariationInScene = showVariationInScene;
			clone.enableAO = enableAO;
			clone.samplesAO = samplesAO;
			clone.strengthAO = strengthAO;
			clone.enablePreviewAO = enablePreviewAO;

			clone.bgColor = bgColor;
			clone.planeSize = planeSize;
			clone.planeTint = planeTint;
			clone.showRuler = showRuler;
			clone.rulerColor = rulerColor;
			clone.gizmosColor = gizmosColor;
			clone.gizmosLineWidth = gizmosLineWidth;
			clone.gizmosUnitSize = gizmosUnitSize;
			clone.showAxisGizmo = showAxisGizmo;
			clone.axisGizmoSize = axisGizmoSize;
			clone.gizmosOutlineWidth = gizmosOutlineWidth;
			clone.gizmosOutlineAlpha = gizmosOutlineAlpha;

			clone.sproutStyleA = sproutStyleA.Clone ();
			clone.sproutStyleB = sproutStyleB.Clone ();
			clone.sproutStyleCrown = sproutStyleCrown.Clone ();

			clone.exportMode = exportMode;
			clone.exportPath = exportPath;
			clone.exportPrefix = exportPrefix;
			clone.exportPrefixVariation = exportPrefixVariation;
			clone.exportTake = exportTake;
			clone.exportTextureSize = exportTextureSize;
			clone.exportAtlasPadding = exportAtlasPadding;
			clone.exportTexturesFlags = exportTexturesFlags;
			clone.atlasAlbedoTexture = atlasAlbedoTexture;
			clone.atlasNormalsTexture = atlasNormalsTexture;
			clone.atlasExtrasTexture = atlasExtrasTexture;
			clone.atlasSubsurfaceTexture = atlasSubsurfaceTexture;
			return clone;
		}
		#endregion
	}
}