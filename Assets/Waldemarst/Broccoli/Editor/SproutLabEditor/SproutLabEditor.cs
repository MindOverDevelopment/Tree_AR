using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

using Broccoli.Base;
using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Utils;
using Broccoli.Factory;
using Broccoli.Manager;
using Broccoli.Catalog;
using Broccoli.TreeNodeEditor;

namespace Broccoli.BroccoEditor
{
	/// <summary>
	/// SproutLab instance.
	/// Main entry point: LoadBranchDescriptorCollection
	/// </summary>
	public class SproutLabEditor {
		#region Canvas Settings
		/// <summary>
		/// Settings for the mesh preview canvas.
		/// </summary>
		public class CanvasSettings {
			public int id = 0;
			public bool freeViewEnabled = true;
			public bool resetZoom = false;
			public float defaultZoomFactor = 2.5f;
			public float minZoomFactor = 0.5f;
			public float maxZoomFactor = 2.5f;
			public bool resetView = true;
			public Vector3 viewOffset = new Vector3 (-0.04f, 0.6f, -5.5f);
			public Vector2 viewDirection = new Vector2 (90f, 0f);
			public Quaternion viewTargetRotation = Quaternion.identity;
			public bool showPlane = false;
			public float planeSize = 0.7f;
			public bool showGizmos = true;
			public bool showRuler = true;
			public bool showViewModes = true;
		}
		#endregion

		#region Structure Settings
		/// <summary>
		/// Settings for the structure implementation.
		/// </summary>
		public class StructureSettings {
			public int id;
			public string branchEntityName = "Branch";
			public string branchEntitiesName = "Branches";
			public bool variantsEnabled = false;
			public bool displayExportDescriptor = true;
			public bool displayExportPrefab = false;
			public bool displayExportTextures = true;
			public bool hasCustomPanels = false;
		}
		#endregion

		#region Target Vars
		public BranchDescriptorCollection branchDescriptorCollection = null;
		public SproutSubfactory sproutSubfactory = null;
		public SnapshotSettings snapSettings = SnapshotSettings.Get();
		#endregion
		
		#region Var
		/// <summary>
		/// Mesh preview utility.
		/// </summary>
		public MeshPreview meshPreview;
		Color defaultPreviewBackgroundColor = new Color (0.35f, 0.35f, 0.35f, 1f);
		Color graySkyPreviewBackgroundColor = new Color (0.28f, 0.38f, 0.47f);
		Color normalPreviewBackgroundColor = new Color(0.5f, 0.5f, 1.0f, 1.0f);
		Color extrasPreviewBackgroundColor = new Color(0f, 0f, 0f, 1.0f);
		Color subsurfacePreviewBackgroundColor = new Color(0f, 0f, 0f, 1.0f);
		public Material[] currentPreviewMaterials = null;
		Material[] compositeMaterials = null;
		/*
		/// <summary>
		/// The area canvas.
		/// </summary>
		public SproutAreaCanvasEditor areaCanvas = new SproutAreaCanvasEditor ();
		*/
		/// <summary>
		/// Texture canvas component.
		/// </summary>
		TextureCanvas textureCanvas;
		/// <summary>
		/// Controls centering the texture on the TextureCanvas.
		/// </summary>
		bool _centerTextureCanvas = false;
		/// <summary>
		/// Controls resizing the TextureCanvas.
		/// </summary>
		bool _resizeTextureCanvas = false;
		/// <summary>
		/// Editor persistence utility.
		/// </summary>
		public EditorPersistence<BranchDescriptorCollectionSO> editorPersistence = null;
		/// <summary>
		/// Holds the canvas settings to display.
		/// </summary>
		private CanvasSettings currentCanvasSettings = null;
		/// <summary>
		/// Holds the structure configuration for the loaded structure implementation.
		/// </summary>
		private StructureSettings currentStructureSettings = null;
		/// <summary>
		/// Default canvas settings when none has been provided by an editor implementation.
		/// </summary>
		private CanvasSettings defaultCanvasSettings = new CanvasSettings ();
		/// <summary>
		/// Default structure settings when none has been provided by an editor implementation.
		/// </summary>
		private StructureSettings defaultStructureSettings = new StructureSettings ();
		public SproutMap.SproutMapArea selectedSproutMapArea = null; 
		int selectedSproutMapGroup = 0;
		int selectedSproutMapAreaIndex = 0;
		bool showViewControls = true;
		bool showLightControls = true;
		bool showLODOptions = false;
		private static Dictionary<int, ISproutLabEditorImpl> _implIdToImplementation = new Dictionary<int, ISproutLabEditorImpl> ();
		private static Dictionary<Type, ISproutLabEditorImpl> _editorImplementations = new Dictionary<Type, ISproutLabEditorImpl> ();
		private static List<ISproutLabEditorImpl> _orderedEditorImplementations = new List<ISproutLabEditorImpl> ();
		private static List<SproutCatalog> _catalogImplementations = new List<SproutCatalog>();
		public static List<SproutCatalog> catalogs {
			get { return _catalogImplementations; }
		}
		public List<int> filterCatalogImplIds = new List<int> ();
		private ISproutLabEditorImpl _currentImplementation = null;
		public ISproutLabEditorImpl currentImplementation {
			get { return _currentImplementation; }
		}
		public int firstLevelView = 0;
		public int secondLevelView = 0;
		public int thirdLevelView = 0;
		#endregion

		#region Debug Vars
		#if BROCCOLI_DEVEL
		private bool debugSkipSimplifyHull = false;
		private bool debugShowCurve = false;
		private bool debugShowTopoPoints = false;
		#endif
		private int debugPolyIndex = 0;
		private bool debugShowConvexHullPoints = false;
		private bool debugShowConvexHullPointsOrder = false;
		private bool debugShowConvexHull = false;
		private bool debugShowAABB = false;
		private bool debugShowOBB = false;
		private bool debugShowTris = false;
		private bool debugShowMeshNormals = false;
		private bool debugShowMeshTangents = false;
		private bool debugShow3DGizmo = false;
		private Vector3 debug3DGizmoOffset = Vector3.zero;
		#endregion

		#region Delegates and Events
		public delegate void BranchDescriptorEvent (BranchDescriptor branchDescriptor, BranchDescriptorCollection branchDescriptorCollection);
		public delegate void VariationDescriptorEvent (VariationDescriptor variationDescriptor, BranchDescriptorCollection branchDescriptorCollection);
		public delegate void ShowNotification (string notification);
		public delegate void WindowSizeChangeEvent (Rect oldSize, Rect newSize);
		public BranchDescriptorEvent onBeforeEditBranchDescriptor;
		public BranchDescriptorEvent onEditBranchDescriptor;
		public VariationDescriptorEvent onBeforeEditVariationDescriptor;
		public VariationDescriptorEvent onEditVariationDescriptor;
		public ShowNotification onShowNotification;
		public WindowSizeChangeEvent onResize;
		public void TriggerOnBeforeEditBranchDescriptor (BranchDescriptor branchDescriptor) { 
			branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
			onBeforeEditBranchDescriptor?.Invoke (branchDescriptor, branchDescriptorCollection);
		}
		public void TriggerOnEditBranchDescriptor (BranchDescriptor branchDescriptor) {
			onEditBranchDescriptor?.Invoke (branchDescriptor, branchDescriptorCollection);
		}
		public void TriggerOnBeforeEditVariationDescriptor (VariationDescriptor variationDescriptor) {
			branchDescriptorCollection.lastVariationIndex = branchDescriptorCollection.variationIndex;
			onBeforeEditVariationDescriptor?.Invoke (variationDescriptor, branchDescriptorCollection);
		}
		public void TriggerOnEditVariationDescriptor (VariationDescriptor variationDescriptor) { 
			onEditVariationDescriptor?.Invoke (variationDescriptor, branchDescriptorCollection);
		}
		#endregion

		#region GUI Vars
		/// <summary>
		/// Root VisualElement to add UIElements to.
		/// </summary>
		public VisualElement rootVisualElement = null;
		public SproutLabDebugPanel debugPanel = null;
		public SproutLabMappingPanel mappingPanel = null;
		public SproutLabSettingsPanel settingsPanel = null;
		int canvasSideButtonsIndex = 0;
		Rect currentRect;
		public EditorGUISplitView verticalSplitView;
		public Rect splitRect {
			get { return verticalSplitView.currentRect; }
		}
		public Rect windowRect {
			get { return verticalSplitView.availableRect; }
		}
		public enum ViewMode {
			SelectMode = 0,
			NotSupported = 1,
			Structure = 2,
			Templates = 3
		}
		private ViewMode _viewMode = ViewMode.Structure;
		public ViewMode viewMode {
			get { return _viewMode; }
		}
		public enum CanvasStructureView {
			Snapshot = 0,
			Variation = 1
		}
		private CanvasStructureView _canvasStructureView = CanvasStructureView.Snapshot;
		public CanvasStructureView canvasStructureView {
			get { return _canvasStructureView; }
		}
		/// <summary>
		/// Selected LOD view.
		/// </summary>
		public int selectedLODView = 0;
		/// <summary>
		/// Reorderable list to use on assigning sprout A textures.
		/// </summary>
		ReorderableList sproutAMapList;
		/// <summary>
		/// Reorderable list to use on assigning sprout B textures.
		/// </summary>
		ReorderableList sproutBMapList;
		/// <summary>
		/// Reorderable list to use on assigning sprout crowns.
		/// </summary>
		ReorderableList crownMapList;
		/// <summary>
		/// Popup to display view modes.
		/// </summary>
		PopupListComponent viewModePopup;
		/// <summary>
		/// Popup to display snapshot options.
		/// </summary>
		PopupListComponent snapshotOptionsPopup;
		/// <summary>
		/// Popup to display variation options.
		/// </summary>
		PopupListComponent variationOptionsPopup;
		/// <summary>
		/// Width for the left column on secondary panels.
		/// </summary>
		private int secondaryPanelColumnWidth = 120;
		/// <summary>
		/// Panel section selected.
		/// </summary>
		int _currentPanelSection = 0;
		public int currentPanelSection {
			get { return _currentPanelSection; }
		}
		/// <summary>
		/// Structure view selected.
		/// </summary>
		int currentStructureView = 0;
		/// <summary>
		/// Texture view selected.
		/// </summary>
		int currenTextureView = 0;
		/// <summary>
		/// Map view selected.
		/// </summary>
		public int currentMapView = 0;
		/// <summary>
		/// Export view selected.
		/// </summary>
		int currentExportView = 0;
		/// <summary>
		/// Saves the vertical scroll position for the structure view.
		/// </summary>
		private Vector2 structurePanelScroll;
		/// <summary>
		/// Saves the vertical scroll position for the texture view.
		/// </summary>
		private Vector2 texturePanelScroll;
		private Vector2 mappingPanelScroll;
		private Vector2 exportPanelScroll;
		private Vector2 debugPanelScroll;
		string[] levelOptions = new string[] {"Main Branch", "One Level", "Two Levels", "Three Levels"};
		bool branchGirthFoldout = false;
		bool branchNoiseFoldout = false;
		bool sproutSizeScaleFoldout = false;
		bool sproutAlignFoldout = false;
		bool sproutBendFoldout = false;
		bool branchMapFoldout = true;
		bool sproutAMapFoldout = true;
		bool sproutBMapFoldout = true;
		bool sproutCrownMapFoldout = true;
		public BranchDescriptor selectedSnapshot = null;
		public VariationDescriptor selectedVariation = null;
		BranchDescriptor.BranchLevelDescriptor selectedBranchLevelDescriptor;
		BranchDescriptor.SproutLevelDescriptor selectedSproutALevelDescriptor;
		BranchDescriptor.SproutLevelDescriptor selectedSproutBLevelDescriptor;
		BranchDescriptor.BranchLevelDescriptor proxyBranchLevelDescriptor = new BranchDescriptor.BranchLevelDescriptor ();
		BranchDescriptor.SproutLevelDescriptor proxySproutALevelDescriptor = new BranchDescriptor.SproutLevelDescriptor ();
		BranchDescriptor.SproutLevelDescriptor proxySproutBLevelDescriptor = new BranchDescriptor.SproutLevelDescriptor ();
		SproutMap.SproutMapArea proxySproutMap = new SproutMap.SproutMapArea ();
		bool sproutMapChanged = false;
		string viewModeDisplayStr = "Composite";
		Vector2 levelFrequency = Vector2.one;
		Vector2 levelRange = new Vector2 (0f, 1f);
		Vector2 levelLengthAtBase = Vector2.zero;
		Vector2 levelLengthAtTop = Vector2.zero;
		float levelSpacingVariance = 0f;
		public static int catalogItemSize = 125;
		Texture2D tmpTexture = null;
		int lightAngleStep = 1;
		float lightAngleStepValue = 45f;
		string lightAngleDisplayStr = "front";
		float lightAngleToAddTime = 0.75f;
		float lightAngleToAddTimeTmp = -1f;
		Vector3 lightAngleEulerFrom = new Vector3 (0,-90,0);
		Vector3 lightAngleEulerTo = new Vector3 (0,-90,0);
		bool viewTransitionEnabled = false;
		bool zoomTransitionEnabled = false;
		Vector2 cameraTransitionDirection = new Vector2 (90, 0);
		Vector3 cameraTransitionOffset = Vector3.zero;
		Quaternion cameraTransitionTargetRotation = Quaternion.identity;
		float cameraTransitionZoom = 2.5f;
		Vector2 cameraTransitionDirectionTmp;
		Vector3 cameraTransitionOffsetTmp;
		Quaternion cameraTransitionTargetRotationTmp;
		float cameraTransitionZoomTmp;
		float cameraTransitionTime = 0.333f;
		float cameraTransitionTimeTmp = 0f;
		static string[] exportTextureOptions = new string[] {"Albedo", "Normals", "Extras", "Subsurface", "Composite"};
		bool showProgressBar = false;
		float progressBarProgress = 0f;
		string progressBarTitle = "";
		Rect meshPreviewRect = Rect.zero;
		#endregion

		#region GUI Content & Labels
		/// <summary>
		/// Tab titles for panel sections.
		/// </summary>
		public static GUIContent[] panelSectionOption = new GUIContent[5];
		/// <summary>
		/// Structure views: branch or leaves.
		/// </summary>
		private static GUIContent[] structureViewOptions = new GUIContent[3];
		/// <summary>
		/// Structure levels.
		/// </summary>
		private GUIContent[] structureLevelOptions = new GUIContent[1];
		/// <summary>
		/// Preview options GUIContent array.
		/// </summary>
		private static GUIContent[] mapViewOptions = new GUIContent[5];
		/// <summary>
		/// Debug options GUIContent array.
		/// </summary>
		private static GUIContent[] debugViewOptions = new GUIContent[6];
		/// <summary>
		/// Debug polygon options GUIContent array.
		/// </summary>
		private static GUIContent[] debugPolygonOptions = new GUIContent[6];
		/// <summary>
		/// Displays the snapshots as a list of options.
		/// </summary>
		private static GUIContent[] snapshotViewOptions;
		/// <summary>
		/// Displays the variations as a list of options.
		/// </summary>
		private static GUIContent[] variationViewOptions;
		/// <summary>
		/// Displays the LOD views as a list of options.
		/// </summary>
		private static GUIContent[] lodViewOptions;
		private static Rect curveLimitsRect = new Rect (0f, 0f, 1f, 1f);
		private static GUIContent exportViewOptionDescriptorGUI = new GUIContent ("Export Descriptor", 
			"Displays the panel with options to export the descriptor, meshes and texture atlas for all the branches to be used on a BroccoTree.");
		private static GUIContent exportViewOptionTexturesGUI = new GUIContent ("Export Textures", 
			"Displays the panel with options to export textures only or create an atlas from the branches.");
		private static GUIContent exportViewOptionPrefabGUI = new GUIContent ("Export Prefab", 
			"Displays the panel with options to export the collection to a Prefab Asset or multiple Prefab Assets.");
		private static GUIContent exportDescriptorAndAtlasGUI = new GUIContent ("Export Descriptor with Atlas Texture", 
			"Saves the Structure Collection to an editable ScriptableObject and creates textures atlases to map its meshes.");
		private static GUIContent exportPrefabGUI = new GUIContent ("Export Prefab", 
			"Exports the collection to a Prefab Asset or multiple Prefab Assets.");
		private static GUIContent selectPathGUI = new GUIContent ("...", 
			"Select the path as destination for the saved files.");
		private static GUIContent exportTexturesGUI = new GUIContent ("Export Textures", 
			"Exports textures only or create an atlas from the branches.");
		private static GUIContent backToCreateProjectGUI = new GUIContent ("Back to Create Project", 
			"Navigates back to the Project Creation options.");
		private static GUIContent backToStructureViewGUI = new GUIContent ("Back to Structure View", 
			"Navigate back to the structure view to edit the working structure collection.");
		private static GUIContent generateNewStructureGUI = new GUIContent ("Generate New Structure", 
			"Generates a new structure using a new randomization seed.");
		private static GUIContent regenerateCurrentGUI = new GUIContent ("Regenerate Current", 
			"Regenerates the current structure using its spawning random seed.");
		private static GUIContent loadFromTemplateGUI = new GUIContent ("Load From Template", 
			"Show the template catalog view to select a structure template to beging working with.");
		private static GUIContent cloneSnapshotGUI = new GUIContent ("Clone Snapshot", 
			"Adds a Snapshot Structure to this Collection with the same values as the one selected.");
		private static GUIContent addSnapshotGUI = new GUIContent ("Add Snapshot",
			"Adds a Snapshot Structure to this Collection.");
		private static GUIContent removeSnapshotGUI = new GUIContent ("Remove", 
			"Removes the selected Snapshot Structure in this Collection.");
		private static GUIContent addVariationGUI = new GUIContent ("Add Variation", 
			"Adds a Variation Structure to this Collection.");
		private static GUIContent removeVariationGUI = new GUIContent ("Remove",
			"Removes the selected Variation Structure in this Collection.");
		private static string labelNotSupportedProject = "Not Supported Sprout Lab Project";
		private static string labelCreateProject = "Create a Project";
		private static string labelStructures = "Structures";
		private static string labelBranches = "Branches";
		private static GUIContent labelSaturation = new GUIContent ("Saturation",
			"Set the color saturation for the sprouts."); 
		private static GUIContent labelShadeRange = new GUIContent ("Shade Range",
			"Range to apply dark shading (darkening) to sprouts.");
		private static GUIContent labelTintColor = new GUIContent ("Tint Color",
			"Tint to apply to sprouts.");
		private static GUIContent labelTintRange = new GUIContent ("Tint Range",
			"Range of values to randomly apply tint to sprouts.");
		private static GUIContent labelMetallic = new GUIContent ("Metallic",
			"PBR material metallic property.");
		private static GUIContent labelGlossiness = new GUIContent ("Glossiness",
			"PBR material glossiness (smooth) property.");
		private static GUIContent labelTransluscencyFactor = new GUIContent ("Transluscency Factor",
			"PBR material subsurface value; how much light is reflected and get through the sprouts.");
		private static string labelSproutA = "Sprout A";
		private static string labelSproutB = "Sprout B";
		private static string labelSproutCrown = "Sprout Crown";
		private static GUIContent labelActiveLevels = new GUIContent ("Active Levels", 
			"The number of structure levels on the hierarchy available for tunning.");
		private static string labelGirthFoldout = "Girth (Radius)";
		private static GUIContent labelGirthAtBase = new GUIContent ("Girth at Base", 
			"The girth of structures at the base of the hierarchy.");
		private static GUIContent labelGirthAtTop = new GUIContent ("Girth at Top", 
			"The girth of structures at the top of the hierarchy.");
		private static string labelNoiseFoldout = "Noise";
		private static GUIContent labelNoiseResolution = new GUIContent ("Noise Resolution", 
			"How fine Perlin noise applied to branches should be.");
		private static GUIContent labelNoiseAtBase = new GUIContent ("Noise at Base", 
			"The amount of noise at the base of the structure hierarchy.");
		private static GUIContent labelNoiseAtTop =  new GUIContent ("Noise at Top", 
			"The amount of noise at the top of the structure hierarchy.");
		private static GUIContent labelNoiseScaleAtBase = new GUIContent ("Noise Scale at Base", 
			"The scale of noise displacement at the base of the structure hierarchy.");
		private static GUIContent labelNoiseScaleAtTop = new GUIContent ("Noise Scale at Top", 
			"The scale of noise displacement at the top of the structure hierarchy.");
		private static string labelSproutASettings = "Sprout A Settings";
		private static string labelSproutBSettings = "Sprout B Settings";
		private static string labelSproutALevelSettings = "Sprout A Level Settings";
		private static string labelSproutBLevelSettings = "Sprout B Level Settings"; 
		private static string labelCrownSettings = "Crown Settings";
		private static GUIContent labelSize = new GUIContent ("Size", 
			"Size (in meters) applied to all elements in this group.");
		private static GUIContent labelScaleAtBase = new GUIContent ("Scale at Base", 
			"Scale applied to the size of elements at the base of the structure hierarchy.");
		private static GUIContent labelScaleAtTop = new GUIContent ("Scale at Top", 
			"Scale applied to the size of elements at the top of the structure hierarchy.");
		private static GUIContent labelSizeScale = new GUIContent ("Size & Scale", 
			"Parameters to specify the size and scaling of sprouts.");
		private static GUIContent labelAlign = new GUIContent ("Alignment", 
			"Parameters to control the direction of sprouts.");
		private static GUIContent labelBend = new GUIContent ("Bending", 
			"Parameters to control the bending of sprouts.");
		private static GUIContent labelScaleVariance = new GUIContent ("Scale Variance", 
			"How much randomness withing the scale parameters the scale will have.");
		private static GUIContent labelScaleMode = new GUIContent ("Scale Mode", 
			"How to calculate the scale based on the position of the structure relative to the its range, its parent branch or the whole structure hierarchy.");
		private static GUIContent labelPlaneAlignment = new GUIContent ("Plane Alignment", 
			"Sprout alignment to the camera default plane.");
		private static GUIContent labelAlignmentRandom = new GUIContent ("Align Randomness", 
			"Adds random alignment to sprouts.");
		private static GUIContent labelBendingAtTop = new GUIContent ("Bending at Top", 
			"Forward bending applied to the sprouts at the top of the structure hierarchy.");
		private static GUIContent labelBendingAtBase = new GUIContent ("Bending at Base", 
			"Forward bending applied to the sprouts at the bottom of the structure hierarchy.");
		private static GUIContent labelSideBendingAtTop = new GUIContent ("Side Bending at Top", 
			"Side bending applied to the sprouts at the top of the structure hierarchy.");
		private static GUIContent labelSideBendingAtBase = new GUIContent ("Side Bending at Base", 
			"Side bending applied to the sprouts at the bottom of the structure hierarchy.");
		private static GUIContent labelFrequency = new GUIContent ("Frequency", 
			"Number of elements to spawn from within the range values.");
		private static GUIContent labelRange = new GUIContent ("Spawn Range", 
			"Range in a parent branch to spawn children elements.");
		private static GUIContent labelSproutDistributionMode = new GUIContent ("Distribution Mode", 
			"Mode to align structures relative to their origin point.");
		private static GUIContent labelDistributionCurve = new GUIContent ("Position Distribution", 
			"Curve to control the distribution of spawn positions along the parent structure.");
		private static GUIContent labelRadius = new GUIContent ("Spawn Radius", 
			"Radius to spawn structures.");
		private static GUIContent labelLengthAtBase = new GUIContent ("Length at Base", 
			"Length of structures at the base of the structure hierarchy.");
		private static GUIContent labelLengthAtTop = new GUIContent ("Length at Top", 
			"Length of structures at the top of the structure hierarchy.");
		private static GUIContent labelLengthCurve = new GUIContent ("Length Distribution", 
			"Curve to control the distribution of length along the position of the parent structure.");
		private static GUIContent labelSpacingVariance = new GUIContent ("Spacing Variance", 
			"Adds random spacing between structures.");
		private static GUIContent labelParallelAlignAtBase = new GUIContent ("Parallel Align at Base", 
			"How much the structures align to the direction of their parent at the base of the structure hierarchy.");
		private static GUIContent labelParallelAlignAtTop = new GUIContent ("Parallel Align at Top", 
			"How much the structures align to the direction of their parent at the top of the structure hierarchy.");
		private static GUIContent labelGravityAlignAtBase = new GUIContent ("Gravity Align at Base", 
			"How much the structures align against the gravity direction at the base of the structure hierarchy.");
		private static GUIContent labelGravityAlignAtTop = new GUIContent ("Gravity Align at Top", 
			"How much the structures align against the gravity direction at the top of the structure hierarchy.");
		private static GUIContent labelBranchRange = new GUIContent ("Branch Range", 
			"Range in the parent structure for the children structures to be spawn.");
		private static GUIContent labelBranchTextures = new GUIContent ("Branch Textures",
			"Textures to apply to branch structures.");
		private static GUIContent labelSproutATextures = new GUIContent ("Sprout A Textures",
			"Textures to apply to sprout A structures.");
		private static GUIContent labelSproutBTextures = new GUIContent ("Sprout B Textures",
			"Textures to apply to sprout B structures.");
		private static GUIContent labelCrownTextures = new GUIContent ("Crown Textures",
			"Textures to apply to crown structures.");
		private static GUIContent labelYDisplacement = new GUIContent ("Y Displacement", 
			"Y displacement to apply to UV mapping.");
		private static string labelExportOptions = "Export Options";
		private static string labelImportOptions = "Import Options";
		private static string labelBranchDescExportSettings = "Branch Descriptor Export Settings";
		private static string labelAtlasTextureSettings = "Atlas Texture Settings";
		private static GUIContent labelAtlasSize = new GUIContent ("Atlas Size", 
			"Pixel size for the output atlas file.");
		private static GUIContent labelPadding = new GUIContent ("Padding", 
			"Distance in pixels between the elements contained in the atlas.");
		private static GUIContent labelTake = new GUIContent ("Take", 
			"Number to add to the file and folder names when exporting prefabs and textures.");
		private static GUIContent labelPrefix = new GUIContent ("Prefix", 
			"Prefix to use on the prefab filename and folder.");
		private static GUIContent labelPath = new GUIContent ("Path:", 
			"Destination path to save the files.");
		private static string labelTexturesFolder = "Textures Folder";
		private static string labelTextures = "Textures";
		private static string labelPrefabSettings = "Prefab Settings";
		private static string labelPrefabFileSettings = "Prefab File Settings";
		private static string labelPrefabTextureSettings = "Prefab Texture Settings";
		private static string labelTextureExportSettings = "Texture Export Settings";
		private static string labelOutputFile = "Output File";
		private static string labelExportMode = "Export Mode";
		private static GUIContent labelEnabled = new GUIContent ("Enabled", 
			"");
		private static GUIContent labelCrownEnabled = new GUIContent ("Enabled", 
			"Activate crown sprouts (like flowers) on this snapshot.");
		private static GUIContent labelCrownRange = new GUIContent ("Range", 
			"How much of the parent range should the crown sprouts occupy.");
		private static GUIContent labelCrownProbability = new GUIContent ("Probability", 
			"Probability of the crown elements (like flowers) to be spawn on the parent branch.");
		private static GUIContent labelCrownFrequency = new GUIContent ("Frequency", 
			"Number of crown elements to spawn.");
		private static GUIContent labelCrownDepth = new GUIContent ("Depth", 
			"Median depth for crown sprouts.");
		private static string labelMoveLeftSnapBtn = "Move to Left";
		private static string tooltipMoveLeftSnapBtn = "Moves the selected Snapshot to the left of the list.";
		private static string labelMoveRightSnapBtn = "Move to Right";
		private static string tooltipMoveRightSnapBtn = "Moves the selected Snapshot to the right of the list.";
		private static string labelRemoveSnapBtn = "Remove Selected";
		private static string tooltipRemoveSnapBtn = "Removes the selected Snapshot from the list.";
		private static string labelMoveLeftVarBtn = "Move to Left";
		private static string tooltipMoveLeftVarBtn = "Moves the selected Variation to the left of the list.";
		private static string labelMoveRightVarBtn = "Move to Right";
		private static string tooltipMoveRightVarBtn = "Moves the selected Variation to the right of the list.";
		private static string labelRemoveVarBtn = "Remove Selected";
		private static string tooltipRemoveVarBtn = "Removes the selected Variation from the list.";
		#endregion

		#region Constants
		public const int STRUCTURE_SNAPSHOT = 0;
		public const int STRUCTURE_VARIATION = 1;
		public const int PANEL_STRUCTURE = 0;
		public const int PANEL_TEXTURE = 1;
		public const int PANEL_MAPPING = 2;
		public const int PANEL_EXPORT = 3;
		public const int PANEL_SETTINGS = 4;
		public const int PANEL_DEBUG = 5;
		public const int VIEW_COMPOSITE = 0;
		public const int VIEW_ALBEDO = 1;
		public const int VIEW_NORMALS = 2;
		public const int VIEW_EXTRAS = 3;
		public const int VIEW_SUBSURFACE = 4; 
		private const int STRUCTURE_BRANCH = 0;
		private const int STRUCTURE_SPROUT_A = 1;
		private const int STRUCTURE_SPROUT_B = 2;
		private const int STRUCTURE_CROWN = 3;
		private const int TEXTURE_VIEW_TEXTURE = 0;
		private const int TEXTURE_VIEW_STRUCTURE = 1;
		private const int EXPORT_DESCRIPTOR = 0;
		private const int EXPORT_PREFAB = 1;
		private const int EXPORT_TEXTURES = 2;
		private const int DEBUG_GEOMETRY = 0;
		private const int DEBUG_CANVAS = 1;
		private const int DEBUG_MESHING = 2;
		private const int DEBUG_PROCESS = 3;
		private const int DEBUG_BUILDER = 4;
		private const int DEBUG_SNAPSHOT = 5;
		private static int SNAPSHOT_ADD_LIMIT = 10;
		private static int VARIATION_ADD_LIMIT = 15;
		#endregion

