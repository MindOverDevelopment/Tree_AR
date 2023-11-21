﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Utils;
using Broccoli.Generator;

namespace Broccoli.Pipe {
	[System.Serializable]
	/// <summary>
	/// Structure generator element.
	/// </summary>
	public class StructureGeneratorElement : PipelineElement, ISproutGroupConsumer, IStructureGenerator, ISerializationCallbackReceiver {
		#region Vars
		/// <summary>
		/// Gets the type of the connection.
		/// </summary>
		/// <value>The type of the connection.</value>
		public override ConnectionType connectionType {
			get { return PipelineElement.ConnectionType.Source; }
		}
		/// <summary>
		/// Gets the type of the element.
		/// </summary>
		/// <value>The type of the element.</value>
		public override ElementType elementType {
			get { return PipelineElement.ElementType.StructureGenerator; }
		}
		/// <summary>
		/// Gets the type of the class.
		/// </summary>
		/// <value>The type of the class.</value>
		public override ClassType classType {
			get { return PipelineElement.ClassType.StructureGenerator; }
		}
		/// <summary>
		/// Value used to position elements in the pipeline. The greater the more towards the end of the pipeline.
		/// </summary>
		/// <value>The position weight.</value>
		public override int positionWeight {
			get { return PipelineElement.structureGeneratorWeight;	}
		}
		/// <summary>
		/// Gets a value indicating whether this <see cref="Broccoli.Pipe.StructureGeneratorElement"/> uses randomization.
		/// </summary>
		/// <value><c>true</c> if uses randomization; otherwise, <c>false</c>.</value>
		public override bool usesRandomization {
			get { return true; }
		}
		/// <summary>
		/// Keeps the offset of the canvas used to edit the levels.
		/// </summary>
		public Vector2 canvasOffset = Vector2.zero;
		/// <summary>
		/// Keeps the structure level tree on a simple list.
		/// </summary>
		public List<StructureGenerator.StructureLevel> flatStructureLevels = new List<StructureGenerator.StructureLevel> ();
		/// <summary>
		/// The structure levels.
		/// </summary>
		[System.NonSerialized]
		public List<StructureGenerator.StructureLevel> structureLevels = 
			new List<StructureGenerator.StructureLevel> ();
		/// <summary>
		/// Holds the generated structures for this tree.
		/// </summary>
		/// <typeparam name="StructureGenerator.Structure"></typeparam>
		/// <returns></returns>
		[SerializeField]
		public List<StructureGenerator.Structure> flatStructures = new List<StructureGenerator.Structure> ();
		/// <summary>
		/// The structures.
		/// </summary>
		[System.NonSerialized]
		public List<StructureGenerator.Structure> structures = 
			new List<StructureGenerator.Structure> ();
		/// <summary>
		/// Id to structure dictionary.
		/// </summary>
		/// <typeparam name="int"></typeparam>
		/// <typeparam name="StructureGenerator.Structure"></typeparam>
		/// <returns></returns>
		[System.NonSerialized] 	
		public Dictionary<int, StructureGenerator.Structure> idToStructure = new Dictionary<int, StructureGenerator.Structure> ();
		/// <summary>
		/// Guid to structure dictionary.
		/// </summary>
		/// <typeparam name="System.Guid"></typeparam>
		/// <typeparam name="StructureGenerator.Structure"></typeparam>
		/// <returns></returns>
		[System.NonSerialized] 	
		public Dictionary<System.Guid, StructureGenerator.Structure> guidToStructure = new Dictionary<System.Guid, StructureGenerator.Structure> ();
		/// <summary>
		/// The identifier used on the last added structure level.
		/// </summary>
		[System.NonSerialized]
		private int lastId = 0;
		/// <summary>
		/// The selected structure level. When 0 no structure level is selected (means root is selected).
		/// </summary>
		[System.NonSerialized]
		public StructureGenerator.StructureLevel selectedLevel = null;
		/// <summary>
		/// Root structure level with instructions to build the root branches of the tree.
		/// </summary>
		/// <returns>Root structure level.</returns>
		public StructureGenerator.StructureLevel rootStructureLevel = new StructureGenerator.StructureLevel ();
		/// <summary>
		/// Id to structure level dictionary.
		/// </summary>
		[System.NonSerialized]
		public Dictionary<int, StructureGenerator.StructureLevel> idToStructureLevels = 
			new Dictionary<int, StructureGenerator.StructureLevel> ();
		/// <summary>
		/// True if all the sprout structure levels are assigned to a sprout group (used on validation).
		/// </summary>
		bool allAssignedToGroup = true;
		public bool inspectStructureEnabled = true;
		/// <summary>
		/// The levels to delete.
		/// </summary>
		List<StructureGenerator.StructureLevel> levelsToDelete = new List<StructureGenerator.StructureLevel> ();
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.Pipe.StructureGeneratorElement"/> class.
		/// </summary>
		public StructureGeneratorElement () {
			this.elementName = "Structure Generator";
			this.elementHelpURL = "https://docs.google.com/document/d/1Nr6Z808i7X2zMFq8PELezPuSJNP5IvRx9C5lJxZ_Z-A/edit#heading=h.nupd9ljjpzl";
			this.elementDescription = "This node give you access to the structure levels used to build the tree." +
				" It displays a canvas to select and edit trunk, branches, roots and sprout level parameters.";
		}
		#endregion

