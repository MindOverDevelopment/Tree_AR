using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

using Broccoli.Base;
using Broccoli.Factory;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Model;

namespace Broccoli.BroccoEditor
{
    public class SproutLabDebugPanel {
        #region Vars
        bool isInit = false;
        static string containerName = "sl-debug-panel";
		SproutLabEditor sproutLabEditor = null;
		public bool requiresRepaint = false;
		private SproutLabDebugSettings debugSettings = SproutLabDebugSettings.instance;
		public bool drawViewCanvas = false;
		#endregion

		#region Texture Vars
		int textureIndex = 0;
		string[] textureOptions;
		List<Hash128> hashOptions = new List<Hash128> ();
		public Texture2D texture = null;
        #endregion

		#region Frags Vars
		List<SnapshotProcessor.Fragment> fragments = new List<SnapshotProcessor.Fragment> ();
		string fragsInfo = string.Empty;
		public PolygonAreaBuilder polygonBuilder = new PolygonAreaBuilder ();
		public SproutCompositeManager compositeManager = new SproutCompositeManager ();
		string[] lodOptions = new string[] {"LOD0", "LOD1", "LOD2"};
		int lodIndex = 0;
		bool simplifyHullEnabled = true;
		int fragsIndex = 0;
		string[] fragsOptions;
		SnapshotProcessor.Fragment currentFrag = null;
		#endregion

		#region Polygon Vars
		int polyIndex = 0;
		string[] polyOptions;
		string polyInfo = string.Empty;
		List<ulong> polyIdOptions = new List<ulong> ();
		PolygonArea currentPolygonArea = null;
        #endregion