		#region Messages
		private static string MSG_EXPORT_DESCRIPTOR = "Exports a ScriptableObject with the Branch Collection and their atlas texture. The Descriptor File can be used on a Broccoli Tree Factory " +
			"to define branches (their meshes and textures).";
		private static string MSG_EXPORT_PREFAB = "Exports the current collection to a Prefab Asset.";
		private static string MSG_EXPORT_TEXTURE = "Exports texture files from a single Branch Snapshot or by creating a Texture Atlas for the Collection.";
		private static string MSG_DELETE_SPROUT_MAP_TITLE = "Remove Sprout Map";
		private static string MSG_DELETE_SPROUT_MAP_MESSAGE = "Do you really want to remove this sprout mapping?";
		private static string MSG_DELETE_SPROUT_MAP_OK = "Yes";
		private static string MSG_DELETE_SPROUT_MAP_CANCEL = "No";
		private static string MSG_DELETE_BRANCH_DESC_TITLE = "Remove Branch Descriptor";
		private static string MSG_DELETE_BRANCH_DESC_MESSAGE = "Do you really want to remove this branch descriptor snapshot?";
		private static string MSG_DELETE_BRANCH_DESC_OK = "Yes";
		private static string MSG_DELETE_BRANCH_DESC_CANCEL = "No";
		private static string MSG_DELETE_VARIATION_DESC_TITLE = "Remove Variation Descriptor";
		private static string MSG_DELETE_VARIATION_DESC_MESSAGE = "Do you really want to remove this variation descriptor?";
		private static string MSG_DELETE_VARIATION_DESC_OK = "Yes";
		private static string MSG_DELETE_VARIATION_DESC_CANCEL = "No";
		private static string MSG_LOAD_CATALOG_ITEM_TITLE = "Load Sprout Template";
		private static string MSG_LOAD_CATALOG_ITEM_MESSAGE = "Do you really want to load this sprout template? (Unsaved settings will be lost).";
		private static string MSG_LOAD_CATALOG_ITEM_OK = "Yes";
		private static string MSG_LOAD_CATALOG_ITEM_CANCEL = "No";
		private static string MSG_NO_SELECTION = "No Snapshot or Variation is selected.\nSelect one to display it on this Canvas.";
		private static string MSG_NO_TEXTURE_SELECTION = "No Texture is selected.\nSelect one to display it on this Canvas.";
		private static string MSG_EMPTY_SNAPSHOTS = "No Snapshots found; you need to have at least one Snapshot to export them to prefabs.";
		private static string MSG_EMPTY_VARIATIONS = "No Variations found; you need to have at least one Variation to export them to prefabs.";
		private static string MSG_NO_SNAPSHOT_PANEL = "No Snapshot has been selected on this Structure Collection. Select one to modify its settings.";
		private static string MSG_NOT_SUPPORTED = "The implementation of this Sprout Lab Collection is not supported. Most likely there's an extension needed to support the {0} Implementation.";
		private static string MSG_CATALOGS_EMPTY = "No catalogs of templates were found (this is probably due to modifications of the internal folder structure of the extension).";
		#endregion

