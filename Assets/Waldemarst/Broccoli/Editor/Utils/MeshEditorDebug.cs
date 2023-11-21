using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using Broccoli.Pipe;
using Broccoli.Model;
using Broccoli.Utils;
using Broccoli.Builder;

namespace Broccoli.BroccoEditor {
    /// <summary>
    /// Class to draw a mesh with debugging options.
    /// </summary>
    public class MeshEditorDebug {
        #region Vars
        /// <summary>
        /// Mesh to debug.
        /// </summary>
        private Mesh _mesh = null;
        /// <summary>
        /// Mesh preview to use as canvas to draw the mesh.
        /// </summary>
        private MeshPreview _meshPreview = null;
        /// <summary>
        /// SproutLan editor to debug.
        /// </summary>
        private SproutLabEditor _sproutLabEditor = null;
        private Mesh _srcMesh = null;
        private ScriptableObject _branchCollection = null;
        private static Vector3[] bps = new Vector3[8];
        private static Vector3 minB;
        private static Vector3 maxB;
        #endregion

        #region Accessors
        public Mesh mesh {
            get { return _mesh; }
        }
        public Material material {
            get { return debugSettings.targetMaterial; }
        }
        #endregion

        #region GUI Vars
        private static string buildPlaneBtn = "Build Plane Mesh";
        private static string buildGridMesh = "Build Grid Mesh";
        private static string buildPlaneXBtn = "Build Plane X Mesh";
        private static string buildMeshBtn = "Build Mesh";
        private static string widthLabel = "Width";
        private static string heightLabel = "Height";
        private static string widthPivotLabel = "Width Pivot";
        private static string heightPivotLabel = "Height Pivot";
        private static string widthSegmentsLabel = "Width Segments";
        private static string heightSegmentsLabel = "Height Segments";
        private static string depthLabel = "Depth";
        private static string meshLabel = "Mesh";
        private static string pivotLabel = "Pivot";
        private static string scaleLabel = "Scale";
        private static string orientationLabel = "Rotation";
        private static string planeFoldoutLabel = "Plane Mesh";
        private static string gridFoldoutLabel = "Grid Mesh";
        private static string planeXFoldoutLabel = "Plane X Mesh";
        private static string meshFoldoutLabel = "Mesh Object";
        private static string branchCollectionFoldoutLabel = "BranchCollectionSO";
        private static string planesLabel = "Planes";
        private bool planeMeshFoldout = false;
        private bool gridMeshFoldout = false;
        private bool planeXMeshFoldout = false;
        private bool meshFoldout = false;
        private bool branchCollectionFoldout = false;
        #endregion

        #region Singleton
        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static MeshEditorDebug _current = null;
        /// <summary>
        /// Gets the singleton instance for this class.
        /// </summary>
        /// <returns>Singleton instance.</returns>
        public static MeshEditorDebug Current () {
            if (_current == null) {
                _current = new MeshEditorDebug ();
            }
            return _current;
        }
        #endregion

