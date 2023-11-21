using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Rendering;

using Broccoli.Base;
using Broccoli.Pipe;
using Broccoli.Model;
using Broccoli.Builder;
using Broccoli.Generator;
using Broccoli.Manager;
using Broccoli.Utils;

namespace Broccoli.Factory
{
    using Pipeline = Broccoli.Pipe.Pipeline;
    /// <summary>
    /// Factory used to generate snapshots and variations related outputs.
    /// ProcessSnapshotPolygons > GenerateSnapshotPolygonsPerLOD: analyzes and creates mesh data (polygons) for each snapshot (snapshot).
    /// </summary>
    public class SproutSubfactory {
        #region Vars
        /// <summary>
        /// Internal TreeFactory instance to create branches. 
        /// It must be provided from a parent TreeFactory when initializing this subfactory.
        /// </summary>
        public TreeFactory treeFactory = null;
        /// <summary>
        /// Factory scale to override every pipeline loaded.
        /// All the exposed factory values will be multiplied by this scale and displayed in meters.
        /// The generated mesh will have scaled vertex positions.
        /// </summary>
        public float factoryScale = 1f;
        /// <summary>
        /// Polygon area builder.
        /// </summary>
        public PolygonAreaBuilder polygonBuilder = new PolygonAreaBuilder ();
        /// <summary>
        /// Sprout composite manager.
        /// </summary>
        public SproutCompositeManager sproutCompositeManager = new SproutCompositeManager ();
        /// <summary>
        /// Simplyfies the convex hull on the branch segments.
        /// </summary>
        public bool simplifyHullEnabled = true;
        /// <summary>
        /// Branch descriptor collection to handle values.
        /// </summary>
        BranchDescriptorCollection branchDescriptorCollection = null;
        /// <summary>
        /// Selected snapshot index.
        /// </summary>
        public int snapshotIndex = -1;
        /// <summary>
        /// Selected variation descriptor index.
        /// </summary>
        public int variationIndex = -1;
        /// <summary>
        /// Saves the branch structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> branchLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout A structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutALevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout B structure levels on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> sproutBLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the crown structure level on the loaded pipeline.
        /// </summary>
        List<StructureGenerator.StructureLevel> crownLevels = new List<StructureGenerator.StructureLevel> ();
        /// <summary>
        /// Saves the sprout mesh instances representing sprout groups.
        /// </summary>
        List<SproutMesh> sproutMeshes = new List<SproutMesh> ();
        /// <summary>
        /// Branch mapper element to set branch textures.
        /// </summary>
        BranchMapperElement branchMapperElement = null;
        /// <summary>
        /// Branch girth element to set branch girth.
        /// </summary>
        GirthTransformElement girthTransformElement = null;
        /// <summary>
        /// Sprout mapper element to set sprout textures.
        /// </summary>
        SproutMapperElement sproutMapperElement = null;
        /// <summary>
        /// Branch bender element to set branch noise.
        /// </summary>
        BranchBenderElement branchBenderElement = null;
        /// <summary>
        /// Number of branch levels available on the pipeline.
        /// </summary>
        /// <value>Count of branch levels.</value>
        public int branchLevelCount { get; private set; }
        /// <summary>
        /// Number of sprout levels available on the pipeline.
        /// </summary>
        /// <value>Count of sprout levels.</value>
        public int sproutLevelCount { get; private set; }
        /// <summary>
        /// Enum describing the possible materials to apply to a preview.
        /// </summary>
        public enum MaterialMode {
            Composite,
            Albedo,
            Normals,
            Extras,
            Subsurface,
            Mask,
            Thickness
        }
        public Broccoli.Model.BroccoTree snapshotTree = null;
        public Mesh snapshotTreeMesh = null;
        public static Dictionary<int, SnapshotProcessor> _snapshotProcessors = 
            new Dictionary<int, SnapshotProcessor> ();
        /// <summary>
        /// Keeps a reference to a governing bounds of a fragment texture used to calculate UVs on subsequent fragments.
        /// </summary>
        /// <typeparam name="Hash128">Fragment hash.</typeparam>
        /// <typeparam name="Bounds">First bound created for the fragment.</typeparam>
        private Dictionary<Hash128, Bounds> _refFragBounds = new Dictionary<Hash128, Bounds> ();
        /// <summary>
        /// Set to true while exporting to prefab (generate normal textures with non linear gamma).
        /// </summary>
        public bool isPrefabExport = false;
        #endregion

        #region Texture Vars
        public TextureManager textureManager;
        #endregion

        #region Constructors
        /// <summary>
		/// Static constructor. Registers processors for this factory.
		/// </summary>
		static SproutSubfactory () {
			_snapshotProcessors.Clear ();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				foreach (Type type in assembly.GetTypes()) {
                    SnapshotProcessorAttribute processorAttribute = type.GetCustomAttribute<SnapshotProcessorAttribute> ();
					if (processorAttribute != null) {
						SnapshotProcessor instance = (SnapshotProcessor)Activator.CreateInstance (type);
                        if (!_snapshotProcessors.ContainsKey (processorAttribute.id)) {
						    _snapshotProcessors.Add (processorAttribute.id, instance);
                        }
					}
				}
			}
		}
        #endregion

        #region Factory Initialization and Termination
        /// <summary>
        /// Initializes the subfactory instance.
        /// </summary>
        /// <param name="treeFactory">TreeFactory instance to use to produce branches.</param>
        public void Init (TreeFactory treeFactory) {
            this.treeFactory = treeFactory;
            if (textureManager != null) {
                textureManager.Clear ();
            }
            textureManager = new TextureManager ();
        }
        /// <summary>
        /// Check if there is a valid tree factory assigned to this sprout factory.
        /// </summary>
        /// <returns>True is there is a valid TreeFactory instance.</returns>
        public bool HasValidTreeFactory () {
            return treeFactory != null;
        }
        /// <summary>
        /// Clears data from this instance.
        /// </summary>
        public void Clear () {
            treeFactory = null;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            crownLevels.Clear ();
            sproutMeshes.Clear ();
            branchMapperElement = null;
            girthTransformElement = null;
            sproutMapperElement = null;
            branchBenderElement = null;
            textureManager.Clear ();
            snapshotTree= null;
            snapshotTreeMesh = null;
        }
        #endregion

