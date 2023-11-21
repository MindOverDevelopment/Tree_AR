using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Broccoli.Pipe;
using Broccoli.Factory;
using Broccoli.Base;
using Broccoli.Utils;

namespace Broccoli.BroccoEditor {
    [SproutLabEditorImpl (0)]
    public class MainSproutLabEditorImpl : ISproutLabEditorImpl
    {
        #region Vars
        private SproutLabEditor sproutLabEditor;
        /// <summary>
        /// Free view zoom and rotation, starting from the side of the target.
        /// </summary>
        private SproutLabEditor.CanvasSettings sideViewCanvasSettings = null;
        /// <summary>
        /// Locked view, starting from the front (top) side of the target.
        /// </summary>
        private SproutLabEditor.CanvasSettings frontViewCanvasSettings = null;
        /// <summary>
        /// Debug canvas view.
        /// </summary>
        private SproutLabEditor.CanvasSettings debugViewCanvasSettings = null;
        /// <summary>
        /// Default structure settings.
        /// </summary>
        private SproutLabEditor.StructureSettings structureSettings = null;
        #endregion

        #region Constants
        public const int IMPL_BRANCH = 0;
        private static string IMPL_BRANCH_NAME = "BranchCollection";
        #endregion

        #region Messages
        private static string MSG_HEADER = "Broccoli Sprout Lab v2.0.0";
        private static string MSG_PREVIEW_TITLE = "Broccoli Branch Collection Descriptor";
        private static GUIContent TEMPLATE_BRANCH_BTN = new GUIContent ("Branch Collection from Template", "Select a template from the Catalog to setup the Project.");
        private static string MSG_CREATE_BRANCH_COLLECTION = "Create a new Branch Collection or select a template to begin working on it. \n" +
            "Branch Collections can be used to add sprout geometry and textures to Broccoli Trees or be used to generate atlas textures for sprouts.";
        #endregion

        #region Init
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize (SproutLabEditor sproutLabEditor) {
            this.sproutLabEditor = sproutLabEditor;
            if (sideViewCanvasSettings == null) {
                sideViewCanvasSettings = new SproutLabEditor.CanvasSettings ();
                sideViewCanvasSettings.id = 1;
                sideViewCanvasSettings.showPlane = false;
                sideViewCanvasSettings.showRuler = false;
                sideViewCanvasSettings.viewOffset = new Vector3 (0.3f, -0.2f, -5.5f);
                sideViewCanvasSettings.viewDirection = new Vector2 (220f, -50f);
                sideViewCanvasSettings.viewTargetRotation = Quaternion.Euler (0f, 0f, 90f);
            }
            if (frontViewCanvasSettings == null) {
                frontViewCanvasSettings = new SproutLabEditor.CanvasSettings ();
                frontViewCanvasSettings.id = 2;
                frontViewCanvasSettings.freeViewEnabled = false;
                frontViewCanvasSettings.showGizmos = false;
                frontViewCanvasSettings.showPlane = false;
                frontViewCanvasSettings.showRuler = false;
            }
            if (debugViewCanvasSettings == null) {
                debugViewCanvasSettings = new SproutLabEditor.CanvasSettings ();
                debugViewCanvasSettings.showPlane = false;
                debugViewCanvasSettings.showRuler = false;
                debugViewCanvasSettings.resetView = false;
            }
            if (structureSettings == null) {
                structureSettings = new SproutLabEditor.StructureSettings ();
                structureSettings.id = 1;
            }
        }
        /// <summary>
        /// Called on the main editor OnEnable.
        /// </summary>
        public void OnEnable () {}
        /// <summary>
        /// Called on the main editor OnDisable.
        /// </summary>
        public void OnDisable () {}
        #endregion

