﻿using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.Component
{
	/// <summary>
	/// Sprout mesh generator component.
	/// </summary>
	public class SproutMeshGeneratorComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The sprout mesh builder.
		/// </summary>
		SproutMeshBuilder sproutMeshBuilder = null;
		/// <summary>
		/// Advanced sprout mesh builder.
		/// </summary>
		AdvancedSproutMeshBuilder advancedSproutMeshBuilder = null;
		/// <summary>
		/// The sprout mesh generator element.
		/// </summary>
		SproutMeshGeneratorElement sproutMeshGeneratorElement = null;
		/// <summary>
		/// The sprout meshes relationship between their group id and the assigned sprout mesh.
		/// </summary>
		Dictionary<int, SproutMesh> sproutMeshes = new Dictionary <int, SproutMesh> ();
		/// <summary>
		/// The sprout mappers.
		/// </summary>
		Dictionary<int, SproutMap> sproutMappers = new Dictionary <int, SproutMap> ();
		/// <summary>
		/// Flag to reduce the complexity of sprouts for LOD purposes.
		/// </summary>
		bool simplifySprouts = false;
		/// <summary>
		/// Saves scaling relative to a map area on an atlas texture.
		/// </summary>
		/// <typeparam name="int">Sprout group multiplied by 10000 plus the index of the map area.</typeparam>
		/// <typeparam name="float">Scaling.</typeparam>
		Dictionary<int, float> _mapAreaToScale = new Dictionary<int, float> ();
		#endregion

		#region Configuration
		/// <summary>
		/// Prepares the parameters to process with this component.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		protected override void PrepareParams (TreeFactory treeFactory,
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) 
		{
			base.PrepareParams (treeFactory, useCache, useLocalCache, processControl);
			sproutMeshBuilder = SproutMeshBuilder.GetInstance ();
			advancedSproutMeshBuilder = AdvancedSproutMeshBuilder.GetInstance ();

			// Gather all SproutMap objects from elements downstream.
			PipelineElement pipelineElement = 
				sproutMeshGeneratorElement.GetDownstreamElement (PipelineElement.ClassType.SproutMapper);
			sproutMappers.Clear ();
			if (pipelineElement != null && pipelineElement.isActive) {
				SproutMapperElement sproutMapperElement = (SproutMapperElement)pipelineElement;
				for (int i = 0; i < sproutMapperElement.sproutMaps.Count; i++) {
					if (sproutMapperElement.sproutMaps[i].groupId > 0) {
						sproutMappers.Add (sproutMapperElement.sproutMaps[i].groupId, sproutMapperElement.sproutMaps[i]);
					}
				}
			}

			// Prepare tree sprouts.
			tree.SetHelperSproutIds ();

			// Gather all SproutMesh objects from element.
			sproutMeshes.Clear ();
			for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
				sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes[i].groupId, sproutMeshGeneratorElement.sproutMeshes[i]);
			}

			sproutMeshBuilder.globalScale = treeFactory.treeFactoryPreferences.factoryScale;
			sproutMeshBuilder.SetGravity (GlobalSettings.gravityDirection);
			sproutMeshBuilder.mapST = true;

			advancedSproutMeshBuilder.globalScale = treeFactory.treeFactoryPreferences.factoryScale;
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.StructureGirth; // TODO
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
			if (sproutMeshBuilder != null)
				sproutMeshBuilder.Clear ();
			sproutMeshBuilder = null;
			if (advancedSproutMeshBuilder != null)
				advancedSproutMeshBuilder.Clear ();
			advancedSproutMeshBuilder = null;
			sproutMeshGeneratorElement = null;
			sproutMeshes.Clear ();
			sproutMappers.Clear ();
			_mapAreaToScale.Clear ();
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="processControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl processControl = null) {
			if (pipelineElement != null && tree != null) {
				sproutMeshGeneratorElement = pipelineElement as SproutMeshGeneratorElement;
				PrepareParams (treeFactory, useCache, useLocalCache, processControl);
				BuildMesh (treeFactory, processControl.lodIndex);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			treeFactory.meshManager.DeregisterMeshByType (MeshManager.MeshData.Type.Sprout);
			if (sproutMeshGeneratorElement != null) {
				sproutMeshGeneratorElement.verticesCount = 0;
				sproutMeshGeneratorElement.trianglesCount = 0;
			}
		}
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private void BuildMesh (TreeFactory treeFactory, int lodIndex) {
			var sproutMeshesEnumerator = sproutMeshes.GetEnumerator ();
			PrepareBuilder (sproutMeshes, sproutMappers);
			sproutMeshBuilder.PrepareBuilder (sproutMeshes, sproutMappers);
			//advancedSproutMeshBuilder.PrepareBuilder (sproutMeshes, sproutMappers);
			SproutMesh sproutMesh;
			sproutMeshGeneratorElement.PrepareSeed ();
			Mesh groupMesh = null;
			int verticesCount = 0;
			int trisCount = 0;
			while (sproutMeshesEnumerator.MoveNext ()) {
				sproutMesh = sproutMeshesEnumerator.Current.Value;
				if (sproutMesh.meshingMode == SproutMesh.MeshingMode.Shape) {
					groupMesh = BuildAdvancedShapeMesh (treeFactory, lodIndex, sproutMesh);
				} else if (sproutMesh.meshingMode == SproutMesh.MeshingMode.BranchCollection) {
					// Normalize LOD to SproutCollection.
					lodIndex = NormalizeToBranchCollectionLOD (treeFactory, lodIndex);
					groupMesh = BuildBranchCollectionMesh (treeFactory, lodIndex, sproutMesh);
				}
				if (groupMesh != null) {
					verticesCount += groupMesh.vertexCount;
					trisCount += (int)(groupMesh.triangles.Length / 3);
				}
			}
			sproutMeshGeneratorElement.verticesCount = verticesCount;
			sproutMeshGeneratorElement.trianglesCount = trisCount;
		}
		private int NormalizeToBranchCollectionLOD (TreeFactory treeFactory, int lodIndex) {
			return 0;
		}
		/// <summary>
		/// Analyzes the SproutMesh and SproutMap instances in preparation to build meshes.
		/// Takes the size of SproutMap areas to assign a scale to each one.
		/// </summary>
		/// <param name="sproutMeshes">Relationship between Sprout Group Id and SproutMesh instance.</param>
		/// <param name="sproutMappers">Relationship between Sprout Group Id and SproutMap instance if present.</param>
		public void PrepareBuilder (Dictionary<int, SproutMesh> sproutMeshes, Dictionary<int, SproutMap> sproutMappers) {
			// Clean sprout mesh to atlas dictionary
			// Iterate through all areas.
			// Save the scalings.
			var sproutMappersEnumerator = sproutMappers.GetEnumerator ();
			int groupId;
			SproutMap sproutMap;
			SproutMap.SproutMapArea sproutArea;
			List<float> areaDiagonals = new List<float> ();
			float maxDiagonal = -1f;
			float diagonal;
			_mapAreaToScale.Clear ();
			while (sproutMappersEnumerator.MoveNext ()) {
				groupId = sproutMappersEnumerator.Current.Key;
				sproutMap = sproutMappersEnumerator.Current.Value;
				areaDiagonals.Clear ();
				maxDiagonal = -1f;
				for (int i = 0; i < sproutMap.sproutAreas.Count; i++) {
					sproutArea = sproutMap.sproutAreas [i];
					if (sproutArea.enabled && sproutArea.texture != null) {
						diagonal = sproutArea.diagonal;
						if (diagonal > maxDiagonal) {
							maxDiagonal = diagonal;
						}
					} else {
						diagonal = 0f;
					}
					areaDiagonals.Add (diagonal);
				}
				for (int i = 0; i < areaDiagonals.Count; i++) {
					int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (groupId, i);
					_mapAreaToScale.Add (meshId, areaDiagonals [i] / maxDiagonal);
				}
			}
		}
		#endregion

		#region Process Shape Mesh
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private Mesh BuildAdvancedShapeMesh (TreeFactory treeFactory, int lodIndex, SproutMesh sproutMesh) {
			int groupId = sproutMesh.groupId;

			// Validation... the Sprout Group does not exist.
			if (!pipelineElement.pipeline.sproutGroups.HasSproutGroup (groupId)) return null;

			// Preparing Mesh to return.
			Mesh groupMesh = null;
			// Shape Meshes having mappers (planes, grid).
			if (sproutMappers.ContainsKey (groupId) && sproutMeshes[groupId].shapeMode != SproutMesh.ShapeMode.Mesh) {
				if (sproutMappers [groupId].IsTextured ()) {
					// Assign Sprouts to their subgroup.
					advancedSproutMeshBuilder.AssignSproutSubgroups (tree, groupId, sproutMappers [groupId]);

					// For each sprout area:
					List<SproutMap.SproutMapArea> sproutAreas = sproutMappers [groupId].sproutAreas;
					for (int i = 0; i < sproutAreas.Count; i++) {
						if (sproutAreas[i].enabled) {
							// Create the mesh for the Sprout Group and Subgroup (area).
							/*
							groupMesh = sproutMeshBuilder.MeshSprouts (tree, 
								groupId, TranslateSproutMesh (sproutMeshes [groupId]), sproutAreas[i], i, false);
								*/
							advancedSproutMeshBuilder.SetMapArea (sproutAreas[i]);
							int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (groupId, i);
							Mesh baseMesh = GetBaseMesh (sproutMesh, sproutAreas[i], meshId);
							advancedSproutMeshBuilder.RegisterMesh (baseMesh, groupId, i);
							groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, i);

							// Customize the normals. TODO: jobify.
							ApplyNormalMode (groupMesh, Vector3.zero);

							// Register the created Mesh on the MeshManager.
							treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId, i);

							// Removing SproutMeshData, now all data should be available at the Mesh data.
							/*
							List<SproutMeshBuilder.SproutMeshData> sproutMeshDatas = sproutMeshBuilder.sproutMeshData;
							for (int j = 0; j < sproutMeshDatas.Count; j++) {
								MeshManager.MeshPart meshPart = treeFactory.meshManager.AddMeshPart (sproutMeshDatas[j].startIndex, 
																	sproutMeshDatas[j].length,
																	sproutMeshDatas[j].position, 
																	0, 
																	sproutMeshDatas[j].origin,
																	MeshManager.MeshData.Type.Sprout,
																	groupId,
																	i);
								meshPart.sproutId = sproutMeshDatas[j].sproutId;
								meshPart.branchId = sproutMeshDatas[j].branchId;
							}
							*/
						} else {
							// Deregister a Mesh if its subgroup is disabled.
							treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId, i);
						}
					}
				} else {
					int meshId = advancedSproutMeshBuilder.GetGroupSubgroupId (groupId);
					// Create the mesh for the Sprout group.
					Mesh baseMesh = GetBaseMesh (sproutMesh, null, meshId);
					advancedSproutMeshBuilder.RegisterMesh (baseMesh, groupId);
					groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId);

					// Customize the normals, TODO: jobify.
					ApplyNormalMode (groupMesh, Vector3.zero);
					
					// Register the created Mesh on the MeshManager.
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				}
			}

			// Shape Meshes having no mappers (unassigned or custom Mesh based).
			else {
				// Process without sprout areas.
				groupMesh = sproutMeshBuilder.MeshSprouts (tree, groupId, sproutMeshes [groupId]);
				ApplyNormalMode (groupMesh, Vector3.zero);
				treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId);
				treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
			}

			return groupMesh;
		}
		private Mesh GetBaseMesh (SproutMesh sproutMesh, SproutMap.SproutMapArea sproutArea = null, int meshId = 0) {
			// Prepare params for plane.
			float width = sproutMesh.width;
			float height = sproutMesh.height;
			float pivotW = sproutMesh.pivotX;
			float pivotH = sproutMesh.pivotY;
			float uvX = 0f;
			float uvY = 0f;
			float uvWidth = 1f;
			float uvHeight = 1f;
			int uvDir = 0;
			if (sproutArea != null && 
				sproutArea.enabled &&
				sproutArea.width > 0 && 
				sproutArea.texture != null) {
				if (sproutMesh.overrideHeightWithTexture)
					height = sproutMesh.width * sproutArea.normalizedHeightPx / (float)sproutArea.normalizedWidthPx;
				if (sproutMesh.includeScaleFromAtlas) {
					width *= _mapAreaToScale [meshId];
					height *= _mapAreaToScale [meshId];
				}
				sproutMesh.overridedHeight = height;
			}
			if (sproutArea != null) {
				pivotW = sproutArea.normalizedPivotX;
				pivotH = sproutArea.normalizedPivotY;
				uvX = sproutArea.x;
				uvY = sproutArea.y;
				uvWidth = sproutArea.width;
				uvHeight = sproutArea.height;
				uvDir = sproutArea.normalizedStep;
			}

			// Build mesh.
			Mesh baseMesh = null;
			switch (sproutMesh.shapeMode) {
				case SproutMesh.ShapeMode.PlaneX:
					PlaneXSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
					PlaneXSproutMeshBuilder.SetIdData (meshId);
					baseMesh = PlaneXSproutMeshBuilder.GetPlaneXMesh (width, height, pivotW, pivotH, sproutMesh.depth);
					break;
				case SproutMesh.ShapeMode.GridPlane:
					GridSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
					GridSproutMeshBuilder.SetIdData (meshId);
					baseMesh = GridSproutMeshBuilder.GetGridMesh (
						width, height, sproutMesh.resolutionWidth, sproutMesh.resolutionHeight, pivotW, pivotH);
					break;
				case SproutMesh.ShapeMode.Mesh:
					MeshSproutMeshBuilder.SetIdData (meshId);
					baseMesh = PlaneSproutMeshBuilder.GetPlaneMesh (1f, 1f, 0f, 0f, 1);
					break;
				default:
					int planes = 1;
					if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Cross) {
						planes = 2;
					} else if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Tricross) {
						planes = 3;
					}
					if (sproutMesh.shapeMode == SproutMesh.ShapeMode.Cross) {
						GridSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
						GridSproutMeshBuilder.SetIdData (meshId);
						baseMesh = GridSproutMeshBuilder.GetGridMesh (
							width, height, sproutMesh.resolutionWidth, sproutMesh.resolutionHeight, pivotW, pivotH, 2);
					} else {
						PlaneSproutMeshBuilder.SetUVData (uvX, uvY, uvWidth, uvHeight, uvDir);
						PlaneSproutMeshBuilder.SetIdData (meshId);
						baseMesh = PlaneSproutMeshBuilder.GetPlaneMesh (
							width, height, pivotW, pivotH, planes);
					}
					break;
			}
			return baseMesh;
		}
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private Mesh BuildShapeMesh (TreeFactory treeFactory, int lodIndex, SproutMesh sproutMesh) {
			Mesh groupMesh = null;
			int groupId = sproutMesh.groupId;
			bool isTwoSided = treeFactory.materialManager.IsSproutTwoSided ();
			if (pipelineElement.pipeline.sproutGroups.HasSproutGroup (groupId)) {
				if (sproutMappers.ContainsKey (groupId) && sproutMeshes[groupId].shapeMode != SproutMesh.ShapeMode.Mesh) {
					if (sproutMappers [groupId].IsTextured ()) {
						sproutMeshBuilder.AssignSproutSubgroups (tree, groupId, sproutMappers [groupId]);
						List<SproutMap.SproutMapArea> sproutAreas = sproutMappers [groupId].sproutAreas;
						for (int i = 0; i < sproutAreas.Count; i++) {
							if (sproutAreas[i].enabled) {
								groupMesh = sproutMeshBuilder.MeshSprouts (tree, 
									groupId, TranslateSproutMesh (sproutMeshes [groupId]), sproutAreas[i], i, isTwoSided);
								ApplyNormalMode (groupMesh, Vector3.zero);
								treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId, i);
								treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId, i);
							} else {
								treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId, i);
							}
						}
					} else {
						groupMesh = sproutMeshBuilder.MeshSprouts (tree, groupId, TranslateSproutMesh (sproutMeshes [groupId]));
						ApplyNormalMode (groupMesh, Vector3.zero);
						treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId);
						treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
					}
				} else {
					// Process without sprout areas.
					groupMesh = sproutMeshBuilder.MeshSprouts (tree, groupId, sproutMeshes [groupId]);
					ApplyNormalMode (groupMesh, Vector3.zero);
					treeFactory.meshManager.DeregisterMesh (MeshManager.MeshData.Type.Sprout, groupId);
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				}
			}
			return groupMesh;
		}
		/// <summary>
		/// Reprocess normals for the sprout mesh.
		/// </summary>
		/// <param name="targetMesh">Target sprout mesh.</param>
		/// <param name="offset">Vector3 offset from the normal reference point (depending on the normal mode applied).</param>
		void ApplyNormalMode (Mesh targetMesh, Vector3 offset) {
			// PER SPROUT (Unchanged).
			if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.PerSprout) return;

			Vector3 referenceCenter = targetMesh.bounds.center;
			// TREE ORIGIN.
			if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.TreeOrigin) {
				referenceCenter.y = 0;
			} 
			// SPROUT BASE/CENTER, get sprouts bounds.
			else {
				Bounds sproutsBounds = GetSproutsBounds (targetMesh);
				referenceCenter = sproutsBounds.center;
				//SPROUT BASE
				if (sproutMeshGeneratorElement.normalMode == SproutMeshGeneratorElement.NormalMode.SproutsBase) {
					referenceCenter.y -= sproutsBounds.min.y;
				}
			}
			List<Vector3> normals = new List<Vector3> ();
			List<Vector3> vertices = new List<Vector3> ();
			targetMesh.GetNormals (normals);
			targetMesh.GetVertices (vertices);
			for (int i = 0; i < normals.Count; i++) {
				normals [i] = Vector3.Lerp (normals[i], (vertices[i] - referenceCenter + offset).normalized, sproutMeshGeneratorElement.normalModeStrength);
			}
			targetMesh.SetNormals (normals);
		}
		Bounds GetSproutsBounds (Mesh targetMesh) {
			Bounds sproutBounds = new Bounds ();
			if (targetMesh != null && targetMesh.vertexCount > 0 && targetMesh.subMeshCount > 0) {
				UnityEngine.Rendering.SubMeshDescriptor smd = targetMesh.GetSubMesh (0);
				int sproutVertexStart = smd.firstVertex;
				int vertexCount = targetMesh.vertexCount;
				Vector3[] vertices = targetMesh.vertices;
				for (int i = sproutVertexStart; i < vertexCount; i++) {
					sproutBounds.Encapsulate (vertices [i]);
				}
			}
			return sproutBounds;
		}
		/// <summary>
		/// Simplifies sprout mesh parameters for LOD purposes.
		/// </summary>
		/// <param name="sproutMesh">SproutMesh to evaluate.</param>
		/// <returns>Translated SproutMesh.</returns>
		SproutMesh TranslateSproutMesh (SproutMesh sproutMesh) {
			if (simplifySprouts) {
				if (sproutMesh.shapeMode == SproutMesh.ShapeMode.GridPlane) {
					SproutMesh simplyfiedSproutMesh = sproutMesh.Clone ();
					if (sproutMesh.resolutionHeight > sproutMesh.resolutionWidth) {
						simplyfiedSproutMesh.resolutionWidth = 1;
						simplyfiedSproutMesh.resolutionHeight = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionHeight / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionHeight);
					} else if (sproutMesh.resolutionWidth > sproutMesh.resolutionHeight) {
						simplyfiedSproutMesh.resolutionHeight = 1;
						simplyfiedSproutMesh.resolutionWidth = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionWidth / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionWidth);
					} else {
						simplyfiedSproutMesh.resolutionHeight = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionHeight / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionHeight);
						simplyfiedSproutMesh.resolutionWidth = 
						(int) Mathf.Clamp ( (float) simplyfiedSproutMesh.resolutionWidth / 2f,
							2.0f, 
							(float) simplyfiedSproutMesh.resolutionWidth);
					}
					return simplyfiedSproutMesh;
				} else if (sproutMesh.shapeMode == SproutMesh.ShapeMode.PlaneX) {
					
				}
			}
			return sproutMesh;
		}
		#endregion

		#region Process Branch Collection Mesh
		/// <summary>
		/// Builds the mesh or meshes for the sprouts.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="lodIndex">Index for the LOD definition.</param>
		private Mesh BuildBranchCollectionMesh (TreeFactory treeFactory, int lodIndex, SproutMesh sproutMesh) {
			Mesh groupMesh = null;
			int groupId = sproutMesh.groupId;
			bool isTwoSided = treeFactory.materialManager.IsSproutTwoSided ();
			if (pipelineElement.pipeline.sproutGroups.HasSproutGroup (groupId) && sproutMesh.branchCollection != null) {
				// Get the branch collection.
				BranchDescriptorCollection branchCollection = ((BranchDescriptorCollectionSO)sproutMesh.branchCollection).branchDescriptorCollection;

				// Assign the sprout subgroups.
				sproutMeshBuilder.AssignSproutSubgroups (tree, groupId, branchCollection, sproutMesh);

				// Register the branch collection.
				RegisterBranchCollection (treeFactory, lodIndex, sproutMesh, branchCollection);

				// Generate a mesh for each snapshot.
				treeFactory.meshManager.DeregisterSproutGroupMeshes (groupId);
				if (sproutMesh.subgroups.Length == 0) {
					groupMesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, -1);
					ApplyNormalMode (groupMesh, Vector3.zero);
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				} else {
					groupMesh = new Mesh ();
					CombineInstance[] combine = new CombineInstance [sproutMesh.subgroups.Length];
					for (int i = 0; i < sproutMesh.subgroups.Length; i++) {
						combine [i].mesh = advancedSproutMeshBuilder.MeshSprouts (tree, sproutMesh, groupId, sproutMesh.subgroups [i]);
						combine [i].transform = Matrix4x4.identity;
						combine [i].subMeshIndex = 0;
						ApplyNormalMode (combine [i].mesh, Vector3.zero);
					}
					groupMesh.CombineMeshes (combine, true, false);
					treeFactory.meshManager.RegisterSproutMesh (groupMesh, groupId);
				}
			}
			return groupMesh;
		}
		private void RegisterBranchCollection (
			TreeFactory treeFactory, 
			int lodIndex, 
			SproutMesh sproutMesh, 
			BranchDescriptorCollection branchDescriptorCollection)
		{
			SproutCompositeManager.Current ().Clear ();

			for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
				Mesh meshToRegister = BranchCollectionSproutMeshBuilder.GetMesh (
					branchDescriptorCollection, i, lodIndex, 
					Vector3.one, Vector3.zero, Quaternion.identity);
					advancedSproutMeshBuilder.RegisterMesh (meshToRegister, sproutMesh.groupId, i);
			}
		}
		/// <summary>
		/// Applies scale and rotation to meshes coming from SproutLab's branch descriptor collection.
		/// </summary>
		/// <param name="mesh">Mesh to appy the transformation.</param>
		/// <param name="scale">Scale transformation.</param>
		/// <param name="rotation">Rotation transformation.</param>
		private void NormalizeBranchCollectionTransform (Mesh mesh, float scale, Quaternion rotation) {
			Vector3[] _vertices = mesh.vertices;
			Vector3[] _normals = mesh.normals;
			Vector4[] _tangents = mesh.tangents;
			for (int i = 0; i < _vertices.Length; i++) {
				_vertices [i] = rotation * _vertices [i] * scale;
				_normals [i] = rotation * _normals [i];
				_tangents [i] = rotation * _tangents [i];
			}
			mesh.vertices = _vertices;
			mesh.normals = _normals;
			mesh.tangents = _tangents;
			mesh.RecalculateBounds ();
		}
		#endregion
	}
}