        #region Draw Builder
        public bool DrawEditorBuildOptions (MeshPreview meshPreview) {
            _meshPreview = meshPreview;
            debugSettings.targetMaterial = (Material) EditorGUILayout.ObjectField (debugSettings.targetMaterial, typeof (Material), true);
            bool result = false;

            // Plane mesh.
            planeMeshFoldout = EditorGUILayout.Foldout (planeMeshFoldout, planeFoldoutLabel);
            if (planeMeshFoldout) {
                DrawSizeOptions ();
                DrawPivotOptions ();
                DrawPlaneOptions ();
                if (GUILayout.Button (buildPlaneBtn)) {
                    DebugAddPlaneMesh ();
                    result = true;
                }
                EditorGUILayout.Space ();
            }

            // Grid mesh.
            gridMeshFoldout = EditorGUILayout.Foldout (gridMeshFoldout, gridFoldoutLabel);
            if (gridMeshFoldout) {
                DrawSizeOptions ();
                DrawPivotOptions ();
                DrawSegmentOptions ();
                DrawPlaneOptions ();
                if (GUILayout.Button (buildGridMesh)) {
                    DebugAddGridMesh ();
                    result = true;
                }
            }

            // Plane X mesh.
            planeXMeshFoldout = EditorGUILayout.Foldout (planeXMeshFoldout, planeXFoldoutLabel);
            if (planeXMeshFoldout) {
                DrawSizeOptions ();
                DrawPivotOptions ();
                DrawDepthOptions ();
                if (GUILayout.Button (buildPlaneXBtn)) {
                    DebugAddPlaneXMesh ();
                    result = true;
                }
                EditorGUILayout.Space ();
            }

            // Source mesh.
            meshFoldout = EditorGUILayout.Foldout (meshFoldout, meshFoldoutLabel);
            if (meshFoldout) {
                DrawMeshOptions ();
                if (GUILayout.Button (buildMeshBtn)) {
                    DebugAddMesh ();
                    result = true;
                }
                EditorGUILayout.Space ();
            }

            // Branch Collection SO.
            branchCollectionFoldout = EditorGUILayout.Foldout (branchCollectionFoldout, branchCollectionFoldoutLabel);
            if (branchCollectionFoldout) {
                DrawBranchCollectionOptions ();
                if (GUILayout.Button (buildMeshBtn)) {
                    DebugAddBranchCollection ();
                    result = true;
                }
                EditorGUILayout.Space ();
            }

            _meshPreview = null;
            return result;
        }

        public void DrawSizeOptions () {
            debugSettings.targetWidth = EditorGUILayout.FloatField (widthLabel, debugSettings.targetWidth);
            debugSettings.targetHeight = EditorGUILayout.FloatField (heightLabel, debugSettings.targetHeight);
        }
        public void DrawPivotOptions () {
            debugSettings.targetWidthPivot = EditorGUILayout.FloatField (widthPivotLabel, debugSettings.targetWidthPivot);
            debugSettings.targetHeightPivot = EditorGUILayout.FloatField (heightPivotLabel, debugSettings.targetHeightPivot);
        }
        public void DrawSegmentOptions () {
            debugSettings.targetWidthSegments = EditorGUILayout.IntField (widthSegmentsLabel, debugSettings.targetWidthSegments);
            debugSettings.targetHeightSegments = EditorGUILayout.IntField (heightSegmentsLabel, debugSettings.targetHeightSegments);
        }
        public void DrawPlaneOptions () {
            debugSettings.targetPlanes = EditorGUILayout.IntSlider (planesLabel, debugSettings.targetPlanes, 1, 3);
        }
        public void DrawDepthOptions () {
            debugSettings.targetDepth = EditorGUILayout.FloatField (depthLabel, debugSettings.targetDepth);
        }
        public void DrawMeshOptions () {
            _srcMesh = (Mesh)EditorGUILayout.ObjectField (meshLabel, _srcMesh, typeof(Mesh), true);
            debugSettings.targetPivot = EditorGUILayout.Vector3Field (pivotLabel, debugSettings.targetPivot);
            debugSettings.targetMeshScale = EditorGUILayout.Vector3Field (scaleLabel, debugSettings.targetMeshScale);
            debugSettings.targetMeshRotation = EditorGUILayout.Vector3Field (orientationLabel, debugSettings.targetMeshRotation);
        }
        public void DrawBranchCollectionOptions () {
            _branchCollection = (BranchDescriptorCollectionSO) EditorGUILayout.ObjectField (
                _branchCollection,
                typeof (BranchDescriptorCollectionSO), 
                true);
        }
        #endregion