        #region Info
        /// <summary>
        /// Order of this implementation.
        /// </summary>
        private int _order = 0;
        /// <summary>
        /// Order of this implementation.
        /// </summary>
        public int order { 
            get { return this._order; } 
            set { this._order = value; }
        }
        /// <summary>
        /// Get the ids for the implementations handled by this editor. 
        /// Id should be different for each implementation.
        /// </summary>
        /// <value>Ids of the implementations handled by this editor.</value>
        public int[] implIds { get { return new int[1]{IMPL_BRANCH}; } }
        /// <summary>
        /// Gets the string to show in the editor header.
        /// </summary>
        /// <returns>Header message.</returns>
        public string GetHeaderMsg () {
            return MSG_HEADER;
        }
        /// <summary>
        /// Gets the title string for the preview mesh canvas.
        /// </summary>
        /// <param name="implId">Implementation id.</param>
        /// <returns>Mesh preview title.</returns>
        public string GetPreviewTitle (int implId) {
            return "Snapshot " + sproutLabEditor.branchDescriptorCollection.snapshotIndex + " (" + 
                    MSG_PREVIEW_TITLE + ")";
        }
        /// <summary>
        /// Gets the canvas setting configuration
        /// </summary>
        /// <param name="panel">Panel index.</param>
        /// <param name="subPanel">Subpanel index.</param>
        /// <returns>Configuration to show the canvas.</returns>
        public SproutLabEditor.CanvasSettings GetCanvasSettings (int panel, int subPanel) {
            switch (panel) {
                case SproutLabEditor.PANEL_MAPPING:
                    return frontViewCanvasSettings;
                case SproutLabEditor.PANEL_DEBUG:
                    return debugViewCanvasSettings;
                case SproutLabEditor.PANEL_STRUCTURE:
                case SproutLabEditor.PANEL_TEXTURE:
                case SproutLabEditor.PANEL_EXPORT:
                default:
                    return sideViewCanvasSettings;
            }
        }
        /// <summary>
        /// Gets the structure settings to use on an implementation.
        /// </summary>
        /// <param name="impId">Id of the implementation.</param>
        /// <returns>Structure settings.</returns>
        public SproutLabEditor.StructureSettings GetStructureSettings (int impId) {
            return structureSettings;
        }
        /// <summary>
        /// Called from the SproutLabEditor instance when a snapshot has been selected.
        /// </summary>
        /// <param name="index">Index of the snapshot selected.</param>
        public void SnapshotSelected (int index) {}
        /// <summary>
        /// Called from the SproutLabEditor instance when a variation has been selected.
        /// </summary>
        /// <param name="index">Index of the variation selected.</param>
        public void VariationSelected (int index) {}
        #endregion

        #region Processing
        /// <summary>
        /// Called after an undo/redo action has been made on the editor.
        /// </summary>
        public void OnUndoRedo () {}
        /// <summary>
        /// Called when requesting generating a new structure.
        /// </summary>
        /// <returns><c>True</c> to continue the process, <c>false</c> to stop propagation.</returns>
        public bool OnGenerateNewStructure () { return true; }
        /// <summary>
        /// Called when requesting regenerating a new structure.
        /// </summary>
        /// <returns><c>True</c> to continue the process, <c>false</c> to stop propagation.</returns>
        public bool OnRegenerateStructure () { return true; }
        /// <summary>
        /// Called after a new structure has been generated.
        /// </summary>
        public void OnAfterGenerateNewStructure () {}
        /// <summary>
        /// Called after a structure has been regenerated.
        /// </summary>
        public void OnAfterRegenerateStructure () {}
        /// <summary>
        /// Called when creating a new branch collecton is requested.
        /// </summary>
        /// <param name="implId">Implementation id.</param>
        public void OnCreateBranchDescrptorCollection (int implId) {
            if (sproutLabEditor.branchDescriptorCollection != null && 
                sproutLabEditor.branchDescriptorCollection.descriptorImplId == -1)
            {
                sproutLabEditor.branchDescriptorCollection.descriptorImplId = implId;
                sproutLabEditor.branchDescriptorCollection.descriptorImplName = IMPL_BRANCH_NAME;
            }
        }
        /// <summary>
        /// Called when a new Branch Descriptor Collection instance is loaded to the Sprout Lab Editor.
        /// </summary>
        /// <param name="branchDescriptorCollection">Loaded Branch Descriptor Collection instance.</param>
        /// <param name="sproutSubfactory">Sprout Subfactory instance.</param>
        public void OnLoadBranchDescriptorCollection (BranchDescriptorCollection branchDescriptorCollection, SproutSubfactory sproutSubfactory) {
            branchDescriptorCollection.onAddSnapshot -= OnAddBranchDescriptor;
            branchDescriptorCollection.onAddSnapshot += OnAddBranchDescriptor;
            branchDescriptorCollection.onRemoveSnapshot -= OnRemoveBranchDescriptor;
            branchDescriptorCollection.onRemoveSnapshot += OnRemoveBranchDescriptor;
            branchDescriptorCollection.onAddVariation -= OnAddVariationDescriptor;
            branchDescriptorCollection.onAddVariation += OnAddVariationDescriptor;
            branchDescriptorCollection.onRemoveVariation -= OnRemoveVariationDescriptor;
            branchDescriptorCollection.onRemoveVariation += OnRemoveVariationDescriptor;

            // Set persistence utility.
            sproutLabEditor.editorPersistence.elementName = "Branch Collection";
			sproutLabEditor.editorPersistence.saveFileDefaultName = "SproutLabBranchCollection";
            
            sproutLabEditor.editorPersistence.savePath = ExtensionManager.fullExtensionPath + GlobalSettings.pipelineSavePath;
            sproutLabEditor.editorPersistence.InitMessages ();
            sproutLabEditor.editorPersistence.btnSaveAsNewElement = "Export to File";
			sproutLabEditor.editorPersistence.btnLoadElement = "Import from File";
        }
        /// <summary>
        /// Called when the branch collection gets unload from this implementation.
        /// </summary>
        public void OnUnloadBranchDescriptorCollection () {}
        /// <summary>
        /// Called when a Branch Descriptor instance gets added to the collection.
        /// </summary>
        /// <param name="branchDescriptor">Added Branch Descriptor instance.</param>
        public void OnAddBranchDescriptor (BranchDescriptor branchDescriptor) {}
        /// <summary>
        /// Called when a Branch Descriptor instance gets removed from the collection.
        /// </summary>
        /// <param name="branchDescriptor">Removed Branch Descriptor instance.</param>
        public void OnRemoveBranchDescriptor (BranchDescriptor branchDescriptor) {}
        /// <summary>
        /// Called when a Variation Descriptor instance gets added to the collection.
        /// </summary>
        /// <param name="branchDescriptor">Added Variation Descriptor instance.</param>
        public void OnAddVariationDescriptor (VariationDescriptor branchDescriptor) {}
        /// <summary>
        /// Called when a Variation Descriptor instance gets removed from the collection.
        /// </summary>
        /// <param name="branchDescriptor">Removed Variation Descriptor instance.</param>
        public void OnRemoveVariationDescriptor (VariationDescriptor branchDescriptor) {}
        #endregion