        #region GUI Vars
        /// <summary>
        /// Container for the UI.
        /// </summary>
        VisualElement container; 
        /// <summary>
        /// Rect used to draw the components in this panel.
        /// </summary>
        Rect rect;
		/// <summary>
		/// Panel xml.
		/// </summary>
		private VisualTreeAsset debugPanelXml;
		/// <summary>
		/// Panel style.
		/// </summary>
		private StyleSheet debugPanelStyle;
		/// <summary>
		/// Path to the panel xml.
		/// </summary>
		private string debugPanelXmlPath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabDebugPanelView.uxml"; }
		}
		/// <summary>
		/// Path to the panel style.
		/// </summary>
		private string debugPanelStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabDebugPanelStyle.uss"; }
		}
		private const int SUBPANEL_GEOMETRY = 0;
		private const int SUBPANEL_CANVAS = 1;
		private const int SUBPANEL_MESH = 2;
		private const int SUBPANEL_PROCESS = 3;
		private const int SUBPANEL_BUILDER = 4;
		private const int SUBPANEL_SNAPSHOT = 5;

		private static List<string> settingsItems = new List<string> {
			"Geometry", 
			"Canvas", 
			"Mesh", 
			"Process", 
			"Builder", 
			"Snapshot"
		};
		private static string optionsListName = "options-list";
		private static string geometryContainerName = "container-geometry";
		private static string canvasContainerName = "container-canvas";
		private static string meshContainerName = "container-mesh";
		private static string processContainerName = "container-process";

		// Builder Container.
		private static string builderContainerName = "container-builder";
		private static string buildCompFoldoutName = "build-comp-foldout";
		private static string buildCompImguiName = "build-comp-imgui";

		// Snapshot Container.
		private static string snapshotContainerName = "container-snapshot";
		private static string snapInfoFoldoutName = "snap-info-foldout"; 
		private static string snapInfoImguiName = "snap-info-imgui"; 
		private static string snapMeshFoldoutName = "snap-mesh-foldout";
		private static string snapMeshImguiName = "snap-mesh-imgui";
		private static string snapProcessFoldoutName = "snap-process-foldout";
		private static string snapProcessImguiName = "snap-process-imgui";
		private static string snapTextureFoldoutName = "snap-texture-foldout";
		private static string snapTextureImguiName = "snap-texture-imgui";
		private static string snapFragsFoldoutName = "snap-frags-foldout";
		private static string snapFragsImguiName = "snap-frags-imgui";
		private static string snapPolysFoldoutName = "snap-polys-foldout";
		private static string snapPolysImguiName = "snap-polys-imgui";

		private static string containerCanvasImguiName = "container-canvas-imgui";
		private static string containerMeshImguiName = "container-mesh-imgui";

		// Snapshot Container Elems.
		private VisualElement builderContainer;
		private Foldout buildCompFoldoutElem;
		private IMGUIContainer buildCompImguiElem;

		// Snapshot Container Elems.
		private VisualElement snapshotContainer;
		private Foldout snapInfoFoldoutElem;
		private IMGUIContainer snapInfoImguiElem;
		private Foldout snapMeshFoldoutElem;
		private IMGUIContainer snapMeshImguiElem;
		private Foldout snapProcessFoldoutElem;
		private IMGUIContainer snapProcessImguiElem;
		private Foldout snapTextureFoldoutElem;
		private IMGUIContainer snapTextureImguiElem;
		private Foldout snapFragsFoldoutElem;
		private IMGUIContainer snapFragsImguiElem;
		private Foldout snapPolysFoldoutElem;
		private IMGUIContainer snapPolysImguiElem;

		private IMGUIContainer containerCanvasImguiElem;
		private IMGUIContainer containerMeshImguiElem;
		private ListView optionsList;
		private VisualElement geometryContainer;
		private VisualElement canvasContainer;
		private VisualElement meshContainer;
		private VisualElement processContainer;
		
		private ColorField bgColorElem;
		private Slider planeSizeElem;
		private ColorField planeTintElem;
		private Toggle gizmos3dElem;
		private Slider gizmos3dSizeElem;
		private Slider gizmosOutlineWidthElem;
		private Slider gizmosOutlineAlphaElem;
		private ColorField gizmosColorElem;
		private Slider gizmosLineWidthElem;
		private Slider gizmosUnitSizeElem;
		private Toggle showRulerElem;
		private ColorField rulerColorElem;
        #endregion

        #region Constructor
        public SproutLabDebugPanel (SproutLabEditor sproutLabEditor) {
            Initialize (sproutLabEditor);
        }
        #endregion

        #region Init
		private void OnSelectionChanged(IEnumerable<object> selectedItems) {
			geometryContainer.style.display = DisplayStyle.None;
			canvasContainer.style.display = DisplayStyle.None;
			meshContainer.style.display = DisplayStyle.None;
			processContainer.style.display = DisplayStyle.None;
			builderContainer.style.display = DisplayStyle.None;
			snapshotContainer.style.display = DisplayStyle.None;
			switch (optionsList.selectedIndex) {
				case SUBPANEL_GEOMETRY:
					geometryContainer.style.display = DisplayStyle.Flex;
					break;
				case SUBPANEL_CANVAS:
					canvasContainer.style.display = DisplayStyle.Flex;
					break;
				case SUBPANEL_MESH:
					meshContainer.style.display = DisplayStyle.Flex;
					break;
				case SUBPANEL_PROCESS:
					processContainer.style.display = DisplayStyle.Flex;
					break;
				case SUBPANEL_BUILDER:
					builderContainer.style.display = DisplayStyle.Flex;
					break;
				case SUBPANEL_SNAPSHOT:
					snapshotContainer.style.display = DisplayStyle.Flex;
					break;
			}
		}
        public void Initialize (SproutLabEditor sproutLabEditor) {
			this.sproutLabEditor = sproutLabEditor;
            if (!isInit) { 
                // Start the container UIElement.
                container = new VisualElement ();
                container.name = containerName;

				// Load the VisualTreeAsset from a file.
				debugPanelXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(debugPanelXmlPath);

				// Create a new instance of the root VisualElement
				container.Add (debugPanelXml.CloneTree()); 

				geometryContainer = container.Q<VisualElement> (geometryContainerName);
				canvasContainer = container.Q<VisualElement> (canvasContainerName);
				meshContainer = container.Q<VisualElement> (meshContainerName);
				processContainer = container.Q<VisualElement> (processContainerName);
				builderContainer = container.Q<VisualElement> (builderContainerName);
				snapshotContainer = container.Q<VisualElement> (snapshotContainerName);

				// Canvas
				containerCanvasImguiElem = container.Q<IMGUIContainer> (containerCanvasImguiName);
				containerCanvasImguiElem.onGUIHandler = OnContainerCanvasIMGUI;

				// Mesh
				containerMeshImguiElem = container.Q<IMGUIContainer> (containerMeshImguiName);
				containerMeshImguiElem.onGUIHandler = OnContainerMeshIMGUI;

				// Init List and Containers.
				optionsList = container.Q<ListView> (optionsListName);
				Func<VisualElement> makeItem = () => new Label();
				optionsList.makeItem = makeItem;
				Action<VisualElement, int> bindSettingItem = (e, i) => (e as Label).text = settingsItems[i];
				optionsList.bindItem = bindSettingItem;
				optionsList.itemsSource = settingsItems;
				#if UNITY_2021_2_OR_NEWER
				optionsList.Rebuild ();
				#else
				optionsList.Refresh ();
				#endif
				#if UNITY_2022_2_OR_NEWER
				optionsList.selectionChanged -= OnSelectionChanged;
				optionsList.selectionChanged += OnSelectionChanged;
				#else
				optionsList.onSelectionChange -= OnSelectionChanged;
				optionsList.onSelectionChange += OnSelectionChanged;
				#endif
				optionsList.selectedIndex = 0;

				// Query Foldouts and IMGUIs.
				buildCompFoldoutElem = container.Q<Foldout> (buildCompFoldoutName); 
				buildCompImguiElem = container.Q<IMGUIContainer> (buildCompImguiName);
				snapInfoFoldoutElem = container.Q<Foldout> (snapInfoFoldoutName);
				snapInfoImguiElem = container.Q<IMGUIContainer> (snapInfoImguiName);
				snapMeshFoldoutElem = container.Q<Foldout> (snapMeshFoldoutName);
				snapMeshImguiElem = container.Q<IMGUIContainer> (snapMeshImguiName);
				snapProcessFoldoutElem = container.Q<Foldout> (snapProcessFoldoutName);
				snapProcessImguiElem = container.Q<IMGUIContainer> (snapProcessImguiName);
				snapTextureFoldoutElem = container.Q<Foldout> (snapTextureFoldoutName);
				snapTextureImguiElem = container.Q<IMGUIContainer> (snapTextureImguiName);
				snapFragsFoldoutElem = container.Q<Foldout> (snapFragsFoldoutName);
				snapFragsImguiElem = container.Q<IMGUIContainer> (snapFragsImguiName);
				snapPolysFoldoutElem = container.Q<Foldout> (snapPolysFoldoutName);
				snapPolysImguiElem = container.Q<IMGUIContainer> (snapPolysImguiName);
				buildCompFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotInfoFoldout = evt.newValue;
					if (evt.newValue) buildCompImguiElem.style.display = DisplayStyle.Flex;
					else buildCompImguiElem.style.display = DisplayStyle.None;
				});
				snapInfoFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotInfoFoldout = evt.newValue;
					if (evt.newValue) snapInfoImguiElem.style.display = DisplayStyle.Flex;
					else snapInfoImguiElem.style.display = DisplayStyle.None;
				});
				snapMeshFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotMeshFoldout = evt.newValue;
					if (evt.newValue) snapMeshImguiElem.style.display = DisplayStyle.Flex;
					else snapMeshImguiElem.style.display = DisplayStyle.None;
				});
				snapProcessFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotProcessFoldout = evt.newValue;
					if (evt.newValue) snapProcessImguiElem.style.display = DisplayStyle.Flex;
					else snapProcessImguiElem.style.display = DisplayStyle.None;
				});
				snapTextureFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotTextureFoldout = evt.newValue;
					if (evt.newValue) snapTextureImguiElem.style.display = DisplayStyle.Flex;
					else snapTextureImguiElem.style.display = DisplayStyle.None;
					PopulateTextureOptions ();
				});
				snapPolysFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotPolysFoldout = evt.newValue;
					if (evt.newValue) snapPolysImguiElem.style.display = DisplayStyle.Flex;
					else snapPolysImguiElem.style.display = DisplayStyle.None;
					PopulatePolysOptions ();
				});
				snapFragsFoldoutElem?.RegisterValueChangedCallback(evt => {
					debugSettings.snapshotFragsFoldout = evt.newValue;
					if (evt.newValue) snapFragsImguiElem.style.display = DisplayStyle.Flex;
					else snapFragsImguiElem.style.display = DisplayStyle.None;
				});
				buildCompImguiElem.onGUIHandler = OnBuildCompIMGUI;
				snapInfoImguiElem.onGUIHandler = OnSnapInfoIMGUI;
				snapMeshImguiElem.onGUIHandler = OnSnapIMGUI;
				snapProcessImguiElem.onGUIHandler = OnSnapProcessIMGUI;
				snapTextureImguiElem.onGUIHandler = OnSnapTextureIMGUI;
				snapFragsImguiElem.onGUIHandler = OnSnapFragsIMGUI;
				snapPolysImguiElem.onGUIHandler = OnSnapPolysIMGUI;

				/*
				// Init the Bg Color field.
				bgColorElem = container.Q<ColorField> (bgColorName);
				bgColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.bgColor) {
						sproutLabEditor.branchDescriptorCollection.bgColor = evt.newValue;
						sproutLabEditor.meshPreview.backgroundColor = sproutLabEditor.branchDescriptorCollection.bgColor;
					}
				});

				// Init plane size field.
				planeSizeElem = container.Q<Slider> (planeSizeName);
				planeSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.planeSize) {
						sproutLabEditor.branchDescriptorCollection.planeSize = evt.newValue;
						sproutLabEditor.RefreshSlider (planeSizeElem, sproutLabEditor.branchDescriptorCollection.planeSize);
						Vector3 planeSize = new Vector3 (sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize);
						sproutLabEditor.meshPreview.SetPlaneMesh (planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeTint);
					}
				});
				sproutLabEditor.SetupSlider (planeSizeElem);

				// Init plane tint field.
				planeTintElem = container.Q<ColorField> (planeTintName);
				planeTintElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.planeTint) {
						sproutLabEditor.branchDescriptorCollection.planeTint = evt.newValue;
						Vector3 planeSize = new Vector3 (sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize);
						sproutLabEditor.meshPreview.SetPlaneMesh (planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeTint);
					}
				});

				// Init gizmos color field.
				gizmosColorElem = container.Q<ColorField> (gizmosColorName);
				gizmosColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosColor) {
						sproutLabEditor.branchDescriptorCollection.gizmosColor = evt.newValue;
					}
				});

				// Init gizmo line width field.
				gizmosLineWidthElem = container.Q<Slider> (gizmosLineWidthName);
				#if UNITY_2020_2_OR_NEWER
				gizmosLineWidthElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosLineWidth) {
						sproutLabEditor.branchDescriptorCollection.gizmosLineWidth = evt.newValue;
						sproutLabEditor.RefreshSlider (gizmosLineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosLineWidth);
					}
				});
				sproutLabEditor.SetupSlider (gizmosLineWidthElem);
				#else
				gizmosLineWidthElem.style.display = DisplayStyle.None;
				#endif

				// Init gizmo unit size field.
				gizmosUnitSizeElem = container.Q<Slider> (gizmosUnitSizeName);
				gizmosUnitSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosUnitSize) {
						sproutLabEditor.branchDescriptorCollection.gizmosUnitSize = evt.newValue;
						sproutLabEditor.RefreshSlider (gizmosUnitSizeElem, sproutLabEditor.branchDescriptorCollection.gizmosUnitSize);
					}
				});
				sproutLabEditor.SetupSlider (gizmosUnitSizeElem);

				// Gizmo outline width field.
				gizmosOutlineWidthElem = container.Q<Slider> (gizmosOutlineWidthName);
				gizmosOutlineWidthElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth) {
						sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth = evt.newValue;
						sproutLabEditor.RefreshSlider (gizmosOutlineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth);
					}
				});
				sproutLabEditor.SetupSlider (gizmosOutlineWidthElem);

				// Gizmo outline alpha field.
				gizmosOutlineAlphaElem = container.Q<Slider> (gizmosOutlineAlphaName);
				gizmosOutlineAlphaElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha) {
						sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha = evt.newValue;
						sproutLabEditor.RefreshSlider (gizmosOutlineAlphaElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha);
					}
				});
				sproutLabEditor.SetupSlider (gizmosOutlineAlphaElem);

				// 3D gizmo.
				gizmos3dElem = container.Q<Toggle> (gizmos3dName);
				gizmos3dElem?.RegisterValueChangedCallback (evt => {
					sproutLabEditor.branchDescriptorCollection.showAxisGizmo = evt.newValue;
					sproutLabEditor.meshPreview.showAxis = evt.newValue;
				});

				// 3D gizmo size.
				gizmos3dSizeElem = container.Q<Slider> (gizmos3dSizeName);
				gizmos3dSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.axisGizmoSize) {
						sproutLabEditor.branchDescriptorCollection.axisGizmoSize = evt.newValue;
						sproutLabEditor.RefreshSlider (gizmos3dSizeElem, sproutLabEditor.branchDescriptorCollection.axisGizmoSize);
						sproutLabEditor.meshPreview.axisGizmoSize = evt.newValue;
					}
				});
				sproutLabEditor.SetupSlider (gizmos3dSizeElem);

				// Show Ruler.
				showRulerElem = container.Q<Toggle> (showRulerName);
				showRulerElem?.RegisterValueChangedCallback (evt => {
					sproutLabEditor.branchDescriptorCollection.showRuler = evt.newValue;
					sproutLabEditor.meshPreview.showRuler = evt.newValue;
					if (evt.newValue) {
						rulerColorElem.style.display = DisplayStyle.Flex;
					} else {
						rulerColorElem.style.display = DisplayStyle.None;
					}
				});

				// Init ruler color field.
				rulerColorElem = container.Q<ColorField> (rulerColorName);
				rulerColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.rulerColor) {
						sproutLabEditor.branchDescriptorCollection.rulerColor = evt.newValue;
						sproutLabEditor.meshPreview.rulerColor = evt.newValue;
					}
				});
				*/


				isInit = true;

                RefreshValues ();
            }
        }
		private void OnBuildCompIMGUI () {
			string info = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetSnapshotsDebugInfo ();
			EditorGUILayout.HelpBox (info, MessageType.None);
		}
		private void OnSnapInfoIMGUI () {
			if (sproutLabEditor.selectedSnapshot != null) {
				string info = sproutLabEditor.selectedSnapshot.GetDebugInfo ();
				EditorGUILayout.HelpBox (info, MessageType.None);
				// Processor Id.
				sproutLabEditor.selectedSnapshot.processorId = EditorGUILayout.IntField ("Processor Id",
					sproutLabEditor.selectedSnapshot.processorId);
			}
			EditorGUILayout.Space ();
		}
		private void OnSnapProcessIMGUI () {
			MeshEditorDebug.Current ().DrawEditorSnapshotProcess (sproutLabEditor);
			EditorGUILayout.Space ();
		}
		private void OnSnapIMGUI () {
			if (sproutLabEditor.selectedSnapshot != null) {
				string info = sproutLabEditor.selectedSnapshot.GetPolygonAreasDebugInfo ();
				EditorGUILayout.HelpBox (info, MessageType.None); 
			}
			MeshEditorDebug.Current ().DrawEditorSnapshotMesh (sproutLabEditor);
			EditorGUILayout.Space ();
		}
		private void OnSnapTextureIMGUI () {
			if (sproutLabEditor.sproutSubfactory != null && sproutLabEditor.sproutSubfactory.sproutCompositeManager != null) {
				//string info = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetTexturesDebugInfo ();
				string info;
				if (texture == null) {
					info = "No texture selected.";
					EditorGUILayout.HelpBox (info, MessageType.None);
					if (drawViewCanvas && GUILayout.Button ("Clear Canvas")) {
						drawViewCanvas = false;
					}
				} else {
					EditorGUILayout.BeginHorizontal ();
					info = string.Format ("Texture width: {0}, height: {1}", texture.width, texture.height);
					EditorGUILayout.HelpBox (info, MessageType.None);
					if (GUILayout.Button ("Clear Texture")) {
						texture = null;
						drawViewCanvas = false;
					}
					EditorGUILayout.EndHorizontal ();
				}
			}
			if (textureOptions.Length > 0) {
				EditorGUILayout.BeginHorizontal ();
				textureIndex = EditorGUILayout.Popup (textureIndex, textureOptions);
				if (textureIndex >= 0) {
					if (GUILayout.Button ("Show Albedo")) {
						texture = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetAlbedoTexture (hashOptions [textureIndex]);
						drawViewCanvas = true;
					}
					if (GUILayout.Button ("Show Normal")) {
						texture = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetNormalTexture (hashOptions [textureIndex]);
						drawViewCanvas = true;
					}
					if (GUILayout.Button ("Show Extras")) {
						texture = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetExtrasTexture (hashOptions [textureIndex]);
						drawViewCanvas = true;
					}
					if (GUILayout.Button ("Show Subsurface")) {
						texture = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetSubsurfaceTexture (hashOptions [textureIndex]);
						drawViewCanvas = true;
					}
				}
				EditorGUILayout.EndHorizontal ();
			}
		}
		private void OnSnapFragsIMGUI () {
			if (sproutLabEditor.selectedSnapshot != null) {
				if (fragments.Count == 0) {
					EditorGUILayout.BeginHorizontal ();
					lodIndex = EditorGUILayout.Popup ("LOD", lodIndex, lodOptions);
					if (GUILayout.Button ("Generate Frags")) {
						GenerateFrags (lodIndex);
					}
					EditorGUILayout.EndHorizontal ();
				} else {
					EditorGUILayout.HelpBox (fragsInfo, MessageType.None);
					EditorGUILayout.BeginHorizontal ();
					fragsIndex = EditorGUILayout.Popup ("Fragment", fragsIndex, fragsOptions);
					if (GUILayout.Button ("Show Frag")) {
						ShowFragment (fragments [fragsIndex]);
					}
					if (GUILayout.Button ("Hide Frag")) {
						HideFragment ();
					}
					if (GUILayout.Button ("Clear Frags")) {
						fragments.Clear ();
					}
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.Space ();
		}
		private void OnSnapPolysIMGUI () {
			if (sproutLabEditor.selectedSnapshot != null) {
				EditorGUILayout.HelpBox (polyInfo, MessageType.None);
				EditorGUILayout.BeginHorizontal ();
				polyIndex = EditorGUILayout.Popup ("Polygon Area", polyIndex, polyOptions);
				if (GUILayout.Button ("Show Polygon")) {
					ShowPolygonArea (polyIdOptions [polyIndex]);
				}
				if (currentPolygonArea != null && GUILayout.Button ("Clear Polygon")) {
					currentPolygonArea = null;
				}
				EditorGUILayout.EndHorizontal ();
			}
			EditorGUILayout.Space ();
		}
		private void OnContainerCanvasIMGUI () {
			EditorGUILayout.HelpBox (sproutLabEditor.meshPreview.GetDebugInfo (), MessageType.None);
			//MeshPreview Target Rotation Offset.
			Vector3 rotationOffset = EditorGUILayout.Vector3Field ("Rotation Offset", sproutLabEditor.meshPreview.targetRotationOffset.eulerAngles);
			if (rotationOffset != sproutLabEditor.meshPreview.targetRotationOffset.eulerAngles) {
				sproutLabEditor.meshPreview.targetRotationOffset.eulerAngles = rotationOffset;
			}
			//MeshPreview Target Position Offset.
			Vector3 positionOffset = EditorGUILayout.Vector3Field ("Position Offset", sproutLabEditor.meshPreview.targetPositionOffset);
			if (positionOffset != sproutLabEditor.meshPreview.targetPositionOffset) {
				sproutLabEditor.meshPreview.targetPositionOffset = positionOffset;
			}
			// MeshPreview Light A.
			Light lightA = sproutLabEditor.meshPreview.GetLightA ();
			float lightAIntensity = EditorGUILayout.FloatField ("Light A Intensity", lightA.intensity);
			if (lightA.intensity != lightAIntensity) {
				lightA.intensity = lightAIntensity;
			}
			Vector3 lightARotation = EditorGUILayout.Vector3Field ("Light A Rot", lightA.transform.rotation.eulerAngles);
			if (lightARotation != lightA.transform.rotation.eulerAngles) {
				lightA.transform.root.eulerAngles = lightARotation;
			}
			// MeshPreview Light B.
			Light lightB = sproutLabEditor.meshPreview.GetLightB ();
			float lightBIntensity = EditorGUILayout.FloatField ("Light B Intensity", lightB.intensity);
			if (lightB.intensity != lightBIntensity) {
				lightB.intensity = lightBIntensity;
			}
			Vector3 lightBRotation = EditorGUILayout.Vector3Field ("Light B Rot", lightB.transform.rotation.eulerAngles);
			if (lightBRotation != lightB.transform.rotation.eulerAngles) {
				lightB.transform.root.eulerAngles = lightBRotation;
			}
			sproutLabEditor.meshPreview.debugShowNormals = EditorGUILayout.Toggle ("Show Normals", sproutLabEditor.meshPreview.debugShowNormals);
			sproutLabEditor.meshPreview.debugShowTangents = EditorGUILayout.Toggle ("Show Tangents", sproutLabEditor.meshPreview.debugShowTangents);
			MeshPreview.SecondPassBlend spb = (MeshPreview.SecondPassBlend)EditorGUILayout.EnumPopup ("Second Pass Blend", sproutLabEditor.meshPreview.secondPassBlend);
			if (spb != sproutLabEditor.meshPreview.secondPassBlend) {
				sproutLabEditor.meshPreview.secondPassBlend = spb;
			}
		}
		private void OnContainerMeshIMGUI () {
			debugSettings.targetMesh = (Mesh)EditorGUILayout.ObjectField ("Mesh", debugSettings.targetMesh, typeof(UnityEngine.Mesh), true);
			debugSettings.targetMaterial = (Material) EditorGUILayout.ObjectField ("Material", debugSettings.targetMaterial, typeof (Material), true);
			if (debugSettings.targetMesh != null && debugSettings.targetMaterial && GUILayout.Button ("Add Mesh")) {
				Material[] mats = new Material[debugSettings.targetMesh.subMeshCount];
				for (int i = 0; i < debugSettings.targetMesh.subMeshCount; i++) {
					mats [i] = debugSettings.targetMaterial;
				}
				MeshEditorDebug.Current ().DebugAddMesh (debugSettings.targetMesh, mats);
			}
			if (sproutLabEditor.meshPreview.GetMesh () != null) {
				EditorGUILayout.Space ();
				if (GUILayout.Button ("Add Mesh To Scene")) {
					Mesh _mesh = sproutLabEditor.meshPreview.GetMesh ();
					GameObject go = new GameObject (string.Format("meshPreviewMesh_{0}", _mesh));

					MeshFilter mf = go.AddComponent<MeshFilter> ();
					mf.sharedMesh = _mesh;

					MeshRenderer mr = go.AddComponent<MeshRenderer> ();
					mr.sharedMaterials = sproutLabEditor.currentPreviewMaterials;
				}
			}
			EditorGUILayout.Space ();
			EditorGUI.BeginChangeCheck ();
			debugSettings.meshShowNormals = EditorGUILayout.Toggle ("Show Normals", debugSettings.meshShowNormals);
			debugSettings.meshShowTangents = EditorGUILayout.Toggle ("Show Tangents", debugSettings.meshShowTangents);
			if (EditorGUI.EndChangeCheck ()) {
				sproutLabEditor.meshPreview.debugShowNormals = debugSettings.meshShowNormals;
				sproutLabEditor.meshPreview.debugShowTangents = debugSettings.meshShowTangents;
			}
			/*
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
			*/
			/*
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
			*/
		}
		public void RefreshValues () {
			snapInfoFoldoutElem.value = debugSettings.snapshotInfoFoldout;
			snapMeshFoldoutElem.value = debugSettings.snapshotMeshFoldout;
			snapProcessFoldoutElem.value = debugSettings.snapshotProcessFoldout;
			snapTextureFoldoutElem.value = debugSettings.snapshotTextureFoldout;
			snapFragsFoldoutElem.value = debugSettings.snapshotFragsFoldout;
			snapTextureFoldoutElem.value = debugSettings.snapshotPolysFoldout;

			if (debugSettings.snapshotInfoFoldout) snapInfoImguiElem.style.display = DisplayStyle.Flex;
			else snapInfoImguiElem.style.display = DisplayStyle.None;

			if (debugSettings.snapshotProcessFoldout) snapProcessImguiElem.style.display = DisplayStyle.Flex;
			else snapProcessImguiElem.style.display = DisplayStyle.None;

			if (debugSettings.snapshotMeshFoldout) snapMeshImguiElem.style.display = DisplayStyle.Flex;
			else snapMeshImguiElem.style.display = DisplayStyle.None;

			if (debugSettings.snapshotTextureFoldout) snapTextureImguiElem.style.display = DisplayStyle.Flex; 
			else snapTextureImguiElem.style.display = DisplayStyle.None;

			if (debugSettings.snapshotFragsFoldout) snapFragsImguiElem.style.display = DisplayStyle.Flex; 
			else snapFragsImguiElem.style.display = DisplayStyle.None;

			if (debugSettings.snapshotPolysFoldout) snapPolysImguiElem.style.display = DisplayStyle.Flex; 
			else snapPolysImguiElem.style.display = DisplayStyle.None;

			PopulateTextureOptions ();
			PopulatePolysOptions ();
			/*
			if (sproutLabEditor.branchDescriptorCollection != null) {
				listenGUIEvents = false;
				bgColorElem.value = sproutLabEditor.branchDescriptorCollection.bgColor;
				sproutLabEditor.RefreshSlider (planeSizeElem, sproutLabEditor.branchDescriptorCollection.planeSize);
				planeTintElem.value = sproutLabEditor.branchDescriptorCollection.planeTint;
				gizmosColorElem.value = sproutLabEditor.branchDescriptorCollection.gizmosColor;
				gizmosLineWidthElem.value = sproutLabEditor.branchDescriptorCollection.gizmosLineWidth;
				gizmosUnitSizeElem.value = sproutLabEditor.branchDescriptorCollection.gizmosUnitSize;
				gizmos3dElem.value = sproutLabEditor.branchDescriptorCollection.showAxisGizmo;
				gizmos3dSizeElem.value = sproutLabEditor.branchDescriptorCollection.axisGizmoSize;
				sproutLabEditor.meshPreview.showAxis = sproutLabEditor.branchDescriptorCollection.showAxisGizmo;
				showRulerElem.value = sproutLabEditor.branchDescriptorCollection.showRuler;
				sproutLabEditor.meshPreview.showRuler = sproutLabEditor.branchDescriptorCollection.showRuler;
				rulerColorElem.value = sproutLabEditor.branchDescriptorCollection.rulerColor;
				sproutLabEditor.meshPreview.rulerColor = sproutLabEditor.branchDescriptorCollection.rulerColor;
				if (sproutLabEditor.branchDescriptorCollection.showRuler) {
					rulerColorElem.style.display = DisplayStyle.Flex;
				} else {
					rulerColorElem.style.display = DisplayStyle.None;
				}
				gizmosOutlineWidthElem.value = sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth;
				sproutLabEditor.RefreshSlider (gizmosOutlineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth);
				gizmosOutlineAlphaElem.value = sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha;
				sproutLabEditor.RefreshSlider (gizmosOutlineAlphaElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha);
				listenGUIEvents = true;
			}
			*/
		}
		public void Attach () {
			if (!this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Add (container);
			}
		}
		public void Detach () {
			if (this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Remove (container);
			}
		}
		public void OnDrawHandles (Rect r, Camera camera) {
			MeshEditorDebug.Current ().OnPreviewMeshDrawHandles (r, camera);
			if (currentFrag != null) {
				float handleSize = 0.8f * HandleUtility.GetHandleSize (Vector3.zero);
				Handles.color = Color.red;
				Handles.ArrowHandleCap (-1, currentFrag.offset, Quaternion.LookRotation (currentFrag.planeNormal), handleSize, EventType.Repaint);
				Handles.color = Color.cyan;
				Handles.ArrowHandleCap (-1, currentFrag.offset, Quaternion.LookRotation (currentFrag.planeAxis), handleSize, EventType.Repaint);
				/*
				Handles.color = Color.green;
				Handles.ArrowHandleCap (-1, currentFrag.offset, Quaternion.LookRotation (Vector3.Cross (currentFrag.planeNormal, currentFrag.planeAxis)), handleSize, EventType.Repaint);
				*/
			}
			if (currentPolygonArea != null) {
				Handles.color = Color.white;
				MeshEditorDebug.DrawBounds (currentPolygonArea.aabb, currentPolygonArea.planeNormal, currentPolygonArea.planeUp, currentPolygonArea.fragmentOffset);
                Handles.color = Color.yellow;
				MeshEditorDebug.DrawPoints (currentPolygonArea.points, currentPolygonArea.planeNormal, currentPolygonArea.planeUp, 0.04f);
				#if BROCCOLI_DEVEL
				Handles.color = Color.magenta;
				MeshEditorDebug.DrawPoints (currentPolygonArea.topoPoints, currentPolygonArea.planeNormal, currentPolygonArea.planeUp, 0.025f);
				#endif
				float handleSize = 0.8f * HandleUtility.GetHandleSize (Vector3.zero);
				Handles.color = Color.red;
				Handles.ArrowHandleCap (-1, 
					currentPolygonArea.fragmentOffset, 
					Quaternion.LookRotation (currentPolygonArea.planeNormal), 
					handleSize, 
					EventType.Repaint);
				Handles.color = Color.cyan;
				Handles.ArrowHandleCap (-1, 
					currentPolygonArea.fragmentOffset, 
					Quaternion.LookRotation (currentPolygonArea.planeUp), 
					handleSize, 
					EventType.Repaint);
			}
		}
		#endregion

		#region Control Utils
		public void Repaint () {
			RefreshValues ();
			requiresRepaint = false;
		}
		private void SetupSlider (Slider slider, bool isInt = false) {
			slider?.RegisterValueChangedCallback(evt => {
				RefreshSlider (slider, evt.newValue, isInt);
			});
		}
		private void SetupMinMaxSlider (MinMaxSlider minMaxSlider, bool isInt = false) {
			minMaxSlider?.RegisterValueChangedCallback(evt => {
				RefreshMinMaxSlider (minMaxSlider, evt.newValue, isInt);
			});
		}
		private void RefreshSlider (Slider slider, float value, bool isInit = false) {
			Label info = slider.Q<Label>("info");
			if (info != null) {
				if (isInit) {
					info.text = string.Format ("{0:00}", Mathf.Round (value));
				} else {
					info.text = string.Format ("{0:00.00}", value);
				}
			}
		}
		private void RefreshMinMaxSlider (MinMaxSlider minMaxSlider, Vector2 value, bool isInit = false) {
			Label info = minMaxSlider.Q<Label>("info");
			if (info != null) {
				if (isInit) {
					info.text = string.Format ("{0:00}/{1:00}", Mathf.Round (value.x), Mathf.Round (value.y));
				} else {
					info.text = string.Format ("{0:00.00}/{1:00.00}", value.x, value.y);
				}
			}
		}
		public void OnUndoRedo () {
			//LoadSidePanelFields (selectedVariationGroup);
		}
        #endregion

		#region Texture
		public void PopulateTextureOptions () {
			List<string >_textureOptions = new List<string> ();
			hashOptions.Clear ();
			textureIndex = 0;
			if (sproutLabEditor.sproutSubfactory != null && 
				sproutLabEditor.sproutSubfactory.sproutCompositeManager != null)
			{
				Dictionary<Hash128, Texture2D> textures = 
					sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetHashToAlbedoTexture ();
				var texEnum = textures.GetEnumerator ();
				while (texEnum.MoveNext ()) {
					_textureOptions.Add (texEnum.Current.Key.ToString ());
					hashOptions.Add (texEnum.Current.Key);
				}
			}
			textureOptions = _textureOptions.ToArray ();
		}
		#endregion

		#region Frags
		private void GenerateFrags (int lodLevel) {
			fragments.Clear ();
			fragsInfo = string.Empty;

			SnapshotProcessor processor = 
				sproutLabEditor.sproutSubfactory.GetSnapshotProcessor (sproutLabEditor.selectedSnapshot.processorId);
            if (processor == null) {
                Debug.Log ("No Snapshot Processor found with id " + sproutLabEditor.selectedSnapshot.id + ", skipping processing.");
                return;
            }
            // Begin usage.
            processor.BeginUsage (
				sproutLabEditor.sproutSubfactory.snapshotTree, 
				sproutLabEditor.sproutSubfactory.snapshotTreeMesh, 
				sproutLabEditor.sproutSubfactory.factoryScale);
            processor.simplifyHullEnabled = simplifyHullEnabled;
            polygonBuilder.BeginUsage (
				sproutLabEditor.sproutSubfactory.snapshotTree, 
				sproutLabEditor.sproutSubfactory.snapshotTreeMesh, 
				sproutLabEditor.sproutSubfactory.factoryScale);

            fragments = processor.GenerateSnapshotFragments (lodLevel, sproutLabEditor.selectedSnapshot);
			
			processor.EndUsage ();
			polygonBuilder.EndUsage ();

			List<string> _fragOptions = new List<string> ();
			string fragHash;

			for (int i = 0; i < fragments.Count; i++) {
				fragHash = fragments [i].IncludesExcludesToString (sproutLabEditor.selectedSnapshot.id);
				Hash128 _hash = Hash128.Compute (fragHash);
				if (i > 0) fragsInfo += "\n";
				fragsInfo += string.Format ("Frag [{0}] includes: {1}, excludes: {2}, hash: {3}\n",
					i, fragments [i].includes.Count, fragments [i].excludes.Count, _hash.ToString ());
				fragsInfo += string.Format ("  angle: {0}, offset: {1}", fragments[i].planeDegrees, fragments[i].offset);
				_fragOptions.Add (_hash.ToString ());
			}
			fragsOptions = _fragOptions.ToArray ();
			fragsIndex = 0;
		}
		private void ShowFragment (SnapshotProcessor.Fragment fragment) {
			compositeManager.BeginUsage (sproutLabEditor.sproutSubfactory.snapshotTree,
				sproutLabEditor.sproutSubfactory.factoryScale);
			compositeManager.ReflectIncludeAndExcludesToMesh (sproutLabEditor.selectedSnapshot,
				fragment.includes, fragment.excludes);
			currentFrag = fragment;
			compositeManager.EndUsage ();
		}
		private void HideFragment () {
			compositeManager.BeginUsage (sproutLabEditor.sproutSubfactory.snapshotTree,
				sproutLabEditor.sproutSubfactory.factoryScale);
			compositeManager.ShowAllBranchesInMesh ();
			currentFrag = null;
			compositeManager.EndUsage ();
		}
		#endregion

		#region Polys
		public void PopulatePolysOptions () {
			List<string>_polyOptions = new List<string> ();
			polyIdOptions.Clear ();
			polyIndex = 0;
			polyInfo = string.Empty;
			if (sproutLabEditor.sproutSubfactory != null && 
				sproutLabEditor.sproutSubfactory.sproutCompositeManager != null)
			{
				Dictionary<ulong, PolygonArea> polygonAreas = 
					sproutLabEditor.sproutSubfactory.sproutCompositeManager.polygonAreas;
				var polyEnum = polygonAreas.GetEnumerator ();
				bool first = true;
				PolygonArea pa;
				while (polyEnum.MoveNext ()) {
					_polyOptions.Add (polyEnum.Current.Key.ToString ());
					polyIdOptions.Add (polyEnum.Current.Key);
					pa = polyEnum.Current.Value;
					if (!first) polyInfo += "\n";
					else first = false;
					polyInfo += string.Format ("PolygonArea id {0}, hash {1}\n", pa.id, pa.hash);
					polyInfo += string.Format ("  snapshot {0}, lod {1}, frag {2}, res {3}", 
						pa.snapshotId, pa.lod, pa.fragment, pa.resolution);
				}
			}
			polyOptions = _polyOptions.ToArray ();
		}
		private void ShowPolygonArea (ulong polygonAreaId) {
			if (sproutLabEditor.sproutSubfactory != null && 
				sproutLabEditor.sproutSubfactory.sproutCompositeManager != null)
			{
				currentPolygonArea = sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetPolygonArea (polygonAreaId);
			} else {
				currentPolygonArea = null;
			}
		}
		#endregion

        #region Draw
		public void DrawViewCanvas (Rect rect) {
			/*
			if (texture != null) {
				sproutLabEditor.areaCanvas.DrawCanvas (rect, texture);
			}
			*/
		}
        public void SetVisible (bool visible) {
            if (visible) {
                container.style.display = DisplayStyle.Flex;
            } else {
                container.style.display = DisplayStyle.None;
            }
        }
        /// <summary>
        /// Sets the draw area for the components.
        /// </summary>
        /// <param name="refRect">Rect to draw the componentes.</param>
        public void SetRect (Rect refRect) {
            if (Event.current.type != EventType.Repaint) return;
            rect = refRect;
            container.style.marginTop = refRect.y;
            container.style.height = refRect.height;
        }
        #endregion
    }
}
