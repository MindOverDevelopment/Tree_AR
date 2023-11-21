using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Factory;
using Broccoli.Utils;

namespace Broccoli.Manager
{
	/// <summary>
	/// Manager for sprout descriptor.
	/// Manages and merges polygon fragments into compound meshes.
	/// Manages textures for polygon fragments.
	/// Manages materials for polygon fragments.
	/// </summary>
	public class SproutCompositeManager {
		#region Vars
		/// <summary>
		/// Size in pixels for the side size of the texture generated.
		/// </summary>
		public int textureSize = 512;
		/// <summary>
		/// Global scale to apply to the texture dimensions.
		/// </summary>
		public float textureGlobalScale = 1f;
		/// <summary>
		/// Maximum level of LOD managed.
		/// </summary>
		private int maxLod = 0;
		/// <summary>
		/// BranchDescriptor id to BranchDescriptor instance dictionary.
		/// </summary>
		/// <typeparam name="int">Id of the snapshot.</typeparam>
		/// <typeparam name="BranchDescriptor">BranchDescriptor instance.</typeparam>
		/// <returns>Relationship between a snapshot and its id.</returns>
		Dictionary<int, BranchDescriptor> _idToSnapshot = new Dictionary<int, BranchDescriptor> ();
		/// <summary>
		/// Polygon area id to PolygonArea instance dictionary.
		/// </summary>
		/// <typeparam name="int">Id of the PolygonArea instance.</typeparam>
		/// <typeparam name="PolygonArea">PolygonArea instance.</typeparam>
		/// <returns>Relationship between a polygon area and its id.</returns>
		Dictionary<ulong, PolygonArea> _idToPolygonArea = new Dictionary<ulong, PolygonArea> ();
		/// <summary>
		/// PolygonArea id to BranchDescriptor instance dictionary.
		/// </summary>
		/// <typeparam name="ulong">PolygonArea id.</typeparam>
		/// <typeparam name="BranchDescriptor">BranchDescriptor instance.</typeparam>
		/// <returns>Relationship between a polygon area id and its snapshot.</returns>
		Dictionary<ulong, BranchDescriptor> _polygonIdToSnapshot = new Dictionary<ulong, BranchDescriptor> ();
		/// <summary>
		/// Snapshot id/LOD to merged mesh dictionary.
		/// </summary>
		/// <typeparam name="int">Snapshot id.</typeparam>
		/// <typeparam name="Mesh">Merged mesh.</typeparam>
		/// <returns>Relationship between a snapshot and its mesh.</returns>
		Dictionary<int, Mesh> _snapshotIdToMesh = new Dictionary<int, Mesh> ();
		/// <summary>
		/// Polygon id to albedo Texture2D instance dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Texture hash for the polygon texture.</typeparam>
		/// <typeparam name="Texture2D">Albedo Texture2D instance.</typeparam>
		/// <returns>Relationship between a polygon area id and its albedo texture.</returns>
		Dictionary<Hash128, Texture2D> _polygonHashToAlbedoTexture = new Dictionary<Hash128, Texture2D> ();
		/// <summary>
		/// Polygon id to normals Texture2D instance dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Texture hash for the polygon texture.</typeparam>
		/// <typeparam name="Texture2D">Normals Texture2D instance.</typeparam>
		/// <returns>Relationship between a polygon area id and its normals texture.</returns>
		Dictionary<Hash128, Texture2D> _polygonHashToNormalsTexture = new Dictionary<Hash128, Texture2D> ();
		/// <summary>
		/// Polygon id to extras Texture2D instance dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Texture hash for the polygon texture.</typeparam>
		/// <typeparam name="Texture2D">Extras Texture2D instance.</typeparam>
		/// <returns>Relationship between a polygon area id and its extras texture.</returns>
		Dictionary<Hash128, Texture2D> _polygonHashToExtrasTexture = new Dictionary<Hash128, Texture2D> ();
		/// <summary>
		/// Polygon id to subsurface Texture2D instance dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Texture hash for the polygon texture.</typeparam>
		/// <typeparam name="Texture2D">Subsurface Texture2D instance.</typeparam>
		/// <returns>Relationship between a polygon area id and its subsurface texture.</returns>
		Dictionary<Hash128, Texture2D> _polygonHashToSubsurfaceTexture = new Dictionary<Hash128, Texture2D> ();
		/// <summary>
		/// Polygon id to Material instance dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Texture hash for the polygon texture.</typeparam>
		/// <typeparam name="Material">Materials.</typeparam>
		/// <returns>Relationship between a polygon area id and its materials.</returns>
		Dictionary<Hash128, Material> _polygonHashToMaterials = new Dictionary<Hash128, Material> ();
		/// <summary>
		/// Polygon hash to rect in atlas dictionary.
		/// </summary>
		/// <typeparam name="Hash128">Polygon hash for the branches selection.</typeparam>
		/// <typeparam name="Rect">Rect in atlas.</typeparam>
		/// <returns>Relationship between hash and rect.</returns>
		Dictionary<Hash128, Rect> _polygonHashToRect = new Dictionary<Hash128, Rect> ();
		#endregion

