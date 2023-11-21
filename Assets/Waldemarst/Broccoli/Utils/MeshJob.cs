using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Broccoli.Utils
{
    /// <summary>
    /// Class to apply scale, position offset, rotation and bending to a target mesh.
    /// Usage:
    /// 1. Use the SetTargetMesh method to specify the mesh to apply transforms to.
    /// 2. Use the AddTransform method to add scale, positional offset and rotation to a group of vertex.
    /// 3. Use the IncludeBending method to add bending (the bending factor is taken from the UV2 channel of the mesh).
    /// 4. Use the IncludeIds method to add ids to a group of vertices (if applyIdsChannel is > 0 then the ids for the group of vertices is applied to this UV channel index).
    /// 5. Call the ExecuteJob method to apply the transformations to the target mesh.
    /// </summary>
    public class MeshJob {
        #region Vars
        public int batchSize = 4;
		protected bool includeTangents = true;
		public bool applyTranslationAtEnd = true;
		public int applyIdsChannel = 0;
		public bool applyUV5Transform = false;
		public bool applyUV6Transform = false;
		public bool applyUV7Transform = false;
		public bool applyUV8Transform = false;
		public enum BendMode {
			Add = 0,
			Multiply = 1,
			Stylized = 2
		}
		public BendMode bendMode = BendMode.Add;
		public static Vector3 gravityDirection = Vector3.down;
        protected List<Vector3> _offsets = new List<Vector3> ();
		protected List<Vector3> _pivots = new List<Vector3> ();
		protected List<Vector3> _scales = new List<Vector3> ();
        protected List<Quaternion> _rotations = new List<Quaternion> ();
		protected List<bool> _flips = new List<bool> ();
        protected List<Vector2> _bendings = new List<Vector2> ();
        protected List<int> _starts = new List<int> ();
        protected List<int> _lengths = new List<int> ();
		protected List<Vector4> _ids = new List<Vector4> ();
		protected List<int> _flags = new List<int> ();
        protected List<Vector3> vertices = new List<Vector3> ();
        protected List<Vector3> normals = new List<Vector3> ();
        protected List<Vector4> tangents = new List<Vector4> ();
		protected List<Vector4> uv2s = new List<Vector4> ();
		protected List<Vector3> uv5s = new List<Vector3> ();
		protected List<Vector3> uv6s = new List<Vector3> ();
		protected List<Vector3> uv7s = new List<Vector3> ();
		protected List<Vector3> uv8s = new List<Vector3> ();
        protected Mesh targetMesh = null;
        #endregion

        #region Job
		/// <summary>
		/// Job structure to process branch skins.
		/// </summary>
		struct MeshJobImpl : IJobParallelFor {
			#region Input
			public bool includeTangents;
			/// <summary>
			/// If <c>true</c> the translation (offset) if applied at the end of the processing (after scaling and rotation).
			/// </summary>
			public bool applyTranslationAtEnd;
			/// <summary>
			/// Contains the PIVOT (x, y, z).
			/// </summary>
			public NativeArray<Vector3> pivot;
			/// <summary>
			/// Contains the OFFSET (x, y, z).
			/// </summary>
			public NativeArray<Vector3> offset;
			/// <summary>
			/// Contains the SCALE (x, y, z).
			/// </summary>
			public NativeArray<Vector3> scale;
			/// <summary>
			/// Contains the ORIENTATION for the mesh segment.
			/// </summary>
			public NativeArray<Quaternion> orientation;
			/// <summary>
			/// Contains the FLIP for normals and tangents for the mesh segment.
			/// </summary>
			public NativeArray<bool> flip;
			/// <summary>
			/// Contains the BENDING for the mesh segment.
			/// </summary>
			public NativeArray<Vector2> bending;
			/// <summary>
			/// Contains the ids to apply to UV2 (ch. 3).
			/// </summary>
			public NativeArray<Vector4> ids;
			/// <summary>
			/// Flags to control the applicacion of transforms.
			/// </summary>
			public NativeArray<int> flags;
			/// <summary>
            /// START for the submesh vertices.
            /// </summary>
            public NativeArray<int> start;
            /// <summary>
            /// LENGTH of the vertices for the submesh
            /// </summary>
            public NativeArray<int> length;
			public Vector3 gravityForward;
			public Vector3 gravityRight;
			public Vector3 gravityUp;
			public Vector3 gravityDirection;
			/// <summary>
			/// Bend mode to combine both forward and side bending.
			/// 0 = add, side bending is applied, then forward bending.
			/// 1 = multiply, forward and side quaternion are multiplied.
			/// 2 = stylized, forward and side blending are lerped.
			/// </summary>
			public int bendMode;
			#endregion

			#region Consts
			public static int APPLY_PIVOT = 1;
			public static int APPLY_FLIP = 2;
			public static int APPLY_SCALE = 4;
			public static int APPLY_OFFSET = 8;
			public static int APPLY_ROTATION = 16;
			public static int APPLY_BEND = 32;
			public static int APPLY_IDS = 64;
			public static int APPLY_UV5_TRANSFORM = 128;
			public static int APPLY_UV6_TRANSFORM = 256;
			public static int APPLY_UV7_TRANSFORM = 512;
			public static int APPLY_UV8_TRANSFORM = 1024;
			#endregion

			#region Mesh Input
			/// <summary>
			/// Vertices for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> vertices;
			/// <summary>
			/// Normals for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> normals;
			/// <summary>
			/// Tangents for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector4> tangents;
			/// <summary>
			/// Output ids.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector4> uvIds;
			/// <summary>
			/// UV2s for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector4> uv2s;
			/// <summary>
			/// UV5s for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> uv5s;
			/// <summary>
			/// UV6s for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> uv6s;
			/// <summary>
			/// UV7s for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> uv7s;
			/// <summary>
			/// UV8s for the input mesh.
			/// </summary>
			[NativeDisableParallelForRestriction]
			public NativeArray<Vector3> uv8s;
			#endregion

			#region Job Methods
			/// <summary>
			/// Executes one per sprout.
			/// </summary>
			/// <param name="i"></param>
			public void Execute (int i) {
				int vertexStart = start [i];
				int vertexEnd = start [i] + length [i];
				int flag = flags [i];
				Vector3 spDirection = Vector3.one;
				// Gravity Factor expresses how much the mesh direction aligns with the gravity plane.
				// A factor of 0 is parallel to the gravity or against-gravity vector.
				// A factor of 1 is perpendicular to the gravity or against gravity vector.
				float gravityFactor = 0f;

				if ((flag & APPLY_PIVOT) == APPLY_PIVOT) {
					Vector3 spPivot = (Vector3)pivot [i];
					ApplyOffset (vertexStart, vertexEnd, -spPivot);
				}

				// Apply the transformations.
				if (!applyTranslationAtEnd) {
					if ((flag & APPLY_OFFSET) == APPLY_OFFSET) {
						Vector3 spOffset = (Vector3)offset [i];
						ApplyOffset (vertexStart, vertexEnd, spOffset);
					}
				}
				if ((flag & APPLY_SCALE) == APPLY_SCALE) {
					Vector3 spScale = scale [i];
					ApplyScale (vertexStart, vertexEnd, spScale);
				}
				if ((flag & APPLY_ROTATION) == APPLY_ROTATION) {
					Quaternion spOrientation = orientation [i];

					spDirection = spOrientation * (gravityDirection * -1);
					gravityFactor = Vector3.Angle (gravityDirection, spDirection);
					if (gravityFactor > 90f) gravityFactor -= 180f;
					gravityFactor /= 90f;
					gravityFactor = Mathf.Abs(gravityFactor);
					//gravityFactor = Mathf.Sqrt(1 - Mathf.Pow (gravityFactor - 1, 2));
					gravityFactor = (1f - Mathf.Pow (1f - gravityFactor, 3f));

					ApplyRotation (vertexStart, vertexEnd, spOrientation, flag);
				}
				if (uv2s.Length > 0 && (flag & APPLY_BEND) == APPLY_BEND) {
					float spFBending = bending [i].x;
					float spSBending = bending [i].y;
					ApplyBend (vertexStart, vertexEnd, spFBending * gravityFactor, spSBending * gravityFactor, spDirection, flag);
				}
				if (applyTranslationAtEnd) {
					if ((flag & APPLY_OFFSET) == APPLY_OFFSET) {
						Vector3 spOffset = (Vector3)offset [i];
						ApplyOffset (vertexStart, vertexEnd, spOffset);
					}
				}
				if ((flag & APPLY_FLIP) == APPLY_FLIP) {
					bool spFlip = (bool)flip [i];
					ApplyFlip (vertexStart, vertexEnd, spFlip);
				}
				if ((flag & APPLY_IDS) == APPLY_IDS) {
					Vector4 spIds = ids [i];
					ApplyIds (vertexStart, vertexEnd, spIds);
				}
			}
			public void ApplyBend (int vertexStart, int vertexEnd, float fBend, float sBend, Vector3 targetDirection, int flags) {
				Vector3 forwardDirection = Vector3.ProjectOnPlane (targetDirection, gravityDirection);
				Quaternion fGravityQuaternion = Quaternion.FromToRotation (gravityDirection, forwardDirection);
				Quaternion fAntigravityQuaternion = Quaternion.FromToRotation (gravityDirection * -1, forwardDirection);
				Quaternion fBendQuaternion;
				Quaternion sGravityQuaternion = Quaternion.FromToRotation (gravityDirection, forwardDirection);
				Quaternion sAntigravityQuaternion = Quaternion.FromToRotation (gravityDirection * -1, forwardDirection);
				Quaternion sBendQuaternion;
				Quaternion bendQuaternion;
				float forwardStrength;
				float sideStrength;
				float bitan = 0;
				Plane fPlane = new Plane (forwardDirection, Vector3.zero);
				float distanceToFPlane = 0f;
				Vector4 tangent;
				for (int i = vertexStart; i < vertexEnd; i++) {
					forwardStrength = uv2s[i].x;
					
					forwardStrength -= 0.35f;
					if (forwardStrength > 0f) {
						forwardStrength = Mathf.Lerp (0f, 0.65f, forwardStrength);
					} else {
						forwardStrength = 0f;
					}

					forwardStrength *= fBend;

					distanceToFPlane = fPlane.GetDistanceToPoint (vertices [i]);
					if (forwardStrength > 0f)
						fBendQuaternion = Quaternion.SlerpUnclamped (Quaternion.identity, (distanceToFPlane < 0 ? fGravityQuaternion : fAntigravityQuaternion), forwardStrength);
					else
						fBendQuaternion = Quaternion.SlerpUnclamped (Quaternion.identity, (distanceToFPlane < 0 ? fAntigravityQuaternion : fGravityQuaternion), -forwardStrength); 
						
					sideStrength = sBend * uv2s[i].y;
					if (sideStrength > 0f)
						sBendQuaternion = Quaternion.Slerp (Quaternion.identity, (distanceToFPlane < 0 ? sGravityQuaternion : sAntigravityQuaternion), sideStrength);
					else
						sBendQuaternion = Quaternion.Slerp (Quaternion.identity, (distanceToFPlane < 0 ? sAntigravityQuaternion: sGravityQuaternion), -sideStrength);

					if (bendMode > 0) {
						if (bendMode == 2) {
							// STYLIZED bending mode.
							float lerpBend = Mathf.Atan2 (uv2s[i].y, uv2s[i].x) / (Mathf.PI * 0.5f);
							bendQuaternion = Quaternion.Lerp (fBendQuaternion, sBendQuaternion, lerpBend);		
						} else {
							// MULTIPLY bending mode.
							bendQuaternion = sBendQuaternion * fBendQuaternion;
						}
						vertices [i] = bendQuaternion * vertices [i];
						normals [i] = bendQuaternion * normals [i];
						bitan = tangents [i].w;
						tangent = bendQuaternion * tangents [i];
						tangent.w = bitan;
						tangents [i] = tangent;
						if ((flags & APPLY_UV5_TRANSFORM) == APPLY_UV5_TRANSFORM) {
							uv5s [i] = bendQuaternion * uv5s[i];
						}
						if ((flags & APPLY_UV6_TRANSFORM) == APPLY_UV6_TRANSFORM) {
							uv6s [i] = bendQuaternion * uv6s[i];
						}
						if ((flags & APPLY_UV7_TRANSFORM) == APPLY_UV7_TRANSFORM) {
							uv7s [i] = bendQuaternion * uv7s[i];
						}
						if ((flags & APPLY_UV8_TRANSFORM) == APPLY_UV8_TRANSFORM) {
							uv8s [i] = bendQuaternion * uv8s[i];
						}
					} else {
						// ADDITIVE bending mode.
						vertices [i] = sBendQuaternion * vertices [i];
						normals [i] = sBendQuaternion * normals [i];
						bitan = tangents [i].w;
						tangent = sBendQuaternion * tangents [i];
						tangent.w = bitan;
						tangents [i] = tangent;
						if ((flags & APPLY_UV5_TRANSFORM) == APPLY_UV5_TRANSFORM) {
							uv5s [i] = sBendQuaternion * uv5s[i];
						}
						if ((flags & APPLY_UV6_TRANSFORM) == APPLY_UV6_TRANSFORM) {
							uv6s [i] = sBendQuaternion * uv6s[i];
						}
						if ((flags & APPLY_UV7_TRANSFORM) == APPLY_UV7_TRANSFORM) {
							uv7s [i] = sBendQuaternion * uv7s[i];
						}
						if ((flags & APPLY_UV8_TRANSFORM) == APPLY_UV8_TRANSFORM) {
							uv8s [i] = sBendQuaternion * uv8s[i];
						}

						float sphereScale = Mathf.LerpUnclamped (1f, 1.35f, Mathf.Abs (forwardStrength));
						vertices [i] = fBendQuaternion * (vertices [i] / sphereScale);
						normals [i] = fBendQuaternion * normals [i];
						bitan = tangents [i].w;
						tangent = fBendQuaternion * tangents [i];
						tangent.w = bitan;
						tangents [i] = tangent;
						if ((flags & APPLY_UV5_TRANSFORM) == APPLY_UV5_TRANSFORM) {
							uv5s [i] = fBendQuaternion * uv5s[i];
						}
						if ((flags & APPLY_UV6_TRANSFORM) == APPLY_UV6_TRANSFORM) {
							uv6s [i] = fBendQuaternion * uv6s[i];
						}
						if ((flags & APPLY_UV7_TRANSFORM) == APPLY_UV7_TRANSFORM) {
							uv7s [i] = fBendQuaternion * uv7s[i];
						}
						if ((flags & APPLY_UV8_TRANSFORM) == APPLY_UV8_TRANSFORM) {
							uv8s [i] = fBendQuaternion * uv8s[i];
						}
					}
				}
			}
			public void ApplyScale (int vertexStart, int vertexEnd, Vector3 scale) {
				for (int i = vertexStart; i < vertexEnd; i++) {
					vertices [i] = Vector3.Scale (vertices [i], scale);
				}
			}
			public void ApplyRotation (
				int vertexStart, 
				int vertexEnd, 
				Quaternion orientation,
				int flags)
			{
				Vector4 tangent;
				float bitangent;
				for (int i = vertexStart; i < vertexEnd; i++) {
					vertices [i] = orientation * vertices [i];
					normals [i] = orientation * normals [i];
					if (includeTangents) {
						tangent = tangents [i];
						bitangent = tangent.w;
						tangents [i] = orientation * tangent;
						tangent.w = bitangent;
						tangents [i] = tangent;
					}
					if ((flags & APPLY_UV5_TRANSFORM) == APPLY_UV5_TRANSFORM)
						uv5s [i] = orientation * uv5s[i];
					if ((flags & APPLY_UV6_TRANSFORM) == APPLY_UV6_TRANSFORM)
						uv6s [i] = orientation * uv6s[i];
					if ((flags & APPLY_UV7_TRANSFORM) == APPLY_UV7_TRANSFORM)
						uv7s [i] = orientation * uv7s[i];
					if ((flags & APPLY_UV8_TRANSFORM) == APPLY_UV8_TRANSFORM)
						uv8s [i] = orientation * uv8s[i];
				}
			}
			public void ApplyOffset (int vertexStart, int vertexEnd, Vector3 offset) {
				for (int i = vertexStart; i < vertexEnd; i++) {
					vertices [i] = vertices [i] + offset;
				}
			}
			public void ApplyFlip (int vertexStart, int vertexEnd, bool flip) {
				for (int i = vertexStart; i < vertexEnd; i++) {
					if (flip) {
						normals [i] = normals [i] * -1;
						tangents [i] = tangents [i] * -1;
					}
				}
			}
			public void ApplyIds (int vertexStart, int vertexEnd, Vector4 ids) {
				for (int i = vertexStart; i < vertexEnd; i++) {
					uvIds [i] = ids; 
				}
			}
			#endregion
		}
		#endregion

		#region Constructor
		public MeshJob (bool applyTranslationAtEnd = true) {
			this.applyTranslationAtEnd = applyTranslationAtEnd;
		}
		#endregion

        #region Processing
		/// <summary>
        /// Clears Job related variables.
        /// </summary>
        public void Clear () {
			_flags.Clear ();
            _offsets.Clear ();
			_pivots.Clear ();
			_scales.Clear ();
            _rotations.Clear ();
			_flips.Clear ();
            _bendings.Clear ();
			_ids.Clear ();
            _starts.Clear ();
            _lengths.Clear ();
			ClearMesh ();
        }
        /// <summary>
        /// Clears Mesh related variables.
        /// </summary>
        protected void ClearMesh () {
            vertices.Clear ();
            normals.Clear ();
            tangents.Clear ();
			uv2s.Clear ();
			uv5s.Clear ();
			uv6s.Clear ();
			uv7s.Clear ();
			uv8s.Clear ();
            targetMesh = null;
        }
        public void SetTargetMesh (Mesh mesh) {
            ClearMesh ();
            targetMesh = mesh;
            vertices.AddRange (mesh.vertices);
            normals.AddRange (mesh.normals);
            tangents.AddRange (mesh.tangents);
			mesh.GetUVs (1, uv2s);
			if (applyUV5Transform)
				mesh.GetUVs (4, uv5s);
			if (applyUV6Transform)
				mesh.GetUVs (5, uv6s);
			if (applyUV7Transform)
				mesh.GetUVs (6, uv7s);
			if (applyUV8Transform)
				mesh.GetUVs (7, uv8s);
        }
        public Mesh GetTargetMesh () {
            return targetMesh;
        }
		public void AddTransform (
			int vertexStart, 
			int vertexLength, 
			Vector3 offset, 
			float scale, 
			Quaternion rotation,
			bool flip)
		{
			AddTransform (vertexStart, vertexLength, Vector3.zero, offset, new Vector3(scale, scale, scale), rotation, flip);
		}
		public void AddTransform (
			int vertexStart, 
			int vertexLength,
			Vector3 pivot,
			Vector3 offset, 
			float scale, 
			Quaternion rotation,
			bool flip = false)
		{
			AddTransform (vertexStart, vertexLength, pivot, offset, new Vector3(scale, scale, scale), rotation, flip);
		}
		public void AddTransform (
			int vertexStart,
			int vertexLength,
			Vector3 offset,
			Vector3 scale,
			Quaternion rotation,
			bool flip = false)
		{
			AddTransform (vertexStart, vertexLength, Vector3.zero, offset, scale, rotation, flip);
		}
        public void AddTransform (
			int vertexStart,
			int vertexLength,
			Vector3 pivot,
			Vector3 offset,
			Vector3 scale,
			Quaternion rotation,
			bool flip)
		{
            _starts.Add (vertexStart);
            _lengths.Add (vertexLength);
            _offsets.Add (offset);
			_pivots.Add (pivot);
			_scales.Add (scale);
            _rotations.Add (rotation);
			_flips.Add (flip);
            _bendings.Add (Vector2.zero);
			_ids.Add (Vector4.zero);
			int flag = 0;
			if (offset != Vector3.zero) flag |= MeshJobImpl.APPLY_OFFSET;
			if (scale != Vector3.one) flag |= MeshJobImpl.APPLY_SCALE;
			if (rotation != Quaternion.identity) flag |= MeshJobImpl.APPLY_ROTATION;
			if (pivot != Vector3.zero) flag |= MeshJobImpl.APPLY_PIVOT;
			if (flip) flag |= MeshJobImpl.APPLY_FLIP;
			if (applyUV5Transform) flag |= MeshJobImpl.APPLY_UV5_TRANSFORM;
			if (applyUV6Transform) flag |= MeshJobImpl.APPLY_UV6_TRANSFORM;
			if (applyUV7Transform) flag |= MeshJobImpl.APPLY_UV7_TRANSFORM;
			if (applyUV8Transform) flag |= MeshJobImpl.APPLY_UV8_TRANSFORM;
			_flags.Add (flag);

        }
		public void IncludeBending (float forwardBending, float sideBending) {
			int lastIndex = _flags.Count - 1;
			_bendings[lastIndex] = new Vector2(forwardBending, sideBending);
			_flags [lastIndex] |= MeshJobImpl.APPLY_BEND;
		}
		public void IncludeIds (int group, int subgroup = -1) {
			int lastIndex = _flags.Count - 1;
			_ids [lastIndex] = new Vector4 (group, subgroup, 0f, 0f);
			_flags [lastIndex] |= MeshJobImpl.APPLY_IDS;
		}
        public void ExecuteJob () {
			// Mark the mesh as dynamic.
			targetMesh.MarkDynamic ();
			// Create the job.
			MeshJobImpl _meshJob = new MeshJobImpl () {
				includeTangents = includeTangents,
				applyTranslationAtEnd = applyTranslationAtEnd,
				gravityForward = Vector3.forward,
				gravityRight = Vector3.right,
				gravityUp = Vector3.up,
				gravityDirection = gravityDirection,
				bendMode = (int)bendMode,
				offset = new NativeArray<Vector3> (_offsets.ToArray (), Allocator.TempJob),
				pivot = new NativeArray<Vector3> (_pivots.ToArray (), Allocator.TempJob),
				scale = new NativeArray<Vector3> (_scales.ToArray (), Allocator.TempJob),
				orientation = new NativeArray<Quaternion> (_rotations.ToArray (), Allocator.TempJob),
				flip = new NativeArray<bool> (_flips.ToArray (), Allocator.TempJob),
				bending = new NativeArray<Vector2> (_bendings.ToArray (), Allocator.TempJob),
				ids = new NativeArray<Vector4> (_ids.ToArray (), Allocator.TempJob),
				start = new NativeArray<int> (_starts.ToArray (), Allocator.TempJob),
				length = new NativeArray<int> (_lengths.ToArray (), Allocator.TempJob),
				flags = new NativeArray<int> (_flags.ToArray (), Allocator.TempJob),
				vertices = new NativeArray<Vector3> (vertices.ToArray (), Allocator.TempJob),
				normals = new NativeArray<Vector3> (normals.ToArray (), Allocator.TempJob),
				tangents = new NativeArray<Vector4> (tangents.ToArray (), Allocator.TempJob),
				uvIds = new NativeArray<Vector4> (new Vector4[(applyIdsChannel>0?vertices.Count:0)], Allocator.TempJob),
				uv2s = new NativeArray<Vector4> (uv2s.ToArray (), Allocator.TempJob),
				uv5s = new NativeArray<Vector3> (uv5s.ToArray (), Allocator.TempJob),
				uv6s = new NativeArray<Vector3> (uv6s.ToArray (), Allocator.TempJob),
				uv7s = new NativeArray<Vector3> (uv7s.ToArray (), Allocator.TempJob),
				uv8s = new NativeArray<Vector3> (uv8s.ToArray (), Allocator.TempJob)
			};
			// Execute the job .
			JobHandle _meshJobHandle = _meshJob.Schedule (_offsets.Count, batchSize);

			// Complete the job.
			_meshJobHandle.Complete();

			targetMesh.SetVertices (_meshJob.vertices);
			targetMesh.SetNormals (_meshJob.normals);
			targetMesh.SetTangents (_meshJob.tangents);
			if (applyIdsChannel > 0) {
				targetMesh.SetUVs (applyIdsChannel, _meshJob.uvIds);
			}
			if (applyUV5Transform)
				targetMesh.SetUVs (4, _meshJob.uv5s);
			if (applyUV6Transform)
				targetMesh.SetUVs (5, _meshJob.uv6s);
			if (applyUV7Transform)
				targetMesh.SetUVs (6, _meshJob.uv7s);
			if (applyUV8Transform)
				targetMesh.SetUVs (7, _meshJob.uv8s);
			//targetMesh.UploadMeshData (true);

			// Dispose allocated memory.
			_meshJob.offset.Dispose ();
			_meshJob.pivot.Dispose ();
			_meshJob.scale.Dispose ();
			_meshJob.orientation.Dispose ();
			_meshJob.flip.Dispose ();
			_meshJob.bending.Dispose ();
			_meshJob.ids.Dispose ();
			_meshJob.start.Dispose ();
			_meshJob.length.Dispose ();
			_meshJob.flags.Dispose ();
			_meshJob.vertices.Dispose ();
			_meshJob.normals.Dispose ();
			_meshJob.tangents.Dispose ();
			_meshJob.uv2s.Dispose ();
			_meshJob.uv5s.Dispose ();
			_meshJob.uv6s.Dispose ();
			_meshJob.uv7s.Dispose ();
			_meshJob.uv8s.Dispose ();
			_meshJob.uvIds.Dispose ();
        }
		public static void ApplyMeshGradient (Mesh mesh, Bounds bounds) {
			List<Vector4> uv2s = new List<Vector4> ();
			List<Vector3> vertices = new List<Vector3> ();
			mesh.GetVertices (vertices);
			int totalVertices = mesh.vertexCount;
			float fGradient = 0f;
			float sGradient = 0f;
			for (int i = 0; i < totalVertices; i++) {
				fGradient = vertices[i].y / bounds.max.y;
				sGradient = Mathf.Abs (vertices[i].z / bounds.max.z);
				uv2s.Add (new Vector4 (fGradient, sGradient, fGradient, sGradient));
			}
			mesh.SetUVs (1, uv2s);
		}
		public static float GetGravityFactory (Quaternion orientation) {
			Vector3 direction = orientation * (gravityDirection * -1);
			float gravityFactor = Vector3.Angle (gravityDirection, direction);
			if (gravityFactor > 90f) gravityFactor -= 180f;
			gravityFactor /= 90f;
			gravityFactor = Mathf.Abs(gravityFactor);
			// EaseOutCirc
			//return Mathf.Sqrt(1 - Mathf.Pow (gravityFactor - 1, 2));
			// EaseOutCubic
			return (1f - Mathf.Pow (1f - gravityFactor, 3f));
		}
        #endregion
    }   
}
