using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;
using Broccoli.Pipe;
using Broccoli.Utils;

namespace Broccoli.Factory
{
    /// <summary>
    /// Base class for snapshot processors.
    /// </summary>
    public abstract class SnapshotProcessor {
        #region Fragment Class
        public class Fragment {
            public List<System.Guid> includes = new List<System.Guid> ();
            public List<System.Guid> excludes = new List<System.Guid> ();
            public int baseBranchId = -1;
            public List<int> includeIds = new List<int> ();
            public List<int> excludeIds = new List<int> ();
            public List<Vector3> anchorPoints = new List<Vector3> ();
            public Vector3 offset = Vector3.zero;
            public int minLevel = 0;
            public Vector3 planeAxis = Vector3.up;
            public Vector3 planeNormal = Vector3.right;
            public float planeDegrees = 0f;
            public bool hasIncludesOrExcludes {
                get {
                    return (includes.Count > 0 || excludes.Count > 0);
                }
            }
            public string IncludesExcludesToString (int snapshotId) {
                string hashable = snapshotId + ":" + 
                    baseBranchId + "-i:";
                includes.Sort ();
                for (int i = 0; i < includes.Count; i++) {
                    hashable += includes [i].ToString ();
                }
                hashable += "e:";
                for (int i = 0; i < excludes.Count; i++) {
                    hashable += excludes [i].ToString ();
                }
                return hashable;
            }
        }
        #endregion

        #region Process Cotroll class
        public class ProcessControl {
            public bool isHulConvex = true;
            public float topoBranchGirthScale = 2f;
            public float topoBranchResolutionAngle = 45f;
            public float topoSproutLengthScale = 1.25f;
            public static ProcessControl GetDefault () {
                ProcessControl pc = new ProcessControl ();
                return pc;
            }
        }
        #endregion

        #region Vars
        protected BroccoTree tree = null;
        protected Mesh treeMesh = null;
        protected float factoryScale = 1f;
        public enum FragmentBias {
            None = 0,
            PlaneAlignment = 1
        }
        protected FragmentBias fragmentBias = FragmentBias.None;
        public bool simplifyHullEnabled = true;
        #endregion

        #region Usage
        /// <summary>
        /// Begins usage of this instance.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="scale"></param>
        public void BeginUsage (BroccoTree tree, Mesh treeMesh, float scale) {
            this.tree = tree;
            this.treeMesh = treeMesh;
            this.factoryScale = scale;
        }
        /// <summary>
        /// Ends usage of this instance.
        /// </summary>
        public void EndUsage () {
            this.tree = null;
            this.treeMesh = null;
        }
        #endregion

        #region Bias
        /// <summary>
        /// Gets the fragmentation parameters according to the
        /// tree max hierarchy level and the LOD.
        /// </summary>
        /// <param name="maxLevel">Tree max hierarchy level.</param>
        /// <param name="lod">Level of detail. From 0 to 2.</param>
        /// <param name="fragLevels">How many fragmentation levels to support. From 1 to n.</param>
        /// <param name="minFragLevel">Where the fragmentation level begins. From 0 to n.</param>
        /// <returns>Fragmentation bias type to generate the fragments.</returns>
        public abstract FragmentBias GetFragmentationBias (
            int maxLevel, 
            int lod, 
            out int fragLevels,
            out int minFragLevel,
            out int maxFragLevel);
        #endregion

        #region Fragments
        /// <summary>
        /// Generates the fragments for a branch descriptor at a specific LOD.
        /// </summary>
        /// <param name="lodLevel">Level of detail.</param>
        /// <param name="snapshot">Snapshot.</param>
        /// <returns>List of fragments for the LOD.</returns>
        public abstract List<Fragment> GenerateSnapshotFragments (
            int lodLevel,
            BranchDescriptor snapshot);
        #endregion