		#region Cache Vars
		private int _cachedSnapshotId = 0;
		private int _cachedLOD = 0;
		private int _cachedResolution = 0;
		List<PolygonArea> _cachedPolygonAreas = null;
		/// <summary>
		/// Tree for the current snapshot.
		/// </summary>
		public BroccoTree _snapshotTree = null;
		/// <summary>
        /// Mesh for the current snapshot.
        /// </summary>
        //public Mesh _snapshotMesh = null;
        /// <summary>
        /// Materials of the current snapshot.
        /// </summary>
        public Material[] _snapshotMaterials = new Material [0];
		#endregion

		#region Singleton
		/// <summary>
		/// Singleton for this class.
		/// </summary>
		private static SproutCompositeManager _current = null;
		/// <summary>
		/// Gets the singleton instance of this class.
		/// </summary>
		/// <returns>Singleton for this class.</returns>
		public static SproutCompositeManager Current () {
			if (_current == null ) _current = new SproutCompositeManager ();
			return _current;
		}
		#endregion

		#region Usage
		/// <summary>
		/// Begins usage of this manager.
		/// </summary>
		/// <param name="tree">Broccoli tree to manage.</param>
		/// <param name="factoryScale">Factory scale.</param>
		public void BeginUsage (BroccoTree tree, float factoryScale) {
			// Get mesh and materials.
			//MeshFilter meshFilter = tree.obj.GetComponent<MeshFilter>();
			//meshFilter.sharedMesh.RecalculateNormals ();
			//_snapshotMesh = Object.Instantiate (meshFilter.sharedMesh);
			MeshRenderer meshRenderer = tree.obj.GetComponent<MeshRenderer>();
			_snapshotMaterials = meshRenderer.sharedMaterials;
			_snapshotTree = tree;
		}
		/// <summary>
		/// Ends usage of this manager.
		/// </summary>
		public void EndUsage () {
			
			_snapshotTree = null;
			_snapshotMaterials = new Material [0];
		}
		#endregion