		#region Serialization
		/// <summary>
		/// Prepares the flatStructure variable to hold the serialized information.
		/// </summary>
		public void OnBeforeSerialize() {
			flatStructures.Clear ();
			SerializeStructures (structures);
		}
		/// <summary>
		/// Deserializes the flatStructure to the working variables.
		/// </summary>
		public void OnAfterDeserialize () {
			DeserializeStructures ();
		}
		/// <summary>
		/// Prepares the structures to be serialized.
		/// </summary>
		/// <param name="structuresToSerialize">List of structures to serialize.</param>
		private void SerializeStructures (List<StructureGenerator.Structure> structuresToSerialize) {
			for (int i = 0; i < structuresToSerialize.Count; i++) {
				flatStructures.Add (structuresToSerialize[i]);

				// Set parent structure id.
				if (structuresToSerialize[i].parentStructure != null) {
					structuresToSerialize[i].parentStructureId = structuresToSerialize[i].parentStructure.id;

					if (structuresToSerialize[i].branch != null) {

						// Set branch parent id.
						if (structuresToSerialize[i].branch.parent != null) {
							structuresToSerialize[i].branch.parentBranchId = structuresToSerialize[i].branch.parent.id;
						} else {
							structuresToSerialize[i].branch.parentBranchId = -1;
						}
					}
				} else {
					structuresToSerialize[i].parentStructureId = -1;
				}
				SerializeStructures (structuresToSerialize[i].childrenStructures);
			}
		}
		/// <summary>
		/// Deserializes flatStructures to structures tree data. 
		/// </summary>
		public void DeserializeStructures () {
			structures.Clear ();
			idToStructure.Clear ();
			guidToStructure.Clear ();
			for (int i = 0; i < flatStructures.Count; i++) {
				flatStructures[i].childrenStructures.Clear ();
				flatStructures[i].branch.branches.Clear ();
				idToStructure.Add (flatStructures[i].id, flatStructures[i]);
				guidToStructure.Add (flatStructures[i].guid, flatStructures[i]);
			}
			StructureGenerator.Structure parentStructure;
			StructureGenerator.Structure childStructure;
			for (int i = 0; i < flatStructures.Count; i++) {
				childStructure = flatStructures[i];
				if (idToStructure.ContainsKey (flatStructures[i].parentStructureId)) {
					parentStructure = idToStructure[flatStructures[i].parentStructureId];

					// Add to parent or this element.
					parentStructure.childrenStructures.Add (childStructure);
					childStructure.parentStructure = parentStructure;
					childStructure.parentStructureId = parentStructure.id;

					// Add branch to parent.
					parentStructure.branch.AddBranch (childStructure.branch, false);
					childStructure.branch.parentBranchId = parentStructure.branch.id;
				} else if (flatStructures[i].parentStructureId == -1) {
					structures.Add (childStructure);
					childStructure.parentStructure = null;
					childStructure.parentStructureId = -1;
				}
			}
			// Update branches.
			for (int i = 0; i < structures.Count; i++) {
				structures[i].branch.Update (true);
			}
			
		}
		public void SetStructureLevelRecursive (StructureGenerator.StructureLevel structureLevel, int level) {
			structureLevel.level = level;
			/*
			for (int i = 0; i < structureLevel.childrenS; i++) {
				
			}
			*/
		}
		#endregion
		

