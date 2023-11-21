using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

using Broccoli.Model;

namespace Broccoli.Utils
{
	/// <summary>
	/// Class to apply scale, position offset, rotation and bending to a list of target BezierCurve instances.
	/// </summary>
    public class CurveJob : MeshJob {
        #region Vars
		public List<BezierCurve> curves = new List<BezierCurve> ();
		public List<Bounds> bounds = new List<Bounds> ();
		private List<int> _nodeStartIndex = new List<int> ();
		private List<int> _pointStartIndex = new List<int> ();
		private List<int> _sampleStartIndex = new List<int> ();
		private List<Vector3> _vertices = new List<Vector3> ();
		private List<Vector3> _normals = new List<Vector3> ();
		private List<Vector3> _uv5s = new List<Vector3> ();
		private List<Vector3> _uv6s = new List<Vector3> ();
		private List<Vector3> _uv7s = new List<Vector3> ();
		private Mesh _targetMesh = null;
		#endregion

		#region Constructor
		public CurveJob () : base() {
			applyUV5Transform = true;
			applyUV6Transform = true;
			applyUV7Transform = true;
		}
		#endregion

		#region Processing
		public void AddTransform (
			BezierCurve curve,
			Bounds curveBounds,
			Vector3 offset,
			float scale,
			Quaternion rotation,
			bool flip = false)
		{
			AddTransform (curve, curveBounds, Vector3.zero, Vector3.zero, scale, rotation, flip); 
		}
		public void AddTransform (
			BezierCurve curve,
			Bounds curveBounds,
			Vector3 pivot,
			Vector3 offset,
			float scale,
			Quaternion rotation,
			bool flip = false)
		{
			int _vertexStart = _vertices.Count; 
			// Add curve.
			curves.Add (curve);
			// Add bounds.
			bounds.Add (curveBounds);

			// Add curve vector reference as first vertex.
			_vertices.Add (Vector3.zero);
			_normals.Add (curve.referenceNormal);
			_uv5s.Add (curve.referenceForward);
			_uv6s.Add (curve.fixedNormal);
			_uv7s.Add (Vector3.zero);

			// Add curve nodes and handles and save its nodes start index reference.
			_nodeStartIndex.Add (_vertices.Count);
			BezierNode node;
			for (int nodeI = 0; nodeI < curve.nodeCount; nodeI++) {
				node = curve.nodes [nodeI];
				_vertices.Add (node.position);
				_normals.Add (node.direction);
				_uv5s.Add (node.up);
				_uv6s.Add (node.handle1);
				_uv7s.Add (node.handle2);
			}

			// Add curve points and save its points start index reference.
			_pointStartIndex.Add (_vertices.Count);
			CurvePoint point;
			for (int pointI = 0; pointI < curve.points.Count; pointI++) {
				point = curve.points [pointI];
				_vertices.Add (point.position);
				_normals.Add (point.normal);
				_uv5s.Add (point.forward);
				_uv6s.Add (point.bitangent);
				_uv7s.Add (point.tangent);
			}

			// Add curve samples and save its points start index reference.
			_sampleStartIndex.Add (_vertices.Count);
			CubicBezierCurve cubicCurve;
			CurvePoint sample;
			for (int i = 0; i < curve.bezierCurves.Count; i++) {
				cubicCurve = curve.bezierCurves [i];
				for (int sampleI = 0; sampleI < cubicCurve.samples.Count; sampleI++) {
					sample = cubicCurve.samples [sampleI];
					_vertices.Add (sample.position);
					_normals.Add (sample.normal);
					_uv5s.Add (sample.forward);
					_uv6s.Add (sample.bitangent);
					_uv7s.Add (sample.tangent);
				}
			}

			int _vertexLength = _vertices.Count - _vertexStart;
			AddTransform (_vertexStart, _vertexLength, pivot, offset, scale, rotation, flip);
		}
		public new void ExecuteJob () {
			includeTangents = false; 
			applyUV5Transform = true;
			applyUV6Transform = true;
			applyUV7Transform = true;
			
			// Buil target mesh.
			if (_targetMesh == null) _targetMesh = new Mesh();
			_targetMesh.Clear (false);
			_targetMesh.SetVertices (_vertices);
			_targetMesh.SetNormals (_normals);
			_targetMesh.SetUVs (4, _uv5s);
			_targetMesh.SetUVs (5, _uv6s);
			_targetMesh.SetUVs (6, _uv7s);
			_targetMesh.RecalculateTangents ();
			if (bounds.Count > 0)
				MeshJob.ApplyMeshGradient (_targetMesh, bounds[0]);

			// Set target mesh.
			base.SetTargetMesh (_targetMesh);

			// Execute Job.
			base.ExecuteJob ();

			// Set mesh data back to curves.
			_targetMesh.GetVertices (_vertices);
			_targetMesh.GetNormals (_normals);
			_targetMesh.GetUVs (4, _uv5s);
			_targetMesh.GetUVs (5, _uv6s);
			_targetMesh.GetUVs (6, _uv7s);

			BezierCurve curve;
			BezierNode node;
			CurvePoint point;
			int nodesIndex = 0;
			int pointsIndex = 0;
			int samplesIndex = 0;
			for (int curveI = 0; curveI < curves.Count; curveI++){
				curve = curves[curveI];
				nodesIndex = _nodeStartIndex [curveI];
				pointsIndex = _pointStartIndex [curveI];
				samplesIndex = _sampleStartIndex [curveI];
				// Set cure vector references.
				curve.referenceNormal = _normals [nodesIndex - 1];
				curve.referenceForward = _uv5s [nodesIndex - 1];
				curve.fixedNormal = _uv6s [nodesIndex - 1];
				// Set curve nodes.
				for (int nodeI = 0; nodeI < curve.nodeCount; nodeI++) {
					node = curve.nodes [nodeI];
					node.listenEvents = false;
					node.position = _vertices [nodesIndex + nodeI];
					node.up = _normals [nodesIndex + nodeI];
					node.direction = _uv5s [nodesIndex + nodeI];
					node.handle1 = _uv6s [nodesIndex + nodeI];
					node.handle2 = _uv7s [nodesIndex + nodeI];
					node.listenEvents = true;
				}
				// Set curve points.
				for (int pointI = 0; pointI < curve.points.Count; pointI++) {
					point = curve.points [pointI];
					point.position = _vertices [pointsIndex + pointI];
					point.normal = _normals [pointsIndex + pointI]; 
					point.forward = _uv5s [pointsIndex + pointI];
					point.bitangent = _uv6s [pointsIndex + pointI];
					point.tangent = _uv7s [pointsIndex + pointI];
				}
				// Set sample points.
				CubicBezierCurve cubicCurve;
				int intraSampleIndex = 0;
				for (int cubicCurveI = 0; cubicCurveI < curve.bezierCurves.Count; cubicCurveI++) {
					cubicCurve = curve.bezierCurves [cubicCurveI];
					for (int sampleI = 0; sampleI < cubicCurve.samples.Count; sampleI++) {
						point = cubicCurve.samples [sampleI];
						point.position = _vertices [samplesIndex + intraSampleIndex];
						point.normal = _normals [samplesIndex + intraSampleIndex]; 
						point.forward = _uv5s [samplesIndex + intraSampleIndex];
						point.bitangent = _uv6s [samplesIndex + intraSampleIndex];
						point.tangent = _uv7s [samplesIndex + intraSampleIndex];
						intraSampleIndex++;
					}
				}
			}
		}
		private new void AddTransform (
			int vertexStart, 
			int vertexLength, 
			Vector3 offset, 
			float scale, 
			Quaternion rotation,
			bool flip = false)
		{
			base.AddTransform (vertexStart, vertexLength, offset, new Vector3(scale, scale, scale), rotation, flip);
		}
        private new void AddTransform (
			int vertexStart, 
			int vertexLength, 
			Vector3 offset, 
			Vector3 scale, 
			Quaternion rotation,
			bool flip = false) 
		{
            base.AddTransform (vertexStart, vertexLength, offset, scale, rotation, flip);
        }
		private new void SetTargetMesh (Mesh mesh) {
			base.SetTargetMesh (mesh);
		}
		private new Mesh GetTargetMesh () {
			return base.GetTargetMesh ();
		}
		public new void Clear () {
			base.Clear();
			curves.Clear ();
			_nodeStartIndex.Clear ();
			_pointStartIndex.Clear ();
			_sampleStartIndex.Clear ();
			_vertices.Clear ();
			_normals.Clear ();
			_uv5s.Clear ();
			_uv6s.Clear ();
			_uv7s.Clear ();
			if (_targetMesh != null)
				_targetMesh.Clear (false);
		}
		#endregion
    }   
}