        #region PolygonArea
        /// <summary>
        /// Generate the hull (convex or non-convex) points for the given fragment and saves them
        /// on the polygon area rotated to the ZY Plane.
        /// </summary>
        /// <param name="polygonArea">Polygon Area ro process.</param>
        /// <param name="fragment">Fragment to process.</param>
        public virtual void GenerateHullPoints (PolygonArea polygonArea, Fragment fragment) {
            ProcessControl pc = ProcessControl.GetDefault ();
            // Add Polygon POINTS,
            if (pc.isHulConvex) {
                CreatePolygonConvexHull (polygonArea, fragment, pc);
            } else {
                CreatePolygonNonConvexHull (polygonArea, fragment, pc);
            }
        }
        protected void RotatePointsToYZ (List<Vector3> points, PolygonArea polygonArea, Fragment fragment, bool reverse = false) {
            Quaternion rot = Quaternion.LookRotation (Vector3.Cross (fragment.planeNormal, fragment.planeAxis), fragment.planeAxis);
            if (!reverse) {
                rot = Quaternion.Inverse (rot);
            }
            for (int i = 0; i < points.Count; i++) {
                points [i] = rot * (points[i] - fragment.offset) + fragment.offset;
            }
        }
        /// <summary>
        /// Get structure points for a fragment from the whole tree.
        /// </summary>
        /// <param name="polygonArea"></param>
        /// <param name="fragment"></param>
        /// <param name="pc"></param>
        /// <returns>List of structure points for the fragment rotated to the YZ plane.</returns>
        protected List<Vector3> GetSnapshotStructurePoints (PolygonArea polygonArea, Fragment fragment, ProcessControl pc = null) {
            if (pc == null) pc = ProcessControl.GetDefault ();

            // Analyze the tree points.
            GeometryAnalyzer ga = GeometryAnalyzer.Current ();
            ga.Clear ();
            
            List<Vector3> snapshotPoints = new List<Vector3> ();

            // Get branch points.
            List<BroccoTree.Branch> _filteredBranches = 
                ga.GetFilteredBranches (tree, fragment.includes, fragment.excludes);

            ga.GetBranchAndSproutOutline (
                _filteredBranches, 
                pc.topoBranchGirthScale, 
                pc.topoBranchResolutionAngle, 
                pc.topoSproutLengthScale);

            snapshotPoints.AddRange (ga.branchPoints);
            snapshotPoints.AddRange (ga.sproutPoints);

            // Clear the geometry analizer points.
            ga.Clear ();

            // Scale points.
            for (int i = 0; i < snapshotPoints.Count; i++) {
                snapshotPoints [i] = snapshotPoints [i] * factoryScale;
                #if BROCCOLI_DEVEL
                polygonArea.topoPoints.Add (snapshotPoints [i]);
                #endif
            }

            RotatePointsToYZ (snapshotPoints, polygonArea, fragment);

            return snapshotPoints;
        }
        protected void CreatePolygonNonConvexHull (PolygonArea polygonArea, Fragment fragment, ProcessControl pc = null) {
            if (pc == null) pc = ProcessControl.GetDefault ();
            // Get the geometry analizer and set the snapshot points.
            GeometryAnalyzer ga = GeometryAnalyzer.Current ();
            List<Vector3> snapshotPoints = new List<Vector3> ();
            List<Vector3> snapshotHull = new List<Vector3> ();
            List<List<Vector3>> polygons = new List<List<Vector3>> ();

            // Get the filtered branches..
            List<BroccoTree.Branch> _filteredBranches = new List<BroccoTree.Branch> ();
            _filteredBranches = ga.GetFilteredBranches (tree, fragment.includes, fragment.excludes);
            

            // For each filtered branch and their sprouts, get a polygon.
            for (int i = 0; i < _filteredBranches.Count; i++) {
                snapshotPoints.Clear ();
                snapshotHull.Clear ();
                
                ga.GetBranchAndSproutOutline 
                    (_filteredBranches [i], 
                    pc.topoBranchGirthScale, 
                    pc.topoBranchResolutionAngle, 
                    pc.topoSproutLengthScale, 
                    false);
                snapshotPoints.AddRange (ga.branchPoints);
                snapshotPoints.AddRange (ga.sproutPoints);

                ga.Clear ();

                RotatePointsToYZ (snapshotPoints, polygonArea, fragment);

                snapshotHull = ga.QuickHullYZ (new List<Vector3>(snapshotPoints));
                for (int j = 0; j < snapshotHull.Count; j++) {
                    snapshotHull [j] = snapshotHull [j] * factoryScale;
                }
                polygons.Add (new List<Vector3> (new List<Vector3> (snapshotHull)));
                #if BROCCOLI_DEVEL
                polygonArea.topoPoints.AddRange (snapshotPoints);
                #endif
                    
            }

            // Combine
            polygonArea.points.Clear ();
            
            if (polygons.Count > 1) {
                polygonArea.points = ga.CombineConvexHullsYZ (polygons);
                if (simplifyHullEnabled)
                    polygonArea.points = ga.SimplifyConvexHullYZ (polygonArea.points, 25f);
            } else {
                polygonArea.points = polygons [0];
                if (simplifyHullEnabled)
                    polygonArea.points = ga.SimplifyConvexHullYZ (polygonArea.points, 25f);
            }
            //polygonArea.points = polygons [0];
            polygonArea.lastConvexPointIndex = polygonArea.points.Count - 1;
            polygonArea.isNonConvexHull = true;
        }
        protected void CreatePolygonConvexHull (PolygonArea polygonArea, Fragment fragment, ProcessControl pc = null) {
            if (pc == null) pc = ProcessControl.GetDefault ();
            GeometryAnalyzer ga = GeometryAnalyzer.Current ();

            // Get structural points.
            List<Vector3> snapshotPoints = GetSnapshotStructurePoints (polygonArea, fragment, pc);

            // ConvexHull points.
            List<Vector3> _convexPoints = ga.QuickHullYZ (snapshotPoints, false);
            _convexPoints = ga.ShiftConvexHullPoint (_convexPoints);
            if (_convexPoints.Count > 0) {
                _convexPoints.Add (_convexPoints [0]);
            }

            // Simplify convex hull points.
            if (simplifyHullEnabled) {
                float simplifyAngle = 35f;
                if (polygonArea.lod == 0) {
                    simplifyAngle = 20f;
                } else if (polygonArea.lod == 1) {
                    simplifyAngle = 28f;
                }
                if (simplifyHullEnabled)
                    _convexPoints = ga.SimplifyConvexHullYZ (_convexPoints, simplifyAngle);
            }
            _convexPoints.RemoveAt (_convexPoints.Count - 1);

            // Set the polygon area points.
            polygonArea.lastConvexPointIndex = _convexPoints.Count - 1;
            polygonArea.points.Clear ();
            polygonArea.points.AddRange (_convexPoints);
        }
        /// <summary>
        /// Adds additional points to the geometry of a polygon area beforeits triangulation process.
        /// </summary>
        /// <param name="polygonArea">Polygon area to process.</param>
        /// <param name="fragment">Fragment to process.</param>
        public virtual void ProcessPolygonDetailPoints (PolygonArea polygonArea, Fragment fragment) {
            if (polygonArea.resolution > 0) {
                List<Vector3> points = new List<Vector3> ();
                for (int i = 0; i < fragment.anchorPoints.Count; i++) {
                    points.Add (fragment.anchorPoints [i] * factoryScale);
                }
                // Higher res.
                if (polygonArea.resolution == PolygonArea.MAX_RESOLUTION) {
                    GeometryAnalyzer ga = GeometryAnalyzer.Current ();
                    ga.GetInnerPoints (tree, fragment.includes, fragment.excludes, false);
                    for (int i = 0; i < ga.branchPoints.Count; i++) {
                        points.Add (ga.branchPoints [i] * factoryScale);   
                    }
                }
                if (polygonArea.resolution == PolygonArea.MAX_RESOLUTION - 1) {
                    Vector3 pointToAdd = tree.branches [0].GetPointAtPosition (1f) * factoryScale;
                    points.Add (pointToAdd);
                    pointToAdd = tree.branches [0].GetPointAtPosition (0.75f) * factoryScale;
                    points.Add (pointToAdd);
                    pointToAdd = tree.branches [0].GetPointAtPosition (0.5f) * factoryScale;
                    points.Add (pointToAdd);
                }
                if (polygonArea.resolution == PolygonArea.MAX_RESOLUTION - 2) {
                    Vector3 pointToAdd = tree.branches [0].GetPointAtPosition (1f) * factoryScale;
                    points.Add (pointToAdd);
                    pointToAdd = tree.branches [0].GetPointAtPosition (0.5f) * factoryScale;
                    points.Add (pointToAdd);
                }
                if (polygonArea.resolution == PolygonArea.MAX_RESOLUTION - 3) {
                    Vector3 pointToAdd = tree.branches [0].GetPointAtPosition (1f) * factoryScale;
                    points.Add (pointToAdd);
                }
                RotatePointsToYZ (points, polygonArea, fragment);
                polygonArea.points.AddRange (points);
            }
        }
        /// <summary>
        /// Preprocessing of polygon area points before creating its mesh.
        /// </summary>
        /// <param name="polygonArea">Polygon area to process.</param>
        /// <param name="fragment">Fragment to process.</param>
        protected virtual void PreProcessPolygonAreaMesh (PolygonArea polygonArea, Fragment fragment) {}
        /// <summary>
        /// Gets the outline points of a fragment and creates de bounds for a polygon area.
        /// 1. Get the topology points to create a hull.
        /// 2. Create a convex hull.
        /// 3. Simplify the convex hull.
        /// 4. Create the AABB.
        /// 5. Create the OBB and set OBB rotation.
        /// </summary>
        /// <param name="polygonArea">Polygon area to process.</param>
        /// <param name="fragment">Fragment to process.</param>
        public virtual void GenerateBounds (PolygonArea polygonArea, Fragment fragment) {
            //Quaternion reverseRotation = Quaternion.AngleAxis (fragment.planeDegrees, fragment.baseAxis);
            /*
            Quaternion rotation = Quaternion.FromToRotation (Vector3.right, fragment.planeNormal);
            Quaternion reverseRotation = Quaternion.FromToRotation (fragment.planeNormal, Vector3.right);
            */

            if (polygonArea.points.Count > 0) {
                /*
                if (polygonArea.lod == 0 && polygonArea.fragment == 0) {
                    Vector3 dist = new Vector3(0.005f, 0.005f, 0.005f);
                    for (int i = 0; i < polygonArea.points.Count; i++) {
                        Debug.DrawLine (polygonArea.points[i], polygonArea.points[i] + dist, Color.white, 10f);
                    }
                }
                */

                // Rotate points.
                /*
                if (fragment.planeDegrees > 0f) {
                    for (int i = 0; i < polygonArea.points.Count; i++) {
                        polygonArea.points [i] = rotation * polygonArea.points [i];
                    }
                }
                */

                /*
                if (polygonArea.lod == 0 && polygonArea.fragment == 0) {
                Vector3 dist = new Vector3(0.005f, 0.005f, 0.005f);
                    for (int i = 0; i < polygonArea.points.Count; i++) {
                        Debug.DrawLine (polygonArea.points[i], polygonArea.points[i] + dist, Color.yellow, 10f);
                    }
                }
                */

                // AABB box.
                Bounds _aabb = GeometryUtility.CalculateBounds (polygonArea.points.ToArray (), Matrix4x4.identity);
                polygonArea.aabb = _aabb;
                polygonArea.planeUp = fragment.planeAxis;
                polygonArea.planeNormal = fragment.planeNormal;
                /*
                if (polygonArea.lod == 1 && polygonArea.fragment == 1) {
                    DrawBounds (_aabb);
                }
                */

                // OBB box.
                float _obbAngle = 0f;
                GeometryAnalyzer ga = GeometryAnalyzer.Current ();
                Bounds _obb = ga.GetOBBFromPolygon (polygonArea.points, out _obbAngle);
                polygonArea.obb = _obb;
                polygonArea.obbAngle = _obbAngle;

                /*
                // Rotate back.
                if (fragment.planeDegrees > 0f) {
                    for (int i = 0; i < polygonArea.points.Count; i++) {
                        polygonArea.points [i] = reverseRotation * polygonArea.points [i];
                    }
                }
                */
            }

            // Set scale.
            tree.obj.transform.rotation = Quaternion.Inverse (Quaternion.LookRotation (Vector3.Cross (fragment.planeNormal, fragment.planeAxis), fragment.planeAxis));
            MeshRenderer mr = tree.obj.GetComponent<MeshRenderer> ();
            /*
            float meshWidth = treeMesh.bounds.max.z - treeMesh.bounds.min.z;
            float meshHeight = treeMesh.bounds.max.y - treeMesh.bounds.min.y;
            */
            
            float meshWidth = mr.bounds.max.z - mr.bounds.min.z;
            float meshHeight = mr.bounds.max.y - mr.bounds.min.y;
            
            if (meshWidth > meshHeight) {
                polygonArea.scale = (polygonArea.aabb.max.z - polygonArea.aabb.min.z) / meshWidth;
            } else {
                polygonArea.scale = (polygonArea.aabb.max.y - polygonArea.aabb.min.y) / meshHeight;
            }
            tree.obj.transform.rotation = Quaternion.identity;
        }




        
        protected void DrawBounds (Bounds bounds) {
            // Get all 8 points for the bounds.
            Vector3 minB = bounds.min;
            Vector3 maxB = bounds.max;
            Vector3[] bps = new Vector3[8];
            float t = 20f;
            Color color = Color.white;
            bps[0] = minB;
            bps[1] = minB;
            bps[1].z = maxB.z;
            bps[2] = maxB;
            bps[2].y = minB.y;
            bps[3] = minB;
            bps[3].x = maxB.x;
            bps[4] = minB;
            bps[4].y = maxB.y;
            bps[5] = maxB;
            bps[5].x = minB.x;
            bps[6] = maxB;
            bps[7] = maxB;
            bps[7].z = minB.z;

            /*
            Quaternion rot = Quaternion.FromToRotation (Vector3.right, forward);
            for (int i = 0; i < 8; i++) {
                bps [i] = rot * bps [i] + bounds.center;
            }
            */

            Debug.DrawLine (bps[0], bps[1], color, t);
            Debug.DrawLine (bps[1], bps[2], color, t);
            Debug.DrawLine (bps[2], bps[3], color, t);
            Debug.DrawLine (bps[3], bps[0], color, t);

            Debug.DrawLine (bps[0], bps[4], color, t);
            Debug.DrawLine (bps[1], bps[5], color, t);
            Debug.DrawLine (bps[2], bps[6], color, t);
            Debug.DrawLine (bps[3], bps[7], color, t);

            Debug.DrawLine (bps[4], bps[5], color, t);
            Debug.DrawLine (bps[5], bps[6], color, t);
            Debug.DrawLine (bps[6], bps[7], color, t);
            Debug.DrawLine (bps[7], bps[4], color, t);
        }

