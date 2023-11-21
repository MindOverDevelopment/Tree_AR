using System.Collections.Generic;

using UnityEngine;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Manager;
using Broccoli.Factory;

namespace Broccoli.Component
{
	using AssetManager = Broccoli.Manager.AssetManager;
	/// <summary>
	/// Branch mapper component.
	/// </summary>
	public class BranchMapperComponent : TreeFactoryComponent {
		#region Vars
		/// <summary>
		/// The bark texture mapper element.
		/// </summary>
		BranchMapperElement branchMapperElement = null;
		/// <summary>
		/// The tree mesh meta builder.
		/// </summary>
		BranchMeshMetaBuilder treeMeshMetaBuilder = null;
		/// <summary>
		/// Component command.
		/// </summary>
		public enum ComponentCommand
		{
			BuildMaterials,
			SetUVs
		}
		private int buildingBranchMapperIndex = -1;
		#endregion

		#region Configuration
		/// <summary>
		/// Gets the process prefab weight.
		/// </summary>
		/// <returns>The process prefab weight.</returns>
		/// <param name="treeFactory">Tree factory.</param>
		public override int GetProcessPrefabWeight (TreeFactory treeFactory) {
			int weight = 0;
			if (branchMapperElement.mainTexture != null)
				weight += 15;
			if (branchMapperElement.normalTexture != null)
				weight += 15;
			return weight;
		}
		/// <summary>
		/// Gets the changed aspects on the tree for this component.
		/// </summary>
		/// <returns>The changed aspects.</returns>
		public override int GetChangedAspects () {
			return (int)TreeFactoryProcessControl.ChangedAspect.Material;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public override void Clear ()
		{
			base.Clear ();
			branchMapperElement = null;
			treeMeshMetaBuilder = null;
		}
		#endregion

		#region Processing
		/// <summary>
		/// Process the tree according to the pipeline element.
		/// </summary>
		/// <param name="treeFactory">Parent tree factory.</param>
		/// <param name="useCache">If set to <c>true</c> use cache.</param>
		/// <param name="useLocalCache">If set to <c>true</c> use local cache.</param>
		/// <param name="ProcessControl">Process control.</param>
		public override bool Process (TreeFactory treeFactory, 
			bool useCache = false, 
			bool useLocalCache = false, 
			TreeFactoryProcessControl ProcessControl = null) {
			if (pipelineElement != null && tree != null) {
				branchMapperElement = pipelineElement as BranchMapperElement;
				BranchMeshGeneratorElement branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) branchMapperElement.GetUpstreamElement (PipelineElement.ClassType.BranchMeshGenerator);
				if (branchMeshGeneratorElement != null && branchMeshGeneratorElement.isActive) {
					BuildMaterials (treeFactory);
					AssignUVs (treeFactory);
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Removes the product of this component on the factory processing.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void Unprocess (TreeFactory treeFactory) {
			treeFactory.meshManager.DeregisterMeshByType (MeshManager.MeshData.Type.Branch);
			treeFactory.materialManager.DeregisterMaterialByType (MeshManager.MeshData.Type.Branch);
		}
		/// <summary>
		/// Process a special command or subprocess on this component.
		/// </summary>
		/// <param name="cmd">Cmd.</param>
		/// <param name="treeFactory">Tree factory.</param>
		public override void ProcessComponentOnly (int cmd, TreeFactory treeFactory) {
			if (pipelineElement != null && tree != null) {
				if (cmd == (int)ComponentCommand.BuildMaterials) {
					BuildMaterials (treeFactory, true);
				} else {
					AssignUVs (treeFactory, true);
				}
			}
		}
		/// <summary>
		/// Processes called only on the prefab creation.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		public override void OnProcessPrefab (TreeFactory treeFactory) {
			#if UNITY_EDITOR
			if (branchMapperElement.materialMode == BranchMapperElement.MaterialMode.Custom) {
				Material material;
				if (treeFactory.treeFactoryPreferences.prefabCloneCustomMaterialEnabled ||
					treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
					AssetManager.MaterialParams materialParams;
					if (treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
						int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
						material = treeFactory.materialManager.GetOverridedMaterial (meshId, false);
						materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native);
						material.name = "Optimized Bark Material";
					} else {
						material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, true);
						materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Custom);
					}
					if (treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled) {
						materialParams.copyTextures = true;
						materialParams.copyTexturesName = "bark";
					}
					treeFactory.assetManager.AddMaterialToPrefab (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					treeFactory.assetManager.AddMaterialParams (materialParams,
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
				} else {
					material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, false);
					AssetManager.MaterialParams materialParams;
					materialParams = new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Custom);
					treeFactory.assetManager.AddMaterialToPrefab (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					treeFactory.assetManager.AddMaterialParams (materialParams,
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
				}
			} else {
				Material material = treeFactory.materialManager.GetMaterial (MeshManager.MeshData.Type.Branch, true);
				if (material != null) {
					material.name = "Optimized Bark Material";
					treeFactory.assetManager.AddMaterialToPrefab (material, 
						treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					if (treeFactory.treeFactoryPreferences.prefabCreateAtlas) {
						AssetManager.MaterialParams materialParams = 
							new AssetManager.MaterialParams (AssetManager.MaterialParams.ShaderType.Native, false);
						if (treeFactory.treeFactoryPreferences.prefabCopyCustomMaterialBarkTexturesEnabled) {
							materialParams.copyTextures = true;
							materialParams.copyTexturesName = "bark";
						}
						treeFactory.assetManager.AddMaterialParams (materialParams,
							treeFactory.meshManager.GetMergedMeshIndex (MeshManager.MeshData.Type.Branch));
					}
				}
			}
			#endif
		}
		/// <summary>
		/// Builds the materials.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		private void BuildMaterials (TreeFactory treeFactory, bool updatePreviewTree = false) {

			if (branchMapperElement.isValidMode) {
				// MATERIAL MODE
				if (branchMapperElement.materialMode == BranchMapperElement.MaterialMode.Custom)
				{
					treeFactory.materialManager.RegisterCustomMaterial (MeshManager.MeshData.Type.Branch,
						branchMapperElement.customMaterial, 0, 0);
				} 
				
				// TEXTURE MODE
				else if (branchMapperElement.materialMode == BranchMapperElement.MaterialMode.Texture)
				{
					SetTexturedMaterial (treeFactory, branchMapperElement.mainTexture, branchMapperElement.normalTexture, branchMapperElement.extrasTexture);
				}

				// MULTI-TEXTURE MATERIAL
				else if (branchMapperElement.materialMode == BranchMapperElement.MaterialMode.MultiTexture)
				{
					Texture2D albedoTexture = null;
					Texture2D normalTexture = null;
					Texture2D extrasTexture = null;
					buildingBranchMapperIndex = branchMapperElement.GetTextures (out albedoTexture, out normalTexture, out extrasTexture);
					SetTexturedMaterial (treeFactory, albedoTexture, normalTexture, extrasTexture);
				}
			}
			// NO MATERIAL 
			else {
				treeFactory.materialManager.DeregisterMaterial (MeshManager.MeshData.Type.Branch);
			}

			if (updatePreviewTree) {
				MeshRenderer renderer = tree.obj.GetComponent<MeshRenderer> ();
				Material[] materials = renderer.sharedMaterials;
				for (int j = 0; j < treeFactory.meshManager.GetMeshesCount (); j++) {
					int meshId = treeFactory.meshManager.GetMergedMeshId (j);
					if (treeFactory.materialManager.GetMaterial (meshId)) {
						if (treeFactory.materialManager.IsCustomMaterial (meshId) &&
						    treeFactory.treeFactoryPreferences.overrideMaterialShaderEnabled) {
							bool isSproutMesh = treeFactory.meshManager.IsSproutMesh (meshId);
							materials [j] = treeFactory.materialManager.GetOverridedMaterial (meshId, isSproutMesh);
						} else {
							materials [j] = treeFactory.materialManager.GetMaterial (meshId, true);
						}
					}
				}
				renderer.sharedMaterials = materials;
			}
		}
		/// <summary>
		/// Set a textured material for branches.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="albedoTexture">Albedo texture.</param>
		/// <param name="normalTexture">Normal texture.</param>
		/// <param name="extrasTexture">Extras texture.</param>
		private void SetTexturedMaterial (TreeFactory treeFactory, Texture2D albedoTexture, Texture2D normalTexture, Texture2D extrasTexture) {
			int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
			Material material;
			if (treeFactory.materialManager.HasMaterial (meshId) && 
				!treeFactory.materialManager.IsCustomMaterial (meshId) &&
				treeFactory.materialManager.GetMaterial (meshId) != null) {
				material = treeFactory.materialManager.GetMaterial (meshId);
			} else {
				material = treeFactory.materialManager.GetOwnedMaterial (meshId, treeFactory.materialManager.GetBarkShader ().name);
			}
			material.SetTexture ("_MainTex", albedoTexture);
			material.SetColor ("_Color", branchMapperElement.color);
			if (ExtensionManager.isHDRP) {
				float hash = 0;
				Vector4 guidVector = Vector4.zero;
				if (branchMapperElement.diffusionProfileSettings != null) {
					hash = ExtensionManager.GetHashFromDiffusionProfile (branchMapperElement.diffusionProfileSettings);
					guidVector = ExtensionManager.GetVector4FromScriptableObject (branchMapperElement.diffusionProfileSettings);
				}
				material.SetFloat ("Diffusion_Profile", hash);
				material.SetVector ("Diffusion_Profile_Asset", guidVector);
			}

			// NORMAL TEXTURE
			if (normalTexture != null) {
				material.SetTexture ("_BumpMap", normalTexture);
				material.SetFloat ("_NormalMapKwToggle", 1f);
				material.EnableKeyword ("EFFECT_BUMP");
			} else {
				material.SetTexture ("_BumpMap", null);
				material.SetFloat ("_NormalMapKwToggle", 0f);
				material.DisableKeyword ("EFFECT_BUMP");
			}

			// EXTRAS TEXTURE
			if (extrasTexture != null) {
				material.EnableKeyword ("EFFECT_EXTRA_TEX");
				material.SetFloat ("EFFECT_EXTRA_TEX", 1f);
				material.SetTexture ("_ExtraTex", extrasTexture);
			} else {
				material.DisableKeyword ("EFFECT_EXTRA_TEX");
				material.SetFloat ("EFFECT_EXTRA_TEX", 0f);
				material.SetFloat ("_Glossiness", branchMapperElement.glossiness);
				material.SetFloat ("_Metallic", branchMapperElement.metallic);
			}
			
			material.SetTexture ("_TranslucencyMap", MaterialManager.GetTranslucencyTex ());
			material.SetFloat ("_WindQuality", 4f);
			material.EnableKeyword ("GEOM_TYPE_BRANCH");
			material.enableInstancing = true;
			material.name = "Bark";
		}
		/// <summary>
		/// Assigns the UVs.
		/// </summary>
		/// <param name="treeFactory">Tree factory.</param>
		/// <param name="updatePreviewTree">If set to <c>true</c> update preview tree.</param>
		private void AssignUVs (TreeFactory treeFactory, bool updatePreviewTree = false) {
			if (treeMeshMetaBuilder == null) {
				treeMeshMetaBuilder = new BranchMeshMetaBuilder ();
			}
			BranchMeshGeneratorElement branchMeshGeneratorElement;
			branchMeshGeneratorElement = 
				(BranchMeshGeneratorElement) branchMapperElement.GetUpstreamElement (
					PipelineElement.ClassType.BranchMeshGenerator);
			if (branchMeshGeneratorElement != null &&
				treeFactory.meshManager.HasMesh (MeshManager.MeshData.Type.Branch)) {
				
				List<Vector4> originalUVs = new List<Vector4> ();
				MeshFilter meshFilter = null;
				if (updatePreviewTree) {
					meshFilter = tree.obj.GetComponent<MeshFilter> ();
					meshFilter.sharedMesh.GetUVs (0, originalUVs);
				}

				List<Vector4> uvs = new List<Vector4> ();
				if (branchMapperElement.materialMode == BranchMapperElement.MaterialMode.MultiTexture && branchMapperElement.isValidMode) {
					BranchMap branchMap = branchMapperElement.branchMaps [buildingBranchMapperIndex];
					uvs = treeMeshMetaBuilder.SetMeshUVs (treeFactory.meshManager.GetMesh (MeshManager.MeshData.Type.Branch),
						0f, branchMapperElement.mappingYDisplacement,
						1, branchMapperElement.mappingYTiles,
						treeFactory.previewTree.minGirth,
						treeFactory.previewTree.maxGirth,
						branchMapperElement.isGirthSensitive,
						true,
						branchMap.x, branchMap.y, 
						branchMap.width, branchMap.height);
				} else if (branchMapperElement.isValidMode) {
					uvs = treeMeshMetaBuilder.SetMeshUVs (treeFactory.meshManager.GetMesh (MeshManager.MeshData.Type.Branch),
						branchMapperElement.mappingXDisplacement, branchMapperElement.mappingYDisplacement,
						branchMapperElement.mappingXTiles, branchMapperElement.mappingYTiles,
						treeFactory.previewTree.minGirth,
						treeFactory.previewTree.maxGirth,
						branchMapperElement.isGirthSensitive);
				}
				if (updatePreviewTree) {
					int meshId = MeshManager.MeshData.GetMeshDataId (MeshManager.MeshData.Type.Branch);
					int vertexOffset = treeFactory.meshManager.GetMergedMeshVertexOffset (meshId);
					for (int j = 0; j < uvs.Count; j++) {
						originalUVs [vertexOffset + j] = uvs [j];
					}
					if (meshFilter != null) {
						meshFilter.sharedMesh.SetUVs (0, originalUVs);
					}
				}
			}
		}
		#endregion
	}
}