		#region Polygon Management
		/// <summary>
		/// Remove elements belonging to a snapshots.
		/// </summary>
		/// <param name="id">Snapshot id.</param>
		/// <returns><c>true</c> if removed.</returns>
		public bool RemoveSnapshot (BranchDescriptor snapshot) {
			// Remove polygon areas.
			UnmanagePolygonAreas (snapshot);
			// Remove meshes.
			RemoveMeshes (snapshot);
			
			// Remove snapshots.
			if (_idToSnapshot.ContainsKey (snapshot.id))
				_idToSnapshot.Remove (snapshot.id);
			return true;
		}
		/// <summary>
		/// Checks if the manager already has polygons for a snapshot.
		/// </summary>
		/// <param name="id">Id of the snapshot or snapshot.</param>
		/// <returns><c>True</c> if the snapshot exists.</returns>
		public bool HasSnapshot (int id) {
			return _idToSnapshot.ContainsKey (id);
		}
		/// <summary>
		/// Adds a snapshot to be managed.
		/// </summary>
		/// <param name="snapshot">Snapshot instance.</param>
		/// <returns><c>True</c> if added.</returns>
		public bool AddSnapshot (BranchDescriptor snapshot) {
			if (!_idToSnapshot.ContainsKey (snapshot.id)) {
				_idToSnapshot.Add (snapshot.id, snapshot);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Adds a polygon area to be managed for this instance.
		/// </summary>
		/// <param name="polygonArea">PolygonArea instance.</param>
		/// <param name="snapshot">Snapshot instance the polygon area belongs to.</param>
		/// <returns><c>True</c> if the polygon area gets managed.</returns>
		public bool ManagePolygonArea (PolygonArea polygonArea, BranchDescriptor snapshot) {
			if (!_idToPolygonArea.ContainsKey (polygonArea.id)) {
				if (polygonArea.lod > maxLod) maxLod = polygonArea.lod;
				_idToPolygonArea.Add (polygonArea.id, polygonArea);
				if (!_polygonIdToSnapshot.ContainsKey (polygonArea.id)) {
					_polygonIdToSnapshot.Add (polygonArea.id, snapshot);
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Removes all polygon areas belonging to a Snapshot from management from this instance.
		/// </summary>
		/// <param name="snapshotId">Id of the snapshot the polygon areas belong to.</param>
		/// <returns><c>True</c> if the polygon areas get removed from management.</returns>
		public bool UnmanagePolygonAreas (BranchDescriptor snapshot) {
			if (snapshot != null) {
				List<ulong> polygonIds = new List<ulong> ();
				var enumerator = _polygonIdToSnapshot.GetEnumerator ();
				while (enumerator.MoveNext ()) {
					if (enumerator.Current.Value == snapshot) {
						polygonIds.Add (enumerator.Current.Key);
					}
				}
				PolygonArea polyArea;
				//Dictionary<ulong, PolygonArea> newIdToPolyArea = new Dictionary<ulong, PolygonArea> ();
				for (int i = 0; i < polygonIds.Count; i++) {
					if (_idToPolygonArea.ContainsKey (polygonIds [i])) {
						polyArea = _idToPolygonArea [polygonIds [i]];
						RemovePolygonAreaElements (polyArea);
						_idToPolygonArea.Remove (polygonIds [i]);
						_polygonIdToSnapshot.Remove (polygonIds [i]);
					}
				}
				/*
				var polyAreaEnum = _idToPolygonArea.GetEnumerator ();
				while (polyAreaEnum.MoveNext ()) {
					newIdToPolyArea.Add (polyAreaEnum.Current.Key, polyAreaEnum.Current.Value);
				}
				_idToPolygonArea.Clear ();
				_idToPolygonArea = newIdToPolyArea;
				*/
				_idToPolygonArea = new Dictionary<ulong, PolygonArea> (_idToPolygonArea);
				/*
				if (_idToPolygonArea.Count == 0) {
					Debug.Log ("Deleting all _idToPolygonArea");
					_idToPolygonArea.Clear ();
				}
				*/
				return true;
			}
			return false;
		}
		private void RemovePolygonAreaElements (PolygonArea polyArea) {
			if (_polygonHashToAlbedoTexture.ContainsKey (polyArea.hash)) {
				Object.DestroyImmediate (_polygonHashToAlbedoTexture [polyArea.hash], false);
				_polygonHashToAlbedoTexture.Remove (polyArea.hash);
			}
			if (_polygonHashToNormalsTexture.ContainsKey (polyArea.hash)) {
				Object.DestroyImmediate (_polygonHashToNormalsTexture [polyArea.hash], false);
				_polygonHashToNormalsTexture.Remove (polyArea.hash);
			}
			if (_polygonHashToExtrasTexture.ContainsKey (polyArea.hash)) {
				Object.DestroyImmediate (_polygonHashToExtrasTexture [polyArea.hash], false);
				_polygonHashToExtrasTexture.Remove (polyArea.hash);
			}
			if (_polygonHashToSubsurfaceTexture.ContainsKey (polyArea.hash)) {
				Object.DestroyImmediate (_polygonHashToSubsurfaceTexture [polyArea.hash], false);
				_polygonHashToSubsurfaceTexture.Remove (polyArea.hash);
			}
			if (_polygonHashToMaterials.ContainsKey (polyArea.hash)) {
				Object.DestroyImmediate (_polygonHashToMaterials [polyArea.hash], false);
				_polygonHashToMaterials.Remove (polyArea.hash);
			}
			if (_polygonHashToRect.ContainsKey (polyArea.hash)) {
				_polygonHashToRect.Remove (polyArea.hash);
			}
		}
		/// <summary>
		/// Gets a resolution index based on the forward and side bending factors.
		/// </summary>
		/// <param name="fBending">Forward bending factor.</param>
		/// <param name="sBending">Side bending factor.</param>
		/// <param name="gravityFactor">Gavity factor, how much the mesh plane is perpendicular to the gravity vector..</param>
		/// <returns>Resolution factor index.</returns>
		public int GetMeshResolution (float fBending, float sBending, float gravityFactory = 1f) {
			float maxResolution = 4;
			if (fBending > sBending) {
				maxResolution *= fBending * gravityFactory;
			} else {
				maxResolution *= sBending * gravityFactory;
			}
			//Debug.Log (string.Format("Res Index: {0}, fB: {1}, sB: {2}, gFactor: {3}", Mathf.RoundToInt(maxResolution), fBending, sBending, gravityFactory));
			return Mathf.RoundToInt (maxResolution);
		}
		/// <summary>
		/// Composite id for snapshot+lod+resolution.
		/// </summary>
		/// <param name="snapshotId">Snapshot id.</param>
		/// <param name="lod">LOD.</param>
		/// <param name="resolution">Resolution.</param>
		/// <returns>Composite id.</returns>
		public int GetCompositeSnapLodId (int snapshotId, int lod, int resolution = 0) {
			return snapshotId * 1000 + lod * 100 + resolution;
		}
		/// <summary>
		/// <c>True</c> if the mesh exists in this manager.
		/// </summary>
		/// <param name="snapshotId"></param>
		/// <param name="lod"></param>
		/// <param name="resolution"></param>
		/// <returns><c>True</c> if mesh exists.</returns>
		public bool HasMesh (int snapshotId, int lod, int resolution = 0) {
			int compositeSnapLodId = GetCompositeSnapLodId (snapshotId, lod, resolution);
			return _snapshotIdToMesh.ContainsKey (compositeSnapLodId);
		}
		/// <summary>
		/// Gets the mesh for a given snapshot id and LOD.
		/// </summary>
		/// <param name="snapshotId"></param>
		/// <param name="lod"></param>
		/// <param name="useCache"></param>
		/// <returns>Mesh for the snapshot / LOD.</returns>
		public Mesh GetMesh (int snapshotId, int lod, int resolution = 0, bool useCache = true) {
			if (lod > maxLod) lod = maxLod;
			int compositeSnapLodId = GetCompositeSnapLodId (snapshotId, lod, resolution);
			if (_snapshotIdToMesh.ContainsKey (compositeSnapLodId) && useCache) {
				return _snapshotIdToMesh [compositeSnapLodId];
			} else {
				// Create the merged mesh for the snapshot/LOD.
				List<PolygonArea> polygons = GetPolygonAreas (snapshotId, lod, resolution, false);
				CombineInstance[] combine = new CombineInstance[polygons.Count];
				for (int i = 0; i < polygons.Count; i++) {
					combine[i].mesh = polygons [i].mesh;
					combine[i].transform = Matrix4x4.identity;
				}
				Mesh mesh = new Mesh();
				mesh.CombineMeshes (combine, false);
				mesh.RecalculateBounds ();

				// Apply forward and side bending gradient.
				MeshJob.ApplyMeshGradient (mesh, mesh.bounds);

				if (_snapshotIdToMesh.ContainsKey (compositeSnapLodId)) {
					UnityEngine.Object.DestroyImmediate (_snapshotIdToMesh [compositeSnapLodId]);
					_snapshotIdToMesh.Remove (compositeSnapLodId);
				}
				_snapshotIdToMesh.Add (compositeSnapLodId, mesh);
				return mesh;
			}
		}
		/// <summary>
		/// Removes all meshes belonging to a snapshot.
		/// </summary>
		/// <param name="snapshotId">Snapshot Id.</param>
		public void RemoveMeshes (BranchDescriptor snapshot) {
			int minId = GetCompositeSnapLodId (snapshot.id, 0, 0);
			int maxId = GetCompositeSnapLodId (snapshot.id + 1, 0, 0);
			int key;
			var meshEnum = _snapshotIdToMesh.GetEnumerator ();
			List<int> idToRemove = new List<int> ();
			while (meshEnum.MoveNext ()) {
				key = meshEnum.Current.Key;
				if (key >= minId && key < maxId) {
					idToRemove.Add (key);
				}
			}
			for (int i = 0; i < idToRemove.Count; i++) {
				Object.DestroyImmediate (_snapshotIdToMesh [idToRemove [i]]);
				_snapshotIdToMesh.Remove (idToRemove [i]);
			}
			_snapshotIdToMesh = new Dictionary<int, Mesh> (_snapshotIdToMesh);
		}
		/// <summary>
		/// Clears this instance.
		/// </summary>
		public void Clear () {
			_idToSnapshot.Clear ();
			_idToPolygonArea.Clear ();
			_polygonIdToSnapshot.Clear ();

			// Clear meshes.
			var meshEnumerator = _snapshotIdToMesh.GetEnumerator ();
			while (meshEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (meshEnumerator.Current.Value);
			}
			_snapshotIdToMesh.Clear ();

			// Clear albedo textures.
			var albedoTextureEnumerator = _polygonHashToAlbedoTexture.GetEnumerator ();
			while (albedoTextureEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (albedoTextureEnumerator.Current.Value);
			}
			_polygonHashToAlbedoTexture.Clear ();

			// Clear normals textures.
			var normalsTextureEnumerator = _polygonHashToNormalsTexture.GetEnumerator ();
			while (normalsTextureEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (normalsTextureEnumerator.Current.Value);
			}
			_polygonHashToNormalsTexture.Clear ();

			// Clear extras textures.
			var extrasTextureEnumerator = _polygonHashToExtrasTexture.GetEnumerator ();
			while (extrasTextureEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (extrasTextureEnumerator.Current.Value);
			}
			_polygonHashToExtrasTexture.Clear ();

			// Clear subsurface textures.
			var subsurfaceTextureEnumerator = _polygonHashToSubsurfaceTexture.GetEnumerator ();
			while (subsurfaceTextureEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (subsurfaceTextureEnumerator.Current.Value);
			}
			_polygonHashToSubsurfaceTexture.Clear ();

			// Clear materials.
			var materialEnumerator = _polygonHashToMaterials.GetEnumerator ();
			while (materialEnumerator.MoveNext ()) {
				UnityEngine.Object.DestroyImmediate (materialEnumerator.Current.Value);
			}
			_polygonHashToMaterials.Clear ();
			_polygonHashToRect.Clear ();
		}
		#endregion

		#region Polygon Querying
		/// <summary>
		/// Return the polygon areas registeed on this manager.
		/// </summary>
		/// <value></value>
		public Dictionary<ulong, PolygonArea> polygonAreas {
			get { return _idToPolygonArea; }
		}
		/// <summary>
		/// Returns a polygon area given its compoung id.
		/// </summary>
		/// <param name="compoundId">Compound id for the polygon area.</param>
		/// <returns>PolygonArea instance if found, otherwise null.</returns>
		public PolygonArea GetPolygonArea (ulong compoundId) {
			if (_idToPolygonArea.ContainsKey (compoundId)) {
				return _idToPolygonArea [compoundId];
			}
			return null;
		}
		/// <summary>
		/// Get a polygon area.
		/// </summary>
		/// <param name="snapshotId">Snapshot id.</param>
		/// <param name="lod">Level of detail.</param>
		/// <param name="fragment">Fragment.</param>
		/// <returns>Polygon area instance.</returns>
		public PolygonArea GetPolygonArea (int snapshotId, int lod, int fragment = 0) {
			ulong id = PolygonArea.GetCompundId (snapshotId, fragment, lod);
			if (_idToPolygonArea.ContainsKey (id)) {
				return _idToPolygonArea [id];
			}
			return null;
		}
		/// <summary>
		/// Get all the polygon area instances that belong to a snapshot and
		/// have an especific LOD.
		/// </summary>
		/// <param name="snapshotId">Snapshot id.</param>
		/// <param name="lod">Level of detail.</param>
		/// <param name="useCache"><c>True</c> to used a cached list when calling recurrently with the same parameters.</param>
		/// <returns>List of polygon areas.</returns>
		public List<PolygonArea> GetPolygonAreas (int snapshotId, int lod, int resolution, bool useCache) {
			if (useCache && _cachedPolygonAreas != null && 
				snapshotId == _cachedSnapshotId && lod == _cachedLOD && resolution == _cachedResolution)
			{
				return _cachedPolygonAreas;
			}
			if (_cachedPolygonAreas == null) {
				_cachedPolygonAreas = new List<PolygonArea> ();	
			} else {
				_cachedPolygonAreas.Clear ();
			}
			_cachedLOD = lod;
			_cachedSnapshotId = snapshotId;
			_cachedResolution = resolution;
			var polyEnumerator = _idToPolygonArea.GetEnumerator ();
			while (polyEnumerator.MoveNext ()) {
				if (polyEnumerator.Current.Value.snapshotId == snapshotId &&
					polyEnumerator.Current.Value.lod == lod &&
					polyEnumerator.Current.Value.resolution == resolution)
				{
					_cachedPolygonAreas.Add (polyEnumerator.Current.Value);
				}
			}
			return _cachedPolygonAreas;
		}
		#endregion

		#region Texture Manager
		/// <summary>
		/// Adds a polygon area to be managed for this instance.
		/// </summary>
		/// <param name="polygonArea">PolygonArea instance.</param>
		/// <param name="snapshot">Snapshot instance the polygon area belongs to.</param>
		/// <returns><c>True</c> if the polygon area gets managed.</returns>
		public bool GenerateTextures (PolygonArea polygonArea, BranchDescriptor snapshot, SnapshotProcessor.Fragment fragment, SproutSubfactory sproutFactory) {
			// Textures for the polygon area exist.
			if (_polygonHashToAlbedoTexture.ContainsKey (polygonArea.hash)) return false;
			
			if (_snapshotTree != null/*_snapshotMesh != null*/) {
				// Show and hide polygons according to snapshot includes and excludes.
				ReflectIncludeAndExcludesToMesh (snapshot, fragment.includes, fragment.excludes);

				MeshFilter meshFilter = _snapshotTree.obj.GetComponent<MeshFilter> ();
				Mesh _snapshotMesh = meshFilter.sharedMesh;

				//Generate textures.
				Texture2D albedoTex = null;
				Texture2D normalsTex = null;
				Texture2D extrasTex = null;
				Texture2D subsurfaceTex = null;
				int texSize = (int)(textureSize * polygonArea.scale * textureGlobalScale);
				sproutFactory.GeneratePolygonTexture (snapshot, _snapshotMesh, polygonArea.aabb, 
					polygonArea.planeNormal, polygonArea.planeUp, polygonArea.fragmentOffset,
					_snapshotMaterials, SproutSubfactory.MaterialMode.Albedo, texSize, texSize, out albedoTex);
				sproutFactory.GeneratePolygonTexture (snapshot, _snapshotMesh, polygonArea.aabb, 
					polygonArea.planeNormal, polygonArea.planeUp, polygonArea.fragmentOffset,
					_snapshotMaterials, SproutSubfactory.MaterialMode.Normals, texSize, texSize, out normalsTex);
				sproutFactory.GeneratePolygonTexture (snapshot, _snapshotMesh, polygonArea.aabb, 
					polygonArea.planeNormal, polygonArea.planeUp, polygonArea.fragmentOffset,
					_snapshotMaterials, SproutSubfactory.MaterialMode.Extras, texSize, texSize, out extrasTex);
				sproutFactory.GeneratePolygonTexture (snapshot, _snapshotMesh, polygonArea.aabb, 
					polygonArea.planeNormal, polygonArea.planeUp, polygonArea.fragmentOffset,
					_snapshotMaterials, SproutSubfactory.MaterialMode.Subsurface, texSize, texSize, out subsurfaceTex);
				if (_polygonHashToAlbedoTexture.ContainsKey (polygonArea.hash)) { 
					UnityEngine.Object.DestroyImmediate (_polygonHashToAlbedoTexture [polygonArea.hash]);
					_polygonHashToAlbedoTexture.Remove (polygonArea.hash);
				}
				_polygonHashToAlbedoTexture.Add (polygonArea.hash, albedoTex);
				if (_polygonHashToNormalsTexture.ContainsKey (polygonArea.hash)) {
					UnityEngine.Object.DestroyImmediate (_polygonHashToNormalsTexture [polygonArea.hash]);
					_polygonHashToNormalsTexture.Remove (polygonArea.hash);
				}
				_polygonHashToNormalsTexture.Add (polygonArea.hash, normalsTex);
				if (_polygonHashToExtrasTexture.ContainsKey (polygonArea.hash)) {
					UnityEngine.Object.DestroyImmediate (_polygonHashToExtrasTexture [polygonArea.hash]);
					_polygonHashToExtrasTexture.Remove (polygonArea.hash);
				}
				_polygonHashToExtrasTexture.Add (polygonArea.hash, extrasTex);
				if (_polygonHashToSubsurfaceTexture.ContainsKey (polygonArea.hash)) {
					UnityEngine.Object.DestroyImmediate (_polygonHashToSubsurfaceTexture [polygonArea.hash]);
					_polygonHashToSubsurfaceTexture.Remove (polygonArea.hash);
				}
				_polygonHashToSubsurfaceTexture.Add (polygonArea.hash, subsurfaceTex);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Show and hide branches on the tree mesh according to their branch id and the includes and excludes list.
		/// </summary>
		/// <param name="snapshot">Snapshot instance.</param>
		/// <param name="includes">List of branch ids to include.</param>
		/// <param name="excludes">List of branch ids to exclude.</param>
		public void ReflectIncludeAndExcludesToMesh (BranchDescriptor snapshot, List<System.Guid> includes, List<System.Guid> excludes) {
			// Get the list of branches to keep on display.
			GeometryAnalyzer ga = GeometryAnalyzer.Current ();
			List<BroccoTree.Branch> shownBranches = 
				ga.GetFilteredBranches (_snapshotTree, includes, excludes);

			MeshFilter meshFilter = _snapshotTree.obj.GetComponent<MeshFilter> ();
			Mesh _snapshotMesh = meshFilter.sharedMesh;

			// Create index of ids of snapshots to include.
			List<int> shownSnapshotIds = new List<int> ();
			for (int i = 0; i < shownBranches.Count; i++) {
				shownSnapshotIds.Add (shownBranches [i].id);
			}

			// Hide and show the selected branches.
			List<Vector4> uv5 = new List<Vector4> ();
			_snapshotMesh.GetUVs (4, uv5);
			Color[] colors = _snapshotMesh.colors;
			if (colors.Length == 0) {
				colors = new Color[uv5.Count];
				for (int i = 0; i < uv5.Count; i++) {
					colors [i] = Color.white;
				}
			}
			for (int i = 0; i < uv5.Count; i++) {
				if (shownSnapshotIds.Contains ((int)uv5 [i].x)) {
					colors [i].a = 1f;
				} else {
					colors [i].a = 0f;
				}
			}
			_snapshotMesh.colors = colors;
		}
		/// <summary>
		/// Resets all hidden branches on the tree mesh, showing them again.
		/// </summary>
		public void ShowAllBranchesInMesh () {
			MeshFilter meshFilter = _snapshotTree.obj.GetComponent<MeshFilter> ();
			Mesh _snapshotMesh = meshFilter.sharedMesh;
			List<Vector4> uv5 = new List<Vector4> ();
			_snapshotMesh.GetUVs (4, uv5);
			Color[] colors = _snapshotMesh.colors;
			for (int i = 0; i < uv5.Count; i++) {
					colors [i].a = 1f;
			}
			_snapshotMesh.colors = colors;
		}
		/// <summary>
		/// Set the rect areas from the atlas belonging to each registered polygon texture.
		/// </summary>
		/// <param name="rects">Rects array.</param>
		public void SetAtlasRects (Rect[] rects) {
			_polygonHashToRect.Clear ();
			if (_polygonHashToAlbedoTexture.Count == rects.Length) {
				var enumTex = _polygonHashToSubsurfaceTexture.GetEnumerator ();
				int i = 0;
				while (enumTex.MoveNext ()) {
					_polygonHashToRect.Add (enumTex.Current.Key, rects [i]);
					i++;
				}
			} else {
				Debug.LogWarning ("Atlas rects count is different to the number of textures.");
			}
		}
		/// <summary>
		/// Apply the rect value from the atlas to the polygons uvs.
		/// </summary>
		public void ApplyAtlasUVs () {
			if (_polygonHashToRect.Count > 0) {
				PolygonArea polygonArea;
				var enumPolys = _idToPolygonArea.GetEnumerator ();
				while (enumPolys.MoveNext ()) {
					polygonArea = enumPolys.Current.Value;
					Vector4 uv;
					if (_polygonHashToRect.ContainsKey (polygonArea.hash)) {
						Rect rect = _polygonHashToRect [polygonArea.hash];
						for (int i = 0; i < polygonArea.uvs.Count; i++) {
							uv = polygonArea.uvs [i];
							uv.x = rect.x + rect.width * uv.z;
							uv.y = rect.y + rect.height * uv.w;
							polygonArea.uvs [i] = uv;
						}
					}
					if (polygonArea.mesh != null) {
						polygonArea.mesh.SetUVs (0, polygonArea.uvs);
					}
				}
			}
		}
		public List<Hash128> GetHashes (List<(int, int)> snapshotIdsLods) {
			List<Hash128> hashes = new List<Hash128> ();

			List<PolygonArea> polys = new List<PolygonArea> ();
			for (int i = 0; i < snapshotIdsLods.Count; i++) {
				var enumPoly = _idToPolygonArea.GetEnumerator ();
				while (enumPoly.MoveNext ()) {
					if (enumPoly.Current.Value.snapshotId == snapshotIdsLods[i].Item1 &&
						enumPoly.Current.Value.lod == snapshotIdsLods[i].Item2 &&
						enumPoly.Current.Value.resolution == 0)
					{
						hashes.Add (enumPoly.Current.Value.hash);
					}
				}
			}
			return hashes;
		}
		#endregion

		#region Texture Querying
		/// <summary>
		/// Get the list of albedo textures.
		/// </summary>
		/// <returns>List of albedo textures.</returns>
		public List<Texture2D> GetAlbedoTextures () {
			List<Texture2D> texs = new List<Texture2D> ();
			var enumTex = _polygonHashToAlbedoTexture.GetEnumerator ();
			while (enumTex.MoveNext ()) {
				texs.Add (enumTex.Current.Value);
			}
			return texs;
		}
		/// <summary>
		/// Get the list of normals textures.
		/// </summary>
		/// <returns>List of normals textures.</returns>
		public List<Texture2D> GetNormalsTextures () {
			List<Texture2D> texs = new List<Texture2D> ();
			var enumTex = _polygonHashToNormalsTexture.GetEnumerator ();
			while (enumTex.MoveNext ()) {
				texs.Add (enumTex.Current.Value);
			}
			return texs;
		}
		/// <summary>
		/// Get the list of extras textures.
		/// </summary>
		/// <returns>List of extras textures.</returns>
		public List<Texture2D> GetExtrasTextures () {
			List<Texture2D> texs = new List<Texture2D> ();
			var enumTex = _polygonHashToExtrasTexture.GetEnumerator ();
			while (enumTex.MoveNext ()) {
				texs.Add (enumTex.Current.Value);
			}
			return texs;
		}
		/// <summary>
		/// Get the list of subsurface textures.
		/// </summary>
		/// <returns>List of subsurface textures.</returns>
		public List<Texture2D> GetSubsurfaceTextures () {
			List<Texture2D> texs = new List<Texture2D> ();
			var enumTex = _polygonHashToSubsurfaceTexture.GetEnumerator ();
			while (enumTex.MoveNext ()) {
				texs.Add (enumTex.Current.Value);
			}
			return texs;
		}
		public Dictionary<Hash128, Texture2D> GetHashToAlbedoTexture () {
			return _polygonHashToAlbedoTexture;
		}
		public Texture2D GetAlbedoTexture (Hash128 hash) {
			if (_polygonHashToAlbedoTexture.ContainsKey (hash)) {
				return _polygonHashToAlbedoTexture [hash];
			}
			return null;
		}
		public Texture2D GetNormalTexture (Hash128 hash) {
			if (_polygonHashToNormalsTexture.ContainsKey (hash)) {
				return _polygonHashToNormalsTexture [hash];
			}
			return null;
		}
		public Texture2D GetExtrasTexture (Hash128 hash) {
			if (_polygonHashToExtrasTexture.ContainsKey (hash)) {
				return _polygonHashToExtrasTexture [hash];
			}
			return null;
		}
		public Texture2D GetSubsurfaceTexture (Hash128 hash) {
			if (_polygonHashToSubsurfaceTexture.ContainsKey (hash)) {
				return _polygonHashToSubsurfaceTexture [hash];
			}
			return null;
		}
		#endregion



		#region Material Manager
		/// <summary>
		/// Generates a leaves material.
		/// </summary>
		/// <returns>Leaves material.</returns>
		public static Material GenerateMaterial (Color color, float cutoff, float glossiness, float metallic, float subsurface,
			Color subsurfaceColor, Texture2D albedoTex, Texture2D normalsTex, Texture2D extrasTex, Texture2D subsurfaceTex, bool enableInstancing = false)
		{
			Material m = MaterialManager.GetLeavesMaterial ();
			MaterialManager.SetLeavesMaterialProperties (
				m, Color.white, 0.6f, 0.1f, 0.1f, 0.5f, Color.white, 
				albedoTex, normalsTex, extrasTex, subsurfaceTex, null);
			m.enableInstancing = enableInstancing;
			return m;
		}
		/// <summary>
		/// Adds a polygon area to be managed for this instance.
		/// </summary>
		/// <param name="polygonArea">PolygonArea instance.</param>
		/// <param name="snapshot">Snapshot instance the polygon area belongs to.</param>
		/// <param name="overrideWithStandardSRP">If <c>True</c> the shaders is a SRP, despite the scene Render Pipeline being HDRP or URP.</param>
		/// <returns><c>True</c> if the polygon area gets managed.</returns>
		public bool GenerateMaterials (PolygonArea polygonArea, BranchDescriptor snapshot, bool overrideWithStandardSRP = false) {
			if (_snapshotTree != null /*_snapshotMesh != null*/) {
				Material m = MaterialManager.GetLeavesMaterial (overrideWithStandardSRP);
				MaterialManager.SetLeavesMaterialProperties (
                	m, Color.white, 0.6f, 0.1f, 0.1f, 0.5f, Color.white, 
					_polygonHashToAlbedoTexture [polygonArea.hash], _polygonHashToNormalsTexture [polygonArea.hash],
					_polygonHashToExtrasTexture [polygonArea.hash], _polygonHashToSubsurfaceTexture [polygonArea.hash], null);
				m.enableInstancing = false; 
				if (_polygonHashToMaterials.ContainsKey (polygonArea.hash)) {
					UnityEngine.Object.DestroyImmediate (_polygonHashToMaterials [polygonArea.hash]);
					_polygonHashToMaterials.Remove (polygonArea.hash);
				}
				_polygonHashToMaterials.Add (polygonArea.hash, m);
			}
			return false;
		}
		#endregion

		#region Material Querying
		public Material[] GetMaterials (int snapshotId, int lod) {
			List<Material> mats = new List<Material> ();
			List<PolygonArea> polys = new List<PolygonArea> ();
			var enumPoly = _idToPolygonArea.GetEnumerator ();
			while (enumPoly.MoveNext ()) {
				if (enumPoly.Current.Value.snapshotId == snapshotId &&
					enumPoly.Current.Value.lod == lod &&
					enumPoly.Current.Value.resolution == 0)
				{
					polys.Add (enumPoly.Current.Value);
				}
			}
			polys.Sort ((p1,p2) => p1.id.CompareTo(p2.id));
			for (int i = 0; i < polys.Count; i++) {
				if (_polygonHashToMaterials.ContainsKey (polys [i].hash)) {
					mats.Add (_polygonHashToMaterials [polys [i].hash]);
				} else { 
					mats.Add (null);
				}
			}
			return mats.ToArray ();
		}
		#endregion

		#region Debug
		/// <summary>
		/// Provides debug information on the snapshots managed by this manager.
		/// </summary>
		/// <returns>Debug information about snapshots.</returns>
		public string GetSnapshotsDebugInfo () {
            string info = string.Format ("Snapshot to Id [{0}]\n", _idToSnapshot.Count);
            var snapsEnum = _idToSnapshot.GetEnumerator ();
			bool isFirst = true;
			int i = 0;
			while (snapsEnum.MoveNext ()) {
				if (!isFirst) info += "\n";
				info += string.Format ("\t{0}\tId {1}\ttype {2}\tprocessor {3}",
					i,
					snapsEnum.Current.Key, 
					snapsEnum.Current.Value.snapshotType, 
					snapsEnum.Current.Value.processorId);
				isFirst = false;
				i++;
			}
            return info;
		}
		public string GetTexturesDebugInfo () {
			string info = string.Empty;
			var texEnum = _polygonHashToAlbedoTexture.GetEnumerator ();
			bool isFirst = true;
			while (texEnum.MoveNext ()) {
				if (!isFirst) info += "\n";
				info += "Texture Hash: " + texEnum.Current.Key;
				isFirst = false;
			}
            return info;
		}
		#endregion
	}
}