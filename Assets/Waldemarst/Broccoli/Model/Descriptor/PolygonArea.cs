using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Broccoli.Model
{
    /// <summary>
    /// Descriptor for a polygon area.
    /// </summary>
    [System.Serializable]
    public class PolygonArea {
        #region Vars
        /// <summary>
        /// Id of the instance.
        /// </summary>
        public ulong id = 0;
        /// <summary>
        /// Hash for the polygon, based on the branches included and excluded.
        /// </summary>
        [System.NonSerialized]
        public Hash128 hash;
        /// <summary>
        /// Global scale to apply to the texture size.
        /// </summary>
        [System.NonSerialized]
        public float scale = 1f;
        /// <summary>
        /// The id of the branch descriptor the polygon area belongs to.
        /// </summary>
        [FormerlySerializedAs("branchDescriptorId")]
        public int snapshotId = 0;
        /// <summary>
        /// The level of detail this polygon area belongs to.
        /// </summary>
        public int lod = 0;
        /// <summary>
        /// The fragment this polygon area belongs to.
        /// </summary>
        public int fragment = 0;
        /// <summary>
        /// Offset when the polygon is a fragment of a snapshot.
        /// </summary>
        public Vector3 fragmentOffset;
        /// <summary>
        /// How dense the number of points on the polygon are will be.
        /// </summary>
        public int resolution = 0;
        /// <summary>
        /// Points for the polygon enclosed area.
        /// </summary>
        /// <typeparam name="Vector3">Point.</typeparam>
        /// <returns>List of points.</returns>
        public List<Vector3> points = new List<Vector3> ();
        /// <summary>
        /// Saves the index to the last point of the convex polygon.
        /// </summary>
        public int lastConvexPointIndex = 0;
        /// <summary>
        /// Marks this polygon to be non convex.
        /// </summary>
        public bool isNonConvexHull = false;
        /// <summary>
        /// Normals for this polygon mesh.
        /// </summary>
        public List<Vector3> normals = new List<Vector3> ();
        /// <summary>
        /// Tangents for this polygon mesh.
        /// </summary>
        public List<Vector4> tangents = new List<Vector4> ();
        /// <summary>
        /// UV mapping.
        /// </summary>
        public List<Vector4> uvs = new List<Vector4> ();
        /// <summary>
        /// Triangles for this poygon mesh.
        /// </summary>
        public List<int> triangles = new List<int> ();
        /// <summary>
        /// AABB bounds.
        /// </summary>
        public Bounds aabb;
        /// <summary>
        /// OBB bounds.
        /// </summary>
        public Bounds obb;
        /// <summary>
        /// OBB angle.
        /// </summary>
        public float obbAngle;
        /// <summary>
        /// The mesh for the polygon area.
        /// </summary>
        [System.NonSerialized]
        public Mesh mesh;
        /// <summary>
        /// Plane up direction.
        /// </summary>
        public Vector3 planeUp;
        /// <summary>
        /// Plane normal direction.
        /// </summary>
        public Vector3 planeNormal;
        /*
        /// <summary>
        /// Guids of the branches included in the polygon area.
        /// </summary>
        /// <typeparam name="System.Guid">Branch guid.</typeparam>
        /// <returns>List of branch guids.</returns>
        [System.NonSerialized]
        public List<System.Guid> includes = new List<System.Guid> ();
        /// <summary>
        /// Guids of the branches excluded in the polygon area.
        /// </summary>
        /// <typeparam name="System.Guid">Branch guid.</typeparam>
        /// <returns>List of branch guids.</returns>
        [System.NonSerialized]
        public List<System.Guid> excludes = new List<System.Guid> ();
        /// <summary>
        /// Ids of the branches included in the polygon area.
        /// </summary>
        /// <typeparam name="int">Branch id.</typeparam>
        /// <returns>List of branch ids.</returns>
        [System.NonSerialized]
        public List<int> includedBranchIds = new List<int> ();
        /// <summary>
        /// Ids of the branches excluded in the polygon area.
        /// </summary>
        /// <typeparam name="int">Branch id.</typeparam>
        /// <returns>List of branch ids.</returns>
        [System.NonSerialized]
        public List<int> excludedBranchIds = new List<int> ();
        */
        /// <summary>
        /// Points from the topology of the branches used to create the polygons.
        /// </summary>
        [System.NonSerialized]
        public List<Vector3> topoPoints = new List<Vector3> ();
        #endregion

        #region Constants
        public static int MAX_RESOLUTION = 4;
        private static int COMPOUND_ID_SNAPSHOT_ID = 100000;
        private static int COMPOUND_ID_LOD = 10000;
        private static int COMPOUND_ID_FRAGMENT = 10;
        #endregion

        #region Construction
        /// <summary>
        /// Private class constructor.
        /// </summary>
        private PolygonArea () {}
        /// <summary>
        /// Class contructor.
        /// </summary>
        public PolygonArea (int snapshotId, int fragmentIndex, int lod = 0, int resolution = 0) {
            this.snapshotId = snapshotId;
            this.fragment = fragmentIndex;
            this.lod = lod;
            this.resolution = resolution;
            id = GetCompundId (snapshotId, fragmentIndex, lod, resolution);
        }
        #endregion

        #region Ops
        /// <summary>
        /// Get an instance id.
        /// </summary>
        /// <param name="snapshotId">Id of the snapshot.</param>
        /// <param name="fragment">Fragment for the polygon (from 0 to 9,999).</param>
        /// <param name="lod">LOD for the polygon (from 0 to 9).</param>
        /// <param name="resolution">Resolution level for the polygon.</param>
        /// <returns>Id for a polygon area instance.</returns>
        public static ulong GetCompundId (int snapshotId, int fragment, int lod = 0, int resolution = 0) {
            ulong _id = (ulong)(snapshotId * COMPOUND_ID_SNAPSHOT_ID);
            return  _id + (ulong)(lod * COMPOUND_ID_LOD + fragment * COMPOUND_ID_FRAGMENT + resolution);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public PolygonArea Clone () {
            PolygonArea clone = new PolygonArea ();
            clone.id = id;
            clone.hash = hash;
            clone.scale = scale;
            clone.snapshotId = snapshotId;
            clone.lod = lod;
            clone.fragment = fragment;
            clone.fragmentOffset = fragmentOffset;
            clone.resolution = resolution;
            clone.lastConvexPointIndex = lastConvexPointIndex;
            clone.isNonConvexHull = isNonConvexHull;
            for (int i = 0; i < points.Count; i++) {
                clone.points.Add (points [i]);
            }
            for (int i = 0; i < normals.Count; i++) {
                clone.normals.Add (normals [i]);
            }
            for (int i = 0; i < uvs.Count; i++) {
                clone.uvs.Add (uvs [i]);
            }
            for (int i = 0; i < tangents.Count; i++) {
                clone.tangents.Add (tangents [i]);
            }
            for (int i = 0; i < triangles.Count; i++) {
                clone.triangles.Add (triangles [i]);
            }
            clone.aabb = aabb;
            clone.obb = obb;
            clone.obbAngle = obbAngle;
            clone.planeNormal = planeNormal;
            clone.planeUp = planeUp;
            #if BROCCOLI_DEVEL
            for (int i = 0; i < topoPoints.Count; i++) {
                clone.topoPoints.Add (topoPoints [i]);
            }
            #endif
            /*
            for (int i = 0; i < includes.Count; i++) {
                clone.includes.Add (includes [i]);
            }
            for (int i = 0; i < excludes.Count; i++) {
                clone.excludes.Add (excludes [i]);
            }
            for (int i = 0; i < includedBranchIds.Count; i++) {
                clone.includedBranchIds.Add (includedBranchIds [i]);
            }
            for (int i = 0; i < excludedBranchIds.Count; i++) {
                clone.excludedBranchIds.Add (excludedBranchIds [i]);
            }
            */
            return clone;
        }
        #endregion
    }
}