using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

using Broccoli.Base;
using Broccoli.Factory;
using Broccoli.Utils;
using Broccoli.Catalog;
using Broccoli.Pipe;
using Broccoli.Serialization;
using Broccoli.BroccoEditor;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Tree factory editor window.
	/// Contains the canvas to edit pipeline element nodes and
	/// all the available tree factory commands.
	/// </summary>
	public class TreeFactoryEditorWindow : EditorWindow 
	{
		#region Vars
		/// <summary>
		/// The tree factory game object.
		/// </summary>
		public GameObject treeFactoryGO;
		/// <summary>
		/// The tree factory behind the the canvas.
		/// </summary>
		public TreeFactory treeFactory;
		int currentUndoGroup = 0;
		/// <summary>
		/// The tree factory serialized object behind the the canvas.
		/// </summary>
		public SerializedObject serializedTreeFactory;
		/// <summary>
		/// The property appendable components.
		/// </summary>
		public SerializedProperty propAppendableComponents;
		/// <summary>
		/// Pipeline graph view.
		/// </summary>
		public PipelineGraphView pipelineGraph;
		/// <summary>
		/// The width of the side window.
		/// </summary>
		private int sidePanelWidth = 400;
		/// <summary>
		/// 
		/// </summary>
		private int titleHeight = (int)EditorGUIUtility.singleLineHeight + 3;
		/// <summary>
		/// Gets the window rect.
		/// </summary>
		/// <value>The total window rect.</value>
		public Rect windowRect { 
			get { 
				return new Rect (0, 0, position.width, position.height); 
			} 
		}
		/// <summary>
		/// Gets the canvas panel rect.
		/// </summary>
		/// <value>The canvas panel rect.</value>
		public Rect canvasPanelRect { 
			get { 
				return new Rect (0, 0, 
					position.width - sidePanelWidth, position.height); 
			} 
		}
		/// <summary>
		/// Gets the side window rect.
		/// </summary>
		/// <value>The side window rect.</value>
		public Rect sidePanelRect { 
			get { 
				return new Rect (position.width - sidePanelWidth, 0, 
					sidePanelWidth, position.height); 
			} 
		}
		/// <summary>
		/// Gets the canvas window rect.
		/// </summary>
		/// <value>The canvas window rect.</value>
		public Rect canvasRect { 
			get { 
				return new Rect (0, titleHeight, position.width - sidePanelWidth, 
					position.height); 
			} 
		}
		/// <summary>
		/// The catalog of tree pipelines.
		/// </summary>
		public BroccoliCatalog catalog;
		public enum EditorView {
			MainOptions,
			FactoryOptions,
			Catalog
		}
		EditorView editorView = EditorView.MainOptions;
		/// <summary>
		/// True when the editor is on play mode.
		/// </summary>
		private bool isPlayModeView = false;
		/// <summary>
		/// The size of the catalog items.
		/// </summary>
		public static int catalogItemSize = 100;
		/// <summary>
		/// The sprout groups reorderable list.
		/// </summary>
		ReorderableList sproutGroupList;
		/// <summary>
		/// The appendable scripts reorderable list.
		/// </summary>
		BReorderableList appendableComponentList;
		/// <summary>
		/// Preview options GUIContent array.
		/// </summary>
		private static GUIContent[] previewOptions = new GUIContent[2];
		/// <summary>
		/// The prefab texture options GUIContent array.
		/// </summary>
		private static GUIContent[] prefabTextureOptions = new GUIContent[2];
		/// <summary>
		/// Saves the vertical scroll position for the side panel.
		/// </summary>
		private Vector2 sidePanelScroll;
		/// <summary>
		/// Saves the vertical scroll position for the catalog.
		/// </summary>
		private Vector2 catalogScroll;
		/// <summary>
		/// Displays the state of the loaded pipeline.
		/// </summary>
		private string pipelineLegend;
		#endregion

		#region Messages
		static string MSG_NOT_LOADED = "To edit a pipeline: please select a Broccoli Tree " +
			"Factory GameObject then press 'Open Broccoli Tree Editor' on the script inspector.";
		static string MSG_PLAY_MODE = "Tree factory node editor is not available on play mode.";
		static string MSG_LOAD_CATALOG_ITEM_TITLE = "Load catalog item";
		static string MSG_LOAD_CATALOG_ITEM_MESSAGE = "Do you really want to load this item? " +
			"You will lose any change not saved in the current pipeline.";
		static string MSG_LOAD_CATALOG_ITEM_OK = "Load Pipeline";
		static string MSG_LOAD_CATALOG_ITEM_CANCEL = "Cancel";
		static string MSG_DELETE_SPROUT_GROUP_TITLE = "Remove Sprout Group";
		static string MSG_DELETE_SPROUT_GROUP_MESSAGE = "Do you really want to remove this sprout group? " +
			"All meshes and maps assigned to it will be left unassigned.";
		static string MSG_DELETE_SPROUT_GROUP_OK = "Remove Sprout Group";
		static string MSG_DELETE_SPROUT_GROUP_CANCEL = "Cancel";
		static string MSG_NEW_PIPELINE_TITLE = "New Pipeline";
		static string MSG_NEW_PIPELINE_MESSAGE = "By creating a new pipeline you will lose any changes " +
			"not saved on the current one. Do you want to continue creating a new pipeline?";
		static string MSG_NEW_PIPELINE_OK = "Yes, create new pipeline";
		static string MSG_NEW_PIPELINE_CANCEL = "No";
		#endregion

		#region GUI Vars
		private static bool isWindowInit = false;
		private static GUIContent fromCatalogBtn;
		private static GUIContent advancedOptionsBtn;
		private static GUIContent generateNewPreviewBtn;
		private static GUIContent createPrefabBtn;
		private static GUIContent createNewPipelineBtn;
		private static GUIContent loadFromAssetBtn;
		private static GUIContent saveAsNewAssetBtn;
		private static GUIContent closeCatalogBtn;
		private static GUIContent closeAdvancedOptionsBtn;
		private static GUIContent saveBtn;
		private static GUIContent regeneratePreviewBtn;
		private static GUIContent generateWithCustomSeedBtn;
		private static string prefabOptionsTitle = "Prefab Options";
		private static GUIContent prefabCloneMaterialsLabel = new GUIContent ("Clone custom materials", 
			"When using custom materials on branch or sprout meshes, the materials assigned to the resulting prefab are cloned from the original material assets.");
		private static GUIContent prefabIncludeAssetsInPrefabLabel = new GUIContent ("Include materials and meshes \ninside the Prefab",
			"If enabled then material and mesh assets will be included inside the Prefab instead of inside a folder belonging to the Prefab.");
		private static GUIContent prefabCopyBranchTexturesLabel = new GUIContent ("Copy branch textures \nto Prefab folder", 
			"If enabled then textures belonging to the branch mapping with be copied to the Prefab folder.");
		private static GUIContent prefabCreateSproutAtlasLabel = new GUIContent ("Create sprouts texture atlas", 
			"Generates a texture atlas packing all the textures assigned to sprout meshes in the prefab.");
		private static GUIContent prefabAtlasSizeLabel = new GUIContent ("Atlas Size", "Size in pixels for the atlas texture.");
		private static GUIContent prefabSavePathLabel = new GUIContent ("Path", "Path to save the generated prefabs.");
		private static GUIContent prefabSavePrefixLabel = new GUIContent ("Prefix", "Prefix for the generated prefabs.");
		private static string prefabButtonLabel = "...";
		private static string prefabMeshesTitle = "Prefab Meshes";
		private static GUIContent billboardTextureLabel = new GUIContent ("Billboard Texture", 
			"Size of the billboard texture, if you are using a far distance to show the billboard LOD, then a small size ir preferable.");
		private static GUIContent repositionEnabledLabel = new GUIContent ("Re-position prefab mesh \nto zero if the tree has \na single root.", 
			"When using multiple positions to generate trunks within a radius, this options sets a unique trunk to zero in the mesh space.");
		private static string selectSavePrefabPathTitle = "Select the path to save Prefabs to";
		#endregion

		#region Singleton
		/// <summary>
		/// The editor window singleton.
		/// </summary>
		private static TreeFactoryEditorWindow _factoryWindow;
		/// <summary>
		/// Gets the editor window.
		/// </summary>
		/// <value>The editor.</value>
		public static TreeFactoryEditorWindow editorWindow { get { return _factoryWindow; } }
		/// <summary>
		/// Gets the tree editor window.
		/// </summary>
		static void GetWindow () {
			_factoryWindow = GetWindow<TreeFactoryEditorWindow> ();
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Static initializer for the <see cref="Broccoli.TreeNodeEditor.TreeFactoryEditorWindow"/> class.
		/// </summary>
		static TreeFactoryEditorWindow () {
			previewOptions [0] = 
				new GUIContent ("Textured", "Display textured meshes.");
			previewOptions [1] = 
				new GUIContent ("Colored", "Display colored meshes without textures.");
			prefabTextureOptions [0] = 
				new GUIContent ("Original", "Keep the provided texture or atlases on the materials.");
			prefabTextureOptions [1] = 
				new GUIContent ("New Atlas", "Create separated atlases for sprouts and branches if necessary.");
		}
		#endregion

		#region Open / Close
		/// <summary>
		/// Opens the tree factory window.
		/// </summary>
		[MenuItem("Window/Broccoli/Tree Factory Editor")]
		static void OpenTreeFactoryWindow ()
		{
			TreeFactory treeFactory = null;
			if (Selection.activeGameObject != null) {
				treeFactory = Selection.activeGameObject.GetComponent<TreeFactory> ();
			}
			OpenTreeFactoryWindow (treeFactory);
		}
		/// <summary>
		/// Checks if the menu item for opening the Tree Editor should be enabled.
		/// </summary>
		/// <returns><c>true</c>, if tree factory window is closed, <c>false</c> otherwise.</returns>
		[MenuItem("Tools/Broccoli Tree Editor", true)]
		static bool ValidateOpenTreeFactoryWindow ()
		{
			return !IsOpen ();
		}
		/// <summary>
		/// Opens the Node Editor window loading the pipeline contained on the TreeFactory object.
		/// </summary>
		/// <returns>The node editor.</returns>
		/// <param name="treeFactory">Tree factory.</param>
		public static TreeFactoryEditorWindow OpenTreeFactoryWindow (TreeFactory treeFactory = null) 
		{
			GUITextureManager.Init ();
			BroccoEditorGUI.Init ();
			
			GetWindow ();

			// Initialize GUI Labels.
			fromCatalogBtn = new GUIContent ("From Catalog", GUITextureManager.folderBtnTexture, "Opens the catalog to select a predefined pipeline to work with.");
			advancedOptionsBtn = new GUIContent("Advanced Options", "Show the options available on the prefab creation process.");
			generateNewPreviewBtn = new GUIContent ("Generate New Preview", GUITextureManager.newPreviewBtnTexture, "Process the pipeline to generate a new preview tree.");
			createPrefabBtn = new GUIContent("Create Prefab", GUITextureManager.createPrefabBtnTexture, "Creates a prefab out of the preview tree processed by the pipeline.");
			createNewPipelineBtn = new GUIContent ("Create New Pipeline", "Creates a new pipeline to work with.");
			loadFromAssetBtn = new GUIContent ("Load From Asset", "Load the Pipeline from an asset.");
			saveAsNewAssetBtn = new GUIContent ("Save As New Asset", "Save the Pipeline as an asset.");
			closeCatalogBtn = new GUIContent ("Close Catalog", "Close the Catalog View and returns to the Main View.");
			closeAdvancedOptionsBtn = new GUIContent ("Close Advanced Options", "Close the Advanced View and returns to the Main View.");
			saveBtn = new GUIContent ("Save", "Saves the current pipeline to its ScriptableObject file.");
			regeneratePreviewBtn = new GUIContent ("Regenerate Preview", "Regenerates the preview tree with the current seed.");
			generateWithCustomSeedBtn = new GUIContent ("Generate with Custom Seed", "Regenerate the preview tree with an specified seed.");

			if (EditorApplication.isPlayingOrWillChangePlaymode && !GlobalSettings.useTreeEditorOnPlayMode) {
				_factoryWindow.isPlayModeView = true;
			} else {
				_factoryWindow.isPlayModeView = false;
			}

			//if (treeFactory != null && treeFactory != _factoryWindow.treeFactory) {
			if (treeFactory != null) {
				SetupTreeFactory (treeFactory, _factoryWindow);
				SetupGraphView (treeFactory, _factoryWindow);
			}

			/*
			Texture iconTexture = 
				ResourceManager.LoadTexture (EditorGUIUtility.isProSkin? "Textures/Icon_Dark.png" : "Textures/Icon_Light.png");
			_factoryWindow.titleContent = new GUIContent ("Tree Editor", iconTexture);
			*/
			// TODO 2023
			_factoryWindow.titleContent = new GUIContent ("Tree Editor");

			isWindowInit = true;

			return _factoryWindow;
		}
		/// <summary>
		/// Prepares a TreeFactory instance to be loaded in the TreeFactoryEditorWindow.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="factoryWindow">Factory window.</param>
		public static void SetupTreeFactory (TreeFactory treeFactory, TreeFactoryEditorWindow factoryWindow) {
			// Validates the pipeline.
			if (treeFactory.localPipeline == null) {
				treeFactory.Init ();
			}
			if (treeFactory.localPipeline == null) return;
			
			treeFactory.localPipeline.Validate ();

			// Initializes TreeFactory objects.
			treeFactory.materialManager.SetBranchShader (treeFactory.treeFactoryPreferences.preferredShader, treeFactory.treeFactoryPreferences.customBranchShader);
			treeFactory.materialManager.SetLeavesShader (treeFactory.treeFactoryPreferences.preferredShader, treeFactory.treeFactoryPreferences.customSproutShader);
			treeFactory.materialManager.SetBillboardShader (treeFactory.treeFactoryPreferences.preferredShader);

			// Assigns the TreeFactory to this window.
			factoryWindow.treeFactory = treeFactory;
			factoryWindow.treeFactoryGO = treeFactory.gameObject;
			treeFactory.SetInstanceAsActive ();
			factoryWindow.serializedTreeFactory = new SerializedObject (treeFactory);
			factoryWindow.propAppendableComponents = 
				factoryWindow.serializedTreeFactory.FindProperty ("treeFactoryPreferences.appendableComponents");

			factoryWindow.minSize = new Vector2 (400, 200);

			if (treeFactory.localPipeline != null) {
				factoryWindow.InitSproutGroupList ();
				factoryWindow.InitAppendableComponentList ();
			}

			factoryWindow.SetEditorView ((TreeFactoryEditorWindow.EditorView)treeFactory.treeFactoryPreferences.editorView);
		}
		/// <summary>
		/// Setups the PipelineGraphView UI element to be used in this window.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="factoryWindow">Factory window.</param>
		public static void SetupGraphView (TreeFactory treeFactory, TreeFactoryEditorWindow factoryWindow) {
			// Create the Pipeline Graph View if not existant.
			if (factoryWindow.pipelineGraph == null) {
				/*
				factoryWindow.pipelineGraph = new PipelineGraphView() { 
					name = "Broccoli Pipeline Graph", 
					viewDataKey = "BroccoPipelineGraphView"};
					*/
				factoryWindow.pipelineGraph = new PipelineGraphView() { 
					name = "Broccoli Pipeline Graph"};
				factoryWindow.BindPipelineGraphEvents (factoryWindow.pipelineGraph);
				factoryWindow.pipelineGraph.Init (
					treeFactory.treeFactoryPreferences.graphOffset, 
					/*treeFactory.treeFactoryPreferences.graphZoom*/ 1f);
				factoryWindow.pipelineGraph.SetTitle (treeFactory.gameObject.name);
				factoryWindow.rootVisualElement.Add (factoryWindow.pipelineGraph);
				factoryWindow.pipelineGraph.StretchToParentSize ();
			} else {
				//factoryWindow.pipelineGraph.Clear ();
			}
			factoryWindow.pipelineGraph.LoadPipeline (treeFactory.localPipeline, 
				treeFactory.treeFactoryPreferences.graphOffset, 
				/*treeFactory.treeFactoryPreferences.graphZoom*/1f);
		}
		/// <summary>
		/// Unloads the tree factory instance associated to this window.
		/// </summary>
		void UnloadFactory () {
			treeFactory = null;
		}
		/// <summary>
		/// Determines if the editor is open.
		/// </summary>
		/// <returns><c>true</c> if is open; otherwise, <c>false</c>.</returns>
		public static bool IsOpen () {
			if (editorWindow == null)
				return false;
			return true;
		}
		/// <summary>
		/// Sets the view to the editor and persists it to the factory preferences.
		/// </summary>
		/// <param name="editorViewToSet">Editor view mode.</param>
		public void SetEditorView (EditorView editorViewToSet) {
			editorView = editorViewToSet;
			treeFactory.treeFactoryPreferences.editorView = (int)editorView;
		}
		#endregion

		#region Events
		/// <summary>
		/// Raises the enable event.
		/// </summary>
		void OnEnable ()
		{
			_factoryWindow = this;

			/*
			NodeEditor.ClientRepaints -= Repaint;
			NodeEditor.ClientRepaints += Repaint;

			EditorLoadingControl.beforeEnteringPlayMode -= OnBeforeEnteringPlayMode;
			EditorLoadingControl.beforeEnteringPlayMode += OnBeforeEnteringPlayMode;

			EditorLoadingControl.justLeftPlayMode -= OnJustLeftPlayMode;
			EditorLoadingControl.justLeftPlayMode += OnJustLeftPlayMode;
			// Here, both justLeftPlayMode and justOpenedNewScene have to act because of timing
			EditorLoadingControl.justOpenedNewScene -= OnJustOpenedNewScene;
			EditorLoadingControl.justOpenedNewScene += OnJustOpenedNewScene;
			*/
			// TODO 2023

			UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnEditorSceneManagerSceneOpened;
			UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
		}
		/// <summary>
		/// Raises the disable event.
		/// </summary>
		void OnDisable () {
			UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnEditorSceneManagerSceneOpened;
		}
		/// <summary>
		/// Called when a scene opens in the editor.
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="mode"></param>
		void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) {
			GUITextureManager.Init (true);
			BroccoEditorGUI.Init (true);
		}
		/// <summary>
		/// Raises the before entering play mode event.
		/// </summary>
		void OnBeforeEnteringPlayMode () {
			if (!GlobalSettings.useTreeEditorOnPlayMode) {
				isPlayModeView = true;
				//GUITextureManager.Clear ();
			}
		}
		/// <summary>
		/// Raises the just left play mode event.
		/// </summary>
		void OnJustLeftPlayMode () {
			isPlayModeView = false;
			if (treeFactory == null && treeFactoryGO != null) {
				treeFactory = treeFactoryGO.GetComponent<TreeFactory> ();
			}
			GUITextureManager.Init (true);
		}
		/// <summary>
		/// Raises the just opened new scene event.
		/// </summary>
		void OnJustOpenedNewScene () {}
		/// <summary>
		/// Raises the hierarchy change event.
		/// </summary>
		void OnHierarchyChange () {}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		private void OnDestroy () {
			/*
			NodeEditor.ClientRepaints -= Repaint;

			EditorLoadingControl.beforeEnteringPlayMode -= OnBeforeEnteringPlayMode;
			EditorLoadingControl.justLeftPlayMode -= OnJustLeftPlayMode;
			EditorLoadingControl.justOpenedNewScene -= OnJustOpenedNewScene;
			TODO 2023
			*/

			GUITextureManager.Clear ();

			UnloadFactory ();
		}
		#endregion

		#region GUI Events and Editor Methods
		/// <summary>
		/// Raises the GUI main draw event.
		/// </summary>
		private void OnGUI () {
			//GUITextureManager.Init ();
			//BroccoEditorGUI.Init (); 


			if (EventType.ValidateCommand == Event.current.type &&
				"UndoRedoPerformed" == Event.current.commandName) {
				if (treeFactory.lastUndoProcessed !=
				    treeFactory.localPipeline.undoControl.undoCount) {
					treeFactory.RequestPipelineUpdate ();
					treeFactory.lastUndoProcessed = treeFactory.localPipeline.undoControl.undoCount;
				}
				if (pipelineGraph.lastUndoProcessed !=
				treeFactory.localPipeline.undoControl.undoCount) {
					pipelineGraph.UpdatePipeline ();
				}
			}

			if (isPlayModeView) {
				DrawPlayModeView ();
				return;
			}

			// Editor view on canvas mode.
			if (editorView == EditorView.Catalog) {
				GUILayout.BeginArea (windowRect);
				DrawCatalogPanel ();
				GUILayout.EndArea ();
				return;
			}

			// Canvas is not initialized.
			if (!isWindowInit || treeFactory == null) {
				DrawNotLoadedView ();
				if (Selection.activeGameObject != null &&
					Selection.activeGameObject.GetComponent<TreeFactory> () != null) {
					OpenTreeFactoryWindow (Selection.activeGameObject.GetComponent<TreeFactory> ());
				} else {
					return;
				}
			}

			pipelineGraph.style.display = DisplayStyle.Flex;
			pipelineGraph.style.marginRight = windowRect.width - canvasRect.width;

			// Draw Side Window
			sidePanelWidth = Math.Min (600, Math.Max(200, (int)(position.width / 5)));

			GUILayout.BeginArea (canvasPanelRect);
			EditorGUILayout.LabelField (treeFactory.gameObject.name, BroccoEditorGUI.labelBoldCentered);
			GUILayout.EndArea ();
			
			GUILayout.BeginArea (sidePanelRect);
			EditorGUILayout.BeginHorizontal ();
			sidePanelScroll = EditorGUILayout.BeginScrollView (sidePanelScroll, GUIStyle.none, TreeCanvasGUI.verticalScrollStyle);
			if (editorView == EditorView.FactoryOptions) {
				DrawFactoryOptionsPanel ();
			} else {
				DrawSidePanel ();
			}
			EditorGUILayout.EndScrollView ();
			EditorGUILayout.EndHorizontal ();
			GUILayout.EndArea ();

			if (pipelineGraph.isDirty) {
				pipelineGraph.isDirty = false;
				EditorUtility.SetDirty (treeFactory);
				if (!Application.isPlaying) {
					EditorSceneManager.MarkAllScenesDirty ();
				}
			}
		}
		/// <summary>
		/// Clears a loaded pipeline.
		/// </summary>
		void ClearPipeline () {
			if (treeFactory.localPipeline != null) {
				List<PipelineElement> pipelineElements = treeFactory.localPipeline.GetElements ();
				for (int i = 0; i < pipelineElements.Count; i++) {
					Undo.ClearUndo (pipelineElements[i]);
				}
				Undo.ClearUndo (treeFactory.localPipeline);
			}
			
			pipelineGraph.ClearElements ();
				
			treeFactory.UnloadAndClearPipeline ();
		}
		#endregion

		#region Sprout Group List
		/// <summary>
		/// Inits the sprout group list.
		/// </summary>
		private void InitSproutGroupList () {
			sproutGroupList = 
				new ReorderableList (treeFactory.localPipeline.sproutGroups.GetSproutGroups (), 
					typeof (SproutGroups.SproutGroup), false, true, true, true);
			sproutGroupList.draggable = false;
			sproutGroupList.drawHeaderCallback += DrawSproutGroupListHeader;
			sproutGroupList.drawElementCallback += DrawSproutGroupListItemElement;
			sproutGroupList.onAddCallback += AddSproutGroupListItem;
			sproutGroupList.onRemoveCallback += RemoveSproutGroupListItem;
		}
		/// <summary>
		/// Draws the sprout group list header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawSproutGroupListHeader (Rect rect) {
			GUI.Label(rect, "Sprout Groups", BroccoEditorGUI.labelBoldCentered);
		}
		/// <summary>
		/// Draws each sprout group list item element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawSproutGroupListItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			SproutGroups.SproutGroup sproutGroup = 
				treeFactory.localPipeline.sproutGroups.GetSproutGroupAtIndex (index);
			if (sproutGroup != null) {
				rect.y += 2;
				EditorGUI.DrawRect (new Rect (rect.x, rect.y, EditorGUIUtility.singleLineHeight, 
					EditorGUIUtility.singleLineHeight), sproutGroup.GetColor ());
				rect.x += 22;
				rect.y -= 2;
				GUI.Label (new Rect (rect.x, rect.y, 150, EditorGUIUtility.singleLineHeight + 5), 
					"Sprout Group " + sproutGroup.id);
			}
		}
		/// <summary>
		/// Adds the sprout group list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void AddSproutGroupListItem (ReorderableList list) {
			if (treeFactory.localPipeline.sproutGroups.CanCreateSproutGroup ()) {
				Undo.RecordObject (treeFactory.localPipeline, "Sprout Group Added");
				treeFactory.localPipeline.sproutGroups.CreateSproutGroup ();
			}
		}
		/// <summary>
		/// Removes the sprout group list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void RemoveSproutGroupListItem (ReorderableList list) {
			SproutGroups.SproutGroup sproutGroup = treeFactory.localPipeline.sproutGroups.GetSproutGroupAtIndex (list.index);
			if (sproutGroup != null) {
				bool hasUsage = treeFactory.localPipeline.HasSproutGroupUsage (sproutGroup.id);
				if ((hasUsage && EditorUtility.DisplayDialog (MSG_DELETE_SPROUT_GROUP_TITLE, 
					MSG_DELETE_SPROUT_GROUP_MESSAGE, 
					MSG_DELETE_SPROUT_GROUP_OK, 
					MSG_DELETE_SPROUT_GROUP_CANCEL)) || !hasUsage) {
					Undo.SetCurrentGroupName( "Zero out selected gameObjects" );
					int group = Undo.GetCurrentGroup();
					Undo.RecordObject (treeFactory.localPipeline, "Sprout Group Removed");
					treeFactory.localPipeline.DeleteSproutGroup (sproutGroup.id);
					Undo.CollapseUndoOperations( group );
				}
			}
		}
		#endregion

		#region Appendable Scripts
		/// <summary>
		/// Inits the appendable component list.
		/// </summary>
		private void InitAppendableComponentList () {
			appendableComponentList = 
				new BReorderableList (serializedTreeFactory, propAppendableComponents, false, true, true, true);
			appendableComponentList.drawHeaderCallback += DrawAppendableComponentListHeader;
			appendableComponentList.drawElementCallback += DrawAppendableComponentListItemElement;
			appendableComponentList.onAddCallback += AddAppendableComponentListItem;
			appendableComponentList.onRemoveCallback += RemoveAppendableComponentListItem;
		}
		/// <summary>
		/// Draws the appendable component list header.
		/// </summary>
		/// <param name="rect">Rect.</param>
		private void DrawAppendableComponentListHeader (Rect rect) {
			GUI.Label(rect, "Appendable Scripts", BroccoEditorGUI.labelBoldCentered);
		}
		/// <summary>
		/// Draws the appendable component list item element.
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <param name="index">Index.</param>
		/// <param name="isActive">If set to <c>true</c> is active.</param>
		/// <param name="isFocused">If set to <c>true</c> is focused.</param>
		private void DrawAppendableComponentListItemElement (Rect rect, int index, bool isActive, bool isFocused) {
			rect.x += 10;
			rect.y += 3;
			rect.width -= 10;
			rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField (rect, propAppendableComponents.GetArrayElementAtIndex (index), GUIContent.none);
		}
		/// <summary>
		/// Adds an appendable component list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void AddAppendableComponentListItem (BReorderableList list) {
			propAppendableComponents.InsertArrayElementAtIndex (list.count);
		}
		/// <summary>
		/// Removes an appendable component list item.
		/// </summary>
		/// <param name="list">List.</param>
		private void RemoveAppendableComponentListItem (BReorderableList list) {
			propAppendableComponents.DeleteArrayElementAtIndex (list.index);
		}
		#endregion

		#region Pipeline Operations
		/// <summary>
		/// Loads a new pipeline.
		/// </summary>
		private void LoadNewPipeline () {
			ClearPipeline ();
			if (!GlobalSettings.useTemplateOnCreateNewPipeline) {
				treeFactory.LoadPipeline (ScriptableObject.CreateInstance<Broccoli.Pipe.Pipeline> (), true);
			} else {
				LoadPipelineAsset (ExtensionManager.fullExtensionPath + GlobalSettings.templateOnCreateNewPipelinePath);
				treeFactory.localPipelineFilepath = "";
			}
			if (GlobalSettings.moveCameraToPipeline) {
				SceneView.lastActiveSceneView.LookAt (treeFactory.transform.position);
			}
			OpenTreeFactoryWindow (treeFactory);
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (
				UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ());
		}
		/// <summary>
		/// Loads a pipeline from an asset file.
		/// </summary>
		/// <param name="pathToAsset">Path to asset.</param>
		/// <param name="fromCatalog">If the Pipeline comes from the catalog.</param>
		private void LoadPipelineAsset (string pathToAsset, bool fromCatalog = false) {
			if (!pathToAsset.Contains (Application.dataPath)) {
				if (!string.IsNullOrEmpty (pathToAsset))
					ShowNotification (
						new GUIContent ("You should select an asset inside your project folder!"));
			} else {
				pathToAsset = pathToAsset.Replace(Application.dataPath, "Assets");
				AssetDatabase.Refresh ();

				Broccoli.Pipe.Pipeline loadedPipeline =
					AssetDatabase.LoadAssetAtPath<Broccoli.Pipe.Pipeline> (pathToAsset);
				ProcessLoadedPipelineAsset (loadedPipeline, fromCatalog);

				if (loadedPipeline == null) {
					throw new UnityException ("Cannot Load Pipeline: The file at the specified path '" + 
						pathToAsset + "' is no valid save file as it does not contain a Pipeline.");
				} else {
					ClearPipeline ();
					treeFactory.UnloadAndClearPipeline ();
					
					treeFactory.LoadPipeline (loadedPipeline.Clone (), pathToAsset, true , true);
					if (treeFactory.previewTree != null && treeFactory.previewTree.obj != null) {
						Selection.activeGameObject = treeFactory.gameObject;
						if (GlobalSettings.moveCameraToPipeline) {
							SceneView.FrameLastActiveSceneView ();
						}
					} else if (GlobalSettings.moveCameraToPipeline) {
						SceneView.lastActiveSceneView.LookAt (treeFactory.transform.position);
					}
					Resources.UnloadAsset (loadedPipeline);
					OpenTreeFactoryWindow (treeFactory);
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (
						UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ());
				}
			}
		}
		/// <summary>
		/// Process a just loaded Pipeline.
		/// </summary>
		/// <param name="loadedPipeline">Pipeline.</param>
		/// <param name="fromCatalog">If the Pipeline comes from the catalog.</param>
		private void ProcessLoadedPipelineAsset (Broccoli.Pipe.Pipeline loadedPipeline, bool fromCatalog) {
			if (loadedPipeline != null) {
				// If the pipeline comes from the catalog and is HDRP, set a default diffusion profile.
				if (fromCatalog && 
					Broccoli.Manager.MaterialManager.renderPipelineType == Broccoli.Manager.MaterialManager.RenderPipelineType.HDRP)
				{
					Broccoli.Manager.MaterialManager.FindDefaultDiffusionProfile ();
					// Set diffusion profile on Branch Mapper.
					List<Broccoli.Pipe.PipelineElement> branchMappers = loadedPipeline.GetElements (Broccoli.Pipe.PipelineElement.ClassType.BranchMapper);
					for (int i = 0; i < branchMappers.Count; i++) {
						Broccoli.Pipe.BranchMapperElement branchMapper = (Broccoli.Pipe.BranchMapperElement)branchMappers[i];
						branchMapper.diffusionProfileSettings = Broccoli.Manager.MaterialManager.defaultDiffusionProfile;
					}
					// Set diffusion profile on Sprout Mapper.
					List<Broccoli.Pipe.PipelineElement> sproutMappers = loadedPipeline.GetElements (Broccoli.Pipe.PipelineElement.ClassType.SproutMapper);
					for (int i = 0; i < sproutMappers.Count; i++) {
						Broccoli.Pipe.SproutMapperElement sproutMapper = (Broccoli.Pipe.SproutMapperElement)sproutMappers[i];
						for (int j = 0; j < sproutMapper.sproutMaps.Count; j++) {
							sproutMapper.sproutMaps [j].diffusionProfileSettings = Broccoli.Manager.MaterialManager.defaultDiffusionProfile;
						}
					}
				}
			}
		}
		/// <summary>
		/// Saves the pipeline asset.
		/// </summary>
		/// <returns><c>true</c>, if pipeline asset was saved, <c>false</c> otherwise.</returns>
		/// <param name="pathToAsset">Path to asset.</param>
		/// <param name="asNewAsset">If set to <c>true</c> as new asset.</param>
		private bool SavePipelineAsset (string pathToAsset, bool asNewAsset = false) {
			if (!string.IsNullOrEmpty (pathToAsset)) {
				try {
					Broccoli.Pipe.Pipeline pipelineToAsset = 
						AssetDatabase.LoadAssetAtPath<Broccoli.Pipe.Pipeline> (pathToAsset);
					if (pipelineToAsset != null && asNewAsset) {
						AssetDatabase.DeleteAsset (pathToAsset);
						DestroyImmediate (pipelineToAsset, true);
					}
					pipelineToAsset = treeFactory.localPipeline.Clone (pipelineToAsset);
					if (pipelineToAsset.isCatalogItem && 
						GlobalSettings.editCatalogEnabled == false && 
						asNewAsset) 
					{
						pipelineToAsset.isCatalogItem = false;
					}
					pipelineToAsset.treeFactoryPreferences = treeFactory.treeFactoryPreferences.Clone ();
					if (asNewAsset) {
						AssetDatabase.CreateAsset (pipelineToAsset, pathToAsset);
					} else {
						// Delete any old sub objects inside a main asset.
						UnityEngine.Object[] subAssets = 
							UnityEditor.AssetDatabase.LoadAllAssetsAtPath(pathToAsset);
						for (int i = 0; i < subAssets.Length; i++) {
							if (subAssets [i] is PipelineElement) {
								UnityEngine.Object.DestroyImmediate (subAssets [i], true);
							}
						}
						EditorUtility.SetDirty (pipelineToAsset);
					}
					
					List<PipelineElement> pipelineElements = pipelineToAsset.GetElements ();
					for (int i = 0; i < pipelineElements.Count; i++) {
						AssetDatabase.AddObjectToAsset (pipelineElements[i], pathToAsset);
					}
					AssetDatabase.SaveAssets ();
					Resources.UnloadAsset (pipelineToAsset);
					//DestroyImmediate (pipelineToAsset, true);
				} catch (UnityException e) {
					Debug.LogException (e);
					return false;
				}

				EditorUtility.FocusProjectWindow ();
				return true;
			}
			return false;
		}
		#endregion

		#region Draw Functions
		/// <summary>
		/// Draws the side window.
		/// </summary>
		private void DrawSidePanel ()
		{
			DrawLogo ();
			EditorGUILayout.Space ();
			// Empty pipeline.
			if (treeFactory.HasEmptyPipeline ()) {
				DrawEmptyPipelineOptions ();
			}
			// Pipeline with elements.
			else {
				EditorGUI.BeginDisabledGroup (!treeFactory.HasValidPipeline ());
				DrawProcessingOptions ();
				EditorGUI.EndDisabledGroup ();
				EditorGUILayout.Space ();
				DrawPersistenceOptions ();
				EditorGUILayout.Space ();
				DrawShowCatalogOptions ();
				EditorGUILayout.Space ();
				DrawSproutGroupList ();
				EditorGUILayout.Space ();
				DrawDebugOptions ();
				EditorGUILayout.Space ();
				DrawShowPrefabOptions ();
				EditorGUILayout.Space ();
				DrawRateMe ();
				EditorGUILayout.Space ();
			}
			DrawBroccoliVersion ();
			EditorGUILayout.Space ();
			DrawLogBox ();
		}
		/// <summary>
		/// Draws the catalog.
		/// </summary>
		private void DrawCatalogPanel () {
			pipelineGraph.style.display = DisplayStyle.None;
				
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Box ("", GUIStyle.none, 
				GUILayout.Width (150), 
				GUILayout.Height (60));
			GUI.DrawTexture (new Rect (5, 8, 140, 48), GUITextureManager.GetBroccoliLogo (), ScaleMode.ScaleToFit);
			string catalogMsg = "Broccoli Tree Creator Catalog.\n v 1.0\n\nShowing " + catalog.totalItems + 
				" elements in " + catalog.totalCategories + " categories.";
			EditorGUILayout.HelpBox (catalogMsg, MessageType.None);
			EditorGUILayout.EndHorizontal ();
			if (GUILayout.Button (closeCatalogBtn)) {
				SetEditorView (EditorView.MainOptions);
			}
			catalogScroll = EditorGUILayout.BeginScrollView (catalogScroll, GUIStyle.none, TreeCanvasGUI.verticalScrollStyle);
			if (catalog.GetGUIContents ().Count > 0) {
				string categoryKey = "";
				var enumerator = catalog.contents.GetEnumerator ();
				while (enumerator.MoveNext ()) {
					var contentPair = enumerator.Current;
					categoryKey = contentPair.Key;
					EditorGUILayout.LabelField (categoryKey, BroccoEditorGUI.label);
					int columns = Mathf.CeilToInt ((windowRect.width - 8) / catalogItemSize);
					int height = Mathf.CeilToInt (catalog.GetGUIContents ()[categoryKey].Count / (float)columns) * catalogItemSize;
					int selectedIndex = 
						GUILayout.SelectionGrid (-1, catalog.GetGUIContents ()[categoryKey].ToArray (), 
							columns, TreeCanvasGUI.catalogItemStyle, GUILayout.Height (height), GUILayout.Width (windowRect.width - 8));
					if (selectedIndex >= 0 &&
					   EditorUtility.DisplayDialog (MSG_LOAD_CATALOG_ITEM_TITLE, 
						   MSG_LOAD_CATALOG_ITEM_MESSAGE, 
						   MSG_LOAD_CATALOG_ITEM_OK, 
						   MSG_LOAD_CATALOG_ITEM_CANCEL)) {
						SetEditorView (EditorView.MainOptions);
						LoadPipelineAsset (ExtensionManager.fullExtensionPath + 
							catalog.GetItemAtIndex (categoryKey, selectedIndex).path, true);
					}
				}
			}
			EditorGUILayout.EndScrollView ();
		}

		/// <summary>
		/// Draws the factory options panel.
		/// </summary>
		private void DrawFactoryOptionsPanel () {
			DrawLogo ();
			EditorGUILayout.Space ();
			if (GUILayout.Button (closeAdvancedOptionsBtn)) {
				SetEditorView (EditorView.MainOptions);
			}
			EditorGUILayout.Space ();
			DrawFactoryOptions ();
			EditorGUILayout.Space ();
			DrawMaterialOptions ();
			EditorGUILayout.Space ();
			DrawPrefabOptions ();
			if (GlobalSettings.editCatalogEnabled) { 
				// Must check against false, otherwise it raises a warning for unreachable code (const var).
				EditorGUILayout.Space ();
				treeFactory.localPipeline.isCatalogItem = 
					EditorGUILayout.Toggle ("Pipeline is a catalog item", treeFactory.localPipeline.isCatalogItem);
			}
			EditorGUILayout.Space ();
			DrawBroccoliVersion ();
			EditorGUILayout.Space ();
			DrawLogBox ();
		}
		/// <summary>
		/// Default view when no valid treeFactory is assigned to the window.
		/// </summary>
		private void DrawNotLoadedView () {
			if (pipelineGraph != null) {
				pipelineGraph.style.display = DisplayStyle.None;
			}
			EditorGUILayout.HelpBox(MSG_NOT_LOADED, MessageType.Warning);
		}
		/// <summary>
		/// Default view when in play mode.
		/// </summary>
		private void DrawPlayModeView () {
			if (pipelineGraph != null) {
				pipelineGraph.style.display = DisplayStyle.None;
			}
			EditorGUILayout.HelpBox(MSG_PLAY_MODE, MessageType.Warning);
		}
		/// <summary>
		/// Draws the logo.
		/// </summary>
		private void DrawLogo () {
			GUILayout.Space (58);
			if (GUITextureManager.GetBroccoliLogo () != null) {
				GUI.DrawTexture (new Rect (5, 8, 180, 48), GUITextureManager.GetBroccoliLogo (), ScaleMode.ScaleToFit);
			}
		}
		/// <summary>
		/// Draws the log box.
		/// </summary>
		private void DrawLogBox () {
			if (serializedTreeFactory != null && treeFactory != null) {
				if (treeFactory.log.Count > 0) {
					var enumerator = treeFactory.log.GetEnumerator ();
					while (enumerator.MoveNext ()) {
						var logItem = enumerator.Current;
						MessageType messageType = UnityEditor.MessageType.Info;
						switch (logItem.messageType) {
						case LogItem.MessageType.Error:
							messageType = UnityEditor.MessageType.Error;
							break;
						case LogItem.MessageType.Warning:
							messageType = UnityEditor.MessageType.Warning;
							break;
						}
						EditorGUILayout.HelpBox (logItem.message, messageType);
					}
				} else {
					pipelineLegend = "Valid Pipeline";
					if (Broccoli.Manager.MaterialManager.renderPipelineType == Manager.MaterialManager.RenderPipelineType.URP) {
						pipelineLegend += " (URP).";
					} else if (Broccoli.Manager.MaterialManager.renderPipelineType == Manager.MaterialManager.RenderPipelineType.HDRP) {
						pipelineLegend += " (HDRP).";
					} else {
						pipelineLegend += " (Std RP).";
					}
					EditorGUILayout.HelpBox (pipelineLegend, UnityEditor.MessageType.Info);
				}
			}
		}
		/// <summary>
		/// Draws the persistence options.
		/// </summary>
		private void DrawPersistenceOptions () {
			if (GUILayout.Button (createNewPipelineBtn)) {
				if (treeFactory.localPipeline.GetElementsCount () == 0 ||
					EditorUtility.DisplayDialog (MSG_NEW_PIPELINE_TITLE, 
						MSG_NEW_PIPELINE_MESSAGE, MSG_NEW_PIPELINE_OK, MSG_NEW_PIPELINE_CANCEL)) {
					LoadNewPipeline ();
				}
			}
			if (GUILayout.Button (loadFromAssetBtn)) {
				string panelPath = ExtensionManager.fullExtensionPath + GlobalSettings.pipelineSavePath;
				string path = EditorUtility.OpenFilePanel("Load Pipeline", panelPath, "asset");
				LoadPipelineAsset (path);
			}
			if (GUILayout.Button (saveAsNewAssetBtn)) {
				string panelPath = ExtensionManager.fullExtensionPath + GlobalSettings.pipelineSavePath;
				string path = EditorUtility.SaveFilePanelInProject ("Save Pipeline", "TreePipeline", "asset", "", panelPath);
				if (SavePipelineAsset (path, true)) {
					treeFactory.localPipelineFilepath = path;
					if (treeFactory.localPipeline.isCatalogItem && !GlobalSettings.editCatalogEnabled) {
						treeFactory.localPipeline.isCatalogItem = false;
					}
					ShowNotification (new GUIContent ("Asset saved at " + path));
					GUIUtility.ExitGUI();
				}
			}
			bool filePathEmpty = string.IsNullOrEmpty (treeFactory.localPipelineFilepath);
			EditorGUI.BeginDisabledGroup (filePathEmpty || 
				(!GlobalSettings.editCatalogEnabled && treeFactory.localPipeline.isCatalogItem));
			if (GUILayout.Button (saveBtn)) {
				if (SavePipelineAsset (treeFactory.localPipelineFilepath)) {
					ShowNotification (new GUIContent ("Asset saved at " + treeFactory.localPipelineFilepath));
					GUIUtility.ExitGUI();
				}
			}
			if (!filePathEmpty) {
				if (treeFactory.localPipeline != null && 
					treeFactory.localPipeline.isCatalogItem && 
					!GlobalSettings.editCatalogEnabled) {
					EditorGUILayout.HelpBox ("To persist changes save this pipeline as a new asset.", MessageType.Info);
				} else {
					GUILayout.Label (new GUIContent ("at: " + treeFactory.localPipelineFilepath, 
						treeFactory.localPipelineFilepath), BroccoEditorGUI.label);
				}
			}
			EditorGUI.EndDisabledGroup ();
		}
		/// <summary>
		/// Draws the options when the pipeline has no elements.
		/// </summary>
		private void DrawEmptyPipelineOptions () {
			if (GUILayout.Button (createNewPipelineBtn)) {
				if (treeFactory.localPipeline.GetElementsCount () == 0 ||
					EditorUtility.DisplayDialog (MSG_NEW_PIPELINE_TITLE, 
						MSG_NEW_PIPELINE_MESSAGE, MSG_NEW_PIPELINE_OK, MSG_NEW_PIPELINE_CANCEL)) {
					LoadNewPipeline ();
				}
			}
			EditorGUILayout.Space ();
			if (GUILayout.Button (loadFromAssetBtn)) {
				string panelPath = ExtensionManager.fullExtensionPath + GlobalSettings.pipelineSavePath;
				string path = EditorUtility.OpenFilePanel("Load Pipeline", panelPath, "asset");
				LoadPipelineAsset (path);
			}
			EditorGUILayout.Space ();
			if (GUILayout.Button (fromCatalogBtn, GUILayout.Height(25), GUILayout.ExpandWidth(true))) {
				SetEditorView (EditorView.Catalog);
				catalog = BroccoliCatalog.GetInstance ();
			}
		}
		/// <summary>
		/// Draws the processing options.
		/// </summary>
		private void DrawProcessingOptions () {
			if (GUILayout.Button(generateNewPreviewBtn)) {
				treeFactory.ProcessPipelinePreview ();
			}
			EditorGUILayout.Space ();
			if (GUILayout.Button (createPrefabBtn, GUILayout.Height(25), GUILayout.ExpandWidth(true))) {
				if (treeFactory.CreatePrefab ()) {
					ShowNotification (
						new GUIContent ("Prefab created at " + treeFactory.prefabPath));
					GUIUtility.ExitGUI();
				}
			}
		}
		/// <summary>
		/// Draws the catalog options.
		/// </summary>
		private void DrawShowCatalogOptions () {
			if (GUILayout.Button (fromCatalogBtn, GUILayout.Height(25), GUILayout.ExpandWidth(true))) {
				SetEditorView (EditorView.Catalog);
				catalog = BroccoliCatalog.GetInstance ();
			}
		}
		/// <summary>
		/// Draws the prefab creation options.
		/// </summary>
		private void DrawShowPrefabOptions () {
			if (GUILayout.Button(advancedOptionsBtn, GUILayout.Height(25))) {
				SetEditorView (EditorView.FactoryOptions);
			}
		}
		/// <summary>
		/// Draws the sprout groups list.
		/// </summary>
		private void DrawSproutGroupList () {
			//if (NodeEditorGUI.IsUsingSidePanelSkin ())
				sproutGroupList.DoLayoutList  ();
		}
		/// <summary>
		/// Draws the debug options.
		/// </summary>
		private void DrawDebugOptions () {
			if (treeFactory == null)
				return;
			EditorGUILayout.LabelField ("Preview Mode", BroccoEditorGUI.labelBoldCentered);
			int currentPreviewMode = (int)treeFactory.treeFactoryPreferences.previewMode;

			currentPreviewMode = GUILayout.Toolbar (currentPreviewMode, previewOptions, GUI.skin.button);

			EditorGUILayout.Space ();
			/*
			EditorGUILayout.LabelField ("Gizmo Options", BroccoEditorGUI.labelBoldCentered);
			bool debugDrawBranches = GUILayout.Toggle (treeFactory.treeFactoryPreferences.debugShowDrawBranches, 
				" Branches");
			bool debugDrawSprouts = GUILayout.Toggle (treeFactory.treeFactoryPreferences.debugShowDrawSprouts, 
				" Sprouts");
			if (debugDrawBranches != treeFactory.treeFactoryPreferences.debugShowDrawBranches ||
				debugDrawSprouts != treeFactory.treeFactoryPreferences.debugShowDrawSprouts) {
				treeFactory.treeFactoryPreferences.debugShowDrawBranches = debugDrawBranches;
				treeFactory.treeFactoryPreferences.debugShowDrawSprouts = debugDrawSprouts;
				SceneView.RepaintAll ();
			}
			*/

			if (currentPreviewMode != (int)treeFactory.treeFactoryPreferences.previewMode) {
				treeFactory.treeFactoryPreferences.previewMode = (TreeFactory.PreviewMode)currentPreviewMode;
				if (treeFactory.localPipeline.state == Broccoli.Pipe.Pipeline.State.Valid) {
					treeFactory.ProcessPipelinePreview (null, true);
				}
			}

			if (GlobalSettings.showPipelineDebugOption) {
				if (GUILayout.Button (new GUIContent ("Print Pipeline Info", 
					"Prints pipeline debugging information to the console."))) {
					if (treeFactory.localPipeline != null) {
						Debug.Log ("Pipeline has: " + treeFactory.localPipeline.GetElementsCount());
						List<PipelineElement> pipelineElements = treeFactory.localPipeline.GetElements ();
						for (int i = 0; i < pipelineElements.Count; i++) {
							Debug.Log ("ID: " + pipelineElements[i].id + ", " + pipelineElements[i].ToString ());
						}
					}
				}
			}
		}
		/// <summary>
		/// Draws the factory options.
		/// </summary>
		private void DrawFactoryOptions () {
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Factory Scale", BroccoEditorGUI.label);
			float factoryScale = treeFactory.treeFactoryPreferences.factoryScale;
			factoryScale = EditorGUILayout.FloatField (factoryScale, GUILayout.Width (100));
			EditorGUILayout.EndHorizontal ();
			if (factoryScale > 0 && factoryScale != treeFactory.treeFactoryPreferences.factoryScale) {
				factoryScale = (Mathf.RoundToInt (factoryScale * 1000)) / 1000f;
				treeFactory.treeFactoryPreferences.factoryScale = factoryScale;
				TreeFactory.GetActiveInstance ().ProcessPipelinePreview (null, true);
			}
			if (treeFactory.localPipeline.IsValid ()) {
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ("Processing Options", BroccoEditorGUI.labelBoldCentered);
				DrawLastSeed ();
				if (GUILayout.Button (regeneratePreviewBtn)) {
					treeFactory.ProcessPipelinePreview (null, true, true);
				}
				if (GUILayout.Button (generateWithCustomSeedBtn)) {
					treeFactory.localPipeline.seed = treeFactory.treeFactoryPreferences.customSeed;
					treeFactory.ProcessPipelinePreview (null, true, true);
				}
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Custom Seed", BroccoEditorGUI.label);
				int customSeed = treeFactory.treeFactoryPreferences.customSeed;
				customSeed = EditorGUILayout.IntField (customSeed, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				if (customSeed != treeFactory.treeFactoryPreferences.customSeed) {
					treeFactory.treeFactoryPreferences.customSeed = customSeed;
				}
			}
		}
		/// <summary>
		/// Draws the options on materials.
		/// </summary>
		private void DrawMaterialOptions () {
			EditorGUILayout.LabelField ("Material Options", BroccoEditorGUI.labelBoldCentered);

			// Preferred tree shader
			EditorGUILayout.LabelField ("Shader", BroccoEditorGUI.label);
			TreeFactoryPreferences.PreferredShader preferredShader = 
				(TreeFactoryPreferences.PreferredShader)EditorGUILayout.EnumPopup (treeFactory.treeFactoryPreferences.preferredShader);
			if (preferredShader != treeFactory.treeFactoryPreferences.preferredShader) {
				treeFactory.treeFactoryPreferences.preferredShader = preferredShader;
				treeFactory.ProcessPipelinePreview (null, true, true);
				if (GlobalSettings.broccoTreeControllerVersion == GlobalSettings.BROCCO_TREE_CONTROLLER_V1) {
					Broccoli.Controller.BroccoTreeController controller = 
						treeFactory.previewTree.obj.GetComponent<Broccoli.Controller.BroccoTreeController> ();
					if (controller != null) {
						controller.EnableEditorWind (controller.editorWindEnabled);
					}
				}
			}
			if (preferredShader == TreeFactoryPreferences.PreferredShader.SpeedTree7Compatible || 
				preferredShader == TreeFactoryPreferences.PreferredShader.SpeedTree8Compatible) {
				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Shader File", BroccoEditorGUI.label);
				Shader customShader = (Shader)EditorGUILayout.ObjectField (treeFactory.treeFactoryPreferences.customBranchShader, typeof(Shader), true, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				if (customShader != treeFactory.treeFactoryPreferences.customBranchShader) {
					treeFactory.treeFactoryPreferences.customBranchShader = customShader;
					treeFactory.treeFactoryPreferences.customSproutShader = customShader;
					TreeFactory.GetActiveInstance ().ProcessPipelinePreview (null, true, true);
				}
			}
			/* TODO: remove Tree Creator.
			if (preferredShader == TreeFactoryPreferences.PreferredShader.TreeCreatorCompatible) {
				EditorGUILayout.Space ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Branch Shader", BroccoEditorGUI.label);
				Shader customBranchShader = (Shader)EditorGUILayout.ObjectField (treeFactory.treeFactoryPreferences.customBranchShader, typeof(Shader), true, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Sprout Shader", BroccoEditorGUI.label);
				Shader customSproutShader = (Shader)EditorGUILayout.ObjectField (treeFactory.treeFactoryPreferences.customSproutShader, typeof(Shader), true, GUILayout.Width (100));
				EditorGUILayout.EndHorizontal ();
				if (customBranchShader != treeFactory.treeFactoryPreferences.customBranchShader || 
					customSproutShader != treeFactory.treeFactoryPreferences.customSproutShader) {
					treeFactory.treeFactoryPreferences.customBranchShader = customBranchShader;
					treeFactory.treeFactoryPreferences.customSproutShader = customSproutShader;
					TreeFactory.GetActiveInstance ().ProcessPipelinePreview (null, true, true);
				}
			}
			*/
			EditorGUILayout.LabelField ("*SRP support relies on the shader availability for the render pipeline.", BroccoEditorGUI.label);

			// Override shader.
			/* Deprecated
			EditorGUILayout.BeginHorizontal ();
			bool overrideShader = GUILayout.Toggle (treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled, "");
			EditorGUILayout.LabelField ("Override shader on custom materials (for WindZone).");
			EditorGUILayout.EndHorizontal ();
			if (overrideShader != treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
				treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled = overrideShader;
				TreeFactory.GetActiveInstance ().ProcessPipelinePreview (null, true, true);
			}
			*/
		}
		/// <summary>
		/// Draws options on prefab creation.
		/// </summary>
		private void DrawPrefabOptions () {
			EditorGUILayout.LabelField (prefabOptionsTitle, BroccoEditorGUI.labelBoldCentered);

			// Prefix.
			EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (false));
			EditorGUILayout.LabelField (prefabSavePrefixLabel, GUILayout.Width (40));
			string prefabPrefix = EditorGUILayout.TextField (string.Empty, treeFactory.treeFactoryPreferences.prefabSavePrefix, GUILayout.Width (120));
			if (!prefabPrefix.Equals (treeFactory.treeFactoryPreferences.prefabSavePrefix)) {
				char[] invalidChars = Path.GetInvalidFileNameChars();
				string sanitizedPrefix = new string(prefabPrefix.Where(c => !invalidChars.Contains(c)).ToArray());
				treeFactory.treeFactoryPreferences.prefabSavePrefix = sanitizedPrefix.Trim ();
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.HelpBox (treeFactory.treeFactoryPreferences.prefabSavePrefix + "000.prefab", MessageType.None);

			// Path.
			EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth (false));
			EditorGUILayout.LabelField (prefabSavePathLabel, GUILayout.Width (40));
			if (GUILayout.Button (prefabButtonLabel, GUILayout.Width(30))) {
				string currentPath = Application.dataPath;
        		if (Directory.Exists(Path.GetDirectoryName(currentPath + treeFactory.treeFactoryPreferences.prefabSavePath))) {
					currentPath += treeFactory.treeFactoryPreferences.prefabSavePath;
				}
				string selectedPath = EditorUtility.OpenFolderPanel (selectSavePrefabPathTitle, currentPath, "");
				if (!string.IsNullOrEmpty (selectedPath)) {
					if (selectedPath.Contains(Application.dataPath)) {
						selectedPath = selectedPath.Substring (Application.dataPath.Length);
					} else {
						selectedPath = string.Empty;
					}
					if (selectedPath.CompareTo (treeFactory.treeFactoryPreferences.prefabSavePath) != 0) {
						treeFactory.treeFactoryPreferences.prefabSavePath = selectedPath;
					}
				}
				GUIUtility.ExitGUI();
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.HelpBox ("Assets" + treeFactory.treeFactoryPreferences.prefabSavePath + "/", MessageType.None);

			// Clone custom materials.
			bool cloneCustomMaterials = GUILayout.Toggle (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled, prefabCloneMaterialsLabel);
			if (cloneCustomMaterials != treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled) {
				treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled = cloneCustomMaterials;
			}

			// Materials and mesh to folder enabled.
			bool includeAssetsInsidePrefab = GUILayout.Toggle (treeFactory.treeFactoryPreferences.prefabIncludeAssetsInsidePrefab, prefabIncludeAssetsInPrefabLabel);
			if (includeAssetsInsidePrefab != treeFactory.treeFactoryPreferences.prefabIncludeAssetsInsidePrefab) {
				treeFactory.treeFactoryPreferences.prefabIncludeAssetsInsidePrefab = includeAssetsInsidePrefab;
			}

			// Copy textures from a bark custom material with shader override to the prefab folder.
			bool copyCustomMaterialTextures = GUILayout.Toggle (treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled, prefabCopyBranchTexturesLabel);
			if (copyCustomMaterialTextures != treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled) {
				treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled = copyCustomMaterialTextures;
			}

			// Create atlas.
			bool createAtlas = GUILayout.Toggle (treeFactory.treeFactoryPreferences.prefabCreateAtlas, prefabCreateSproutAtlasLabel);
			if (createAtlas != treeFactory.treeFactoryPreferences.prefabCreateAtlas) {
				treeFactory.treeFactoryPreferences.prefabCreateAtlas = createAtlas;
			}
			if (treeFactory.treeFactoryPreferences.prefabCreateAtlas) {
				// Atlas size.
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (prefabAtlasSizeLabel, BroccoEditorGUI.label);
				TreeFactory.TextureSize atlasTextureSize = 
					(TreeFactory.TextureSize)EditorGUILayout.EnumPopup (treeFactory.treeFactoryPreferences.atlasTextureSize, GUILayout.Width (120));
				EditorGUILayout.EndHorizontal ();
				if (atlasTextureSize != treeFactory.treeFactoryPreferences.atlasTextureSize) {
					treeFactory.treeFactoryPreferences.atlasTextureSize = atlasTextureSize;
				}
			}

			EditorGUILayout.Space ();

			EditorGUILayout.LabelField (prefabMeshesTitle, BroccoEditorGUI.labelBoldCentered);

			// Billboard size.
			if (treeFactory.treeFactoryPreferences.prefabIncludeBillboard) {
				// Billboard texture size.
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (billboardTextureLabel, BroccoEditorGUI.label, GUILayout.Width (100));
				TreeFactory.TextureSize billboardTextureSize = 
					(TreeFactory.TextureSize)EditorGUILayout.EnumPopup (treeFactory.treeFactoryPreferences.billboardTextureSize, 
						GUILayout.Width (80));
				EditorGUILayout.EndHorizontal ();
				if (billboardTextureSize != treeFactory.treeFactoryPreferences.billboardTextureSize) {
					treeFactory.treeFactoryPreferences.billboardTextureSize = billboardTextureSize;
				}
			}
			// Reposition root.
			bool repositionEnabled = GUILayout.Toggle (treeFactory.treeFactoryPreferences.prefabRepositionEnabled, repositionEnabledLabel);
			if (repositionEnabled != treeFactory.treeFactoryPreferences.prefabRepositionEnabled) {
				treeFactory.treeFactoryPreferences.prefabRepositionEnabled = repositionEnabled;
			}
			EditorGUILayout.Space ();
		}
		/// <summary>
		/// Draws the Broccoli Version.
		/// </summary>
		private void DrawBroccoliVersion () {
			// Display Broccoli Version.
			EditorGUILayout.HelpBox (BroccoliExtensionInfo.GetNamedVersion (), MessageType.None);
		}
		/// <summary>
		/// Draws the last seed.
		/// </summary>
		private void DrawLastSeed () {
			if (treeFactory != null && treeFactory.localPipeline != null) {
				EditorGUILayout.LabelField ("Last Seed: " + treeFactory.localPipeline.seed, BroccoEditorGUI.label);
			}
		}
		private void DrawRateMe () {
			EditorGUILayout.HelpBox ("Your honest review and rating will help us greatly to continue improving this asset or you.", MessageType.None);
			if (GUILayout.Button ("Rate Us!")) {
				Application.OpenURL("http://u3d.as/1emq");
			}
		}
		#endregion

		#region Graph View Events
		public void BindPipelineGraphEvents (PipelineGraphView pipelineGraph) {
			pipelineGraph.onZoomDone -= OnGraphZoomDone;
			pipelineGraph.onZoomDone += OnGraphZoomDone;
			pipelineGraph.onPanDone -= OnGraphPanDone;
			pipelineGraph.onPanDone += OnGraphPanDone;
			pipelineGraph.onSelectNode -= OnGraphSelectNode;
			pipelineGraph.onSelectNode += OnGraphSelectNode;
			pipelineGraph.onDeselectNode -= OnGraphDeselectNode;
			pipelineGraph.onDeselectNode += OnGraphDeselectNode;
			pipelineGraph.onBeforeEnableNode -= OnGraphBeforeEnableNode;
			pipelineGraph.onBeforeEnableNode += OnGraphBeforeEnableNode;
			pipelineGraph.onEnableNode -= OnGraphEnableNode;
			pipelineGraph.onEnableNode += OnGraphEnableNode;
			pipelineGraph.onBeforeMoveNodes -= OnGraphBeforeMoveNodes;
			pipelineGraph.onBeforeMoveNodes += OnGraphBeforeMoveNodes;
			pipelineGraph.onMoveNodes -= OnGraphMoveNodes;
			pipelineGraph.onMoveNodes += OnGraphMoveNodes;
			pipelineGraph.onBeforeAddNode -= OnGraphBeforeAddNode;
			pipelineGraph.onBeforeAddNode += OnGraphBeforeAddNode;
			pipelineGraph.onAddNode -= OnGraphAddNode;
			pipelineGraph.onAddNode += OnGraphAddNode;
			pipelineGraph.onBeforeRemoveNodes -= OnGraphBeforeRemoveNodes;
			pipelineGraph.onBeforeRemoveNodes += OnGraphBeforeRemoveNodes;
			pipelineGraph.onRemoveNodes -= OnGraphRemoveNodes;
			pipelineGraph.onRemoveNodes += OnGraphRemoveNodes;
			pipelineGraph.onBeforeAddConnection -= OnGraphBeforeAddConnection;
			pipelineGraph.onBeforeAddConnection += OnGraphBeforeAddConnection;
			pipelineGraph.onAddConnection -= OnGraphAddConnection;
			pipelineGraph.onAddConnection += OnGraphAddConnection;
			pipelineGraph.onBeforeRemoveConnections -= OnGraphBeforeRemoveConnections;
			pipelineGraph.onBeforeRemoveConnections += OnGraphBeforeRemoveConnections;
			pipelineGraph.onRemoveConnections -= OnGraphRemoveConnections;
			pipelineGraph.onRemoveConnections += OnGraphRemoveConnections;
			pipelineGraph.onRequestUpdatePipeline -= OnRequestUpdatePipeline;
			pipelineGraph.onRequestUpdatePipeline += OnRequestUpdatePipeline;
		}
		void OnRequestUpdatePipeline () {
			treeFactory.RequestPipelineUpdate ();
		}
		void OnGraphZoomDone (float currentZoom, float previousZoom) {
			treeFactory.treeFactoryPreferences.graphZoom = currentZoom;
			treeFactory.localPipeline.treeFactoryPreferences.graphZoom = currentZoom;
		}
		void OnGraphPanDone (Vector2 currentOffset, Vector2 previousOffset) {
			treeFactory.treeFactoryPreferences.graphOffset = currentOffset;
			treeFactory.localPipeline.treeFactoryPreferences.graphOffset = currentOffset;
		}
		void OnGraphBeforeEnableNode (PipelineNode pipelineNode, bool enable) {
			Undo.RecordObject (pipelineNode.pipelineElement, "Enabling " + pipelineNode.pipelineElement.name + " element.");
		}
		void OnGraphEnableNode (PipelineNode pipelineNode, bool enable) {
			treeFactory.localPipeline.undoControl.undoCount++;
			treeFactory.RequestPipelineUpdate ();
		}
		void OnGraphSelectNode (PipelineNode pipelineNode) {
			UnityEditor.Selection.activeObject = pipelineNode.pipelineElement;
		}
		void OnGraphDeselectNode (PipelineNode pipelineNode) {}
		void OnGraphBeforeMoveNodes (List<PipelineNode> pipelineNodes, Vector2 delta) {}
		void OnGraphMoveNodes (List<PipelineNode> pipelineNodes, Vector2 delta) {}
		void OnGraphBeforeAddNode (PipelineElement pipelineElement) {
			treeFactory.localPipeline.undoControl.undoCount++;
			Undo.RecordObject (treeFactory.localPipeline, "Adding " + pipelineElement.name + " element.");
		}
		void OnGraphAddNode (PipelineNode pipelineNode) {}
		void OnGraphBeforeRemoveNodes (List<PipelineNode> pipelineNodesToRemove) {
			treeFactory.localPipeline.undoControl.undoCount++;
			currentUndoGroup = Undo.GetCurrentGroup ();
			Undo.RecordObject (treeFactory.localPipeline, "Removing elements.");
			Undo.CollapseUndoOperations (currentUndoGroup);
		}
		void OnGraphRemoveNodes (List<PipelineNode> pipelineNodesRemoved) {
			for (int i = 0; i < pipelineNodesRemoved.Count; i++) {
				if (pipelineNodesRemoved [i].pipelineElement ==
					UnityEditor.Selection.activeObject) {
						UnityEditor.Selection.activeObject = null;
						break;
					}
			}
		}
		void OnGraphBeforeAddConnection (PipelineNode srcNode, PipelineNode sinkNode) {
			treeFactory.localPipeline.undoControl.undoCount++;
			currentUndoGroup = Undo.GetCurrentGroup ();
			Undo.RecordObject (treeFactory.localPipeline, "Connecting elements.");
			Undo.CollapseUndoOperations (currentUndoGroup);
		}
		void OnGraphAddConnection (PipelineNode srcNode, PipelineNode sinkNode) {}
		void OnGraphBeforeRemoveConnections (List<UnityEditor.Experimental.GraphView.Edge> edges) {
			treeFactory.localPipeline.undoControl.undoCount++;
			currentUndoGroup = Undo.GetCurrentGroup ();
			Undo.RecordObject (treeFactory.localPipeline, "Disconnecting elements.");
			Undo.CollapseUndoOperations (currentUndoGroup);
		}
		void OnGraphRemoveConnections (List<UnityEditor.Experimental.GraphView.Edge> edges) {}
		#endregion
	}
}