        #region Pipeline Load and Analysis
        /// <summary>
        /// Loads a Broccoli pipeline to process branches.
        /// The branch is required to have from 1 to 3 hierarchy levels of branch nodes.
        /// </summary>
        /// <param name="pipeline">Pipeline to load on this subfactory.</param>
        /// <param name="pathToAsset">Path to the asset.</param>
        public void LoadPipeline (Pipeline pipeline, BranchDescriptorCollection branchDescriptorCollection, string pathToAsset) {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
                treeFactory.LoadPipeline (pipeline.Clone (), pathToAsset, true , true);
                this.branchDescriptorCollection = branchDescriptorCollection;
            }
        }
        /// <summary>
        /// Analyzes the loaded pipeline to index the branch and sprout levels to modify using the
        /// BranchDescriptor instance values.
        /// </summary>
        void AnalyzePipeline (BranchDescriptor snapshot = null) {
            branchLevelCount = 0;
            sproutLevelCount = 0;
            branchLevels.Clear ();
            sproutALevels.Clear ();
            sproutBLevels.Clear ();
            crownLevels.Clear ();
            sproutMeshes.Clear ();

            // Set working pipeline.
            if (snapshot != null && !string.IsNullOrEmpty (snapshot.snapshotType)) {
                treeFactory.localPipeline.SetPreferredSrcElement (snapshot.snapshotType);
            } else {
                treeFactory.localPipeline.SetPreferredSrcElement ("Main");
            }

            // t structures for branches and sprouts.
            StructureGeneratorElement structureGeneratorElement = (StructureGeneratorElement)treeFactory.localPipeline.root;
            AnalyzePipelineStructure (structureGeneratorElement.rootStructureLevel);

            // Get sprout meshes.
            SproutMeshGeneratorElement sproutMeshGeneratorElement = 
                (SproutMeshGeneratorElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.SproutMeshGenerator);
            if (sproutMeshGeneratorElement != null) {
                for (int i = 0; i < sproutMeshGeneratorElement.sproutMeshes.Count; i++) {
                    sproutMeshes.Add (sproutMeshGeneratorElement.sproutMeshes [i]);
                }
            }

            // Get the branch mapper to set textures for branches.
            branchMapperElement = 
                (BranchMapperElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.BranchMapper);
            girthTransformElement = 
                (GirthTransformElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.GirthTransform);
            sproutMapperElement = 
                (SproutMapperElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.SproutMapper);
            branchBenderElement = 
                (BranchBenderElement)treeFactory.localPipeline.root.GetDownstreamElement (PipelineElement.ClassType.BranchBender);
        }
        void OnDirectionalBending (BroccoTree tree, BranchBenderElement branchBenderElement) {
            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];
            BranchDescriptor.BranchLevelDescriptor branchLevelDesc;
            int branchLevel;
            List<BroccoTree.Branch> allBranches = tree.GetDescendantBranches ();
            for (int i = 0; i < allBranches.Count; i++) {
                branchLevel = allBranches [i].GetLevel();
                if (branchLevel >= 1) {
                    branchLevelDesc = snapshot.branchLevelDescriptors [branchLevel];
                    Vector3 dir = allBranches [i].GetDirectionAtPosition (0f);
                    dir.x = UnityEngine.Random.Range (branchLevelDesc.minPlaneAlignAtBase, branchLevelDesc.maxPlaneAlignAtBase);
                    allBranches [i].ApplyDirectionalLength (dir, allBranches [i].length);
                }
            }
        }
        void AnalyzePipelineStructure (StructureGenerator.StructureLevel structureLevel) {
            if (!structureLevel.isSprout) {
                // Add branch structure level.
                branchLevels.Add (structureLevel);
                branchLevelCount++;
                // Add sprout A structure level.
                StructureGenerator.StructureLevel sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (1);
                if (sproutStructureLevel != null) {
                    sproutALevels.Add (sproutStructureLevel);
                    sproutLevelCount++;
                }
                // Add sprout B structure level.
                sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (2);
                if (sproutStructureLevel != null) {
                    sproutBLevels.Add (sproutStructureLevel);
                }
                // Add crown structure level.
                sproutStructureLevel = structureLevel.GetSproutStructureLevelByGroupId (3);
                if (sproutStructureLevel != null) {
                    crownLevels.Add (sproutStructureLevel);
                }
                // Send the next banch structure level to analysis if found.
                StructureGenerator.StructureLevel branchStructureLevel = 
                    structureLevel.GetFirstBranchStructureLevel ();
                if (branchStructureLevel != null) {
                    AnalyzePipelineStructure (branchStructureLevel);                    
                }
            }
        }
        public void UnloadPipeline () {
            if (treeFactory != null) {
                treeFactory.UnloadAndClearPipeline ();
            }
        }
        #endregion

        #region Pipeline Reflection
        /// <summary>
        /// Reflects the selected SNAPSHOT to a PIPELINE.
        /// </summary>
        public void SnapshotCollectionToPipeline () {
            if (snapshotIndex < 0 || branchDescriptorCollection.snapshots.Count == 0) return;

            BranchDescriptor.BranchLevelDescriptor branchLD;
            StructureGenerator.StructureLevel branchSL;
            BranchDescriptor.SproutLevelDescriptor sproutALD;
            StructureGenerator.StructureLevel sproutASL;
            BranchDescriptor.SproutLevelDescriptor sproutBLD;
            StructureGenerator.StructureLevel sproutBSL;

            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];

            AnalyzePipeline (snapshot);
            ProcessTextures ();

            // Set seed.
            treeFactory.localPipeline.seed = snapshot.seed;

            // Set Factory Scale to 1/10.
            treeFactory.treeFactoryPreferences.factoryScale = factoryScale;

            SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);

            // Update branch girth.
            if (girthTransformElement != null) {
                girthTransformElement.minGirthAtBase = snapshot.girthAtBase;
                girthTransformElement.maxGirthAtBase = snapshot.girthAtBase;
                girthTransformElement.minGirthAtTop = snapshot.girthAtTop;
                girthTransformElement.maxGirthAtTop = snapshot.girthAtTop;
            }
            // Update branch noise.
            if (branchBenderElement) {
                branchBenderElement.noiseResolution = snapshot.noiseResolution * snapshotSettings.noiseResolution;
                branchBenderElement.noiseStrength = snapshotSettings.noiseStrength;
                branchBenderElement.noiseAtBase = snapshot.noiseAtBase;
                branchBenderElement.noiseAtTop = snapshot.noiseAtTop;
                branchBenderElement.noiseScaleAtBase = snapshot.noiseScaleAtBase;
                branchBenderElement.noiseScaleAtTop = snapshot.noiseScaleAtTop;
                branchBenderElement.onDirectionalBending -= OnDirectionalBending;
                if (snapshotSettings.level1PlaneAlignmentEnabled) {
                    branchBenderElement.onDirectionalBending += OnDirectionalBending;
                }
            }
            // Update snapshot active levels.
            for (int i = 0; i < branchLevels.Count; i++) {
                if (i <= snapshot.activeLevels) {
                    branchLevels [i].enabled = true;
                } else {
                    branchLevels [i].enabled = false;
                }
            }
            // Update branch level descriptors.
            for (int i = 0; i < snapshot.branchLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    branchLD = snapshot.branchLevelDescriptors [i];
                    branchSL = branchLevels [i];
                    // Pass Values.
                    branchSL.minFrequency = branchLD.minFrequency;
                    branchSL.maxFrequency = branchLD.maxFrequency;
                    branchSL.minRange = branchLD.minRange;
                    branchSL.maxRange = branchLD.maxRange;
                    if (branchSL.minRange > 0f || branchSL.maxRange < 1f) {
                        branchSL.actionRangeEnabled = true;
                    } else {
                        branchSL.actionRangeEnabled = false;
                    }
                    branchSL.distributionCurve = branchLD.distributionCurve;
                    if (branchDescriptorCollection.descriptorImplId == 0) {
                        branchSL.radius = 0;
                    } else {
                        branchSL.radius = branchLD.radius;
                    }
                    branchSL.minLengthAtBase = branchLD.minLengthAtBase;
                    branchSL.maxLengthAtBase = branchLD.maxLengthAtBase;
                    branchSL.minLengthAtTop = branchLD.minLengthAtTop;
                    branchSL.maxLengthAtTop = branchLD.maxLengthAtTop;
                    branchSL.lengthCurve = branchLD.lengthCurve;
                    branchSL.distributionSpacingVariance = branchLD.spacingVariance;
                    branchSL.minParallelAlignAtBase = branchLD.minParallelAlignAtBase;
                    branchSL.maxParallelAlignAtBase = branchLD.maxParallelAlignAtBase;
                    branchSL.minParallelAlignAtTop = branchLD.minParallelAlignAtTop;
                    branchSL.maxParallelAlignAtTop = branchLD.maxParallelAlignAtTop;
                    branchSL.minGravityAlignAtBase = branchLD.minGravityAlignAtBase;
                    branchSL.maxGravityAlignAtBase = branchLD.maxGravityAlignAtBase;
                    branchSL.minGravityAlignAtTop = branchLD.minGravityAlignAtTop;
                    branchSL.maxGravityAlignAtTop = branchLD.maxGravityAlignAtTop;
                }
            }
            // Update branch mapping textures.
            if (branchMapperElement != null) {
                branchMapperElement.mainTexture = branchDescriptorCollection.branchAlbedoTexture;
                branchMapperElement.normalTexture = branchDescriptorCollection.branchNormalTexture;
                branchMapperElement.mappingYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
            }
            // Update sprout A level descriptors.
            for (int i = 0; i < snapshot.sproutALevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutALD = snapshot.sproutALevelDescriptors [i];
                    sproutASL = sproutALevels [i];
                    // Pass Values.
                    sproutASL.enabled = sproutALD.isEnabled;
                    sproutASL.minFrequency = sproutALD.minFrequency;
                    sproutASL.maxFrequency = sproutALD.maxFrequency;
                    sproutASL.minParallelAlignAtBase = sproutALD.minParallelAlignAtBase;
                    sproutASL.maxParallelAlignAtBase = sproutALD.maxParallelAlignAtBase;
                    sproutASL.minParallelAlignAtTop = sproutALD.minParallelAlignAtTop;
                    sproutASL.maxParallelAlignAtTop = sproutALD.maxParallelAlignAtTop;
                    sproutASL.minGravityAlignAtBase = sproutALD.minGravityAlignAtBase;
                    sproutASL.maxGravityAlignAtBase = sproutALD.maxGravityAlignAtBase;
                    sproutASL.minGravityAlignAtTop = sproutALD.minGravityAlignAtTop;
                    sproutASL.maxGravityAlignAtTop = sproutALD.maxGravityAlignAtTop;
                    sproutASL.flipSproutAlign = snapshot.sproutAFlipAlign;
                    sproutASL.normalSproutRandomness = snapshot.sproutANormalRandomness;
                    sproutASL.actionRangeEnabled = true;
                    sproutASL.minRange = sproutALD.minRange;
                    sproutASL.maxRange = sproutALD.maxRange;
                    sproutASL.distribution = (StructureGenerator.StructureLevel.Distribution)sproutALD.distribution;
                    if (sproutASL.distribution == StructureGenerator.StructureLevel.Distribution.Alternative) {
                        sproutASL.minTwirl = 0f;
                        sproutASL.maxTwirl = 0f;
                        sproutASL.twirlOffset = 0.5f;
                    } else {
                        sproutASL.minTwirl = 0.5f;
                        sproutASL.maxTwirl = 0.5f;
                        sproutASL.twirlOffset = 0.75f;
                    }
                    sproutASL.distributionCurve = sproutALD.distributionCurve;
                    sproutASL.distributionSpacingVariance = sproutALD.spacingVariance;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 0) {
                sproutMeshes [0].width = snapshot.sproutASize;
                sproutMeshes [0].scaleAtBase = snapshot.sproutAScaleAtBase;
                sproutMeshes [0].scaleAtTop = snapshot.sproutAScaleAtTop;
                sproutMeshes [0].scaleVariance = snapshot.sproutAScaleVariance;
                sproutMeshes [0].scaleMode = snapshot.sproutAScaleMode;
                sproutMeshes [0].gravityBendingAtBase = snapshot.sproutABendingAtBase;
                sproutMeshes [0].gravityBendingAtTop = snapshot.sproutABendingAtTop;
                sproutMeshes [0].sideGravityBendingAtBase = snapshot.sproutASideBendingAtBase;
                sproutMeshes [0].sideGravityBendingAtTop = snapshot.sproutASideBendingAtTop;
            }
            // Update sprout mapping textures.
            if (sproutMapperElement != null) {
                sproutMapperElement.sproutMaps [0].colorVarianceMode = SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [0].minColorShade = branchDescriptorCollection.sproutStyleA.minColorShade;
                sproutMapperElement.sproutMaps [0].maxColorShade = branchDescriptorCollection.sproutStyleA.maxColorShade;
                sproutMapperElement.sproutMaps [0].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [0].colorTint = branchDescriptorCollection.sproutStyleA.colorTint;
                sproutMapperElement.sproutMaps [0].minColorTint = branchDescriptorCollection.sproutStyleA.minColorTint;
                sproutMapperElement.sproutMaps [0].maxColorTint = branchDescriptorCollection.sproutStyleA.maxColorTint;
                sproutMapperElement.sproutMaps [0].metallic = branchDescriptorCollection.sproutStyleA.metallic;
                sproutMapperElement.sproutMaps [0].glossiness = branchDescriptorCollection.sproutStyleA.glossiness;
                sproutMapperElement.sproutMaps [0].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleA.subsurface - 0.5f);
                sproutMapperElement.sproutMaps [0].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutStyleA.sproutMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutStyleA.sproutMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (0, i);
                    sproutMapperElement.sproutMaps [0].sproutAreas.Add (sma);
                }
            }
            // Update sprout B level descriptors.
            for (int i = 0; i < snapshot.sproutBLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutBLD = snapshot.sproutBLevelDescriptors [i];
                    sproutBSL = sproutBLevels [i];
                    // Pass Values.
                    sproutBSL.enabled = sproutBLD.isEnabled;
                    sproutBSL.minFrequency = sproutBLD.minFrequency;
                    sproutBSL.maxFrequency = sproutBLD.maxFrequency;
                    sproutBSL.minParallelAlignAtBase = sproutBLD.minParallelAlignAtBase;
                    sproutBSL.maxParallelAlignAtBase = sproutBLD.maxParallelAlignAtBase;
                    sproutBSL.minParallelAlignAtTop = sproutBLD.minParallelAlignAtTop;
                    sproutBSL.maxParallelAlignAtTop = sproutBLD.maxParallelAlignAtTop;
                    sproutBSL.minGravityAlignAtBase = sproutBLD.minGravityAlignAtBase;
                    sproutBSL.maxGravityAlignAtBase = sproutBLD.maxGravityAlignAtBase;
                    sproutBSL.minGravityAlignAtTop = sproutBLD.minGravityAlignAtTop;
                    sproutBSL.maxGravityAlignAtTop = sproutBLD.maxGravityAlignAtTop;
                    sproutBSL.flipSproutAlign = snapshot.sproutBFlipAlign;
                    sproutBSL.normalSproutRandomness = snapshot.sproutBNormalRandomness;
                    sproutBSL.actionRangeEnabled = true;
                    sproutBSL.minRange = sproutBLD.minRange;
                    sproutBSL.maxRange = sproutBLD.maxRange;
                    sproutBSL.distribution = (StructureGenerator.StructureLevel.Distribution)sproutBLD.distribution;
                    if (sproutBSL.distribution == StructureGenerator.StructureLevel.Distribution.Alternative) {
                        sproutBSL.minTwirl = 0f;
                        sproutBSL.maxTwirl = 0f;
                        sproutBSL.twirlOffset = 0.5f;
                    } else {
                        sproutBSL.minTwirl = 0.5f;
                        sproutBSL.maxTwirl = 0.5f;
                        sproutBSL.twirlOffset = 0.75f;
                    }
                    sproutBSL.distributionCurve = sproutBLD.distributionCurve;
                    sproutBSL.distributionSpacingVariance = sproutBLD.spacingVariance;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 1) {
                sproutMeshes [1].width = snapshot.sproutBSize;
                sproutMeshes [1].scaleAtBase = snapshot.sproutBScaleAtBase;
                sproutMeshes [1].scaleAtTop = snapshot.sproutBScaleAtTop;
                sproutMeshes [1].scaleVariance = snapshot.sproutBScaleVariance;
                sproutMeshes [1].scaleMode = snapshot.sproutBScaleMode;
                sproutMeshes [1].gravityBendingAtBase = snapshot.sproutBBendingAtBase;
                sproutMeshes [1].gravityBendingAtTop = snapshot.sproutBBendingAtTop;
                sproutMeshes [1].sideGravityBendingAtBase = snapshot.sproutBSideBendingAtBase;
                sproutMeshes [1].sideGravityBendingAtTop = snapshot.sproutBSideBendingAtTop;
            }
            // Update sprout mapping textures.
            if (sproutMapperElement != null && sproutMapperElement.sproutMaps.Count > 1) {
                sproutMapperElement.sproutMaps [1].colorVarianceMode =  SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [1].minColorShade = branchDescriptorCollection.sproutStyleB.minColorShade;
                sproutMapperElement.sproutMaps [1].maxColorShade = branchDescriptorCollection.sproutStyleB.maxColorShade;
                sproutMapperElement.sproutMaps [1].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [1].colorTint = branchDescriptorCollection.sproutStyleB.colorTint;
                sproutMapperElement.sproutMaps [1].minColorTint = branchDescriptorCollection.sproutStyleB.minColorTint;
                sproutMapperElement.sproutMaps [1].maxColorTint = branchDescriptorCollection.sproutStyleB.maxColorTint;
                sproutMapperElement.sproutMaps [1].metallic = branchDescriptorCollection.sproutStyleB.metallic;
                sproutMapperElement.sproutMaps [1].glossiness = branchDescriptorCollection.sproutStyleB.glossiness;
                sproutMapperElement.sproutMaps [1].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleB.subsurface - 0.5f);
                sproutMapperElement.sproutMaps [1].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutStyleB.sproutMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutStyleB.sproutMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (1, i);
                    sproutMapperElement.sproutMaps [1].sproutAreas.Add (sma);
                }
            }

            // Update crown.
            if (snapshotSettings.hasCrown && sproutMeshes.Count > 2) {
                sproutMeshes[2].depth = snapshot.crownDepth;
                sproutMeshes[2].width = snapshot.crownSize;
                sproutMeshes[2].scaleAtBase = snapshot.crownScaleAtBase;
                sproutMeshes[2].scaleAtTop = snapshot.crownScaleAtTop;
                sproutMeshes[2].scaleVariance = snapshot.crownScaleVariance;
                // Update crown mapping.
                if (sproutMapperElement != null && sproutMapperElement.sproutMaps.Count > 2) {
                    sproutMapperElement.sproutMaps [2].colorVarianceMode =  SproutMap.ColorVarianceMode.Shades;
                    sproutMapperElement.sproutMaps [2].minColorShade = branchDescriptorCollection.sproutStyleCrown.minColorShade;
                    sproutMapperElement.sproutMaps [2].maxColorShade = branchDescriptorCollection.sproutStyleCrown.maxColorShade;
                    sproutMapperElement.sproutMaps [2].colorTintEnabled = true;
                    sproutMapperElement.sproutMaps [2].colorTint = branchDescriptorCollection.sproutStyleCrown.colorTint;
                    sproutMapperElement.sproutMaps [2].minColorTint = branchDescriptorCollection.sproutStyleCrown.minColorTint;
                    sproutMapperElement.sproutMaps [2].maxColorTint = branchDescriptorCollection.sproutStyleCrown.maxColorTint;
                    sproutMapperElement.sproutMaps [2].metallic = branchDescriptorCollection.sproutStyleCrown.metallic;
                    sproutMapperElement.sproutMaps [2].glossiness = branchDescriptorCollection.sproutStyleCrown.glossiness;
                    sproutMapperElement.sproutMaps [2].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleCrown.subsurface - 0.5f);
                    sproutMapperElement.sproutMaps [2].sproutAreas.Clear ();
                    for (int i = 0; i < branchDescriptorCollection.sproutStyleCrown.sproutMapAreas.Count; i++) {
                        SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [i].Clone ();
                        sma.texture = GetSproutTexture (2, i);
                        sproutMapperElement.sproutMaps [2].sproutAreas.Add (sma);
                    }
                }
            }
            StructureGenerator.StructureLevel sl;
            for (int i = 0; i < crownLevels.Count; i++) {
                sl = crownLevels [i];
                sl.enabled = false;
                StructureGenerator.StructureLevel siblingBranchStructureLevel = 
                    sl.parentStructureLevel.GetFirstBranchStructureLevel ();
                if (siblingBranchStructureLevel != null) {
                    //siblingBranchStructureLevel.actionRangeEnabled = false;
                    siblingBranchStructureLevel.onBeforeGenerateStructures = null;
                    if (snapshotSettings.hasCrown && i == snapshot.activeLevels - 1 && snapshot.crownEnabled) {
                        sl.minParallelAlignAtBase = snapshot.sproutCrownLevelDescriptor.minParallelAlignAtBase;
                        sl.maxParallelAlignAtBase = snapshot.sproutCrownLevelDescriptor.maxParallelAlignAtBase;
                        sl.minParallelAlignAtTop = snapshot.sproutCrownLevelDescriptor.minParallelAlignAtTop;
                        sl.maxParallelAlignAtTop = snapshot.sproutCrownLevelDescriptor.maxParallelAlignAtTop;
                        siblingBranchStructureLevel.onBeforeGenerateStructures = OnBeforeGenerateCrownedLevel;
                        siblingBranchStructureLevel.obj = snapshot;
                    }
                }
            }
        }
        private void OnBeforeGenerateCrownedLevel (
            StructureGenerator.StructureLevel structureLevel, 
            StructureGenerator.Structure parentStructure)
        {
            StructureGenerator.StructureLevel crownStructureLevel = structureLevel.parentStructureLevel.GetSproutStructureLevelByGroupId (3);
            float randomVal = UnityEngine.Random.Range (0f, 1f);
            BranchDescriptor snapshot = (BranchDescriptor)structureLevel.obj;
            if (randomVal <= snapshot.crownProbability) {
                crownStructureLevel.enabled = true;
                structureLevel.actionRangeEnabled = true;
                crownStructureLevel.actionRangeEnabled = true;
                float range = UnityEngine.Random.Range (snapshot.crownMinRange, snapshot.crownMaxRange);
                crownStructureLevel.minRange = 1f - range;
                structureLevel.maxRange = 1f - range;
                if (structureLevel.minRange > structureLevel.maxRange) {
                    structureLevel.minRange = structureLevel.maxRange;
                }
                crownStructureLevel.minFrequency = snapshot.crownMinFrequency;
                crownStructureLevel.maxFrequency = snapshot.crownMaxFrequency;
            } else {
                crownStructureLevel.enabled = false;
                /*
                structureLevel.actionRangeEnabled = false;
                structureLevel.maxRange = 1f;
                */
            }
        }
        /// <summary>
        /// Loads values from a PIPELINE to a SNAPSHOT.
        /// </summary>
        /// <param name="snapshot"></param>
        public void PipelineToSnapshot (BranchDescriptor snapshot) {

            BranchDescriptor.BranchLevelDescriptor branchLD;
            StructureGenerator.StructureLevel branchSL;
            BranchDescriptor.SproutLevelDescriptor sproutALD;
            StructureGenerator.StructureLevel sproutASL;
            BranchDescriptor.SproutLevelDescriptor sproutBLD;
            StructureGenerator.StructureLevel sproutBSL;

            // Setting for the snapshot.
            SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);
            snapshot.activeLevels = snapshotSettings.defaultActiveLevels;
            snapshot.processorId = snapshotSettings.processorId;

            AnalyzePipeline (snapshot);

            // Update branch girth.
            if (girthTransformElement != null) {
                snapshot.girthAtBase = girthTransformElement.minGirthAtBase;
                snapshot.girthAtBase = girthTransformElement.maxGirthAtBase;
                snapshot.girthAtTop = girthTransformElement.minGirthAtTop;
                snapshot.girthAtTop = girthTransformElement.maxGirthAtTop;
            }
            // Update branch noise.
            if (branchBenderElement) {
                snapshot.noiseResolution = branchBenderElement.noiseResolution;
                snapshot.noiseAtBase = branchBenderElement.noiseAtBase;
                snapshot.noiseAtTop = branchBenderElement.noiseAtTop;
                snapshot.noiseScaleAtBase = branchBenderElement.noiseScaleAtBase;
                snapshot.noiseScaleAtTop = branchBenderElement.noiseScaleAtTop;
            }
            /*
            // Update snapshot active levels.
            for (int i = 0; i < branchLevels.Count; i++) {
                if (i <= branchDescriptor.activeLevels) {
                    branchLevels [i].enabled = true;
                } else {
                    branchLevels [i].enabled = false;
                }
            }
            */
            // Update branch level descriptors.
            for (int i = 0; i < snapshot.branchLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    branchLD = snapshot.branchLevelDescriptors [i];
                    branchSL = branchLevels [i];
                    // Pass Values.
                    branchLD.minFrequency = branchSL.minFrequency;
                    branchLD.maxFrequency = branchSL.maxFrequency;
                    branchLD.minRange = branchSL.minRange;
                    branchLD.maxRange = branchSL.maxRange;
                    branchLD.distributionCurve = branchSL.distributionCurve;
                    if (branchDescriptorCollection.descriptorImplId == 0) {
                        branchLD.radius = 0;
                    } else {
                        branchLD.radius = branchSL.radius;
                    }
                    branchLD.minLengthAtBase        = branchSL.minLengthAtBase;
                    branchLD.maxLengthAtBase        = branchSL.maxLengthAtBase;
                    branchLD.minLengthAtTop         = branchSL.minLengthAtTop;
                    branchLD.maxLengthAtTop         = branchSL.maxLengthAtTop;
                    branchLD.lengthCurve            = branchSL.lengthCurve;
                    branchLD.spacingVariance         = branchSL.distributionSpacingVariance;
                    branchLD.minParallelAlignAtBase = branchSL.minParallelAlignAtBase;
                    branchLD.maxParallelAlignAtBase = branchSL.maxParallelAlignAtBase;
                    branchLD.minParallelAlignAtTop  = branchSL.minParallelAlignAtTop;
                    branchLD.maxParallelAlignAtTop  = branchSL.maxParallelAlignAtTop;
                    branchLD.minGravityAlignAtBase  = branchSL.minGravityAlignAtBase;
                    branchLD.maxGravityAlignAtBase  = branchSL.maxGravityAlignAtBase;
                    branchLD.minGravityAlignAtTop   = branchSL.minGravityAlignAtTop;
                    branchLD.maxGravityAlignAtTop   = branchSL.maxGravityAlignAtTop;
                }
            }
            /*
            // Update branch mapping textures.
            if (branchMapperElement != null) {
                branchMapperElement.mainTexture = branchDescriptorCollection.branchAlbedoTexture;
                branchMapperElement.normalTexture = branchDescriptorCollection.branchNormalTexture;
                branchMapperElement.mappingYDisplacement = branchDescriptorCollection.branchTextureYDisplacement;
            }
            */
            // Update sprout A level descriptors.
            for (int i = 0; i < snapshot.sproutALevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutALD = snapshot.sproutALevelDescriptors [i];
                    sproutASL = sproutALevels [i];
                    // Pass Values.
                    sproutALD.isEnabled = sproutASL.enabled;
                    sproutALD.minFrequency = sproutASL.minFrequency;
                    sproutALD.maxFrequency = sproutASL.maxFrequency;
                    sproutALD.minParallelAlignAtBase = sproutASL.minParallelAlignAtBase;
                    sproutALD.maxParallelAlignAtBase = sproutASL.maxParallelAlignAtBase;
                    sproutALD.minParallelAlignAtTop = sproutASL.minParallelAlignAtTop;
                    sproutALD.maxParallelAlignAtTop = sproutASL.maxParallelAlignAtTop;
                    sproutALD.minGravityAlignAtBase = sproutASL.minGravityAlignAtBase;
                    sproutALD.maxGravityAlignAtBase = sproutASL.maxGravityAlignAtBase;
                    sproutALD.minGravityAlignAtTop = sproutASL.minGravityAlignAtTop;
                    sproutALD.maxGravityAlignAtTop = sproutASL.maxGravityAlignAtTop;
                    snapshot.sproutAFlipAlign = sproutASL.flipSproutAlign;
                    snapshot.sproutANormalRandomness = sproutASL.normalSproutRandomness;
                    //sproutALD.actionRangeEnabled = true;
                    sproutALD.minRange = sproutASL.minRange;
                    sproutALD.maxRange = sproutASL.maxRange;
                    sproutALD.distribution = (BranchDescriptor.SproutLevelDescriptor.Distribution)sproutASL.distribution;
                    sproutALD.distributionCurve = sproutASL.distributionCurve;
                    sproutALD.spacingVariance = sproutASL.distributionSpacingVariance;
                }
            }
            // Update sprout A properties.
            if (sproutMeshes.Count > 0) {
                snapshot.sproutASize = sproutMeshes [0].width;
                snapshot.sproutAScaleAtBase = sproutMeshes [0].scaleAtBase;
                snapshot.sproutAScaleAtTop = sproutMeshes [0].scaleAtTop;
                snapshot.sproutAScaleVariance = sproutMeshes [0].scaleVariance;
                snapshot.sproutAScaleMode = sproutMeshes [0].scaleMode;
                snapshot.sproutABendingAtBase = sproutMeshes [0].gravityBendingAtBase;
                snapshot.sproutABendingAtTop = sproutMeshes [0].gravityBendingAtTop;
                snapshot.sproutASideBendingAtBase = sproutMeshes [0].sideGravityBendingAtBase;
                snapshot.sproutASideBendingAtTop = sproutMeshes [0].sideGravityBendingAtTop;
            }
            /*
            // Update sprout mapping textures.
            if (sproutMapperElement != null) {
                sproutMapperElement.sproutMaps [0].colorVarianceMode = SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [0].minColorShade = branchDescriptorCollection.sproutStyleA.minColorShade;
                sproutMapperElement.sproutMaps [0].maxColorShade = branchDescriptorCollection.sproutStyleA.maxColorShade;
                sproutMapperElement.sproutMaps [0].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [0].colorTint = branchDescriptorCollection.sproutStyleA.colorTint;
                sproutMapperElement.sproutMaps [0].minColorTint = branchDescriptorCollection.sproutStyleA.minColorTint;
                sproutMapperElement.sproutMaps [0].maxColorTint = branchDescriptorCollection.sproutStyleA.maxColorTint;
                sproutMapperElement.sproutMaps [0].metallic = branchDescriptorCollection.sproutStyleA.metallic;
                sproutMapperElement.sproutMaps [0].glossiness = branchDescriptorCollection.sproutStyleA.glossiness;
                sproutMapperElement.sproutMaps [0].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleA.subsurfaceMul - 0.5f);
                sproutMapperElement.sproutMaps [0].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutAMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutAMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (0, i);
                    sproutMapperElement.sproutMaps [0].sproutAreas.Add (sma);
                }
            }
            */

            // Update sprout B level descriptors.
            for (int i = 0; i < snapshot.sproutBLevelDescriptors.Count; i++) {
                if (i < branchLevelCount) {
                    sproutBLD = snapshot.sproutBLevelDescriptors [i];
                    sproutBSL = sproutBLevels [i];
                    // Pass Values.
                    sproutBLD.isEnabled = sproutBSL.enabled;
                    sproutBLD.minFrequency = sproutBSL.minFrequency;
                    sproutBLD.maxFrequency = sproutBSL.maxFrequency;
                    sproutBLD.minParallelAlignAtBase = sproutBSL.minParallelAlignAtBase;
                    sproutBLD.maxParallelAlignAtBase = sproutBSL.maxParallelAlignAtBase;
                    sproutBLD.minParallelAlignAtTop = sproutBSL.minParallelAlignAtTop;
                    sproutBLD.maxParallelAlignAtTop = sproutBSL.maxParallelAlignAtTop;
                    sproutBLD.minGravityAlignAtBase = sproutBSL.minGravityAlignAtBase;
                    sproutBLD.maxGravityAlignAtBase = sproutBSL.maxGravityAlignAtBase;
                    sproutBLD.minGravityAlignAtTop = sproutBSL.minGravityAlignAtTop;
                    sproutBLD.maxGravityAlignAtTop = sproutBSL.maxGravityAlignAtTop;
                    snapshot.sproutBFlipAlign = sproutBSL.flipSproutAlign;
                    snapshot.sproutBNormalRandomness = sproutBSL.normalSproutRandomness;
                    //sproutBLD.actionRangeEnabled = true;
                    sproutBLD.minRange = sproutBSL.minRange;
                    sproutBLD.maxRange = sproutBSL.maxRange;
                    sproutBLD.distribution = (BranchDescriptor.SproutLevelDescriptor.Distribution)sproutBSL.distribution;
                    sproutBLD.distributionCurve = sproutBSL.distributionCurve;
                    sproutBLD.spacingVariance = sproutBSL.distributionSpacingVariance;
                }
            }
            // Update sprout B properties.
            if (sproutMeshes.Count > 1) {
                snapshot.sproutBSize = sproutMeshes [1].width;
                snapshot.sproutBScaleAtBase = sproutMeshes [1].scaleAtBase;
                snapshot.sproutBScaleAtTop = sproutMeshes [1].scaleAtTop;
                snapshot.sproutBScaleVariance = sproutMeshes [1].scaleVariance;
                snapshot.sproutBScaleMode = sproutMeshes [1].scaleMode;
                snapshot.sproutBBendingAtBase = sproutMeshes [1].gravityBendingAtBase;
                snapshot.sproutBBendingAtTop = sproutMeshes [1].gravityBendingAtTop;
                snapshot.sproutBSideBendingAtBase = sproutMeshes [1].sideGravityBendingAtBase;
                snapshot.sproutBSideBendingAtTop = sproutMeshes [1].sideGravityBendingAtTop;
            }
            
            /*
            // Update sprout mapping textures.
            if (sproutMapperElement != null && sproutMapperElement.sproutMaps.Count > 1) {
                sproutMapperElement.sproutMaps [1].colorVarianceMode =  SproutMap.ColorVarianceMode.Shades;
                sproutMapperElement.sproutMaps [1].minColorShade = branchDescriptorCollection.sproutStyleB.minColorShade;
                sproutMapperElement.sproutMaps [1].maxColorShade = branchDescriptorCollection.sproutStyleB.maxColorShade;
                sproutMapperElement.sproutMaps [1].colorTintEnabled = true;
                sproutMapperElement.sproutMaps [1].colorTint = branchDescriptorCollection.sproutStyleB.colorTint;
                sproutMapperElement.sproutMaps [1].minColorTint = branchDescriptorCollection.sproutStyleB.minColorTint;
                sproutMapperElement.sproutMaps [1].maxColorTint = branchDescriptorCollection.sproutStyleB.maxColorTint;
                sproutMapperElement.sproutMaps [1].metallic = branchDescriptorCollection.sproutStyleB.metallic;
                sproutMapperElement.sproutMaps [1].glossiness = branchDescriptorCollection.sproutStyleB.glossiness;
                sproutMapperElement.sproutMaps [1].subsurfaceValue = 0.5f + Mathf.Lerp (-0.4f, 0.4f, branchDescriptorCollection.sproutStyleB.subsurfaceMul - 0.5f);
                sproutMapperElement.sproutMaps [1].sproutAreas.Clear ();
                for (int i = 0; i < branchDescriptorCollection.sproutBMapAreas.Count; i++) {
                    SproutMap.SproutMapArea sma = branchDescriptorCollection.sproutBMapAreas [i].Clone ();
                    sma.texture = GetSproutTexture (1, i);
                    sproutMapperElement.sproutMaps [1].sproutAreas.Add (sma);
                }
            }
            */

            // Update crown properties.
            if (sproutMeshes.Count > 2) {
                snapshot.crownDepth = sproutMeshes[2].depth;
                snapshot.crownSize = sproutMeshes[2].width;
                snapshot.crownScaleAtBase = sproutMeshes[2].scaleAtBase;
                snapshot.crownScaleAtTop = sproutMeshes[2].scaleAtTop;
                snapshot.crownScaleVariance = sproutMeshes[2].scaleVariance;
            }
        }
        #endregion

        #region Snapshot Processing
        /*
        /// <summary>
        /// Regenerates a preview for the loaded snapshot.
        /// </summary>
        /// <param name="materialMode">Materials mode to apply.</param>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessSnapshot (int snapshotIndex, MaterialMode materialMode = MaterialMode.Composite, bool isNewSeed = false) {
            int _branchDescriptorIndex = this.snapshotIndex;
            this.snapshotIndex = snapshotIndex;
            ProcessSnapshot (materialMode, isNewSeed);
            // TODO: selectively reprocess.
            this.snapshotIndex = _branchDescriptorIndex;
        }
        */
        /// <summary>
        /// Regenerates a preview for the loaded snapshot.
        /// </summary>
        /// <param name="materialMode">Materials mode to apply.</param>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessSnapshot (int snapshotIndex, bool hasChanged = false, MaterialMode materialMode = MaterialMode.Composite, bool isNewSeed = false) {
            this.snapshotIndex = snapshotIndex;

            if (snapshotIndex < 0 || snapshotIndex >= branchDescriptorCollection.snapshots.Count) return;

            BranchDescriptor snapshot = branchDescriptorCollection.snapshots [snapshotIndex];
            
            if (!isNewSeed) {
                treeFactory.localPipeline.seed = snapshot.seed;
                treeFactory.ProcessPipelinePreview (null, true, true);
            } else {
                treeFactory.ProcessPipelinePreview ();
                snapshot.seed = treeFactory.localPipeline.seed;
            }
            // Set submesh indexes index.
            SetSnapshotSubmeshIndexes (snapshot, treeFactory);

            if (hasChanged)
                sproutCompositeManager.RemoveSnapshot (snapshot);

            if (GlobalSettings.showSproutLabTreeFactoryInHierarchy) {
                treeFactory.previewTree.obj.SetActive (true);
            } else {
                treeFactory.previewTree.obj.SetActive (false);
            }

            // Get materials.
            MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter> ();
            Material[] compositeMaterials = meshRenderer.sharedMaterials;
            if (materialMode == MaterialMode.Albedo) { // Albedo
                meshRenderer.sharedMaterials = GetAlbedoMaterials (compositeMaterials,
                    branchDescriptorCollection.sproutStyleA,
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    branchDescriptorCollection.branchColorShade,
                    branchDescriptorCollection.branchColorSaturation,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            } else if (materialMode == MaterialMode.Normals) { // Normals
                meshRenderer.sharedMaterials = GetNormalMaterials (compositeMaterials, isPrefabExport?true:false);
            } else if (materialMode == MaterialMode.Extras) { // Extras
                meshRenderer.sharedMaterials = GetExtraMaterials (compositeMaterials,
                    branchDescriptorCollection.sproutStyleA,
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            } else if (materialMode == MaterialMode.Subsurface) { // Subsurface
                meshRenderer.sharedMaterials = GetSubsurfaceMaterials (compositeMaterials,
                branchDescriptorCollection.sproutStyleA, 
                branchDescriptorCollection.sproutStyleB,
                branchDescriptorCollection.sproutStyleCrown,
                branchDescriptorCollection.branchColorSaturation,
                snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            } else if (materialMode == MaterialMode.Composite) { // Composite
                meshRenderer.sharedMaterials = GetCompositeMaterials (compositeMaterials,
                    branchDescriptorCollection.sproutStyleA, 
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            }

            snapshotTree = treeFactory.previewTree;
            snapshotTreeMesh = meshFilter.sharedMesh;
        }
        /// <summary>
        /// Sets the index for submeshes belonging to sprouts on the snapshot.
        /// </summary>
        /// <param name="snapshot">Snapshot to set info about the submesh indexes.</param>
        /// <param name="treeFactory">TreeFactory instance that produced the snapshot.</param>
        private void SetSnapshotSubmeshIndexes (BranchDescriptor snapshot, TreeFactory treeFactory) {
            snapshot.sproutASubmeshIndex = -1;
            snapshot.sproutBSubmeshIndex = -1;
            snapshot.sproutCrownSubmeshIndex = -1;
            snapshot.sproutASubmeshCount = 0;
            snapshot.sproutBSubmeshCount = 0;
            snapshot.sproutCrownSubmeshCount = 0;
            Dictionary<int, MeshManager.MeshData> meshDatas = treeFactory.meshManager.GetMeshDatas ();
            var enumMeshDatas = meshDatas.GetEnumerator ();
            MeshManager.MeshData meshData;
            int submeshIndex = 0;
            while (enumMeshDatas.MoveNext ()) {
                meshData = enumMeshDatas.Current.Value;
                if (meshData.type == MeshManager.MeshData.Type.Sprout) {
                    if (meshData.groupId == 1) {
                        snapshot.sproutASubmeshCount++;
                        if (snapshot.sproutASubmeshIndex == -1)
                            snapshot.sproutASubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 2 && snapshot.sproutBSubmeshIndex == -1) {
                        snapshot.sproutBSubmeshCount++;
                        if (snapshot.sproutBSubmeshIndex == -1)
                            snapshot.sproutBSubmeshIndex = submeshIndex;
                    }
                    if (meshData.groupId == 3 && snapshot.sproutCrownSubmeshIndex == -1) {
                        snapshot.sproutCrownSubmeshCount++;
                        if (snapshot.sproutCrownSubmeshIndex == -1)
                            snapshot.sproutCrownSubmeshIndex = submeshIndex;
                    }
                }
                submeshIndex++;
            }
        }
        /// <summary>
        /// Creates the polygons for a snapshot. It saves their textures to the
        /// snapshotTextures buffer.
        /// It should be called with after ProcessSnapshot to have the mesh, materials and tree
        /// corresponding to the last snapshot processed.
        /// </summary>
        /// <param name="snapshot">Branch Descriptor to process as snapshot.</param>
        public void ProcessSnapshotPolygons (BranchDescriptor snapshot) {
            // Validate the snapshot has been processed.
            if (!sproutCompositeManager.HasSnapshot (snapshot.id)) {
                SnapshotSettings snapshotSettings = SnapshotSettings.Get (snapshot.snapshotType);

                // Generate curve.
                GenerateSnapshotCurve (snapshot, snapshotSettings);

                // Clear reference bounds.
                _refFragBounds.Clear ();

                // Generate polygons per LOD.
                snapshot.polygonAreas.Clear ();
                for (int lodLevel = snapshot.lodCount - 1; lodLevel >= 0; lodLevel--) {
                    GenerateSnapshotPolygonsPerLOD (lodLevel, snapshot);
                }

                sproutCompositeManager.AddSnapshot (snapshot);
                _refFragBounds.Clear ();
            }
        }
        /// <summary>
        /// Gets a snapshot processor given and id.
        /// </summary>
        /// <param name="processorId">Snapshot processor id.</param>
        /// <returns>Processor instance or null if not found.</returns>
        public SnapshotProcessor GetSnapshotProcessor (int processorId) {
            if (_snapshotProcessors.ContainsKey (processorId)) {
                return _snapshotProcessors [processorId];
            }
            return null;
        }
        /// <summary>
        /// Creates the curve as axis for the snapshot of a snapshot.
        /// </summary>
        /// <param name="snapshot">Branch Descriptor to create the curve to.</param>
        public void GenerateSnapshotCurve (BranchDescriptor snapshot, SnapshotSettings snapshotSettings) {
            // Creal existing snapshot curve.
            snapshot.curve.RemoveAllNodes ();

            // Traverse the tree to add nodes.
            BroccoTree.Branch currentBranch = null;
            bool isFirstBranch = true;
            Vector3 offset = Vector3.zero;
            BezierNode node = null;

            if (snapshotTree != null && snapshotTree.branches.Count > 0) {
                currentBranch = snapshotTree.branches [0];
            }
            float noiseScaleAtFirstNode = 1;
            float noiseFactorAtFirstNode = 0;
            float noiseScaleAtLastNode = 1;
            float noiseFactorAtLastNode = 0;
            float noiseOffset = 0;
            float noiseResolution = 4f;
            float noiseStrength = 0.2f;
            bool spareFirstNode = true;

            int steps = 4;
            float step = 1f / (float)steps;
            Vector3 branchFirstP;
            Vector3 branchLastP;
            Vector3 branchP;
            Vector3 nodeP;

            while (currentBranch != null) {
                branchFirstP = currentBranch.GetPointAtPosition (0f);
                branchLastP = currentBranch.GetPointAtPosition (1f);
                if (isFirstBranch) {
                    //node = new BezierNode (currentBranch.GetPointAtPosition (0f));
                    nodeP = currentBranch.GetPointAtPosition (0f);
                    nodeP.x = branchFirstP.x;
                    node = new BezierNode (nodeP);
                    snapshot.curve.AddNode (node, false);
                }
                for (float i = 1; i <= steps; i++) {
                    //node = new BezierNode (currentBranch.GetPointAtPosition (i * step));
                    nodeP = currentBranch.GetPointAtPosition (i * step);
                    branchP = Vector3.Lerp (branchFirstP, branchLastP, i * step);
                    nodeP.x = branchP.x;
                    node = new BezierNode (nodeP);
                    snapshot.curve.AddNode (node, false);
                }
                isFirstBranch = false; 
                if (snapshotSettings.curveLevelLimit > -1 && 
                    currentBranch.GetLevel () == snapshotSettings.curveLevelLimit)
                {
                    currentBranch = null; 
                } else {
                    currentBranch = currentBranch.followUp;
                }
            }
            snapshot.curve.Process ();
            snapshot.curve.SetNoise (noiseFactorAtFirstNode, noiseFactorAtLastNode,
                noiseScaleAtFirstNode, noiseScaleAtLastNode, spareFirstNode, noiseResolution, noiseStrength, noiseOffset);
        }
        /// <summary>
        /// Generates and registers polygon areas for a snapshot at a specific LOD.
        /// </summary>
        /// <param name="lodLevel">Level of detail.</param>
        /// <param name="snapshot">Snapshot of the snapshot.</param>
        public void GenerateSnapshotPolygonsPerLOD (int lodLevel, BranchDescriptor snapshot) {
            // Get Snapshot Processor.
            SnapshotProcessor processor = GetSnapshotProcessor (snapshot.processorId);
            if (processor == null) {
                Debug.LogWarning ("No Snapshot Processor found with id " + snapshot + ", skipping processing.");
                return;
            }
            // Begin usage.
            processor.BeginUsage (snapshotTree, snapshotTreeMesh, factoryScale);
            processor.simplifyHullEnabled = simplifyHullEnabled;
            polygonBuilder.BeginUsage (snapshotTree, snapshotTreeMesh, factoryScale);
            sproutCompositeManager.BeginUsage (snapshotTree, factoryScale);

            List<SnapshotProcessor.Fragment> fragments = 
                processor.GenerateSnapshotFragments (lodLevel, snapshot);

            // Process each fragment to:
            //
            SnapshotProcessor.Fragment fragment;

            Transform parentTransform = treeFactory.previewTree.obj.transform.parent;
            treeFactory.previewTree.obj.transform.parent = null;
            for (int fragIndex = 0; fragIndex < fragments.Count; fragIndex++) {
                //int resolution = 0;
                for (int resolution = 0; resolution <= PolygonArea.MAX_RESOLUTION; resolution ++) {
                    fragment = fragments [fragIndex];

                    // Create polygon per fragment/resolution.
                    PolygonArea polygonArea = new PolygonArea (snapshot.id, fragIndex, lodLevel, resolution);
                    polygonArea.resolution = resolution;
                    polygonArea.fragmentOffset = fragment.offset;

                    Hash128 _hash = Hash128.Compute (fragment.IncludesExcludesToString (snapshot.id));
                    polygonArea.hash = _hash;

                    // Generate hull points.
                    //polygonBuilder.GenerateHullPoints (polygonArea, fragment);
                    processor.GenerateHullPoints (polygonArea, fragment);

                    // Get bounds.
                    //polygonBuilder.ProcessPolygonAreaBounds (polygonArea, fragment);
                    processor.GenerateBounds (polygonArea, fragment);

                    // Additional points for the fragment.
                    //polygonBuilder.ProcessPolygonDetailPoints (polygonArea, fragment);
                    processor.ProcessPolygonDetailPoints (polygonArea, fragment);

                    // Set the triangles and build the mesh.
                    Bounds refBounds = polygonArea.aabb;
                    if (!_refFragBounds.ContainsKey (polygonArea.hash)) {
                        _refFragBounds.Add (polygonArea.hash, polygonArea.aabb);
                    } else {
                        refBounds = _refFragBounds [polygonArea.hash];
                    }
                    // Create mesh data (vertices, normals, tangents)
                    //polygonBuilder.ProcessPolygonAreaMesh (polygonArea, refBounds);
                    processor.ProcessPolygonAreaMesh (polygonArea, refBounds, fragment);

                    // Adds the unique polygon to the snapshot.
                    snapshot.polygonAreas.Add (polygonArea);

                    // Add polygon area to the SproutCompositeManager.
                    sproutCompositeManager.ManagePolygonArea (polygonArea, snapshot);

                    if (resolution == 0) {
                        // Generate Textures and materials.
                        sproutCompositeManager.GenerateTextures (polygonArea, snapshot, fragment, this);
                        sproutCompositeManager.GenerateMaterials (polygonArea, snapshot, true);
                    }
                }
            }
            
            sproutCompositeManager.ShowAllBranchesInMesh ();

            treeFactory.previewTree.obj.transform.parent = parentTransform;
            sproutCompositeManager.EndUsage ();
            polygonBuilder.EndUsage ();
            processor.EndUsage ();
        }        
        #endregion

        #region Variation Processing
        /// <summary>
        /// Regenerates a preview for the selected variation.
        /// </summary>
        /// <param name="isNewSeed"><c>True</c> to create a new preview (new seed).</param>
        public void ProcessVariation (bool isNewSeed = false) {
            // Process snapshots and cache them. 
            ProcessSnapshots ();
        }
        public void ProcessSnapshots (bool force = false) {
            BranchDescriptor snapshot;
            for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                snapshot = branchDescriptorCollection.snapshots [i];
                if (!sproutCompositeManager.HasSnapshot (snapshot.id) || force) {
                    snapshotIndex = i;
                    SnapshotCollectionToPipeline ();
                    ProcessSnapshot (i, true);
                    ProcessSnapshotPolygons (snapshot);
                }
            }
        }
        #endregion

        #region Texture Processing
        public bool GeneratePolygonTexture (
            BranchDescriptor snapshot,
            Mesh mesh, 
            Bounds bounds,
            Vector3 planeNormal,
            Vector3 planeUp,
            Vector3 planeOffset,
            Material[] originalMaterials,
            MaterialMode materialMode,
            int width,
            int height,
            out Texture2D texture)
        {
            texture = null;

            // Apply material mode.
            GameObject previewTree = TreeFactory.GetActiveInstance ().previewTree.obj;
            MeshRenderer meshRenderer = previewTree.GetComponent<MeshRenderer> ();
            if (materialMode == MaterialMode.Albedo) { // Albedo
                meshRenderer.sharedMaterials = GetAlbedoMaterials (originalMaterials,
                    branchDescriptorCollection.sproutStyleA,
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    branchDescriptorCollection.branchColorShade,
                    branchDescriptorCollection.branchColorSaturation,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex,
                    true);
            } else if (materialMode == MaterialMode.Normals) { // Normals
                meshRenderer.sharedMaterials = GetNormalMaterials (originalMaterials, isPrefabExport?true:false);
            } else if (materialMode == MaterialMode.Extras) { // Extras
                meshRenderer.sharedMaterials = GetExtraMaterials (originalMaterials,
                    branchDescriptorCollection.sproutStyleA,
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            } else if (materialMode == MaterialMode.Subsurface) { // Subsurface
                meshRenderer.sharedMaterials = GetSubsurfaceMaterials (originalMaterials,
                    branchDescriptorCollection.sproutStyleA, 
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    branchDescriptorCollection.branchColorSaturation,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            } else if (materialMode == MaterialMode.Composite) { // Composite
                meshRenderer.sharedMaterials = GetCompositeMaterials (originalMaterials,
                    branchDescriptorCollection.sproutStyleA, 
                    branchDescriptorCollection.sproutStyleB,
                    branchDescriptorCollection.sproutStyleCrown,
                    snapshot.sproutASubmeshIndex,
                    snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
            }

            // Prepare texture builder according to the material mode.
            TextureBuilder tb = new TextureBuilder ();
            if (materialMode == MaterialMode.Normals) {
                tb.backgroundColor = new Color (0.5f, 0.5f, 1f, 1f);
                tb.textureFormat = TextureFormat.RGB24;
            } else if (materialMode == MaterialMode.Subsurface) {
                tb.backgroundColor = new Color (0f, 0f, 0f, 1f);
                tb.textureFormat = TextureFormat.RGB24;
            } else if (materialMode == MaterialMode.Extras) {
                tb.backgroundColor = new Color (0f, 0f, 1f, 1f);
                tb.textureFormat = TextureFormat.RGB24;
            }

            // Set the mesh..
            tb.useTextureSizeToTargetRatio = true;
            tb.BeginUsage (previewTree, mesh);
            tb.textureSize = new Vector2 (width, height);
            
            Vector3 cameraCenter = bounds.center - planeOffset;
            cameraCenter = Quaternion.LookRotation (Vector3.Cross (planeNormal, planeUp), planeUp) * cameraCenter;
            cameraCenter += planeOffset;

            texture = tb.GetTexture (cameraCenter, planeNormal, planeUp, bounds);
            
            tb.EndUsage ();

            return true;
        }
        public bool GenerateSnapshopTextures (int snapshotIndex, BranchDescriptorCollection branchDescriptorCollection,
            int width, int height, string albedoPath, string normalPath, string extrasPath, string subsurfacePath, string compositePath) {
            return GenerateSnapshopTextures (snapshotIndex, branchDescriptorCollection, width, height, GetPreviewTreeBounds (),
                albedoPath, normalPath, extrasPath, subsurfacePath, compositePath);
        }
        public bool GenerateSnapshopTextures (int snapshotIndex, BranchDescriptorCollection branchDescriptorCollection,
            int width, int height, Bounds bounds,
            string albedoPath, string normalPath, string extrasPath, string subsurfacePath, string compositePath) {
            BeginSnapshotProgress (branchDescriptorCollection);
            // ALBEDO
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                ReportProgress ("Processing albedo texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Albedo, 
                    width,
                    height,
                    bounds,
                    albedoPath);
                ReportProgress ("Processing albedo texture.", 20f);
            }
            // NORMALS
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                ReportProgress ("Processing normal texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex,
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Normals, 
                    width,
                    height,
                    bounds,
                    normalPath);
                ReportProgress ("Processing normal texture.", 20f);
            }
            // EXTRAS
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) {
                ReportProgress ("Processing extras texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Extras, 
                    width,
                    height,
                    bounds,
                    extrasPath);
                ReportProgress ("Processing extras texture.", 20f);
            }
            // SUBSURFACE
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) {
                ReportProgress ("Processing subsurface texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Subsurface, 
                    width,
                    height,
                    bounds,
                    subsurfacePath);
                ReportProgress ("Processing subsurface texture.", 20f);
            }
            // COMPOSITE
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) {
                ReportProgress ("Processing composite texture.", 0f);
                GenerateSnapshopTexture (
                    branchDescriptorCollection.snapshotIndex, 
                    branchDescriptorCollection,
                    SproutSubfactory.MaterialMode.Composite, 
                    width,
                    height,
                    bounds,
                    compositePath);
                ReportProgress ("Processing composite texture.", 20f);
            }
            FinishSnapshotProgress ();
            
            // Cleanup.
            MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
            UnityEngine.Object.DestroyImmediate (meshFilter.sharedMesh);

            return true;
        }
        /// <summary>
        /// Generates the texture for a giver snapshot.
        /// </summary>
        /// <param name="snapshotIndex">Index for the snapshot.</param>
        /// <param name="materialMode">Mode mode: composite, albedo, normals, extras or subsurface.</param>
        /// <param name="width">Maximum width for the texture.</param>
        /// <param name="height">Maximum height for the texture.</param>
        /// <param name="texturePath">Path to save the texture.</param>
        /// <returns>Texture generated.</returns>
        public Texture2D GenerateSnapshopTexture (
            int snapshotIndex, 
            BranchDescriptorCollection branchDescriptorCollection, 
            MaterialMode materialMode, 
            int width, 
            int height,
            Bounds bounds,
            string texturePath = "") 
        {
            if (snapshotIndex >= branchDescriptorCollection.snapshots.Count) {
                Debug.LogWarning ("Could not generate branch snapshot texture. Index out of range.");
            } else {
                // Regenerate branch mesh and apply material mode.
                this.snapshotIndex = snapshotIndex;
                ProcessSnapshot (snapshotIndex, false, materialMode);
                // Build and save texture.
                TextureBuilder tb = new TextureBuilder ();
                if (materialMode == MaterialMode.Normals) {
                    tb.backgroundColor = new Color (0.5f, 0.5f, 1f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                } else if (materialMode == MaterialMode.Subsurface) {
                    tb.backgroundColor = new Color (0f, 0f, 0f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                } else if (materialMode == MaterialMode.Extras) {
                    tb.backgroundColor = new Color (0f, 0f, 1f, 1f);
                    tb.textureFormat = TextureFormat.RGB24;
                }
                // Get tree mesh.
                GameObject previewTree = treeFactory.previewTree.obj;
                tb.useTextureSizeToTargetRatio = true;
                tb.BeginUsage (previewTree);
                tb.textureSize = new Vector2 (width, height);
                Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up, bounds, texturePath);
                tb.EndUsage ();
                return sproutTexture;
            }
            return null;
        }
        Bounds GetPreviewTreeBounds () {
            GameObject previewTree = treeFactory.previewTree.obj;
            MeshFilter meshFilder = previewTree.GetComponent<MeshFilter> ();
            if (meshFilder != null) {
                return meshFilder.sharedMesh.bounds;
            }
            return new Bounds ();
        }
        /// <summary>
        /// Generates an atlas texture from a snapshot at each snapshot in the collection.
        /// </summary>
        /// <param name="branchDescriptorCollection">Collection of snapshot.</param>
        /// <param name="width">Width in pixels for the atlas.</param>
        /// <param name="height">Height in pixels for the atlas.</param>
        /// <param name="padding">Padding in pixels between each atlas sprite.</param>
        /// <param name="albedoPath">Path to save the albedo texture.</param>
        /// <param name="normalsPath">Path to save the normals texture.</param>
        /// <param name="extrasPath">Path to save the extras texture.</param>
        /// <param name="subsurfacePath">Path to save the subsurface texture.</param>
        /// <param name="compositePath">Path to save the composite texture.</param>
        /// <returns><c>True</c> if the atlases were created.</returns>
        public bool GenerateAtlasTexture (
            BranchDescriptorCollection branchDescriptorCollection, 
            int width, 
            int height, 
            int padding,
            string albedoPath, 
            string normalPath, 
            string extrasPath, 
            string subsurfacePath, 
            string compositePath) 
        {
            #if UNITY_EDITOR
            if (branchDescriptorCollection.snapshots.Count == 0) {
                Debug.LogWarning ("Could not generate atlas texture, no branch snapshots were found.");
            } else {
                // 1. Generate each snapshot mesh.
                float largestMeshSize = 0f; 
                List<Mesh> meshes = new List<Mesh> (); // Save the mesh for each snapshot.
                List<BranchDescriptor> snapshots = new List<BranchDescriptor> ();
                BranchDescriptor snapshot;
                List<Material[]> materials = new List<Material[]> ();
                List<Texture2D> texturesForAtlas = new List<Texture2D> ();
                Material[] modeMaterials;
                TextureBuilder tb = new TextureBuilder ();
                Texture2D atlas;
                tb.useTextureSizeToTargetRatio = true;

                double editorTime = UnityEditor.EditorApplication.timeSinceStartup;

                BeginAtlasProgress (branchDescriptorCollection);

                MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 0f);
                    snapshotIndex = i;
                    SnapshotCollectionToPipeline ();
                    ProcessSnapshot (i, false);
                    meshes.Add (UnityEngine.Object.Instantiate (meshFilter.sharedMesh));
                    snapshots.Add (branchDescriptorCollection.snapshots [i]);
                    materials.Add (meshRenderer.sharedMaterials);
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 10f);
                }

                // 2. Get the larger snapshot.
                for (int i = 0; i < meshes.Count; i++) {
                    if (meshes [i].bounds.max.magnitude > largestMeshSize) {
                        largestMeshSize = meshes [i].bounds.max.magnitude;
                    }
                }

                // Generate each mode texture.
                GameObject previewTree = treeFactory.previewTree.obj;

                // ALBEDO
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating albedo texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.1 Albedo.
                        modeMaterials = GetAlbedoMaterials (materials [i],
                            branchDescriptorCollection.sproutStyleA,
                            branchDescriptorCollection.sproutStyleB,
                            branchDescriptorCollection.sproutStyleCrown,
							branchDescriptorCollection.branchColorShade,
							branchDescriptorCollection.branchColorSaturation,
                            snapshot.sproutASubmeshIndex,
                            snapshot.sproutBSubmeshIndex,
                            snapshot.sproutCrownSubmeshIndex);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating albedo texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating albedo atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    SaveTextureToFile (atlas, albedoPath);
                    CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating albedo atlas texture.", 10f);
                }

                // NORMALS
                if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                    for (int i = 0; i < meshes.Count; i++) {
                        ReportProgress ("Creating normal texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.2 Normals.
                        modeMaterials = GetNormalMaterials (materials [i], true);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0.5f, 0.5f, 1f, 1f);
                        tb.textureFormat = TextureFormat.RGB24;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating extra texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating normal atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    SaveTextureToFile (atlas, normalPath);
                    SetTextureAsNormalMap (atlas, normalPath);
                    CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating normal atlas texture.", 10f);
                }

                // EXTRAS
                if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating extras texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.3 Extra.
                        modeMaterials = GetExtraMaterials (materials [i],
                            branchDescriptorCollection.sproutStyleA,
                            branchDescriptorCollection.sproutStyleB,
                            branchDescriptorCollection.sproutStyleCrown,
                            snapshot.sproutASubmeshIndex,
                            snapshot.sproutBSubmeshIndex,
                            snapshot.sproutCrownSubmeshIndex);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0f, 0f, 1f, 1f);
                        tb.textureFormat = TextureFormat.RGB24;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating extras texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating extras atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    SaveTextureToFile (atlas, extrasPath);
                    CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating extras atlas texture.", 10f);
                }

                // SUBSURFACE
                if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) {
                    for (int i = 0; i < meshes.Count; i++) {
                        snapshot = snapshots [i];
                        ReportProgress ("Creating subsurface texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.4 Subsurface.
                        modeMaterials = GetSubsurfaceMaterials (materials [i],
                            branchDescriptorCollection.sproutStyleA, 
                            branchDescriptorCollection.sproutStyleB,
                            branchDescriptorCollection.sproutStyleCrown,
                            branchDescriptorCollection.branchColorSaturation,
                            snapshot.sproutASubmeshIndex,
                            snapshot.sproutBSubmeshIndex,
                            snapshot.sproutCrownSubmeshIndex);
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0f, 0f, 0f, 1f);
                        tb.textureFormat = TextureFormat.RGB24;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating subsurface texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating subsurface atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    SaveTextureToFile (atlas, subsurfacePath);
                    CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating subsurface atlas texture.", 10f);
                }

                // COMPOSITE
                if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) {
                    for (int i = 0; i < meshes.Count; i++) {
                        ReportProgress ("Creating composite texture for snapshot " + i + ".", 0f);
                        // 3. Get the texture scale for the texture.
                        float meshScale = meshes [i].bounds.max.magnitude / largestMeshSize;
                        // 4.5 Composite.
                        modeMaterials = materials [i];
                        for (int k = 0; k < modeMaterials.Length; k++) {
                            modeMaterials [k].EnableKeyword ("_WINDQUALITY_NONE");
                        }
                        /*
                        GetCompositeMaterials (materials [i],
                            GetMaterialAStartIndex (branchDescriptorCollection), GetMaterialBStartIndex (branchDescriptorCollection));
                            */
                        meshFilter.sharedMesh = meshes [i];
                        meshRenderer.sharedMaterials = modeMaterials;
                        tb.backgroundColor = new Color (0.5f, 0.5f, 0.5f, 0f);
                        tb.textureFormat = TextureFormat.RGBA32;
                        tb.BeginUsage (previewTree);
                        tb.textureSize = new Vector2 (width * meshScale, height * meshScale);
                        Texture2D sproutTexture = tb.GetTexture (new Plane (Vector3.right, Vector3.zero), Vector3.up);
                        texturesForAtlas.Add (sproutTexture);
                        tb.EndUsage ();
                        DestroyMaterials (modeMaterials);
                        ReportProgress ("Creating composite texture for snapshot " + i + ".", 20f);
                    }
                    ReportProgress ("Creating composite atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    SaveTextureToFile (atlas, compositePath);
                    CleanTextures (texturesForAtlas);
                    UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating composite atlas texture.", 10f);
                }

                // Cleanup, destroy meshes, materials and textures.
                for (int i = 0; i < meshes.Count; i++) {
                    UnityEngine.Object.DestroyImmediate (meshes [i]);
                }
                for (int i = 0; i < materials.Count; i++) {
                    for (int j = 0; j < materials [i].Length; j++) {
                        UnityEngine.Object.DestroyImmediate (materials [i][j]);   
                    }
                }
                FinishAtlasProgress ();
                return true;
            }
            #endif
            return false;
        }
        /// <summary>
        /// Generates an atlas texture from the textures registered at the SproutCompositeManager.
        /// </summary>
        /// <param name="branchDescriptorCollection">Collection of snapshot.</param>
        /// <param name="width">Width in pixels for the atlas.</param>
        /// <param name="height">Height in pixels for the atlas.</param>
        /// <param name="padding">Padding in pixels between each atlas sprite.</param>
        /// <param name="albedoPath">Path to save the albedo texture.</param>
        /// <param name="normalsPath">Path to save the normals texture.</param>
        /// <param name="extrasPath">Path to save the extras texture.</param>
        /// <param name="subsurfacePath">Path to save the subsurface texture.</param>
        /// <param name="compositePath">Path to save the composite texture.</param>
        /// <returns><c>True</c> if the atlases were created.</returns>
        public bool GenerateAtlasTextureFromPolygons (
            BranchDescriptorCollection branchDescriptorCollection, 
            int width, 
            int height, 
            int padding,
            string albedoPath, 
            string normalsPath, 
            string extrasPath, 
            string subsurfacePath, 
            string compositePath) 
        {
            #if UNITY_EDITOR
            if (branchDescriptorCollection.snapshots.Count == 0) {
                Debug.LogWarning ("Could not generate atlas texture, no branch snapshots were found.");
            } else {
                // 1. Save the mesh and materials for each snapshot.
                List<Mesh> meshes = new List<Mesh> (); // Save the mesh for each snapshot.
                List<Material[]> materials = new List<Material[]> ();
                List<Texture2D> texturesForAtlas = new List<Texture2D> ();
                List<BroccoTree> trees = new List<BroccoTree> ();

                // 2. Create atlas texture.
                Texture2D atlas;

                // 3. Init helper vars.
                float largestMeshSize = 0f;
                Rect[] atlasRects = null;

                // 4. Begin atlas creation process.
                BeginAtlasProgress (branchDescriptorCollection);

                // 5. For each snapshot create its snapshot.
                MeshFilter meshFilter = treeFactory.previewTree.obj.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = treeFactory.previewTree.obj.GetComponent<MeshRenderer>();

                sproutCompositeManager.Clear ();

                ProcessSnapshots ();
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 0f);
                    snapshotIndex = i;
                    SnapshotCollectionToPipeline ();
                    ProcessSnapshot (i, false);

                    // 5.1 Save the snapshot tree, mesh and snapshot materials.
                    meshes.Add (UnityEngine.Object.Instantiate (meshFilter.sharedMesh));
                    materials.Add (meshRenderer.sharedMaterials);
                    trees.Add (treeFactory.previewTree);
                    treeFactory.previewTree.obj.transform.parent = null;
                    treeFactory.previewTree.obj.hideFlags = HideFlags.None;
                    treeFactory.previewTree = null;
                    ReportProgress ("Creating mesh for snapshot " + i + ".", 10f);

                }

                // 6. Get the snapshot with the largest area.
                for (int i = 0; i < meshes.Count; i++) {
                    if (meshes [i].bounds.max.magnitude > largestMeshSize) {
                        largestMeshSize = meshes [i].bounds.max.magnitude;
                    }
                }

                // 7. For each snapshot create its polygons.
                for (int i = 0; i < branchDescriptorCollection.snapshots.Count; i++) {
                    treeFactory.previewTree = trees [i];
                    snapshotTree = treeFactory.previewTree;
                    snapshotTreeMesh = meshFilter.sharedMesh;
                    sproutCompositeManager.textureGlobalScale = snapshotTreeMesh.bounds.max.magnitude / largestMeshSize; 
                    ProcessSnapshotPolygons (branchDescriptorCollection.snapshots [i]);
                }

                // 8.1 Generate the ALBEDO texture.
                branchDescriptorCollection.atlasAlbedoTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> albedoTextures = sproutCompositeManager.GetAlbedoTextures ();
                    for (int i = 0; i < albedoTextures.Count; i++) {
                        texturesForAtlas.Add (albedoTextures [i]);
                    }
                    ReportProgress ("Creating albedo atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlasRects = atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    branchDescriptorCollection.atlasAlbedoTexture = SaveTextureToFile (atlas, albedoPath, true);
                    CleanTextures (texturesForAtlas);
                    //UnityEngine.Object.DestroyImmediate (atlas, false);
                    ReportProgress ("Creating albedo atlas texture.", 10f);
                }

                // 8.2 Generate the NORMALS texture.
                branchDescriptorCollection.atlasNormalsTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) {
                    List<Texture2D> normalsTextures = sproutCompositeManager.GetNormalsTextures ();
                    for (int i = 0; i < normalsTextures.Count; i++) {
                        texturesForAtlas.Add (normalsTextures [i]);
                    }
                    ReportProgress ("Creating normals atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlasRects = atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    branchDescriptorCollection.atlasNormalsTexture = SaveTextureToFile (atlas, normalsPath, true);
                    SetTextureAsNormalMap (atlas, normalsPath);
                    CleanTextures (texturesForAtlas);
                    //UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating normals atlas texture.", 10f);
                }

                // 8.3 Generate the EXTRAS texture.
                branchDescriptorCollection.atlasExtrasTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> extrasTextures = sproutCompositeManager.GetExtrasTextures ();
                    for (int i = 0; i < extrasTextures.Count; i++) {
                        texturesForAtlas.Add (extrasTextures [i]);
                    }
                    ReportProgress ("Creating extras atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlasRects = atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    branchDescriptorCollection.atlasExtrasTexture = SaveTextureToFile (atlas, extrasPath, true);
                    CleanTextures (texturesForAtlas);
                    //UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating extras atlas texture.", 10f);
                }

                // 8.4 Generate the SUBSURFACE texture.
                branchDescriptorCollection.atlasSubsurfaceTexture = null;
                if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) {
                    List<Texture2D> subsurfaceTextures = sproutCompositeManager.GetSubsurfaceTextures ();
                    for (int i = 0; i < subsurfaceTextures.Count; i++) {
                        texturesForAtlas.Add (subsurfaceTextures [i]);
                    }
                    ReportProgress ("Creating subsurface atlas texture.", 0f);
                    atlas = new Texture2D (2, 2);
                    atlasRects = atlas.PackTextures (texturesForAtlas.ToArray (), padding, width, false);
                    atlas.alphaIsTransparency = true;
                    branchDescriptorCollection.atlasSubsurfaceTexture = SaveTextureToFile (atlas, subsurfacePath, true);
                    CleanTextures (texturesForAtlas);
                    //UnityEngine.Object.DestroyImmediate (atlas);
                    ReportProgress ("Creating subsurface atlas texture.", 10f);
                }

                // 8.5 Finish atlases creation.
                FinishAtlasProgress ();

                // 9. Set atlas rects to meshes.
                if (atlasRects != null) {
                    sproutCompositeManager.SetAtlasRects (atlasRects);
                    sproutCompositeManager.ApplyAtlasUVs ();
                }

                // 11. Clean up atlas building.
                sproutCompositeManager.textureGlobalScale = 1f;
                treeFactory.previewTree = null;
                for (int i = 0; i < trees.Count; i++) {
                    UnityEngine.Object.DestroyImmediate (trees[i].obj);
                }
                UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate ();
                return true;
            }
            #endif
            return false;
        }
        public Texture2D GetSproutTexture (int group, int index) {
            string textureId = GetSproutTextureId (group, index);
            return textureManager.GetTexture (textureId);
        }
        Texture2D GetOriginalSproutTexture (int group, int index) {
            Texture2D texture = null;
            List<SproutMap.SproutMapArea> sproutMapAreas = null;
            if (group == 0) {
                sproutMapAreas = branchDescriptorCollection.sproutStyleA.sproutMapAreas;
            } else if (group == 1) {
                sproutMapAreas = branchDescriptorCollection.sproutStyleB.sproutMapAreas;
            } else if (group == 2) {
                sproutMapAreas = branchDescriptorCollection.sproutStyleCrown.sproutMapAreas;
            }
            if (sproutMapAreas != null && sproutMapAreas.Count >= index) {
                texture = sproutMapAreas[index].texture;
            }
            return texture;
        }
        public void ProcessTextures () {
            //textureManager.Clear ();
            string textureId;
            // Process Sprout A albedo textures.
            for (int i = 0; i < branchDescriptorCollection.sproutStyleA.sproutMapAreas.Count; i++) {
                textureId = GetSproutTextureId (0, i);
                if (!textureManager.HasTexture (textureId)) {
                    textureManager.AddOrReplaceTexture (textureId, 
                        branchDescriptorCollection.sproutStyleA.sproutMapAreas [i].texture);
                }
            }
            // Process Sprout B albedo textures.    
            for (int i = 0; i < branchDescriptorCollection.sproutStyleB.sproutMapAreas.Count; i++) {
                textureId = GetSproutTextureId (1, i);
                if (!textureManager.HasTexture (textureId)) {
                    textureManager.AddOrReplaceTexture (textureId, 
                        branchDescriptorCollection.sproutStyleB.sproutMapAreas [i].texture);
                }
            }
            // Process Crown albedo textures.    
            for (int i = 0; i < branchDescriptorCollection.sproutStyleCrown.sproutMapAreas.Count; i++) {
                textureId = GetSproutTextureId (2, i);
                if (!textureManager.HasTexture (textureId)) {
                    textureManager.AddOrReplaceTexture (textureId, 
                        branchDescriptorCollection.sproutStyleCrown.sproutMapAreas [i].texture);
                }
            }
        }
        public void ProcessTexture (Texture2D texture, int group, int index) {
            #if UNITY_EDITOR
            string textureId = GetSproutTextureId (group, index);
            if (group == 0) {
                textureManager.AddOrReplaceTexture (textureId, texture);
            } else if (group == 1) {
                textureManager.AddOrReplaceTexture (textureId, texture);
            } else if (group == 2) {
                textureManager.AddOrReplaceTexture (textureId, texture);
            }
            SnapshotCollectionToPipeline ();
            #endif
            /*
            #if UNITY_EDITOR
            string textureId = GetSproutTextureId (group, index);
            //if (textureManager.HasTexture (textureId)) {
                Texture2D originalTexture = GetOriginalSproutTexture (group, index);
                Texture2D newTexture = ApplyTextureTransformations (originalTexture, alpha);
                newTexture.alphaIsTransparency = true;
                textureManager.AddOrReplaceTexture (textureId, newTexture, true);
                SnapshotCollectionToPipeline ();
            //}
            #endif
            */
        }
        Texture2D ApplyTextureTransformations (Texture2D originTexture, float alpha) {
            if (originTexture != null) {
                Texture2D tex = textureManager.GetCopy (originTexture, alpha);
                return tex;
            }
            return null;
        }
        public static string GetTextureFileName (string path, string subfolder, string prefix, int take, SproutSubfactory.MaterialMode materialMode, bool isAtlas) {
			string _path = "";
			string takeString = FileUtils.GetFileSuffix (take);
			string modeString;
			if (materialMode == SproutSubfactory.MaterialMode.Albedo) {
				modeString = "Albedo";
			} else if (materialMode == SproutSubfactory.MaterialMode.Normals) {
				modeString = "Normals";
			} else if (materialMode == SproutSubfactory.MaterialMode.Extras) {
				modeString = "Extras";
			} else if (materialMode == SproutSubfactory.MaterialMode.Subsurface) {
				modeString = "Subsurface";
			} else if (materialMode == SproutSubfactory.MaterialMode.Mask) {
				modeString = "Mask";
			} else if (materialMode == SproutSubfactory.MaterialMode.Thickness) {
				modeString = "Thickness";
			} else {
				modeString = "Composite";
			}
			_path = "Assets" + path + "/" + subfolder + "/" + 
				prefix + takeString + (isAtlas?"_Atlas":"_Snapshot") + "_" + modeString + ".png";
			return _path;
		}
        public string GetSproutTextureId (int group, int index) {
            return  "sprout_" + group + "_" + index;
        }
        /// <summary>
		/// Saves a texture to a file.
		/// </summary>
		/// <param name="texture">Texture.</param>
		/// <param name="filename">Filename.</param>
		public static Texture2D SaveTextureToFile (Texture2D texture, string filename, bool importAsset = true) {
			#if UNITY_EDITOR
			System.IO.File.WriteAllBytes (filename, texture.EncodeToPNG());
            if (importAsset) {
                UnityEditor.AssetDatabase.Refresh ();
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D> (filename);
            }
            return texture;
            #else
            return null;
			#endif
		}
        public static void SetTextureAsNormalMap (Texture2D texture, string texturePath) {
            #if UNITY_EDITOR
            UnityEditor.TextureImporter importer = (UnityEditor.TextureImporter)UnityEditor.TextureImporter.GetAtPath (texturePath);
            importer.textureType = UnityEditor.TextureImporterType.NormalMap;
            importer.SaveAndReimport ();
            #endif
        }
        void CleanTextures (List<Texture2D> texturesToClean) {
            for (int i = 0; i < texturesToClean.Count; i++) {
                UnityEngine.Object.DestroyImmediate (texturesToClean [i]);
            }
            texturesToClean.Clear ();
        }
        #endregion

        #region Material Processing
        
        public Material[] GetCompositeMaterials (Material[] originalMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1) 
        {
            Material[] mats = new Material[originalMaterials.Length];
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < originalMaterials.Length; i++) {
                if (originalMaterials [i] != null) {
                    if (i == 0) {
                        mats[0] = originalMaterials [0];
                        mats[0].shader = GetSpeedTree8Shader ();
                        mats[0].SetFloat ("_WindQuality", 0f);
                        mats[0].enableInstancing = true;
                    } else {
                        Material m = new Material (originalMaterials[i]);
                        //m.shader = Shader.Find ("Hidden/Broccoli/SproutLabComposite");
                        //m.shader = Shader.Find ("Broccoli/SproutLabComposite");
                        m.shader = GetSpeedTree8Shader ();
                        m.enableInstancing = true;
                        /*
                        m.EnableKeyword ("EFFECT_BUMP");
                        m.EnableKeyword ("EFFECT_SUBSURFACE");
                        m.EnableKeyword ("EFFECT_EXTRA_TEX");
                        */
                        m.SetFloat ("_WindQuality", 0f);
                        m.EnableKeyword ("GEOM_TYPE_LEAF");
                        m.SetFloat ("_SubsurfaceKwToggle", 1f);
                        float subsurfaceIndirect = 0.5f;
                        if (originalMaterials[i].HasProperty("_SubsurfaceScale")) {
                            subsurfaceIndirect = originalMaterials[i].GetFloat ("_SubsurfaceScale") * 0.5f;
                        } else if (originalMaterials[i].HasProperty("_SubsurfaceIndirect")) {
                            subsurfaceIndirect = originalMaterials[i].GetFloat ("_SubsurfaceIndirect");
                        }
                        m.SetFloat ("_SubsurfaceIndirect", subsurfaceIndirect);
                        mats [i] = m;
                    }
                }
            }
            UpdateCompositeMaterials (mats, styleA, styleB, styleCrown, materialAStartIndex, materialBStartIndex, materialCrownStartIndex);
            return mats;
        }
        public void UpdateCompositeMaterials (Material[] compositeMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1) 
        {
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < compositeMaterials.Length; i++) {
                if (compositeMaterials [i] != null) {
                    Material m = compositeMaterials [i];
                    if (i >= materialAStartIndex) {
                        if (i >= materialCrownStartIndex && materialCrownStartIndex >= 0) {
                            m.SetFloat ("_Metallic", styleCrown.metallic);
                            m.SetFloat ("_Glossiness", styleCrown.glossiness);
				            m.SetFloat ("_SubsurfaceIndirect", styleCrown.subsurface);
                        } else if (i >= materialBStartIndex && materialBStartIndex >= 0) {
                            m.SetFloat ("_Metallic", styleB.metallic);
                            m.SetFloat ("_Glossiness", styleB.glossiness);
				            m.SetFloat ("_SubsurfaceIndirect", styleB.subsurface);
                        } else if (materialAStartIndex >= 0) {
                            m.SetFloat ("_Metallic", styleA.metallic);
                            m.SetFloat ("_Glossiness", styleA.glossiness);
				            m.SetFloat ("_SubsurfaceIndirect", styleA.subsurface);
                        }
                    }
                }
            }
        }
        public Material[] GetAlbedoMaterials (
            Material[] originalMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            float branchMaterialShade = 1f,
            float branchMaterialSaturation = 1f,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1,
            bool extraSaturation = false)
        {
            Material[] mats = new Material[originalMaterials.Length];
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m;
                if (originalMaterials [i] == null) {
                    m = originalMaterials [i];
                } else {
                    m = new Material (originalMaterials[i]);
                    m.shader = Shader.Find ("Hidden/Broccoli/SproutLabAlbedo");
                    m.enableInstancing = true;
                }
                mats [i] = m;
            }
            UpdateAlbedoMaterials (mats, styleA, styleB, styleCrown, branchMaterialShade, branchMaterialSaturation,
                materialAStartIndex, materialBStartIndex, materialCrownStartIndex, extraSaturation);
            return mats;
        }
        public void UpdateAlbedoMaterials (
            Material[] albedoMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            float branchMaterialShade = 1f,
            float branchMaterialSaturation = 1f,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1,
            bool extraSaturation = false)
        {
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            Material m;
            for (int i = 0; i < albedoMaterials.Length; i++) {
                m = albedoMaterials [i];
                if (albedoMaterials [i] != null) {
                    m.SetFloat ("_BranchShade", branchMaterialShade);
                    m.SetFloat ("_BranchSat", branchMaterialSaturation);
                    //m.SetFloat ("_ApplyExtraSat", applyExtraSaturation?1f:0f);
                    #if UNITY_EDITOR
                    m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                    #endif
                    if (i >= materialAStartIndex) {
                        if (i >= materialCrownStartIndex && materialCrownStartIndex >= 0) {
                            m.SetColor ("_TintColor", styleCrown.colorTint);
                            m.SetFloat ("_MinSproutTint", styleCrown.minColorTint);
                            m.SetFloat ("_MaxSproutTint", styleCrown.maxColorTint);
                            m.SetInt ("_SproutTintMode", (int)styleCrown.sproutTintMode);
                            m.SetFloat ("_InvertSproutTintMode", (styleCrown.invertSproutTintMode?1f:0f));
                            m.SetFloat ("_SproutTintVariance", styleCrown.sproutTintVariance);
                            m.SetFloat ("_MinSproutSat", styleCrown.minColorSaturation);
                            m.SetFloat ("_MaxSproutSat", styleCrown.maxColorSaturation);
                            m.SetInt ("_SproutSatMode", (int)styleCrown.sproutSaturationMode);
                            m.SetFloat ("_InvertSproutSatMode", (styleCrown.invertSproutSaturationMode?1f:0f)); 
                            m.SetFloat ("_SproutSatVariance", styleCrown.sproutSaturationVariance);
                            m.SetFloat ("_ApplyExtraSat", (extraSaturation?1f:0f));
                        } else if (i >= materialBStartIndex && materialBStartIndex >= 0) {
                            m.SetColor ("_TintColor", styleB.colorTint);
                            m.SetFloat ("_MinSproutTint", styleB.minColorTint);
                            m.SetFloat ("_MaxSproutTint", styleB.maxColorTint);
                            m.SetInt ("_SproutTintMode", (int)styleB.sproutTintMode);
                            m.SetFloat ("_InvertSproutTintMode", (styleB.invertSproutTintMode?1f:0f));
                            m.SetFloat ("_SproutTintVariance", styleB.sproutTintVariance);
                            m.SetFloat ("_MinSproutSat", styleB.minColorSaturation);
                            m.SetFloat ("_MaxSproutSat", styleB.maxColorSaturation);
                            m.SetInt ("_SproutSatMode", (int)styleB.sproutSaturationMode);
                            m.SetFloat ("_InvertSproutSatMode", (styleB.invertSproutSaturationMode?1f:0f));
                            m.SetFloat ("_SproutSatVariance", styleB.sproutSaturationVariance);
                            m.SetFloat ("_ApplyExtraSat", (extraSaturation?1f:0f));
                        } else if (materialAStartIndex >= 0) {
                            m.SetColor ("_TintColor", styleA.colorTint);
                            m.SetFloat ("_MinSproutTint", styleA.minColorTint);
                            m.SetFloat ("_MaxSproutTint", styleA.maxColorTint);
                            m.SetInt ("_SproutTintMode", (int)styleA.sproutTintMode);
                            m.SetFloat ("_InvertSproutTintMode", (styleA.invertSproutTintMode?1f:0f));
                            m.SetFloat ("_SproutTintVariance", styleA.sproutTintVariance);
                            m.SetFloat ("_MinSproutSat", styleA.minColorSaturation);
                            m.SetFloat ("_MaxSproutSat", styleA.maxColorSaturation);
                            m.SetInt ("_SproutSatMode", (int)styleA.sproutSaturationMode);
                            m.SetFloat ("_InvertSproutSatMode", (styleA.invertSproutSaturationMode?1f:0f));
                            m.SetFloat ("_SproutSatVariance", styleA.sproutSaturationVariance);
                            m.SetFloat ("_ApplyExtraSat", (extraSaturation?1f:0f));
                        }
                    }
                }
            }
        }
        public Material[] GetNormalMaterials (Material[] originalMaterials, bool isGammaDisplay) {
            Material[] mats = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabNormals");
                m.SetFloat ("_IsGammaDisplay", isGammaDisplay?1f:0f);
                #if UNITY_EDITOR
                float linearSpace = (UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                m.SetFloat ("_IsLinearColorSpace", linearSpace);
                #endif
                mats [i] = m;
            }
            return mats;
        }
        public Material[] GetExtraMaterials (Material[] originalMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1)
        { 
            Material[] mats = new Material[originalMaterials.Length];
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabExtra");
                #if UNITY_EDITOR
                m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                #endif
                mats [i] = m;
            }
            UpdateExtrasMaterials (mats, styleA, styleB, styleCrown, materialAStartIndex, materialBStartIndex, materialCrownStartIndex);
            return mats;
        }
        public void UpdateExtrasMaterials (Material[] extrasMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1) 
        {
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < extrasMaterials.Length; i++) {
                if (extrasMaterials [i] != null) {
                    Material m = extrasMaterials [i];
                    if (extrasMaterials [i] != null) {
                        #if UNITY_EDITOR
                        m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                        #endif
                        if (i >= materialAStartIndex) {
                            if (i >= materialCrownStartIndex && materialCrownStartIndex >= 0) {
                                m.SetFloat ("_Metallic", styleCrown.metallic);
                                m.SetFloat ("_Glossiness", styleCrown.glossiness);
                            } else if (i >= materialBStartIndex && materialBStartIndex >= 0) {
                                m.SetFloat ("_Metallic", styleB.metallic);
                                m.SetFloat ("_Glossiness", styleB.glossiness);
                            } else if (materialAStartIndex >= 0) {
                                m.SetFloat ("_Metallic", styleA.metallic);
                                m.SetFloat ("_Glossiness", styleA.glossiness);
                            }
                        }
                    }
                }
            }
        }
        public Material[] GetSubsurfaceMaterials (Material[] originalMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            float branchSaturation = 1f,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1) 
        {
            Material[] mats = new Material[originalMaterials.Length];
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < originalMaterials.Length; i++) {
                Material m = new Material (originalMaterials[i]);
                m.shader = Shader.Find ("Hidden/Broccoli/SproutLabSubsurface");
                m.SetFloat ("_BranchSat", branchSaturation);
                #if UNITY_EDITOR
                m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                #endif
                mats [i] = m;
            }
            UpdateSubsurfaceMaterials (mats, styleA, styleB, styleCrown, branchSaturation,
                materialAStartIndex, materialBStartIndex, materialCrownStartIndex);
            return mats;
        }
        public void UpdateSubsurfaceMaterials (Material[] subsurfaceMaterials,
            BranchDescriptorCollection.SproutStyle styleA,
            BranchDescriptorCollection.SproutStyle styleB,
            BranchDescriptorCollection.SproutStyle styleCrown,
            float branchSaturation = 1f,
            int materialAStartIndex = -1,
            int materialBStartIndex = -1,
            int materialCrownStartIndex = -1) 
        {
            if (materialAStartIndex == -1) materialAStartIndex = 0;
            for (int i = 0; i < subsurfaceMaterials.Length; i++) {
                if (subsurfaceMaterials [i] != null) {
                    Material m = subsurfaceMaterials [i];
                    if (subsurfaceMaterials [i] != null) {
                        m.SetFloat ("_BranchSat", branchSaturation);
                        #if UNITY_EDITOR
                        m.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
                        #endif
                        if (i >= materialAStartIndex) {
                            if (i >= materialCrownStartIndex && materialCrownStartIndex >= 0) {
                                m.SetColor ("_TintColor", styleCrown.colorTint);
                                m.SetFloat ("_MinSproutTint", styleCrown.minColorTint);
                                m.SetFloat ("_MaxSproutTint", styleCrown.maxColorTint);
                                m.SetInt ("_SproutTintMode", (int)styleCrown.sproutTintMode);
                                m.SetFloat ("_InvertSproutTintMode", (styleCrown.invertSproutTintMode?1f:0f));
                                m.SetFloat ("_SproutTintVariance", styleCrown.sproutTintVariance);
                                m.SetFloat ("_MinSproutSat", styleCrown.minColorSaturation);
                                m.SetFloat ("_MaxSproutSat", styleCrown.maxColorSaturation);
                                m.SetInt ("_SproutSatMode", (int)styleCrown.sproutSaturationMode);
                                m.SetFloat ("_InvertSproutSatMode", (styleCrown.invertSproutSaturationMode?1f:0f)); 
                                m.SetFloat ("_SproutSatVariance", styleCrown.sproutSaturationVariance);
                                m.SetFloat ("_SproutSubsurface", styleCrown.subsurface);
                            } else if (i >= materialBStartIndex && materialBStartIndex >= 0) {
                                m.SetColor ("_TintColor", styleB.colorTint);
                                m.SetFloat ("_MinSproutTint", styleB.minColorTint);
                                m.SetFloat ("_MaxSproutTint", styleB.maxColorTint);
                                m.SetInt ("_SproutTintMode", (int)styleB.sproutTintMode);
                                m.SetFloat ("_InvertSproutTintMode", (styleB.invertSproutTintMode?1f:0f));
                                m.SetFloat ("_SproutTintVariance", styleB.sproutTintVariance);
                                m.SetFloat ("_MinSproutSat", styleB.minColorSaturation);
                                m.SetFloat ("_MaxSproutSat", styleB.maxColorSaturation);
                                m.SetInt ("_SproutSatMode", (int)styleB.sproutSaturationMode);
                                m.SetFloat ("_InvertSproutSatMode", (styleB.invertSproutSaturationMode?1f:0f));
                                m.SetFloat ("_SproutSubsurface", styleB.subsurface);
                            } else if (materialAStartIndex >= 0) {
                                m.SetColor ("_TintColor", styleA.colorTint);
                                m.SetFloat ("_MinSproutTint", styleA.minColorTint);
                                m.SetFloat ("_MaxSproutTint", styleA.maxColorTint);
                                m.SetInt ("_SproutTintMode", (int)styleA.sproutTintMode);
                                m.SetFloat ("_InvertSproutTintMode", (styleA.invertSproutTintMode?1f:0f));
                                m.SetFloat ("_SproutTintVariance", styleA.sproutTintVariance);
                                m.SetFloat ("_MinSproutSat", styleA.minColorSaturation);
                                m.SetFloat ("_MaxSproutSat", styleA.maxColorSaturation);
                                m.SetInt ("_SproutSatMode", (int)styleA.sproutSaturationMode);
                                m.SetFloat ("_InvertSproutSatMode", (styleA.invertSproutSaturationMode?1f:0f));
                                m.SetFloat ("_SproutSatVariance", styleA.sproutSaturationVariance);
                                m.SetFloat ("_SproutSubsurface", styleA.subsurface);
                            }
                        }
                    }
                }
            }
        }
        /*
        public static int GetMaterialAStartIndex (BranchDescriptorCollection branchDescriptorCollection) {
            return 1;
        }
        public static int GetMaterialBStartIndex (BranchDescriptorCollection branchDescriptorCollection) {
            int materialIndex = branchDescriptorCollection.sproutStyleA.sproutMapAreas.Count + 1;
            return materialIndex;
        }
        public static int GetMaterialCrownStartIndex (BranchDescriptorCollection branchDescriptorCollection) {
            int materialIndex = branchDescriptorCollection.sproutStyleA.sproutMapAreas.Count + 
                branchDescriptorCollection.sproutStyleB.sproutMapAreas.Count + 1;
            return materialIndex;
        }
        */
        public void DestroyMaterials (Material[] materials) {
            for (int i = 0; i < materials.Length; i++) {
                UnityEngine.Object.DestroyImmediate (materials [i]);
            }
        }
        private Shader GetSpeedTree8Shader () {
            Shader st8Shader = null;
            st8Shader = Shader.Find ("Nature/SpeedTree8");
            return st8Shader;
        }
        #endregion

        #region Processing Progress
        public delegate void OnReportProgress (string msg, float progress);
        public delegate void OnFinishProgress ();
        public OnReportProgress onReportProgress;
        public OnFinishProgress onFinishProgress;
        float progressGone = 0f;
        float progressToGo = 0f;
        public string progressTitle = "";
        public void BeginSnapshotProgress (BranchDescriptorCollection branchDescriptorCollection) {
            progressGone = 0f;
            progressToGo = 0f;
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) progressToGo += 20; // Albedo
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) progressToGo += 20; // Normals
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) progressToGo += 20; // Extras
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) progressToGo += 20; // Subsurface
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) progressToGo += 20; // Composite
            progressTitle = "Creating Snapshot Textures";
        }
        public void FinishSnapshotProgress () {
            progressGone = progressToGo;
            ReportProgress ("Finish " + progressTitle, 0f);
            onFinishProgress?.Invoke ();
        }
        public void BeginAtlasProgress (BranchDescriptorCollection branchDescriptorCollection) {
            progressGone = 0f;
            progressToGo = branchDescriptorCollection.snapshots.Count * 10f;
            if ((branchDescriptorCollection.exportTexturesFlags & 1) == 1) progressToGo += 30; // Albedo
            if ((branchDescriptorCollection.exportTexturesFlags & 2) == 2) progressToGo += 30; // Normals
            if ((branchDescriptorCollection.exportTexturesFlags & 4) == 4) progressToGo += 30; // Extras
            if ((branchDescriptorCollection.exportTexturesFlags & 8) == 8) progressToGo += 30; // Subsurface
            if ((branchDescriptorCollection.exportTexturesFlags & 16) == 16) progressToGo += 30; // Composite
            progressTitle = "Creating Atlas Textures";
        }
        public void FinishAtlasProgress () {
            progressGone = progressToGo;
            ReportProgress ("Finish " + progressTitle, 0f);
            onFinishProgress?.Invoke ();
        }
        void ReportProgress (string title, float progressToAdd) {
            progressGone += progressToAdd;
            onReportProgress?.Invoke (title, progressGone/progressToGo);
        }
        #endregion
    }
}