        protected void DrawPoints (List<Vector3> points) {
            float t = 20f;
            Vector3 d = new Vector3(0.01f, 0.01f, 0.01f);
            Color color = Color.yellow;
            for (int i = 0; i < points.Count; i++) {
                Debug.DrawLine (points[i], points[i] + d, color, t);
            }
        }
        /// <summary>
        /// Calculates the mesh related values for the polygon area definition and
        /// creates its mesh.
        /// </summary>
        /// <param name="polygonArea">Polygon area to create the mesh from.</param>
        public virtual void ProcessPolygonAreaMesh (PolygonArea polygonArea, Bounds refBounds, Fragment fragment) {
            // TRIANGLES.
            GeometryAnalyzer ga = GeometryAnalyzer.Current ();
            List<int> _triangles = new List<int> ();
            if (polygonArea.isNonConvexHull) {
                _triangles = ga.DelaunayConstrainedTriangulationYZ (polygonArea.points, polygonArea.lastConvexPointIndex);
            } else {
                _triangles = ga.DelaunayTriangulationYZ (polygonArea.points);
            }
            polygonArea.triangles.Clear ();
            polygonArea.triangles.AddRange (_triangles);

            PreProcessPolygonAreaMesh (polygonArea, fragment);


            // UVS.
			float z, y;
            /*
            if (polygonArea.lod == 1 && polygonArea.fragment == 1) {
                Debug.Log ("lod 1, frag 1");
                DrawBounds (refBounds);
                DrawPoints (polygonArea.points);
            }
            */
			List<Vector4> uvs = new List<Vector4> ();
			for (int i = 0; i < polygonArea.points.Count; i++) {
				//z = Mathf.InverseLerp (polygonArea.aabb.min.z, polygonArea.aabb.max.z, polygonArea.points [i].z);
				//y = Mathf.InverseLerp (polygonArea.aabb.min.y, polygonArea.aabb.max.y, polygonArea.points [i].y);
                z = Mathf.InverseLerp (refBounds.min.z, refBounds.max.z, polygonArea.points [i].z);
				y = Mathf.InverseLerp (refBounds.min.y, refBounds.max.y, polygonArea.points [i].y);
                
				uvs.Add (new Vector4 (z, y, z, y));
                /*
                if (polygonArea.lod == 1 && polygonArea.fragment == 1) {
                    Debug.Log (string.Format ("uv[{0}]: {1}, {2}", i, z, y));
                }
                */
			}
			polygonArea.uvs.Clear ();
			polygonArea.uvs.AddRange (uvs);


            // NORMALS.
            /*
            mesh.RecalculateNormals ();
            List<Vector3> _normals = new List<Vector3> ();
            mesh.GetNormals (_normals);
            polygonArea.normals.AddRange (_normals);
            */
            /*
            float normalPivotFactor = 0.25f;
            float normalPivotDistance = 0.1f;
            */
            float normalPivotFactor = 0f;
            float normalPivotDistance = 0.1f;
            if ((refBounds.max.z - refBounds.min.z) > (refBounds.max.y - refBounds.min.y)) {
                // Wider
                normalPivotDistance *= refBounds.max.z - refBounds.min.z;
            } else {
                // Taller
                normalPivotDistance *= refBounds.max.y - refBounds.min.y;
            }
            Vector3 _normalsPivot = refBounds.center;
            _normalsPivot.y = refBounds.min.y;
            //_normalsPivot = _normalsPivot - (Vector3.right * normalPivotDistance);
            _normalsPivot = _normalsPivot - (Vector3.right * normalPivotDistance);
            Vector3[] _normals = new Vector3[polygonArea.points.Count];
            for (int i = 0; i < _normals.Length; i++) {
                //_normals [i] = Vector3.right;
                _normals [i] = (Vector3.Lerp (Vector3.right, polygonArea.points [i] - _normalsPivot, normalPivotFactor)).normalized;
            }
            polygonArea.normals.AddRange (_normals);


            // Rotate from YZ plane back to original plane.
            RotatePointsToYZ (polygonArea.points, polygonArea, fragment, true);
            RotatePointsToYZ (polygonArea.normals, polygonArea, fragment, true);

			Mesh mesh = new Mesh ();
			// Set vertices.
			mesh.SetVertices (polygonArea.points);
            // Set normals.
            mesh.SetNormals (polygonArea.normals);
			// Set triangles.
			mesh.SetTriangles (polygonArea.triangles, 0);
            // Set uvs.
            mesh.SetUVs (0, uvs);            

            // Set Tangents.
            /*
            Vector4[] _tangents = new Vector4[polygonArea.points.Count];
            for (int i = 0; i < _tangents.Length; i++) {
                _tangents [i] = Vector3.forward;
                _tangents [i].w = -1;
            }
            mesh.SetTangents (_tangents);
            polygonArea.tangents.AddRange (_tangents);
            */

            mesh.RecalculateTangents ();
            List<Vector4> _tangents = new List<Vector4> ();
            mesh.GetTangents (_tangents);
            polygonArea.tangents.AddRange (_tangents);

            // Set tangents.
            //mesh.RecalculateTangents ();
            /*
			Vector4[] _tangents = new Vector4[polygonArea.points.Count];
			for (int i = 0; i < _tangents.Length; i++) {
				_tangents [i] = Vector3.forward; 
				_tangents [i].w = 1f;
			}
			mesh.tangents = _tangents;
            */
            /*
			polygonArea.tangents.Clear ();
			polygonArea.tangents.AddRange (mesh.tangents);
            */

            /*
            for (int i = 0; i < _normals.Length; i++) {
                _normals [i] = _normals [i] * -1;
            }
            */

			mesh.RecalculateBounds ();

			// Set the mesh.
			polygonArea.mesh = mesh;
		}
        #endregion
    }
}