        #region Draw Process
        public bool DrawEditorProcessOptions (SproutLabEditor sproutLabEditor) {
            _sproutLabEditor = sproutLabEditor;
            _meshPreview = sproutLabEditor.meshPreview;

            debugSettings.targetMaterial = (Material) EditorGUILayout.ObjectField (debugSettings.targetMaterial, typeof (Material), true);
            bool result = false;

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ("Process Mesh", EditorStyles.label);

            _meshPreview = null;
            return result;
        }
        /// <summary>
        /// Displays options to process orientation.
        /// </summary>
        public void DrawOrientationOptions () {
            debugSettings.targetOrientationForward = EditorGUILayout.Vector3Field ("Forward", debugSettings.targetOrientationForward);
            debugSettings.targetOrientationNormal = EditorGUILayout.Vector3Field ("Normal", debugSettings.targetOrientationNormal);
        }
        /// <summary>
        /// Displays options to process orientation using Euler angles.
        /// </summary>
        public void DrawOrientationEulerOptions () {
            debugSettings.targetOrientationEuler = EditorGUILayout.Vector3Field ("Forward", debugSettings.targetOrientationEuler);
        }
        /// <summary>
        /// Display options to process scaling.
        /// </summary>
        public void DrawScaleOptions () {
            debugSettings.targetScale = EditorGUILayout.FloatField ("Scale", debugSettings.targetScale);
        }
        /// <summary>
        /// Display options to apply a positional offset.
        /// </summary>
        public void DrawPositionOptions () {
            debugSettings.targetPosition = EditorGUILayout.Vector3Field ("Position",  debugSettings.targetPosition);
        }
        public void DrawBendOptions () {
            debugSettings.targetBendMode = (int)((MeshJob.BendMode)EditorGUILayout.EnumPopup ("Bend Mode", (MeshJob.BendMode)debugSettings.targetBendMode));
            debugSettings.targetBendForward = EditorGUILayout.Slider ("Fw Bend", debugSettings.targetBendForward, -5f, 5f);
            debugSettings.targetBendSide = EditorGUILayout.Slider ("Side Bend", debugSettings.targetBendSide, -5f, 5f);
        }
        #endregion

