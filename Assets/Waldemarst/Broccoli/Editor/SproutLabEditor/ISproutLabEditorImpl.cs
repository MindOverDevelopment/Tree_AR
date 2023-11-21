using UnityEngine;

using Broccoli.Pipe;
using Broccoli.Factory;

namespace Broccoli.BroccoEditor
{
    /// <summary>
    /// Interface for implementation making use of the SproutLab Editor.
    /// </summary>
    public interface ISproutLabEditorImpl {
        #region Initialization
        /// <summary>
        /// Initialize this instance.
        /// </summary>
        void Initialize (SproutLabEditor sproutLabEditor);
        /// <summary>
        /// Called on the main editor OnEnable.
        /// </summary>
        void OnEnable ();
        /// <summary>
        /// Called on the main editor OnDisable.
        /// </summary>
        void OnDisable ();
        #endregion

        #region Configuration
        /// <summary>
        /// Order of this implementation.
        /// </summary>
        int order { get; set; }
        /// <summary>
        /// Get the ids for the implementations handled by this editor. 
        /// Id should be different for each implementation.
        /// </summary>
        /// <value>Ids of the implementations handled by this editor.</value>
        int[] implIds { get; }
        /// <summary>
        /// Gets the string to show in the editor header.
        /// </summary>
        /// <returns>Header message.</returns>
        string GetHeaderMsg ();
        /// <summary>
        /// Gets the title string for the preview mesh canvas.
        /// </summary>
        /// <param name="implId">Implementation id.</param>
        /// <returns>Mesh preview title.</returns>
        string GetPreviewTitle (int implId);
        /// <summary>
        /// Gets the canvas setting configuration
        /// </summary>
        /// <param name="panel">Panel index.</param>
        /// <param name="subPanel">Subpanel index.</param>
        /// <returns>Configuration to show the canvas.</returns>
        SproutLabEditor.CanvasSettings GetCanvasSettings (int panel, int subPanel);
        /// <summary>
        /// Gets the structure settings to use on an implementation.
        /// </summary>
        /// <param name="impId">Id of the implementation.</param>
        /// <returns>Structure settings.</returns>
        SproutLabEditor.StructureSettings GetStructureSettings (int impId);
        /// <summary>
        /// Called from the SproutLabEditor instance when a snapshot has been selected.
        /// </summary>
        /// <param name="index">Index of the snapshot selected.</param>
        void SnapshotSelected (int index);
        /// <summary>
        /// Called from the SproutLabEditor instance when a variation has been selected.
        /// </summary>
        /// <param name="index">Index of the variation selected.</param>
        void VariationSelected (int index);
        #endregion

