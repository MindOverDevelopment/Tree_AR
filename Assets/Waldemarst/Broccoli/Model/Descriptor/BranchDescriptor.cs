using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Broccoli.Model;

namespace Broccoli.Pipe {
    /// <summary>
    /// Branch Descriptor (Snapshot) class.
    /// * Description to pipeline: SproutSubfactory.BranchDescriptionCollectionToPipeline.
    /// * Description pipeline processing: SproutSubfactory.ProcessSnapshot.
    /// * Description polygons processing: SproutSubfactory.ProcessSnapshotPolygons.
    /// </summary>
    [System.Serializable]
    public class BranchDescriptor {
        #region Branch Level Descriptor
        [System.Serializable]
        public class BranchLevelDescriptor {
            #region Vars
            public bool isEnabled = true;
            public int minFrequency = 1;
            public int maxFrequency = 1;
            public float radius = 0f;
            public float minRange = 0f;
            public float maxRange = 1f;
            public AnimationCurve distributionCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public float minLengthAtBase = 3f;
            public float maxLengthAtBase = 4f;
            public float minLengthAtTop = 3f;
            public float maxLengthAtTop = 4f;
            public AnimationCurve lengthCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public float spacingVariance = 0f;
            public float minParallelAlignAtTop = 0f;
            public float maxParallelAlignAtTop = 0f;
            public float minParallelAlignAtBase = 0f;
            public float maxParallelAlignAtBase = 0f;
            public float minGravityAlignAtTop = 0f;
            public float maxGravityAlignAtTop = 0f;
            public float minGravityAlignAtBase = 0f;
            public float maxGravityAlignAtBase = 0f;
            public float minPlaneAlignAtTop = 0f;
            public float maxPlaneAlignAtTop = 0f;
            public float minPlaneAlignAtBase = 0f;
            public float maxPlaneAlignAtBase = 0f;
            #endregion

            #region Clone
            public BranchLevelDescriptor Clone () {
                BranchLevelDescriptor clone = new BranchLevelDescriptor ();
                clone.isEnabled = isEnabled;
                clone.minFrequency = minFrequency;
                clone.maxFrequency = maxFrequency;
                clone.radius = radius;
                clone.minRange = minRange;
                clone.maxRange = maxRange;
                clone.distributionCurve = new AnimationCurve (distributionCurve.keys);
                clone.minLengthAtBase = minLengthAtBase;
                clone.maxLengthAtBase = maxLengthAtBase;
                clone.minLengthAtTop = minLengthAtTop;
                clone.maxLengthAtTop = maxLengthAtTop;
                clone.lengthCurve = new AnimationCurve (lengthCurve.keys);
                clone.spacingVariance = spacingVariance;
                clone.minParallelAlignAtTop = minParallelAlignAtTop;
                clone.maxParallelAlignAtTop = maxParallelAlignAtTop;
                clone.minParallelAlignAtBase = minParallelAlignAtBase;
                clone.maxParallelAlignAtBase = maxParallelAlignAtBase;
                clone.minGravityAlignAtTop = minGravityAlignAtTop;
                clone.maxGravityAlignAtTop = maxGravityAlignAtTop;
                clone.minGravityAlignAtBase = minGravityAlignAtBase;
                clone.maxGravityAlignAtBase = maxGravityAlignAtBase;
                clone.minPlaneAlignAtTop = minPlaneAlignAtTop;
                clone.maxPlaneAlignAtTop = maxPlaneAlignAtTop;
                clone.minPlaneAlignAtBase = minPlaneAlignAtBase;
                clone.maxPlaneAlignAtBase = maxPlaneAlignAtBase;
                return clone;
            }
            #endregion
        }
        #endregion

        #region Sprout Level Descriptor
        [System.Serializable]
        public class SproutLevelDescriptor {
            #region Vars
            public bool isEnabled = true;
            public int minFrequency = 5;
            public int maxFrequency = 9;
            public float minParallelAlignAtTop = 0f;
            public float maxParallelAlignAtTop = 0f;
            public float minParallelAlignAtBase = 0f;
            public float maxParallelAlignAtBase = 0f;
            public float minGravityAlignAtTop = 0f;
            public float maxGravityAlignAtTop = 0f;
            public float minGravityAlignAtBase = 0f;
            public float maxGravityAlignAtBase = 0f;
            public float minRange = 0f;
            public float maxRange = 1f;
            public enum Distribution
			{
				Alternative,
				Opposite
            }
            public Distribution distribution = Distribution.Alternative;
            public AnimationCurve distributionCurve = AnimationCurve.Linear (0f, 0f, 1f, 1f);
            public float spacingVariance = 0f;
            #endregion