		#region Validation
		/// <summary>
		/// Validate this instance.
		/// </summary>
		public override bool Validate () {
			log.Clear ();
			allAssignedToGroup = true;
			for (int i = 0; i < structureLevels.Count; i++) {
				ValidateGroupAssigned (structureLevels[i]);
				if (!allAssignedToGroup)
					break;
			}
			if (!allAssignedToGroup) {
				log.Enqueue (LogItem.GetWarnItem ("There are sprout levels not assigned to a group."));
			}
			this.RaiseValidateEvent ();
			return true;
		}
		/// <summary>
		/// Validates if a sprout structure level is assigned to a sprout group.
		/// </summary>
		/// <param name="level">Structure level.</param>
		private void ValidateGroupAssigned (StructureGenerator.StructureLevel level) {
			// TODO: protect against looping.
			if (allAssignedToGroup && level.isSprout && level.sproutGroupId <= 0) {
				allAssignedToGroup = false;
			}
			if (allAssignedToGroup) {
				for (int i = 0; i < level.structureLevels.Count; i++) {
					ValidateGroupAssigned (level.structureLevels[i]);
					if (!allAssignedToGroup)
						break;
				}
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// Raises the add to pipeline event.
		/// </summary>
		public override void OnAddToPipeline () {
			BuildStructureLevelTree ();
		}
		#endregion

		#region StructureLevels operations
		/// <summary>
		/// Gets the structure level identifier.
		/// </summary>
		/// <returns>The structure level identifier.</returns>
		private int GetStructureLevelId () {
			int id = lastId + 1;
			bool found = false;
			while (!found) {
				found = true;
				for (int i = 0; i < flatStructureLevels.Count; i++) {
					if (flatStructureLevels[i].id == id) found = false;
				}
				if (!found) id++;
			}
			lastId = id;
			return id;
		}
		/// <summary>
		/// Gets the index of the structure level.
		/// </summary>
		/// <returns>The structure level index.</returns>
		/// <param name="structureLevel">Structure level.</param>
		public int GetStructureLevelIndex (StructureGenerator.StructureLevel structureLevel) {
			int index = -1;
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i] == structureLevel) {
					index = i;
					break;
				}
			}
			return index;
		}
		/// <summary>
		/// Adds a new structure level.
		/// </summary>
		/// <returns>The structure level.</returns>
		/// <param name="nodeId">Node identifier.</param>
		/// <param name="isSprout">If set to <c>true</c> the structure level is for sprouts.</param>
		/// <param name="isRoot">If set to <c>true</c> the structure level is for roots.</param>
		public StructureGenerator.StructureLevel AddStructureLevel (int nodeId, bool isSprout = false, bool isRoot = false)
		{
			StructureGenerator.StructureLevel newLevel = new StructureGenerator.StructureLevel ();
			newLevel.id = nodeId;
			newLevel.parentId = -1;
			newLevel.isSprout = isSprout;
			newLevel.isRoot = isRoot;
			if (isRoot) {
				SetRootStructureLevel (newLevel);
			}

			AddStructureLevel (newLevel);
			return newLevel;
		}
		/// <summary>
		/// Adds a new structure level.
		/// </summary>
		/// <param name="structureLevel">StructureLevel instance to add.</param>
		public void AddStructureLevel (StructureGenerator.StructureLevel structureLevel) {
			flatStructureLevels.Add (structureLevel);
			idToStructureLevels.Add (structureLevel.id, structureLevel);
			BuildStructureLevelTree ();
			RaiseChangeEvent ();
		}
		/// <summary>
		/// Adds a new structure level.
		/// </summary>
		/// <returns>The structure level.</returns>
		/// <param name="parent">Parent structure level, null to have root as parent.</param>
		/// <param name="isSprout">If set to <c>true</c> the structure level is for sprouts.</param>
		public StructureGenerator.StructureLevel AddStructureLevel (StructureGenerator.StructureLevel parent = null,
			bool isSprout = false, bool isRoot = false)
		{
			StructureGenerator.StructureLevel newLevel = new StructureGenerator.StructureLevel ();
			newLevel.id = GetStructureLevelId ();
			newLevel.isSprout = isSprout;
			newLevel.isRoot = isRoot;
			if (parent != null) {
				newLevel.parentId = parent.id;
				newLevel.nodePosition = parent.nodePosition + new Vector2 (50, (isRoot?70:-70));
				// Root structure level has another root structure level as parent.
				if (isRoot) {
					SetRootStructureLevel (newLevel);
				}
			} else {
				newLevel.nodePosition = rootStructureLevel.nodePosition + new Vector2 (50, (isRoot?70:-70));
				// Root structure level at the main trunk.
				if (isRoot) {
					SetRootStructureLevel (newLevel, true);
				}
			}
			flatStructureLevels.Add (newLevel);
			idToStructureLevels.Add (newLevel.id, newLevel);
			BuildStructureLevelTree ();
			RaiseChangeEvent ();
			return newLevel;
		}
		/// <summary>
		/// Set default values for structure levels to generate roots.
		/// </summary>
		/// <param name="rootStructureLevel"></param>
		/// <param name="fromTrunk">True is the structure level has the trunk as parent.</param>
		public void SetRootStructureLevel (StructureGenerator.StructureLevel rootStructureLevel, bool fromTrunk = false) {
			if (fromTrunk) {
				rootStructureLevel.actionRangeEnabled = true;
				rootStructureLevel.minRange = 0.05f;
				rootStructureLevel.maxRange = 0.05f;
				rootStructureLevel.distribution = StructureGenerator.StructureLevel.Distribution.Whorled;
				rootStructureLevel.childrenPerNode = 5;
				rootStructureLevel.minFrequency = 4;
				rootStructureLevel.maxFrequency = 5;
				rootStructureLevel.gravityAlignAtBase = -0.1f;
				rootStructureLevel.gravityAlignAtTop = -0.1f;
				rootStructureLevel.parallelAlignAtBase = -0.1f;
				rootStructureLevel.parallelAlignAtTop = -0.1f;
				rootStructureLevel.minGirthScale = 0.7f;
				rootStructureLevel.maxGirthScale = 0.85f;
				rootStructureLevel.distributionAngleVariance = 0.05f;
				rootStructureLevel.distributionSpacingVariance = 0.05f;
				rootStructureLevel.distributionOrigin = StructureGenerator.StructureLevel.DistributionOrigin.FromTip;
			} else {
				rootStructureLevel.gravityAlignAtBase = -0.1f;
				rootStructureLevel.gravityAlignAtTop = -0.1f;
				rootStructureLevel.randomTwirlOffsetEnabled = false;
				rootStructureLevel.minFrequency = 2;
				rootStructureLevel.maxFrequency = 3;
			}
		}
		/// <summary>
		/// Adds a new structure level sharing odds of occurence with a sibling node.
		/// </summary>
		/// <returns>The new structure level.</returns>
		/// <param name="siblingLevel">Level to share the occurrence with.</param>
		/// <param name="isSprout">If set to <c>true</c> the structure level is for sprouts.</param>
		public StructureGenerator.StructureLevel AddSharedStructureLevel (
			StructureGenerator.StructureLevel siblingLevel, 
			bool isSprout = false)
		{
			if (siblingLevel != null) {
				StructureGenerator.StructureLevel newLevel = new StructureGenerator.StructureLevel ();
				newLevel.id = GetStructureLevelId ();
				newLevel.isSprout = isSprout;
				newLevel.parentId = siblingLevel.parentId;
				flatStructureLevels.Add (newLevel);
				idToStructureLevels.Add (newLevel.id, newLevel);

				// Set sharing next.
				int stepsFromMain = 1;
				StructureGenerator.StructureLevel lastLevel = GetLastInSharedGroup (siblingLevel, out stepsFromMain);
				lastLevel.sharingNextId = newLevel.id;
				newLevel.nodePosition = lastLevel.nodePosition + new Vector2 (40f, 0);

				// Set sharing group.
				if (siblingLevel.sharingGroupId == 0) {
					newLevel.sharingGroupId = siblingLevel.id;
				} else {
					newLevel.sharingGroupId = siblingLevel.sharingGroupId;
				}
					
				BuildStructureLevelTree ();
				return newLevel;
			}
			return null;
		}
		/// <summary>
		/// Gets the last structure level in a shared group.
		/// </summary>
		/// <returns>The last structure level in a shared group.</returns>
		/// <param name="memberLevel">Member level of the shared group.</param>
		/// <param name="position">Position on the shared group, non-zero based.</param>
		StructureGenerator.StructureLevel GetLastInSharedGroup (StructureGenerator.StructureLevel memberLevel, out int position) {
			position = 1;
			if (memberLevel.sharingGroupId == 0 && memberLevel.sharingNextId == 0) {
				return memberLevel;
			} else {
				int mainLevelId = memberLevel.id;
				if (memberLevel.sharingGroupId != 0)
					mainLevelId = memberLevel.sharingGroupId;
				StructureGenerator.StructureLevel currentLevel = idToStructureLevels [mainLevelId];
				int maxLoop = 40;
				do {
					currentLevel = idToStructureLevels [currentLevel.sharingNextId];
					position++;
					maxLoop--;
				} while (currentLevel.sharingNextId != 0 && maxLoop > 0);
				if (maxLoop <= 0) {
					Debug.LogWarning ("Probable endless loop found on shared structure levels.");
				}
				return currentLevel;
			}
		}
		/// <summary>
		/// Builds the structure level tree.
		/// </summary>
		public void BuildStructureLevelTree () {
			structureLevels.Clear ();
			Dictionary<int, List<StructureGenerator.StructureLevel>> levelRel = 
				new Dictionary<int, List<StructureGenerator.StructureLevel>> ();
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].parentId == 0) {
					structureLevels.Add (flatStructureLevels[i]);
					flatStructureLevels[i].parentStructureLevel = rootStructureLevel;
				} else {
					if (!levelRel.ContainsKey (flatStructureLevels[i].parentId)) {
						levelRel.Add (flatStructureLevels[i].parentId, new List<StructureGenerator.StructureLevel> ());
					}
					levelRel [flatStructureLevels[i].parentId].Add (flatStructureLevels[i]);
				}
				SproutGroups.SproutGroup sproutGroup = pipeline.sproutGroups.GetSproutGroup (flatStructureLevels[i].sproutGroupId);
				if (sproutGroup != null) {
					flatStructureLevels[i].sproutGroupColor = sproutGroup.GetColor ();
				}
			}
			idToStructureLevels.Clear ();
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (levelRel.ContainsKey (flatStructureLevels[i].id)) {
					flatStructureLevels[i].structureLevels = levelRel [flatStructureLevels[i].id];
					for (int j = 0; j < flatStructureLevels[i].structureLevels.Count; j++) {
						flatStructureLevels[i].structureLevels[j].parentStructureLevel = flatStructureLevels[i];
					}
				}
				idToStructureLevels.Add (flatStructureLevels [i].id, flatStructureLevels [i]);
				// Order structure levels by older id.
				//flatStructureLevels [i].structureLevels.Sort ((sl1,sl2) => s2.branch.position.CompareTo(s1.branch.position));
				//flatStructureLevels [i].structureLevels.Sort ((sl1,sl2) => (sl2.isSprout&&!sl1.isSprout?-1:1));
				flatStructureLevels [i].structureLevels.Sort ((sl1,sl2) => {
					if (sl2.isSprout && !sl1.isSprout) return -1;
					if (!sl2.isSprout && sl1.isSprout) return 1;
					return sl1.id.CompareTo (sl2.id);
				});
			}
			rootStructureLevel.structureLevels = structureLevels;
			rootStructureLevel.structureLevels.Sort ((sl1,sl2) => {
				if (sl2.isSprout && !sl1.isSprout) return -1;
				if (!sl2.isSprout && sl1.isSprout) return 1;
				return sl1.id.CompareTo (sl2.id);
			});
			UpdateDrawVisible ();
			levelRel.Clear ();
		}
		/// <summary>
		/// Removes a structure level.
		/// </summary>
		/// <param name="levelToDelete">Level to delete.</param>
		public void RemoveStructureLevel (StructureGenerator.StructureLevel levelToDelete) {
			MarkHierarchyForDeletion (levelToDelete);

			// Remove from any sharing group if present in one.
			if (levelToDelete.IsShared ()) {
				RemoveFromSharingGroup (levelToDelete);
			}

			// Delete all the levels marked for deletion (levelToDelete and its hierarchy).
			levelsToDelete.Clear ();
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].isMarkedForDeletion) {
					levelsToDelete.Add (flatStructureLevels[i]);
				}
			}
			for (int i = 0; i < levelsToDelete.Count; i++) {
				flatStructureLevels.Remove (levelsToDelete [i]);
				idToStructureLevels.Remove (levelsToDelete [i].id);
			}
			levelsToDelete.Clear ();

			//Delete structure level from parent
			if (levelToDelete.parentStructureLevel != null) {
				int indexAt = -1;
				for (int i = 0; i < levelToDelete.parentStructureLevel.structureLevels.Count; i++) {
					if (levelToDelete == levelToDelete.parentStructureLevel.structureLevels [i]) {
						indexAt = i;
					}
				}
				if (indexAt >= 0) {
					levelToDelete.parentStructureLevel.structureLevels.RemoveAt (indexAt);
				}
			}
			RemoveStructureLevelRecursive (levelToDelete);

			// Build the structure tree again.
			BuildStructureLevelTree ();

			// Cleaning up.
			levelsToDelete.Clear ();

			RaiseChangeEvent ();
		}
		/// <summary>
		/// Removes a list of structure levels given their ids..
		/// </summary>
		/// <param name="levelToDelete">Ids of the levels to delete.</param>
		public void RemoveStructureLevels (List<int> idsToDelete) {
			StructureGenerator.StructureLevel structureLevel;
			for (int i = 0; i < idsToDelete.Count; i++) {
				if (idToStructureLevels.ContainsKey (idsToDelete [i])) {
					structureLevel = idToStructureLevels [idsToDelete [i]];
					// Disconnect from parent.
					structureLevel.parentId = -1;
					if (structureLevel.parentStructureLevel != null) {
						structureLevel.parentStructureLevel.structureLevels.Remove (structureLevel);
					}
					structureLevel.parentStructureLevel = null;
					// Disconnect children.
					for (int j = 0; j < structureLevel.structureLevels.Count; j++) {
						structureLevel.structureLevels [j].parentId = -1;
						structureLevel.structureLevels.Remove (structureLevel.structureLevels [j]);
						structureLevel.structureLevels [j].parentStructureLevel = null;
					}
					// Disable node to remove.
					structureLevel.enabled = false;
					flatStructureLevels.Remove (structureLevel);
					idToStructureLevels.Remove (idsToDelete [i]);
				}
			}

			// Build the structure tree again.
			BuildStructureLevelTree ();

			RaiseChangeEvent ();
		}
		/// <summary>
		/// Adds a connection between a parent structure level and a child structure level, given their ids.
		/// </summary>
		/// <param name="parentId">Parent structure level id.</param>
		/// <param name="childId">Child structure level id.</param>
		public bool AddConnection (int parentId, int childId) {
			StructureGenerator.StructureLevel parentStructureLevel = null;
			StructureGenerator.StructureLevel childStructureLevel = null;
			bool isFromTrunk = false;
			if (parentId == 0) {
				parentStructureLevel = rootStructureLevel;
				isFromTrunk = true;
			} else if (idToStructureLevels.ContainsKey (parentId)) {
				parentStructureLevel = idToStructureLevels [parentId];
			}
			if (idToStructureLevels.ContainsKey (childId)) childStructureLevel = idToStructureLevels [childId];
			if (parentStructureLevel != null && childStructureLevel != null) {
				childStructureLevel.parentId = parentId;
				childStructureLevel.parentStructureLevel = parentStructureLevel;
				parentStructureLevel.structureLevels.Add (parentStructureLevel);
				if (isFromTrunk && childStructureLevel.isRoot) {
					SetRootStructureLevel (childStructureLevel, true);
				}

				// Build the structure tree again.
				BuildStructureLevelTree ();

				RaiseChangeEvent ();

				return true;
			}
			return false;
		}
		/// <summary>
		/// Remevo the connection of a list of children structure levels to their parent.
		/// </summary>
		/// <param name="childIds">Id of children level structures to remove their parent from.</param>
		public void RemoveConnections (List<int> childIds) {
			StructureGenerator.StructureLevel structureLevel;
			for (int i = 0; i < childIds.Count; i++) {
				if (idToStructureLevels.ContainsKey (childIds [i])) {
					structureLevel = idToStructureLevels [childIds [i]];
					// Disconnect from parent.
					structureLevel.parentId = -1;
					if (structureLevel.parentStructureLevel != null) {
						structureLevel.parentStructureLevel.structureLevels.Remove (structureLevel);
					}
					structureLevel.parentStructureLevel = null;
				}
			}

			// Build the structure tree again.
			BuildStructureLevelTree ();

			RaiseChangeEvent ();
		}
		/// <summary>
		/// Marks a structure level for deletion.
		/// </summary>
		/// <param name="levelToMark">Level to mark.</param>
		private void MarkHierarchyForDeletion (StructureGenerator.StructureLevel levelToMark) {
			levelToMark.isMarkedForDeletion = true;
			for (int i = 0; i < levelToMark.structureLevels.Count; i++) {
				MarkHierarchyForDeletion (levelToMark.structureLevels[i]);
			}
		}
		/// <summary>
		/// Removes the structure levels recursively.
		/// </summary>
		/// <param name="level">Level.</param>
		private void RemoveStructureLevelRecursive (StructureGenerator.StructureLevel level) {
			for (int i = 0; i < level.structureLevels.Count; i++) {
				RemoveStructureLevelRecursive (level.structureLevels[i]);
			}
			level.structureLevels.Clear ();
		}
		/// <summary>
		/// Removes a structure level from a sharing group, updating id references.
		/// </summary>
		/// <param name="levelToDelete">Level to delete.</param>
		private void RemoveFromSharingGroup (StructureGenerator.StructureLevel levelToDelete) {
			if (levelToDelete.sharingGroupId == 0) {
				// levelToDelete is main, so we turn the next level to main
				StructureGenerator.StructureLevel nextLevel = idToStructureLevels [levelToDelete.sharingNextId];
				nextLevel.sharingGroupId = 0;

				// Update the levels on the sharing group with the new sharing group id.
				int sharingGroupId = nextLevel.id;
				int maxLoop = 40;
				do {
					if (idToStructureLevels.ContainsKey (nextLevel.sharingNextId)) {
						nextLevel = idToStructureLevels [nextLevel.sharingNextId];
						nextLevel.sharingGroupId = sharingGroupId;
					} else {
						nextLevel = null;
					}
					maxLoop --;
				} while (nextLevel != null && maxLoop > 0);
				if (maxLoop <= 0) {
					Debug.LogWarning ("Probable endless loop found on shared structure levels.");
				}
			} else {
				// Level is last or somewhere in the middle of the sharing group.
				// First we get the main level for the group.
				StructureGenerator.StructureLevel currentLevel = idToStructureLevels [levelToDelete.sharingGroupId];
				// Reference to the previous element.
				StructureGenerator.StructureLevel previousLevel = currentLevel;
				int maxLoop = 40;
				bool addPositionOffset = false;
				do {
					if (idToStructureLevels.ContainsKey (currentLevel.sharingNextId)) {
						currentLevel = idToStructureLevels [currentLevel.sharingNextId];
						if (currentLevel.id == levelToDelete.id) {
							previousLevel.sharingNextId = currentLevel.sharingNextId;
							addPositionOffset = true;
						}
						if (addPositionOffset)
							currentLevel.nodePosition -= new Vector2 (40, 0);
						previousLevel = currentLevel;
					} else {
						currentLevel = null;
					}
					maxLoop --;
				} while (currentLevel != null && maxLoop > 0);
				if (maxLoop <= 0) {
					Debug.LogWarning ("Probable endless loop found on shared structure levels.");
				}
			}
		}
		/// <summary>
		/// Updates the draw visible structure levels.
		/// </summary>
		public void UpdateDrawVisible () {
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				flatStructureLevels[i].isDrawVisible = true;
			}
			for (int i = 0; i < structureLevels.Count; i++) {
				SetDrawVisible (structureLevels[i]);
			}
		}
		/// <summary>
		/// Sets the draw visible structure levels.
		/// </summary>
		/// <param name="levelToSet">Level to set.</param>
		/// <param name="overrideToFalse">If set to <c>true</c> override to false.</param>
		private void SetDrawVisible (StructureGenerator.StructureLevel levelToSet, bool overrideToFalse = false) {
			if (!levelToSet.enabled || overrideToFalse) {
				levelToSet.isDrawVisible = false;
				overrideToFalse = true;
			}
			for (int i = 0; i < levelToSet.structureLevels.Count; i++) {
				SetDrawVisible (levelToSet.structureLevels[i], overrideToFalse);
			}
		}
		#endregion

		#region Cloning
		/// <summary>
		/// Clone this instance.
		/// </summary>
		/// <param name="isDuplicate">If <c>true</c> then the clone has elements with new ids.</param>
		/// <returns>Clone of this instance.</returns>
		override public PipelineElement Clone (bool isDuplicate = false) {
			StructureGeneratorElement clone = ScriptableObject.CreateInstance<StructureGeneratorElement> ();
			SetCloneProperties (clone, isDuplicate);
			clone.rootStructureLevel = rootStructureLevel.Clone ();
			clone.canvasOffset = canvasOffset;
			clone.inspectStructureEnabled = inspectStructureEnabled;
			int maxStructureLevelId = 0;
			if (isDuplicate) {
				for (int i = 0; i < flatStructureLevels.Count; i++) {
					if (flatStructureLevels[i].id > maxStructureLevelId) {
						maxStructureLevelId = flatStructureLevels[i].id;
					}
				}
				maxStructureLevelId += 1;
			}
			StructureGenerator.StructureLevel structureLevelClone;
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				structureLevelClone = flatStructureLevels[i].Clone ();
				structureLevelClone.id += maxStructureLevelId;
				if (structureLevelClone.parentId > 0)structureLevelClone.parentId += maxStructureLevelId;
				clone.flatStructureLevels.Add (structureLevelClone);
			}
			int maxStructureId = 0;
			if (isDuplicate) {
				for (int i = 0; i < flatStructures.Count; i++) {
					if (flatStructures[i].id > maxStructureId) {
						maxStructureId = flatStructures[i].id;
					}
				}
				maxStructureId += 1;
			}
			StructureGenerator.Structure structureClone;
			for (int i = 0; i < flatStructures.Count; i++) {
				structureClone = flatStructures[i].Clone ();
				structureClone.id += maxStructureId;
				if (structureClone.parentStructureId > 0) structureClone.parentStructureId += maxStructureId;
				structureClone.generatorId += maxStructureLevelId;
				structureClone.mainGeneratorId += maxStructureLevelId;
				clone.flatStructures.Add (structureClone);
			}
			clone.DeserializeStructures ();
			return clone;
		}
		#endregion

		#region Sprout Group Consumer
		/// <summary>
		/// Look if certain sprout group is being used in this element.
		/// </summary>
		/// <returns><c>true</c>, if sprout group is being used, <c>false</c> otherwise.</returns>
		/// <param name="sproutGroupId">Sprout group identifier.</param>
		public bool HasSproutGroupUsage (int sproutGroupId) {
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].isSprout && flatStructureLevels[i].sproutGroupId == sproutGroupId)
					return true;
			}
			return false;
		}
		/// <summary>
		/// Commands the element to stop using certain sprout group.
		/// </summary>
		/// <param name="sproutGroupId">Sprout group identifier.</param>
		/// <returns><c>True</c> if sprout group has been removed, <c>false</c> otherwise.</returns>
		public bool StopSproutGroupUsage (int sproutGroupId) {
			bool hasRemoved = false;
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].isSprout && flatStructureLevels[i].sproutGroupId == sproutGroupId) {
					#if UNITY_EDITOR
					UnityEditor.Undo.RecordObject (this, "Sprout Group Removed from Level");
					#endif
					flatStructureLevels[i].sproutGroupId = 0;
					flatStructureLevels[i].sproutGroupColor = Color.clear;
					hasRemoved = true;
				}
			}
			return hasRemoved;
		}
		/// <summary>
		/// Gets the array of ids of groups consumed by this element.
		/// </summary>
		/// <returns>Arrays of group ids.</returns>
		public int[] GetGroupIds () {
			List<int> ids = new List<int> ();
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].isSprout) {
					ids.Add (flatStructureLevels [i].sproutGroupId);
				}
			}
			return ids.ToArray ();
		}
		/// <summary>
		/// Gets the array of colors for the groups consumed by this element.
		/// </summary>
		/// <returns>Arrays of group colors.</returns>
		public Color[] GetGroupColors () {
			List<Color> colors = new List<Color> ();
			for (int i = 0; i < flatStructureLevels.Count; i++) {
				if (flatStructureLevels[i].isSprout) {
					if (pipeline != null) {
						colors.Add (pipeline.sproutGroups.GetSproutGroupColor (flatStructureLevels [i].sproutGroupId));
					} else {
						colors.Add (Color.black);
					}
				}
			}
			return colors.ToArray ();
		}
		#endregion
	}
}