		#region Constructor and Initialization
		/// <summary>
		/// Static constructor. Registers this editor's implementations.
		/// </summary>
		static SproutLabEditor() {
			_implIdToImplementation.Clear ();
			_editorImplementations.Clear ();
			_orderedEditorImplementations.Clear ();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (Type type in assembly.GetTypes()) {
					// Editor Implementations.
					SproutLabEditorImplAttribute sproutLabEditorImplAttr = type.GetCustomAttribute<SproutLabEditorImplAttribute>();
					if (sproutLabEditorImplAttr != null) {
						ISproutLabEditorImpl instance = (ISproutLabEditorImpl)Activator.CreateInstance (type);
						instance.order = sproutLabEditorImplAttr.order;
						_editorImplementations.Add (type, instance);
						_orderedEditorImplementations.Add (instance);
						int[] implIds = instance.implIds;
						for (int i = 0; i < implIds.Length; i++) {
							if (!_implIdToImplementation.ContainsKey (implIds [i])) {
								_implIdToImplementation.Add (implIds [i], instance);
							} else {
								Debug.LogWarning ("Registering duplicated SproutLabImplementation with implId: " + implIds [i]);
							}
						}
					}
				}
			}
			_orderedEditorImplementations.Sort ((p1,p2) => p1.order.CompareTo (p2.order));
			LoadCatalogs ();
		}
		static void LoadCatalogs () {
			_catalogImplementations.Clear ();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (Type type in assembly.GetTypes()) {
					// Catalog Implementations.
					SproutCatalogImplAttribute attr = type.GetCustomAttribute<SproutCatalogImplAttribute>();
					if (attr != null) {
						SproutCatalog instance = (SproutCatalog)Activator.CreateInstance (type);
						instance.order = attr.order;
						instance.LoadPackages ();
						_catalogImplementations.Add (instance);
					}
				}
			}
			_catalogImplementations = _catalogImplementations.OrderBy(e => e.order).ToList ();
		}
		/// <summary>
		/// Creates a new SproutLabEditor instance.
		/// </summary>
		public SproutLabEditor (VisualElement parentRootVisualElement) {
			this.rootVisualElement = parentRootVisualElement;
			var enumImpl = _implIdToImplementation.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.Value.Initialize (this);
			}

			// Popups.
			viewModePopup = new PopupListComponent (rootVisualElement);
			InitViewModePopup ();
			snapshotOptionsPopup = new PopupListComponent (rootVisualElement);
			InitSnapshotOptionsPopup ();
			variationOptionsPopup = new PopupListComponent (rootVisualElement);
			InitVariationOptionsPopup ();

			// Texture Canvas.
			if (textureCanvas == null) {
				textureCanvas = new TextureCanvas ();
				//structureGraph.Init (structureGeneratorElement.canvasOffset, 1f);
				textureCanvas.Init (Vector2.zero, 1f);
				rootVisualElement.Add (textureCanvas);
				textureCanvas.style.position = UnityEngine.UIElements.Position.Absolute;
				textureCanvas.StretchToParentSize ();
				BindTextureCanvasEvents ();
			}
			textureCanvas.Hide ();
			textureCanvas.RegisterArea (1, 0.2f, 0.2f, 0.5f, 0.5f, 0.5f, 0.5f);


			#if BROCCOLI_DEVEL
			panelSectionOption = new GUIContent[6];
			#endif
			panelSectionOption [0] = 
				new GUIContent ("Structure", "Settings for tunning the structure of branches and leafs.");
			panelSectionOption [1] = 
				new GUIContent ("Textures", "Select the textures to apply to the branch and leaves.");
			panelSectionOption [2] = 
				new GUIContent ("Mapping", "Settings for textures and materials.");
			panelSectionOption [3] = 
				new GUIContent ("Export / Import", "Save or load a branch collection from file or export texture files.");
			panelSectionOption [4] = 
				new GUIContent ("Settings", "Setting optiong for this editor.");
			#if BROCCOLI_DEVEL
			panelSectionOption [5] = 
				new GUIContent ("Debug", "Debug tools.");
			#endif
			structureViewOptions [0] = 
				new GUIContent ("Branches", "Settings for branches.");
			structureViewOptions [1] = 
				new GUIContent ("Sprouts A", "Settings for A sprouts.");
			structureViewOptions [2] = 
				new GUIContent ("Sprouts B", "Settings for B sprouts.");
			structureLevelOptions [0] = 
				new GUIContent ("Level 0", "Modify properties for Snapshot structure Level 0.");
			mapViewOptions [0] = 
				new GUIContent ("Composite", "Composite branch preview.");
			mapViewOptions [1] = 
				new GUIContent ("Albedo", "Unlit albedo texture.");
			mapViewOptions [2] = 
				new GUIContent ("Normals", "Normal (bump) texture.");
			mapViewOptions [3] = 
				new GUIContent ("Extras", "Metallic (R), Glossiness (G), AO (B) texture.");
			mapViewOptions [4] = 
				new GUIContent ("Subsurface", "Subsurface texture.");
			debugViewOptions [0] = 
				new GUIContent ("Geometry", "Geometry debugging options.");
			debugViewOptions [1] = 
				new GUIContent ("Canvas", "Canvas debugging options.");
			debugViewOptions [2] = 
				new GUIContent ("Meshing", "Mesh debugging options.");
			debugViewOptions [3] = 
				new GUIContent ("Process", "Processing debugging options.");
			debugViewOptions [4] = 
				new GUIContent ("Builder", "Builder testers.");
			debugViewOptions [5] = 
				new GUIContent ("Snapshot", "Snapshot testers.");
			debugPolygonOptions [0] = 
				new GUIContent ("All", "Display debug data for all options.");
			debugPolygonOptions [1] = 
				new GUIContent ("Base", "Display debug data for the base polygon.");
			debugPolygonOptions [2] = 
				new GUIContent ("Poly 1", "Display debug data for polygon 1.");
			debugPolygonOptions [3] = 
				new GUIContent ("Poly 2", "Display debug data for polygon 2.");
			debugPolygonOptions [4] = 
				new GUIContent ("Poly 3", "Display debug data for polygon 3.");
			debugPolygonOptions [5] = 
				new GUIContent ("Poly 4", "Display debug data for polygon 4.");
			OnEnable ();
			
		}
		public void OnEnable () {
			// Add update method.
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.update += OnEditorUpdate;

			var callback = EditorApplication.update;
			if (mappingPanel == null) {
				mappingPanel = new SproutLabMappingPanel (this);
			}
			if (settingsPanel == null) {
				settingsPanel = new SproutLabSettingsPanel (this);
			}
			if (debugPanel == null) {
				debugPanel = new SproutLabDebugPanel (this);
			}

			// Init mesh preview
			if (meshPreview == null) {
				meshPreview = MeshPreview.GetInstance ("SproutLabEditor");
				meshPreview.debugShowDebugInfo = false;
				meshPreview.showPivot = false;
				meshPreview.onDrawHandles = OnPreviewMeshDrawHandles;
				meshPreview.onDrawGUI = OnPreviewMeshDrawGUI;
				meshPreview.onRequiresRepaint = OnMeshPreviewRequiresRepaint;
				meshPreview.SetOffset (defaultCanvasSettings.viewOffset);
				meshPreview.SetDirection (defaultCanvasSettings.viewDirection);
				meshPreview.SetZoom (defaultCanvasSettings.defaultZoomFactor);
				meshPreview.minZoomFactor = defaultCanvasSettings.minZoomFactor;
				meshPreview.maxZoomFactor = defaultCanvasSettings.maxZoomFactor;
				Light light = meshPreview.GetLightA ();
				light.lightShadowCasterMode = LightShadowCasterMode.Everything;
				light.spotAngle = 1f;
				light.color = Color.white;
				light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
				light.shadowStrength = 0.4f;
				light.shadowBias = 0;
				light.shadowNormalBias = 2f;
				light.shadows = LightShadows.Hard;
				if (PlayerSettings.colorSpace == ColorSpace.Linear) {
					meshPreview.SetLightA (1.2f, Quaternion.Euler (30, 0, 0));
				} else {
					meshPreview.SetLightA (0.75f, Quaternion.Euler (30, 0, 0));
				}
				SetLightAngle (lightAngleStepValue * -lightAngleStep);
				light = meshPreview.GetLightB ();
				light.lightShadowCasterMode = LightShadowCasterMode.Everything;
				light.intensity = 1.2f;
				lightAngleDisplayStr = "Left 45";

				if (_currentImplementation != null) {
					SetCanvasSettings (_currentImplementation.GetCanvasSettings (PANEL_STRUCTURE, 0));
				} else {
					SetCanvasSettings (null);
				}
			} else {
				meshPreview.Clear ();
			}
			// Init Editor Persistence.
			if (editorPersistence == null) {
				editorPersistence = new EditorPersistence<BranchDescriptorCollectionSO>();
				editorPersistence.elementName = "Branch Collection";
				editorPersistence.saveFileDefaultName = "SproutLabBranchCollection";
				editorPersistence.btnSaveAsNewElement = "Export to File";
				editorPersistence.btnLoadElement = "Import from File";
				editorPersistence.InitMessages ();
				editorPersistence.onCreateNew += OnCreateNewBranchDescriptorCollectionSO;
				editorPersistence.onLoad += OnLoadBranchDescriptorCollectionSO;
				editorPersistence.onGetElementToSave += OnGetBranchDescriptorCollectionSOToSave;
				editorPersistence.onGetElementToSaveFilePath += OnGetBranchDescriptorCollectionSOToSaveFilePath;
				editorPersistence.onSaveElement += OnSaveBranchDescriptorCollectionSO;
				editorPersistence.savePath = ExtensionManager.fullExtensionPath + GlobalSettings.pipelineSavePath;
				editorPersistence.showCreateNewEnabled = false;
				editorPersistence.showSaveCurrentEnabled = false;
			}

			if (verticalSplitView == null) {
				verticalSplitView = new EditorGUISplitView (EditorGUISplitView.Direction.Vertical, SproutFactoryEditorWindow.focusedWindow);
				verticalSplitView.AddFixedSplit (90);
				verticalSplitView.AddDynamicSplit (0.6f);
				verticalSplitView.AddDynamicSplit ();
			}
			ShowPreviewMesh ();
		
			var enumImpl = _implIdToImplementation.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.Value.OnEnable ();
			}
		}
		public void OnDisable () {
			// Remove update method.
			meshPreview.Clear ();
			meshPreview = null;
			EditorApplication.update -= OnEditorUpdate;
			if (sproutSubfactory != null) {
				sproutSubfactory.onReportProgress -= OnReportProgress;
				sproutSubfactory.onFinishProgress -= OnFinishProgress;
			}
			var enumImpl = _implIdToImplementation.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.Value.OnDisable ();
			}
			_currentImplementation = null;
			currentCanvasSettings = null;
			currentStructureSettings = null;
			Clear ();
		}
		void Clear () {
			if (meshPreview != null) {
				meshPreview.Clear ();
			}
			selectedSnapshot = null;
			selectedSproutALevelDescriptor = null;
			selectedSproutBLevelDescriptor = null;
			selectedSproutMapArea = null;
			branchDescriptorCollection = null;
		}
		/// <summary>
		/// Event called when destroying this editor.
		/// </summary>
		private void OnDestroy() {
			meshPreview.Clear ();
			verticalSplitView.Clear ();
			if (textureCanvas != null) textureCanvas.parent.Remove (textureCanvas);
			if (meshPreview.onDrawHandles != null) {
				meshPreview.onDrawHandles -= OnPreviewMeshDrawHandles;
				meshPreview.onDrawGUI -= OnPreviewMeshDrawGUI;
			}
		}
		#endregion

		#region Implementations
		/// <summary>
		/// Retuns an editor implementation according to a registered id.
		/// </summary>
		/// <param name="implId">Implementation id.</param>
		/// <returns>SproutLab implementation or null if none has been registered.</returns>
		private ISproutLabEditorImpl GetImplementation (int implId) {
			if (_implIdToImplementation.ContainsKey (implId)) {
				return _implIdToImplementation [implId];
			}
			return null;
		}
		#endregion

		#region Branch Descriptor Processing
		/// <summary>
		/// Load a BranchDescriptorCollection instance to this Editor.
		/// </summary>
		/// <param name="branchDescriptorCollection">Collection descriptor instance.</param>
		/// <param name="sproutSubfactory">Sprout Factory of the collection.</param>
		/// <param name="path">Path to file if loaded from one, otherwise empty string.</param>
		public void LoadBranchDescriptorCollection (BranchDescriptorCollection branchDescriptorCollection, SproutSubfactory sproutSubfactory, string path = "") {
			GUITextureManager.Init ();
			BroccoEditorGUI.Init ();

			// Dettach panels.
			mappingPanel.Detach ();
			settingsPanel.Detach ();
			debugPanel.Detach ();

			// Hide preview from scene.
			ShowVariationInScene (false);

			// Unload existing implementations.
			var enumImpl = _implIdToImplementation.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.Value.OnUnloadBranchDescriptorCollection ();
			}

			// Assign the current branch descriptor.
			this.branchDescriptorCollection = branchDescriptorCollection;
			this.branchDescriptorCollection.BuildIdToSnapshot ();

			// Creates the sprout factory to handle the branches processing.
			this.sproutSubfactory = sproutSubfactory;
			sproutSubfactory.sproutCompositeManager.Clear ();
			sproutSubfactory.textureManager.Clear ();

			// If the descriptor collection is new.
			if (branchDescriptorCollection.descriptorImplId < 0) {
				//viewMode = ViewMode.SelectMode;
				SetView (ViewMode.SelectMode);
			} else {
				// Set editor implementation according to the collection instance.
				_currentImplementation = GetImplementation (branchDescriptorCollection.descriptorImplId);
				// Implementation found.
				if (_currentImplementation != null) {
					meshPreview.showPreviewTitle = true;
					// meshPreview.previewTitle = _currentImplementation.GetPreviewTitle (branchDescriptorCollection.descriptorImplId);
					SetCanvasSettings (_currentImplementation.GetCanvasSettings (currentPanelSection, 0));
					SetStructureSettings (_currentImplementation.GetStructureSettings (branchDescriptorCollection.descriptorImplId));
					_currentImplementation.OnLoadBranchDescriptorCollection (branchDescriptorCollection, sproutSubfactory);

					// Set the editor view mode.
					SetView (ViewMode.Structure);
					
					sproutSubfactory.onReportProgress -= OnReportProgress;
					sproutSubfactory.onReportProgress += OnReportProgress;
					sproutSubfactory.onFinishProgress -= OnFinishProgress;
					sproutSubfactory.onFinishProgress += OnFinishProgress;

					// Set the editor canvas view mode and select the first index of them (snapshot or variation).
					InitVariationViewOptions ();
					if (branchDescriptorCollection.variations.Count > 0) {
						int variationIndex = (branchDescriptorCollection.variationIndex>-1&&
							branchDescriptorCollection.variationIndex<branchDescriptorCollection.variations.Count?branchDescriptorCollection.variationIndex:0);
						branchDescriptorCollection.variationIndex = variationIndex;
						sproutSubfactory.variationIndex = variationIndex;
					} else {
						branchDescriptorCollection.variationIndex = -1;
						sproutSubfactory.variationIndex = -1;
					}
						
					InitSnapshotViewOptions ();
					if (branchDescriptorCollection.snapshots.Count > 0) {
						int snapshotIndex = (branchDescriptorCollection.snapshotIndex>-1&&
							branchDescriptorCollection.snapshotIndex<branchDescriptorCollection.snapshots.Count?branchDescriptorCollection.snapshotIndex:0);
						branchDescriptorCollection.snapshotIndex = snapshotIndex;
						sproutSubfactory.snapshotIndex = snapshotIndex;
					} else {
						branchDescriptorCollection.snapshotIndex = -1;
						sproutSubfactory.snapshotIndex = -1;
					}
					InitSproutMapLists ();
					
					// Prepare the internal tree factory to process the branch descriptor.
					LoadBranchDescriptorCollectionTreeFactory ();

					if (currentStructureSettings.variantsEnabled) {
						SelectVariation (branchDescriptorCollection.variationIndex);
					} else {
						SelectSnapshot (branchDescriptorCollection.snapshotIndex);
					}
					Vector3 planeSize = new Vector3 (branchDescriptorCollection.planeSize, branchDescriptorCollection.planeSize, branchDescriptorCollection.planeSize);
					meshPreview.SetPlaneMesh (planeSize, branchDescriptorCollection.planeTint);
					meshPreview.SetRuler (branchDescriptorCollection.showRuler, branchDescriptorCollection.rulerColor);
					defaultPreviewBackgroundColor = branchDescriptorCollection.bgColor;
					meshPreview.backgroundColor = defaultPreviewBackgroundColor;
					meshPreview.showAxis = branchDescriptorCollection.showAxisGizmo;
					meshPreview.axisGizmoSize = branchDescriptorCollection.axisGizmoSize;
				}
				// Implementation not found.
				else {
					SetView (ViewMode.NotSupported);
				}
			}
		}
		/// <summary>
		/// Loads an item from the catalog.
		/// </summary>
		/// <param name="itemToLoad">Catalog item to load.</param>
		public bool LoadCatalogItem (SproutCatalog.CatalogItem itemToLoad) {
			if (itemToLoad != null) {
				string pathToCollection = itemToLoad.parentCatalog.GetPathToItemAsset (itemToLoad);
				BranchDescriptorCollectionSO branchDescriptorCollectionSO = editorPersistence.LoadElementFromFile (pathToCollection);
				if (branchDescriptorCollectionSO != null) {
					OnLoadBranchDescriptorCollectionSO (branchDescriptorCollectionSO, pathToCollection);
					SetView (ViewMode.Structure);
					return true;
				} else {
					Debug.LogWarning ("Could not find BranchDescriptorCollectionSO at: " + pathToCollection);
				}
			}
			return false;
		}
		public void UnloadBranchDescriptorCollection () {
			selectedSnapshot = null;
			if (sproutSubfactory != null)
				sproutSubfactory.UnloadPipeline ();
		}
		private void LoadBranchDescriptorCollectionTreeFactory () {
			// Load Sprout Lab base pipeline.
			string pathToAsset = ExtensionManager.fullExtensionPath + GlobalSettings.templateSproutLabPipelinePath;
			pathToAsset = pathToAsset.Replace(Application.dataPath, "Assets");
			Broccoli.Pipe.Pipeline loadedPipeline =
				AssetDatabase.LoadAssetAtPath<Broccoli.Pipe.Pipeline> (pathToAsset);

			if (loadedPipeline == null) {
				throw new UnityException ("Cannot Load Pipeline: The file at the specified path '" + 
					pathToAsset + "' is no valid save file as it does not contain a Pipeline.");
			}
			sproutSubfactory.LoadPipeline (loadedPipeline, branchDescriptorCollection, pathToAsset);
			Resources.UnloadAsset (loadedPipeline);
			ReflectChangesToPipeline ();
			RegenerateStructure ();
			selectedSproutMapArea = null;
		}
		public void ReflectChangesToPipeline () {
			sproutSubfactory.SnapshotCollectionToPipeline ();
		}
		private void ProcessPolygonAreaMesh (PolygonArea polygonArea) {
			Mesh mesh = new Mesh ();
			// Set vertices.
			mesh.SetVertices (polygonArea.points);
			// Set triangles.
			mesh.SetTriangles (polygonArea.triangles, 0);
			mesh.RecalculateBounds ();
			// Set normals.
			mesh.RecalculateNormals ();
			polygonArea.normals.Clear ();
			polygonArea.normals.AddRange (mesh.normals);
			// Set tangents.
			Vector4[] _tangents = new Vector4[polygonArea.points.Count];
			for (int i = 0; i < _tangents.Length; i++) {
				_tangents [i] = Vector3.forward;
				_tangents [i].w = 1f;
			}
			mesh.tangents = _tangents;
			polygonArea.tangents.Clear ();
			polygonArea.tangents.AddRange (mesh.tangents);
			// Set UVs.
			float z, y;
			List<Vector4> uvs = new List<Vector4> ();
			for (int i = 0; i < polygonArea.points.Count; i++) {
				z = Mathf.InverseLerp (polygonArea.aabb.min.z, polygonArea.aabb.max.z, polygonArea.points [i].z);
				y = Mathf.InverseLerp (polygonArea.aabb.min.y, polygonArea.aabb.max.y, polygonArea.points [i].y);
				uvs.Add (new Vector4 (z, y, z, y));
			}
			mesh.SetUVs (0, uvs);
			polygonArea.uvs.Clear ();
			polygonArea.uvs.AddRange (uvs);
			// Set the mesh.
			polygonArea.mesh = mesh;
		}
		/// <summary>
		/// Sets the descriptor type loaded on this editor.
		/// </summary>
		/// <param name="descriptorImplId">Descriptor type to set.</param>
		public void SetBranchDescriptorCollectionImpl (int descriptorImplId, string descriptorImplName, BranchDescriptorCollection collectionToLoad = null) {
			SetView (ViewMode.Structure);
		}
		#endregion

		#region Draw Methods
		/// <summary>
		/// Sets the view mode for this editor.
		/// </summary>
		/// <param name="newViewMode">View mode.</param>
		public void SetView (ViewMode newViewMode) {
			if (newViewMode != _viewMode) {
				SetViewLevels ((int)newViewMode, (int)_canvasStructureView, 0);
			}
		}
		/// <summary>
		/// Sets the view mode and canvas structure view for this editor.
		/// </summary>
		/// <param name="newViewMode">View mode.</param>
		/// <param name="newCanvastructureView">Canvas structure mode.</param>
		public void SetView (ViewMode newViewMode, CanvasStructureView newCanvasStructureView) {
			SetViewLevels ((int)newViewMode, (int)newCanvasStructureView, 0);
		}
		/// <summary>
		/// Sets the view to display templates from the catalog.
		/// </summary>
		/// <param name="implId">Implementation identifier to filter the items on the catalog.</param>
		public void SetTemplateView (int implId = -1) {
			// Set catalog's filter.
			filterCatalogImplIds.Clear ();
			if (implId >= 0) {
				filterCatalogImplIds.Add (implId);
			} 
			SetView ( ViewMode.Templates);
		}
		/// <summary>
		/// Sets the view to display templates from the catalog.
		/// </summary>
		/// <param name="impld">Implementation identifier to filter the items on the catalog.</param>
		public void SetTemplateView (int[] implIds) {
			// Set catalog's filter.
			filterCatalogImplIds.Clear ();
			filterCatalogImplIds.AddRange (implIds);
			SetView (ViewMode.Templates);
		}
		/// <summary>
		/// Sets the canvas structure view mode for this editor.
		/// </summary>
		/// <param name="newCanvasStructureView">Canvas structure view mode.</param>
		public void SetSecondLevelView (CanvasStructureView newCanvasStructureView) {
			if (newCanvasStructureView != _canvasStructureView) {
				SetViewLevels ((int)_viewMode, (int)newCanvasStructureView, 0);
			}
		}
		/// <summary>
		/// Sets the panel section view index.
		/// </summary>
		/// <param name="panelSectionIndex">Panel section index.</param>
		public void SetThirdLevelView (int panelSectionIndex, bool force = false) {
			if (panelSectionIndex != _currentPanelSection || force) {
				SetViewLevels ((int)_viewMode, (int)_canvasStructureView, panelSectionIndex);
			}
		}
		/// <summary>
		/// Set the three levels of view for the editor.
		/// First level: view mode.
		/// Second level: canvas structure view.
		/// Third level: panel section view.
		/// </summary>
		/// <param name="firstLevelView">View mode index.</param>
		/// <param name="secondLevelView">Canvas structure index.</param>
		/// <param name="thirdLevelView">Panel section index.</param>
		public void SetViewLevels (int firstLevelView, int secondLevelView, int thirdLevelView) {
			int oldFirstView = (int)_viewMode;
			_viewMode = (ViewMode)firstLevelView;
			int oldSecondView = (int)_canvasStructureView;
			_canvasStructureView = (CanvasStructureView)secondLevelView;
			int oldThirdView = _currentPanelSection;
			_currentPanelSection = thirdLevelView;

			mappingPanel.Attach ();
			mappingPanel.SetVisible (false);
			settingsPanel.Attach ();
			settingsPanel.SetVisible (false);
			debugPanel.Attach ();
			debugPanel.SetVisible (false);
			textureCanvas.Hide ();
			textureCanvas.SetTexture (null);
			tmpTexture = null;

			// SNAPSHOT.
			if (_canvasStructureView == CanvasStructureView.Snapshot) {
				if (currentPanelSection == PANEL_MAPPING) {
					mappingPanel?.SetVisible (true);
					mappingPanel?.RefreshValues ();
				} else if (currentPanelSection == PANEL_SETTINGS) {
					settingsPanel?.SetVisible (true);
					settingsPanel?.RefreshValues ();
				} else if (_currentPanelSection == PANEL_DEBUG) {
					debugPanel?.SetVisible (true);
					debugPanel?.RefreshValues ();
				}
			}
			// VARIATION.
			else {}

			if (_currentImplementation != null) {
				_currentImplementation.ViewChanged (
					firstLevelView, oldFirstView,
					secondLevelView, oldSecondView,
					thirdLevelView, oldThirdView);
			}
		}
		/// <summary>
		/// Main window GUI draw function.
		/// </summary>
		/// <param name="windowRect">Rect for the main window.</param>
        public void Draw (Rect windowRect) {
			if (currentRect != windowRect) {
				onResize?.Invoke (currentRect, windowRect);
			}
			currentRect = windowRect;
			// SELECT MODE.
			if (viewMode == ViewMode.SelectMode) {
				DrawHeader (windowRect);
				DrawSelectModeView (windowRect);
			}
			// STRUCTURE VIEW.
			else if (viewMode == ViewMode.Structure) {
				if (compositeMaterials == null) {
					SetMapView (VIEW_COMPOSITE, true);
				}
				verticalSplitView.BeginSplitView ();
				DrawHeader (windowRect);
				DrawStructureViewHeader (windowRect);
				verticalSplitView.Split ();
				DrawStructureViewCanvas (windowRect);
				verticalSplitView.Split ();
				DrawStructureViewControlPanel (windowRect);
				verticalSplitView.EndSplitView ();
			}
			// TEMPLATES MODE.
			else if (viewMode == ViewMode.Templates) {				
				DrawHeader (windowRect);
				// Default template view.
				DrawTemplateView (windowRect);
			}
			// NOT SUPPORTED MODE.
			else { 
				DrawNotSupportedView (windowRect);
			}
        }
		/// <summary>
		/// Draws the editor header.
		/// </summary>
		/// <param name="windowRect">Rect for the main window.</param>
		public void DrawHeader (Rect windowRect) {
			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Box ("", GUIStyle.none, 
				GUILayout.Width (150), 
				GUILayout.Height (60));
			if (_currentImplementation != null) {
				GUI.DrawTexture (new Rect (5, 8, 140, 48), _currentImplementation.GetHeaderLogo (), ScaleMode.ScaleToFit);
			} else {
				GUI.DrawTexture (new Rect (5, 8, 140, 48), GUITextureManager.GetBroccoliLogo (), ScaleMode.ScaleToFit);
			}
			string headerMsg = string.Empty;
			var enumImpl = _orderedEditorImplementations.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				headerMsg += enumImpl.Current.GetHeaderMsg () + "\n";
			}
			if (ExtensionManager.isHDRP || ExtensionManager.isURP) {
				headerMsg += "Editor SRP is " + (ExtensionManager.isHDRP?"HDRP":"URP");
			} else {
				headerMsg += "Editor SRP is Standard";
			}
			EditorGUILayout.HelpBox (headerMsg, MessageType.None);
			EditorGUILayout.EndHorizontal ();
		}
		public void SetMapView (int mapView, bool force = false) {
			if (mapView != currentMapView || force) {
				currentMapView = mapView;
				if (compositeMaterials == null) {
					compositeMaterials = sproutSubfactory.treeFactory.previewTree.obj.GetComponent<MeshRenderer>().sharedMaterials;
				}
				BranchDescriptor snapshot = selectedSnapshot;
				if (snapshot != null) {
					if (compositeMaterials.Length > 0 && compositeMaterials[0] != null) {
						if (currentMapView == VIEW_COMPOSITE) { // Composite
							viewModeDisplayStr = "Composite";
							currentPreviewMaterials = sproutSubfactory.GetCompositeMaterials (compositeMaterials,
								branchDescriptorCollection.sproutStyleA,
								branchDescriptorCollection.sproutStyleB,
								branchDescriptorCollection.sproutStyleCrown,
								snapshot.sproutASubmeshIndex,
								snapshot.sproutBSubmeshIndex,
								snapshot.sproutCrownSubmeshIndex);
							meshPreview.backgroundColor = branchDescriptorCollection.bgColor;
							meshPreview.hasSecondPass = true;
							meshPreview.secondPassBackgroundColor = Color.clear;
							meshPreview.secondPassBlend = MeshPreview.SecondPassBlend.BlendColorAlpha;
							meshPreview.secondPassMaterials = sproutSubfactory.GetAlbedoMaterials (compositeMaterials,
								branchDescriptorCollection.sproutStyleA,
								branchDescriptorCollection.sproutStyleB,
								branchDescriptorCollection.sproutStyleCrown,
								branchDescriptorCollection.branchColorShade,
								branchDescriptorCollection.branchColorSaturation,
								snapshot.sproutASubmeshIndex,
								snapshot.sproutBSubmeshIndex,
								snapshot.sproutCrownSubmeshIndex);
							showLightControls = true;
							meshPreview.showAxis = branchDescriptorCollection.showAxisGizmo;
							meshPreview.showRuler = branchDescriptorCollection.showRuler && currentCanvasSettings.showRuler;
							meshPreview.ShowPlaneMesh (currentCanvasSettings.showPlane, branchDescriptorCollection.planeSize, Vector3.zero);
						} else if (currentMapView == VIEW_ALBEDO) { // Albedo
							viewModeDisplayStr = "Albedo";
							currentPreviewMaterials = sproutSubfactory.GetAlbedoMaterials (compositeMaterials,
								branchDescriptorCollection.sproutStyleA,
								branchDescriptorCollection.sproutStyleB,
								branchDescriptorCollection.sproutStyleCrown,
								branchDescriptorCollection.branchColorShade,
								branchDescriptorCollection.branchColorSaturation,
								snapshot.sproutASubmeshIndex,
								snapshot.sproutBSubmeshIndex,
								snapshot.sproutCrownSubmeshIndex);
							meshPreview.backgroundColor = defaultPreviewBackgroundColor;
							meshPreview.hasSecondPass = false;
							showLightControls = false;
							meshPreview.showAxis = false;
							meshPreview.showRuler = false;
							meshPreview.HidePlaneMesh ();
						} else if (currentMapView == VIEW_NORMALS) { // Normals
							viewModeDisplayStr = "Normals";
							currentPreviewMaterials = sproutSubfactory.GetNormalMaterials (compositeMaterials, true);
							meshPreview.backgroundColor = normalPreviewBackgroundColor;
							meshPreview.hasSecondPass = false;
							showLightControls = false;
							meshPreview.showAxis = false;
							meshPreview.showRuler = false;
							meshPreview.HidePlaneMesh ();
						} else if (currentMapView == VIEW_EXTRAS) { // Extra
							viewModeDisplayStr = "Extras";
							currentPreviewMaterials = sproutSubfactory.GetExtraMaterials (compositeMaterials,
								branchDescriptorCollection.sproutStyleA,
								branchDescriptorCollection.sproutStyleB,
								branchDescriptorCollection.sproutStyleCrown,
								snapshot.sproutASubmeshIndex,
								snapshot.sproutBSubmeshIndex,
								snapshot.sproutCrownSubmeshIndex);
							meshPreview.backgroundColor = extrasPreviewBackgroundColor;
							meshPreview.hasSecondPass = false;
							showLightControls = false;
							meshPreview.showAxis = false;
							meshPreview.showRuler = false;
							meshPreview.HidePlaneMesh ();
						} else if (currentMapView == VIEW_SUBSURFACE) { // Subsurface
							viewModeDisplayStr = "Subsurface";
							currentPreviewMaterials = sproutSubfactory.GetSubsurfaceMaterials (compositeMaterials,
								branchDescriptorCollection.sproutStyleA, 
								branchDescriptorCollection.sproutStyleB,
								branchDescriptorCollection.sproutStyleCrown,
								branchDescriptorCollection.branchColorSaturation,
								snapshot.sproutASubmeshIndex,
								snapshot.sproutBSubmeshIndex,
								snapshot.sproutCrownSubmeshIndex);
							meshPreview.backgroundColor = subsurfacePreviewBackgroundColor;
							meshPreview.hasSecondPass = false;
							showLightControls = false;
							meshPreview.showAxis = false;
							meshPreview.showRuler = false;
							meshPreview.HidePlaneMesh (); 
						}
					} else {
						viewModeDisplayStr = "Albedo";
						currentPreviewMaterials = sproutSubfactory.GetAlbedoMaterials (compositeMaterials,
							branchDescriptorCollection.sproutStyleA,
							branchDescriptorCollection.sproutStyleB,
							branchDescriptorCollection.sproutStyleCrown,
							branchDescriptorCollection.branchColorShade,
							branchDescriptorCollection.branchColorSaturation,
							snapshot.sproutASubmeshIndex,
							snapshot.sproutBSubmeshIndex,
							snapshot.sproutCrownSubmeshIndex);
						meshPreview.backgroundColor = defaultPreviewBackgroundColor;
						meshPreview.hasSecondPass = true;
						meshPreview.secondPassBlend = MeshPreview.SecondPassBlend.BlendColor;
						meshPreview.secondPassMaterials = compositeMaterials;
					}
				}
			}
		}
		/// <summary>
		/// Editor View Mode for when the Sprout Factory implementation is not supported.
		/// </summary>
		/// <param name="windowRect">Window rect.</param>
		public void DrawNotSupportedView (Rect windowRect) {
			GUILayout.BeginArea(new Rect(windowRect.width * 0.15f, 0, windowRect.width * 0.7f, windowRect.height));
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical ();
			GUILayout.FlexibleSpace();

			EditorGUILayout.LabelField (labelNotSupportedProject, BroccoEditorGUI.labelBoldCentered);
			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox (string.Format (MSG_NOT_SUPPORTED, branchDescriptorCollection.descriptorImplName), MessageType.Warning);
			EditorGUILayout.Space (200);

			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
		}
		/// <summary>
		/// Editor View Mode for when the Sprout Factory is empty.
		/// </summary>
		/// <param name="windowRect">Window rect.</param>
		public void DrawSelectModeView (Rect windowRect) {
			GUILayout.BeginArea(new Rect(windowRect.width * 0.15f, 0, windowRect.width * 0.7f, windowRect.height));
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical ();
			GUILayout.FlexibleSpace();

			EditorGUILayout.LabelField (labelCreateProject, BroccoEditorGUI.labelBoldCentered);
			EditorGUILayout.Space ();
			var enumImpl = _orderedEditorImplementations.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.DrawSelectModeViewBeforeOptions ();
			}
			enumImpl = _orderedEditorImplementations.GetEnumerator ();
			while (enumImpl.MoveNext ()) {
				enumImpl.Current.DrawSelectModeViewAfterOptions ();
			}
			EditorGUILayout.Space (200);

			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
		}
		/// <summary>
		/// Draws the header for the structure canvas view.
		/// </summary>
		/// <param name="windowRect">Window rect.</param>
		public void DrawStructureViewHeader (Rect windowRect) {
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (generateNewStructureGUI)) {
				GenerateNewStructure ();
			}
			if (GUILayout.Button (regenerateCurrentGUI)) {
				RegenerateStructure ();
			}
			if (GUILayout.Button (loadFromTemplateGUI)) {
				if (_currentImplementation != null) {
					SetTemplateView (_currentImplementation.implIds);
				} else {
					SetTemplateView (-1);
				}
			}
			GUILayout.EndHorizontal ();
		}
		/// <summary>
		/// Generates the mesh for the selected snapshot or variation.
		/// </summary>
		public void GenerateNewStructure () {
			bool propagate = true;
			if (_currentImplementation != null) propagate = _currentImplementation.OnGenerateNewStructure ();
			if (propagate) {
				if (canvasStructureView == CanvasStructureView.Snapshot) {
					onBeforeEditBranchDescriptor (selectedSnapshot, branchDescriptorCollection);
					sproutSubfactory.ProcessSnapshot (branchDescriptorCollection.snapshotIndex, true, SproutSubfactory.MaterialMode.Composite, true);
					onEditBranchDescriptor (selectedSnapshot, branchDescriptorCollection);
					ShowPreviewMesh ();
				} else {
					onBeforeEditVariationDescriptor (selectedVariation, branchDescriptorCollection);
					sproutSubfactory.ProcessVariation (true);
					onEditVariationDescriptor (selectedVariation, branchDescriptorCollection);
					ShowPreviewMesh ();
				}
			}
			if (_currentImplementation != null) _currentImplementation.OnAfterGenerateNewStructure ();
		}
		/// <summary>
		/// Regenerates the mesh for the selected snapshot or variation.
		/// </summary>
		public void RegenerateStructure (bool changed = false, int viewMode = VIEW_COMPOSITE) {
			bool propagate = true;
			if (_currentImplementation != null) propagate = _currentImplementation.OnRegenerateStructure ();
			if (propagate) {
				if (canvasStructureView == CanvasStructureView.Snapshot) {
					sproutSubfactory.ProcessSnapshot (branchDescriptorCollection.snapshotIndex, changed);
					compositeMaterials = null;
					ShowPreviewMesh (viewMode);
				} else {
					sproutSubfactory.ProcessVariation ();
					compositeMaterials = null;
					ShowPreviewMesh (viewMode);
				}
			}
			if (_currentImplementation != null) _currentImplementation.OnAfterRegenerateStructure ();
		}
		/// <summary>
		/// Displays or hides the working Variation on the open scene.
		/// </summary>
		/// <param name="showInScene"></param>
		public void ShowVariationInScene (bool showInScene) {
			//sproutL
			SproutFactory factory = SproutFactory.GetActiveInstance ();
			if (factory != null) {
				// Get Preview GO.
				GameObject previewGO;
				MeshFilter meshFilter;
				MeshRenderer meshRenderer;
				Transform previewTransform = factory.gameObject.transform.Find ("preview");
				if (previewTransform == null) {
					previewGO = new GameObject ("preview");
					previewGO.transform.parent = factory.transform;
					meshFilter = previewGO.AddComponent<MeshFilter> ();
					meshRenderer = previewGO.AddComponent<MeshRenderer> ();
				} else {
					previewGO = previewTransform.gameObject;
					meshFilter = previewGO.GetComponent<MeshFilter> ();
					meshRenderer = previewGO.GetComponent<MeshRenderer> ();
				}
				if (showInScene) {
					meshFilter.sharedMesh = meshPreview.GetMesh (0);
					meshRenderer.sharedMaterials = currentPreviewMaterials;
				} else {
					meshFilter.sharedMesh = null;
					meshRenderer.sharedMaterials = new Material[0];
				}
				previewGO.hideFlags = HideFlags.HideAndDontSave;
			}
		}
		/// <summary>
		/// Editor View Mode for when the Sprout Factory is displaying a structure.
		/// </summary>
		/// <param name="windowRect">Window rect.</param>
		public void DrawStructureViewCanvas (Rect windowRect) {
			int splitHeigth = verticalSplitView.GetCurrentSplitSize ();
			GUILayout.Box ("", GUIStyle.none, 
				GUILayout.Width (windowRect.width), 
				GUILayout.Height (splitHeigth > 0 ? splitHeigth - 2 : 0));
			Rect viewRect = GUILayoutUtility.GetLastRect ();
			// STRUCTURE IS SNAPSHOT.
			if (canvasStructureView == CanvasStructureView.Snapshot) {
				// DRAW NOT SELECTED SNAPSHOT.
				if (selectedSnapshot == null) {
					DrawEmptyCanvas (viewRect);
				}
				// DRAW TEXTURE CANVAS.
				else if (currentPanelSection == PANEL_TEXTURE && (currentStructureView == STRUCTURE_SPROUT_A || currentStructureView == STRUCTURE_SPROUT_B || currentStructureView == STRUCTURE_CROWN)  
					&& selectedSproutMapArea != null)
				{
					if (selectedSproutMapArea.texture != null) {
						currenTextureView = TEXTURE_VIEW_TEXTURE;
						tmpTexture = sproutSubfactory.GetSproutTexture (selectedSproutMapGroup, selectedSproutMapAreaIndex);

						if (tmpTexture != null) {
							if (textureCanvas.SetTexture (tmpTexture)) {
								textureCanvas.Show ();
								textureCanvas.SetAreaRectAndPivot (1, selectedSproutMapArea.rect, selectedSproutMapArea.pivot);
								_resizeTextureCanvas = true;
								_centerTextureCanvas = true;
							}
						}

						if (Event.current.type == EventType.Repaint) {
							if (_resizeTextureCanvas) {
								textureCanvas.style.marginTop = viewRect.y + verticalSplitView.GetCurrentSplitOffset ();
								textureCanvas.style.height = viewRect.height;
								textureCanvas.style.width = viewRect.width;
								textureCanvas.guiRect = viewRect;
								_resizeTextureCanvas = false;
							}
							if (_centerTextureCanvas) {
								textureCanvas.CenterTexture ();
								_centerTextureCanvas = false;
							}
						}
					} else {
						tmpTexture = null;
						if (textureCanvas.SetTexture (tmpTexture)) {
							textureCanvas.Hide ();
						}
						DrawEmptyTexture (viewRect);
					}
				}
				#if BROCCOLI_DEVEL
				else if (currentPanelSection == PANEL_DEBUG && debugPanel.drawViewCanvas) {
					debugPanel.DrawViewCanvas (viewRect);
				}
				#endif
				// DRAW STRUCTURE MESH.
				else {
					if (currenTextureView == TEXTURE_VIEW_TEXTURE) {
						RegenerateStructure ();
						currenTextureView = TEXTURE_VIEW_STRUCTURE;
					}
					if (viewRect.height > 0) {
						meshPreviewRect = viewRect;
						meshPreview.RenderViewport (viewRect, GUIStyle.none, currentPreviewMaterials);
					}
				}
			}
			// STRUCTURE IS VARIATION.
			else {
				// DRAW NOT SELECTED VARIATION.
				if (selectedVariation == null) {
					DrawEmptyCanvas (viewRect);
				}
				// DRAW SELECTED VARIATION.
				else {
					if (viewRect.height > 0) {
						meshPreviewRect = viewRect;
						bool drawn = meshPreview.RenderViewport (viewRect, GUIStyle.none, currentPreviewMaterials);
						if (!drawn) {
							RegenerateStructure ();
						}
					}
				}
			}
		}
		/// <summary>
		/// Displays an empty canvas when no structure (snapshot or variation) has been selected.
		/// </summary>
		/// <param name="rect">Recto to display the empty canvas.</param>
		void DrawEmptyCanvas (Rect rect) {
			rect.x = rect.width / 2f - 80;
			rect.y = rect.height / 2f;
			rect.height = EditorGUIUtility.singleLineHeight * 2;
			rect.width = 220;
			EditorGUI.HelpBox (rect, MSG_NO_SELECTION, MessageType.Info);
		}
		/// <summary>
		/// Displays an empty canvas when no texture has been selected.
		/// </summary>
		/// <param name="rect">Recto to display the empty canvas.</param>
		void DrawEmptyTexture (Rect rect) {
			rect.x = rect.width / 2f - 80;
			rect.y = rect.height / 2f;
			rect.height = EditorGUIUtility.singleLineHeight * 2;
			rect.width = 240;
			EditorGUI.HelpBox (rect, MSG_NO_TEXTURE_SELECTION, MessageType.Info);
		}
		/// <summary>
		/// Editor View Mode for when the Sprout Factory is displaying the template catalog.
		/// </summary>
		/// <param name="windowRect">Window rect.</param>
		public void DrawTemplateView (Rect windowRect) {
			Rect toolboxRect = new Rect (windowRect);
			toolboxRect.height = EditorGUIUtility.singleLineHeight;
			GUILayout.BeginHorizontal ();
			if (branchDescriptorCollection.descriptorImplId < 0) {
				if (GUILayout.Button (backToCreateProjectGUI)) {
					SetView (ViewMode.SelectMode);
				}
				#if BROCCOLI_DEVEL
				if (GUILayout.Button ("Reload Catalogs")) {
					LoadCatalogs ();
				}
				#endif
			} else {
				if (GUILayout.Button (backToStructureViewGUI)) {
					SetView (ViewMode.Structure);
				}
				#if BROCCOLI_DEVEL
				if (GUILayout.Button ("Reload Catalogs")) {
					LoadCatalogs ();
				}
				#endif
			}
			GUILayout.EndHorizontal ();
			
			// Draw Templates.  
			if (catalogs.Count > 0) {
				string categoryKey = "";
				SproutCatalog catalog;
				for (int i = 0; i < catalogs.Count; i++) {
					catalog = catalogs [i];
					if (filterCatalogImplIds.Count == 0 || filterCatalogImplIds.Contains (catalog.implId)) {
						var enumerator = catalog.contents.GetEnumerator ();
						while (enumerator.MoveNext ()) {
							var contentPair = enumerator.Current;
							categoryKey = contentPair.Key;
							EditorGUILayout.LabelField (categoryKey, BroccoEditorGUI.label);
							int columns = Mathf.CeilToInt ((windowRect.width - 8) / catalogItemSize);
							Dictionary<string, List<GUIContent>> contents = catalog.GetGUIContents ();
							int guiContentCount = contents[categoryKey].Count;
							int height = Mathf.CeilToInt (guiContentCount / (float)columns) * catalogItemSize;
							int selectedIndex = 
								GUILayout.SelectionGrid (-1, catalog.GetGUIContents ()[categoryKey].ToArray (), 
									columns, Broccoli.Utils.TreeCanvasGUI.catalogItemStyle, GUILayout.Height (height), GUILayout.Width (windowRect.width - 8));
							if (selectedIndex >= 0 &&
							EditorUtility.DisplayDialog (MSG_LOAD_CATALOG_ITEM_TITLE, 
								MSG_LOAD_CATALOG_ITEM_MESSAGE, 
								MSG_LOAD_CATALOG_ITEM_OK, 
								MSG_LOAD_CATALOG_ITEM_CANCEL)) {
								// Load the Snapshot Collection SO
								SproutCatalog.CatalogItem itemToLoad = catalog.GetItemAtIndex (categoryKey, selectedIndex);
								if (LoadCatalogItem (itemToLoad)) {
									selectedIndex = -1;
								}
								GUIUtility.ExitGUI ();
							}
						}
					}
				}
			} else {
				EditorGUILayout.HelpBox (MSG_CATALOGS_EMPTY, MessageType.Warning);
			}
		}
		public void DrawStructureViewControlPanel (Rect windowRect) {
			DrawSnapshotsPanel ();
			if (currentStructureSettings.variantsEnabled) {
				DrawVariationsPanel ();
			}
			// Custom panel per editor implementation.
			if (currentStructureSettings.hasCustomPanels) {
				_currentImplementation.DrawPanels (windowRect);
			}
			// Default structure collection panels.
			else {
				DrawDefaultPanels (windowRect);	
			}
		}
		#endregion

		#region Structure Panel
		public void DrawEmptyPanel (string msg) {
			Rect rect = new Rect (splitRect);
			rect.x = rect.width / 2f - 120;
			rect.y = rect.y + (rect.height / 2f);
			rect.height = EditorGUIUtility.singleLineHeight * 2;
			rect.width = 300;
			EditorGUI.HelpBox (rect, msg, MessageType.Warning);
		}
		public void DrawDefaultPanels (Rect windowRect) {
			if (selectedSnapshot == null) {
                DrawEmptyPanel (MSG_NO_SNAPSHOT_PANEL);
                return;
            }
			int _currentPanelSection = GUILayout.Toolbar (currentPanelSection, panelSectionOption, GUI.skin.button);
			if (_currentPanelSection != currentPanelSection) {
				SetThirdLevelView (_currentPanelSection);
				ShowPreviewMesh (currentMapView);
				if (_currentImplementation != null) {
					SetCanvasSettings (_currentImplementation.GetCanvasSettings (currentPanelSection, 0));
				}
			}
			switch (currentPanelSection) {
				case PANEL_STRUCTURE:
					DrawStructurePanel (windowRect);
					break;
				case PANEL_TEXTURE:
					DrawTexturePanel ();
					break;
				case PANEL_MAPPING:
					DrawMappingPanel ();
					break;
				case PANEL_EXPORT:
					DrawExportPanel ();
					break;
				case PANEL_SETTINGS:
					DrawSettingsPanel ();
					break;
				case PANEL_DEBUG:
					DrawDebugPanel ();
					break;
			}
		}
		/// <summary>
		/// Draw the structure panel window view.
		/// </summary>
		public void DrawStructurePanel (Rect windowRect) {
			if (selectedSnapshot == null) return;

			bool changed = false;
			float girthAtBase = selectedSnapshot.girthAtBase;
			float girthAtTop = selectedSnapshot.girthAtTop;
			float noiseResolution = selectedSnapshot.noiseResolution;
			float noiseAtBase = selectedSnapshot.noiseAtBase;
			float noiseAtTop = selectedSnapshot.noiseAtTop;
			float noiseScaleAtBase = selectedSnapshot.noiseScaleAtBase;
			float noiseScaleAtTop = selectedSnapshot.noiseScaleAtTop;
			float sproutASize = selectedSnapshot.sproutASize;
			float sproutAScaleAtBase = selectedSnapshot.sproutAScaleAtBase;
			float sproutAScaleAtTop = selectedSnapshot.sproutAScaleAtTop;
			float sproutAScaleVariance = selectedSnapshot.sproutAScaleVariance;
			SproutMesh.ScaleMode sproutAScaleMode = selectedSnapshot.sproutAScaleMode;
			float sproutAFlipAlign = selectedSnapshot.sproutAFlipAlign;
			float sproutANormalRandomness = selectedSnapshot.sproutANormalRandomness;
			float sproutABendingAtTop = selectedSnapshot.sproutABendingAtTop;
            float sproutABendingAtBase = selectedSnapshot.sproutABendingAtBase;
            float sproutASideBendingAtTop = selectedSnapshot.sproutASideBendingAtTop;
            float sproutASideBendingAtBase = selectedSnapshot.sproutASideBendingAtBase;
			float sproutBSize = selectedSnapshot.sproutBSize;
			float sproutBScaleAtBase = selectedSnapshot.sproutBScaleAtBase;
			float sproutBScaleAtTop = selectedSnapshot.sproutBScaleAtTop;
			float sproutBScaleVariance = selectedSnapshot.sproutBScaleVariance;
			SproutMesh.ScaleMode sproutBScaleMode = selectedSnapshot.sproutBScaleMode;
			float sproutBFlipAlign = selectedSnapshot.sproutBFlipAlign;
			float sproutBNormalRandomness = selectedSnapshot.sproutBNormalRandomness;
			float sproutBBendingAtTop = selectedSnapshot.sproutBBendingAtTop;
            float sproutBBendingAtBase = selectedSnapshot.sproutBBendingAtBase;
            float sproutBSideBendingAtTop = selectedSnapshot.sproutBSideBendingAtTop;
            float sproutBSideBendingAtBase = selectedSnapshot.sproutBSideBendingAtBase;
			int activeLevels = selectedSnapshot.activeLevels;
			bool crownEnabled = selectedSnapshot.crownEnabled;
			float crownProbability = selectedSnapshot.crownProbability;
			float crownMinRange = selectedSnapshot.crownMinRange;
			float crownMaxRange = selectedSnapshot.crownMaxRange;
			int crownMinFrequency = selectedSnapshot.crownMinFrequency;
			int crownMaxFrequency = selectedSnapshot.crownMaxFrequency;
			float crownDepth = selectedSnapshot.crownDepth;
			float crownSize = selectedSnapshot.crownSize;
			float crownScaleAtBase = selectedSnapshot.crownScaleAtBase;
			float crownScaleAtTop = selectedSnapshot.crownScaleAtTop;
			float crownScaleVariance = selectedSnapshot.crownScaleVariance;
			float crownMinParallelAlignAtBase = selectedSnapshot.sproutCrownLevelDescriptor.minParallelAlignAtBase;
			float crownMaxParallelAlignAtBase = selectedSnapshot.sproutCrownLevelDescriptor.maxParallelAlignAtBase;
			float crownMinParallelAlignAtTop = selectedSnapshot.sproutCrownLevelDescriptor.minParallelAlignAtTop;
			float crownMaxParallelAlignAtTop = selectedSnapshot.sproutCrownLevelDescriptor.maxParallelAlignAtTop;
			showLODOptions = true;

			if (activeLevels + 1 != structureLevelOptions.Length) {
				UpdateStructureLevelsGUI (activeLevels);
			}

			EditorGUILayout.BeginHorizontal ();
			// View Mode Selection.
			EditorGUILayout.BeginVertical ();
			EditorGUILayout.LabelField (labelStructures, BroccoEditorGUI.labelBoldCentered);
			currentStructureView = GUILayout.SelectionGrid (currentStructureView, structureViewOptions, 1, GUILayout.Width (secondaryPanelColumnWidth));
			EditorGUILayout.EndVertical ();
			// Mapping Settings.
			structurePanelScroll = EditorGUILayout.BeginScrollView (structurePanelScroll, GUILayout.ExpandWidth (true));
			switch (currentStructureView) {
				case STRUCTURE_BRANCH: // BRANCHES.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (currentStructureSettings.branchEntityName + " Global Settings", BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					// Active levels
					if (snapSettings.enableSelectActiveLevels) {
						EditorGUI.BeginChangeCheck ();
						activeLevels = EditorGUILayout.Popup (labelActiveLevels, selectedSnapshot.activeLevels, levelOptions); 
						changed |= EditorGUI.EndChangeCheck ();
					}
					
					// GIRTH.
					branchGirthFoldout = EditorGUILayout.Foldout (branchGirthFoldout, labelGirthFoldout, BroccoEditorGUI.foldoutBold);
					if (branchGirthFoldout) {
						EditorGUI.indentLevel++;
						// Branch structure settings
						girthAtBase = EditorGUILayout.Slider (
							labelGirthAtBase, 
							selectedSnapshot.girthAtBase, 
							snapSettings.girthAtBaseMinLimit, 
							snapSettings.girthAtBaseMaxLimit);
						if (girthAtBase != selectedSnapshot.girthAtBase) {
							changed |= true;
						}
						girthAtTop = EditorGUILayout.Slider (
							labelGirthAtTop, 
							selectedSnapshot.girthAtTop, 
							snapSettings.girthAtTopMinLimit, 
							snapSettings.girthAtTopMaxLimit);
						if (girthAtTop != selectedSnapshot.girthAtTop) {
							changed |= true;
						}
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}

					// NOISE.
					branchNoiseFoldout = EditorGUILayout.Foldout (branchNoiseFoldout, labelNoiseFoldout, BroccoEditorGUI.foldoutBold);
					if (branchNoiseFoldout) {
						EditorGUI.indentLevel++;
						noiseResolution = EditorGUILayout.Slider (labelNoiseResolution, selectedSnapshot.noiseResolution, 0f, 1.5f);
						if (noiseResolution != selectedSnapshot.noiseResolution) {
							changed |= true;
						}
						noiseAtBase = EditorGUILayout.Slider (labelNoiseAtBase, selectedSnapshot.noiseAtBase, 0f, 1f);
						if (noiseAtBase != selectedSnapshot.noiseAtBase) {
							changed |= true;
						}
						noiseAtTop = EditorGUILayout.Slider (labelNoiseAtTop, selectedSnapshot.noiseAtTop, 0f, 1f);
						if (noiseAtTop != selectedSnapshot.noiseAtTop) {
							changed |= true;
						}
						noiseScaleAtBase = EditorGUILayout.Slider (labelNoiseScaleAtBase, selectedSnapshot.noiseScaleAtBase, 0f, 1f);
						if (noiseScaleAtBase != selectedSnapshot.noiseScaleAtBase) {
							changed |= true;
						}
						noiseScaleAtTop = EditorGUILayout.Slider (labelNoiseScaleAtTop, selectedSnapshot.noiseScaleAtTop, 0f, 1f);
						if (noiseScaleAtTop != selectedSnapshot.noiseScaleAtTop) {
							changed |= true;
						}
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (currentStructureSettings.branchEntityName + " Levels Settings", BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					// Draw Branch Structure Panel
					changed |= DrawBranchStructurePanel ();
					break;
				case STRUCTURE_SPROUT_A: // LEAVES.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelSproutASettings, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					// Active levels
					EditorGUI.BeginChangeCheck ();
					activeLevels = EditorGUILayout.Popup (labelActiveLevels, selectedSnapshot.activeLevels, levelOptions); 
					changed = EditorGUI.EndChangeCheck ();
					// Sprout structure settings
					EditorGUI.BeginChangeCheck ();
					// SIZE & SCALE
					sproutSizeScaleFoldout = EditorGUILayout.Foldout (sproutSizeScaleFoldout, labelSizeScale, BroccoEditorGUI.foldoutBold);
					if (sproutSizeScaleFoldout) {
						EditorGUI.indentLevel++;
						sproutASize = EditorGUILayout.Slider (labelSize, selectedSnapshot.sproutASize, 0.005f, 0.3f);
						sproutAScaleAtBase = EditorGUILayout.Slider (labelScaleAtBase, selectedSnapshot.sproutAScaleAtBase, 0.1f, 5f);
						sproutAScaleAtTop = EditorGUILayout.Slider (labelScaleAtTop, selectedSnapshot.sproutAScaleAtTop, 0.1f, 5f);
						sproutAScaleVariance = EditorGUILayout.Slider (labelScaleVariance, selectedSnapshot.sproutAScaleVariance, 0f, 1f);
						sproutAScaleMode = (SproutMesh.ScaleMode)EditorGUILayout.EnumPopup (labelScaleMode, selectedSnapshot.sproutAScaleMode);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					// ALIGN
					sproutAlignFoldout = EditorGUILayout.Foldout (sproutAlignFoldout, labelAlign, BroccoEditorGUI.foldoutBold);
					if (sproutAlignFoldout) {
						EditorGUI.indentLevel++;
						sproutAFlipAlign = EditorGUILayout.Slider (labelPlaneAlignment, selectedSnapshot.sproutAFlipAlign, 0.5f, 1f);
						sproutANormalRandomness = EditorGUILayout.Slider (labelAlignmentRandom, selectedSnapshot.sproutANormalRandomness, 0f, 1f);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					// BEND
					sproutBendFoldout = EditorGUILayout.Foldout (sproutBendFoldout, labelBend, BroccoEditorGUI.foldoutBold);
					if (sproutBendFoldout) {
						EditorGUI.indentLevel++;
						sproutABendingAtTop = EditorGUILayout.Slider (labelBendingAtTop, selectedSnapshot.sproutABendingAtTop, -1f, 1f);
						sproutABendingAtBase = EditorGUILayout.Slider (labelBendingAtBase, selectedSnapshot.sproutABendingAtBase, -1f, 1f);
						sproutASideBendingAtTop = EditorGUILayout.Slider (labelSideBendingAtTop, selectedSnapshot.sproutASideBendingAtTop, -1f, 1f);
						sproutASideBendingAtBase = EditorGUILayout.Slider (labelSideBendingAtBase, selectedSnapshot.sproutASideBendingAtBase, -1f, 1f);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					changed |= EditorGUI.EndChangeCheck ();
					EditorGUILayout.Space ();
					// Draw Sprout A Hierarchy Structure Panel
					EditorGUILayout.LabelField (labelSproutALevelSettings, BroccoEditorGUI.labelBold);
					changed |= DrawSproutAStructurePanel ();
					break;
				case STRUCTURE_SPROUT_B: // LEAVES.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelSproutBSettings, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					// Active levels
					EditorGUI.BeginChangeCheck ();
					activeLevels = EditorGUILayout.Popup (labelActiveLevels, selectedSnapshot.activeLevels, levelOptions); 
					changed = EditorGUI.EndChangeCheck ();
					// Sprout structure settings
					EditorGUI.BeginChangeCheck ();
					// SIZE & SCALE
					sproutSizeScaleFoldout = EditorGUILayout.Foldout (sproutSizeScaleFoldout, labelSizeScale, BroccoEditorGUI.foldoutBold);
					if (sproutSizeScaleFoldout) {
						EditorGUI.indentLevel++;
						sproutBSize = EditorGUILayout.Slider (labelSize, selectedSnapshot.sproutBSize, 0.005f, 0.3f);
						sproutBScaleAtBase = EditorGUILayout.Slider (labelScaleAtBase, selectedSnapshot.sproutBScaleAtBase, 0.1f, 5f);
						sproutBScaleAtTop = EditorGUILayout.Slider (labelScaleAtTop, selectedSnapshot.sproutBScaleAtTop, 0.1f, 5f);
						sproutBScaleVariance = EditorGUILayout.Slider (labelScaleVariance, selectedSnapshot.sproutBScaleVariance, 0f, 1f);
						sproutBScaleMode = (SproutMesh.ScaleMode)EditorGUILayout.EnumPopup (labelScaleMode, selectedSnapshot.sproutBScaleMode);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					// ALIGN
					sproutAlignFoldout = EditorGUILayout.Foldout (sproutAlignFoldout, labelAlign, BroccoEditorGUI.foldoutBold);
					if (sproutAlignFoldout) {
						EditorGUI.indentLevel++;
						sproutBFlipAlign = EditorGUILayout.Slider (labelPlaneAlignment, selectedSnapshot.sproutBFlipAlign, 0.5f, 1f);
						sproutBNormalRandomness = EditorGUILayout.Slider (labelAlignmentRandom, selectedSnapshot.sproutBNormalRandomness, 0f, 1f);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					// BEND
					sproutBendFoldout = EditorGUILayout.Foldout (sproutBendFoldout, labelBend, BroccoEditorGUI.foldoutBold);
					if (sproutBendFoldout) {
						EditorGUI.indentLevel++;
						sproutBBendingAtTop = EditorGUILayout.Slider (labelBendingAtTop, selectedSnapshot.sproutBBendingAtTop, -1f, 1f);
						sproutBBendingAtBase = EditorGUILayout.Slider (labelBendingAtBase, selectedSnapshot.sproutBBendingAtBase, -1f, 1f);
						sproutBSideBendingAtTop = EditorGUILayout.Slider (labelSideBendingAtTop, selectedSnapshot.sproutBSideBendingAtTop, -1f, 1f);
						sproutBSideBendingAtBase = EditorGUILayout.Slider (labelSideBendingAtBase, selectedSnapshot.sproutBSideBendingAtBase, -1f, 1f);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					changed |= EditorGUI.EndChangeCheck ();
					EditorGUILayout.Space ();
					// Draw Sprout B Hierarchy Structure Panel
					EditorGUILayout.LabelField (labelSproutBLevelSettings, BroccoEditorGUI.labelBold);
					changed |= DrawSproutBStructurePanel ();
					break;
				case STRUCTURE_CROWN: // FLOWERS.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelCrownSettings, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					// Active levels
					EditorGUI.BeginChangeCheck ();
					crownEnabled = EditorGUILayout.Toggle (labelCrownEnabled, crownEnabled);
					crownProbability = EditorGUILayout.Slider (
						labelCrownProbability, 
						selectedSnapshot.crownProbability,
						snapSettings.crownMinProbability,
						1f);
					changed = EditorGUI.EndChangeCheck ();
					changed |= BroccoEditorGUI.IntRangePropertyField (
						ref crownMinFrequency,
						ref crownMaxFrequency,
						snapSettings.crownMinFrequency, 
						snapSettings.crownMaxFrequency,
						labelCrownFrequency);
					changed |= BroccoEditorGUI.FloatRangePropertyField (
						ref crownMinRange,
						ref crownMaxRange,
						snapSettings.crownMinRange, 
						snapSettings.crownMaxRange,
						labelCrownRange);
					EditorGUI.BeginChangeCheck ();
					crownDepth = EditorGUILayout.Slider (
						labelCrownDepth, 
						selectedSnapshot.crownDepth,
						-0.15f,
						0.15f);
					// SIZE & SCALE
					sproutSizeScaleFoldout = EditorGUILayout.Foldout (sproutSizeScaleFoldout, labelSizeScale, BroccoEditorGUI.foldoutBold);
					if (sproutSizeScaleFoldout) {
						EditorGUI.indentLevel++;
						crownSize = EditorGUILayout.Slider (labelSize, selectedSnapshot.crownSize, 0.005f, 0.2f);
						crownScaleAtBase = EditorGUILayout.Slider (labelScaleAtBase, selectedSnapshot.crownScaleAtBase, 0.1f, 2.5f);
						crownScaleAtTop = EditorGUILayout.Slider (labelScaleAtTop, selectedSnapshot.crownScaleAtTop, 0.1f, 2.5f);
						crownScaleVariance = EditorGUILayout.Slider (labelScaleVariance, selectedSnapshot.crownScaleVariance, 0f, 1f);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					changed |= EditorGUI.EndChangeCheck ();
					// ALIGN
					sproutAlignFoldout = EditorGUILayout.Foldout (sproutAlignFoldout, labelAlign, BroccoEditorGUI.foldoutBold);
					if (sproutAlignFoldout) {
						EditorGUI.indentLevel++;
						changed |= BroccoEditorGUI.FloatRangePropertyField (
							ref crownMinParallelAlignAtBase,
							ref crownMaxParallelAlignAtBase,
							-1f, 1f, labelParallelAlignAtBase);
						changed |= BroccoEditorGUI.FloatRangePropertyField (
							ref crownMinParallelAlignAtTop,
							ref crownMaxParallelAlignAtTop,
							-1f, 1f, labelParallelAlignAtTop);
						EditorGUI.indentLevel--;
						EditorGUILayout.Space ();
					}
					break;
			}
			EditorGUILayout.EndScrollView ();
			EditorGUILayout.EndHorizontal ();
			if (changed) {
				branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				CopyFromProxyBranchLevelDescriptor ();
				CopyFromProxySproutALevelDescriptor ();
				CopyFromProxySproutBLevelDescriptor ();
				if (selectedSnapshot.activeLevels != activeLevels) {
					selectedSnapshot.activeLevels = activeLevels;
					UpdateStructureLevelsGUI (activeLevels);
				}
				selectedSnapshot.girthAtBase = girthAtBase;
				selectedSnapshot.girthAtTop = girthAtTop;
				selectedSnapshot.noiseResolution = noiseResolution;
				selectedSnapshot.noiseAtBase = noiseAtBase;
				selectedSnapshot.noiseAtTop = noiseAtTop;
				selectedSnapshot.noiseScaleAtBase = noiseScaleAtBase;
				selectedSnapshot.noiseScaleAtTop = noiseScaleAtTop;
				selectedSnapshot.sproutASize = sproutASize;
				selectedSnapshot.sproutAScaleAtBase = sproutAScaleAtBase;
				selectedSnapshot.sproutAScaleAtTop = sproutAScaleAtTop;
				selectedSnapshot.sproutAScaleVariance = sproutAScaleVariance;
				selectedSnapshot.sproutAScaleMode = sproutAScaleMode;
				selectedSnapshot.sproutAFlipAlign = sproutAFlipAlign;
				selectedSnapshot.sproutANormalRandomness = sproutANormalRandomness;
				selectedSnapshot.sproutABendingAtTop = sproutABendingAtTop;
				selectedSnapshot.sproutABendingAtBase = sproutABendingAtBase;
				selectedSnapshot.sproutASideBendingAtTop = sproutASideBendingAtTop;
				selectedSnapshot.sproutASideBendingAtBase = sproutASideBendingAtBase;
				selectedSnapshot.sproutBSize = sproutBSize;
				selectedSnapshot.sproutBScaleAtBase = sproutBScaleAtBase;
				selectedSnapshot.sproutBScaleAtTop = sproutBScaleAtTop;
				selectedSnapshot.sproutBScaleVariance = sproutBScaleVariance;
				selectedSnapshot.sproutBScaleMode = sproutBScaleMode;
				selectedSnapshot.sproutBFlipAlign = sproutBFlipAlign;
				selectedSnapshot.sproutBNormalRandomness = sproutBNormalRandomness;
				selectedSnapshot.sproutBBendingAtTop = sproutBBendingAtTop;
				selectedSnapshot.sproutBBendingAtBase = sproutBBendingAtBase;
				selectedSnapshot.sproutBSideBendingAtTop = sproutBSideBendingAtTop;
				selectedSnapshot.sproutBSideBendingAtBase = sproutBSideBendingAtBase;
				selectedSnapshot.crownEnabled = crownEnabled;
				selectedSnapshot.crownProbability = crownProbability;
				selectedSnapshot.crownMinRange = crownMinRange;
				selectedSnapshot.crownMaxRange = crownMaxRange;
				selectedSnapshot.crownMinFrequency = crownMinFrequency;
				selectedSnapshot.crownMaxFrequency = crownMaxFrequency;
				selectedSnapshot.crownDepth = crownDepth;
				selectedSnapshot.crownSize = crownSize;
				selectedSnapshot.crownScaleAtBase = crownScaleAtBase;
				selectedSnapshot.crownScaleAtTop = crownScaleAtTop;
				selectedSnapshot.crownScaleVariance = crownScaleVariance;
				selectedSnapshot.sproutCrownLevelDescriptor.minParallelAlignAtBase = crownMinParallelAlignAtBase;
				selectedSnapshot.sproutCrownLevelDescriptor.maxParallelAlignAtBase = crownMaxParallelAlignAtBase;
				selectedSnapshot.sproutCrownLevelDescriptor.minParallelAlignAtTop = crownMinParallelAlignAtTop;
				selectedSnapshot.sproutCrownLevelDescriptor.maxParallelAlignAtTop = crownMaxParallelAlignAtTop;
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				ReflectChangesToPipeline ();
				RegenerateStructure (true);
			}
		}
		void UpdateStructureLevelsGUI (int activeLevels) {
			structureLevelOptions = new GUIContent [activeLevels + 1];
			for (int i = 0; i <= activeLevels; i++) {
				structureLevelOptions [i] = new GUIContent ("Level " + i, "Modify properties for Snapshot structure Level " + i + ".");
			}
			if (selectedSnapshot != null && selectedSnapshot.selectedLevelIndex >= activeLevels) {
				selectedSnapshot.selectedLevelIndex = 0;
			}
		}
		bool DrawBranchStructurePanel () {
			bool changed = false;
			// Foldouts per hierarchy branch level.
			GUIStyle st = BroccoEditorGUI.foldoutBold;
			GUIStyle stB = BroccoEditorGUI.labelBold;
			if (structureLevelOptions.Length > 1) {
				selectedSnapshot.selectedLevelIndex = GUILayout.Toolbar (selectedSnapshot.selectedLevelIndex, structureLevelOptions);
			}

			EditorGUI.indentLevel++;
			selectedBranchLevelDescriptor = selectedSnapshot.branchLevelDescriptors [selectedSnapshot.selectedLevelIndex];
			CopyToProxyBranchLevelDescriptor ();


			// Properties for non-root levels.
			if (selectedSnapshot.selectedLevelIndex == 0) {
				// FREQUENCY
				if (snapSettings.stemFrequencyEnabled) {
					changed |= BroccoEditorGUI.IntRangePropertyField (
						ref proxyBranchLevelDescriptor.minFrequency,
						ref proxyBranchLevelDescriptor.maxFrequency,
						snapSettings.stemMinFrequency, 
						snapSettings.stemMaxFrequency, 
						labelFrequency);
				}
				// LENGTH
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minLengthAtBase,
					ref proxyBranchLevelDescriptor.maxLengthAtBase,
					snapSettings.stemMinLength, 
					snapSettings.stemMaxLength, 
					currentStructureSettings.branchEntitiesName + " Length");
				// RADIUS
				if (snapSettings.stemRadiusEnabled) {
					float newRadius = EditorGUILayout.Slider (
						labelRadius, 
						proxyBranchLevelDescriptor.radius, 
						snapSettings.stemMinRadius, 
						snapSettings.stemMaxRadius);
					if (newRadius != proxyBranchLevelDescriptor.radius) {
						proxyBranchLevelDescriptor.radius = newRadius;
						changed = true;
					}
				}
			} else {
				if (selectedSnapshot.selectedLevelIndex == 1) {
					levelFrequency.x = snapSettings.level1MinFrequency;
					levelFrequency.y = snapSettings.level1MaxFrequency;
					levelLengthAtBase.x = snapSettings.level1MinLengthAtBase;
					levelLengthAtBase.y = snapSettings.level1MaxLengthAtBase;
					levelLengthAtTop.x = snapSettings.level1MinLengthAtTop;
					levelLengthAtTop.y = snapSettings.level1MaxLengthAtTop;
				} else if (selectedSnapshot.selectedLevelIndex == 2) {
					levelFrequency.x = snapSettings.level2MinFrequency;
					levelFrequency.y = snapSettings.level2MaxFrequency;
					levelLengthAtBase.x = snapSettings.level2MinLengthAtBase;
					levelLengthAtBase.y = snapSettings.level2MaxLengthAtBase;
					levelLengthAtTop.x = snapSettings.level2MinLengthAtTop;
					levelLengthAtTop.y = snapSettings.level2MaxLengthAtTop;
				} else {
					levelFrequency.x = snapSettings.level3MinFrequency;
					levelFrequency.y = snapSettings.level3MaxFrequency;
					levelLengthAtBase.x = snapSettings.level3MinLengthAtBase;
					levelLengthAtBase.y = snapSettings.level3MaxLengthAtBase;
					levelLengthAtTop.x = snapSettings.level3MinLengthAtTop;
					levelLengthAtTop.y = snapSettings.level3MaxLengthAtTop;
				}
				// FREQUENCY
				changed |= BroccoEditorGUI.IntRangePropertyField (
					ref proxyBranchLevelDescriptor.minFrequency,
					ref proxyBranchLevelDescriptor.maxFrequency,
					(int)levelFrequency.x, 
					(int)levelFrequency.y, 
					labelFrequency);
				// RANGE.
				if (snapSettings.hasRange) {
					changed |= BroccoEditorGUI.FloatRangePropertyField (
						ref proxyBranchLevelDescriptor.minRange,
						ref proxyBranchLevelDescriptor.maxRange,
						levelRange.x, 
						levelRange.y, 
						labelRange);
				}
				EditorGUI.BeginChangeCheck ();
				proxyBranchLevelDescriptor.distributionCurve = 
					EditorGUILayout.CurveField (labelDistributionCurve, proxyBranchLevelDescriptor.distributionCurve, Color.yellow, curveLimitsRect);
				if (EditorGUI.EndChangeCheck ()) {
					changed = true;
				}
				EditorGUILayout.Space ();

				// LENGTH
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minLengthAtBase,
					ref proxyBranchLevelDescriptor.maxLengthAtBase,
					levelLengthAtBase.x, 
					levelLengthAtBase.y, 
					labelLengthAtBase);
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minLengthAtTop,
					ref proxyBranchLevelDescriptor.maxLengthAtTop,
					levelLengthAtTop.x, 
					levelLengthAtTop.y, 
					labelLengthAtTop);
				EditorGUI.BeginChangeCheck ();
				proxyBranchLevelDescriptor.lengthCurve = 
					EditorGUILayout.CurveField (labelLengthCurve, proxyBranchLevelDescriptor.lengthCurve, Color.yellow, curveLimitsRect);
				if (EditorGUI.EndChangeCheck ()) {
					changed = true;
				}
				// SPACING VARIANCE
				levelSpacingVariance = EditorGUILayout.Slider (
					labelSpacingVariance, 
					proxyBranchLevelDescriptor.spacingVariance,
					0f, 
					1f);
				if (levelSpacingVariance!= proxyBranchLevelDescriptor.spacingVariance) {
					proxyBranchLevelDescriptor.spacingVariance = levelSpacingVariance;
					changed = true;
				}
				EditorGUILayout.Space ();
				// ALIGNMENT
				// Min Branch Branch Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minParallelAlignAtBase,
					ref proxyBranchLevelDescriptor.maxParallelAlignAtBase,
					-1f, 1f, labelParallelAlignAtBase);
				// Max Branch Branch Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minParallelAlignAtTop,
					ref proxyBranchLevelDescriptor.maxParallelAlignAtTop,
					-1f, 1f, labelParallelAlignAtTop);
				// Min Branch Gravity Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minGravityAlignAtBase,
					ref proxyBranchLevelDescriptor.maxGravityAlignAtBase,
					-1f, 1f, labelGravityAlignAtBase);
				// Max Branch Gravity Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxyBranchLevelDescriptor.minGravityAlignAtTop,
					ref proxyBranchLevelDescriptor.maxGravityAlignAtTop,
					-1f, 1f, labelGravityAlignAtTop);
				if (snapSettings.level1PlaneAlignmentEnabled) {
					// Min Branch Plane Align At Base.
					changed |= BroccoEditorGUI.FloatRangePropertyField (
						ref proxyBranchLevelDescriptor.minPlaneAlignAtBase,
						ref proxyBranchLevelDescriptor.maxPlaneAlignAtBase,
						-2f, 2f, labelPlaneAlignment);
				}
			}
			EditorGUI.indentLevel--;
					
			return changed;
		}
		bool DrawSproutAStructurePanel () {
			bool changed = false;

			if (structureLevelOptions.Length > 1) {
				selectedSnapshot.selectedLevelIndex = GUILayout.Toolbar (selectedSnapshot.selectedLevelIndex, structureLevelOptions);
			}
			
			selectedSproutALevelDescriptor = selectedSnapshot.sproutALevelDescriptors [selectedSnapshot.selectedLevelIndex];
			CopyToProxySproutALevelDescriptor ();
			// ENABLED
			bool isEnabled = EditorGUILayout.Toggle ("Enabled", proxySproutALevelDescriptor.isEnabled);
			if (isEnabled != proxySproutALevelDescriptor.isEnabled) {
				changed = true;
				proxySproutALevelDescriptor.isEnabled = isEnabled;
			}
			EditorGUILayout.Space ();
			if (isEnabled) {
				int minFreq = 0;
				int maxFreq = 25;
				if (selectedSnapshot.selectedLevelIndex == 0) {
					minFreq = snapSettings.stemMinSproutFrequency;
					maxFreq = snapSettings.stemMaxSproutFrequency;
				} else if (selectedSnapshot.selectedLevelIndex == 1) {
					minFreq = snapSettings.level1MinSproutFrequency;
					maxFreq = snapSettings.level1MaxSproutFrequency;
				} else if (selectedSnapshot.selectedLevelIndex == 2) {
					minFreq = snapSettings.level2MinSproutFrequency;
					maxFreq = snapSettings.level2MaxSproutFrequency;
				} else {
					minFreq = snapSettings.level3MinSproutFrequency;
					maxFreq = snapSettings.level3MaxSproutFrequency;
				}
				// FREQUENCY
				changed |= BroccoEditorGUI.IntRangePropertyField (
					ref proxySproutALevelDescriptor.minFrequency,
					ref proxySproutALevelDescriptor.maxFrequency,
					minFreq, 
					maxFreq, 
					labelFrequency);
				// RANGE.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutALevelDescriptor.minRange,
					ref proxySproutALevelDescriptor.maxRange,
					0f, 1f, labelRange);
				EditorGUI.BeginChangeCheck ();
				proxySproutALevelDescriptor.distribution =
					(BranchDescriptor.SproutLevelDescriptor.Distribution) EditorGUILayout.EnumPopup (labelSproutDistributionMode, proxySproutALevelDescriptor.distribution);
				proxySproutALevelDescriptor.distributionCurve = EditorGUILayout.CurveField (labelDistributionCurve, proxySproutALevelDescriptor.distributionCurve,
					Color.yellow, curveLimitsRect);
				proxySproutALevelDescriptor.spacingVariance =  EditorGUILayout.Slider (
					labelSpacingVariance, 
					proxySproutALevelDescriptor.spacingVariance,
					0f, 
					1f);
				if (EditorGUI.EndChangeCheck ()) {
					changed = true;
				}
				EditorGUILayout.Space ();
				// ALIGNMENT
				// Min Branch Branch Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutALevelDescriptor.minParallelAlignAtBase,
					ref proxySproutALevelDescriptor.maxParallelAlignAtBase,
					-1f, 1f, labelParallelAlignAtBase);
				// Max Branch Branch Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutALevelDescriptor.minParallelAlignAtTop,
					ref proxySproutALevelDescriptor.maxParallelAlignAtTop,
					-1f, 1f, labelParallelAlignAtTop);
				// Min Branch Gravity Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutALevelDescriptor.minGravityAlignAtBase,
					ref proxySproutALevelDescriptor.maxGravityAlignAtBase,
					-1f, 1f, labelGravityAlignAtBase);
				// Max Branch Gravity Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutALevelDescriptor.minGravityAlignAtTop,
					ref proxySproutALevelDescriptor.maxGravityAlignAtTop,
					-1f, 1f, labelGravityAlignAtTop);
				EditorGUILayout.Space ();
			}
						
			return changed;
		}
		bool DrawSproutBStructurePanel () {
			bool changed = false;

			if (structureLevelOptions.Length > 1) {
				selectedSnapshot.selectedLevelIndex = GUILayout.Toolbar (selectedSnapshot.selectedLevelIndex, structureLevelOptions);
			}
			
			selectedSproutBLevelDescriptor = selectedSnapshot.sproutBLevelDescriptors [selectedSnapshot.selectedLevelIndex];
			CopyToProxySproutBLevelDescriptor ();
			// ENABLED
			bool isEnabled = EditorGUILayout.Toggle (labelEnabled, proxySproutBLevelDescriptor.isEnabled);
			if (isEnabled != proxySproutBLevelDescriptor.isEnabled) {
				changed = true;
				proxySproutBLevelDescriptor.isEnabled = isEnabled;
			}
			EditorGUILayout.Space ();
			if (isEnabled) {
				// FREQUENCY
				changed |= BroccoEditorGUI.IntRangePropertyField (
					ref proxySproutBLevelDescriptor.minFrequency,
					ref proxySproutBLevelDescriptor.maxFrequency,
					1, 25, labelFrequency);
				// RANGE.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutBLevelDescriptor.minRange,
					ref proxySproutBLevelDescriptor.maxRange,
					0f, 1f, labelRange);
				EditorGUI.BeginChangeCheck ();
				proxySproutBLevelDescriptor.distribution =
					(BranchDescriptor.SproutLevelDescriptor.Distribution) EditorGUILayout.EnumPopup (labelSproutDistributionMode, proxySproutBLevelDescriptor.distribution);
				proxySproutBLevelDescriptor.distributionCurve = EditorGUILayout.CurveField (labelDistributionCurve, proxySproutBLevelDescriptor.distributionCurve,
					Color.yellow, curveLimitsRect);
				proxySproutBLevelDescriptor.spacingVariance =  EditorGUILayout.Slider (
					labelSpacingVariance, 
					proxySproutBLevelDescriptor.spacingVariance,
					0f, 
					1f);
				if (EditorGUI.EndChangeCheck ()) {
					changed = true;
				}
				EditorGUILayout.Space ();
				// ALIGNMENT
				// Min Branch Branch Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutBLevelDescriptor.minParallelAlignAtBase,
					ref proxySproutBLevelDescriptor.maxParallelAlignAtBase,
					-1f, 1f, labelParallelAlignAtBase);
				// Max Branch Branch Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutBLevelDescriptor.minParallelAlignAtTop,
					ref proxySproutBLevelDescriptor.maxParallelAlignAtTop,
					-1f, 1f, labelParallelAlignAtTop);
				// Min Branch Gravity Align At Base.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutBLevelDescriptor.minGravityAlignAtBase,
					ref proxySproutBLevelDescriptor.maxGravityAlignAtBase,
					-1f, 1f, labelGravityAlignAtBase);
				// Max Branch Gravity Align At Top.
				changed |= BroccoEditorGUI.FloatRangePropertyField (
					ref proxySproutBLevelDescriptor.minGravityAlignAtTop,
					ref proxySproutBLevelDescriptor.maxGravityAlignAtTop,
					-1f, 1f, labelGravityAlignAtTop);
				EditorGUILayout.Space ();
			}
					
			return changed;
		}
		#endregion

		#region Texture Panel
		/// <summary>
		/// Draws the texture panel window view.
		/// </summary>
		public void DrawTexturePanel () {
			bool changed = false;
			showLODOptions = false;
			Texture2D branchAlbedoTexture = branchDescriptorCollection.branchAlbedoTexture;
			Texture2D branchNormalTexture = branchDescriptorCollection.branchNormalTexture;
			float branchTextureYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
			EditorGUILayout.BeginHorizontal ();
			// View Mode Selection.
			EditorGUILayout.BeginVertical ();
			EditorGUILayout.LabelField (labelStructures, BroccoEditorGUI.labelBoldCentered);
			currentStructureView = GUILayout.SelectionGrid (currentStructureView, structureViewOptions, 1, GUILayout.Width (secondaryPanelColumnWidth));
			EditorGUILayout.EndVertical ();
			// Mapping Settings.
			texturePanelScroll = EditorGUILayout.BeginScrollView (texturePanelScroll, GUILayout.ExpandWidth (true));
			switch (currentStructureView) {
				case STRUCTURE_BRANCH: // BRANCHES.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelBranchTextures, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginVertical (GUILayout.Width (200));
					branchAlbedoTexture = (Texture2D) EditorGUILayout.ObjectField ("Main Texture", branchDescriptorCollection.branchAlbedoTexture, typeof (Texture2D), false);
					if (branchAlbedoTexture != branchDescriptorCollection.branchAlbedoTexture) {
						changed = true;
					}
					branchNormalTexture = (Texture2D) EditorGUILayout.ObjectField ("Normal Texture", branchDescriptorCollection.branchNormalTexture, typeof (Texture2D), false);
					if (branchNormalTexture != branchDescriptorCollection.branchNormalTexture) {
						changed = true;
					}
					EditorGUILayout.EndVertical ();
					branchTextureYDisplacement = EditorGUILayout.Slider (labelYDisplacement, branchDescriptorCollection.branchTextureYDisplacement, -3f, 4f);
					if (branchTextureYDisplacement != branchDescriptorCollection.branchTextureYDisplacement) {
						changed = true;
					}
					break;
				case STRUCTURE_SPROUT_A: // SPROUT A.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelSproutATextures, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					sproutMapChanged = false;
					sproutAMapList.DoLayoutList ();
					changed |= sproutMapChanged;
					break;
				case STRUCTURE_SPROUT_B: // SPROUT B.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelSproutBTextures, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					sproutMapChanged = false;
					sproutBMapList.DoLayoutList ();
					changed |= sproutMapChanged;
					break;
				case STRUCTURE_CROWN: // CROWN.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelCrownTextures, BroccoEditorGUI.labelBold);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					sproutMapChanged = false;
					crownMapList.DoLayoutList ();
					changed |= sproutMapChanged;
					break;
			}
			EditorGUILayout.EndScrollView ();
			EditorGUILayout.EndHorizontal ();
			if (changed) {
				branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				CopyFromProxyBranchLevelDescriptor ();
				branchDescriptorCollection.branchAlbedoTexture = branchAlbedoTexture;
				branchDescriptorCollection.branchNormalTexture = branchNormalTexture;
				branchDescriptorCollection.branchTextureYDisplacement = branchTextureYDisplacement;
				if (sproutMapChanged && selectedSproutMapArea != null) {
					CopyFromProxySproutMap ();
				}
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				ReflectChangesToPipeline ();
				RegenerateStructure ();
			}
		}
		#endregion

		#region Mapping Panel
		/// <summary>
		/// Draws the mapping panel window view.
		/// </summary>
		public void DrawMappingPanel () {
			// Calculate the rect.
            EditorGUILayout.LabelField (string.Empty);
            Rect offsetRect = GUILayoutUtility.GetLastRect ();
            float splitTop = verticalSplitView.GetCurrentSplitOffset ();
            Rect splitRect = verticalSplitView.currentRect;
            splitRect.Set (splitRect.x, splitTop + offsetRect.yMin, splitRect.width, splitRect.height - offsetRect.yMin);
			mappingPanel.SetRect (splitRect);

            if (mappingPanel.requiresRepaint) {
                mappingPanel.Repaint ();
            }
			return;
		}
		public void DrawCompositeMappingSettings () {
			float branchColorSaturation = branchDescriptorCollection.branchColorSaturation;
			float branchColorShade = branchDescriptorCollection.branchColorShade;

			float minColorShadeA = branchDescriptorCollection.sproutStyleA.minColorShade;
			float maxColorShadeA = branchDescriptorCollection.sproutStyleA.maxColorShade;
			Color tintColorA = branchDescriptorCollection.sproutStyleA.colorTint;
			float minColorTintA = branchDescriptorCollection.sproutStyleA.minColorTint;
			float maxColorTintA = branchDescriptorCollection.sproutStyleA.maxColorTint;
			float metallicA = branchDescriptorCollection.sproutStyleA.metallic;
			float glossinessA = branchDescriptorCollection.sproutStyleA.glossiness;
			float subsurfaceMulA = branchDescriptorCollection.sproutStyleA.subsurface;
			float colorSaturationA = branchDescriptorCollection.sproutStyleA.minColorSaturation;
			
			float minColorShadeB = branchDescriptorCollection.sproutStyleB.minColorShade;
			float maxColorShadeB = branchDescriptorCollection.sproutStyleB.maxColorShade;
			Color tintColorB = branchDescriptorCollection.sproutStyleB.colorTint;
			float minColorTintB = branchDescriptorCollection.sproutStyleB.minColorTint;
			float maxColorTintB = branchDescriptorCollection.sproutStyleB.maxColorTint;
			float metallicB = branchDescriptorCollection.sproutStyleB.metallic;
			float glossinessB = branchDescriptorCollection.sproutStyleB.glossiness;
			float subsurfaceMulB = branchDescriptorCollection.sproutStyleB.subsurface;
			float colorSaturationB = branchDescriptorCollection.sproutStyleB.minColorSaturation;

			float minColorShadeCrown = branchDescriptorCollection.sproutStyleCrown.minColorShade;
			float maxColorShadeCrown = branchDescriptorCollection.sproutStyleCrown.maxColorShade;
			Color tintColorCrown = branchDescriptorCollection.sproutStyleCrown.colorTint;
			float minColorTintCrown = branchDescriptorCollection.sproutStyleCrown.minColorTint;
			float maxColorTintCrown = branchDescriptorCollection.sproutStyleCrown.maxColorTint;
			float metallicCrown = branchDescriptorCollection.sproutStyleCrown.metallic;
			float glossinessCrown = branchDescriptorCollection.sproutStyleCrown.glossiness;
			float subsurfaceMulCrown = branchDescriptorCollection.sproutStyleCrown.subsurface;
			float colorSaturationCrown = branchDescriptorCollection.sproutStyleCrown.minColorSaturation;

			EditorGUI.BeginChangeCheck ();

			// Branches
			branchMapFoldout = EditorGUILayout.Foldout (branchMapFoldout, labelBranches);
			if (branchMapFoldout) {
				EditorGUI.indentLevel++;
				//branchColorShade = EditorGUILayout.Slider ("Shade", branchColorShade, 0.6f, 1f);
				branchColorSaturation = EditorGUILayout.Slider (labelSaturation, branchColorSaturation, 0f, 1.5f);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space ();

			// Sprout A
			sproutAMapFoldout = EditorGUILayout.Foldout (sproutAMapFoldout, labelSproutA);
			if (sproutAMapFoldout) {
				EditorGUI.indentLevel++;
				// Saturation A
				colorSaturationA = EditorGUILayout.Slider (labelSaturation, colorSaturationA, 0f, 1.5f);

				// Shade A
				BroccoEditorGUI.FloatRangePropertyField (
							ref minColorShadeA,
							ref maxColorShadeA,
							0.65f, 1f, labelShadeRange);
				// Tint A
				tintColorA = EditorGUILayout.ColorField (labelTintColor, tintColorA);
				BroccoEditorGUI.FloatRangePropertyField (
							ref minColorTintA,
							ref maxColorTintA,
							0f, 1f, labelTintRange);
				// Metallic, glossiness, surface A
				metallicA = EditorGUILayout.Slider (labelMetallic, metallicA, 0f, 1f);
				glossinessA = EditorGUILayout.Slider (labelGlossiness, glossinessA, 0f, 1f);

				// Subsurface.
				subsurfaceMulA = EditorGUILayout.Slider (labelTransluscencyFactor, subsurfaceMulA, 0.5f, 1.5f);
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space ();

			// Sprout B
			sproutBMapFoldout = EditorGUILayout.Foldout (sproutBMapFoldout, labelSproutB);
			if (sproutBMapFoldout) {
				EditorGUI.indentLevel++;
				// Saturation B
				colorSaturationB = EditorGUILayout.Slider (labelSaturation, colorSaturationB, 0f, 1.5f);

				// Shade B
				BroccoEditorGUI.FloatRangePropertyField (
							ref minColorShadeB,
							ref maxColorShadeB,
							0.65f, 1f, labelShadeRange);
				// Tint B
				tintColorB = EditorGUILayout.ColorField (labelTintColor, tintColorB);
				BroccoEditorGUI.FloatRangePropertyField (
							ref minColorTintB,
							ref maxColorTintB,
							0f, 1f, labelTintRange);
				// Metallic, glossiness, surface B
				metallicB = EditorGUILayout.Slider (labelMetallic, metallicB, 0f, 1f);
				glossinessB = EditorGUILayout.Slider (labelGlossiness, glossinessB, 0f, 1f);

				// Subsurface.
				subsurfaceMulB = EditorGUILayout.Slider (labelTransluscencyFactor, subsurfaceMulB, 0.5f, 1.5f);
				EditorGUI.indentLevel--;
			}

			// Sprout Crown
			if (snapSettings.hasCrown) {
				sproutCrownMapFoldout = EditorGUILayout.Foldout (sproutCrownMapFoldout, labelSproutCrown);
				if (sproutCrownMapFoldout) {
					EditorGUI.indentLevel++;
					// Saturation
					colorSaturationCrown = EditorGUILayout.Slider (labelSaturation, colorSaturationCrown, 0f, 1.5f);

					// Shade B
					BroccoEditorGUI.FloatRangePropertyField (
								ref minColorShadeCrown,
								ref maxColorShadeCrown,
								0.65f, 1f, labelShadeRange);
					// Tint
					tintColorCrown = EditorGUILayout.ColorField (labelTintColor, tintColorCrown);
					BroccoEditorGUI.FloatRangePropertyField (
								ref minColorTintCrown,
								ref maxColorTintCrown,
								0f, 1f, labelTintRange);
					// Metallic, glossiness, surface
					metallicCrown = EditorGUILayout.Slider (labelMetallic, metallicCrown, 0f, 1f);
					glossinessCrown = EditorGUILayout.Slider (labelGlossiness, glossinessCrown, 0f, 1f);

					// Subsurface.
					subsurfaceMulCrown = EditorGUILayout.Slider (labelTransluscencyFactor, subsurfaceMulCrown, 0.5f, 1.5f);
					EditorGUI.indentLevel--;
				}
			}

			if (EditorGUI.EndChangeCheck ()) {
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				branchDescriptorCollection.branchColorShade = branchColorShade;
				branchDescriptorCollection.branchColorSaturation = branchColorSaturation;
				branchDescriptorCollection.sproutStyleA.minColorShade = minColorShadeA;
				branchDescriptorCollection.sproutStyleA.maxColorShade = maxColorShadeA;
				branchDescriptorCollection.sproutStyleA.colorTint = tintColorA;
				branchDescriptorCollection.sproutStyleA.minColorTint = minColorTintA;
				branchDescriptorCollection.sproutStyleA.maxColorTint = maxColorTintA;
				branchDescriptorCollection.sproutStyleA.metallic = metallicA;
				branchDescriptorCollection.sproutStyleA.glossiness = glossinessA;
				branchDescriptorCollection.sproutStyleA.subsurface = subsurfaceMulA;
				branchDescriptorCollection.sproutStyleA.minColorSaturation = colorSaturationA;
				branchDescriptorCollection.sproutStyleB.minColorShade = minColorShadeB;
				branchDescriptorCollection.sproutStyleB.maxColorShade = maxColorShadeB;
				branchDescriptorCollection.sproutStyleB.colorTint = tintColorB;
				branchDescriptorCollection.sproutStyleB.minColorTint = minColorTintB;
				branchDescriptorCollection.sproutStyleB.maxColorTint = maxColorTintB;
				branchDescriptorCollection.sproutStyleB.metallic = metallicB;
				branchDescriptorCollection.sproutStyleB.glossiness = glossinessB;
				branchDescriptorCollection.sproutStyleB.subsurface = subsurfaceMulB;
				branchDescriptorCollection.sproutStyleB.minColorSaturation = colorSaturationB;
				branchDescriptorCollection.sproutStyleCrown.minColorShade = minColorShadeCrown;
				branchDescriptorCollection.sproutStyleCrown.maxColorShade = maxColorShadeCrown;
				branchDescriptorCollection.sproutStyleCrown.colorTint = tintColorCrown;
				branchDescriptorCollection.sproutStyleCrown.minColorTint = minColorTintCrown;
				branchDescriptorCollection.sproutStyleCrown.maxColorTint = maxColorTintCrown;
				branchDescriptorCollection.sproutStyleCrown.metallic = metallicCrown;
				branchDescriptorCollection.sproutStyleCrown.glossiness = glossinessCrown;
				branchDescriptorCollection.sproutStyleCrown.subsurface = subsurfaceMulCrown;
				branchDescriptorCollection.sproutStyleCrown.minColorSaturation = colorSaturationCrown;
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				ReflectChangesToPipeline ();
				sproutSubfactory.sproutCompositeManager.Clear ();
				RegenerateStructure (true, currentMapView);
			}
		}
		#endregion

		#region Export Panel
		/// <summary>
		/// Draws the export panel window view.
		/// </summary>
		public void DrawExportPanel () {
			bool changed = false;
			showLODOptions = false;
			EditorGUILayout.BeginHorizontal ();
			
			// View Mode Selection.
			EditorGUILayout.BeginVertical ();
			if (currentStructureSettings.displayExportDescriptor && 
				!currentStructureSettings.displayExportPrefab &&
				!currentStructureSettings.displayExportTextures)
			{
				currentExportView = EXPORT_DESCRIPTOR;
			} else if (!currentStructureSettings.displayExportDescriptor && 
				currentStructureSettings.displayExportPrefab &&
				!currentStructureSettings.displayExportTextures)
			{
				currentExportView = EXPORT_PREFAB;
			} else if (!currentStructureSettings.displayExportDescriptor && 
				!currentStructureSettings.displayExportPrefab &&
				currentStructureSettings.displayExportTextures)
			{
				currentExportView = EXPORT_TEXTURES;
			} else {
				EditorGUILayout.LabelField (labelExportOptions, BroccoEditorGUI.labelBoldCentered);
				if (currentStructureSettings.displayExportDescriptor) {
					if (GUILayout.Button (exportViewOptionDescriptorGUI)) currentExportView = EXPORT_DESCRIPTOR;
				}
				if (currentStructureSettings.displayExportPrefab) {
					if (GUILayout.Button (exportViewOptionPrefabGUI)) currentExportView = EXPORT_PREFAB;
				}
				if (currentStructureSettings.displayExportTextures) {
					if (GUILayout.Button (exportViewOptionTexturesGUI)) currentExportView = EXPORT_TEXTURES;
				}
				EditorGUILayout.Space ();
			}

			EditorGUILayout.LabelField (labelImportOptions, BroccoEditorGUI.labelBoldCentered);
			editorPersistence.DrawOptions ();
			EditorGUILayout.EndVertical ();
			// Export Settings.
			exportPanelScroll = EditorGUILayout.BeginScrollView (exportPanelScroll, GUILayout.ExpandWidth (true));
			BranchDescriptorCollection.TextureSize exportTextureSize;
			int exportTake;
			string exportPrefix;
			int paddingSize;
			bool isAtlas;
			string albedoPath;
			string normalsPath;
			string extrasPath;
			string subsurfacePath;
			int exportFlags;
			BranchDescriptorCollection.ExportMode exportMode;
			string subfolder;
			switch (currentExportView) {
				case EXPORT_DESCRIPTOR: // Branch Descriotor.
					EditorGUILayout.LabelField (labelBranchDescExportSettings, BroccoEditorGUI.labelBold);
					EditorGUILayout.HelpBox (MSG_EXPORT_DESCRIPTOR, MessageType.None);
					// SNAPSHOT PROCESSING.
					if (GUILayout.Button (exportDescriptorAndAtlasGUI)) {
						ExportDescriptorWithAtlas (true);
					}
					EditorGUILayout.Space ();
					// ATLAS TEXTURE.
					EditorGUILayout.LabelField (labelAtlasTextureSettings, BroccoEditorGUI.labelBold);
					// Atlas size.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelAtlasSize, BroccoEditorGUI.label);
					exportTextureSize = 
						(BranchDescriptorCollection.TextureSize)EditorGUILayout.EnumPopup (branchDescriptorCollection.exportTextureSize, GUILayout.Width (120));
					changed |= exportTextureSize != branchDescriptorCollection.exportTextureSize;
					EditorGUILayout.EndHorizontal ();
					// Atlas padding.
					paddingSize = EditorGUILayout.IntField (labelPadding, branchDescriptorCollection.exportAtlasPadding);
					if (paddingSize < 0 || paddingSize > 25) {
						paddingSize = branchDescriptorCollection.exportAtlasPadding;			
					}
					changed |= paddingSize != branchDescriptorCollection.exportAtlasPadding;
					// Export take.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelTake, BroccoEditorGUI.label);
					exportTake = EditorGUILayout.IntField (branchDescriptorCollection.exportTake);
					changed |= exportTake != branchDescriptorCollection.exportTake;
					EditorGUILayout.EndHorizontal ();
					// Export prefix.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelPrefix, BroccoEditorGUI.label);
					exportPrefix = EditorGUILayout.TextField (branchDescriptorCollection.exportPrefix);
					changed |= !exportPrefix.Equals (branchDescriptorCollection.exportPrefix);
					EditorGUILayout.EndHorizontal ();
					// Export path.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelPath, BroccoEditorGUI.label);
					EditorGUILayout.LabelField ("/Assets" + branchDescriptorCollection.exportPath);
					if (GUILayout.Button (selectPathGUI, GUILayout.Width (30))) {
						string currentPath = Application.dataPath + branchDescriptorCollection.exportPath;
						string selectedPath = EditorUtility.OpenFolderPanel (labelTexturesFolder, currentPath, "");
						if (!string.IsNullOrEmpty (selectedPath)) {
							selectedPath = selectedPath.Substring (Application.dataPath.Length);
							if (selectedPath.CompareTo (branchDescriptorCollection.exportPath) != 0) {
								branchDescriptorCollection.exportPath = selectedPath;
								changed = true;
							}
						}
						GUIUtility.ExitGUI();
					}
					EditorGUILayout.EndHorizontal ();

					// List of paths
					isAtlas = branchDescriptorCollection.exportMode == BranchDescriptorCollection.ExportMode.Atlas;
					subfolder = branchDescriptorCollection.exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
					albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, isAtlas);
					normalsPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, isAtlas);
					extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, isAtlas);
					subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, isAtlas);
					EditorGUILayout.HelpBox (albedoPath + "\n" + normalsPath + "\n" + extrasPath + "\n" + subsurfacePath, MessageType.None);

					// Export textures flags
					exportFlags = EditorGUILayout.MaskField(labelTextures, branchDescriptorCollection.exportTexturesFlags, exportTextureOptions);
					changed |= exportFlags != branchDescriptorCollection.exportTexturesFlags;
					if (changed) {
						branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
						onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
						branchDescriptorCollection.exportTextureSize = exportTextureSize;
						branchDescriptorCollection.exportAtlasPadding = paddingSize;
						branchDescriptorCollection.exportTake = exportTake;
						branchDescriptorCollection.exportPrefix = exportPrefix;
						branchDescriptorCollection.exportTexturesFlags = exportFlags;
						onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					}
					break;
				case EXPORT_PREFAB: // Prefab.
					EditorGUILayout.LabelField (labelPrefabSettings, BroccoEditorGUI.labelBold);
					EditorGUILayout.HelpBox (MSG_EXPORT_PREFAB, MessageType.None);
					if (GUILayout.Button (exportPrefabGUI)) {
						ExportPrefab ();
					}
					EditorGUILayout.Space ();

					// OUTPUT FILE
					EditorGUILayout.LabelField (labelPrefabFileSettings, BroccoEditorGUI.labelBold);
					// Export path.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelPath, BroccoEditorGUI.label);
					EditorGUILayout.LabelField ("Assets" + branchDescriptorCollection.exportPath);
					if (GUILayout.Button (selectPathGUI, GUILayout.Width (30))) {
						string currentPath = Application.dataPath + branchDescriptorCollection.exportPath;
						string selectedPath = EditorUtility.OpenFolderPanel (labelTexturesFolder, currentPath, "");
						if (!string.IsNullOrEmpty (selectedPath)) {
							selectedPath = selectedPath.Substring (Application.dataPath.Length);
							if (selectedPath.CompareTo (branchDescriptorCollection.exportPath) != 0) {
								branchDescriptorCollection.exportPath = selectedPath;
								changed = true;
							}
						}
						GUIUtility.ExitGUI();
					}
					EditorGUILayout.EndHorizontal ();
					// Export prefix.
					exportPrefix = EditorGUILayout.TextField (labelPrefix, branchDescriptorCollection.exportPrefix);
					changed |= !exportPrefix.Equals (branchDescriptorCollection.exportPrefix);
					// Export take.
					exportTake = EditorGUILayout.IntField (labelTake, branchDescriptorCollection.exportTake);
					changed |= exportTake != branchDescriptorCollection.exportTake;
					string prefabPath = FileUtils.GetFilePath ("Assets" + branchDescriptorCollection.exportPath, branchDescriptorCollection.exportPrefix,
						"prefab", branchDescriptorCollection.exportTake);
					EditorGUILayout.HelpBox (prefabPath, MessageType.None);
					EditorGUILayout.Space ();

					// TEXTURES
					EditorGUILayout.LabelField (labelPrefabTextureSettings, BroccoEditorGUI.labelBold);
					// Atlas size.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelAtlasSize, BroccoEditorGUI.label);
					exportTextureSize = 
						(BranchDescriptorCollection.TextureSize)EditorGUILayout.EnumPopup (branchDescriptorCollection.exportTextureSize, GUILayout.Width (120));
					changed |= exportTextureSize != branchDescriptorCollection.exportTextureSize;
					EditorGUILayout.EndHorizontal ();
					// Atlas padding.
					paddingSize = EditorGUILayout.IntField (labelPadding, branchDescriptorCollection.exportAtlasPadding);
					if (paddingSize < 0 || paddingSize > 25) {
						paddingSize = branchDescriptorCollection.exportAtlasPadding;
					}
					changed |= paddingSize != branchDescriptorCollection.exportAtlasPadding;
					EditorGUILayout.Space ();

					// List of paths
					isAtlas = branchDescriptorCollection.exportMode == BranchDescriptorCollection.ExportMode.Atlas;
					subfolder = branchDescriptorCollection.exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
					albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, isAtlas);
					normalsPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, isAtlas);
					extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, isAtlas);
					subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, isAtlas);
					EditorGUILayout.HelpBox (albedoPath + "\n" + normalsPath + "\n" + extrasPath + "\n" + subsurfacePath, MessageType.None);
					
					// Export textures flags
					exportFlags = EditorGUILayout.MaskField(labelTextures, branchDescriptorCollection.exportTexturesFlags, exportTextureOptions);
					changed |= exportFlags != branchDescriptorCollection.exportTexturesFlags;
					if (changed) {
						branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
						onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
						branchDescriptorCollection.exportPrefix = exportPrefix;
						branchDescriptorCollection.exportTextureSize = exportTextureSize;
						branchDescriptorCollection.exportAtlasPadding = paddingSize;
						branchDescriptorCollection.exportTake = exportTake;
						branchDescriptorCollection.exportTexturesFlags = exportFlags;
						onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					}
					
					break;
				case EXPORT_TEXTURES: // Texture.
					EditorGUILayout.LabelField (labelTextureExportSettings, BroccoEditorGUI.labelBold);
					EditorGUILayout.HelpBox (MSG_EXPORT_TEXTURE, MessageType.None);
					if (GUILayout.Button (exportTexturesGUI)) {
						ExportTextures ();
					}
					EditorGUILayout.Space ();
					// Atlas size.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelAtlasSize, BroccoEditorGUI.label);
					exportTextureSize = 
						(BranchDescriptorCollection.TextureSize)EditorGUILayout.EnumPopup (branchDescriptorCollection.exportTextureSize, GUILayout.Width (120));
					changed |= exportTextureSize != branchDescriptorCollection.exportTextureSize;
					EditorGUILayout.EndHorizontal ();
					// Atlas padding.
					paddingSize = EditorGUILayout.IntField (labelPadding, branchDescriptorCollection.exportAtlasPadding);
					if (paddingSize < 0 || paddingSize > 25) {
						paddingSize = branchDescriptorCollection.exportAtlasPadding;			
					}
					changed |= paddingSize != branchDescriptorCollection.exportAtlasPadding;
					EditorGUILayout.Space ();
					// OUTPUT FILE
					EditorGUILayout.LabelField (labelOutputFile, BroccoEditorGUI.labelBold);
					// Export mode.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelExportMode, BroccoEditorGUI.label);
					exportMode = 
						(BranchDescriptorCollection.ExportMode)EditorGUILayout.EnumPopup (branchDescriptorCollection.exportMode, GUILayout.Width (120));
					changed |= exportMode != branchDescriptorCollection.exportMode;
					EditorGUILayout.EndHorizontal ();
					// Export take.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelTake, BroccoEditorGUI.label);
					exportTake = EditorGUILayout.IntField (branchDescriptorCollection.exportTake);
					changed |= exportTake != branchDescriptorCollection.exportTake;
					EditorGUILayout.EndHorizontal ();
					// Export path.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (labelPath, BroccoEditorGUI.label);
					EditorGUILayout.LabelField ("/Assets" + branchDescriptorCollection.exportPath);
					if (GUILayout.Button (selectPathGUI, GUILayout.Width (30))) {
						string currentPath = Application.dataPath + branchDescriptorCollection.exportPath;
						string selectedPath = EditorUtility.OpenFolderPanel (labelTexturesFolder, currentPath, "");
						if (!string.IsNullOrEmpty (selectedPath)) {
							selectedPath = selectedPath.Substring (Application.dataPath.Length);
							if (selectedPath.CompareTo (branchDescriptorCollection.exportPath) != 0) {
								branchDescriptorCollection.exportPath = selectedPath;
								changed = true;
							}
						}
						GUIUtility.ExitGUI();
					}
					EditorGUILayout.EndHorizontal ();
					// List of paths
					isAtlas = branchDescriptorCollection.exportMode == BranchDescriptorCollection.ExportMode.Atlas;
					subfolder = branchDescriptorCollection.exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
					albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, isAtlas);
					normalsPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, isAtlas);
					extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, isAtlas);
					subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
						branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, isAtlas);
					EditorGUILayout.HelpBox (albedoPath + "\n" + normalsPath + "\n" + extrasPath + "\n" + subsurfacePath, MessageType.None);
					// Export textures flags
					exportFlags = EditorGUILayout.MaskField(labelTextures, branchDescriptorCollection.exportTexturesFlags, exportTextureOptions);
					changed |= exportFlags != branchDescriptorCollection.exportTexturesFlags;
					if (changed) {
						branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
						onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
						branchDescriptorCollection.exportTextureSize = exportTextureSize;
						branchDescriptorCollection.exportAtlasPadding = paddingSize;
						branchDescriptorCollection.exportMode = exportMode;
						branchDescriptorCollection.exportTake = exportTake;
						branchDescriptorCollection.exportTexturesFlags = exportFlags;
						onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					}
					break;
			}
			EditorGUILayout.EndScrollView ();
			EditorGUILayout.EndHorizontal ();
		}
		#endregion

		#region Settings Panel
		/// <summary>
		/// Draw the settings panel window mode.
		/// </summary>
		public void DrawSettingsPanel () {
			// Calculate the rect.
            EditorGUILayout.LabelField (string.Empty);
            Rect offsetRect = GUILayoutUtility.GetLastRect ();
            float splitTop = verticalSplitView.GetCurrentSplitOffset ();
            Rect splitRect = verticalSplitView.currentRect;
            splitRect.Set (splitRect.x, splitTop + offsetRect.yMin, splitRect.width, splitRect.height - offsetRect.yMin);
			settingsPanel.SetRect (splitRect);

            if (settingsPanel.requiresRepaint) {
                settingsPanel.Repaint ();
            }
			return;
		}
		#endregion

		#region Debug
		/// <summary>
		/// Draw the debug panel window mode.
		/// </summary>
		public void DrawDebugPanel () {
			// Calculate the rect.
            EditorGUILayout.LabelField (string.Empty);
            Rect offsetRect = GUILayoutUtility.GetLastRect ();
            float splitTop = verticalSplitView.GetCurrentSplitOffset ();
            Rect splitRect = verticalSplitView.currentRect;
            splitRect.Set (splitRect.x, splitTop + offsetRect.yMin, splitRect.width, splitRect.height - offsetRect.yMin);
			debugPanel.SetRect (splitRect);

            if (debugPanel.requiresRepaint) {
                debugPanel.Repaint ();
            }
			return;

			
			
			/*
			showLODOptions = true;
			EditorGUILayout.BeginHorizontal ();
			// View Mode Selection.
			EditorGUILayout.BeginVertical ();
			EditorGUILayout.LabelField ("Debug", BroccoEditorGUI.labelBoldCentered);
			int _currentDebugView = GUILayout.SelectionGrid (currentDebugView, debugViewOptions, 1, GUILayout.Width (secondaryPanelColumnWidth));
			if (_currentDebugView != currentDebugView) {
				currentDebugView = _currentDebugView;
				
			}
			EditorGUILayout.EndVertical ();
			// Debugging Settings.
			debugPanelScroll = EditorGUILayout.BeginScrollView (debugPanelScroll, GUILayout.ExpandWidth (true));
			switch (currentDebugView) {
				case DEBUG_GEOMETRY: // GEOMETRY.
					string geoInfo = "Factory scale: " + sproutSubfactory.factoryScale;
					geoInfo += "\nLoaded from: " + debugLoadPath;
					EditorGUILayout.HelpBox (geoInfo, MessageType.None);
					debugPolyIndex = EditorGUILayout.Popup (debugPolyIndex, debugPolygonOptions, GUILayout.Width (secondaryPanelColumnWidth));
					EditorGUILayout.Space ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Geometry Debug", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					if (selectedSnapshot != null) {
						string snapshotInfo = $"Snapshot [id: {selectedSnapshot.id}], Curve [{selectedSnapshot.curve.nodeCount} nodes]";
						EditorGUILayout.HelpBox (snapshotInfo, MessageType.None);
					}

					// Buttons for debug processing.
					EditorGUILayout.BeginHorizontal ();
					if (GUILayout.Button ("Generate Topo Points")) {
						Debug.Log ("Generating Topo Points...");
					}
					EditorGUILayout.EndHorizontal ();

					// Flags for displaying gizmos.
					debugShowCurve = EditorGUILayout.Toggle ("Show Curve", debugShowCurve);
					debugShowTopoPoints = EditorGUILayout.Toggle ("Show Topo Points", debugShowTopoPoints);
					debugShowConvexHullPoints = EditorGUILayout.Toggle ("Show Convex Hull Points", debugShowConvexHullPoints);
					debugShowConvexHullPointsOrder = EditorGUILayout.Toggle ("Show Convex Hull Points Order", debugShowConvexHullPointsOrder);
					debugShowConvexHull = EditorGUILayout.Toggle ("Show Convex Hull", debugShowConvexHull);
					debugShowAABB = EditorGUILayout.Toggle ("Show AABB", debugShowAABB);
					debugShowOBB = EditorGUILayout.Toggle ("Show OBB", debugShowOBB);
					debugShowTris = EditorGUILayout.Toggle ("Show Tris", debugShowTris);
					//EditorGUILayout.LabelField ($"OBB angle: {_obbAngle}");
					bool _debugSkipSimplifyHull = EditorGUILayout.Toggle ("Skip Simplify Hull", debugSkipSimplifyHull);
					if (_debugSkipSimplifyHull != debugSkipSimplifyHull) {
						debugSkipSimplifyHull = _debugSkipSimplifyHull;
						RegenerateStructure ();
					}
					int _lods = EditorGUILayout.IntSlider ("LODs", selectedSnapshot.lodCount, 1, 3);
					if (_lods != selectedSnapshot.lodCount) {
						selectedSnapshot.lodCount = _lods;
					}
					break;
				case DEBUG_CANVAS: // CANVAS.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Canvas Debug", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.HelpBox (meshPreview.GetDebugInfo (), MessageType.None);
					//MeshPreview Target Rotation Offset.
					Vector3 rotationOffset = EditorGUILayout.Vector3Field ("Rotation Offset", meshPreview.targetRotationOffset.eulerAngles);
					if (rotationOffset != meshPreview.targetRotationOffset.eulerAngles) {
						meshPreview.targetRotationOffset.eulerAngles = rotationOffset;
					}
					//MeshPreview Target Position Offset.
					Vector3 positionOffset = EditorGUILayout.Vector3Field ("Position Offset", meshPreview.targetPositionOffset);
					if (positionOffset != meshPreview.targetPositionOffset) {
						meshPreview.targetPositionOffset = positionOffset;
					}
					// MeshPreview Light A.
					Light lightA = meshPreview.GetLightA ();
					float lightAIntensity = EditorGUILayout.FloatField ("Light A Intensity", lightA.intensity);
					if (lightA.intensity != lightAIntensity) {
						lightA.intensity = lightAIntensity;
					}
					Vector3 lightARotation = EditorGUILayout.Vector3Field ("Light A Rot", lightA.transform.rotation.eulerAngles);
					if (lightARotation != lightA.transform.rotation.eulerAngles) {
						lightA.transform.root.eulerAngles = lightARotation;
					}
					debugShow3DGizmo = EditorGUILayout.Toggle ("Show 3D Gizmo", debugShow3DGizmo);
					meshPreview.debugShowNormals = EditorGUILayout.Toggle ("Show Normals", meshPreview.debugShowNormals);
					meshPreview.debugShowTangents = EditorGUILayout.Toggle ("Show Tangents", meshPreview.debugShowTangents);
					MeshPreview.SecondPassBlend spb = (MeshPreview.SecondPassBlend)EditorGUILayout.EnumPopup ("Second Pass Blend", meshPreview.secondPassBlend);
                    if (spb != meshPreview.secondPassBlend) {
                        meshPreview.secondPassBlend = spb;
                    }
					break;
				case DEBUG_MESHING: // MESHING.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Meshing Debug", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					bool _debugShowMeshWireframe = EditorGUILayout.Toggle ("Show Wireframe", debugShowMeshWireframe);
					if (_debugShowMeshWireframe != debugShowMeshWireframe) {
						debugShowMeshWireframe = _debugShowMeshWireframe;
						meshPreview.showWireframe = debugShowMeshWireframe;
					}
					debugShowMeshNormals = EditorGUILayout.Toggle ("Show Normals", debugShowMeshNormals);
					debugShowMeshTangents = EditorGUILayout.Toggle ("Show Tangents", debugShowMeshTangents);
					EditorGUILayout.BeginHorizontal ();
					debugClearTargetId = EditorGUILayout.IntField ("Target Branch", debugClearTargetId);
					if (GUILayout.Button ("Set Alpha color to 0")) {
						Mesh m = sproutSubfactory.snapshotTreeMesh;
						Color[] colors = m.colors;
						List<Vector4> uv6 = new List<Vector4> ();
						m.GetUVs (5, uv6);
						for (int i = 0; i < colors.Length; i++) {
							if (uv6 [i].x == debugClearTargetId) {
								colors[i].a = 0;
							}
						}
						m.colors = colors;
					}
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					debugAtlas = (Texture2D)EditorGUILayout.ObjectField ("Atlas", debugAtlas, typeof(Texture2D), true);
					if (GUILayout.Button ("Show Atlased Mesh")) {
						// Regenerate the meshes for the polygons.
						var polys = sproutSubfactory.sproutCompositeManager.polygonAreas;
						var enumPolys = polys.GetEnumerator ();
						while (enumPolys.MoveNext ()) {
							Broccoli.Builder.PolygonAreaBuilder.SetPolygonAreaMesh (enumPolys.Current.Value);
						} 
						meshPreview.hasSecondPass = false;
						Mesh lodMesh = sproutSubfactory.sproutCompositeManager.GetMesh (selectedSnapshot.id, selectedLODView - 1, 0, false);
						Material[] mats = sproutSubfactory.sproutCompositeManager.GetMaterials (selectedSnapshot.id, selectedLODView - 1);
						Material uniqueMat = SproutCompositeManager.GenerateMaterial (Color.white, 0.3f, 0.1f, 0.1f, 0.5f, Color.white,
							debugAtlas, null, null, null);
						for (int i = 0; i < mats.Length; i++) {
							mats [i] = uniqueMat;
						}
						ShowPreviewMesh (lodMesh, mats);
					}
					EditorGUILayout.EndHorizontal ();
					break;
				case DEBUG_PROCESS: // PROCESS.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Processing", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					if (GUILayout.Button ("Generate Snapshot Texture")) {
						DebugTakeSnapshotTexture ();
					}
					debugRescale = EditorGUILayout.FloatField ("Rescale Factor", debugRescale);
					if (GUILayout.Button ("Rescale")) {
						DebugRescaleDescriptor (debugRescale);
					}
					if (MeshEditorDebug.Current ().DrawEditorProcessOptions (this))
						ShowPreviewMesh (MeshEditorDebug.Current ().mesh, MeshEditorDebug.Current ().material);
					break;
				case DEBUG_BUILDER: // BUILDERS.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Builder", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					if (MeshEditorDebug.Current ().DrawEditorBuildOptions (meshPreview))
						ShowPreviewMesh (MeshEditorDebug.Current ().mesh, MeshEditorDebug.Current ().material);
					break;
				case DEBUG_SNAPSHOT: // SNAPSHOT.
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Snapshot", BroccoEditorGUI.labelBoldCentered);
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
					if (selectedSnapshot != null) {
						EditorGUILayout.HelpBox (selectedSnapshot.GetDebugInfo (), MessageType.None);
					}
					MeshEditorDebug.Current ().DrawEditorSnapshotMesh (this);
					EditorGUILayout.Space ();
					if (GUILayout.Button ("Add Main Snapshot")) {
						AddSnapshot ("Main");
					}
					if (GUILayout.Button ("Add Spike Snapshpt")) {
						AddSnapshot ("Spike");
					}
					break;
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.EndScrollView ();
			*/
		}
		private void DebugRescaleDescriptor (float scale) {
			// For each snapshot.
			BranchDescriptor snapshot;
			for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
				snapshot = branchDescriptorCollection.snapshots [i];
				// Scale girths.
				snapshot.girthAtBase *= scale;
				snapshot.girthAtTop *= scale;
				// Scale Sprouts.
				snapshot.sproutASize *= scale;
				snapshot.sproutBSize *= scale;
				// Scale level descriptors.
				BranchDescriptor.BranchLevelDescriptor branchLevel;
				for (int j = 0; j < snapshot.branchLevelDescriptors.Count; j++) {
					branchLevel = snapshot.branchLevelDescriptors [j];
					branchLevel.minLengthAtBase *= scale;
					branchLevel.maxLengthAtBase *= scale;
					branchLevel.minLengthAtTop *= scale;
					branchLevel.maxLengthAtTop *= scale;
				}
			}
		}
		private void DebugTakeSnapshotTexture () {
			Texture2D texture = null;
			GameObject targetGO =  sproutSubfactory.treeFactory.previewTree.obj;
			MeshRenderer meshRenderer = targetGO.GetComponent<MeshRenderer> ();
			MeshFilter meshFilter = targetGO.GetComponent<MeshFilter> ();
			Bounds bounds = meshFilter.sharedMesh.bounds;
			// Prepare texture builder according to the material mode.
			Broccoli.Builder.TextureBuilder tb = new Broccoli.Builder.TextureBuilder ();
			// Set background to transparent.
			tb.backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
			tb.textureFormat = TextureFormat.ARGB32;
			//tb.useTextureSizeToTargetRatio = true;
			tb.BeginUsage (targetGO);
			tb.textureSize = new Vector2 (256, 256);
			int steps = 12;
			float angleStep = 180f / (float)steps;
			for (int i = 0; i < steps; i++) {
				Vector3 planeDirection = Quaternion.AngleAxis (i * angleStep, Vector3.up) * Vector3.right;
				texture = tb.GetTexture (new Plane (planeDirection, Vector3.zero), Vector3.up, bounds, "Assets/sl_thumb_" + (i<10?"0":"") + i + ".png");
			}
			tb.EndUsage ();
		}
		#endregion

		#region Undo
		public void OnUndoRedo () {
			if (canvasStructureView == CanvasStructureView.Snapshot) {
				SelectSnapshot (branchDescriptorCollection.snapshotIndex);
			} else {
				SelectVariation (branchDescriptorCollection.variationIndex);
			}
			mappingPanel?.OnUndoRedo ();
			settingsPanel?.OnUndoRedo ();
			debugPanel?.OnUndoRedo ();
			if (_currentImplementation != null) _currentImplementation.OnUndoRedo ();
		}
		void CopyToProxyBranchLevelDescriptor () {
			proxyBranchLevelDescriptor.minFrequency = selectedBranchLevelDescriptor.minFrequency;
			proxyBranchLevelDescriptor.maxFrequency = selectedBranchLevelDescriptor.maxFrequency;
			proxyBranchLevelDescriptor.minRange = selectedBranchLevelDescriptor.minRange;
			proxyBranchLevelDescriptor.maxRange = selectedBranchLevelDescriptor.maxRange;
			proxyBranchLevelDescriptor.distributionCurve = selectedBranchLevelDescriptor.distributionCurve;
			proxyBranchLevelDescriptor.radius = selectedBranchLevelDescriptor.radius;
			proxyBranchLevelDescriptor.minLengthAtBase = selectedBranchLevelDescriptor.minLengthAtBase;
			proxyBranchLevelDescriptor.maxLengthAtBase = selectedBranchLevelDescriptor.maxLengthAtBase;
			proxyBranchLevelDescriptor.minLengthAtTop = selectedBranchLevelDescriptor.minLengthAtTop;
			proxyBranchLevelDescriptor.maxLengthAtTop = selectedBranchLevelDescriptor.maxLengthAtTop;
			proxyBranchLevelDescriptor.lengthCurve = selectedBranchLevelDescriptor.lengthCurve;
			proxyBranchLevelDescriptor.minParallelAlignAtTop = selectedBranchLevelDescriptor.minParallelAlignAtTop;
			proxyBranchLevelDescriptor.maxParallelAlignAtTop = selectedBranchLevelDescriptor.maxParallelAlignAtTop;
			proxyBranchLevelDescriptor.minParallelAlignAtBase = selectedBranchLevelDescriptor.minParallelAlignAtBase;
			proxyBranchLevelDescriptor.maxParallelAlignAtBase = selectedBranchLevelDescriptor.maxParallelAlignAtBase;
			proxyBranchLevelDescriptor.minGravityAlignAtTop = selectedBranchLevelDescriptor.minGravityAlignAtTop;
			proxyBranchLevelDescriptor.maxGravityAlignAtTop = selectedBranchLevelDescriptor.maxGravityAlignAtTop;
			proxyBranchLevelDescriptor.minGravityAlignAtBase = selectedBranchLevelDescriptor.minGravityAlignAtBase;
			proxyBranchLevelDescriptor.maxGravityAlignAtBase = selectedBranchLevelDescriptor.maxGravityAlignAtBase;
			proxyBranchLevelDescriptor.minPlaneAlignAtTop = selectedBranchLevelDescriptor.minPlaneAlignAtTop;
			proxyBranchLevelDescriptor.maxPlaneAlignAtTop = selectedBranchLevelDescriptor.maxPlaneAlignAtTop;
			proxyBranchLevelDescriptor.minPlaneAlignAtBase = selectedBranchLevelDescriptor.minPlaneAlignAtBase;
			proxyBranchLevelDescriptor.maxPlaneAlignAtBase = selectedBranchLevelDescriptor.maxPlaneAlignAtBase;
		}
		void CopyFromProxyBranchLevelDescriptor () {
			if (selectedBranchLevelDescriptor != null) {
				selectedBranchLevelDescriptor.minFrequency = proxyBranchLevelDescriptor.minFrequency;
				selectedBranchLevelDescriptor.maxFrequency = proxyBranchLevelDescriptor.maxFrequency;
				selectedBranchLevelDescriptor.minRange = proxyBranchLevelDescriptor.minRange; 
				selectedBranchLevelDescriptor.maxRange = proxyBranchLevelDescriptor.maxRange;
				selectedBranchLevelDescriptor.distributionCurve = proxyBranchLevelDescriptor.distributionCurve;
				selectedBranchLevelDescriptor.radius = proxyBranchLevelDescriptor.radius;
				selectedBranchLevelDescriptor.minLengthAtBase = proxyBranchLevelDescriptor.minLengthAtBase;
				selectedBranchLevelDescriptor.maxLengthAtBase = proxyBranchLevelDescriptor.maxLengthAtBase;
				selectedBranchLevelDescriptor.minLengthAtTop = proxyBranchLevelDescriptor.minLengthAtTop;
				selectedBranchLevelDescriptor.maxLengthAtTop = proxyBranchLevelDescriptor.maxLengthAtTop;
				selectedBranchLevelDescriptor.lengthCurve = proxyBranchLevelDescriptor.lengthCurve;
				selectedBranchLevelDescriptor.spacingVariance = proxyBranchLevelDescriptor.spacingVariance;
				selectedBranchLevelDescriptor.minParallelAlignAtTop = proxyBranchLevelDescriptor.minParallelAlignAtTop;
				selectedBranchLevelDescriptor.maxParallelAlignAtTop = proxyBranchLevelDescriptor.maxParallelAlignAtTop;
				selectedBranchLevelDescriptor.minParallelAlignAtBase = proxyBranchLevelDescriptor.minParallelAlignAtBase;
				selectedBranchLevelDescriptor.maxParallelAlignAtBase = proxyBranchLevelDescriptor.maxParallelAlignAtBase;
				selectedBranchLevelDescriptor.minGravityAlignAtTop = proxyBranchLevelDescriptor.minGravityAlignAtTop;
				selectedBranchLevelDescriptor.maxGravityAlignAtTop = proxyBranchLevelDescriptor.maxGravityAlignAtTop;
				selectedBranchLevelDescriptor.minGravityAlignAtBase = proxyBranchLevelDescriptor.minGravityAlignAtBase;
				selectedBranchLevelDescriptor.maxGravityAlignAtBase = proxyBranchLevelDescriptor.maxGravityAlignAtBase;
				selectedBranchLevelDescriptor.minPlaneAlignAtTop = proxyBranchLevelDescriptor.minPlaneAlignAtTop;
				selectedBranchLevelDescriptor.maxPlaneAlignAtTop = proxyBranchLevelDescriptor.maxPlaneAlignAtTop;
				selectedBranchLevelDescriptor.minPlaneAlignAtBase = proxyBranchLevelDescriptor.minPlaneAlignAtBase;
				selectedBranchLevelDescriptor.maxPlaneAlignAtBase = proxyBranchLevelDescriptor.maxPlaneAlignAtBase;
			}
		}
		void CopyToProxySproutALevelDescriptor () {
			proxySproutALevelDescriptor.isEnabled = selectedSproutALevelDescriptor.isEnabled;
			proxySproutALevelDescriptor.minFrequency = selectedSproutALevelDescriptor.minFrequency;
			proxySproutALevelDescriptor.maxFrequency = selectedSproutALevelDescriptor.maxFrequency;
			proxySproutALevelDescriptor.minParallelAlignAtTop = selectedSproutALevelDescriptor.minParallelAlignAtTop;
			proxySproutALevelDescriptor.maxParallelAlignAtTop = selectedSproutALevelDescriptor.maxParallelAlignAtTop;
			proxySproutALevelDescriptor.minParallelAlignAtBase = selectedSproutALevelDescriptor.minParallelAlignAtBase;
			proxySproutALevelDescriptor.maxParallelAlignAtBase = selectedSproutALevelDescriptor.maxParallelAlignAtBase;
			proxySproutALevelDescriptor.minGravityAlignAtTop = selectedSproutALevelDescriptor.minGravityAlignAtTop;
			proxySproutALevelDescriptor.maxGravityAlignAtTop = selectedSproutALevelDescriptor.maxGravityAlignAtTop;
			proxySproutALevelDescriptor.minGravityAlignAtBase = selectedSproutALevelDescriptor.minGravityAlignAtBase;
			proxySproutALevelDescriptor.maxGravityAlignAtBase = selectedSproutALevelDescriptor.maxGravityAlignAtBase;
			proxySproutALevelDescriptor.minRange = selectedSproutALevelDescriptor.minRange;
			proxySproutALevelDescriptor.maxRange = selectedSproutALevelDescriptor.maxRange;
			proxySproutALevelDescriptor.distribution = selectedSproutALevelDescriptor.distribution;
			proxySproutALevelDescriptor.distributionCurve = selectedSproutALevelDescriptor.distributionCurve;
			proxySproutALevelDescriptor.spacingVariance = selectedSproutALevelDescriptor.spacingVariance;
		}
		void CopyFromProxySproutALevelDescriptor () {
			if (selectedSproutALevelDescriptor != null) {
				selectedSproutALevelDescriptor.isEnabled = proxySproutALevelDescriptor.isEnabled;
				selectedSproutALevelDescriptor.minFrequency = proxySproutALevelDescriptor.minFrequency;
				selectedSproutALevelDescriptor.maxFrequency = proxySproutALevelDescriptor.maxFrequency;
				selectedSproutALevelDescriptor.minParallelAlignAtTop = proxySproutALevelDescriptor.minParallelAlignAtTop;
				selectedSproutALevelDescriptor.maxParallelAlignAtTop = proxySproutALevelDescriptor.maxParallelAlignAtTop;
				selectedSproutALevelDescriptor.minParallelAlignAtBase = proxySproutALevelDescriptor.minParallelAlignAtBase;
				selectedSproutALevelDescriptor.maxParallelAlignAtBase = proxySproutALevelDescriptor.maxParallelAlignAtBase;
				selectedSproutALevelDescriptor.minGravityAlignAtTop = proxySproutALevelDescriptor.minGravityAlignAtTop;
				selectedSproutALevelDescriptor.maxGravityAlignAtTop = proxySproutALevelDescriptor.maxGravityAlignAtTop;
				selectedSproutALevelDescriptor.minGravityAlignAtBase = proxySproutALevelDescriptor.minGravityAlignAtBase;
				selectedSproutALevelDescriptor.maxGravityAlignAtBase = proxySproutALevelDescriptor.maxGravityAlignAtBase;
				selectedSproutALevelDescriptor.minRange = proxySproutALevelDescriptor.minRange;
				selectedSproutALevelDescriptor.maxRange = proxySproutALevelDescriptor.maxRange;
				selectedSproutALevelDescriptor.distribution = proxySproutALevelDescriptor.distribution;
				selectedSproutALevelDescriptor.distributionCurve = proxySproutALevelDescriptor.distributionCurve;
				selectedSproutALevelDescriptor.spacingVariance = proxySproutALevelDescriptor.spacingVariance;
			}
		}
		void CopyToProxySproutBLevelDescriptor () {
			proxySproutBLevelDescriptor.isEnabled = selectedSproutBLevelDescriptor.isEnabled;
			proxySproutBLevelDescriptor.minFrequency = selectedSproutBLevelDescriptor.minFrequency;
			proxySproutBLevelDescriptor.maxFrequency = selectedSproutBLevelDescriptor.maxFrequency;
			proxySproutBLevelDescriptor.minParallelAlignAtTop = selectedSproutBLevelDescriptor.minParallelAlignAtTop;
			proxySproutBLevelDescriptor.maxParallelAlignAtTop = selectedSproutBLevelDescriptor.maxParallelAlignAtTop;
			proxySproutBLevelDescriptor.minParallelAlignAtBase = selectedSproutBLevelDescriptor.minParallelAlignAtBase;
			proxySproutBLevelDescriptor.maxParallelAlignAtBase = selectedSproutBLevelDescriptor.maxParallelAlignAtBase;
			proxySproutBLevelDescriptor.minGravityAlignAtTop = selectedSproutBLevelDescriptor.minGravityAlignAtTop;
			proxySproutBLevelDescriptor.maxGravityAlignAtTop = selectedSproutBLevelDescriptor.maxGravityAlignAtTop;
			proxySproutBLevelDescriptor.minGravityAlignAtBase = selectedSproutBLevelDescriptor.minGravityAlignAtBase;
			proxySproutBLevelDescriptor.maxGravityAlignAtBase = selectedSproutBLevelDescriptor.maxGravityAlignAtBase;
			proxySproutBLevelDescriptor.minRange = selectedSproutBLevelDescriptor.minRange;
			proxySproutBLevelDescriptor.maxRange = selectedSproutBLevelDescriptor.maxRange;
			proxySproutBLevelDescriptor.distribution = selectedSproutBLevelDescriptor.distribution;
			proxySproutBLevelDescriptor.distributionCurve = selectedSproutBLevelDescriptor.distributionCurve;
			proxySproutBLevelDescriptor.spacingVariance = selectedSproutBLevelDescriptor.spacingVariance;
		}
		void CopyFromProxySproutBLevelDescriptor () {
			if (selectedSproutBLevelDescriptor != null) {
				selectedSproutBLevelDescriptor.isEnabled = proxySproutBLevelDescriptor.isEnabled;
				selectedSproutBLevelDescriptor.minFrequency = proxySproutBLevelDescriptor.minFrequency;
				selectedSproutBLevelDescriptor.maxFrequency = proxySproutBLevelDescriptor.maxFrequency;
				selectedSproutBLevelDescriptor.minParallelAlignAtTop = proxySproutBLevelDescriptor.minParallelAlignAtTop;
				selectedSproutBLevelDescriptor.maxParallelAlignAtTop = proxySproutBLevelDescriptor.maxParallelAlignAtTop;
				selectedSproutBLevelDescriptor.minParallelAlignAtBase = proxySproutBLevelDescriptor.minParallelAlignAtBase;
				selectedSproutBLevelDescriptor.maxParallelAlignAtBase = proxySproutBLevelDescriptor.maxParallelAlignAtBase;
				selectedSproutBLevelDescriptor.minGravityAlignAtTop = proxySproutBLevelDescriptor.minGravityAlignAtTop;
				selectedSproutBLevelDescriptor.maxGravityAlignAtTop = proxySproutBLevelDescriptor.maxGravityAlignAtTop;
				selectedSproutBLevelDescriptor.minGravityAlignAtBase = proxySproutBLevelDescriptor.minGravityAlignAtBase;
				selectedSproutBLevelDescriptor.maxGravityAlignAtBase = proxySproutBLevelDescriptor.maxGravityAlignAtBase;
				selectedSproutBLevelDescriptor.minRange = proxySproutBLevelDescriptor.minRange;
				selectedSproutBLevelDescriptor.maxRange = proxySproutBLevelDescriptor.maxRange;
				selectedSproutBLevelDescriptor.distribution = proxySproutBLevelDescriptor.distribution;
				selectedSproutBLevelDescriptor.distributionCurve = proxySproutBLevelDescriptor.distributionCurve;
				selectedSproutBLevelDescriptor.spacingVariance = proxySproutBLevelDescriptor.spacingVariance;
			}
		}
		void CopyToProxySproutMap () {
			if (selectedSproutMapArea != null) {
				proxySproutMap.texture = selectedSproutMapArea.texture;
				proxySproutMap.normalMap = selectedSproutMapArea.normalMap;
				proxySproutMap.extraMap = selectedSproutMapArea.extraMap;
				proxySproutMap.subsurfaceMap = selectedSproutMapArea.subsurfaceMap;
			}
		}
		void CopyFromProxySproutMap () {
			selectedSproutMapArea.texture = proxySproutMap.texture;
			selectedSproutMapArea.normalMap = proxySproutMap.normalMap;
			selectedSproutMapArea.extraMap = proxySproutMap.extraMap;
			selectedSproutMapArea.subsurfaceMap = proxySproutMap.subsurfaceMap;
		}
		#endregion

		#region Snapshots
		/// <summary>
		/// Initializes the view options for the snapshots present in the loaded collection.
		/// </summary>
		void InitSnapshotViewOptions () {
			// Build GUIContents per branch.
			snapshotViewOptions = new GUIContent[branchDescriptorCollection.snapshots.Count];
			for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
				snapshotViewOptions [i] = new GUIContent ("S" + i);
			}
		}
		/// <summary>
		/// Draws the list of snapshots on this collection.
		/// </summary>
		void DrawSnapshotsPanel () {
			EditorGUILayout.BeginHorizontal ();
			Rect tmpCtrlRect = EditorGUILayout.GetControlRect(GUILayout.Width (3));
			tmpCtrlRect.width = 3;
			if (canvasStructureView == CanvasStructureView.Snapshot) {
				EditorGUI.DrawRect (tmpCtrlRect, Color.green);
			} else {
				EditorGUI.DrawRect (tmpCtrlRect, Color.gray);
			}
			EditorGUI.BeginChangeCheck ();
			if (branchDescriptorCollection.snapshots.Count > 0) {
				branchDescriptorCollection.snapshotIndex = GUILayout.Toolbar (branchDescriptorCollection.snapshotIndex, snapshotViewOptions);
			} else {
				EditorGUILayout.HelpBox (MSG_EMPTY_SNAPSHOTS, MessageType.None, true);
			}
			if (EditorGUI.EndChangeCheck ()) {
				SelectSnapshot (branchDescriptorCollection.snapshotIndex);
				ReflectChangesToPipeline ();
				RegenerateStructure (false, currentMapView);
			}
			GUILayout.FlexibleSpace ();
			if (selectedSnapshot != null) {
				if (GUILayout.Button (cloneSnapshotGUI)) {
					AddSnapshot (string.Empty, true);
				}
			}
			if (GUILayout.Button (addSnapshotGUI)) {
				if (!_currentImplementation.OnEditorAddSnapshotClicked ()) {
					AddSnapshot ("Main");
				}
			}
			EditorGUI.BeginDisabledGroup (branchDescriptorCollection.snapshotIndex < 0);
			if (GUILayout.Button ("..")) {
				Vector2 mousePos = Event.current.mousePosition;
				mousePos.y += verticalSplitView.GetCurrentSplitOffset ();
				snapshotOptionsPopup.Open (mousePos);
			}
			EditorGUILayout.EndHorizontal ();
		}
		/// <summary>
		/// Adds a snapshot to the loaded collection.
		/// </summary>
		public void AddSnapshot (string snapshotType, bool clone = false) {
			if (branchDescriptorCollection.snapshots.Count < SNAPSHOT_ADD_LIMIT) {
				BranchDescriptor newBranchDescriptor;
				if (clone && selectedSnapshot != null) {
					newBranchDescriptor = selectedSnapshot.Clone ();
				} else {
					newBranchDescriptor = new BranchDescriptor ();
					newBranchDescriptor.snapshotType = snapshotType;
					this.sproutSubfactory.PipelineToSnapshot (newBranchDescriptor);
				}
				branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				branchDescriptorCollection.AddSnapshot (newBranchDescriptor);
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				InitSnapshotViewOptions ();
				SelectSnapshot (branchDescriptorCollection.snapshots.Count - 1);
				ReflectChangesToPipeline ();
				RegenerateStructure ();
			} else {
				Debug.LogWarning ("Snapshots limit reached: " + SNAPSHOT_ADD_LIMIT);
			}
		}
		/// <summary>
		/// Removes the selected snapshot in the loaded collection.
		/// </summary>
		void RemoveSnapshot () {
			if (EditorUtility.DisplayDialog (MSG_DELETE_BRANCH_DESC_TITLE, 
				MSG_DELETE_BRANCH_DESC_MESSAGE, 
				MSG_DELETE_BRANCH_DESC_OK, 
				MSG_DELETE_BRANCH_DESC_CANCEL)) {
				branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				branchDescriptorCollection.RemoveSnapshot (selectedSnapshot.id);
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				SelectSnapshot (0);
				ReflectChangesToPipeline ();
				RegenerateStructure ();
				snapshotOptionsPopup.Close ();
				GUIUtility.ExitGUI ();
			}
		}
		/// <summary>
		/// Moves the selected snapshot to the left or right of the list.
		/// </summary>
		/// <param name="direction">Positive for right and negative for left.</param>
		void MoveSnapshot (int direction) {
			if (selectedSnapshot != null) {
				// Move Left
				int snapI = branchDescriptorCollection.snapshotIndex;
				if (direction < 0 && snapI > 0) {
					BranchDescriptor snap = branchDescriptorCollection.snapshots [snapI];
					branchDescriptorCollection.snapshots [snapI] = branchDescriptorCollection.snapshots [snapI - 1];
					branchDescriptorCollection.snapshots [snapI - 1] = snap;
					SelectSnapshot (snapI - 1);
				}
				// Move Right
				else if (direction > 0 && snapI < branchDescriptorCollection.snapshots.Count - 1) {
					BranchDescriptor snap = branchDescriptorCollection.snapshots [snapI];
					branchDescriptorCollection.snapshots [snapI] = branchDescriptorCollection.snapshots [snapI + 1];
					branchDescriptorCollection.snapshots [snapI + 1] = snap;
					SelectSnapshot (snapI + 1);
				}
			}
		}
		/// <summary>
		/// Selects a snapshot in the loaded collection.
		/// </summary>
		/// <param name="index">Index of the snapshot to select.</param>
		public void SelectSnapshot (int index) {
			SetSecondLevelView (CanvasStructureView.Snapshot);
			ShowVariationInScene (false);
			mappingPanel.Initialize (this);
			mappingPanel.SproutSelected ();
			settingsPanel.Initialize (this);
			debugPanel.Initialize (this);
			SetCanvasSettings (_currentImplementation.GetCanvasSettings (currentPanelSection, 0));
			InitSnapshotViewOptions ();
			if (branchDescriptorCollection.snapshots.Count > 0 && index >= 0 && index < branchDescriptorCollection.snapshots.Count) {
				branchDescriptorCollection.snapshotIndex = index;
				this.selectedSnapshot = branchDescriptorCollection.snapshots [index];
				this.snapSettings = SnapshotSettings.Get (this.selectedSnapshot.snapshotType);
				this.sproutSubfactory.snapshotIndex = index;
				meshPreview.previewTitle = _currentImplementation.GetPreviewTitle (branchDescriptorCollection.descriptorImplId);
				meshPreview.showTrisCount = false;
			} else {
				branchDescriptorCollection.snapshotIndex = -1;
				this.selectedSnapshot = null;
				this.snapSettings = SnapshotSettings.Get ();
				this.sproutSubfactory.snapshotIndex = -1;
			}
			ApplySnapshotSettings ();
			if (_currentImplementation != null) {
				_currentImplementation.SnapshotSelected (branchDescriptorCollection.snapshotIndex);
			}
			InitLODViewOptions ();
		}
		private void ApplySnapshotSettings () {
			GUIContent[] _structureViewOptions;
			if (snapSettings.hasCrown) {
				_structureViewOptions = new GUIContent[4];
			} else {
				_structureViewOptions = new GUIContent[3];
			}
			for (int i = 0; i < 3; i++) {
				_structureViewOptions [i] = structureViewOptions [i];
			}
			if (snapSettings.hasCrown) {
				_structureViewOptions [3] = new GUIContent (snapSettings.crownName, snapSettings.crownTooltip);
			}
			structureViewOptions = _structureViewOptions; 
		}
		#endregion

		#region Variations
		/// <summary>
		/// Initializes the view options for the variations present in the loaded collection.
		/// </summary>
		void InitVariationViewOptions () {
			// Build GUIContents per variation.
			variationViewOptions = new GUIContent[branchDescriptorCollection.variations.Count];
			for (int i = 0; i < branchDescriptorCollection.variations.Count; i++) {
				variationViewOptions [i] = new GUIContent ("Var" + i);
			}
		}
		/// <summary>
		/// Draws the list of variations on this collection.
		/// </summary>
		void DrawVariationsPanel () {
			EditorGUILayout.BeginHorizontal ();
			Rect tmpCtrlRect = EditorGUILayout.GetControlRect(GUILayout.Width (3));
			tmpCtrlRect.width = 3;
			if (canvasStructureView == CanvasStructureView.Variation) {
				EditorGUI.DrawRect (tmpCtrlRect, Color.green);
			} else {
				EditorGUI.DrawRect (tmpCtrlRect, Color.gray);
			}
			EditorGUI.BeginChangeCheck ();
			if (branchDescriptorCollection.variations.Count > 0) {
				branchDescriptorCollection.variationIndex = GUILayout.Toolbar (branchDescriptorCollection.variationIndex, variationViewOptions);
			} else {
				EditorGUILayout.HelpBox (MSG_EMPTY_VARIATIONS, MessageType.None, true);
			}
			if (EditorGUI.EndChangeCheck ()) {
				SelectVariation (branchDescriptorCollection.variationIndex);
			}
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button (addVariationGUI)) {
				AddVariation ();
			}
			EditorGUI.BeginDisabledGroup (branchDescriptorCollection.variationIndex < 0);
			if (GUILayout.Button ("..")) {
				Vector2 mousePos = Event.current.mousePosition;
				mousePos.y += verticalSplitView.GetCurrentSplitOffset ();
				variationOptionsPopup.Open (mousePos);
			}
			EditorGUI.EndDisabledGroup ();
			EditorGUILayout.EndHorizontal ();
		}
		/// <summary>
		/// Adds a variation to the loaded collection.
		/// </summary>
		void AddVariation () {
			if (branchDescriptorCollection.variations.Count < VARIATION_ADD_LIMIT) {
				VariationDescriptor newVariationDescriptor;
				if (selectedVariation != null) {
					newVariationDescriptor = selectedVariation.Clone ();
				} else {
					newVariationDescriptor = new VariationDescriptor ();
				}
				branchDescriptorCollection.lastVariationIndex = branchDescriptorCollection.variationIndex;
				TriggerOnBeforeEditVariationDescriptor (newVariationDescriptor);
				branchDescriptorCollection.AddVariation (newVariationDescriptor);
				TriggerOnEditVariationDescriptor (newVariationDescriptor);
				InitVariationViewOptions ();
				SelectVariation (branchDescriptorCollection.variations.Count - 1);
			} else {
				Debug.LogWarning ("Variations limit reached: " + VARIATION_ADD_LIMIT);
			}
		}
		/// <summary>
		/// Move the variation in the list, positive for right and negative for left.
		/// </summary>
		void MoveVariation (int direction) {
			if (selectedVariation != null) {
				// Move Left
				int varI = branchDescriptorCollection.variationIndex;
				if (direction < 0 && varI > 0) {
					VariationDescriptor variation = branchDescriptorCollection.variations [varI];
					branchDescriptorCollection.variations [varI] = branchDescriptorCollection.variations [varI - 1];
					branchDescriptorCollection.variations [varI - 1] = variation;
					SelectVariation (varI - 1);
				}
				// Move Right
				else if (direction > 0 && varI < branchDescriptorCollection.variations.Count - 1) {
					VariationDescriptor variation = branchDescriptorCollection.variations [varI];
					branchDescriptorCollection.variations [varI] = branchDescriptorCollection.variations [varI + 1];
					branchDescriptorCollection.variations [varI + 1] = variation;
					SelectVariation (varI + 1);
				}
			}
		}
		/// <summary>
		/// Removes the selected variation in the loaded collection.
		/// </summary>
		void RemoveVariation () {
			if (EditorUtility.DisplayDialog (MSG_DELETE_VARIATION_DESC_TITLE, 
				MSG_DELETE_VARIATION_DESC_MESSAGE, 
				MSG_DELETE_VARIATION_DESC_OK, 
				MSG_DELETE_VARIATION_DESC_CANCEL)) {
				branchDescriptorCollection.lastVariationIndex = branchDescriptorCollection.variationIndex;
				TriggerOnBeforeEditVariationDescriptor (selectedVariation);
				branchDescriptorCollection.RemoveVariation (selectedVariation);
				SelectVariation (0);
				TriggerOnEditVariationDescriptor (selectedVariation);
				variationOptionsPopup.Close ();
				GUIUtility.ExitGUI ();
			}
		}
		/// <summary>
		/// Selects a variation in the loaded collection.
		/// </summary>
		/// <param name="index">Index of the variation to select.</param>
		public void SelectVariation (int index) {
			SetSecondLevelView (CanvasStructureView.Variation);
			ShowVariationInScene (false);
			SetCanvasSettings (_currentImplementation.GetCanvasSettings (_currentPanelSection, 0));
			SetMapView (VIEW_COMPOSITE);
			InitVariationViewOptions ();
			if (branchDescriptorCollection.variations.Count > 0 && index >= 0 && index < branchDescriptorCollection.variations.Count) {
				branchDescriptorCollection.variationIndex = index;
				this.selectedVariation = branchDescriptorCollection.variations [index];
				this.selectedVariation.BuildGroupTree ();
				this.sproutSubfactory.variationIndex = index;
				meshPreview.previewTitle = _currentImplementation.GetPreviewTitle (branchDescriptorCollection.descriptorImplId);
				meshPreview.showTrisCount = true;
			} else {
				branchDescriptorCollection.variationIndex = -1;
				this.selectedVariation = null;
				this.sproutSubfactory.variationIndex = -1;
			}
			if (_currentImplementation != null) {
				_currentImplementation.VariationSelected (branchDescriptorCollection.variationIndex);
			}
		}
		#endregion

		#region LODs
		/// <summary>
		/// Initializes the view options for the LODs present in the loaded collection.
		/// </summary>
		void InitLODViewOptions () {
			if (selectedSnapshot != null) {
				int lodCount = selectedSnapshot.lodCount + 1;
				lodViewOptions = new GUIContent[lodCount];
				lodViewOptions [0] = new GUIContent ("#");
				for (int i = 1; i < lodCount; i++) {
					lodViewOptions [i] = new GUIContent ("LOD" + (i - 1));
				}
				selectedLODView = 0;
			}
		}
		#endregion

		#region Mesh Preview
		/// <summary>
		/// Shows a mesh preview according to the selected snapshot.
		/// </summary>
		public void ShowPreviewMesh (int viewMode = VIEW_COMPOSITE) {
			if (sproutSubfactory == null) return;

			// SNAPSHOT.
			if (canvasStructureView == CanvasStructureView.Snapshot) {
				SetMapView (viewMode, true);
				// Gets the shared mesh on the MeshFilter component on the sprout subfactory.
				Mesh mesh = sproutSubfactory.treeFactory.previewTree.obj.GetComponent<MeshFilter> ().sharedMesh;
				Material material = new Material(Shader.Find ("Standard"));

				meshPreview.Clear ();
				meshPreview.CreateViewport ();
				mesh.RecalculateBounds();
				if (material != null) {
					meshPreview.AddMesh (0, mesh, material, true);
				} else {
					meshPreview.AddMesh (0, mesh, true);
				}
			}
			// VARIATION.
			else {
				meshPreview.Clear ();
				meshPreview.CreateViewport ();
			}
			// VARIATION.
			selectedLODView = 0;
		}
		/// <summary>
		/// Shows a mesh set as preview.
		/// </summary>
		/// <param name="previewMesh"></param>
		public void ShowPreviewMesh (Mesh previewMesh, Material material = null) {
			if (previewMesh == null) return;
			if (material == null) {
				material = new Material(Shader.Find ("Standard"));
			}
			meshPreview.Clear ();
			meshPreview.CreateViewport ();
			previewMesh.RecalculateBounds ();
			currentPreviewMaterials = new Material[previewMesh.subMeshCount];
			for (int i = 0; i < previewMesh.subMeshCount; i++) {
				currentPreviewMaterials [i] = material;	
			}
			meshPreview.AddMesh (0, previewMesh, material, true);
		}
		/// <summary>
		/// Shows a mesh set as preview.
		/// </summary>
		/// <param name="previewMesh"></param>
		public void ShowPreviewMesh (Mesh previewMesh, Material[] materials) {
			if (previewMesh == null) return;
			meshPreview.Clear ();
			meshPreview.CreateViewport ();
			if (previewMesh.vertexCount > 0)
				previewMesh.RecalculateBounds ();
			currentPreviewMaterials = materials;
			meshPreview.AddMesh (0, previewMesh, true);
		}
		/// <summary>
		/// Draw additional handles on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
		public void OnPreviewMeshDrawHandles (Rect r, Camera camera) {
			if (showLightControls) {
				Handles.color = Color.yellow;
				Handles.ArrowHandleCap (0,
					//Vector3.zero, 
					meshPreview.GetLightA ().transform.rotation * Vector3.back * 1.5f,
					meshPreview.GetLightA ().transform.rotation, 
					1f * MeshPreview.GetHandleSize (Vector3.zero, camera), 
					EventType.Repaint); 
			}
			if (debugShow3DGizmo) {
				Handles.color = Color.green;
				Handles.ArrowHandleCap (0, debug3DGizmoOffset, 
					Quaternion.LookRotation (Vector3.up), 
					1f * MeshPreview.GetHandleSize (debug3DGizmoOffset, camera), EventType.Repaint);
				Handles.color = Color.red;
				Handles.ArrowHandleCap (0, debug3DGizmoOffset, 
					Quaternion.LookRotation (Vector3.right), 
					1f * MeshPreview.GetHandleSize (debug3DGizmoOffset, camera), EventType.Repaint);
				Handles.color = Color.blue;
				Handles.ArrowHandleCap (0, debug3DGizmoOffset, 
					Quaternion.LookRotation (Vector3.forward), 
					1f * MeshPreview.GetHandleSize (debug3DGizmoOffset, camera), EventType.Repaint);
			}
			if (_currentImplementation != null) {
				_currentImplementation.OnCanvasDrawHandles (r, camera);
			}
			// SNAPSHOT.
			if (canvasStructureView == CanvasStructureView.Snapshot && selectedSnapshot != null) {
				if (snapSettings.stemRadiusEnabled) {
					Handles.color = Color.yellow;
					Handles.DrawWireDisc (Vector3.zero, Vector3.up, 
						selectedSnapshot.branchLevelDescriptors [0].radius);
				}
				bool showDebugOptions = false;
				#if BROCCOLI_DEVEL
				showDebugOptions = true;
				#endif
				if (showDebugOptions && Event.current.type == EventType.Repaint) {
					if (_currentPanelSection == PANEL_DEBUG) debugPanel.OnDrawHandles (r, camera);
					List<PolygonArea> polygonAreas = 
						sproutSubfactory.sproutCompositeManager.GetPolygonAreas (selectedSnapshot.id, selectedLODView - 1, 0, true);
					// Draw debugging for each polygon area.
					PolygonArea _polygonArea;				
					for (int pI = 0; pI < polygonAreas.Count; pI++) {
						if (debugPolyIndex == 0 || pI == (debugPolyIndex - 1)) {
							_polygonArea = polygonAreas [pI];
							#if BROCCOLI_DEVEL
							if (debugShowCurve) {
								BezierCurveDraw.DrawCurve (selectedSnapshot.curve, Vector3.zero, sproutSubfactory.factoryScale, Color.yellow, 5f);
								BezierCurveDraw.DrawCurveNodes (selectedSnapshot.curve, Vector3.zero, sproutSubfactory.factoryScale, Color.yellow);	
							}
							if (debugShowTopoPoints && _polygonArea != null) {
								Handles.color = Color.white;
								float handleSize;
								float handleSizeScale = 0.045f;
								for (int i = 0; i < _polygonArea.topoPoints.Count; i++) {
									handleSize = HandleUtility.GetHandleSize (_polygonArea.topoPoints[i]) * handleSizeScale;
									Handles.DotHandleCap (-1, _polygonArea.topoPoints [i], 
										Quaternion.identity, handleSize, EventType.Repaint);
								}
							}
							#endif
							if (debugShowConvexHull && _polygonArea != null) {
								Handles.color = Color.yellow;
								int i = 0;
								for (i = 0; i < _polygonArea.lastConvexPointIndex; i++) {
									Handles.DrawLine (_polygonArea.points [i], _polygonArea.points [i + 1]);
								}
								Handles.DrawLine (_polygonArea.points [0], _polygonArea.points [_polygonArea.lastConvexPointIndex]);
							}
							if (debugShowConvexHullPoints && _polygonArea != null) {
								float handleSize;
								float handleSizeScale = 0.045f;
								Handles.color = Color.yellow;
								for (int i = 0; i <= _polygonArea.lastConvexPointIndex; i++) {
									if (debugShowConvexHullPointsOrder) {
										float step = (float)i / (_polygonArea.lastConvexPointIndex == 0?1:_polygonArea.lastConvexPointIndex + 1);
										handleSize = HandleUtility.GetHandleSize (_polygonArea.points [i]) * handleSizeScale * Mathf.Lerp (1f, 2f, step);
										Handles.color = Color.Lerp (Color.yellow, Color.red, step * 0.65f);
									} else {
										handleSize = HandleUtility.GetHandleSize (_polygonArea.points[i]) * handleSizeScale;
									}
									Handles.DotHandleCap (-1, _polygonArea.points [i], Quaternion.identity, handleSize, EventType.Repaint);
								}
								List<Vector3> anglesPos = GeometryAnalyzer.Current ().debugAnglePos;
								List<float> angles = GeometryAnalyzer.Current ().debugAngles;
								float scale = sproutSubfactory.factoryScale;
								Vector3 rPos = Vector3.zero;
								Matrix4x4 formerMatrix = Handles.matrix;
								Handles.matrix = Matrix4x4.identity;
								for (int i = 0; i < angles.Count; i++) {
									Handles.Label (anglesPos [i], i + ": " + angles [i]);
									Handles.DrawLine (rPos, anglesPos [i]);
									rPos = anglesPos [i];
								}
								Handles.matrix = formerMatrix;
							}
							if (debugShowAABB) {
								Handles.color = Color.white;
								Vector3 topLeft = _polygonArea.aabb.min;
								topLeft.y = _polygonArea.aabb.max.y;
								Handles.DrawLine (_polygonArea.aabb.min, topLeft);
								Handles.DrawLine (topLeft, _polygonArea.aabb.max);
								Vector3 bottomRight = _polygonArea.aabb.max;
								bottomRight.y = _polygonArea.aabb.min.y;
								Handles.DrawLine (_polygonArea.aabb.max, bottomRight);
								Handles.DrawLine (bottomRight, _polygonArea.aabb.min);
							}
							if (debugShowOBB) {
								Handles.color = Color.white;
								Vector3 topLeft = _polygonArea.obb.min;
								topLeft.y = _polygonArea.obb.max.y;
								Handles.DrawLine (_polygonArea.obb.min, topLeft);
								Handles.DrawLine (topLeft, _polygonArea.obb.max);
								Vector3 bottomRight = _polygonArea.obb.max;
								bottomRight.y = _polygonArea.obb.min.y;
								Handles.DrawLine (_polygonArea.obb.max, bottomRight);
								Handles.DrawLine (bottomRight, _polygonArea.obb.min);
							}
							if (debugShowTris && _polygonArea != null && _polygonArea.mesh != null) {
								int[] tris = _polygonArea.mesh.triangles;
								Vector3[] vert = _polygonArea.mesh.vertices;
								Handles.color = Color.white;
								for (int i = 0; i < tris.Length; i = i + 3) {
									Handles.DrawLine (vert [tris [i]], vert [tris [i + 1]]);
									Handles.DrawLine (vert [tris [i + 1]], vert [tris [i + 2]]);
									Handles.DrawLine (vert [tris [i + 2]], vert [tris [i]]);
								}
							}
							if (debugShowMeshNormals  && _polygonArea != null && _polygonArea.mesh != null) {
								Vector3[] _vertices = _polygonArea.mesh.vertices;
								Vector3[] _normals = _polygonArea.mesh.normals;
								Handles.color = Color.yellow;
								for (int i = 0; i < _vertices.Length; i++) {
									Handles.DrawLine (_vertices [i], _vertices [i] + _normals [i]);
								}
							}
							if (debugShowMeshTangents  && _polygonArea != null && _polygonArea.mesh != null) {
								Vector3[] _vertices = _polygonArea.mesh.vertices;
								Vector4[] _tangents = _polygonArea.mesh.tangents;
								Vector3 _tan;
								Handles.color = Color.Lerp (Color.red, Color.white, 0.6f);
								for (int i = 0; i < _vertices.Length; i++) {
									_tan = (Vector3)_tangents [i];
									Handles.DrawLine (_vertices [i], _vertices [i] + _tan);
								}
							}
							if (_polygonArea != null) {
								GeometryAnalyzer ga = GeometryAnalyzer.Current ();
								Handles.color = Color.white;
								float scale = sproutSubfactory.factoryScale;
								Handles.color = Color.white;
								for (int i = 0; i < ga.debugCombinedPoly.Count - 1; i ++) {
									Handles.DrawLine (ga.debugCombinedPoly [i] * scale, ga.debugCombinedPoly [i + 1] * scale);
								}
							}
						}
					}
				}
			}
			// VARIATION.
			else if (canvasStructureView == CanvasStructureView.Variation && selectedVariation != null) {
			}
		}
		/// <summary>
		/// Draws GUI elements on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
		public void OnPreviewMeshDrawGUI (Rect r, Camera camera) {
			canvasSideButtonsIndex = 0;
			if (showViewControls && currentCanvasSettings.showViewModes) {
				DrawViewControls (r, canvasSideButtonsIndex);
				canvasSideButtonsIndex++;
			}
			if (showLightControls) {
				DrawLightControls (r, canvasSideButtonsIndex);
				canvasSideButtonsIndex++;
			}
			DrawLODViewControls (r);
			if (showProgressBar) {
				EditorGUI.ProgressBar(new Rect(0, 0, r.width, EditorGUIUtility.singleLineHeight), 
					progressBarProgress, progressBarTitle);
			}
		}
		/// <summary>
		/// Called when the mesh preview requires repaint.
		/// </summary>
		void OnMeshPreviewRequiresRepaint () {
			if (SproutFactoryEditorWindow.editorWindow != null)
				SproutFactoryEditorWindow.editorWindow.Repaint ();
		}
		/// <summary>
		/// Draws the controls to swith view mode.
		/// </summary>
		/// <param name="r"></param>
		public void DrawViewControls (Rect r, int optionsIndex) {
			r.x = 4;
			r.y = r.height - (EditorGUIUtility.singleLineHeight + 5) * (optionsIndex  + 1);
			r.height = EditorGUIUtility.singleLineHeight;
			r.width = 115;
			if (GUI.Button (r, "View: " + viewModeDisplayStr)) {
				Vector2 mousePos = Event.current.mousePosition;
				mousePos.y += verticalSplitView.GetCurrentSplitOffset ();
				viewModePopup.Open (mousePos);
			}
		}
		/// <summary>
		/// Draws the control to rotate the lights.
		/// </summary>
		/// <param name="r"></param>
		public void DrawLightControls (Rect r, int optionsIndex) {
			r.x = 4;
			r.y = r.height - (EditorGUIUtility.singleLineHeight + 5) * (optionsIndex  + 1);
			r.height = EditorGUIUtility.singleLineHeight;
			r.width = 115;
			if (GUI.Button (r, "Light: " + lightAngleDisplayStr)) {
				AddLightStep ();
			}
		}
		/// <summary>
		/// Draws the controls to switch between mesh LOD views.
		/// </summary>
		/// <param name="r"></param>
		public void DrawLODViewControls (Rect r) {
			bool drawLOD = true;
			if (_currentImplementation != null) {
				drawLOD = _currentImplementation.DrawLODControls (r);
			}
			if (!drawLOD || !showLODOptions) return;
			r.x = (r.width / 2f) - (lodViewOptions.Length * 45f / 2f);
			r.y = r.height - 4 - EditorGUIUtility.singleLineHeight;
			r.height = EditorGUIUtility.singleLineHeight;
			r.width = lodViewOptions.Length * 45f;
			int _selectedLODView = GUI.SelectionGrid (r, selectedLODView, lodViewOptions, lodViewOptions.Length);
			if (_selectedLODView != selectedLODView) {
				selectedLODView = _selectedLODView;
				if (selectedLODView == 0) {
					ShowPreviewMesh ();
					meshPreview.showTrisCount = false;
				} else {
					#if BROCCOLI_DEVEL
					sproutSubfactory.simplifyHullEnabled =!debugSkipSimplifyHull;
					#endif
					SetMapView (VIEW_COMPOSITE);
					sproutSubfactory.ProcessSnapshotPolygons (selectedSnapshot);
					meshPreview.hasSecondPass = false;
					meshPreview.showTrisCount = true;
					Mesh lodMesh = sproutSubfactory.sproutCompositeManager.GetMesh (selectedSnapshot.id, selectedLODView - 1);
					Material[] mats = sproutSubfactory.sproutCompositeManager.GetMaterials (selectedSnapshot.id, selectedLODView - 1);
					ShowPreviewMesh (lodMesh, mats);
				}
			}
		}
		public void AddLightStep () {
			if (lightAngleToAddTimeTmp <= 0) {
				SetEditorDeltaTime ();
				lightAngleToAddTimeTmp = lightAngleToAddTime;
				lightAngleEulerFrom = meshPreview.GetLightA ().transform.rotation.eulerAngles;
				lightAngleEulerTo = lightAngleEulerFrom;
				lightAngleEulerTo.y += lightAngleStepValue;
				lightAngleStep++;
				if (lightAngleStep >= 8) lightAngleStep = 0;
				switch (lightAngleStep) {
					case 0: lightAngleDisplayStr = "Front";
						break;
					case 1:  lightAngleDisplayStr = "Left 45";
						break;
					case 2:  lightAngleDisplayStr = "Left";
						break;
					case 3:  lightAngleDisplayStr = "Left -45";
						break;
					case 4:  lightAngleDisplayStr = "Back";
						break;
					case 5:  lightAngleDisplayStr = "Right -45";
						break;
					case 6:  lightAngleDisplayStr = "Right";
						break;
					case 7:  lightAngleDisplayStr = "Right 45";
						break;
				}
			}
		}
		public void SetCanvasSettings (CanvasSettings canvasSettings) {
			if (currentCanvasSettings == null || currentCanvasSettings.id != canvasSettings.id) {
				// Set the current canvas settings.
				if (canvasSettings == null) currentCanvasSettings = defaultCanvasSettings;
				else currentCanvasSettings = canvasSettings;

				// Set mesh preview controls and helpers.
				meshPreview.freeViewEnabled = currentCanvasSettings.freeViewEnabled;
				if (currentCanvasSettings.showPlane && meshPreview.planeTexture == null) {
					// Load plane texture.
					int textureIndex = GUITextureManager.LoadCustomTexture ("broccoli_plane.png");
					if (textureIndex >= 0) {
						meshPreview.planeTexture = GUITextureManager.GetCustomTexture (textureIndex);
					}
				}
				meshPreview.ShowPlaneMesh (currentCanvasSettings.showPlane, 
					currentCanvasSettings.planeSize, Vector3.zero);
				meshPreview.showRuler = currentCanvasSettings.showRuler;
				meshPreview.showAxis = currentCanvasSettings.showGizmos;

				bool resetZoom = currentCanvasSettings.resetZoom;
				float defaultZoomFactor = currentCanvasSettings.defaultZoomFactor;
				meshPreview.minZoomFactor = currentCanvasSettings.minZoomFactor;
				if (meshPreview.GetZoom () < currentCanvasSettings.minZoomFactor) {
					defaultZoomFactor = currentCanvasSettings.minZoomFactor;
					resetZoom = true;
				}
				meshPreview.maxZoomFactor = currentCanvasSettings.maxZoomFactor;
				if (meshPreview.GetZoom () > currentCanvasSettings.maxZoomFactor) {
					defaultZoomFactor = currentCanvasSettings.maxZoomFactor;
					resetZoom = true;
				}
				if (resetZoom) {
					SetEditorDeltaTime ();
					zoomTransitionEnabled = true;
					cameraTransitionZoom = defaultZoomFactor;
					cameraTransitionZoomTmp = meshPreview.GetZoom ();
				} else {
					zoomTransitionEnabled = false;
				}				

				// Transition to the new view settings.
				if (currentCanvasSettings.resetView) {
					SetEditorDeltaTime ();
					viewTransitionEnabled = true;
					cameraTransitionDirection = currentCanvasSettings.viewDirection;
					cameraTransitionOffset = currentCanvasSettings.viewOffset;
					cameraTransitionTargetRotation = currentCanvasSettings.viewTargetRotation;
					cameraTransitionTimeTmp = cameraTransitionTime;
					cameraTransitionDirectionTmp = meshPreview.GetDirection ();
					cameraTransitionOffsetTmp = meshPreview.GetOffset ();
					cameraTransitionTargetRotationTmp = meshPreview.GetTargetRotation ();
				} else {
					viewTransitionEnabled = false;
				}
			}
		}
		void SetStructureSettings (StructureSettings structureSettings) {
			if (currentStructureSettings == null || currentStructureSettings.id != structureSettings.id) {
				// Set the current structure settings.
				if (structureSettings == null) currentStructureSettings = defaultStructureSettings;
				else currentStructureSettings = structureSettings;
				levelOptions = new string[] {"Main " + currentStructureSettings.branchEntityName, 
					"One Level", "Two Levels", "Three Levels"};
				structureViewOptions [0] = 
					new GUIContent (currentStructureSettings.branchEntitiesName, "Settings for branches.");
			}
		}
		#endregion

		#region Persistence
		/// <summary>
		/// Creates a new branch descriptor collection.
		/// </summary>
		private void OnCreateNewBranchDescriptorCollectionSO () {}
		/// <summary>
		/// Loads a BanchDescriptorCollection from a file.
		/// </summary>
		/// <param name="loadedBranchDescriptorCollection">Branch collection loaded.</param>
		/// <param name="pathToFile">Path to file.</param>
		private void OnLoadBranchDescriptorCollectionSO (BranchDescriptorCollectionSO loadedBranchDescriptorCollectionSO, string pathToFile) {
			onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);

			LoadBranchDescriptorCollection (loadedBranchDescriptorCollectionSO.branchDescriptorCollection.Clone (), sproutSubfactory, pathToFile);
			onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
		}
		/// <summary>
		/// Gets the branch descriptor collection to save when the user requests it.
		/// </summary>
		/// <returns>Object to save.</returns>
		private BranchDescriptorCollectionSO OnGetBranchDescriptorCollectionSOToSave () { 
			BranchDescriptorCollectionSO toSave = ScriptableObject.CreateInstance<BranchDescriptorCollectionSO> ();
			toSave.branchDescriptorCollection = branchDescriptorCollection;
			return toSave;
		}
		/// <summary>
		/// Gets the path to file when the user requests it.
		/// </summary>
		/// <returns>The path to file or empty string if not has been set.</returns>
		private string OnGetBranchDescriptorCollectionSOToSaveFilePath () {
			return "";
		}
		/// <summary>
		/// Receives the object just saved.
		/// </summary>
		/// <param name="branchDescriptorCollectionSO">Saved object.</param>
		/// <param name="pathToFile">Path to file.</param>
		private void OnSaveBranchDescriptorCollectionSO (BranchDescriptorCollectionSO branchDescriptorCollectionSO, string pathToFile) {
			//LoadBranchDescriptorCollection (branchDescriptorCollectionSO.branchDescriptorCollection, sproutSubfactory);
		}
		#endregion

		#region Sprout Map List
		/// <summary>
		/// Inits the sprout map list.
		/// </summary>
		private void InitSproutMapLists () {
			// Sprout A Map List.
			sproutAMapList = new ReorderableList (branchDescriptorCollection.sproutStyleA.sproutMapAreas,
					typeof (SproutMap.SproutMapArea), false, true, true, true);
			sproutAMapList.draggable = false;
			sproutAMapList.drawHeaderCallback += DrawSproutMapListHeader;
			sproutAMapList.drawElementCallback += DrawSproutAMapListItemElement;
			sproutAMapList.onAddCallback += AddSproutAMapListItem;
			sproutAMapList.onRemoveCallback += RemoveSproutAMapListItem;
			// Sprout B Map List.
			sproutBMapList = new ReorderableList (branchDescriptorCollection.sproutStyleB.sproutMapAreas,
					typeof (SproutMap.SproutMapArea), false, true, true, true);
			sproutBMapList.draggable = false;
			sproutBMapList.drawHeaderCallback += DrawSproutMapListHeader;
			sproutBMapList.drawElementCallback += DrawSproutBMapListItemElement;
			sproutBMapList.onAddCallback += AddSproutBMapListItem;
			sproutBMapList.onRemoveCallback += RemoveSproutBMapListItem;
			// Crown Map List.
			crownMapList = new ReorderableList (branchDescriptorCollection.sproutStyleCrown.sproutMapAreas, 
					typeof (SproutMap.SproutMapArea), false, true, true, true);
			crownMapList.draggable = false;
			crownMapList.drawHeaderCallback += DrawSproutMapListHeader;
			crownMapList.drawElementCallback += DrawCrownMapListItemElement;
			crownMapList.onAddCallback += AddCrownMapListItem;
			crownMapList.onRemoveCallback += RemoveCrownMapListItem;
		}
		/// <summary>
		/// Draws the sprout map list header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawSproutMapListHeader (Rect rect) {
			GUI.Label(rect, "Sprout Maps", BroccoEditorGUI.labelBoldCentered);
		}
		/// <summary>
		/// Draws each sprout map list item element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawSproutAMapListItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			SproutMap.SproutMapArea sproutMapArea = branchDescriptorCollection.sproutStyleA.sproutMapAreas [index];
			if (sproutMapArea != null) {
				GUI.Label (new Rect (rect.x, rect.y, 150, EditorGUIUtility.singleLineHeight + 5), 
					"Textures for Leaf Type " + (index + 1));
				if (isActive) {
					if (selectedSproutMapArea != branchDescriptorCollection.sproutStyleA.sproutMapAreas [index]) {
						selectedSproutMapArea = branchDescriptorCollection.sproutStyleA.sproutMapAreas [index];
						selectedSproutMapGroup = 0;
						selectedSproutMapAreaIndex = index;
						textureCanvas.SetAreaRectAndPivot (1, selectedSproutMapArea.rect, selectedSproutMapArea.pivot);
					}
					CopyToProxySproutMap ();
					EditorGUILayout.BeginVertical (GUILayout.Width (200));
					EditorGUI.BeginChangeCheck ();
					proxySproutMap.texture = (Texture2D) EditorGUILayout.ObjectField ("Main Texture", proxySproutMap.texture, typeof (Texture2D), false);
					proxySproutMap.normalMap = (Texture2D) EditorGUILayout.ObjectField ("Normal Texture", proxySproutMap.normalMap, typeof (Texture2D), false);
					EditorGUILayout.EndVertical ();
					/*
					branchDescriptorCollection.sproutStyleA.sproutMapAlphas [selectedSproutMapIndex] = 
						EditorGUILayout.Slider ("Alpha Factor", branchDescriptorCollection.sproutStyleA.sproutMapAlphas [selectedSproutMapIndex], 0f, 1f);
						*/
					if (EditorGUI.EndChangeCheck ()) {
						//WaitProcessTexture (sproutSubfactory.GetSproutTextureId (0, index), branchDescriptorCollection.sproutStyleA.sproutMapAlphas [selectedSproutMapIndex]);
						sproutSubfactory.ProcessTexture (proxySproutMap.texture, 0, index);
						sproutMapChanged = true;
					}
				}
			}
		}
		/// <summary>
		/// Adds the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void AddSproutAMapListItem (ReorderableList list) {
			branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
			onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
			if (branchDescriptorCollection.sproutStyleA.sproutMapAreas.Count < 10) {
				SproutMap.SproutMapArea sproutMapArea= new SproutMap.SproutMapArea ();
				branchDescriptorCollection.sproutStyleA.sproutMapAreas.Add (sproutMapArea);
				branchDescriptorCollection.sproutStyleA.sproutMapAlphas.Add (1f);
			}
			onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
		}
		/// <summary>
		/// Removes the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void RemoveSproutAMapListItem (ReorderableList list) {
			SproutMap.SproutMapArea sproutMap = branchDescriptorCollection.sproutStyleA.sproutMapAreas [list.index];
			if (sproutMap != null) {
				if (EditorUtility.DisplayDialog (MSG_DELETE_SPROUT_MAP_TITLE, 
					MSG_DELETE_SPROUT_MAP_MESSAGE, 
					MSG_DELETE_SPROUT_MAP_OK, 
					MSG_DELETE_SPROUT_MAP_CANCEL)) {
					branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
					onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					branchDescriptorCollection.sproutStyleA.sproutMapAreas.RemoveAt (list.index);
					ReflectChangesToPipeline ();
					selectedSproutMapArea = null;
					onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				}
			}
		}
		/// <summary>
		/// Draws each sprout map list item element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawSproutBMapListItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			SproutMap.SproutMapArea sproutMapArea = branchDescriptorCollection.sproutStyleB.sproutMapAreas [index];
			if (sproutMapArea != null) {
				GUI.Label (new Rect (rect.x, rect.y, 150, EditorGUIUtility.singleLineHeight + 5), 
					"Textures for Leaf Type " + (index + 1));
				if (isActive) {
					if (selectedSproutMapArea != branchDescriptorCollection.sproutStyleB.sproutMapAreas [index]) {
						selectedSproutMapArea = branchDescriptorCollection.sproutStyleB.sproutMapAreas [index];
						selectedSproutMapGroup = 1;
						selectedSproutMapAreaIndex = index;
						textureCanvas.SetAreaRectAndPivot (1, selectedSproutMapArea.rect, selectedSproutMapArea.pivot);
					}
					CopyToProxySproutMap ();
					EditorGUILayout.BeginVertical (GUILayout.Width (200));
					EditorGUI.BeginChangeCheck ();
					proxySproutMap.texture = (Texture2D) EditorGUILayout.ObjectField ("Main Texture", proxySproutMap.texture, typeof (Texture2D), false);
					proxySproutMap.normalMap = (Texture2D) EditorGUILayout.ObjectField ("Normal Texture", proxySproutMap.normalMap, typeof (Texture2D), false);
					EditorGUILayout.EndVertical ();
					/*
					branchDescriptorCollection.sproutStyleB.sproutMapAlphas [selectedSproutMapIndex] = 
						EditorGUILayout.Slider ("Alpha Factor", branchDescriptorCollection.sproutStyleB.sproutMapAlphas [selectedSproutMapIndex], 0f, 1f);
						*/
					if (EditorGUI.EndChangeCheck ()) {
						//WaitProcessTexture (sproutSubfactory.GetSproutTextureId (1, index), branchDescriptorCollection.sproutStyleB.sproutMapAlphas [selectedSproutMapIndex]);
						sproutSubfactory.ProcessTexture (proxySproutMap.texture, 1, index);
						sproutMapChanged = true;
					}
				}
			}
		}
		/// <summary>
		/// Adds the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void AddSproutBMapListItem (ReorderableList list) {
			branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
			onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
			if (branchDescriptorCollection.sproutStyleB.sproutMapAreas.Count < 10) {
				SproutMap.SproutMapArea sproutMapArea= new SproutMap.SproutMapArea ();
				branchDescriptorCollection.sproutStyleB.sproutMapAreas.Add (sproutMapArea);
				branchDescriptorCollection.sproutStyleB.sproutMapAlphas.Add (1f);
			}
			onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
		}
		/// <summary>
		/// Removes the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void RemoveSproutBMapListItem (ReorderableList list) {
			SproutMap.SproutMapArea sproutMap = branchDescriptorCollection.sproutStyleB.sproutMapAreas [list.index];
			if (sproutMap != null) {
				if (EditorUtility.DisplayDialog (MSG_DELETE_SPROUT_MAP_TITLE, 
					MSG_DELETE_SPROUT_MAP_MESSAGE, 
					MSG_DELETE_SPROUT_MAP_OK, 
					MSG_DELETE_SPROUT_MAP_CANCEL)) {
					branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
					onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					branchDescriptorCollection.sproutStyleB.sproutMapAreas.RemoveAt (list.index);
					ReflectChangesToPipeline ();
					selectedSproutMapArea = null;
					onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				}
			}
		}
		/// <summary>
		/// Draws each sprout map list item element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawCrownMapListItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			SproutMap.SproutMapArea sproutMapArea = branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [index];
			if (sproutMapArea != null) {
				GUI.Label (new Rect (rect.x, rect.y, 150, EditorGUIUtility.singleLineHeight + 5), 
					"Textures for Leaf Type " + (index + 1));
				if (isActive) {
					if (selectedSproutMapArea != branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [index]) {
						selectedSproutMapArea = branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [index];
						selectedSproutMapGroup = 2;
						selectedSproutMapAreaIndex = index;
						textureCanvas.SetAreaRectAndPivot (1, selectedSproutMapArea.rect, selectedSproutMapArea.pivot);
					}
					CopyToProxySproutMap ();
					EditorGUILayout.BeginVertical (GUILayout.Width (200));
					EditorGUI.BeginChangeCheck ();
					proxySproutMap.texture = (Texture2D) EditorGUILayout.ObjectField ("Main Texture", proxySproutMap.texture, typeof (Texture2D), false);
					proxySproutMap.normalMap = (Texture2D) EditorGUILayout.ObjectField ("Normal Texture", proxySproutMap.normalMap, typeof (Texture2D), false);
					EditorGUILayout.EndVertical ();
					/*
					branchDescriptorCollection.sproutStyleCrown.sproutMapAlphas [selectedSproutMapIndex] = 
						EditorGUILayout.Slider ("Alpha Factor", branchDescriptorCollection.sproutStyleCrown.sproutMapAlphas [selectedSproutMapIndex], 0f, 1f);
						*/
					if (EditorGUI.EndChangeCheck ()) {
						//WaitProcessTexture (sproutSubfactory.GetSproutTextureId (2, index), branchDescriptorCollection.sproutStyleCrown.sproutMapAlphas [selectedSproutMapIndex]);
						sproutSubfactory.ProcessTexture (proxySproutMap.texture, 2, index);
						sproutMapChanged = true;
					}
				}
			}
		}
		/// <summary>
		/// Adds the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void AddCrownMapListItem (ReorderableList list) {
			branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
			onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
			if (branchDescriptorCollection.sproutStyleCrown.sproutMapAreas.Count < 10) {
				SproutMap.SproutMapArea sproutMapArea= new SproutMap.SproutMapArea ();
				branchDescriptorCollection.sproutStyleCrown.sproutMapAreas.Add (sproutMapArea);
				branchDescriptorCollection.sproutStyleCrown.sproutMapAlphas.Add (1f);
			}
			onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
		}
		/// <summary>
		/// Removes the sprout map list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void RemoveCrownMapListItem (ReorderableList list) {
			SproutMap.SproutMapArea sproutMap = branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [list.index];
			if (sproutMap != null) {
				if (EditorUtility.DisplayDialog (MSG_DELETE_SPROUT_MAP_TITLE, 
					MSG_DELETE_SPROUT_MAP_MESSAGE, 
					MSG_DELETE_SPROUT_MAP_OK, 
					MSG_DELETE_SPROUT_MAP_CANCEL)) {
					branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
					onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
					branchDescriptorCollection.sproutStyleCrown.sproutMapAreas.RemoveAt (list.index);
					ReflectChangesToPipeline ();
					selectedSproutMapArea = null;
					onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				}
			}
		}
		#endregion
		
		#region Export Process
		/// <summary>
		/// Exports the collection to a BranchDescriptorSO.
		/// </summary>
		/// <param name="exportAtlas"></param>
		void ExportDescriptorWithAtlas (bool exportAtlas) {
			// Get file path to save to.
			string savePath = editorPersistence.GetSavePath ();

			if (!string.IsNullOrEmpty (savePath)) {
				sproutSubfactory.ProcessSnapshots ();
				string albedoPath;
				string normalsPath;
				string extrasPath;
				string subsurfacePath;
				bool done = ExportTexturesFromPolygons (branchDescriptorCollection.exportPrefix, out albedoPath, out normalsPath, out extrasPath, out subsurfacePath);
				// Exporting the branch descriptor.
				if (done) {
					BranchDescriptorCollectionSO bdSO = ScriptableObject.CreateInstance<BranchDescriptorCollectionSO> ();
					bdSO.branchDescriptorCollection = branchDescriptorCollection;
					AssetDatabase.Refresh ();
					editorPersistence.SaveElementToFile (bdSO, savePath);
					onShowNotification?.Invoke ("Branch Descriptor Saved at: " + savePath);
					bdSO = AssetDatabase.LoadAssetAtPath<BranchDescriptorCollectionSO> (savePath);
				}
			}

			GUIUtility.ExitGUI ();
		}
		void ExportPrefab () {
			AssetManager assetManager = new AssetManager ();
			sproutSubfactory.ProcessSnapshotPolygons (selectedSnapshot);
			Mesh lodMesh = sproutSubfactory.sproutCompositeManager.GetMesh (selectedSnapshot.id, 0);
			Material[] mats = sproutSubfactory.sproutCompositeManager.GetMaterials (selectedSnapshot.id, 0);
		}
		public bool ExportTexturesFromPolygons (
			string exportPrefix, 
			out string albedoPath, 
			out string normalPath, 
			out string extrasPath, 
			out string subsurfacePath)
		{
			// Create the atlas from polygons.
			string basePath = "Assets" + branchDescriptorCollection.exportPath;
			string subfolder = exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
			bool subfolderCreated = FileUtils.CreateSubfolder ("Assets" + branchDescriptorCollection.exportPath, subfolder);
			albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, true);
			normalPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, true);
			extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, true);
			subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, true);
			string compositePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Composite, true);
			bool done = sproutSubfactory.GenerateAtlasTextureFromPolygons (
				branchDescriptorCollection,
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				branchDescriptorCollection.exportAtlasPadding,
				albedoPath, normalPath, extrasPath, subsurfacePath, compositePath);
			if (done) {
				onShowNotification?.Invoke ("Atlas textures Saved at: " + basePath);
			}
			return done;
		}
		void ExportTextures () {
			// Generate Snapshot Texture
			if (branchDescriptorCollection.exportMode == BranchDescriptorCollection.ExportMode.SelectedSnapshot) {
				ExportTexturesSingleSnapshot ();
			} else {
				// Generate atlas texture.
				ExportTexturesAtlas (branchDescriptorCollection.exportPrefix);
			}
		}
		void ExportTexturesSingleSnapshot () {
			int index = branchDescriptorCollection.snapshotIndex;
			string basePath = "Assets" + branchDescriptorCollection.exportPath;
			string subfolder = branchDescriptorCollection.exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
			bool subfolderCreated = FileUtils.CreateSubfolder ("Assets" + branchDescriptorCollection.exportPath, subfolder);
			if (!subfolderCreated) return;
			string albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, false);
			string normalPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, false);
			string extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, false);
			string subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, false);
			string compositePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Composite, false);
			bool done = sproutSubfactory.GenerateSnapshopTextures (
				branchDescriptorCollection.snapshotIndex,
				branchDescriptorCollection,
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				albedoPath, normalPath, extrasPath, subsurfacePath, compositePath);
			if (done) {
				onShowNotification?.Invoke ("Textures for Snapshot S" + index + " saved at: \n" + basePath);
			}
		}
		public void ExportTexturesAtlas (string exportPrefix) {
			string basePath = "Assets" + branchDescriptorCollection.exportPath;
			
			string subfolder = exportPrefix + FileUtils.GetFileSuffix (branchDescriptorCollection.exportTake);
			bool subfolderCreated = FileUtils.CreateSubfolder ("Assets" + branchDescriptorCollection.exportPath, subfolder);
			if (!subfolderCreated) return;
			string albedoPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Albedo, true);
			string normalPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Normals, true);
			string extrasPath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Extras, true);
			string subsurfacePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Subsurface, true);
			string compositePath = SproutSubfactory.GetTextureFileName (branchDescriptorCollection.exportPath, subfolder, branchDescriptorCollection.exportPrefix, 
				branchDescriptorCollection.exportTake, SproutSubfactory.MaterialMode.Composite, true);
			bool done = sproutSubfactory.GenerateAtlasTexture (
				branchDescriptorCollection,
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				GetTextureSize (branchDescriptorCollection.exportTextureSize),
				branchDescriptorCollection.exportAtlasPadding,
				albedoPath, normalPath, extrasPath, subsurfacePath, compositePath);
			if (done) {
				onShowNotification?.Invoke ("Atlas textures saved at: " + basePath);
			}
		}
		public int GetTextureSize (BranchDescriptorCollection.TextureSize textureSize) {
			if (textureSize == BranchDescriptorCollection.TextureSize._2048px) {
				return 2048;
			} else if (textureSize == BranchDescriptorCollection.TextureSize._1024px) {
				return 1024;
			} else if (textureSize == BranchDescriptorCollection.TextureSize._512px) {
				return 512;
			} else if (textureSize == BranchDescriptorCollection.TextureSize._256px) {
				return 256;
			} else {
				return 128;
			}
		}
		void OnReportProgress (string title, float progress) {
			if (!showProgressBar) {
				showProgressBar = true;
			}
			progressBarProgress = progress;
			progressBarTitle = title;
			//UnityEditor.EditorUtility.DisplayProgressBar (sproutSubfactory.progressTitle, title, progress);
			//UnityEditor.EditorUtility.DisplayCancelableProgressBar (sproutSubfactory.progressTitle, title, progress);
			EditorGUI.ProgressBar(new Rect (0, 0, meshPreviewRect.width, 
				EditorGUIUtility.singleLineHeight), progressBarProgress, progressBarTitle);
			meshPreview.RenderViewport (meshPreviewRect, GUIStyle.none, currentPreviewMaterials);
			EditorWindow view = EditorWindow.GetWindow<SproutFactoryEditorWindow>();
			view?.Repaint ();
			InternalEditorUtility.RepaintAllViews ();
		}
		void OnFinishProgress () {
			showProgressBar = false;
			//UnityEditor.EditorUtility.ClearProgressBar ();
			//GUIUtility.ExitGUI();
		}
		public void Repaint () {
			EditorWindow view = EditorWindow.GetWindow<SproutFactoryEditorWindow>();
			view?.Repaint ();
		}
		#endregion

		#region Editor Updates
		double editorDeltaTime = 0f;
		double lastTimeSinceStartup = 0f;
		/*
		double secondsToUpdateTexture = 0f;
		string _textureId = "";
		float _alpha = 1.0f;
		*/
		/// <summary>
		/// Raises the editor update event.
		/// </summary>
		void OnEditorUpdate () {
			/*
			if (secondsToUpdateTexture > 0) {
				SetEditorDeltaTime();
				secondsToUpdateTexture -= (float) editorDeltaTime;
				if (secondsToUpdateTexture < 0) {
					sproutSubfactory.ProcessTexture (selectedSproutMapGroup, selectedSproutMapIndex, _alpha);
					secondsToUpdateTexture = 0;
					EditorWindow view = EditorWindow.GetWindow<SproutFactoryEditorWindow>();
					view.Repaint();
				}
			}
			*/
			if (lightAngleToAddTimeTmp >= 0f) {
				SetEditorDeltaTime();
				lightAngleToAddTimeTmp -= (float)editorDeltaTime;
				UpdateLightAngle ();
				EditorWindow view = EditorWindow.GetWindow<SproutFactoryEditorWindow>();
				view.Repaint();
			}
			if (cameraTransitionTimeTmp >= 0f) {
				SetEditorDeltaTime();
				cameraTransitionTimeTmp -= (float)editorDeltaTime;
				if (viewTransitionEnabled) {
					meshPreview.SetDirection (Vector2.Lerp (cameraTransitionDirection, cameraTransitionDirectionTmp, cameraTransitionTimeTmp/cameraTransitionTime));
					meshPreview.SetOffset (Vector3.Lerp (cameraTransitionOffset, cameraTransitionOffsetTmp, cameraTransitionTimeTmp/cameraTransitionTime));
					meshPreview.SetTargetRotation (Quaternion.Lerp (cameraTransitionTargetRotation, cameraTransitionTargetRotationTmp, cameraTransitionTimeTmp/cameraTransitionTime));
				}
				if (zoomTransitionEnabled) {
					meshPreview.SetZoom (Mathf.Lerp (cameraTransitionZoom, cameraTransitionZoomTmp, cameraTransitionTimeTmp/cameraTransitionTime));
				}
				EditorWindow view = EditorWindow.GetWindow<SproutFactoryEditorWindow>();
				view.Repaint();
			}
		}
		void SetEditorDeltaTime ()
		{
			#if UNITY_EDITOR
			if (lastTimeSinceStartup == 0f)
			{
				lastTimeSinceStartup = EditorApplication.timeSinceStartup;
			}
			editorDeltaTime = EditorApplication.timeSinceStartup - lastTimeSinceStartup;
			lastTimeSinceStartup = EditorApplication.timeSinceStartup;
			#endif
		}
		/*
		void WaitProcessTexture (string textureId, float alpha) {
			secondsToUpdateTexture = 0.5f;
			_textureId = textureId;
			_alpha = alpha;
			SetEditorDeltaTime ();
		}
		*/
		void UpdateLightAngle () {
			Vector3 angle = Vector3.Lerp (lightAngleEulerFrom, lightAngleEulerTo, Mathf.InverseLerp (lightAngleToAddTime, 0, lightAngleToAddTimeTmp));
			meshPreview.GetLightA ().transform.rotation = Quaternion.Euler (angle);
		}
		void SetLightAngle (float degrees) {
			Vector3 angle = meshPreview.GetLightA ().transform.rotation.eulerAngles;
			angle.y += degrees;
			meshPreview.GetLightA ().transform.rotation = Quaternion.Euler (angle);
		}
		#endregion

		#region Editor Popups
		private void InitViewModePopup () {
			viewModePopup.width = 200;
			viewModePopup.height = 150;
			viewModePopup.title = "View Modes";
			viewModePopup.alignLeft = false;
			// View Mode Composite
			Button viewModeCompositeBtn = new Button ();
			viewModeCompositeBtn.text = "Composite";
			viewModeCompositeBtn.clicked += () => {
				SetMapView (VIEW_COMPOSITE);
				viewModePopup.Close ();
			};
			viewModePopup.listElem.hierarchy.Add (viewModeCompositeBtn);
			// View Mode Albedo
			Button viewModeAlbedoBtn = new Button ();
			viewModeAlbedoBtn.text = "Albedo";
			viewModeAlbedoBtn.clicked += () => {
				SetMapView (VIEW_ALBEDO);
				viewModePopup.Close ();
			};
			viewModePopup.listElem.hierarchy.Add (viewModeAlbedoBtn);
			// View Mode Normals
			Button viewModeNormalsBtn = new Button ();
			viewModeNormalsBtn.text = "Normals";
			viewModeNormalsBtn.clicked += () => {
				SetMapView (VIEW_NORMALS);
				viewModePopup.Close ();
			};
			viewModePopup.listElem.hierarchy.Add (viewModeNormalsBtn);
			// View Mode Extras
			Button viewModeExtrasBtn = new Button ();
			viewModeExtrasBtn.text = "Extras";
			viewModeExtrasBtn.clicked += () => {
				SetMapView (VIEW_EXTRAS);
				viewModePopup.Close ();
			};
			viewModePopup.listElem.hierarchy.Add (viewModeExtrasBtn);
			// View Mode Subsurface
			Button viewModeSubsurfaceBtn = new Button ();
			viewModeSubsurfaceBtn.text = "Subsurface";
			viewModeSubsurfaceBtn.clicked += () => {
				SetMapView (VIEW_SUBSURFACE);
				viewModePopup.Close ();
			};
			viewModePopup.listElem.hierarchy.Add (viewModeSubsurfaceBtn);
		}
		private void InitSnapshotOptionsPopup () {
			snapshotOptionsPopup.width = 200;
			snapshotOptionsPopup.height = 110;
			snapshotOptionsPopup.title = "Snapshot Operations";
			// Move Left button.
			Button moveLeftSnapBtn = new Button ();
			moveLeftSnapBtn.name = moveLeftBtnName;
			moveLeftSnapBtn.text = labelMoveLeftSnapBtn;
			moveLeftSnapBtn.tooltip = tooltipMoveLeftSnapBtn;
			moveLeftSnapBtn.clicked += () => {
				MoveSnapshot (-1);
				snapshotOptionsPopup.Close ();
			};
			snapshotOptionsPopup.listElem.hierarchy.Add (moveLeftSnapBtn);
			// Move Right button.
			Button moveRightSnapBtn = new Button ();
			moveRightSnapBtn.name = moveRightBtnName;
			moveRightSnapBtn.text = labelMoveRightSnapBtn;
			moveRightSnapBtn.tooltip = tooltipMoveRightSnapBtn;
			moveRightSnapBtn.clicked += () => {
				MoveSnapshot (1);
				snapshotOptionsPopup.Close ();
			};
			snapshotOptionsPopup.listElem.hierarchy.Add (moveRightSnapBtn);
			// Remove button.
			Button removeSnapBtn = new Button ();
			removeSnapBtn.name = removeBtnName;
			removeSnapBtn.text = labelRemoveSnapBtn;
			removeSnapBtn.tooltip = tooltipRemoveSnapBtn;
			removeSnapBtn.clicked += () => {
				RemoveSnapshot ();
				snapshotOptionsPopup.Close ();
			};
			snapshotOptionsPopup.listElem.hierarchy.Add (removeSnapBtn);
			snapshotOptionsPopup.onBeforeOpen -= OnBeforeOpenSnapshotOptionsPopup;
			snapshotOptionsPopup.onBeforeOpen += OnBeforeOpenSnapshotOptionsPopup;
		}
		private void OnBeforeOpenSnapshotOptionsPopup (VisualElement popupContainer) {
			Button moveLeftBtn = popupContainer.Q<Button> (moveLeftBtnName);
			Button moveRightBtn = popupContainer.Q<Button> (moveRightBtnName);
			Button removeBtn = popupContainer.Q<Button> (removeBtnName);
			moveLeftBtn.SetEnabled (false);
			moveRightBtn.SetEnabled (false);
			removeBtn.SetEnabled (false);
			if (selectedSnapshot != null) {
				removeBtn.SetEnabled (true);
				if (branchDescriptorCollection.snapshots.Count > 1) {
					if (branchDescriptorCollection.snapshotIndex > 0) {
						moveLeftBtn.SetEnabled (true);
					}
					if (branchDescriptorCollection.snapshotIndex < branchDescriptorCollection.snapshots.Count - 1) {
						moveRightBtn.SetEnabled (true);
					}
				}
			}
		}
		private void InitVariationOptionsPopup () {
			variationOptionsPopup.width = 200;
			variationOptionsPopup.height = 110;
			variationOptionsPopup.title = "Varshot Operations";
			// Move Left button.
			Button moveLeftVarBtn = new Button ();
			moveLeftVarBtn.name = moveLeftBtnName;
			moveLeftVarBtn.text = labelMoveLeftVarBtn;
			moveLeftVarBtn.tooltip = tooltipMoveLeftVarBtn;
			moveLeftVarBtn.clicked += () => {
				MoveVariation (-1);
				variationOptionsPopup.Close ();
			};
			variationOptionsPopup.listElem.hierarchy.Add (moveLeftVarBtn);
			// Move Right button.
			Button moveRightVarBtn = new Button ();
			moveRightVarBtn.name = moveRightBtnName;
			moveRightVarBtn.text = labelMoveRightVarBtn;
			moveRightVarBtn.tooltip = tooltipMoveRightVarBtn;
			moveRightVarBtn.clicked += () => {
				MoveVariation (1);
				variationOptionsPopup.Close ();
			};
			variationOptionsPopup.listElem.hierarchy.Add (moveRightVarBtn);
			// Remove button.
			Button removeVarBtn = new Button ();
			removeVarBtn.name = removeBtnName;
			removeVarBtn.text = labelRemoveVarBtn;
			removeVarBtn.tooltip = tooltipRemoveVarBtn;
			removeVarBtn.clicked += () => {
				RemoveVariation ();
				variationOptionsPopup.Close ();
			};
			variationOptionsPopup.listElem.hierarchy.Add (removeVarBtn);
			variationOptionsPopup.onBeforeOpen -= OnBeforeOpenVariationOptionsPopup;
			variationOptionsPopup.onBeforeOpen += OnBeforeOpenVariationOptionsPopup;
		}
		private void OnBeforeOpenVariationOptionsPopup (VisualElement popupContainer) {
			Button moveLeftBtn = popupContainer.Q<Button> (moveLeftBtnName);
			Button moveRightBtn = popupContainer.Q<Button> (moveRightBtnName);
			Button removeBtn = popupContainer.Q<Button> (removeBtnName);
			moveLeftBtn.SetEnabled (false);
			moveRightBtn.SetEnabled (false);
			removeBtn.SetEnabled (false);
			if (selectedVariation != null) {
				removeBtn.SetEnabled (true);
				if (branchDescriptorCollection.variations.Count > 1) {
					if (branchDescriptorCollection.variationIndex > 0) {
						moveLeftBtn.SetEnabled (true);
					}
					if (branchDescriptorCollection.variationIndex < branchDescriptorCollection.variations.Count - 1) {
						moveRightBtn.SetEnabled (true);
					}
				}
			}
		}
		private static string moveLeftBtnName = "move-left-btn";
		private static string moveRightBtnName = "move-right-btn";
		private static string removeBtnName = "remove-btn";
		#endregion

		#region Texture Canvas
		private void BindTextureCanvasEvents () {
			textureCanvas.onZoomDone -= OnCanvasZoomDone;
			textureCanvas.onZoomDone += OnCanvasZoomDone;
			textureCanvas.onPanDone -= OnCanvasPanDone;
			textureCanvas.onPanDone += OnCanvasPanDone;
			textureCanvas.onBeforeEditArea -= OnBeforeEditArea;
			textureCanvas.onBeforeEditArea += OnBeforeEditArea;
			textureCanvas.onEditArea -= OnEditArea;
			textureCanvas.onEditArea += OnEditArea;
			onResize -= SetTextureCanvasSize;
			onResize += SetTextureCanvasSize;
		}
		void OnCanvasZoomDone (float currentZoom, float previousZoom) {}
		void OnCanvasPanDone (Vector2 currentOffset, Vector2 previousOffset) {}
		void OnBeforeEditArea (TextureCanvas.Area area) {
			if (selectedSproutMapArea != null) {
				branchDescriptorCollection.lastSnapshotIndex = branchDescriptorCollection.snapshotIndex;
				onBeforeEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
			}
		}
		void OnEditArea (TextureCanvas.Area area) {
			// Update area rect and anchor.
			if (selectedSproutMapArea != null && selectedSproutMapArea.texture != null) {
				selectedSproutMapArea.x = area.rect.x;
				selectedSproutMapArea.y = area.rect.y;
				selectedSproutMapArea.width = area.rect.width;
				selectedSproutMapArea.height = area.rect.height;
				selectedSproutMapArea.pivotX = area.pivot.x;
				selectedSproutMapArea.pivotY = area.pivot.y;
				onEditBranchDescriptor?.Invoke (selectedSnapshot, branchDescriptorCollection);
				ReflectChangesToPipeline ();
				RegenerateStructure (true);
			}
		}
		void SetTextureCanvasSize (Rect oldWindowSize, Rect newWindowSize) {
			_resizeTextureCanvas = true;
		}
		#endregion

		#region Utils
		public static void SetupSlider (Slider slider, bool isInt = false) {
			slider?.RegisterValueChangedCallback(evt => {
				RefreshSlider (slider, evt.newValue, isInt);
			});
		}
		public static void SetupSlider (SliderInt slider) {
			slider?.RegisterValueChangedCallback(evt => {
				RefreshSlider (slider, evt.newValue);
			});
		}
		public static void SetupMinMaxSlider (MinMaxSlider minMaxSlider, bool isInt = false) {
			minMaxSlider?.RegisterValueChangedCallback(evt => {
				RefreshMinMaxSlider (minMaxSlider, evt.newValue, isInt);
			});
		}
		public static void RefreshSlider (Slider slider, float value, bool isInt = false) {
			Label info = slider.Q<Label>("info");
			if (info != null) {
				if (isInt) {
					info.text = string.Format ("{0:0}", Mathf.Round (value));
				} else {
					info.text = string.Format ("{0:0.00}", value);
				}
			}
		}
		public static void RefreshSlider (SliderInt slider, float value) {
			Label info = slider.Q<Label>("info");
			if (info != null)
				info.text = string.Format ("{0}", value);
		}
		public static void RefreshMinMaxSlider (MinMaxSlider minMaxSlider, Vector2 value, bool isInt = false) {
			Label info = minMaxSlider.Q<Label>("info");
			if (info != null) {
				if (isInt) {
					info.text = string.Format ("{0:0}/{1:0}", Mathf.Round (value.x), Mathf.Round (value.y));
				} else {
					info.text = string.Format ("{0:0.00}/{1:0.00}", value.x, value.y); 
				}
			}
		}
		#endregion
	}
}