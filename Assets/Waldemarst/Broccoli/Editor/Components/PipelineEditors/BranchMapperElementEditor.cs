using System;

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Component;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Branch mapper element editor.
	/// </summary>
	[CustomEditor(typeof(BranchMapperElement))]
	public class BranchMapperElementEditor : PipelineElementEditor {
		#region Vars
		/// <summary>
		/// The branch mapper element.
		/// </summary>
		public BranchMapperElement branchMapperElement;
		SerializedProperty propMaterialMode;
		SerializedProperty propCustomMaterial;
		SerializedProperty propMainTexture;
		SerializedProperty propNormalTexture;
		SerializedProperty propExtrasTexture;
		SerializedProperty propMappingXDisplacement;
		SerializedProperty propMappingYDisplacement;
		SerializedProperty propMappingXTiles;
		SerializedProperty propMappingYTiles;
		SerializedProperty propIsGirthSensitive;
		SerializedProperty propColor;
		SerializedProperty propGlossiness;
		SerializedProperty propMetallic;
		SerializedProperty propDiffusionProfileSettings;
		SerializedProperty propBranchMaps;
		private static int minTreeTilesValue = -5;
		private static int maxTreeTilesValue = 5;
		private static int minShrubTilesValue = -10;
		private static int maxShrubTilesValue = 10;
		private static int minTreeDisplacementValue = -5;
		private static int maxTreeDisplacementValue = 5;
		private static int minShrubDisplacementValue = -10;
		private static int maxShrubDisplacementValue = 10;
		#endregion

		#region GUI Vars
		/// <summary>
		/// The textures list.
		/// </summary>
		ReorderableList branchMapList;
		/// <summary>
		/// Texture canvas component.
		/// </summary>
		TextureCanvas textureCanvas;
		/// <summary>
		/// The current branch map.
		/// </summary>
		BranchMap currentBranchMap = null;
		bool _centerTexture = false;
		private static GUIContent materialModeLabel = new GUIContent ("Material Mode", "Select using a material provided by the tree factory or a custom material provided by you.");
		private static GUIContent customMaterialLabel = new GUIContent ("Custom Material", "Custom material to use for branches.");
		private static GUIContent mainTextureLabel = new GUIContent ("Main Texture", "The albedo texture to use on the branch material.");
		private static GUIContent normalTextureLabel = new GUIContent ("Normal Texture", "The normal texture to use on the branch material.");
		private static GUIContent extrasTextureLabel = new GUIContent ("Extras Texture", "The extras texture to use on the branch material (red channel for metallic, green for glossiness, blue for ambient occlussion).");
		private static GUIContent colorTintLabel = new GUIContent ("Tint", "Color tint to apply to the branch material.");
		private static GUIContent glossinessLabel = new GUIContent ("Glossiness", "Glossiness (the quality of the material of being smooth and shiny) value pass to the shader.");
		private static GUIContent metallicLabel = new GUIContent ("Metallic", "Metallic (the quality of the material of being metal-like) value pass to the shader.");
		private static GUIContent mappingXDisplacementLabel = new GUIContent ("X Displacement", "Incremental offset to apply to the U value to the UV mapping along the girth of the branches.");
		private static GUIContent mappingYDisplacementLabel = new GUIContent ("Y Displacement", "Incremental offset to apply to the V value to the UV mapping along the length of the branches.");
		private static GUIContent mappingXTilesLabel = new GUIContent ("X Tiles", "Number of titles to use on the U value on the UV mapping.");
		private static GUIContent mappingYTilesLabel = new GUIContent ("Y Tiles", "Number of titles to use on the V value on the UV mapping.");
		private static GUIContent isGirthSensitiveLabel = new GUIContent ("Girth Sensitive", "The UV mapping displacement values adapts to the branch girth.");
		private static GUIContent diffusionProfileLabel = new GUIContent ("Diffusion Profile", "Diffusion Profile Asset used for HDRP materials.");
		#endregion

		#region Messages
		private static string MSG_MATERIAL_MODE = "Material mode to apply.";
		private static string MSG_CUSTOM_MATERIAL = "Material applied to the branches.";
		private static string MSG_MAIN_TEXTURE = "Main texture for the generated material.";
		private static string MSG_NORMAL_TEXTURE = "Normal map texture for the generated material.";
		private static string MSG_EXTRAS_TEXTURE = "Extras map texture for the generated material (red channel for metallic, green for glossiness, blue for ambient occlussion).";
		private static string MSG_MAPPING_X_DISP = "Girth to be used at the base of the tree trunk.";
		private static string MSG_MAPPING_Y_DISP = "Girth to be used at the tip of a terminal branch.";
		private static string MSG_MAPPING_X_TILES = "Multiplies the number of tiles for the texture on the X axis.";
		private static string MSG_MAPPING_Y_TILES = "Multiplies the number of tiles for the texture on the Y axis.";
		private static string MSG_GIRTH_SENSITIVE = "UV mapping is smaller at lower values of girth on the branches.";
		private static string MSG_COLOR = "Color value to pass to the shader.";
		private static string MSG_GLOSSINESS = "Glossiness value to pass to the shader.";
		private static string MSG_METALLIC = "Metallic value to pass to the shader.";
		private static string MSG_DIFFUSION_PROFILE = "Diffusion Profile Settings asset for HDRP materials. Make sure this profile is listed at the HDRP Project Settings. " +
			"Broccoli can only assign a Diffusion Profile in Edit Mode, so it is not available when creating trees at runtime.";
		#endregion

		#region Events
		/// <summary>
		/// Creates the UI Elements to be displayed in this inspector.
		/// </summary>
		/// <returns>UI elements to be displayed.</returns>
		public override VisualElement CreateInspectorGUI () {
			var container = new VisualElement();
 
			container.Add(new IMGUIContainer(OnInspectorGUI));
			if (textureCanvas == null) {
				textureCanvas = new TextureCanvas ();
				//structureGraph.Init (structureGeneratorElement.canvasOffset, 1f);
				textureCanvas.Init (Vector2.zero, 1f);
				container.Add (textureCanvas);
				textureCanvas.style.position = UnityEngine.UIElements.Position.Absolute;
				textureCanvas.StretchToParentSize ();
				BindTextureCanvasEvents ();
			}
			textureCanvas.Hide ();
			textureCanvas.RegisterArea (1, 0.2f, 0.2f, 0.5f, 0.5f);
			TextureCanvas.Area area = textureCanvas.GetArea (1);
			area.SetHeightEditable (false);
		
			return container;
		}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy () {
			if (textureCanvas != null) textureCanvas.parent.Remove (textureCanvas);
		}
		/// <summary>
		/// Actions to perform on the concrete class when the enable event is raised.
		/// </summary>
		protected override void OnEnableSpecific () {
			branchMapperElement = target as BranchMapperElement;

			propMaterialMode = GetSerializedProperty ("materialMode");
			propCustomMaterial = GetSerializedProperty ("customMaterial");
			propMainTexture = GetSerializedProperty ("mainTexture");
			propNormalTexture = GetSerializedProperty ("normalTexture");
			propExtrasTexture = GetSerializedProperty ("extrasTexture");
			propMappingXDisplacement = GetSerializedProperty ("mappingXDisplacement");
			propMappingYDisplacement = GetSerializedProperty ("mappingYDisplacement");
			propMappingXTiles = GetSerializedProperty ("mappingXTiles");
			propMappingYTiles = GetSerializedProperty ("mappingYTiles");
			propIsGirthSensitive = GetSerializedProperty ("isGirthSensitive");
			propColor = GetSerializedProperty ("color");
			propGlossiness = GetSerializedProperty ("glossiness");
			propMetallic = GetSerializedProperty ("metallic");
			propDiffusionProfileSettings = GetSerializedProperty ("diffusionProfileSettings");
			propBranchMaps = GetSerializedProperty ("branchMaps");

			branchMapList = new ReorderableList (serializedObject, propBranchMaps, false, true, true, true);
			branchMapList.draggable = false;
			branchMapList.drawHeaderCallback += DrawTexturesListHeader;
			branchMapList.drawElementCallback += DrawTextureElement;
			branchMapList.onSelectCallback += OnSelectTextureElement;
			branchMapList.onAddCallback += OnAddTextureElement;
			branchMapList.onRemoveCallback += OnRemoveTextureElement;
		}
		protected virtual bool IsMaterialModeElegible(Enum _enum)
        {
            BranchMapperElement.MaterialMode candidate = (BranchMapperElement.MaterialMode)_enum;
			if (GlobalSettings.experimentalTrunkCompositeTexture) {
				return true;
			} else {
				return candidate != BranchMapperElement.MaterialMode.MultiTexture;
			}
        }
		/// <summary>
		/// Raises the inspector GUI event.
		/// </summary>
		protected override void OnInspectorGUISpecific () {
			CheckUndoRequest ();

			UpdateSerialized ();

			// Log box.
			DrawLogBox ();
			BranchMapperElement.MaterialMode materialMode = 
				(BranchMapperElement.MaterialMode)EditorGUILayout.EnumPopup (materialModeLabel, branchMapperElement.materialMode, IsMaterialModeElegible, false);
			if (materialMode != branchMapperElement.materialMode) {
				SetUndoControlCounter ();
				propMaterialMode.enumValueIndex = (int)materialMode;
				branchMapperElement.materialMode = materialMode;
				ApplySerialized ();
				UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, 
						GlobalSettings.processingDelayLow);
			}
			//EditorGUILayout.PropertyField (propMaterialMode, materialModeLabel);
			int materialModeIndex = propMaterialMode.enumValueIndex;
			ShowHelpBox (MSG_MATERIAL_MODE);
			EditorGUILayout.Space ();

			// MATERIAL MODE CUSTOM.
			if (materialModeIndex == (int)BranchMapperElement.MaterialMode.Custom) {
				EditorGUILayout.LabelField ("Material Properties", EditorStyles.boldLabel);
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propCustomMaterial, customMaterialLabel);
				ShowHelpBox (MSG_CUSTOM_MATERIAL);
				if (EditorGUI.EndChangeCheck () ||
				    materialModeIndex != propMaterialMode.enumValueIndex) {
					ApplySerialized ();
					UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, 
						GlobalSettings.processingDelayLow);
					// TODO: update with pink material when no material is set.
					SetUndoControlCounter ();
				}
			}

			// MATERIAL MODE TEXTURE.
			else if (materialModeIndex == (int)BranchMapperElement.MaterialMode.Texture) {
				EditorGUILayout.LabelField ("Texture Properties", EditorStyles.boldLabel);
				bool mainTextureChanged = false;
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propMainTexture, mainTextureLabel);
				if (EditorGUI.EndChangeCheck ()) {
					mainTextureChanged = true;
				}
				ShowHelpBox (MSG_MAIN_TEXTURE);

				bool normalTextureChanged = false;	
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propNormalTexture, normalTextureLabel);
				if (EditorGUI.EndChangeCheck ()) {
					normalTextureChanged = true;
				}
				ShowHelpBox (MSG_NORMAL_TEXTURE);

				bool extrasTextureChanged = false;	
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propExtrasTexture, extrasTextureLabel);
				if (EditorGUI.EndChangeCheck ()) {
					extrasTextureChanged = true;
				}
				ShowHelpBox (MSG_EXTRAS_TEXTURE);

				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (propColor, colorTintLabel);
				ShowHelpBox (MSG_COLOR);

				if (branchMapperElement.extrasTexture == null) {
					EditorGUILayout.Slider (propGlossiness, 0f, 1f, glossinessLabel);
					ShowHelpBox (MSG_GLOSSINESS);

					EditorGUILayout.Slider (propMetallic, 0f, 1f, metallicLabel);
					ShowHelpBox (MSG_METALLIC);
				}

				if (ExtensionManager.isHDRP) {
					ScriptableObject former = (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue;
					former = 
						(ScriptableObject)EditorGUILayout.ObjectField (
							diffusionProfileLabel, 
							former, 
							System.Type.GetType ("UnityEngine.Rendering.HighDefinition.DiffusionProfileSettings, Unity.RenderPipelines.HighDefinition.Runtime"), 
							false);
					if (former != (ScriptableObject)propDiffusionProfileSettings.objectReferenceValue) {
						propDiffusionProfileSettings.objectReferenceValue = former;
						mainTextureChanged = true;
					}
					ShowHelpBox (MSG_DIFFUSION_PROFILE);
				}
				if (materialModeIndex != propMaterialMode.enumValueIndex ||
				    mainTextureChanged || normalTextureChanged || extrasTextureChanged ||
					EditorGUI.EndChangeCheck ()) {
					ApplySerialized ();
					UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, 
						GlobalSettings.processingDelayLow);
					SetUndoControlCounter ();
				}
			}

			// MATERIAL MODE MULTI-TEXTURE
			else {
				EditorGUILayout.LabelField ("Texture Collection Properties", EditorStyles.boldLabel);
				branchMapList.DoLayoutList ();
			}
			EditorGUILayout.Space ();

			// MAPPING PROPERTIES
			EditorGUILayout.LabelField ("Mapping Properties", EditorStyles.boldLabel);
			float textureXDisplacement = 0;
			float textureYDisplacement = 0;
			if (pipelineElement.preset == PipelineElement.Preset.Tree) {
				textureXDisplacement = propMappingXDisplacement.floatValue;
				if (branchMapperElement.materialMode != BranchMapperElement.MaterialMode.MultiTexture) {
					EditorGUILayout.Slider (propMappingXDisplacement, minTreeDisplacementValue, maxTreeDisplacementValue, mappingXDisplacementLabel);
					ShowHelpBox (MSG_MAPPING_X_DISP);
				}
				textureYDisplacement = propMappingYDisplacement.floatValue;
				EditorGUILayout.Slider (propMappingYDisplacement, minTreeDisplacementValue, maxTreeDisplacementValue, mappingYDisplacementLabel);
				ShowHelpBox (MSG_MAPPING_Y_DISP);
			} else {
				textureXDisplacement = propMappingXDisplacement.floatValue;
				if (branchMapperElement.materialMode != BranchMapperElement.MaterialMode.MultiTexture) {
					EditorGUILayout.Slider (propMappingXDisplacement, minShrubDisplacementValue, maxShrubDisplacementValue, mappingXDisplacementLabel);
					ShowHelpBox (MSG_MAPPING_X_DISP);
				}
				textureYDisplacement = propMappingYDisplacement.floatValue;
				EditorGUILayout.Slider (propMappingYDisplacement, minShrubDisplacementValue, maxShrubDisplacementValue, mappingYDisplacementLabel);
				ShowHelpBox (MSG_MAPPING_Y_DISP);
			}

			int textureXTiles = 1;
			int textureYTiles = 1;
			if (pipelineElement.preset == PipelineElement.Preset.Tree) {
				textureXTiles = propMappingXTiles.intValue;
				if (branchMapperElement.materialMode != BranchMapperElement.MaterialMode.MultiTexture) {
					EditorGUILayout.IntSlider (propMappingXTiles, minTreeTilesValue, maxTreeTilesValue, mappingXTilesLabel);
					ShowHelpBox (MSG_MAPPING_X_TILES);
				}
				textureYTiles = propMappingYTiles.intValue;
				EditorGUILayout.IntSlider (propMappingYTiles, minTreeTilesValue, maxTreeTilesValue, mappingYTilesLabel);
				ShowHelpBox (MSG_MAPPING_Y_TILES);
			} else {
				textureXTiles = propMappingXTiles.intValue;
				if (branchMapperElement.materialMode != BranchMapperElement.MaterialMode.MultiTexture) {
					EditorGUILayout.IntSlider (propMappingXTiles, minShrubTilesValue, maxShrubTilesValue, mappingXTilesLabel);
					ShowHelpBox (MSG_MAPPING_X_TILES);
				}
				textureYTiles = propMappingYTiles.intValue;
				EditorGUILayout.IntSlider (propMappingYTiles, minShrubTilesValue, maxShrubTilesValue, mappingYTilesLabel);
				ShowHelpBox (MSG_MAPPING_Y_TILES);
			}

			bool isGirthSensitive = propIsGirthSensitive.boolValue;
			EditorGUILayout.PropertyField (propIsGirthSensitive, isGirthSensitiveLabel);
			ShowHelpBox (MSG_GIRTH_SENSITIVE);

			if (textureXDisplacement != propMappingXDisplacement.floatValue ||
				textureYDisplacement != propMappingYDisplacement.floatValue ||
				textureXTiles != propMappingXTiles.intValue ||
				textureYTiles != propMappingYTiles.intValue ||
				isGirthSensitive != propIsGirthSensitive.boolValue) 
			{
				ApplySerialized ();
				UpdateComponent ((int)BranchMapperComponent.ComponentCommand.SetUVs, 
					GlobalSettings.processingDelayLow);
				SetUndoControlCounter ();
			}
			DrawSeparator ();

			// Field descriptors option.
			DrawFieldHelpOptions ();

			// Preset.
			DrawPresetOptions ();
		}
		#endregion

		#region Textures List
		/// <summary>
		/// Draws the sprout area header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawTexturesListHeader(Rect rect) {
			GUI.Label(rect, "Textures");
		}
		/// <summary>
		/// Draws the sprout area element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawTextureElement (Rect rect, int index, bool isActive, bool isFocused) {
			var branchMapProp = branchMapList.serializedProperty.GetArrayElementAtIndex (index);
			EditorGUI.LabelField (new Rect (rect.x, rect.y, 
					150, EditorGUIUtility.singleLineHeight), "Texture " + index);
			if (isActive) {
				if (index != branchMapperElement.selectedBranchMapIndex) {
					branchMapperElement.selectedBranchMapIndex = index;
				}
				bool uvChanged = false;
				bool materialChanged = false;
				currentBranchMap = branchMapperElement.branchMaps [index];
				EditorGUILayout.Space ();

				// Enabled
				EditorGUI.BeginChangeCheck ();
				//EditorGUILayout.PropertyField (branchMapProp.FindPropertyRelative ("enabled"));
				// Albedo texture.
				EditorGUILayout.PropertyField (branchMapProp.FindPropertyRelative ("albedoTexture"));
				// Normal texture.
				EditorGUILayout.PropertyField (branchMapProp.FindPropertyRelative ("normalTexture"));
				// Extras texture.
				EditorGUILayout.PropertyField (branchMapProp.FindPropertyRelative ("extrasTexture"));
				if (EditorGUI.EndChangeCheck ()) {
					materialChanged = true;
				}

				Texture2D texture = branchMapperElement.branchMaps [index].albedoTexture;

				if (texture != null) {
					// x, y, width, height
					EditorGUI.BeginChangeCheck ();
					EditorGUILayout.Slider (branchMapProp.FindPropertyRelative ("x"), 0f, 1f, "Area X");
					EditorGUILayout.Slider (branchMapProp.FindPropertyRelative ("y"), 0f, 1f, "Area Y");
					EditorGUILayout.Slider (branchMapProp.FindPropertyRelative ("width"), 0f, 1f, "Area Width");
					EditorGUILayout.Slider (branchMapProp.FindPropertyRelative ("height"), 0f, 1f, "Area Height");
					EditorGUILayout.Space (35);
					if (EditorGUI.EndChangeCheck ()) {
						uvChanged = true;
						currentBranchMap.Validate ();
						textureCanvas.SetAreaRect (1, currentBranchMap.rect);
					}

					EditorGUILayout.Space ();
					float canvasSize = (1f / EditorGUIUtility.pixelsPerPoint) * Screen.width - 40;
					GUILayout.Box ("", GUIStyle.none, 
						GUILayout.ExpandWidth (true), 
						GUILayout.Height (canvasSize));
					Rect canvasRect = GUILayoutUtility.GetLastRect ();
					if (_centerTexture) {
						textureCanvas.style.marginTop = canvasRect.y;
						textureCanvas.style.height = canvasRect.height;
						textureCanvas.style.width = canvasRect.width - 4;
					}

					if (textureCanvas.SetTexture (texture)) {
						textureCanvas.Show ();
						textureCanvas.SetAreaRect (1, currentBranchMap.rect);
						_centerTexture = true;
					}
					if (_centerTexture && Event.current.type == EventType.Repaint) {
						textureCanvas.guiRect = canvasRect;
						textureCanvas.CenterTexture ();
						_centerTexture = false;
					}
				} else {
					textureCanvas.Hide ();
				}

				if (materialChanged || uvChanged) {
					ApplySerialized ();
					if (materialChanged)
						UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, GlobalSettings.processingDelayLow);
					if (uvChanged)
						UpdateComponent ((int)BranchMapperComponent.ComponentCommand.SetUVs, 
							GlobalSettings.processingDelayLow);
					SetUndoControlCounter ();
				}
			}
		}
		/// <summary>
		/// Raises the select sprout area item event.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnSelectTextureElement (ReorderableList list)
		{
			branchMapperElement.selectedBranchMapIndex = list.index;
			currentBranchMap = branchMapperElement.branchMaps [list.index];
			textureCanvas.SetAreaRect (1, currentBranchMap.rect); 
		}
		/// <summary>
		/// Adds a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnAddTextureElement(ReorderableList list)
		{
			Undo.RecordObject (branchMapperElement, "Branch Map added");
			branchMapperElement.branchMaps.Add (new BranchMap ());
		}
		/// <summary>
		/// Removes a list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void OnRemoveTextureElement(ReorderableList list)
		{
			int undoGroup = Undo.GetCurrentGroup ();
			//Undo.RecordObject (branchMapperElement, "Branch Map Area removed");
			Undo.SetCurrentGroupName ("Branch Map Area removed");
			branchMapperElement.branchMaps.RemoveAt (list.index);
			branchMapperElement.selectedBranchMapIndex = -1;
			//Undo.CollapseUndoOperations (undoGroup);
			ApplySerialized ();
			UpdateComponent ((int)BranchMapperComponent.ComponentCommand.BuildMaterials, GlobalSettings.processingDelayLow);
		}
		#endregion

		#region Structure Graph Events
		private void BindTextureCanvasEvents () {
			textureCanvas.onZoomDone -= OnCanvasZoomDone;
			textureCanvas.onZoomDone += OnCanvasZoomDone;
			textureCanvas.onPanDone -= OnCanvasPanDone;
			textureCanvas.onPanDone += OnCanvasPanDone;
			textureCanvas.onBeforeEditArea -= OnBeforeEditArea;
			textureCanvas.onBeforeEditArea += OnBeforeEditArea;
			textureCanvas.onEditArea -= OnEditArea;
			textureCanvas.onEditArea += OnEditArea;
		}
		void OnCanvasZoomDone (float currentZoom, float previousZoom) {}
		void OnCanvasPanDone (Vector2 currentOffset, Vector2 previousOffset) {}
		void OnBeforeEditArea (TextureCanvas.Area area) {
			if (currentBranchMap != null) {
				Undo.RecordObject (branchMapperElement, "Edit Texture areas.");
			}
		}
		void OnEditArea (TextureCanvas.Area area) {
			// Update area rect and anchor.
			if (currentBranchMap != null) {
				currentBranchMap.x = area.rect.x;
				currentBranchMap.y = area.rect.y;
				currentBranchMap.width = area.rect.width;
				currentBranchMap.height = area.rect.height;
				ApplySerialized ();
				currentBranchMap.Validate ();
				UpdatePipelineUpstream (PipelineElement.ClassType.SproutMeshGenerator, 
					GlobalSettings.processingDelayVeryHigh);
				SetUndoControlCounter ();
			}
		}
		#endregion
	}
}