            #region Clone
            public SproutLevelDescriptor Clone () {
                SproutLevelDescriptor clone = new SproutLevelDescriptor ();
                clone.isEnabled = isEnabled;
                clone.minFrequency = minFrequency;
                clone.maxFrequency = maxFrequency;
                clone.minParallelAlignAtTop = minParallelAlignAtTop;
                clone.maxParallelAlignAtTop = maxParallelAlignAtTop;
                clone.minParallelAlignAtBase = minParallelAlignAtBase;
                clone.maxParallelAlignAtBase = maxParallelAlignAtBase;
                clone.minGravityAlignAtTop = minGravityAlignAtTop;
                clone.maxGravityAlignAtTop = maxGravityAlignAtTop;
                clone.minGravityAlignAtBase = minGravityAlignAtBase;
                clone.maxGravityAlignAtBase = maxGravityAlignAtBase;
                clone.minRange = minRange;
                clone.maxRange = maxRange;
                clone.distribution = distribution;
                clone.distributionCurve = new AnimationCurve (distributionCurve.keys);
                clone.spacingVariance = spacingVariance;
                return clone;
            }
            #endregion
        }
        #endregion

        #region Structure Vars
        public int id = 0;
        public string snapshotType = string.Empty;
        public int processorId = 0;
        public int seed = 0;
        /// <summary>
        /// Selects how many branch structural levels are enabled in the branch hierarchy.
        /// Level 0 has only the main branch enabled.
        /// </summary>
        public int activeLevels = 1;
        /// <summary>
        /// Curve with point along the main series of branches (parent - follow up).
        /// </summary>
        /// <returns></returns>
        public BezierCurve curve = new BezierCurve ();
        public float girthAtBase = 0.2f;
        public float girthAtTop = 0.01f;
        public float noiseResolution = 0.5f;
        public float noiseAtBase = 0.5f;
        public float noiseAtTop = 0.5f;
        public float noiseScaleAtBase = 0.75f;
        public float noiseScaleAtTop = 0.75f;
        public List<BranchLevelDescriptor> branchLevelDescriptors = new List<BranchLevelDescriptor> ();
        public int sproutASubmeshIndex = -1;
        public int sproutBSubmeshIndex = -1;
        public int sproutCrownSubmeshIndex = -1;
        public int sproutASubmeshCount = 0;
        public int sproutBSubmeshCount = 0;
        public int sproutCrownSubmeshCount = 0;
        public float sproutASize = 1f;
        public float sproutAScaleAtBase = 1f;
        public float sproutAScaleAtTop = 1f;
        public float sproutAScaleVariance = 0f;
        public SproutMesh.ScaleMode sproutAScaleMode = SproutMesh.ScaleMode.Hierarchy;
        public float sproutAFlipAlign = 0.8f;
        public float sproutANormalRandomness = 0.5f;
        public float sproutABendingAtTop = 0f;
		public float sproutABendingAtBase = 0f;
		public float sproutASideBendingAtTop = 0f;
		public float sproutASideBendingAtBase = 0f;
        public List<SproutLevelDescriptor> sproutALevelDescriptors = new List<SproutLevelDescriptor> ();
        public float sproutBSize = 1f;
        public float sproutBScaleAtBase = 1f;
        public float sproutBScaleAtTop = 1f;
        public float sproutBScaleVariance = 0f;
        public SproutMesh.ScaleMode sproutBScaleMode = SproutMesh.ScaleMode.Hierarchy;
        public float sproutBFlipAlign = 0.8f;
        public float sproutBNormalRandomness = 0.5f;
        public float sproutBBendingAtTop = 0f;
		public float sproutBBendingAtBase = 0f;
		public float sproutBSideBendingAtTop = 0f;
		public float sproutBSideBendingAtBase = 0f;
        public List<SproutLevelDescriptor> sproutBLevelDescriptors = new List<SproutLevelDescriptor> ();
        public List<PolygonArea> polygonAreas = new List<PolygonArea> ();
        public int lodCount = 3;
        public Vector3 lastSproutPosition = Vector3.zero;
        public bool crownEnabled = false;
        public float crownMinRange = 0f;
        public float crownMaxRange = 0f;
        public float crownProbability = 1f;
        public int crownMinFrequency = 1;
        public int crownMaxFrequency = 1;
        public float crownDepth = 0f;
        public float crownSize = 0.05f;
        public float crownScaleAtTop = 1f;
        public float crownScaleAtBase = 1f;
        public float crownScaleVariance = 0f;
        public SproutLevelDescriptor sproutCrownLevelDescriptor = new SproutLevelDescriptor ();
        public int selectedLevelIndex = -1;
        #endregion