        #region Draw Snapshots
        private SproutLabDebugSettings debugSettings = SproutLabDebugSettings.instance;
        BezierCurve _snapshotBaseCurve = null;
        BezierCurve _snapshotCurve = null;
        Mesh _snapshotMesh = null;
        Material[] _snapshotMats;
        public bool DrawEditorSnapshotMesh (SproutLabEditor sproutLabEditor) {
            _sproutLabEditor = sproutLabEditor;
            _meshPreview = sproutLabEditor.meshPreview;

            bool result = false;
            if (_sproutLabEditor.selectedSnapshot != null) {
                debugSettings.targetMaterial = (Material) EditorGUILayout.ObjectField ("Preview Material", debugSettings.targetMaterial, typeof (Material), true);
                debugSettings.snapshotLODIndex = EditorGUILayout.IntField ("LOD", debugSettings.snapshotLODIndex);
                debugSettings.snapshotResolution = EditorGUILayout.IntField ("Resolution", debugSettings.snapshotResolution);
                debugSettings.wireframeEnabled = EditorGUILayout.Toggle ("Wireframe", debugSettings.wireframeEnabled);
                if (debugSettings.wireframeEnabled) {
                    EditorGUI.indentLevel++;
                    debugSettings.wireframeBaseColor = EditorGUILayout.ColorField ("Base Color", debugSettings.wireframeBaseColor);
                    EditorGUI.indentLevel--;
                }
                if (GUILayout.Button ("Display Snapshot Mesh")) {
                    if (debugSettings.snapshotLODIndex <= _sproutLabEditor.selectedSnapshot.lodCount - 1) {
                        DebugDisplaySnapshot (debugSettings.snapshotLODIndex, debugSettings.snapshotResolution, debugSettings.wireframeEnabled);
                        result = true;
                    }
                }
                if (GUILayout.Button ("Display Snapshot Mesh and Export to Scene")) {
                    if (debugSettings.snapshotLODIndex <= _sproutLabEditor.selectedSnapshot.lodCount - 1) {
                        DebugDisplaySnapshot (
                            debugSettings.snapshotLODIndex, 
                            debugSettings.snapshotResolution, 
                            debugSettings.wireframeEnabled,
                            true);
                        result = true;
                    }
                }
                EditorGUILayout.Space ();
                debugSettings.showMeshBounds = EditorGUILayout.Toggle ("Show Bounds", debugSettings.showMeshBounds);
                debugSettings.snapshotShowCurve = EditorGUILayout.Toggle ("Show Curve", debugSettings.snapshotShowCurve);
                if (debugSettings.snapshotShowCurve) {
                    EditorGUI.indentLevel++;
                    debugSettings.showCurveNodes = EditorGUILayout.Toggle ("Show Nodes", debugSettings.showCurveNodes);
                    if (debugSettings.showCurveNodes) {
                        EditorGUI.indentLevel++;
                        debugSettings.showCurveNodeForward = EditorGUILayout.Toggle ("Show Forward", debugSettings.showCurveNodeForward);
                        debugSettings.showCurveNodeUp = EditorGUILayout.Toggle ("Show Up", debugSettings.showCurveNodeUp);
                        EditorGUI.indentLevel--;
                    }
                    debugSettings.showCurvePoints = EditorGUILayout.Toggle ("Show Points", debugSettings.showCurvePoints);
                    if (debugSettings.showCurvePoints) {
                        EditorGUI.indentLevel++;
                        debugSettings.showCurvePointForward = EditorGUILayout.Toggle ("Show Forward", debugSettings.showCurvePointForward);
                        debugSettings.showCurvePointNormal = EditorGUILayout.Toggle ("Show Normal", debugSettings.showCurvePointNormal);
                        debugSettings.showCurvePointUp = EditorGUILayout.Toggle ("Show Up", debugSettings.showCurvePointUp);
                        debugSettings.showCurvePointTangent = EditorGUILayout.Toggle ("Show Tangent", debugSettings.showCurvePointTangent);
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
                debugSettings.showGravityDirection = EditorGUILayout.Toggle ("Show Gravity Direction", debugSettings.showGravityDirection);
            }

            return result;
        }
        public bool DrawEditorSnapshotProcess (SproutLabEditor sproutLabEditor) {
            _sproutLabEditor = sproutLabEditor;
            _meshPreview = sproutLabEditor.meshPreview;

            bool result = false;
            DrawPositionOptions ();
            DrawScaleOptions ();
            //DrawOrientationOptions ();
            DrawOrientationEulerOptions ();
            DrawBendOptions ();
            if (GUILayout.Button ("Process Mesh")) {
                ProcessMesh (); 
            }

            return result;
        }
        #endregion

        #region Draw Material
        public bool DrawMaterialOptions (SproutLabEditor sproutLabEditor) {
            _sproutLabEditor = sproutLabEditor;
            _meshPreview = sproutLabEditor.meshPreview;

            bool result = false;

            EditorGUILayout.Space ();
            EditorGUILayout.LabelField ("Material Replacement", EditorStyles.boldLabel);
            debugSettings.targetMaterial = (Material) EditorGUILayout.ObjectField ("Preview Material", debugSettings.targetMaterial, typeof (Material), true);
            debugSettings.wireframeEnabled = EditorGUILayout.Toggle ("Wireframe", debugSettings.wireframeEnabled);
            if (debugSettings.wireframeEnabled) {
                EditorGUI.indentLevel++;
                debugSettings.wireframeBaseColor = EditorGUILayout.ColorField ("Base Color", debugSettings.wireframeBaseColor);
                EditorGUI.indentLevel--;
            }
            if (GUILayout.Button ("Replace Material")) {
                for (int i = 0; i < sproutLabEditor.currentPreviewMaterials.Length; i++) {
                    if (debugSettings.targetMaterial == null) {
                        sproutLabEditor.currentPreviewMaterials[i].shader = Shader.Find ("Hidden/Broccoli/WireframeUnlit");
                        sproutLabEditor.currentPreviewMaterials[i].SetColor ("_BaseColor", debugSettings.wireframeBaseColor);
                        sproutLabEditor.currentPreviewMaterials[i].SetColor ("_WireColor", new Color (0.9f, 0.9f, 0.9f));
                        sproutLabEditor.currentPreviewMaterials[i].SetFloat ("_WireThickness", 600f);
                    } else {
                        sproutLabEditor.currentPreviewMaterials[i] = debugSettings.targetMaterial;
                    }
                }
            }

            return result;
        }
        #endregion

        #region Draw Handles
        public void OnPreviewMeshDrawHandles (Rect r, Camera camera) {
            if (_sproutLabEditor != null) {
                if (debugSettings.snapshotShowCurve && _snapshotCurve != null) {
                    BezierCurveDraw.DrawCurve (
                        _snapshotCurve, 
                        Vector3.zero, 
                        _sproutLabEditor.sproutSubfactory.factoryScale, 
                        Color.yellow, 
                        5f);
                    if (debugSettings.showCurvePoints) {
                        BezierCurveDraw.DrawCurvePoints (
                            _snapshotCurve,
                            Vector3.zero,
                            _sproutLabEditor.sproutSubfactory.factoryScale,
                            Color.yellow,
                            debugSettings.showCurvePointForward,
                            debugSettings.showCurvePointNormal,
                            debugSettings.showCurvePointUp,
                            debugSettings.showCurvePointTangent
                        );
                    }
                    if (debugSettings.showCurveNodes) {
                        BezierCurveDraw.DrawCurveNodes (
                            _snapshotCurve, 
                            Vector3.zero, 
                            _sproutLabEditor.sproutSubfactory.factoryScale, 
                            Color.yellow,
                            debugSettings.showCurveNodeForward,
                            debugSettings.showCurveNodeUp);
                    }
                }
                if (debugSettings.showMeshBounds && _snapshotMesh != null) {
                    Handles.color = Color.white;
                    Handles.DrawWireCube (_snapshotMesh.bounds.center, _snapshotMesh.bounds.size);
                }
                if (debugSettings.showGravityDirection && _snapshotMesh != null) {
                    Handles.color = debugSettings.gravityDirectionColor;
                    float handleSize = HandleUtility.GetHandleSize (debugSettings.gravityDirectionPos) * debugSettings.gravityDirectionScale;
                    Handles.ArrowHandleCap (-1, debugSettings.gravityDirectionPos, Quaternion.LookRotation(debugSettings.gravityDirection), handleSize, EventType.Repaint);
                }
            }
        }
        #endregion

        #region Mesh Cases
        private void ProcessMesh () {
            if (_snapshotMesh != null) {
                Object.DestroyImmediate (_snapshotMesh);
            }
            _snapshotMesh = Mesh.Instantiate(_mesh);

            // Create Job and apply procesing.
            MeshJob meshJob = new MeshJob ();
            meshJob.applyIdsChannel = 4;
            MeshJob.gravityDirection = debugSettings.gravityDirection;
            meshJob.bendMode = (MeshJob.BendMode)debugSettings.targetBendMode;

            // Add job for the mesh/submesh unit.
            Quaternion targetRotation = Quaternion.Euler (debugSettings.targetOrientationEuler); 


            meshJob.AddTransform (
                0, 
                _snapshotMesh.vertexCount,
                Vector3.zero,
                debugSettings.targetPosition, 
                debugSettings.targetScale, 
                targetRotation);
            meshJob.IncludeBending (debugSettings.targetBendForward, debugSettings.targetBendSide);    

            // Set the target mesh and execute the Job system.
            meshJob.SetTargetMesh (_snapshotMesh);
            meshJob.ExecuteJob ();
                        
            CurveJob curveJob = new CurveJob ();
            CurveJob.gravityDirection = debugSettings.gravityDirection;
            curveJob.bendMode = (MeshJob.BendMode)debugSettings.targetBendMode;
            _snapshotCurve = _snapshotBaseCurve.Clone ();
            curveJob.AddTransform (
                _snapshotCurve,
                _snapshotMesh.bounds,
                Vector3.zero,
                debugSettings.targetPosition, 
                debugSettings.targetScale, 
                targetRotation);
            curveJob.IncludeBending (debugSettings.targetBendForward, debugSettings.targetBendSide);
            curveJob.ExecuteJob ();

            /*
            // Create Jobs.
            MeshJob meshJob = new MeshJob ();
            meshJob.applyIdsChannel = 4;
            WindJob windJob = new WindJob ();
            BranchDescriptor branchDescriptor;

            // Create mesh related vars.
            Mesh snapshotMesh;
            List<Mesh> unitMeshes = new List<Mesh> ();
            List<Mesh> snapshotSubmeshes = new List<Mesh> ();
            int submeshVertexCount = 0;
            int vertexIndex = 0;

            // Clear list of snapshot ids.
            variationDescriptor.snapshotIds.Clear ();

            // Iterate through all the snapshots in the collection.
            // See if units in the variation groups have them assigned to their units.
            for (int snapshotI = 0; snapshotI < branchDescriptorCollection.snapshots.Count; snapshotI++) {
                branchDescriptor = branchDescriptorCollection.snapshots [snapshotI];
                VariationDescriptor.VariationGroupCluster cluster;
                VariationDescriptor.VariationUnit unit;
                // Get the snapshot.
                BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotI];
                // Get the snapshot mesh for the LOD.
                snapshotMesh = sproutSubfactory.sproutCompositeManager.GetMesh (snapshot.id, lod);
                // If the mesh is not null, iterate through each submesh.
                for (int submeshI = 0; submeshI < snapshotMesh.subMeshCount; submeshI++) {
                    submeshVertexCount = snapshotMesh.GetSubMesh (submeshI).vertexCount;
                    // Iterate units in the clusters to merge submeshes.
                    var clusterEnums = variationDescriptor.variationGroupClusters.GetEnumerator ();
                    while (clusterEnums.MoveNext ()) {
                        cluster = clusterEnums.Current.Value;
                        // For each unit in the cluster, add mesh variables.
                        for (int unitI = 0; unitI < cluster.variationUnits.Count; unitI++) {
                            // Get the mesh for the snapshot id.
                            unit = cluster.variationUnits [unitI];
                            // If the unit has the same snapshot index we are iterating through.
                            if (unit.snapshotIndex >= 0 && unit.snapshotIndex == snapshotI) {
                                // Add unit mesh.
                                unitMeshes.Add (snapshotMesh);
                                // Add job for the mesh/submesh unit.
                                meshJob.AddTransform (vertexIndex, submeshVertexCount, unit.position, unit.scale, unit.rotation);
                                meshJob.IncludeBending (unit.bending);
                                meshJob.IncludeIds (cluster.groupId);
                                // Add wind job params.
                                windJob.AddWindUnit (vertexIndex, submeshVertexCount, Random.Range (0f, 5f), Random.Range (0f, 14f), Random.Range (0f, 15f), unit.position);
                                // Add vertex count.
                                vertexIndex += submeshVertexCount;
                            }
                        }
                    }
                    if (unitMeshes.Count > 0) {
                        // Combine all unitMeshes for the submesh.
                        snapshotSubmeshes.Add (MeshUtils.CombineMeshes (unitMeshes, submeshI));
                        if (!variationDescriptor.snapshotIds.Contains (snapshot.id)) variationDescriptor.snapshotIds.Add (snapshot.id);
                    }
                    unitMeshes.Clear ();
                }
            }
            // Combine all snapshot meshes.
            Mesh variationMesh = MeshUtils.CombineMeshesAdditive (snapshotSubmeshes);
            // Set the target mesh and execute the Job system.
            meshJob.SetTargetMesh (variationMesh);
            meshJob.ExecuteJob ();
            // Set the target mesh and execute the Wind Job system.
            windJob.SetTargetMesh (variationMesh);
            windJob.ExecuteJob ();
            */

            // Display resulting mesh.
            DebugAddMesh (_snapshotMesh, _snapshotMats);
        }
        private void DebugDisplaySnapshot (
            int lodIndex, 
            int resolution, 
            bool wireframeEnabled, 
            bool exportToScene = false) 
        {
            _sproutLabEditor.sproutSubfactory.ProcessSnapshotPolygons (_sproutLabEditor.selectedSnapshot);
            _sproutLabEditor.meshPreview.hasSecondPass = false;
            _snapshotBaseCurve = _sproutLabEditor.selectedSnapshot.curve.Clone ();
            _snapshotCurve = _snapshotBaseCurve.Clone ();

            for (int j = 0; j < _sproutLabEditor.selectedSnapshot.polygonAreas.Count; j++) {
                PolygonAreaBuilder.SetPolygonAreaMesh (_sproutLabEditor.selectedSnapshot.polygonAreas [j]);
                _sproutLabEditor.sproutSubfactory.sproutCompositeManager.ManagePolygonArea (_sproutLabEditor.selectedSnapshot.polygonAreas [j], _sproutLabEditor.selectedSnapshot);
            }
			_mesh = Mesh.Instantiate(_sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetMesh (_sproutLabEditor.selectedSnapshot.id, lodIndex, resolution));
            _snapshotMesh = Mesh.Instantiate(_mesh);


            _snapshotMats = _sproutLabEditor.sproutSubfactory.sproutCompositeManager.GetMaterials (_sproutLabEditor.selectedSnapshot.id, lodIndex);
            if (wireframeEnabled) {
                for (int i = 0; i < _snapshotMats.Length; i++) {
                    if (debugSettings.targetMaterial == null) {
                        _snapshotMats[i].shader = Shader.Find ("Hidden/Broccoli/WireframeUnlit");
                        _snapshotMats[i].SetColor ("_BaseColor", debugSettings.wireframeBaseColor);
                        _snapshotMats[i].SetColor ("_WireColor", new Color (0.9f, 0.9f, 0.9f));
                        _snapshotMats[i].SetFloat ("_WireThickness", 600f);
                    } else {
                        _snapshotMats[i] = debugSettings.targetMaterial;
                    }
                }
            }
			DebugAddMesh (_snapshotMesh, _snapshotMats);
            if (exportToScene) {
                GameObject go = new GameObject (string.Format("snapshot_{0}_lod_{1}_res_{2}", 
                    _sproutLabEditor.selectedSnapshot.id, lodIndex, resolution));

                MeshFilter mf = go.AddComponent<MeshFilter> ();
                mf.sharedMesh = _snapshotMesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer> ();
                mr.sharedMaterials = _snapshotMats;
            }
		}
        private void DebugAddPlaneMesh () {
            Broccoli.Builder.PlaneSproutMeshBuilder.SetUVData (0f, 0f, 1f, 1f, 0);
			Mesh planeMesh = 
                Broccoli.Builder.PlaneSproutMeshBuilder.GetPlaneMesh (
                    debugSettings.targetWidth, debugSettings.targetHeight, debugSettings.targetWidthPivot, debugSettings.targetHeightPivot, debugSettings.targetPlanes
                );
			DebugAddMesh (planeMesh);
		}
		private void DebugAddGridMesh () {
            Broccoli.Builder.GridSproutMeshBuilder.SetUVData (0f, 0f, 1f, 1f, 0);
			Mesh gridMesh = 
                Broccoli.Builder.GridSproutMeshBuilder.GetGridMesh (
                    debugSettings.targetWidth, debugSettings.targetHeight, debugSettings.targetWidthSegments, debugSettings.targetHeightSegments, debugSettings.targetWidthPivot, debugSettings.targetHeightPivot, debugSettings.targetPlanes
                );
			DebugAddMesh (gridMesh);
		}
        private void DebugAddPlaneXMesh () {
            Broccoli.Builder.PlaneXSproutMeshBuilder.SetUVData (0f, 0f, 1f, 1f, 0);
			Mesh planeMesh = 
                Broccoli.Builder.PlaneXSproutMeshBuilder.GetPlaneXMesh (
                    debugSettings.targetWidth, debugSettings.targetHeight, debugSettings.targetWidthPivot, debugSettings.targetHeightPivot, debugSettings.targetDepth
                );
			DebugAddMesh (planeMesh);
		}
        private void DebugAddMesh () {
			Mesh mesh = 
                Broccoli.Builder.MeshSproutMeshBuilder.GetMesh (
                    _srcMesh, debugSettings.targetMeshScale, debugSettings.targetPivot, Quaternion.Euler (debugSettings.targetMeshRotation)
                );
			DebugAddMesh (mesh);
		}
        private void DebugAddBranchCollection () {
			Mesh mesh = 
                Broccoli.Builder.BranchCollectionSproutMeshBuilder.GetMesh (
                    ((BranchDescriptorCollectionSO)_branchCollection).branchDescriptorCollection, 
                    0, 0, debugSettings.targetMeshScale, debugSettings.targetPivot, Quaternion.Euler (debugSettings.targetMeshRotation)
                );
			DebugAddMesh (mesh);
		}
		private void DebugAddMesh (Mesh mesh) {
			Material material;
			if (debugSettings.targetMaterial == null) {
				material = new Material(Shader.Find ("Hidden/Broccoli/WireframeUnlit"));
				material.SetColor ("_BaseColor", new Color (0f, 0.25f, 0.5f));
				material.SetColor ("_WireColor", new Color (0.9f, 0.9f, 0.9f));
				material.SetFloat ("_WireThickness", 600f);
			} else {
				material = debugSettings.targetMaterial;
			}
            _mesh = mesh;
            debugSettings.targetMaterial = material;
			//meshPreview.AddMesh (0, planeMesh, false);
			_meshPreview.ShowAxisHandles (true, 0.5f, Vector3.zero);
			_meshPreview.hasSecondPass = false;
			_meshPreview.SetTargetRotation (Quaternion.identity);
			_meshPreview.SetOffset (new Vector3 (0f, 0.15f, -5.5f));
            _meshPreview.SetDirection (new Vector2 (135f, -15f));
		}
		public void DebugAddMesh (Mesh previewMesh, Material[] materials) {
			if (previewMesh == null) return;
			_meshPreview.Clear ();
			_meshPreview.CreateViewport ();
			if (previewMesh.vertexCount > 0)
				previewMesh.RecalculateBounds ();
            _meshPreview.hasSecondPass = false;
			_sproutLabEditor.currentPreviewMaterials = materials;
			_meshPreview.AddMesh (0, previewMesh, true);
		}
        #endregion

        #region Utils
        public static void DrawBounds (Bounds bounds, Vector3 forward, Vector3 up, Vector3 pivot) {
            // Get all 8 points for the bounds.
            minB = bounds.min - pivot;
            maxB = bounds.max - pivot;

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

            Quaternion rot = Quaternion.LookRotation (Vector3.Cross (forward, up), up);
            for (int i = 0; i < 8; i++) {
                bps [i] = rot * bps [i] + pivot;
            }

            Handles.DrawLine (bps[0], bps[1]);
            Handles.DrawLine (bps[1], bps[2]);
            Handles.DrawLine (bps[2], bps[3]);
            Handles.DrawLine (bps[3], bps[0]);

            Handles.DrawLine (bps[0], bps[4]);
            Handles.DrawLine (bps[1], bps[5]);
            Handles.DrawLine (bps[2], bps[6]);
            Handles.DrawLine (bps[3], bps[7]);

            Handles.DrawLine (bps[4], bps[5]);
            Handles.DrawLine (bps[5], bps[6]);
            Handles.DrawLine (bps[6], bps[7]);
            Handles.DrawLine (bps[7], bps[4]);
        }
        public static void DrawPoints (List<Vector3> points, Vector3 forward, Vector3 up, float sizeFactor = 0.05f) {
            Quaternion rot = Quaternion.FromToRotation (Vector3.right, forward);
            Vector3 cameraF = Camera.current.transform.forward;
            for (int i = 0; i < points.Count; i++) {
                Handles.DrawSolidDisc (points[i], cameraF, HandleUtility.GetHandleSize (points[i]) * sizeFactor);
            }
        }
        #endregion
    }
}