        #region Processing
        /// <summary>
        /// Called after an undo/redo action has been made on the editor.
        /// </summary>
        void OnUndoRedo ();
        /// <summary>
        /// Called when requesting generating a new structure.
        /// </summary>
        /// <returns><c>True</c> to continue the process, <c>false</c> to stop propagation.</returns>
        bool OnGenerateNewStructure ();
        /// <summary>
        /// Called when requesting regenerating a new structure.
        /// </summary>
        /// <returns><c>True</c> to continue the process, <c>false</c> to stop propagation.</returns>
        bool OnRegenerateStructure ();
        /// <summary>
        /// Called after a new structure has been generated.
        /// </summary>
        void OnAfterGenerateNewStructure ();
        /// <summary>
        /// Called after a structure has been regenerated.
        /// </summary>
        void OnAfterRegenerateStructure ();
        /// <summary>
        /// Called when creating a new branch collecton is requested.
        /// </summary>
        /// <param name="implId">Implementation id.</param>
        void OnCreateBranchDescrptorCollection (int implId);
        /// <summary>
        /// Called when a new Branch Descriptor Collection instance is loaded to the Sprout Lab Editor.
        /// </summary>
        /// <param name="branchDescriptorCollection">Loaded Branch Descriptor Collection instance.</param>
        /// <param name="sproutSubfactory">Sprout Subfactory instance.</param>
        void OnLoadBranchDescriptorCollection (BranchDescriptorCollection branchDescriptorCollection, SproutSubfactory sproutSubfactory);
        /// <summary>
        /// Called when the branch collection gets unload from this implementation.
        /// </summary>
        void OnUnloadBranchDescriptorCollection ();
        /// <summary>
        /// Called when a Branch Descriptor instance gets added to the collection.
        /// </summary>
        /// <param name="branchDescriptor">Added Branch Descriptor instance.</param>
        void OnAddBranchDescriptor (BranchDescriptor branchDescriptor);
        /// <summary>
        /// Called when a Branch Descriptor instance gets removed from the collection.
        /// </summary>
        /// <param name="branchDescriptor">Removed Branch Descriptor instance.</param>
        void OnRemoveBranchDescriptor (BranchDescriptor branchDescriptor);
        /// <summary>
        /// Called when a Variation Descriptor instance gets added to the collection.
        /// </summary>
        /// <param name="branchDescriptor">Added Variation Descriptor instance.</param>
        void OnAddVariationDescriptor (VariationDescriptor branchDescriptor);
        /// <summary>
        /// Called when a Variation Descriptor instance gets removed from the collection.
        /// </summary>
        /// <param name="branchDescriptor">Removed Variation Descriptor instance.</param>
        void OnRemoveVariationDescriptor (VariationDescriptor branchDescriptor);
        #endregion

        #region Editor Events
        /// <summary>
        /// Called when the editor Add Snapshot button gets clicked.
        /// </summary>
        /// <returns><c>True</c> if the action should be managed on this instance.</returns>
        bool OnEditorAddSnapshotClicked ();
        #endregion

        #region Draw
        /// <summary>
        /// Gets the editor header logo texture.
        /// </summary>
        /// <returns>Editor header logo texture.</returns>
        Texture2D GetHeaderLogo ();
        /// <summary>
		/// Draw additional handles on the mesh preview area.
		/// </summary>
		/// <param name="r">Rect</param>
		/// <param name="camera">Camera</param>
        void OnCanvasDrawHandles (Rect r, Camera camera);
        /// <summary>
        /// Draw the first options on the Select Mode View.
        /// </summary>
        void DrawSelectModeViewBeforeOptions ();
        /// <summary>
        /// Draw the second options on the Select Mode View.
        /// </summary>
        void DrawSelectModeViewAfterOptions ();
        /// <summary>
        /// Draws custom panels for the loaded structure collection.
        /// </summary>
        void DrawPanels (Rect windowRect);
        /// <summary>
        /// Draws LOD controls.
        /// </summary>
        /// <param name="r">Rect</param>
        /// <returns><c>True</c> to execute the base editor LOD control drawing</returns>
        bool DrawLODControls (Rect r);
        /// <summary>
        /// Called when the view in the editor changes.
        /// </summary>
        /// <param name="firsLevelIndex"></param>
        /// <param name="oldFirstLevelIndex"></param>
        /// <param name="secondLevelIndex"></param>
        /// <param name="oldSecondLevelIndex"></param>
        /// <param name="thirdLevelIndex"></param>
        /// <param name="oldThirdLevelIndex"></param>
        void ViewChanged (
            int firsLevelIndex, int oldFirstLevelIndex,
            int secondLevelIndex, int oldSecondLevelIndex,
            int thirdLevelIndex, int oldThirdLevelIndex);
        /// <summary>
        /// Creates materials to outline the variation group selected.
        /// </summary>
        /// <param name="variationGroupId">Id of the variation group being selected.</param>
        void CreateOutlineMaterials (int variationGroupId = -1);
        /// <summary>
        /// Refresh the current outline selection materials.
        /// </summary>
        void RefreshOutlineMaterials ();
        #endregion
    }
}