        #region Editor Events
        /// <summary>
        /// Called when the editor Add Snapshot button gets clicked.
        /// </summary>
        /// <returns><c>True</c> if the action should be managed on this instance.</returns>
        public bool OnEditorAddSnapshotClicked () {
            return false;
        }
        #endregion

        #region Draw
        /// <summary>
        /// Gets the editor header logo texture.
        /// </summary>
        /// <returns>Editor header logo texture.</returns>
        public Texture2D GetHeaderLogo () {
            return GUITextureManager.GetBroccoliLogo ();
        }
        /// <summary>
		/// Draw additional handles on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
        public void OnCanvasDrawHandles (Rect r, Camera camera) {}
        /// <summary>
        /// Draw the first options on the Select Mode View.
        /// </summary>
        public void DrawSelectModeViewBeforeOptions () {
            if (GUILayout.Button (TEMPLATE_BRANCH_BTN)) {
                sproutLabEditor.SetTemplateView (IMPL_BRANCH);
			}
			EditorGUILayout.HelpBox (MSG_CREATE_BRANCH_COLLECTION, MessageType.None);
        }
        /// <summary>
        /// Draw the second options on the Select Mode View.
        /// </summary>
        public void DrawSelectModeViewAfterOptions () {}
        /// <summary>
        /// Draws custom panels for the loaded structure collection.
        /// </summary>
        public void DrawPanels (Rect windowRect) {}
        /// <summary>
        /// Draws LOD controls.
        /// </summary>
        /// <param name="r">Rect</param>
        /// <returns><c>True</c> to execute the base editor LOD control drawing</returns>
        public bool DrawLODControls (Rect r) { return true; }
        /// <summary>
        /// Called when the view in the editor changes.
        /// </summary>
        /// <param name="firsLevelIndex"></param>
        /// <param name="oldFirstLevelIndex"></param>
        /// <param name="secondLevelIndex"></param>
        /// <param name="oldSecondLevelIndex"></param>
        /// <param name="thirdLevelIndex"></param>
        /// <param name="oldThirdLevelIndex"></param>
        public void ViewChanged (
            int firsLevelIndex, int oldFirstLevelIndex,
            int secondLevelIndex, int oldSecondLevelIndex,
            int thirdLevelIndex, int oldThirdLevelIndex)
        {}
        /// <summary>
        /// Creates materials to outline the variation group selected.
        /// </summary>
        /// <param name="variationGroupId">Id of the variation group being selected.</param>
        public void CreateOutlineMaterials (int variationGroupId = -1) {}
        /// <summary>
        /// Refresh the current outline selection materials.
        /// </summary>
        public void RefreshOutlineMaterials () {}
        #endregion
    }
}