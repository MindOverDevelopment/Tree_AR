using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Builder;
using Broccoli.Utils;
using Broccoli.Serialization;
using Broccoli.Factory;

/// <summary>
/// Managment classes for asset components of the tree.
/// </summary>
namespace Broccoli.Manager
{
	/// <summary>
	/// Manager to create prefab from the processed trees,
	/// to optimize meshes, materials and create atlases.
	/// </summary>
	public class AssetManager {
		#region MaterialParams Class
		/// <summary>
		/// Parameters applied to the materials on the prefab.
		/// </summary>
		public class MaterialParams {
			/// <summary>
			/// Shader type.
			/// </summary>
			public enum ShaderType {
				Native,
				Custom
			}
			/// <summary>
			/// The type of the shader.
			/// </summary>
			public ShaderType shaderType = ShaderType.Native;
			/// <summary>
			/// The material textures can be used on the atlas creation process.
			/// </summary>
			public bool useInAtlas = false;
			/// <summary>
			/// If used in atlas the textures need cropping.
			/// </summary>
			public bool needsCrop = false;
			/// <summary>
			/// If true the main texture and the normal texture gets copied to the prefab folder.
			/// </summary>
			public bool copyTextures = false;
			/// <summary>
			/// Name to use when copying textures from a material.
			/// </summary>
			public string copyTexturesName = "";
			/// <summary>
			/// Initializes a new instance of the <see cref="Broccoli.Factory.AssetManager+MaterialParams"/> class.
			/// </summary>
			/// <param name="shaderType">Shader type.</param>
			/// <param name="useInAtlas">If set to <c>true</c> the textures can be used in an atlas.</param>
			/// <param name="needsCrop">If set to <c>true</c> and textures are used in an atlas, then the textures need cropping.</param>
			public MaterialParams (ShaderType shaderType, bool useInAtlas = false, bool needsCrop = false) {
				this.shaderType = shaderType;
				this.useInAtlas = useInAtlas;
				this.needsCrop = needsCrop;
			}
		}
		#endregion

		#region Mesh Class
		/// <summary>
		/// Mesh data on every LOD made to process the tree.
		/// Contains the LOD index and the submeshes belonging to the whole mesh.
		/// </summary>
		class AssetMesh {
			/// <summary>
			/// LOD index the mesh belongs to (from 0 to n).
			/// </summary>
			public int lodIndex = 0;
			/// <summary>
			/// LOD group percentage.
			/// </summary>
			public float lodGroupPercentage = 0.1f;
			/// <summary>
			/// Submeshes to be included on the prefab. The submesh index is used as key.
			/// </summary>
			public Dictionary<int, List<Mesh>> submeshes = new Dictionary<int, List<Mesh>> ();
			/// <summary>
			/// Clear this instance.
			/// </summary>
			public void Clear () {
				var submeshesEnumerator = submeshes.GetEnumerator ();
				while (submeshesEnumerator.MoveNext ()) {
					int submeshIndex = submeshesEnumerator.Current.Key;
					if (submeshes.ContainsKey (submeshIndex)) {
						submeshes [submeshIndex].Clear ();
					}
				}
				submeshes.Clear ();
			}
		}
		#endregion

		#region AssetInfo Class
		/// <summary>
		/// Information about a mesh asset.
		/// </summary>
		public class AssetInfo {
			public int lodLevel = 0;
			public int submeshCount = 0;
			public int verticesCount = 0;
			public int trisCount = 0;
			public bool isBillboard = false;
		}
		#endregion

		#region Delegates and Events
		/// <summary>
		/// Delegate to call functions related to a prefab LOD gameo bject.
		/// </summary>
		/// <param name="lodGameObject"></param>
		public delegate void OnLODEvent (GameObject lodGameObject);
		/// <summary>
		/// To be called when a LOD GameObject is ready and before it gets added to the prefab.
		/// </summary>
		public OnLODEvent onLODReady;
		#endregion

		#region Vars
		#if UNITY_EDITOR
		/// <summary>
		/// Mesh information container for the asset.
		/// The index is the number of processing LOD that generated the mesh.
		/// </summary>
		Dictionary<int, AssetMesh> assetMeshes = new Dictionary<int, AssetMesh> ();
		/// <summary>
		/// Relationship between group ids and their submeshes.
		/// </summary>
		public Dictionary<int, List<int>> groupIdToSubmeshIndex = new Dictionary<int, List<int>> ();
		/// <summary>
		/// Relationship between submesh index and map areas assigned to them.
		/// </summary>
		public Dictionary<int, SproutMap.SproutMapArea> submeshToArea = new Dictionary<int, SproutMap.SproutMapArea> ();
		/// <summary>
		/// Materials to be included on the prefab.
		/// The index of the submesh receiving the material is used as key.
		/// </summary>
		Dictionary<int, List<Material>> materials = new Dictionary<int, List<Material>> ();
		/// <summary>
		/// The material parameters.
		/// </summary>
		Dictionary<int, MaterialParams> materialParameters = new Dictionary<int, MaterialParams> ();
		/// <summary>
		/// True if the prefab folder has been prepared to use native materials.
		/// </summary>
		bool nativeMaterialSet = false;
		/// <summary>
		/// True if the prefab folder has been prepared to use native normal on materials.
		/// </summary>
		bool nativeMaterialNormalSet = false;
		/// <summary>
		/// Colliders to add to the prefab.
		/// </summary>
		public CapsuleCollider[] colliders = new CapsuleCollider[0];
		/// <summary>
		/// The preview target game object.
		/// </summary>
		GameObject previewTargetGameObject;
		/// <summary>
		/// Prefab object.
		/// </summary>
		UnityEngine.Object prefab;
		/// <summary>
		/// The game object to create the prefab from.
		/// </summary>
		GameObject prefabGameObject;
		/// <summary>
		/// Prefab is valid flag.
		/// </summary>
		bool prefabIsValid = false;
		/*
		/// <summary>
		/// The prefab prefix.
		/// </summary>
		public string prefabPrefix = "BroccoTree_";
		*/
		/// <summary>
		/// If true an offset is applied to the prefab mesh vertices.
		/// </summary>
		public bool applyVerticesOffset = false;
		/// <summary>
		/// Offset to apply to the prefab mesh vertices.
		/// </summary>
		public Vector3 verticesOffset = Vector3.zero;
		#endif
		
		/// <summary>
		/// The full path to the prefab.
		/// </summary>
		public string prefabFullPath = "";
		/// <summary>
		/// The name of the prefab.
		/// </summary>
		public string prefabName = "";
		/// <summary>
		/// The path to the folder containing the resources for the prefab.
		/// </summary>
		public string prefabResourcesPath = "";
		/// <summary>
		/// The path to the folder containing the prefab.
		/// </summary>
		public string prefabSavePath = "";
		/// <summary>
		/// List of assets information when creating a prefab.
		/// </summary>
		public List<AssetInfo> assetInfos = new List<AssetInfo> ();
		/// <summary>
		/// LOD fading mode.
		/// </summary>
		public LODFadeMode lodFadeMode = LODFadeMode.CrossFade;
		/// <summary>
		/// Animate LOD fading.
		/// </summary>
		public bool lodFadeAnimate = false;
		/// <summary>
		/// LOD transition width for cross fade mode.
		/// </summary>
		public float lodTransitionWidth = 0.3f;
		/// <summary>
		/// Splits the Prefab submeshes into meshes.
		/// </summary>
		public bool splitSubmeshesIntoGOs = true;
		#endregion

