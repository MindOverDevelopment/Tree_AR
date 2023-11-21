﻿﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace Broccoli.BroccoEditor
{
	/// <summary>
	/// Editor utility class to preview meshes on custom editors.
	/// </summary>
	public class MeshPreview
	{
		#region Config Vars
		/// <summary>
		/// Does autozoom on the first mesh added to a viewport.
		/// </summary>
		public bool autoZoomEnabled = false;
		/// <summary>
		/// Flag to enable the free view controls.
		/// </summary>
		public bool freeViewEnabled = true;
		/// <summary>
		/// Flag to enable the zoom controls.
		/// </summary>
		public bool zoomEnabled = true;
		/// <summary>
		/// Offset to apply to the target rotation.
		/// </summary>
		public Quaternion targetRotationOffset = Quaternion.identity;
		/// <summary>
		/// Offset to apply to the target position.
		/// </summary>
		public Vector3 targetPositionOffset = Vector3.zero;
		/// <summary>
		/// Minimum zoom factor value.
		/// </summary>
		public float minZoomFactor = 0.3f;
		/// <summary>
		/// Maximum zoom factor value.
		/// </summary>
		public float maxZoomFactor = 10f;
		/// <summary>
		/// The preview has a second pass.
		/// </summary>
		public bool hasSecondPass = false;
		/// <summary>
		/// Second pass materials.
		/// </summary>
		public Material[] secondPassMaterials;
		/// <summary>
		/// Blend modes available for the second pass.
		/// </summary>
		public enum SecondPassBlend {
			BlendNormal       = 0,
			BlendLighten      = 1,
			BlendDarken       = 2,
			BlendMultiply     = 3,
			BlendAverage      = 4,
			BlendAdd          = 5,
			BlendSubstract    = 6,
			BlendDifference   = 7,
			BlendNegation     = 8,
			BlendExclusion    = 9,
			BlendScreen       = 10,
			BlendOverlay      = 11,
			BlendSoftLight    = 12,
			BlendHardLight    = 13,
			BlendColorDodge   = 14,
			BlendColorBurn    = 15,
			BlendLinearDodge  = 16,
			BlendLinearBurn   = 17,
			BlendLinearLight  = 18,
			BlendVividLight   = 19,
			BlendPinLight     = 20,
			BlendHardMix      = 21,
			BlendReflect      = 22,
			BlendGlow         = 23,
			BlendPhoenix      = 24,
			BlendOpacity      = 25,
			BlendHue          = 26,
			BlendSaturation   = 27,
			BlendColor        = 28,
			BlendColorAlpha   = 31, 
			BlendLuminosity   = 29,
			BlendBlend   = 30
		}
		private SecondPassBlend _secondPassBlend = SecondPassBlend.BlendNormal;
		/// <summary>
		/// Second pass blend.
		/// </summary>
		public SecondPassBlend secondPassBlend {
			get { return _secondPassBlend; }
			set {
				_secondPassBlend = value;
				if (blendMaterial != null) {
					blendMaterial.SetFloat ("_BlendMode", (float)_secondPassBlend);
				}
			}
		}
		/// <summary>
		/// Second pass background color.
		/// </summary>
		public Color secondPassBackgroundColor = Color.black;
		/// <summary>
		/// Display a plane mesh. Set through a method.
		/// </summary>
		private bool _showPlane = false;
		/// <summary>
		/// Texture to use on the plane mesh.
		/// </summary>
		public Texture2D planeTexture = null;
		/// <summary>
		/// Scale to draw the plane mesh.
		/// </summary>
		private Vector3 _planeScale = Vector3.zero;
		/// <summary>
		/// Color to use on the plane mesh material.
		/// </summary>
		private Color _planeTint = Color.white;
		/// <summary>
		/// Material used for blending.
		/// </summary>
		private Material blendMaterial = null;
		private bool _fogEnabled = true;
		private RenderPipelineAsset _graphRP = null;
		private RenderPipelineAsset _qualityRP = null;
		private LightingDataAsset _lightData = null;
		private Vector3 _rulerOrigin = Vector3.zero;
		private Vector3 _rulerDirection = Vector3.up;
		#endregion

		#region Debug Vars
		/// <summary>
		/// Flag to display debug information about the preview parameters.
		/// </summary>
		public bool debugShowDebugInfo = false;
		/// <summary>
		/// Show the normals for the displayed meshes.
		/// </summary>
		public bool debugShowNormals = false;
		/// <summary>
		/// Length of the normal handles for the displayed meshes.
		/// </summary>
		public float debugNormalsLength = 0.5f;
		/// <summary>
		/// Color of the normal handles for the displayed meshes.
		/// </summary>
		public Color debugNormalsColor = Color.red;
		/// <summary>
		/// Show the tangents for the displayed meshes.
		/// </summary>
		public bool debugShowTangents = false;
		/// <summary>
		/// Length of the tangent handles for the displayed meshes.
		/// </summary>
		public float debugTangentsLength = 0.5f;
		/// <summary>
		/// Color of the tangent handles for the displayed meshes.
		/// </summary>
		public Color debugTangentsColor = Color.Lerp (Color.red, Color.blue, 0.5f);
		#endregion

		#region Style Vars
		/// <summary>
		/// Background color to use on the camera. Defaults to transparent.
		/// </summary>
		/// <returns>Camera background color.</returns>
		public Color backgroundColor = new Color (1f, 1f, 1f, 0f);
		/// <summary>
		/// Show a title for the current mesh preview.
		/// </summary>
		public bool showPreviewTitle = false;
		/// <summary>
		/// Title for the current mesh preview.
		/// </summary>
		public string previewTitle = "Mesh Preview";
		/// <summary>
		/// Shows the triangles and vertices count on the preview.
		/// </summary>
		public bool showTrisCount = true;
		/// <summray>
		/// Draw the mesh using wireframe mode.
		/// </summary>
		public bool showWireframe = false;
		/// <summray>
		/// Draws a pivot dot.
		/// </summary>
		public bool showPivot = false;
		/// <summray>
		/// Position for the pivot.
		/// </summary>
		public Vector3 pivotPosition = Vector3.zero;
		/// <summary>
		/// Draw arrow handles at each axis direction.
		/// </summary>
		public bool showAxis = true;
		/// <summary>
		/// Draws a ruler next to the generated mesh.
		/// </summary>
		public bool showRuler = false;
		/// <summary>
		/// Color to use when drawing the ruler.
		/// </summary>
		public Color rulerColor = Color.white;
		/// <summary>
		/// How many units the rule displays a whole unit.
		/// </summary>
		public float rulerUnit= 1f;
		/// <summary>
		/// How many divisions per units.
		/// </summary>
		public int rulerSubUnits = 2;
		/// <summary>
		/// Position to draw the axis handles.
		/// </summary>
		public Vector3 axisGizmoPos = Vector3.zero;
		/// <summary>
		/// Size for the axis handles.
		/// </summary>
		public float axisGizmoSize = 0.5f;
		/// <summray>
		/// Size used for the handles.
		/// </summary>
		public float handlesSize = 0.2f;
		/// <summray>
		/// Color for the pivot handle.
		/// </summary>
		public Color pivotHandleColor = Color.yellow;
		#endregion

		#region Vars
		/// <summary>
		/// The meshes.
		/// </summary>
		private List<Mesh> _meshes = new List<Mesh> ();
		/// <summary>
		/// Max height found on the displaying meshes.
		/// </summary>
		private float _maxMeshHeight = 0f;
		/// <summary>
		/// Min height found on the displaying meshes.
		/// </summary>
		private float _minMeshHeight = 0f;
		/// <summary>
		/// Min width on the z axis found on the displaying meshes.
		/// </summary>
		private float _minMeshWidth = 0f;
		/// <summary>
		/// The materials.
		/// </summary>
		private List<Material> _materials = new List<Material> ();
		/// <summary>
		/// Light A on the preview.
		/// </summary>
		private Light _lightA = new Light ();
		/// <summary>
		/// Light B on the preview.
		/// </summary>
		private Light _lightB = new Light ();
		/// <summary>
		/// The default materials.
		/// </summary>
		private List<Material> _defaultMaterials = new List<Material> ();
		/// <summary>
		/// The mesh to viewport.
		/// </summary>
		private Dictionary<int, int> _meshToViewport = new Dictionary<int, int> ();
		/// <summary>
		/// The mesh tris count.
		/// </summary>
		private Dictionary<int, bool> _meshTrisCount = new Dictionary<int, bool> ();
		/// <summary>
		/// The viewport names.
		/// </summary>
		private List<string> _viewportNames = new List<string> ();
		/// <summary>
		/// The default material.
		/// </summary>
		private Material _defaultMaterial = null;
		/// <summary>
		/// The selected viewport.
		/// </summary>
		private int _selectedViewport = -1;
		/// <summary>
		/// The preview render utility.
		/// </summary>
		private PreviewRenderUtility _previewRenderUtility;
		public Camera camera {
			get { return _previewRenderUtility.camera; }
		}
		/// <summary>
		/// Camera position.
		/// </summary>
		Vector3 camPos = Vector3.zero;
		/// <summary>
		/// The avatar scale.
		/// </summary>
		private float m_AvatarScale = 1.0f;
		/// <summary>
		/// The zoom factor.
		/// </summary>
		private float m_ZoomFactor = 1.0f;
		/// <summary>
		/// The preview string.
		/// </summary>
		const string s_PreviewStr = "Mesh Preview";
		/// <summary>
		/// The preview direction.
		/// </summary>
		//private Vector2 m_PreviewDir = new Vector2 (120, -20);
		private Vector2 m_PreviewDir = new Vector3 (90, 0);
		/// <summary>
		/// Offset for camera.
		/// </summary>
		/// <returns>Offset.</returns>
		private Vector3 m_PreviewOffset = new Vector3 (0f, 0f, -5.5f);
		/// <summary>
		/// The preview hint.
		/// </summary>
		private int m_PreviewHint = s_PreviewStr.GetHashCode();
		/// <summary>
		/// The text style.
		/// </summary>
		private GUIStyle textStyle = new GUIStyle ();
		/// <summary>
		/// The tris count.
		/// </summary>
		private int _trisCount = 0;
		/// <summary>
		/// The verts count.
		/// </summary>
		private int _vertsCount = 0;
		/// <summary>
		/// String used to display debug information.
		/// </summary>
		private string debugInfo = string.Empty;
		/// <summary>
		/// Current pass.
		/// </summary>
		private int currentPass = 1;
		/// <summary>
		/// Texture for the first texture.
		/// </summary>
		private RenderTexture firstPassTex = null;
		/// <summary>
		/// Texture for blending.
		/// </summary>
		private RenderTexture blendTex = null;
		/// <summary>
		/// Plane mesh.
		/// </summary>
		private Mesh _planeMesh = null;
		/// <summary>
		/// Plane rotation.
		/// </summary>
		private Quaternion _planeRotation = Quaternion.AngleAxis (-90f, Vector3.up);
		/// <summary>
		/// Plane material.
		/// </summary>
		private Material _planeMaterial = null;
		#endregion

		#region Events
		/// <summray>
		/// DrawExtras delegate definition.
		/// </summary>
		public delegate void DrawExtras (Rect r, Camera camera);
		/// <summary>
		/// Repaint related events.
		/// </summary>
		public delegate void RepaintEvent ();
		/// <summray>
		/// DrawExtras multidelegate for handles.
		/// </summary>
		public DrawExtras onDrawHandles;
		/// <summray>
		/// DrawExtras multidelegate for GUI.
		/// </summary>
		public DrawExtras onDrawGUI;
		/// <summary>
		/// Called when the preview requires redraw.
		/// </summary>
		public RepaintEvent onRequiresRepaint;
		#endregion

		#region Pool
		/// <summary>
		/// Pool of mesh previews.
		/// </summary>
		/// <typeparam name="string">Id for the mesh preview.</typeparam>
		/// <typeparam name="MeshPreview">Mesh Preview object.</typeparam>
		/// <returns>MeshPreview instance.</returns>
		private static Dictionary<string, MeshPreview> _instances = new Dictionary<string, MeshPreview> ();
		/// <summary>
		/// Gets an instance of MeshPreview.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>MeshPreview instance.</returns>
		public static MeshPreview GetInstance (string id) {
			if (_instances.ContainsKey (id)) {
				if (_instances [id] == null) {
					_instances [id] = new MeshPreview ();
				}
				return _instances [id];
			}
			MeshPreview instance = new MeshPreview ();
			_instances.Add (id, instance);
			return instance;
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Broccoli.TreeNodeEditor.MeshPreview"/> class.
		/// </summary>
		protected MeshPreview () {
			// Init PreviewRenderUtility
			_previewRenderUtility = new PreviewRenderUtility ();

			//We set the previews camera to 6 units back, look towards the middle of the 'scene'
			_previewRenderUtility.camera.transform.position = new Vector3 (0, 0, -8);
			_previewRenderUtility.camera.transform.rotation = Quaternion.identity;
			_previewRenderUtility.ambientColor = new Color (0.5f, 0.5f, 0.5f, 1f);

			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
			Camera.onPostRender -= OnPostRender;
			Camera.onPostRender += OnPostRender; 

			// Lighting
			_lightA = _previewRenderUtility.lights[0];
			_lightA.intensity = 1f;
			_lightA.shadows = LightShadows.Hard;
			_lightA.enabled = true;
			_lightA.transform.rotation = Quaternion.Euler(90f, 90f, 0f);
			_lightB = _previewRenderUtility.lights[1];
			_lightB.intensity = 1f;
			_lightB.shadows = LightShadows.Hard;
			_lightB.transform.rotation = Quaternion.Euler(90f, 90f, 0f); 
			_lightB.enabled = true;

			// Init preview default material.
			_defaultMaterial = new Material (Shader.Find ("Diffuse"));

			// Style
			textStyle.normal.textColor = Color.white;
		}
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy () {
			Camera.onPostRender -= OnPostRender;
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			Object.DestroyImmediate (_defaultMaterial);
			_previewRenderUtility.Cleanup();
			Object.DestroyImmediate (_planeMesh);
			Object.DestroyImmediate (_planeMaterial);
			planeTexture = null;
			if (firstPassTex != null)
				RenderTexture.ReleaseTemporary (firstPassTex);
			if (blendTex != null)
				RenderTexture.ReleaseTemporary (blendTex);
		}
		#endregion

		#region Management Mesh, Lights, Viewports.
		/// <summary>
		/// Creates a viewport.
		/// </summary>
		/// <returns>The viewport index.</returns>
		/// <param name="name">Name for the viewport.</param>
		public int CreateViewport (string name = "mesh") {
			_viewportNames.Add (name);
			return _viewportNames.Count - 1;
		}
		/// <summary>
		/// Gets the Mesh being previewed in the viewport.
		/// </summary>
		/// <param name="index">Index for the mesh.</param>
		/// <returns>The mesh at the preview index, null if none is found.</returns>
		public Mesh GetMesh (int index = 0) {
			if (index >= 0 && index < _meshes.Count) {
				return _meshes [index];
			}
			return null;
		}
		/// <summary>
		/// Adds a mesh to the viewport.
		/// </summary>
		/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
		/// <param name="viewportIndex">Viewport index.</param>
		/// <param name="mesh">Mesh.</param>
		/// <param name="countTris">If set to <c>true</c> count tris.</param>
		public bool AddMesh (int viewportIndex, Mesh mesh, bool countTris = false) {
			return AddMesh (viewportIndex, mesh, null, countTris);
		}
		/// <summary>
		/// Adds a mesh to the viewport.
		/// </summary>
		/// <returns><c>true</c>, if mesh was added, <c>false</c> otherwise.</returns>
		/// <param name="viewportIndex">Viewport index.</param>
		/// <param name="mesh">Mesh.</param>
		/// <param name="material">Material.</param>
		/// <param name="countTris">If set to <c>true</c> count tris.</param>
		public bool AddMesh (int viewportIndex, Mesh mesh, Material material = null, bool countTris = false) {
			if (viewportIndex < _viewportNames.Count) {
				_meshes.Add (mesh);
				if (material == null) {
					if (_defaultMaterial == null) _defaultMaterial = new Material (Shader.Find ("Diffuse")); 
					material = Object.Instantiate (_defaultMaterial);
					_defaultMaterials.Add (material);
				}
				_materials.Add (material);
				bool autoZoom = false;
				if (!_meshToViewport.ContainsValue (viewportIndex)) {
					autoZoom = true;
				}
				_meshToViewport.Add (_meshes.Count - 1, viewportIndex);
				_meshTrisCount.Add (_meshes.Count - 1, countTris);
				if (_selectedViewport == -1) {
					_selectedViewport = 0;
				}
				if (autoZoom && autoZoomEnabled) {
					CalculateZoom (mesh);
				}
				// Calculate min and max all meshes height.
				_minMeshHeight = 0f;
				_maxMeshHeight = 0f;
				_minMeshWidth = 0f;
				for (int i = 0; i < _meshes.Count; i++) {
					if (_meshes [i].bounds.max.y > _maxMeshHeight) {
						_maxMeshHeight = _meshes [i].bounds.max.y;
					}
					if (_meshes [i].bounds.min.y < _minMeshHeight) {
						_minMeshHeight = _meshes [i].bounds.min.y;
					}
					if (_meshes [i].bounds.min.z < _minMeshWidth) {
						_minMeshWidth = _meshes [i].bounds.min.z;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Sets Light A intensity and transform rotation.
		/// </summary>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		public void SetLightA (float intensity, Quaternion quaternion) {
			SetLight (_lightA, intensity, quaternion);
		}
		/// <summary>
		/// Sets Light B intensity and transform rotation.
		/// </summary>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		public void SetLightB (float intensity, Quaternion quaternion) {
			SetLight (_lightB, intensity, quaternion);
		}
		/// <summary>
		/// Gets the light A on this instance.
		/// </summary>
		/// <returns>Light A.</returns>
		public Light GetLightA () {
			return _lightA;
		}
		/// <summary>
		/// Gets the light B on this instance.
		/// </summary>
		/// <returns>Light B.</returns>
		public Light GetLightB () {
			return _lightB;
		}
		/// <summary>
		/// Sets a ligth intensity and transform rotation.
		/// </summary>
		/// <param name="light">Light to set values.</param>
		/// <param name="intensity">Light intensity.</param>
		/// <param name="quaternion">Transform rotation.</param>
		void SetLight (Light light, float intensity, Quaternion quaternion) {
			light.intensity = intensity;
			light.transform.rotation = quaternion;
		}
		/// <summary>
		/// Selects the viewport for rendering.
		/// </summary>
		/// <returns><c>true</c>, if viewport was selected, <c>false</c> otherwise.</returns>
		/// <param name="index">Index.</param>
		public bool SelectViewport (int index) {
			if (index >= 0 && index < _viewportNames.Count) {
				_selectedViewport = index;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Gets the viewport count.
		/// </summary>
		/// <returns>The viewport count.</returns>
		public int GetViewportCount () {
			return _viewportNames.Count;
		}
		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear () {
			_meshes.Clear ();
			_materials.Clear ();
			_defaultMaterials.Clear ();
			for (int i = 0; i < _defaultMaterials.Count; i++) {
				Object.DestroyImmediate (_defaultMaterials [i]);
			}
			_meshToViewport.Clear ();
			_viewportNames.Clear ();
			_meshTrisCount.Clear ();
			_selectedViewport = -1;
		}
		#endregion

		#region Rendering
		private void PreRenderViewport (Rect r, GUIStyle background) {
			// Handle preview GUI.
			int previewID = GUIUtility.GetControlID(m_PreviewHint, FocusType.Passive, r);
			Event evt = Event.current;
			EventType type = evt.GetTypeForControl(previewID);
			if (r.Contains (evt.mousePosition)) {
				HandleViewTool (evt, type, previewID, r);
			}

			// Draw if event is REPAINT,  otherwise return.
			if (Event.current.type != EventType.Repaint) return;
			// Begin preview
			_previewRenderUtility.BeginPreview (r, GUIStyle.none);

			if (currentPass == 2) {
				_previewRenderUtility.camera.backgroundColor = secondPassBackgroundColor;;
				_previewRenderUtility.camera.clearFlags = CameraClearFlags.Color;
			} else {
				_previewRenderUtility.camera.backgroundColor = backgroundColor;
				_previewRenderUtility.camera.clearFlags = CameraClearFlags.Color;
			}
			_previewRenderUtility.camera.renderingPath = RenderingPath.Forward;
			_previewRenderUtility.camera.clearStencilAfterLightingPass = true;
			_previewRenderUtility.camera.nearClipPlane = 0.5f * m_ZoomFactor;
			_previewRenderUtility.camera.farClipPlane = 200.0f * m_AvatarScale;

			// Position
			Quaternion camRot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0);
			camPos = camRot * (m_PreviewOffset * m_ZoomFactor);
			_previewRenderUtility.camera.transform.position = camPos;
			_previewRenderUtility.camera.transform.rotation = camRot;

			// we are technically rendering everything in the scene, so scene fog might affect it...
            _fogEnabled = RenderSettings.fog; // ... let's remember the current fog setting...
            Unsupported.SetRenderSettingsUseFogNoDirty(false); // ... and then temporarily turn it off
		}
		/// <summary>
		/// Renders the viewport.
		/// </summary>
		/// <param name="r">Rect to render to.</param>
		/// <param name="background">Background.</param>
		public void RenderViewport (Rect r, GUIStyle background) {
			PreRenderViewport (r, background);
			
			// Rendering meshes.
			_vertsCount = 0;
			_trisCount = 0;
			var meshToViewportEnumerator = _meshToViewport.GetEnumerator ();
			while (meshToViewportEnumerator.MoveNext ()) {
				if (meshToViewportEnumerator.Current.Value == _selectedViewport) {
					if (_showPlane && currentPass == 1) {
						//_previewRenderUtility.DrawMeshB (_planeMesh, Vector3.zero, Quaternion.identity, _planeMaterial, 0);
						_previewRenderUtility.DrawMeshB (_planeMesh, Vector3.zero, _planeScale, Quaternion.identity, _planeMaterial, 0);
					}
					Mesh meshToDraw = _meshes [meshToViewportEnumerator.Current.Key];
					Material materialToUse;
					if (showWireframe) {
						materialToUse = _defaultMaterial;
					} else {
						materialToUse = _materials [meshToViewportEnumerator.Current.Key];
					}
					for (int j = 0; j < meshToDraw.subMeshCount; j++) {
						_previewRenderUtility.DrawMeshB (meshToDraw, 
							Vector3.zero, Quaternion.identity, materialToUse, j);
					}

					if (showTrisCount && _meshTrisCount [meshToViewportEnumerator.Current.Key]) {
						_trisCount += meshToDraw.triangles.Length / 3;
						_vertsCount += meshToDraw.vertices.Length;
					}
				}
			}

			PostRenderViewport (r, background, 1);
		}
		public bool RenderViewport (Rect r, GUIStyle background, Material[] materials) {
			//Switch RP if REPAINT.
			if (Event.current.type == EventType.Repaint) {
				_graphRP = GraphicsSettings.defaultRenderPipeline;
				_qualityRP = QualitySettings.renderPipeline;
				_lightData = Lightmapping.lightingDataAsset;
				GraphicsSettings.defaultRenderPipeline = null;
				QualitySettings.renderPipeline = null;
				Lightmapping.lightingDataAsset = null;
			}
			
			RenderViewport (r, background, materials, 1);
			if (hasSecondPass) {
				if (materials.Length != secondPassMaterials.Length) return false;
				RenderViewport (r, background, secondPassMaterials, 2);
			}

			//Switch back RP if REPAINT.
			if (Event.current.type == EventType.Repaint) {
				GraphicsSettings.defaultRenderPipeline = _graphRP;
				QualitySettings.renderPipeline = _qualityRP;
				Lightmapping.lightingDataAsset = _lightData;
			}

			return true;
		}
		private void RenderViewport (Rect r, GUIStyle background, Material[] materials, int pass) {
			currentPass = pass; 
			PreRenderViewport (r, background);

			// Rendering meshes.
			_vertsCount = 0;
			_trisCount = 0;
			
			// Draw Meshes if REPAINT event.
			if (Event.current.type == EventType.Repaint) {
				// Show plane.
				if (_showPlane && currentPass == 1) {
					_previewRenderUtility.DrawMeshB (_planeMesh, Vector3.zero, _planeScale, _planeRotation, _planeMaterial, 0);
				}
				var meshToViewportEnumerator = _meshToViewport.GetEnumerator ();
				while (meshToViewportEnumerator.MoveNext ()) {
					if (meshToViewportEnumerator.Current.Value == _selectedViewport) {
						Mesh meshToDraw = _meshes [meshToViewportEnumerator.Current.Key];
						//if (meshToDraw.subMeshCount != materials.Length) return;
						for (int j = 0; j < meshToDraw.subMeshCount; j++) {
							_previewRenderUtility.DrawMeshB (meshToDraw, 
								targetPositionOffset, targetRotationOffset, materials[j], j);
						}
						if (meshToDraw.vertexCount > 0) {
							if (showTrisCount && _meshTrisCount [meshToViewportEnumerator.Current.Key]) {
								_trisCount += meshToDraw.triangles.Length / 3;
								_vertsCount += meshToDraw.vertices.Length;
							}
						}
					}
				}
			}

			PostRenderViewport (r, background, pass);
		}
		private void PostRenderViewport (Rect r, GUIStyle background, int pass) {
			currentPass = pass;

			if (Event.current.type == EventType.Repaint) {
				// Final camera rendering.
				if (showWireframe) {
					GL.wireframe = true;
				}
				//bool fog = RenderSettings.fog;
				//Unsupported.SetRenderSettingsUseFogNoDirty(false);
				
				_previewRenderUtility.camera.Render ();
				
				if (showWireframe) {
					GL.wireframe = false;
				}
				
				// Draw pivot and handles.
				if (currentPass == 1 && (showPivot || onDrawHandles != null)) {
					Handles.SetCamera (_previewRenderUtility.camera);
				}
				if (onDrawHandles != null && currentPass == 1) {
					onDrawHandles (r, _previewRenderUtility.camera);
				}
				if (showPivot) {
					Handles.color = Color.yellow;
					Handles.DrawSolidDisc (Vector3.zero, 
						_previewRenderUtility.camera.transform.forward, 
						0.1f * GetHandleSize (Vector3.zero, _previewRenderUtility.camera));
				}
				if (showAxis) {
					Handles.color = Color.green;
					Handles.ArrowHandleCap (-1, axisGizmoPos, Quaternion.LookRotation (Vector3.up), axisGizmoSize, EventType.Repaint);
					Handles.color = Color.red;
					Handles.ArrowHandleCap (-1, axisGizmoPos, Quaternion.LookRotation (Vector3.right), axisGizmoSize, EventType.Repaint);
					Handles.color = Color.blue;
					Handles.ArrowHandleCap (-1, axisGizmoPos, Quaternion.LookRotation (Vector3.forward), axisGizmoSize, EventType.Repaint);		
				}
				if (showRuler) {
					DrawRuler ();
				}

				if (_meshes.Count > 0 && (debugShowNormals || debugShowTangents)) {
					for (int i = 0; i < _meshes.Count; i++) {
						Vector3[] vertices = _meshes [i].vertices;
						if (debugShowNormals) {
							Handles.color = debugNormalsColor;
							Vector3[] normals = _meshes [i].normals;
							for (int j = 0; j < normals.Length; j++) {
								Handles.DrawLine (vertices [j], vertices [j] + normals [j] * debugNormalsLength);
							}
						}
						if (debugShowTangents) {
							Handles.color = debugTangentsColor;
							Vector4[] tangents = _meshes [i].tangents;
							for (int j = 0; j < tangents.Length; j++) {
								Handles.DrawLine (vertices [j], vertices [j] + (Vector3)tangents [j] * debugTangentsLength);
							}
						}
					}
				}

				// Draw rendered texture
				if (hasSecondPass) {
					if (pass == 1) {
						Texture resultRender = _previewRenderUtility.EndPreview();
						if (firstPassTex == null) {
							firstPassTex = RenderTexture.GetTemporary (resultRender.width, resultRender.height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
						} else if (firstPassTex.width != resultRender.width || firstPassTex.height != resultRender.height) {
							RenderTexture.ReleaseTemporary (firstPassTex);
							firstPassTex = RenderTexture.GetTemporary (resultRender.width, resultRender.height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
						}
						Graphics.CopyTexture (resultRender, firstPassTex); 
						//Graphics.Blit (resultRender, firstPassTex);
					} else {
						Texture resultRender = _previewRenderUtility.EndPreview();
						GUI.DrawTexture (r, blendTex, ScaleMode.StretchToFill, false);
					}
				} else {
					Texture resultRender = _previewRenderUtility.EndPreview();
					GUI.DrawTexture (r, resultRender, ScaleMode.StretchToFill, false);
				}
				Unsupported.SetRenderSettingsUseFogNoDirty(_fogEnabled);
			}

			// Draw GUI .
			if ((!hasSecondPass && pass == 1) || (hasSecondPass && pass == 2)) {
				if (showPreviewTitle) {
					GUI.Label (r, previewTitle, textStyle);
				}
				if (showTrisCount) {
					r.y += EditorGUIUtility.singleLineHeight;
					GUI.Label (r, "Tris: " + _trisCount + ", Verts: " + _vertsCount, textStyle);
				}
				if (debugShowDebugInfo) {
					r.y += EditorGUIUtility.singleLineHeight;
					GUI.Label (r, "\n" + GetDebugInfo (), textStyle);
				}
				if (onDrawGUI != null) {
					onDrawGUI (r, _previewRenderUtility.camera);
				}
			}
		}
        /// <summary>
		/// Get world space size of a manipulator handle at given position.
		/// </summary>
		/// <param name="position">Postion of the handle.</param>
		/// <param name="camera">Camera.</param>
		public static float GetHandleSize (Vector3 position, Camera camera)
        {
            position = Handles.matrix.MultiplyPoint(position);
			float k_KHandleSize = 80.0f;
            if (camera)
            {
                Transform tr = camera.transform;
                Vector3 camPos = tr.position;
                float distance = Vector3.Dot(position - camPos, tr.TransformDirection(new Vector3(0, 0, 1)));
                Vector3 screenPos = camera.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(0, 0, distance)));
                Vector3 screenPos2 = camera.WorldToScreenPoint(camPos + tr.TransformDirection(new Vector3(1, 0, distance)));
                float screenDist = (screenPos - screenPos2).magnitude;
                return (k_KHandleSize / Mathf.Max(screenDist, 0.0001f)) * EditorGUIUtility.pixelsPerPoint;
            }
            return 20.0f;
        }
		/// <summary>
		/// Draws a ruler based on the top most and bottom most bounds of the meshes.
		/// </summary>
		public void DrawRuler () {
			Handles.color = rulerColor;
			_rulerOrigin.z = _minMeshWidth - 0.05f;
			if (_maxMeshHeight > 0.05f) {
				float topLength = Mathf.CeilToInt (_maxMeshHeight);
				Handles.DrawLine (_rulerOrigin, _rulerOrigin + Vector3.up * topLength);
				Vector3 rulerUnitPos = _rulerOrigin;
				Vector3 rulerSubunitPos;
				for (float i = 0f; i < topLength; i += rulerUnit) {
					if (rulerSubUnits > 1) {
						rulerSubunitPos = rulerUnitPos;
						for (int j = 1; j < rulerSubUnits; j++) {
							rulerSubunitPos.y += rulerUnit / (float)rulerSubUnits;
							Handles.DrawLine (rulerSubunitPos, rulerSubunitPos + Vector3.forward * 0.025f);
							Handles.Label (rulerSubunitPos + Vector3.forward * - 0.1f, rulerSubunitPos.y.ToString ());
						}
					}
					rulerUnitPos.y += rulerUnit;
					Handles.DrawLine (rulerUnitPos, rulerUnitPos + Vector3.forward * 0.05f);
					Handles.Label (rulerUnitPos + Vector3.forward * - 0.1f, rulerUnitPos.y.ToString ());
				}
			}
			if (_minMeshHeight < -0.05f) {
				float bottomLength = Mathf.CeilToInt (-_minMeshHeight);
				Handles.DrawLine (_rulerOrigin, _rulerOrigin + Vector3.down * bottomLength);
			}
		}
		#endregion

		#region Camera
		private void CreateBlendMaterial () {
			if (blendMaterial != null) Object.DestroyImmediate (blendMaterial);
			var shader = Shader.Find("Hidden/Broccoli/SproutLabMix");
			blendMaterial = new Material(shader);
			blendMaterial.hideFlags = HideFlags.HideAndDontSave;
			blendMaterial.SetFloat ("_IsLinearColorSpace", UnityEditor.PlayerSettings.colorSpace == ColorSpace.Linear?1f:0f);
			blendMaterial.SetFloat ("_BlendMode", (float)_secondPassBlend);
		}
		private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) {
         	OnPostRender (camera);
		}
		void OnPostRender (Camera cam)
		{
			if (cam == _previewRenderUtility.camera) {
				if (hasSecondPass && currentPass == 2) {
					GL.PushMatrix();
					
					GL.LoadPixelMatrix();
					//GL.LoadOrtho();
					if (blendMaterial == null) {
						CreateBlendMaterial ();
					}
					if (blendTex == null) {
						blendTex = RenderTexture.GetTemporary (firstPassTex.width, firstPassTex.height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
					} else if (blendTex.width != firstPassTex.width || blendTex.height != firstPassTex.height) {
						RenderTexture.ReleaseTemporary (blendTex);
						blendTex = RenderTexture.GetTemporary (firstPassTex.width, firstPassTex.height, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
					}
			
					//Graphics.DrawTexture ( new Rect (0,0,1023, -1023), firstPassTex, 0, 0, 0, 0, null);
					//Graphics.DrawTexture ( new Rect (0, firstPassTex.height, firstPassTex.width, -firstPassTex.height), firstPassTex, 0, 0, 0, 0, null);

					RenderTexture sourceTex = RenderTexture.active;
					blendMaterial.SetTexture ("_MainTex", firstPassTex);
					blendMaterial.SetTexture ("_BlendTex", sourceTex);
					//GL.PopMatrix();
					//mat.SetTexture ("_MainTex", sourceTex);
					//mat.SetTexture ("_BlendTex", firstPassTex);
					//Graphics.Blit (firstPassTex, sourceTex, mat, 0);
					RenderTexture.active = blendTex;
					Graphics.DrawTexture ( new Rect (0, firstPassTex.height, firstPassTex.width, -firstPassTex.height), firstPassTex, 0, 0, 0, 0, blendMaterial, 0);

					//RenderTexture.active = blendTex;
					//RenderTexture sourceTex = RenderTexture.active;
					//Graphics.CopyTexture (sourceTex, sourceTex);
			
					GL.PopMatrix();
				}
				

				/*
				if (!mat)
				{
					// Unity has a built-in shader that is useful for drawing
					// simple colored things. In this case, we just want to use
					// a blend mode that inverts destination colors.
					var shader = Shader.Find("Hidden/Internal-Colored");
					mat = new Material(shader);
					mat.hideFlags = HideFlags.HideAndDontSave;
					// Set blend mode to invert destination colors.
					mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
					mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					// Turn off backface culling, depth writes, depth test.
					mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					mat.SetInt("_ZWrite", 0);
					mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
				}

				GL.PushMatrix();
				GL.LoadOrtho();

				// activate the first shader pass (in this case we know it is the only pass)
				mat.SetPass(0);
				// draw a quad over whole screen
				GL.Begin(GL.QUADS);
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(1, 0, 0);
				GL.Vertex3(1, 1, 0);
				GL.Vertex3(0, 1, 0);
				GL.End();

				GL.PopMatrix();
				*/
				/*
				_previewRenderUtility.camera.targetTexture = null; //null means framebuffer
				//Graphics.Blit(myRenderTexture,null as RenderTexture, postProcessMaterial, postProcessMaterialPassNum);
				Graphics.Blit(myRenderTexture,null as RenderTexture);
				RenderTexture.ReleaseTemporary(myRenderTexture);
				*/
			}
		}
		#endregion

		#region UI Events
		/// <summary>
		/// Handles the view tool.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="eventType">Event type.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleViewTool (Event evt, EventType eventType, int id, Rect previewRect) {
			switch (eventType) {
				case EventType.ScrollWheel: DoAvatarPreviewZoom (evt, HandleUtility.niceMouseDeltaZoom * (evt.shift ? 2.0f : 0.5f)); break;
				case EventType.MouseDown:   HandleMouseDown (evt, id, previewRect); break;
				case EventType.MouseUp:     HandleMouseUp (evt, id); break;
				case EventType.MouseDrag:   HandleMouseDrag (evt, id, previewRect); break;
			}
		}
		/// <summary>
		/// Handles the mouse down.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleMouseDown (Event evt, int id, Rect previewRect)	{
			if (freeViewEnabled) {
				EditorGUIUtility.SetWantsMouseJumping (1);
				GUIUtility.hotControl = id;
			}
		}
		/// <summary>
		/// Handles the mouse up.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		protected void HandleMouseUp (Event evt, int id)	{
			if (freeViewEnabled && GUIUtility.hotControl == id) {
				GUIUtility.hotControl = 0;
				EditorGUIUtility.SetWantsMouseJumping (0);
				//evt.Use ();
			}
		}
		/// <summary>
		/// Handles the mouse drag.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="id">Identifier.</param>
		/// <param name="previewRect">Preview rect.</param>
		protected void HandleMouseDrag (Event evt, int id, Rect previewRect)	{
			if (freeViewEnabled && GUIUtility.hotControl == id) {
				if (evt.control) {
					DoAvatarPreviewOffset (evt, previewRect);
				} else {
					DoAvatarPreviewOrbit (evt, previewRect);
				}
			}
		}
		/// <summary>
		/// Does the avatar preview orbit.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="previewRect">Preview rect.</param>
		void DoAvatarPreviewOrbit (Event evt, Rect previewRect) {
			m_PreviewDir -= evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(previewRect.width, previewRect.height) * 140.0f;
			m_PreviewDir.y = Mathf.Clamp (m_PreviewDir.y, -90, 90);
			onRequiresRepaint?.Invoke ();
		}
		/// <summary>
		/// Does the avatar preview offset.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="previewRect">Preview rect.</param>
		void DoAvatarPreviewOffset (Event evt, Rect previewRect) {
			/*
			m_PreviewOffset.x -= evt.delta.x * 0.015f / m_ZoomFactor;
			m_PreviewOffset.y += evt.delta.y * 0.015f / m_ZoomFactor;
			*/
			/*
			float distance_to_screen = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
			Vector3 pos_move = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance_to_screen ));
			transform.position = new Vector3( pos_move.x, transform.position.y, pos_move.z );
			*/
			Vector3 startPoint = _previewRenderUtility.camera.ScreenToViewportPoint (Vector3.zero);
			Vector3 endPoint = _previewRenderUtility.camera.ScreenToViewportPoint (evt.delta);
			Vector3 range = endPoint - startPoint;
			float factor = 1.8f * m_ZoomFactor;
			m_PreviewOffset.x -= range.x * factor;
			m_PreviewOffset.y += range.y * factor;
			onRequiresRepaint?.Invoke ();
		}
		/// <summary>
		/// Does the avatar preview zoom.
		/// </summary>
		/// <param name="evt">Evt.</param>
		/// <param name="delta">Delta.</param>
		void DoAvatarPreviewZoom(Event evt, float delta)	{
			if (zoomEnabled) {
				float zoomDelta = -delta * 0.05f;
				m_ZoomFactor += m_ZoomFactor * zoomDelta;
				if (m_ZoomFactor < minZoomFactor) 
					m_ZoomFactor = minZoomFactor;
				else if (m_ZoomFactor > maxZoomFactor) 
					m_ZoomFactor = maxZoomFactor;
				// zoom is clamp too 10 time closer than the original zoom
				//m_ZoomFactor = Mathf.Max (m_ZoomFactor, m_AvatarScale / 2.0f);
				onRequiresRepaint?.Invoke ();
				//evt.Use();
			}
		}
		/// <summary>
		/// Sets a zoom value for the camera.
		/// </summary>
		/// <param name="factor">Factor for zoom.</param>
		public void SetZoom (float factor) {
			if (factor < minZoomFactor) 
				factor = minZoomFactor;
			else if (factor > maxZoomFactor) 
				factor = maxZoomFactor;
			m_ZoomFactor = factor;
			onRequiresRepaint?.Invoke ();
		}
		/// <summary>
		/// Set the camera direction.
		/// </summary>
		/// <param name="direction">Vector3 direction.</param>
		public void SetDirection (Vector2 direction) {
			m_PreviewDir = direction;
		}
		/// <summary>
		/// Sets an offset value for the camera.
		/// </summary>
		/// <param name="offset"></param>
		public void SetOffset (Vector3 offset) {
			m_PreviewOffset = offset;
		}
		/// <summary>
		/// Set the target rotation for the camera.
		/// </summary>
		/// <param name="targetRotation">Target rotation.</param>
		public void SetTargetRotation (Quaternion targetRotation) {
			targetRotationOffset = targetRotation;
		}
		/// <summary>
		/// Get the preview zoom factor.
		/// </summary>
		/// <returns>Zoom factor.</returns>
		public float GetZoom () {
			return m_ZoomFactor;
		}
		/// <summary>
		/// Get the preview direction.
		/// </summary>
		/// <returns>Preview direction.</returns>
		public Vector2 GetDirection () {
			return m_PreviewDir;
		}
		/// <summary>
		/// Get the preview offset.
		/// </summary>
		/// <returns>Preview offset.</returns>
		public Vector3 GetOffset () {
			return m_PreviewOffset;
		}
		/// <summary>
		/// Get the target rotation.
		/// </summary>
		/// <returns>Target rotation.</returns>
		public Quaternion GetTargetRotation () {
			return targetRotationOffset;
		}
		/// <summary>
		/// Calculates the best zoom for a mesh.
		/// </summary>
		/// <param name="mesh"></param>
		public void CalculateZoom (Mesh mesh) {
			mesh.RecalculateBounds ();
			float distance = Vector3.Distance (mesh.bounds.min, mesh.bounds.max);
			SetZoom (distance * 0.8f);
		}
		#endregion

		#region Helper Objects
		/// <summary>
		/// Enables or disables showing a plan mesh as part of preview.
		/// </summary>
		/// <param name="enabled">Enable flag to show the plane.</param>
		/// <param name="size">Size of each side of the length.</param>
		/// <param name="position">Center position for the plane.</param>
		public void ShowPlaneMesh (bool enabled, float size, Vector3 position) {
			DestroyPlaneMesh ();
			_showPlane = enabled;
			if (_showPlane) {
				BuildPlaneMesh (size, position);
			}
		}
		/// <summary>
		/// Disables showing the plane mesh.
		/// </summary>
		public void HidePlaneMesh () {
			DestroyPlaneMesh ();
			_showPlane = false;
		}
		/// <summary>
		/// Enabled or disables showing a axis arrow handles as part of the preview.
		/// </summary>
		/// <param name="enabled">Enable flag to show the arrow handles.</param>
		public void ShowAxisHandles (bool enabled) {
			showAxis = enabled;
		}
		/// <summary>
		/// Enabled or disables showing a axis arrow handles as part of the preview.
		/// </summary>
		/// <param name="enabled">Enable flag to show the arrow handles.</param>
		/// <param name="size">Size for the axis arrow handles.</param>
		/// <param name="position">Center position for the handles.</param>
		public void ShowAxisHandles (bool enabled, float size, Vector3 position) {
			showAxis = enabled;
			axisGizmoSize = size;
			axisGizmoPos = position;
		}
		/// <summary>
		/// Builds or rebuilds this mesh preview plane mesh.
		/// </summary>
		/// <param name="size">Size of each side of the length.</param>
		/// <param name="position">Center position for the plane.</param>
		private void BuildPlaneMesh (float size, Vector3 position) {
			_planeMesh = new Mesh ();
			float hSize = size / 2f;
			Vector3[] vertices = new Vector3 [4];
			vertices [0] = position + new Vector3 (-hSize, 0, -hSize);
			vertices [1] = position + new Vector3 (hSize, 0, -hSize);
			vertices [2] = position + new Vector3 (hSize, 0, hSize);
			vertices [3] = position + new Vector3 (-hSize, 0, hSize);
			Vector2[] uvs = new Vector2[4];
			uvs [0] = new Vector2 (0, 0);
			uvs [1] = new Vector2 (1, 0);
			uvs [2] = new Vector2 (1, 1);
			uvs [3] = new Vector2 (0, 1);
			int[] tris = new int [6] {0, 2, 1, 0, 3, 2};
			_planeMesh.vertices = vertices;
			_planeMesh.uv = uvs;
			_planeMesh.triangles = tris;
			_planeMesh.RecalculateNormals ();
			_planeMesh.RecalculateTangents ();
			_planeMesh.RecalculateBounds ();
			Shader matShader = Shader.Find ("Standard");
			_planeMaterial = new Material (matShader);
			_planeMaterial.SetTexture ("_MainTex", planeTexture);
			_planeMaterial.SetColor ("_Color", _planeTint);
		}
		/// <summary>
		/// Sets the configuration to draw the plane mesh.
		/// </summary>
		/// <param name="scale">Scale of the plane mesh.</param>
		/// <param name="tint">Tint of the plane material.</param>
		public void SetPlaneMesh (Vector3 scale, Color tint) {
			_planeScale = scale;
			_planeTint = tint;
			if (_planeMaterial != null) {
				_planeMaterial.SetColor ("_Color", _planeTint);
			}
		}
		/// <summary>
		/// Builds or rebuild this mesh preview plane mesh.
		/// </summary>
		private void DestroyPlaneMesh () {
			if (_planeMesh != null) {
				UnityEngine.Object.DestroyImmediate (_planeMesh);
			}
			if (_planeMaterial != null) {
				UnityEngine.Object.DestroyImmediate (_planeMaterial);
			}
			_showPlane = false;
		}
		/// <summary>
		/// Sets the rules display settings.
		/// </summary>
		/// <param name="showRuler">Flag to show a ruler.</param>
		/// <param name="rulerColor">Color for the ruler.</param>
		public void SetRuler (bool showRuler, Color rulerColor) {
			this.showRuler = showRuler;
			this.rulerColor = rulerColor;
		}
		#endregion

		#region Debug
		/// <summary>
		/// Get a string with debug information about this mesh view.
		/// </summary>
		/// <returns>String with debug information.</returns>
		public string GetDebugInfo () {
			debugInfo = string.Empty; 
			for (int i = 0; i < _meshes.Count; i++) {
				if (_meshes [i].isReadable) {
					debugInfo += string.Format ("Mesh {0}, submeshes: {1}, vertices: {2}, tris: {3}\n", i, _meshes [i].subMeshCount, _meshes [i].vertexCount, _meshes [i].triangles.Length);
					debugInfo += string.Format ("\tnormals: {0}, tangents: {1}\n", _meshes [i].normals.Length, _meshes [i].tangents.Length);
					debugInfo += string.Format ("\tUVs: {0}, UV2s: {1}, UV3s: {1}, UV4s: {1}\n", _meshes [i].uv.Length, _meshes [i].uv2.Length, _meshes [i].uv3.Length, _meshes [i].uv4.Length);
					debugInfo += string.Format ("\tUV5s: {0}, UV6s: {1}, UV7s: {1}, UV8s: {1}\n", _meshes [i].uv5.Length, _meshes [i].uv6.Length, _meshes [i].uv7.Length, _meshes [i].uv8.Length);
					debugInfo += string.Format ("\tbounds: {0}", _meshes [i].bounds);
				} else {
					debugInfo += string.Format ("Mesh {0}, is not readable.\n", i);
				}
			}
			debugInfo += string.Format ("\nCamera Pos: {0}, {1}, {2}\n", camPos.x.ToString ("F3"), camPos.y.ToString ("F3"), camPos.z.ToString ("F3"));
			debugInfo += string.Format ("Camera Offset: {0}, {1}, {2}\n", m_PreviewOffset.x.ToString ("F3"), m_PreviewOffset.y.ToString ("F3"), m_PreviewOffset.z.ToString ("F3"));
			debugInfo += string.Format ("Camera Direction: {0}, {1}\n", m_PreviewDir.x.ToString ("F3"), m_PreviewDir.y.ToString ("F3"));
			debugInfo += string.Format ("Zoom Factor: {0}\n", m_ZoomFactor);
			debugInfo += string.Format ("Light 0 Intensity: {0}, Rotation: {1}\n", _lightA.intensity.ToString ("F2"), _lightA.transform.rotation.eulerAngles);
			debugInfo += string.Format ("Light 0 Color: {0}, Bounce Intensity: {1}, Active&Enabled: {2}, Range: {3}\n", _lightA.color, _lightA.bounceIntensity, _lightA.isActiveAndEnabled, _lightA.range);
			debugInfo += string.Format ("Light 1 Intensity: {0}, Rotation: {1}\n", _lightB.intensity.ToString ("F2"), _lightB.transform.rotation.eulerAngles);
			debugInfo += string.Format ("Light 1 Color: {0}, Bounce Intensity: {1}, Active&Enabled: {2}, Range: {3}\n", _lightB.color, _lightB.bounceIntensity, _lightB.isActiveAndEnabled, _lightB.range);
			debugInfo += string.Format ("Render Settings Ambient Ligth: {0}\n", RenderSettings.ambientLight);
			debugInfo += string.Format ("Render Settings Ambient Intensity: {0}\n", RenderSettings.ambientIntensity);
			debugInfo += string.Format ("Render Settings Ambient Ground Color: {0}\n", RenderSettings.ambientGroundColor);
			debugInfo += string.Format ("Render Settings Ambient Probe: {0}\n", RenderSettings.ambientProbe);
			debugInfo += string.Format ("Render Settings Ambient Mode: {0}\n", RenderSettings.ambientMode);
			debugInfo += string.Format ("Render Settings Default Reflection Mode: {0}\n", RenderSettings.defaultReflectionMode);
			debugInfo += string.Format ("Quality Settings Active Color Space: {0}\n", QualitySettings.activeColorSpace);
			debugInfo += string.Format ("Has Second Pass: {0}\n", hasSecondPass);
			if (hasSecondPass) {
				debugInfo += string.Format ("  Second Pass Materials: {0}\n", secondPassMaterials.Length);
				debugInfo += string.Format ("  Second Pass Blend Mode: {0}\n", secondPassBlend);
			}
			return debugInfo;
		}
		#endregion
	}
}