        #region Constructor
        public BranchDescriptor (string snapshotType = null) {
            if (!string.IsNullOrEmpty (snapshotType)) {
                this.snapshotType = snapshotType;
            }
            if (branchLevelDescriptors.Count == 0) {
                for (int i = 0; i < 4; i++) {
                    branchLevelDescriptors.Add (new BranchLevelDescriptor ());
                }
            }
            if (sproutALevelDescriptors.Count == 0) {
                for (int i = 0; i < 4; i++) {
                    sproutALevelDescriptors.Add (new SproutLevelDescriptor ());
                }
            }
            if (sproutBLevelDescriptors.Count == 0) {
                for (int i = 0; i < 4; i++) {
                    sproutBLevelDescriptors.Add (new SproutLevelDescriptor ());
                }
            }
        }
        #endregion

        #region Clone
        /// <summary>
        /// Clone this instance.
        /// </summary>
        public BranchDescriptor Clone () {
            BranchDescriptor clone = new BranchDescriptor ();
            clone.id = id;
            clone.snapshotType = snapshotType;
            clone.processorId = processorId;
            clone.seed = seed;
            clone.activeLevels = activeLevels;
            clone.curve = curve.Clone ();
            clone.girthAtBase = girthAtBase;
            clone.girthAtTop = girthAtTop;
            clone.noiseResolution = noiseResolution;
            clone.noiseAtBase = noiseAtBase;
            clone.noiseAtTop = noiseAtTop;
            clone.noiseScaleAtBase = noiseScaleAtBase;
            clone.noiseScaleAtTop = noiseScaleAtTop;
            clone.branchLevelDescriptors.Clear ();
            for (int i = 0; i < branchLevelDescriptors.Count; i++) {
                clone.branchLevelDescriptors.Add (branchLevelDescriptors [i].Clone ());
            }
            clone.sproutASubmeshIndex = sproutASubmeshIndex;
            clone.sproutBSubmeshIndex = sproutBSubmeshIndex;
            clone.sproutCrownSubmeshIndex = sproutCrownSubmeshIndex;
            clone.sproutASize = sproutASize;
            clone.sproutAScaleAtBase = sproutAScaleAtBase;
            clone.sproutAScaleAtTop = sproutAScaleAtTop;
            clone.sproutAScaleVariance = sproutAScaleVariance;
            clone.sproutAScaleMode = sproutAScaleMode;
            clone.sproutAFlipAlign = sproutAFlipAlign;
            clone.sproutANormalRandomness = sproutANormalRandomness;
            clone.sproutABendingAtTop = sproutABendingAtTop;
            clone.sproutABendingAtBase = sproutABendingAtBase;
            clone.sproutASideBendingAtTop = sproutASideBendingAtTop;
            clone.sproutASideBendingAtBase = sproutASideBendingAtBase;
            clone.sproutALevelDescriptors.Clear ();
            for (int i = 0; i < sproutALevelDescriptors.Count; i++) {
                clone.sproutALevelDescriptors.Add (sproutALevelDescriptors [i].Clone ());
            }
            clone.sproutBSize = sproutBSize;
            clone.sproutBScaleAtBase = sproutBScaleAtBase;
            clone.sproutBScaleAtTop = sproutBScaleAtTop;
            clone.sproutBScaleVariance = sproutBScaleVariance;
            clone.sproutBScaleMode = sproutBScaleMode;
            clone.sproutBFlipAlign = sproutBFlipAlign;
            clone.sproutBNormalRandomness = sproutBNormalRandomness;
            clone.sproutBBendingAtTop = sproutBBendingAtTop;
            clone.sproutBBendingAtBase = sproutBBendingAtBase;
            clone.sproutBSideBendingAtTop = sproutBSideBendingAtTop;
            clone.sproutBSideBendingAtBase = sproutBSideBendingAtBase;
            clone.sproutBLevelDescriptors.Clear ();
            for (int i = 0; i < sproutBLevelDescriptors.Count; i++) {
                clone.sproutBLevelDescriptors.Add (sproutBLevelDescriptors [i].Clone ());
            }
            for (int i = 0; i < polygonAreas.Count; i++) {
                clone.polygonAreas.Add (polygonAreas [i].Clone ());
            }
            clone.lodCount = lodCount;
            clone.crownEnabled = crownEnabled;
            clone.crownMinRange = crownMinRange;
            clone.crownMaxRange = crownMaxRange;
            clone.crownProbability = crownProbability;
            clone.crownMinFrequency = crownMinFrequency;
            clone.crownMaxFrequency = crownMaxFrequency;
            clone.crownDepth = crownDepth;
            clone.crownSize = crownSize;
            clone.crownScaleAtBase = crownScaleAtBase;
            clone.crownScaleAtTop = crownScaleAtTop;
            clone.crownScaleVariance = crownScaleVariance;
            clone.sproutCrownLevelDescriptor = sproutCrownLevelDescriptor.Clone ();
            clone.selectedLevelIndex = selectedLevelIndex;
            return clone;
        }
        #endregion