		#region Singleton
		/// <summary>
		/// Asset manager singleton.
		/// </summary>
		static AssetManager _assetManager = null;
		/// <summary>
		/// Gets the singleton instance of the asset manager..
		/// </summary>
		/// <returns>The instance.</returns>
		public static AssetManager GetInstance () {
			if (_assetManager == null) {
				_assetManager = new AssetManager ();
			}
			return _assetManager;
		}
		public bool enableUnwrappedUV1 = true;
		public bool enableAO = false;
		public int samplesAO = 5;
		public float strengthAO = 0.5f;
		#endregion

		#region Prefab Operations
		/// <summary>
		/// Begins with the creation process for the prefab.
		/// Makes sure the destination path is writable, creates the empty prefab and
		/// the prefab container object.
		/// </summary>
		/// <param name="previewTarget">The preview game object.</param>
		/// <param name="previewFolder">Folder path to the container folder.</param>
		public void BeginWithCreatePrefab (GameObject previewTarget, string prefabSavePath, string prefabName) {
			#if UNITY_EDITOR
			// Clear previous prefab process variables.
			Clear ();

			// Set the prefab name, folder path and full path.
			this.prefabName = prefabName;
			this.prefabSavePath = prefabSavePath;
			this.prefabResourcesPath = GetPrefabResourceFolder ();
			this.prefabFullPath = Path.Combine (prefabSavePath, prefabName) + ".prefab";
			// Validate the folder path.
			if (!FileUtils.IsValidFolder (this.prefabSavePath)) {
				throw new UnityException ("AssetManager: Path to create/edit the prefab is not valid (" + this.prefabSavePath + ")");
			}

			// Set the target GameObject.
			previewTargetGameObject = previewTarget;

			// Create the prefab GameObject.
			prefabGameObject = new GameObject ();

			// Create the prefab object.
			#if UNITY_2018_3_OR_NEWER
			prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabFullPath);
			#else
			prefab = PrefabUtility.CreatePrefab (prefabPath, prefabGameObject);
			#endif
			prefabIsValid = true;
			#endif
		}
		/// <summary>
		/// Ends the with the creation process for the prefab commiting the result to a prefab object.
		/// </summary>
		/// <param name="generateBillboard">True to generate a billboard asset.</param>
		/// <param name="billboarPercentage">True if the tree uses Unity Tree Creator shaders.</param>
		/// <returns><c>true</c>, if with commit was successful, <c>false</c> otherwise.</returns>
		public bool EndWithCommit (bool generateBillboard, float billboardPercentage = 0f) {
			bool result = SavePrefab (generateBillboard, billboardPercentage);
			#if UNITY_EDITOR
			previewTargetGameObject = null;
			Object.DestroyImmediate (prefabGameObject);
			EditorUtility.UnloadUnusedAssetsImmediate ();
			#endif
			return result;
		}
		/// <summary>
		/// Adds a mesh to be included on the prefab.
		/// </summary>
		/// <returns><c>true</c>, if the mesh gets added, <c>false</c> otherwise.</returns>
		/// <param name="submeshesToAdd">Submeshes to add.</param>
		/// <param name="lodIndex">LOD index.</param>
		public bool AddMeshToPrefab (Mesh[] submeshesToAdd, int lodIndex, float lodGroupPercentage) {
			#if UNITY_EDITOR
			if (prefabIsValid) {
				if (!assetMeshes.ContainsKey (lodIndex)) {
					AssetMesh assetMesh = new AssetMesh ();
					assetMesh.lodIndex = lodIndex;
					assetMesh.lodGroupPercentage = lodGroupPercentage;
					assetMeshes.Add (lodIndex, assetMesh);
				}
				for (int i = 0; i < submeshesToAdd.Length; i++) {
					if (!assetMeshes[lodIndex].submeshes.ContainsKey (i)) {
						assetMeshes[lodIndex].submeshes [i] = new List<Mesh> ();
					}
					assetMeshes[lodIndex].submeshes [i].Add (Object.Instantiate(submeshesToAdd[i]));
				}
				return true;
			}
			#endif
			return false;
		}
		/// <summary>
		/// Adds and binds a material to a submesh based on its index.
		/// </summary>
		/// <returns><c>true</c>, if material was added, <c>false</c> otherwise.</returns>
		/// <param name="material">Material.</param>
		/// <param name="submeshIndex">Submesh index.</param>
		/// <param name="groupId">Group identifier if the submesh belong to one.</param>
		/// <param name="area">Map area if the material belong to one.</param>
		public bool AddMaterialToPrefab (Material material, int submeshIndex, int groupId = 0, SproutMap.SproutMapArea area = null) {
			#if UNITY_EDITOR
			if (prefabIsValid) {
				if (materials.ContainsKey (submeshIndex)) {
					materials [submeshIndex].Clear ();
				} else {
					materials [submeshIndex] = new List<Material> ();
				}
				if (groupIdToSubmeshIndex.ContainsKey (submeshIndex)) {
					groupIdToSubmeshIndex.Remove (submeshIndex);
				}
				//materials [submeshIndex].Add (Object.Instantiate<Material> (material));
				materials [submeshIndex].Add (material);
				if (groupId > 0) {
					if (!groupIdToSubmeshIndex.ContainsKey (groupId)) {
						groupIdToSubmeshIndex [groupId] = new List<int> ();
					}
					groupIdToSubmeshIndex [groupId].Add (submeshIndex);
				}
				if (area != null) {
					if (!submeshToArea.ContainsKey (submeshIndex)) {
						submeshToArea.Add (submeshIndex, area);
					}
				}
				return true;
			}
			#endif
			return false;
		}
		/// <summary>
		/// Adds the material parameters.
		/// </summary>
		/// <param name="materialParams">Material parameters.</param>
		/// <param name="submeshIndex">Submesh index.</param>
		public void AddMaterialParams (MaterialParams materialParams, int submeshIndex) {
			#if UNITY_EDITOR
			if (materialParams != null) {
				if (materialParameters.ContainsKey (submeshIndex)) {
					materialParameters.Remove (submeshIndex);
				}
				materialParameters.Add (submeshIndex, materialParams);
			}
			#endif
		}
		/// <summary>
		/// Clear this instance and prepares it for a new prefab creation process.
		/// </summary>
		public void Clear () {
			#if UNITY_EDITOR
			Object.DestroyImmediate (prefabGameObject);
			previewTargetGameObject = null;
			prefabGameObject = null;
			prefab = null;
			prefabName = string.Empty;
			prefabFullPath = string.Empty;
			prefabSavePath = string.Empty;
			prefabResourcesPath = string.Empty;
			assetInfos.Clear ();
			prefabIsValid = false;
			applyVerticesOffset = false;
			verticesOffset = Vector3.zero;
			var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
			while (assetMeshesEnumerator.MoveNext ()) {
				assetMeshesEnumerator.Current.Value.Clear ();
			}
			assetMeshes.Clear ();
			var groupIdToSubmeshIndexEnumerator = groupIdToSubmeshIndex.GetEnumerator ();
			int groupId;
			while (groupIdToSubmeshIndexEnumerator.MoveNext ()) {
				groupId = groupIdToSubmeshIndexEnumerator.Current.Key;
				if (groupIdToSubmeshIndex.ContainsKey (groupId)) {
					groupIdToSubmeshIndex [groupId].Clear ();
				}
			}
			groupIdToSubmeshIndex.Clear ();
			var materialsEnumerator = materials.GetEnumerator ();
			int materialIndex;
			while (materialsEnumerator.MoveNext ()) {
				materialIndex = materialsEnumerator.Current.Key;
				if (materials.ContainsKey (materialIndex)) {
					materials [materialIndex].Clear ();
				}
			}
			submeshToArea.Clear ();
			materials.Clear ();
			materialParameters.Clear ();
			nativeMaterialSet = false;
			nativeMaterialNormalSet = false;
			#endif
		}
		/// <summary>
		/// Gets the mesh for the prefab according to the LOD index.
		/// </summary>
		/// <returns>The LOD mesh.</returns>
		/// <param name="matrix">Transform matrix to apply to the mesh.</param>
		/// <param name="lodIndex">Mesh LOD index.</param>
		Mesh GetMeshForPrefab (Matrix4x4 matrix, int lodIndex = 0) {
			Mesh mergingMesh = new Mesh ();
			#if UNITY_EDITOR
			if (assetMeshes.ContainsKey (lodIndex)) {
				List<Mesh> meshes = new List<Mesh> ();
				var submeshesEnumerator = assetMeshes [lodIndex].submeshes.GetEnumerator ();
				int branchTrisLength = -1;
				int meshId;
				while (submeshesEnumerator.MoveNext ()) {
					meshId = submeshesEnumerator.Current.Key;
					if (assetMeshes[lodIndex].submeshes [meshId].Count == 1) {
						meshes.Add (assetMeshes[lodIndex].submeshes [meshId] [0]);
						if (branchTrisLength < 0) {
							branchTrisLength = assetMeshes[lodIndex].submeshes [meshId] [0].triangles.Length;
						}
					} else {
						meshes.Add (MergeMeshes (matrix, assetMeshes[lodIndex].submeshes [meshId], true));
					}
				}
				mergingMesh.subMeshCount = meshes.Count;
				mergingMesh = MergeMeshes (matrix, meshes);
				mergingMesh.name = "Mesh";
				
				// Apply UV1 (UV channel 1) XY unique UV mapping (for lightmapping, decals or mesh painting).
				#if UNITY_EDITOR
				if (enableUnwrappedUV1) {
					List<Vector4> uv1s = new List<Vector4> ();
					List<Vector4> uv6s = new List<Vector4> ();
					List<Vector4> uv7s = new List<Vector4> ();

					// Copy uv1.zw to uv6.w and uv7.w
					mergingMesh.GetUVs (1, uv1s);
					mergingMesh.GetUVs (6, uv6s);
					mergingMesh.GetUVs (7, uv7s);
					bool hasUV6s = uv6s.Count > 0;
					bool hasUV7s = uv7s.Count > 0;
					Vector4 uv6;
					Vector4 uv7;
					for (int i = 0; i < uv1s.Count; i++) {
						if (hasUV6s) {
							uv6 = uv6s [i];
							uv6.w = uv1s[i].z;
							uv6s[i] = uv6;
						} else {
							uv6s.Add (new Vector4 (0, 0, 0, uv1s[i].z));
						}
						if (hasUV7s) {
							uv7 = uv7s [i];
							uv7.w = uv1s[i].w;
							uv7s[i] = uv7;
						} else {
							uv7s.Add (new Vector4 (0, 0, 0, uv1s[i].w));
						}
					}
					mergingMesh.SetUVs (6, uv6s);
					mergingMesh.SetUVs (7, uv7s);

					// Uwrapping.
					float hardAngle = 160f;
					float angleError = 25f;
					float areaError = 25f;
					int packMargin = 5;
					UnwrapParam unwrapParams = new UnwrapParam ();
					UnwrapParam.SetDefaults (out unwrapParams);
					unwrapParams.hardAngle = hardAngle; // Angle between neighbor triangles that will generate seam.
					unwrapParams.angleError = angleError * 0.01f; // Measured in percents. Angle error measures deviation of UV angles from geometry angles. Area error measures deviation of UV triangles area from geometry triangles if they were uniformly scaled.
					unwrapParams.areaError = areaError * 0.01f;
					unwrapParams.packMargin = (float)packMargin * 0.001f; // How much UV islands will be padded.
					List<Vector4> uv0s = new List<Vector4> ();
					Unwrapping.GenerateSecondaryUVSet (mergingMesh, unwrapParams);

					// Copy uv6.w and uv7.w to uv1.zw
					mergingMesh.GetUVs (1, uv1s);
					mergingMesh.GetUVs (6, uv6s);
					mergingMesh.GetUVs (7, uv7s);
					Vector4 uv1;
					for (int i = 0; i < uv1s.Count; i++) {
						uv1 = uv1s [i];
						uv1.z = uv6s [i].w;
						uv1.w = uv7s [i].w;
					}
					mergingMesh.SetUVs (1, uv1s);
				}
				#endif

				// Apply AO is enabled.
				if (enableAO) {
					Broccoli.Factory.TreeFactory.GetActiveInstance ().BeginColliderUsage ();
					Color[] colors = mergingMesh.colors;
					List<int> triangles = new List<int> (mergingMesh.triangles);
					Broccoli.Utils.AmbientOcclusionBaker.BakeAO (
						Broccoli.Factory.TreeFactory.GetActiveInstance ().GetMeshCollider (),
						ref colors,
						mergingMesh.vertices,
						mergingMesh.normals,
						(branchTrisLength == triangles.Count?triangles.ToArray ():triangles.GetRange (0, branchTrisLength).ToArray ()),
						(branchTrisLength == triangles.Count?new int[0]:triangles.GetRange (branchTrisLength, triangles.Count - branchTrisLength).ToArray ()),
						Broccoli.Factory.TreeFactory.GetActiveInstance ().gameObject,
						samplesAO,
						0.5f,
						strengthAO
					);
					mergingMesh.colors = colors;
					Broccoli.Factory.TreeFactory.GetActiveInstance ().EndColliderUsage ();
				}
				// Apply offset if required.
				if (applyVerticesOffset && verticesOffset != Vector3.zero) {
					Vector3[] vertices = mergingMesh.vertices;
					for (int i = vertices.Length - 1; i >= 0; i--) {
						vertices [i] = vertices [i] - verticesOffset;
					}
					mergingMesh.vertices = vertices;
					mergingMesh.RecalculateBounds ();
				}

				// Strip off extra UV channels.
				mergingMesh.SetUVs (4, new List<Vector2> ());
				mergingMesh.SetUVs (5, new List<Vector2> ());
				mergingMesh.SetUVs (6, new List<Vector2> ());
				mergingMesh.SetUVs (7, new List<Vector2> ());
			}
			#endif
			return mergingMesh;
		}
		/// <summary>
		/// Merges a list of submeshes into one mesh.
		/// </summary>
		/// <returns>A single mesh.</returns>
		/// <param name="submeshesToMerge">Submeshes to merge.</param>
		/// <param name="fullMerge">If set to <c>true</c> the submeshes are not included as indexed submeshes on the final mesh.</param>
		Mesh MergeMeshes (Matrix4x4 matrix, List<Mesh> submeshesToMerge, bool fullMerge = false) {
			Mesh mergingMesh = new Mesh ();
			#if UNITY_EDITOR
			mergingMesh.subMeshCount = submeshesToMerge.Count;
			CombineInstance[] combine = new CombineInstance[submeshesToMerge.Count];
			for (int i = 0; i < submeshesToMerge.Count; i++) {
				combine [i].mesh = submeshesToMerge[i];
				combine [i].transform = matrix;
				combine [i].subMeshIndex = 0;
			}
			mergingMesh.CombineMeshes (combine, fullMerge, false);
			mergingMesh.name = "Mesh";
			#endif
			return mergingMesh;
		}
		/// <summary>
		/// Saves the prefab.
		/// </summary>
		/// <param name="generateBillboard">True to generate a billboard asset.</param>
		/// <param name="billboardPercentage">Billboard LOD group percentage.</param>
		/// <returns><c>true</c>, if prefab was saved, <c>false</c> otherwise.</returns>
		bool SavePrefab (bool generateBillboard, float billboardPercentage) {
			#if UNITY_EDITOR
			if (prefabIsValid) {

				// Add all materials to the Prefab.
				var materialsEnumerator = materials.GetEnumerator ();
				List<Material> prefabMaterials = new List<Material> ();
				while (materialsEnumerator.MoveNext ()) {
					var materialPair = materialsEnumerator.Current;
					if (string.IsNullOrEmpty (AssetDatabase.GetAssetPath (materialPair.Value [0]))) {
						materialPair.Value [0] = Object.Instantiate<Material> (materialPair.Value [0]);
						materialPair.Value [0].name = materialPair.Value [0].name.Replace ("(Clone)", "");
						if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabIncludeAssetsInsidePrefab) {
							AssetDatabase.AddObjectToAsset (materialPair.Value [0], prefab);
						} else {
							string folderPath = GetPrefabResourceFolder ();
							string materialPath = folderPath + "/" + materialPair.Value [0].name + ".mat";
							AssetDatabase.CreateAsset (materialPair.Value [0], materialPath);
						}
					}
					if (materialParameters.ContainsKey (materialPair.Key)) {
						switch (materialParameters [materialPair.Key].shaderType) {
						case MaterialParams.ShaderType.Native:
							SetNativeMaterial (materialPair.Value [0], false);
							break;
						case MaterialParams.ShaderType.Custom:
							// TODO: Not implemented yet.
							break;
						}
					}
					prefabMaterials.Add (materialPair.Value [0]);
				}

				// No LODS
				if (assetMeshes.Count == 1 && !generateBillboard) {
					// Get LOD Mesh.
					Mesh lodMesh = GetMeshForPrefab (prefabGameObject.transform.localToWorldMatrix, 0);

					if (splitSubmeshesIntoGOs) {
						Mesh[] submeshMeshes = SplitMesh (lodMesh);
						int submeshCount = lodMesh.subMeshCount;

						for (int submeshI = 0; submeshI < submeshCount; submeshI++) {
							GameObject submeshGO = new GameObject ();
							//Mesh lodMesh = CreateMeshGameObject (lodGameObject, i);
							CreateMeshGameObject (submeshGO, submeshMeshes[submeshI], prefabMaterials.ToArray (), 0, submeshI);
							submeshGO.transform.parent = prefabGameObject.transform;
							// Add the mesh to the asset.
							AddMeshAsAsset (submeshMeshes[submeshI], prefab);
							// Add tree controller.
							if (GlobalSettings.prefabAddController) AddBroccoTreeController (submeshGO);
							// Add appendable controllers.
							AddAppendableControllers (submeshGO);
							if (submeshI == 0) {
								// Add collision objects.
								for (int j = 0; j < colliders.Length; j++) {
									CapsuleCollider collider = prefabGameObject.AddComponent<CapsuleCollider> ();
									collider.name = "Collider_" + j;
									collider.center = colliders[j].center;
									collider.direction = colliders[j].direction;
									collider.height = colliders[j].height;
									collider.radius = colliders[j].radius;
								}
							}
						}
						if (onLODReady != null) onLODReady.Invoke (prefabGameObject);
					} else {
						//Mesh lodMesh = CreateMeshGameObject (lodGameObject, i);
						CreateMeshGameObject (prefabGameObject, lodMesh, prefabMaterials.ToArray (), 0, -1);
						// Add the mesh to the asset.
						AddMeshAsAsset (lodMesh, prefab);
						// Add tree controller.
						if (GlobalSettings.prefabAddController) AddBroccoTreeController (prefabGameObject);
						// Add appendable controllers.
						AddAppendableControllers (prefabGameObject);
						// Add collision objects.
						for (int j = 0; j < colliders.Length; j++) {
							CapsuleCollider collider = prefabGameObject.AddComponent<CapsuleCollider> ();
							collider.name = "Collider_" + j;
							collider.center = colliders[j].center;
							collider.direction = colliders[j].direction;
							collider.height = colliders[j].height;
							collider.radius = colliders[j].radius;
						}
						if (onLODReady != null) onLODReady.Invoke (prefabGameObject);
					}

					#if UNITY_2018_3_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabFullPath);
					#else
					PrefabUtility.ReplacePrefab (prefabGameObject, prefab);
					#endif
				}
				// LODs. 
				else {
					// LOD Group Component on Prefab.
					LODGroup lodGroup = prefabGameObject.AddComponent<LODGroup> ();
					lodGroup.animateCrossFading = lodFadeAnimate;
					lodGroup.fadeMode = lodFadeMode;
					LOD[] lods = new LOD[assetMeshes.Count + 1];
					GameObject firstLOD = null;

					// Create LODs.
					float lodGroupAccum = 0f;
					int i = 0;
					for (i = 0; i < assetMeshes.Count; i++) {
						// Get LOD Mesh.
						Mesh lodMesh = GetMeshForPrefab (prefabGameObject.transform.localToWorldMatrix, i);

						if (splitSubmeshesIntoGOs) {
							Mesh[] submeshMeshes = SplitMesh (lodMesh);
							Renderer[] renderers = new Renderer[lodMesh.subMeshCount];
							int submeshCount = lodMesh.subMeshCount;

							for (int submeshI = 0; submeshI < submeshCount; submeshI++) {
								// LOD GameObject.
								GameObject lodGameObject = new GameObject ();
								if (firstLOD == null) {
									firstLOD = lodGameObject;
								}
								//Mesh lodMesh = CreateMeshGameObject (lodGameObject, i);
								CreateMeshGameObject (lodGameObject, submeshMeshes [submeshI], prefabMaterials.ToArray (), i, submeshI);
								// Set Prefab GO as parent of the LOD GO.
								lodGameObject.transform.parent = prefabGameObject.transform;
								// Add the mesh to the asset.
								AddMeshAsAsset (submeshMeshes [submeshI], prefab);
								// Add Tree Controller.
								if (GlobalSettings.prefabAddController) AddBroccoTreeController (lodGameObject);
								// Add appendable components.
								AddAppendableControllers (lodGameObject);
								// Register the renderers for the LOD.
								
								renderers[submeshI] = lodGameObject.GetComponent<Renderer> ();	
								// Call onLODReady.
								if (onLODReady != null) onLODReady.Invoke (lodGameObject);
							}
							
							lodGroupAccum += assetMeshes [i].lodGroupPercentage;
							lods [i] = new LOD (1f - lodGroupAccum, renderers); 
							lods [i].fadeTransitionWidth = lodTransitionWidth;
						} else {
							// LOD GameObject.
							GameObject lodGameObject = new GameObject ();
							if (firstLOD == null) {
								firstLOD = lodGameObject;
							}
							//Mesh lodMesh = CreateMeshGameObject (lodGameObject, i);
							CreateMeshGameObject (lodGameObject, lodMesh, prefabMaterials.ToArray (), i, -1);
							// Set Prefab GO as parent of the LOD GO.
							lodGameObject.transform.parent = prefabGameObject.transform;
							// Add the mesh to the asset.
							AddMeshAsAsset (lodMesh, prefab);
							// Add Tree Controller.
							if (GlobalSettings.prefabAddController) AddBroccoTreeController (lodGameObject);
							// Add appendable components.
							AddAppendableControllers (lodGameObject);
							// Register the renderers for the LOD.
							Renderer[] renderers = new Renderer[1];
							renderers[0] = lodGameObject.GetComponent<Renderer> ();
							lodGroupAccum += assetMeshes [i].lodGroupPercentage;
							lods [i] = new LOD (1f - lodGroupAccum, renderers); 
							lods [i].fadeTransitionWidth = lodTransitionWidth;
							// Call onLODReady.
							if (onLODReady != null) onLODReady.Invoke (lodGameObject);
						}
					}

					// Create billboard.
					BillboardBuilder billboardBuilder = BillboardBuilder.GetInstance ();
					if (generateBillboard) {
						// Create and save billboard texture.
						int textureSize = TreeFactory.GetAtlasSize (TreeFactory.GetActiveInstance ().treeFactoryPreferences.billboardTextureSize);
						billboardBuilder.textureSize = new Vector2 (textureSize, textureSize);
						billboardBuilder.billboardTexturePath = 
							GetPrefabResourceFolder () + "/" + GlobalSettings.prefabTexturesPrefix + "billboard.png";
						billboardBuilder.billboardNormalTexturePath = 
							GetPrefabResourceFolder () + "/" + GlobalSettings.prefabTexturesPrefix + "billboard_normal.png";
						// Generate the Billboard LOD
						bool isST8 = MaterialManager.leavesShaderType == MaterialManager.LeavesShaderType.SpeedTree8OrSimilar;
						GameObject billboardGameObject = 
							billboardBuilder.GenerateBillboardAsset (previewTargetGameObject, isST8);
						billboardGameObject.transform.parent = prefabGameObject.transform;
						billboardGameObject.name = "LOD_" + i;
						// Get billboard material.
						Material billboardMaterial = billboardBuilder.GetBillboardMaterial (true);
						billboardMaterial.name = "Billboard Material";
						MaterialManager.SetDiffusionProfile (billboardMaterial, MaterialManager.defaultDiffusionProfile, 0.3f);
						// Get billboard asset (ST7) ot mesh object (ST8).
						BillboardAsset billboardAsset = null;
						Mesh billboardMesh = null;
						if (isST8) {
							billboardMesh = billboardBuilder.GetBillboardMesh (true);
							MeshFilter meshFilter= billboardGameObject.GetComponent<MeshFilter> ();
							meshFilter.sharedMesh = billboardMesh;
							MeshRenderer meshRenderer= billboardGameObject.GetComponent<MeshRenderer> ();
							meshRenderer.sharedMaterial = billboardMaterial;
						} else {
							billboardAsset = billboardBuilder.GetBillboardAsset (true);
							billboardAsset.name = "Billboard Asset";
							billboardAsset.material = billboardMaterial;
							billboardGameObject.GetComponent<BillboardRenderer> ().billboard = billboardAsset;
						}
						billboardGameObject.transform.position = new Vector3(0, billboardBuilder.meshTargetYOffset, 0);
						// Call OnLODReady
						if (onLODReady != null) onLODReady.Invoke (billboardGameObject);
						// Save inside prefab or to folder.
						if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabIncludeAssetsInsidePrefab) {
							if (isST8) {
								AssetDatabase.AddObjectToAsset (billboardMesh, prefab);
							} else {
								AssetDatabase.AddObjectToAsset (billboardAsset, prefab);
							}
						} else {
							string folderPath = GetPrefabResourceFolder ();
							string billboardMatPath = folderPath + "/billboard_material.mat";
							AssetDatabase.CreateAsset (billboardMaterial, billboardMatPath);
							if (isST8) {
								string billboardMeshPath = folderPath + "/" + billboardGameObject.name + ".asset";
								AssetDatabase.CreateAsset (billboardMesh, billboardMeshPath);
							} else {
								string billboardPath = folderPath + "/billboard.asset";
								AssetDatabase.CreateAsset (billboardAsset, billboardPath);
							}
						}
						Renderer[] billboardRenderers = new Renderer[1];
						billboardRenderers[0] = billboardGameObject.GetComponent<Renderer> ();
						
						lodGroupAccum += billboardPercentage;
						lods [i] = new LOD (1f - lodGroupAccum, billboardRenderers); 
						lods [i].fadeTransitionWidth = lodTransitionWidth;
					}

					lodGroup.SetLODs (lods);

					// Add collision objects.
					for (int j = 0; j < colliders.Length; j++) {
						if (firstLOD != null) {
							//CapsuleCollider collider = prefabGameObject.AddComponent<CapsuleCollider> ();
							CapsuleCollider collider = firstLOD.AddComponent<CapsuleCollider> ();
							//collider.name = "Collider_" + j;
							collider.center = colliders[j].center;
							collider.direction = colliders[j].direction;
							collider.height = colliders[j].height;
							collider.radius = colliders[j].radius;
						}
					}

					#if UNITY_2018_3_OR_NEWER
					prefab = PrefabUtility.SaveAsPrefabAsset (prefabGameObject, prefabFullPath);
					#else
					PrefabUtility.ReplacePrefab (prefabGameObject, prefab);
					#endif
					billboardBuilder.Clear ();
				}
					
				return true;
			}
			#endif
			return false;
		}
		Mesh[] SplitMesh (Mesh mesh) {
			Mesh[] submeshMeshes;
			int submeshCount = mesh.subMeshCount;
			if (submeshCount < 2) {
				submeshMeshes = new Mesh[] {mesh};
			} else {
				submeshMeshes = new Mesh[submeshCount];
				Vector3[] vertices = mesh.vertices;
				Vector3[] normals = mesh.normals;
				Vector4[] tangents = mesh.tangents;
				Color[] colors = mesh.colors;
				List<Vector4> uv0s = new List<Vector4> ();
				List<Vector4> uv1s = new List<Vector4> ();
				List<Vector4> uv2s = new List<Vector4> ();
				List<Vector4> uv3s = new List<Vector4> ();
				mesh.GetUVs (0, uv0s);
				mesh.GetUVs (1, uv1s);
				mesh.GetUVs (2, uv2s);
				mesh.GetUVs (3, uv3s);
				List<Vector3> subVertices = new List<Vector3> ();
				List<Vector3> subNormals = new List<Vector3> ();
				List<Vector4> subTangents = new List<Vector4> ();
				List<Color> subColors = new List<Color> ();
				List<Vector4> subUV0s = new List<Vector4> ();
				List<Vector4> subUV1s = new List<Vector4> ();
				List<Vector4> subUV2s = new List<Vector4> ();
				List<Vector4> subUV3s = new List<Vector4> ();
				for (int submeshI = 0; submeshI < submeshCount; submeshI++) {
					Mesh submeshMesh = new Mesh ();
					UnityEngine.Rendering.SubMeshDescriptor submeshDesc = mesh.GetSubMesh (submeshI);
					int startVertex = submeshDesc.firstVertex;
					int endVertex = submeshDesc.firstVertex + submeshDesc.vertexCount;
					int[] submeshTris = mesh.GetTriangles (submeshI);
					for (int vI = startVertex; vI < endVertex; vI++) {
						subVertices.Add (vertices [vI]);
						subNormals.Add (normals [vI]);
						subTangents.Add (tangents [vI]);
						subColors.Add (colors [vI]);
						subUV0s.Add (uv0s [vI]);
						subUV1s.Add (uv1s [vI]);
						subUV2s.Add (uv2s [vI]);
						subUV3s.Add (uv3s [vI]);
					}
					for (int trisI = 0; trisI < submeshTris.Length; trisI++) {
						submeshTris [trisI] = submeshTris [trisI] - startVertex;
					}
					submeshMesh.SetVertices (subVertices);
					submeshMesh.SetNormals (subNormals);
					submeshMesh.SetTangents (subTangents);
					submeshMesh.SetColors (subColors);
					submeshMesh.SetUVs (0, subUV0s);
					submeshMesh.SetUVs (1, subUV1s);
					submeshMesh.SetUVs (2, subUV2s);
					submeshMesh.SetUVs (3, subUV3s);
					submeshMesh.triangles = submeshTris;
					submeshMeshes [submeshI] = submeshMesh;

					subVertices.Clear ();
					subNormals.Clear ();
					subTangents.Clear ();
					subColors.Clear ();
					subUV0s.Clear ();
					subUV1s.Clear ();
					subUV2s.Clear ();
					subUV3s.Clear ();
				}
			}
			return submeshMeshes;
		}
		void AddMeshAsAsset (Mesh mesh, Object _prefab) {
			#if UNITY_EDITOR
			if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabIncludeAssetsInsidePrefab) {
				AssetDatabase.AddObjectToAsset (mesh, _prefab);
			} else {
				string folderPath = GetPrefabResourceFolder ();
				string meshPath = folderPath + "/" + mesh.name + ".asset";
				AssetDatabase.CreateAsset (mesh, meshPath);
			}
			#endif
		}
		void AddBroccoTreeController (GameObject gameObject) {
			if (GlobalSettings.broccoTreeControllerVersion == GlobalSettings.BROCCO_TREE_CONTROLLER_V1) {
				Broccoli.Controller.BroccoTreeController treeController = gameObject.AddComponent<Broccoli.Controller.BroccoTreeController> ();
				treeController.shaderType = (Broccoli.Controller.BroccoTreeController.ShaderType)MaterialManager.leavesShaderType;
				treeController.version = Broccoli.Base.BroccoliExtensionInfo.GetVersion ();
				// Set Wind
				WindEffectElement windEffectElement= (WindEffectElement)TreeFactory.GetActiveInstance ().localPipeline.GetElement (PipelineElement.ClassType.WindEffect, true);
				if (windEffectElement) {
					treeController.localWindAmplitude = windEffectElement.windAmplitude;
					treeController.sproutTurbulance = windEffectElement.sproutTurbulence;
					treeController.sproutSway = windEffectElement.minSprout1Sway;
				}
			} else {
				Broccoli.Controller.BroccoTreeController2 treeController = gameObject.AddComponent<Broccoli.Controller.BroccoTreeController2> ();
				treeController.localShaderType = (Broccoli.Controller.BroccoTreeController2.ShaderType)MaterialManager.leavesShaderType;
				treeController.version = Broccoli.Base.BroccoliExtensionInfo.GetVersion ();
				treeController.windInstance = Controller.BroccoTreeController2.WindInstance.Global;
				treeController.globalWindSource = Controller.BroccoTreeController2.WindSource.WindZone;
				// Set Wind
				WindEffectElement windEffectElement= (WindEffectElement)TreeFactory.GetActiveInstance ().localPipeline.GetElement (PipelineElement.ClassType.WindEffect, true);
				if (windEffectElement) {
					treeController.trunkBending= windEffectElement.trunkBending;
				}
			}
		}
		void AddAppendableControllers (GameObject gameObject) {
			List<ComponentReference> components = 
				TreeFactory.GetActiveInstance ().treeFactoryPreferences.appendableComponents;
			for (int k = 0; k < components.Count; k++) {
				if (components[k] != null) {
					components[k].AddTo (gameObject);
				}
			}
		}
		/// <summary>
		/// Creates a GameObject with mesh components (MeshFilter, MeshRenderer).
		/// </summary>
		/// <param name="gameObject">GameObject to received the mesh and components.</param>
		/// <param name="mesh">Mesh to assign to the object.</param>
		/// <param name="materials">Materials to assign to the renderer.</param>
		/// <param name="lodIndex">LOD index of the mesh.</param>
		/// <param name="submesh">If the value >= 0, the mesh received has been split, the material assigned to the MeshRenderer will take this index.</param>
		/// <returns></returns>
		void CreateMeshGameObject (GameObject gameObject, Mesh mesh, Material[] materials, int lodIndex = 0, int submesh = -1) {
			// Add mesh related component.
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter> ();
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer> ();

			// Name the mesh and the GameObject.
			string meshName = "LOD_" + lodIndex;
			if (submesh >= 0) {
				meshName += "_" + submesh;
			}
			mesh.name = meshName;
			gameObject.name = meshName;

			//Assign the mesh.
			meshFilter.sharedMesh = mesh;

			// Assign the materials.
			if (submesh < 0) {
				meshRenderer.sharedMaterials = materials;
			} else {
				meshRenderer.sharedMaterials = new Material[] {materials[submesh]};
			}
			/*
			Mesh mainMesh = null;
			#if UNITY_EDITOR
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter> ();
			gameObject.AddComponent<MeshRenderer> ();
			mainMesh = GetMeshForPrefab (meshFilter.transform.localToWorldMatrix, lodIndex);
			if (TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabIncludeAssetsInsidePrefab) {
				AssetDatabase.AddObjectToAsset (mainMesh, prefab);
			} else {
				string folderPath = GetPrefabResourceFolder ();
				string meshPath = folderPath + "/LOD_" + lodIndex + ".asset";
				AssetDatabase.CreateAsset (mainMesh, meshPath);
			}
			meshFilter.sharedMesh = mainMesh;
			#endif
			return mainMesh;
			*/
		}
		/// <summary>
		/// Sets the properties of a native material.
		/// </summary>
		/// <param name="nativeMaterial">Native material.</param>
		/// <param name="isTreeCreator">True if the tree uses Unity Tree Creator shaders.</param>
		void SetNativeMaterial (Material nativeMaterial, bool isTreeCreator) {
			#if UNITY_EDITOR
			// TODO: move special case processing to object using an interface.
			string texturePath = GetPrefabResourceFolder ();
			string shadowTexPath = texturePath + "/shadow.png";
			string translucencyTexPath = texturePath + "/translucency_gloss.png";
			if (!nativeMaterialSet) {
				if (isTreeCreator) {
					AssetDatabase.CopyAsset (MaterialManager.GetShadowTexPath (), shadowTexPath);
					AssetDatabase.CopyAsset (MaterialManager.GetTranslucencyTexPath (), translucencyTexPath);
					AssetDatabase.ImportAsset (shadowTexPath);
					AssetDatabase.ImportAsset (translucencyTexPath);
				}
				nativeMaterialSet = true;
			}
			if (nativeMaterial.HasProperty ("_ShadowTex")) {
				Texture2D shadowTex = AssetDatabase.LoadAssetAtPath<Texture2D> (shadowTexPath);
				nativeMaterial.SetTexture ("_ShadowTex", shadowTex);
			}
			if (nativeMaterial.HasProperty ("_TranslucencyMap")) {
				Texture2D translucencyTex = AssetDatabase.LoadAssetAtPath<Texture2D> (translucencyTexPath);
				nativeMaterial.SetTexture ("_TranslucencyMap", translucencyTex);
			}
			if (nativeMaterial.HasProperty ("_BumpSpecMap") && nativeMaterial.GetTexture ("_BumpSpecMap") == null) {
				string normalSpecularTexPath = texturePath + "/normal_specular.png";
				if (!nativeMaterialNormalSet) {
					AssetDatabase.CopyAsset (MaterialManager.GetNormalSpecularTexPath (), normalSpecularTexPath);
					AssetDatabase.ImportAsset (normalSpecularTexPath);
					nativeMaterialNormalSet = true;
				}
				Texture2D normalSpecularTex = AssetDatabase.LoadAssetAtPath<Texture2D> (normalSpecularTexPath);
				nativeMaterial.SetTexture ("_BumpSpecMap", normalSpecularTex);
			}
			#endif
		}
		#endregion

		#region Prefab Optimization
		/// <summary>
		/// Optimizes the submeshes (mergin) and materials based on their group id.
		/// </summary>
		public void OptimizeOnGroups () {
			#if UNITY_EDITOR
			Dictionary<string, List<int>> textureToSubmeshIndex = new Dictionary<string, List<int>> ();
			string textureName;

			// Traverse groups with submeshes
			var groupIdToSubmeshIndexEnumerator = groupIdToSubmeshIndex.GetEnumerator ();
			int groupId;
			while (groupIdToSubmeshIndexEnumerator.MoveNext ()) {
				groupId = groupIdToSubmeshIndexEnumerator.Current.Key;
				var textureToSubmeshIndexEnumerator = textureToSubmeshIndex.GetEnumerator ();
				while (textureToSubmeshIndexEnumerator.MoveNext ()) {
					textureToSubmeshIndexEnumerator.Current.Value.Clear ();
				}
				textureToSubmeshIndex.Clear ();

				int submeshId;
				for (int i = 0; i < groupIdToSubmeshIndex [groupId].Count; i++) {
					submeshId = groupIdToSubmeshIndex [groupId] [i];
					if (materials.ContainsKey (submeshId)) {
						// TODO: use texture instance instead
						if (materials [submeshId] [0].HasProperty ("_MainTex")) {
							textureName = materials [submeshId] [0].mainTexture.GetInstanceID () + "";
							if (!textureToSubmeshIndex.ContainsKey (textureName)) {
								textureToSubmeshIndex [textureName] = new List<int> ();
							}
							textureToSubmeshIndex [textureName].Add (submeshId);
						}
					}
				}

				// Add submeshes to merge on the same list.
				var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
				int meshPass;
				bool setMaterialOnPass = false;
				while (assetMeshesEnumerator.MoveNext ()) {
					meshPass = assetMeshesEnumerator.Current.Key;
					textureToSubmeshIndexEnumerator = textureToSubmeshIndex.GetEnumerator ();
					while (textureToSubmeshIndexEnumerator.MoveNext ()) {
						var textureToSubmeshIndexPair = textureToSubmeshIndexEnumerator.Current;
						int containerSubmeshIndex = -1;
						int submeshToMergeIndex;
						for (int i = 0; i < textureToSubmeshIndexPair.Value.Count; i++) {
							submeshToMergeIndex = textureToSubmeshIndexPair.Value [i];
							if (containerSubmeshIndex < 0) {
								containerSubmeshIndex = submeshToMergeIndex;
							} else {
								if (assetMeshes [meshPass].submeshes.ContainsKey (containerSubmeshIndex)) {
									assetMeshes [meshPass].submeshes [containerSubmeshIndex].Add (
										assetMeshes [meshPass].submeshes [submeshToMergeIndex] [0]);
									if (assetMeshes [meshPass].submeshes.ContainsKey (submeshToMergeIndex)) {
										assetMeshes [meshPass].submeshes [submeshToMergeIndex].Clear ();
										assetMeshes [meshPass].submeshes.Remove (submeshToMergeIndex);
									}
								}
								if (materials.ContainsKey (containerSubmeshIndex) && !setMaterialOnPass) {
									materials [containerSubmeshIndex].Add (materials [submeshToMergeIndex] [0]);
									if (materials.ContainsKey (submeshToMergeIndex)) {
										materials [submeshToMergeIndex].Clear ();
										materials.Remove (submeshToMergeIndex);
									}
								}
							}
						}
					}
					setMaterialOnPass = true;
				}
			}

			// Cleaning
			var enumerator = textureToSubmeshIndex.GetEnumerator ();
			while (enumerator.MoveNext ()) {
				enumerator.Current.Value.Clear ();
			}
			textureToSubmeshIndex.Clear ();
			#endif
		}
		/// <summary>
		/// Optimizes texture materials by creating an atlas.
		/// </summary>
		/// <param name="atlasMaximumSize">Atlas maximum size.</param>
		public void OptimizeForAtlas (int atlasMaximumSize = 512) {
			#if UNITY_EDITOR
			TextureManager textureManager = TextureManager.GetInstance ();
			textureManager.Clear ();
			Dictionary <int, int> rectToSubmeshIndex = new Dictionary<int, int> ();
			int i = 0;

			var materialsEnumerator = materials.GetEnumerator ();
			int submeshIndex;
			while (materialsEnumerator.MoveNext ()) {
				submeshIndex = materialsEnumerator.Current.Key;
				if (materialParameters.ContainsKey (submeshIndex)) {
					MaterialParams materialParams = materialParameters [submeshIndex];
					if (materialParams.useInAtlas) {
						Material material = materials [submeshIndex][0];
						if (materialParams.needsCrop && submeshToArea [submeshIndex] != null) {
							SproutMap.SproutMapArea sproutArea = submeshToArea [submeshIndex];
							sproutArea.Normalize ();
							Texture2D texture = TextureUtil.CropTextureRelative (textureManager.GetCopy (sproutArea.texture),
								                    sproutArea.x, 
								                    sproutArea.y, 
								                    sproutArea.width, 
								                    sproutArea.height);
							textureManager.AddTexture (submeshIndex.ToString (), texture);
							textureManager.RegisterTextureToAtlas (submeshIndex.ToString());
							RegisterNormalTextureToAtlas (submeshIndex, material, texture.width, texture.height);
							rectToSubmeshIndex.Add (i, submeshIndex);
							i++;
						} else {
							Texture2D mainTexture = textureManager.GetMainTexture (material);
							if (mainTexture != null) {
								textureManager.AddTexture (submeshIndex.ToString (), mainTexture);
								textureManager.RegisterTextureToAtlas (submeshIndex.ToString ());
								RegisterNormalTextureToAtlas (submeshIndex, material, mainTexture.width, mainTexture.height);
								rectToSubmeshIndex.Add (i, submeshIndex);
								i++;
							}
						}
					}
				}
			}

			// Create atlas.
			if (textureManager.GetTextureCount() > 0) {
				// Create atlas.
				string folderPath = GetPrefabResourceFolder ();
				textureManager.SaveAtlasesToAssets (folderPath, atlasMaximumSize);
				Texture2D atlasTexture = 
					UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (textureManager.GetAtlasAssetPath ());
				Texture2D normalAtlasTexture = 
					UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (textureManager.GetAtlasAssetPath ("normal_atlas"));
				Rect[] submeshesRect = textureManager.GetAtlasRects ();

				// Set materials.
				if (!string.IsNullOrEmpty (textureManager.GetAtlasAssetPath ())) {
					var rectToSubmeshIndexEnumerator = rectToSubmeshIndex.GetEnumerator ();
					int rectIndex;
					while (rectToSubmeshIndexEnumerator.MoveNext ()) {
						rectIndex = rectToSubmeshIndexEnumerator.Current.Key;
						submeshIndex = rectToSubmeshIndex [rectIndex];
						// Set main texture to atlas.
						materials [submeshIndex] [0].SetTexture ("_MainTex", atlasTexture);
						// Set normal texture to atlas.
						materials [submeshIndex] [0].SetTexture ("_BumpSpecMap", normalAtlasTexture);
						var assetMeshesEnumerator = assetMeshes.GetEnumerator ();
						int meshPass;
						while (assetMeshesEnumerator.MoveNext ()) {
							meshPass = assetMeshesEnumerator.Current.Key;
							if (submeshToArea.ContainsKey (submeshIndex)) {
								UpdateUVs (assetMeshes[meshPass].submeshes [submeshIndex] [0], 
									submeshesRect [rectIndex], 
									submeshToArea [submeshIndex]);
							} else if (submeshIndex >= 0) {
								UpdateUVs (assetMeshes[meshPass].submeshes [submeshIndex] [0], 
									submeshesRect [rectIndex]);
							}
						}
					}
				}
			}

			// Copy required textures.
			var materialParametersEnumerator = materialParameters.GetEnumerator ();
			while (materialParametersEnumerator.MoveNext ()) {
				submeshIndex = materialParametersEnumerator.Current.Key;
				if (materialParameters [submeshIndex].copyTextures && 
					!string.IsNullOrEmpty (materialParameters [submeshIndex].copyTexturesName)) {
					string folderPath = GetPrefabResourceFolder ();
					string texturePath = folderPath + "/" + materialParameters [submeshIndex].copyTexturesName + ".png";
					Material material = materials [submeshIndex] [0];
					Texture2D mainTex = textureManager.GetMainTexture (material, false);
					if (mainTex != null) {
						AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (mainTex), texturePath);
						AssetDatabase.ImportAsset (texturePath);
						material.SetTexture("_MainTex",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
						// Set normal texture.
						Texture2D normalTex = textureManager.GetNormalTexture (material, false);
						texturePath = folderPath + "/" + materialParameters [submeshIndex].copyTexturesName + "_normal.png";
						if (normalTex != null) {
							AssetDatabase.CopyAsset (AssetDatabase.GetAssetPath (normalTex), texturePath);
							AssetDatabase.ImportAsset (texturePath);
							if (material.HasProperty ("_BumpSpecMap")) {
								material.SetTexture("_BumpSpecMap",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
							} else if (material.HasProperty ("_BumpMap")) {
								material.SetTexture("_BumpMap",  AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath));
							}
						}
					}
				}
			}

			rectToSubmeshIndex.Clear ();
			textureManager.Clear ();
			#endif
		}
		/// <summary>
		/// Registers a normal texture to atlas.
		/// </summary>
		/// <param name="submeshIndex">Submesh index.</param>
		/// <param name="baseMaterial">Base material.</param>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		private void RegisterNormalTextureToAtlas (int submeshIndex, Material baseMaterial, int width, int height) {
			#if UNITY_EDITOR
			if (baseMaterial != null && width > 0 && height > 0) {
				TextureManager textureManager = TextureManager.GetInstance ();
				Texture2D normalTexture = textureManager.GetNormalTexture (baseMaterial, true);
				if (normalTexture == null) {
					normalTexture = MaterialManager.GetNormalSpecularTex (true);
				}
				if (normalTexture != null) {
					TextureUtil.BilinearScale (normalTexture, width, height);
					textureManager.AddTexture (submeshIndex.ToString () + "n", normalTexture);
					textureManager.RegisterTextureToAtlas (submeshIndex.ToString () + "n", "normal_atlas");
				}
			}
			#endif
		}
		/// <summary>
		/// Gets the prefab resources folder.
		/// </summary>
		/// <returns>The prefab resources folder.</returns>
		public string GetPrefabResourceFolder () {
			return TextureManager.GetInstance ().GetOrCreateFolder (GetPrefabFolder (), GetPrefabName () + "_Resources");
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		private void UpdateUVs (Mesh mesh, Rect rect) {
			UpdateUVs (mesh, rect, 0f, 0f, 1f, 1f);
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		/// <param name="sproutArea">Sprout mapping area.</param>
		private void UpdateUVs (Mesh mesh, Rect rect, SproutMap.SproutMapArea sproutArea) {
			UpdateUVs (mesh, rect, sproutArea.x, sproutArea.y, sproutArea.width, sproutArea.height);
		}
		/// <summary>
		/// UV channel update process for new texture areas after atlas optimization.
		/// </summary>
		/// <param name="mesh">Mesh to update UVs.</param>
		/// <param name="rect">Rect resulting from atlas creation.</param>
		/// <param name="originalX">Current x offset used on the UVs.</param>
		/// <param name="originalY">Current y offset used on the UVs.</param>
		/// <param name="originalWidth">Current width used on the UVs.</param>
		/// <param name="originalHeight">Current height used on the UVs.</param>
		private void UpdateUVs (Mesh mesh, Rect rect, float originalX, float originalY, float originalWidth, float originalHeight) {
			float widthRel, heightRel;
			widthRel = rect.width / originalWidth;
			heightRel = rect.height / originalHeight;
			List<Vector4> uvs = new List<Vector4> ();
			mesh.GetUVs (0, uvs);
			for (int i = 0; i < uvs.Count; i++) {
				uvs[i] = new Vector4 (rect.x + (widthRel * (uvs[i].x - originalX)),
					rect.y + (heightRel * (uvs[i].y - originalY)), uvs[i].z, uvs[i].w);
			}
			mesh.SetUVs (0, uvs);
		}
		#endregion

		#region Data
		/// <summary>
		/// Gets the temp filename.
		/// </summary>
		/// <returns>The temp filename.</returns>
		/// <param name="referenceObj">Reference object.</param>
		public string GetTempFilename (Object referenceObj) {
			return ("Temp/UnityTempFile" + referenceObj.GetInstanceID());
		}
		/// <summary>
		/// Gets the prefab folder.
		/// </summary>
		/// <returns>The prefab folder.</returns>
		public string GetPrefabFolder () {
			// Get the Prefab save path.
			string prefabSavePath;
			if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(Application.dataPath + 
				TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabSavePath)))
			{
				prefabSavePath = "Assets" + TreeFactory.GetActiveInstance ().treeFactoryPreferences.prefabSavePath;
			} else {
				prefabSavePath = "Assets";
			}
			return prefabSavePath;
		}
		/// <summary>
		/// Gets the name of the prefab.
		/// </summary>
		/// <returns>The prefab name.</returns>
		public string GetPrefabName () {
			return prefabName;
		}
		#endregion
	}
}