        #region Debug
        /// <summary>
        /// Get debug information about this instance values as a string.
        /// </summary>
        /// <returns>Debug information.</returns>
        public string GetDebugInfo () {
            string info = string.Format ("Id: {0}, snapshotType: {1}\n", id, snapshotType);
            info += string.Format ("ActiveLevels: {0}, processorId: {1}, seed: {2}\n", activeLevels, processorId, seed);
            info += string.Format ("Girth atBase: {0}, atTop: {1}\n", girthAtBase, girthAtTop);
            info += string.Format ("Noise Res: {0}, atBase: {1}, atTop: {2}, scaleAtBase: {3}, scaleAtTop: {4}", 
                noiseResolution, noiseAtBase, noiseAtTop, noiseScaleAtBase, noiseScaleAtTop);
            return info;
            /*
            public List<BranchLevelDescriptor> branchLevelDescriptors = new List<BranchLevelDescriptor> ();
            public float sproutASize = 1f;
            public float sproutAScaleAtBase = 1f;
            public float sproutAScaleAtTop = 1f;
            public float sproutAFlipAlign = 0.8f;
            public float sproutANormalRandomness = 0.5f;
            public List<SproutLevelDescriptor> sproutALevelDescriptors = new List<SproutLevelDescriptor> ();
            public float sproutBSize = 1f;
            public float sproutBScaleAtBase = 1f;
            public float sproutBScaleAtTop = 1f;
            public float sproutBFlipAlign = 0.8f;
            public float sproutBNormalRandomness = 0.5f;
            public List<SproutLevelDescriptor> sproutBLevelDescriptors = new List<SproutLevelDescriptor> ();
            public List<PolygonArea> polygonAreas = new List<PolygonArea> ();
            public int lodCount = 3;
            public BezierCurve curve = new BezierCurve ();
            public Vector3 lastSproutPosition = Vector3.zero;
            */
        }
        public string GetPolygonAreasDebugInfo () {
            string info = string.Format ("Polygon Areas: {0}{1}", polygonAreas.Count, (polygonAreas.Count>0?"\n":""));
            PolygonArea pa;
            int lodFrag = -1;
            for (int i = 0; i < polygonAreas.Count; i++) {
                pa = polygonAreas [i];
                if (lodFrag != pa.lod * 1000 + pa.fragment) {
                    if (i >0) info += "]\n";
                    info += string.Format ("Polygon [{0}], hash {1}:\n", pa.id, pa.hash);
                    info += string.Format ("  snapshot {0}, LOD {1}, frag {2},\n", pa.snapshotId, pa.lod, pa.fragment);
                    info += string.Format ("  scale {0}, fragOffset {1}:\n", pa.scale, pa.fragmentOffset);
                    info += string.Format ("  resolutions [{0}", pa.resolution);
                    lodFrag = pa.lod * 1000 + pa.fragment;
                } else {
                    info += ", " + pa.resolution;
                }
            }
            return info;
        }
        #endregion
    }
}