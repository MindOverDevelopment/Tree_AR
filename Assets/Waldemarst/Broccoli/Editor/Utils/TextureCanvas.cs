using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

using Broccoli.Base;
using Broccoli.Pipe;

namespace Broccoli.TreeNodeEditor
{
	/// <summary>
	/// Canvas GUI component to show textures and select areas on it.
	/// </summary>
    public class TextureCanvas : GraphView {
        #region Class Area
		/// <summary>
		/// Models an area selected on a texture.
		/// The area uses a 0 to 1 value for width and height begining from
		/// the bottom left corner and ending at the top right one.
		/// </summary>
        public class Area {
            #region Vars
            public int id;
            public Rect rect;
			public bool hasPivot = true;
			public Vector2 pivot = Vector2.zero;
			protected bool _isHeightEditable = true;
			public bool isHeightEditable {
				get { return _isHeightEditable; }
			}
			protected bool _isWidthEditable = true;
			public bool isWidthEditable {
				get { return _isWidthEditable; }
			}
			private float canvasHandleSize = 12f;
            private Rect _elemRect;
			private Rect _pivotRect = new Rect ();
			private bool _pivotMove = false;
			private Rect _tmpRect;
			private Rect _cornerHRect = new Rect ();
			private Rect _borderHRect = new Rect ();
			private Rect _areaHRect = new Rect ();
			private Rect _pivotHRect = new Rect ();
			private float _pivotElemSize = 8f;
			private float _pivotElemHandleSize = 12f;
            private AreaGraphElement _areaGraphElem;
			private AreaGraphElement _pivotGraphElem;
			private MoveHandleGraphElement _topLeftHandle;
			private MoveHandleGraphElement _topRightHandle;
			private MoveHandleGraphElement _bottomLeftHandle;
			private MoveHandleGraphElement _bottomRightHandle;
			private MoveHandleGraphElement _areaHandle;
			private MoveHandleGraphElement _topHandle;
			private MoveHandleGraphElement _rightHandle;
			private MoveHandleGraphElement _bottomHandle;
			private MoveHandleGraphElement _leftHandle;
			private MoveHandleGraphElement _pivotHandle;
			private Vector2 _topLeftCorner = Vector2.zero;
			private Vector2 _bottomRightCorner = Vector2.zero;
			private static string areaGraphElemClass = "area-graph-elem";
			private static string areaPivotGraphElemClass = "area-pivot-graph-elem";
			private static string areaHandleElemClass = "area-handle";
			private static string cornerHandleClass = "corner-handle";
			private static string borderHandleElemClass = "border-handle";
			private static string resizeUpLeftHandleClass = "resize-up-left";
			private static string resizeUpRightHandleClass = "resize-up-right";
			private static string resizeVerticalClass = "resize-vertical";
			private static string resizeHorizontalClass = "resize-horizontal";
			private static string pivotHandleClass = "pivot-handle";
            #endregion

            #region Contructor and Setup
			/// <summary>
			/// Texture area constructor with no pivot point.
			/// </summary>
			/// <param name="id">Area identifier.</param>
			/// <param name="areaRect">Area rect in GL coordinates.</param>
			/// <param name="canvas">Texture canvas instance.</param>
			public Area (int id, Rect areaRect, TextureCanvas canvas) {
				SetupArea (id, areaRect, false, Vector2.zero, canvas);
			}
			/// <summary>
			/// Texture area constructor with pivot point.
			/// </summary>
			/// <param name="id">Area identifier.</param>
			/// <param name="areaRect">Area rect in GL coordinates.</param>
			/// <param name="pivot">Pivot point position relative to the areaRect, in GL coordinates.</param>
			/// <param name="canvas">Texture canvas instance.</param>
			public Area (int id, Rect areaRect, Vector2 pivot, TextureCanvas canvas) {
				SetupArea (id, areaRect, true, pivot, canvas);
			}
			/// <summary>
			/// Shared instance setup method for constructors.
			/// </summary>
			/// <param name="id">Area identifier.</param>
			/// <param name="areaRect">Area rect in GL coordinates.</param>
			/// <param name="hasPivot"><c>True</c> if the area should have an pivot point.</param>
			/// <param name="pivot">Pivot point position relative to the areaRect, in GL coordinates.</param>
			/// <param name="canvas">Texture canvas instance.</param>
            private void SetupArea (int id, Rect areaRect, bool hasPivot, Vector2 pivot, TextureCanvas canvas) {
                this.id = id;
				this.hasPivot = hasPivot;
                this._elemRect = new Rect ();
                _areaGraphElem = new AreaGraphElement ();
				_areaGraphElem.AddToClassList (areaGraphElemClass);

				if (hasPivot) {
					_pivotGraphElem = new AreaGraphElement ();
					_pivotGraphElem.AddToClassList (areaPivotGraphElemClass);
				}

				_areaHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Area);
				_areaHandle.AddToClassList (areaHandleElemClass);

				_topHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Top);
				_topHandle.AddToClassList (borderHandleElemClass);
				_topHandle.AddToClassList (resizeVerticalClass);

				_bottomHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Bottom);
				_bottomHandle.AddToClassList (borderHandleElemClass);
				_bottomHandle.AddToClassList (resizeVerticalClass);

				_leftHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Left);
				_leftHandle.AddToClassList (borderHandleElemClass);
				_leftHandle.AddToClassList (resizeHorizontalClass);

				_rightHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Right);
				_rightHandle.AddToClassList (borderHandleElemClass);
				_rightHandle.AddToClassList (resizeHorizontalClass);

				_topLeftHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.TopLeft);
				_topLeftHandle.AddToClassList (resizeUpLeftHandleClass);
				_topLeftHandle.AddToClassList (cornerHandleClass);

				_topRightHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.TopRight);
				_topRightHandle.AddToClassList (resizeUpLeftHandleClass);
				_topRightHandle.AddToClassList (cornerHandleClass);

				_bottomLeftHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.BottomLeft);
				_bottomLeftHandle.AddToClassList (resizeUpRightHandleClass);
				_bottomLeftHandle.AddToClassList (cornerHandleClass);

				_bottomRightHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.BottomRight);
				_bottomRightHandle.AddToClassList (resizeUpRightHandleClass);
				_bottomRightHandle.AddToClassList (cornerHandleClass);

				if (hasPivot) {
					_pivotHandle = new MoveHandleGraphElement (id, MoveHandleGraphElement.HandleType.Pivot);
					_pivotHandle.AddToClassList (pivotHandleClass);
				}
				
                SetAreaRect (areaRect, canvas);
            }
			public void AddElementsToCanvas (TextureCanvas canvas) {
				canvas.AddElement (_areaGraphElem);
				canvas.AddElement (_areaHandle);
				canvas.AddElement (_topHandle);
				canvas.AddElement (_bottomHandle);
				canvas.AddElement (_rightHandle);
				canvas.AddElement (_leftHandle);
				canvas.AddElement (_topRightHandle);
				canvas.AddElement (_topLeftHandle);
				canvas.AddElement (_bottomRightHandle);
				canvas.AddElement (_bottomLeftHandle);
				if (hasPivot) {
					canvas.AddElement (_pivotGraphElem);
					canvas.AddElement (_pivotHandle);
				}
			}
            #endregion

            #region Area Grap Element Ops
			/// <summary>
			/// Activates or deactivates the width for edition.
			/// </summary>
			/// <param name="isWidthEditable"><c>True</c> to mark the width of the area for edition.</param>
			public void SetWidthEditable (bool isWidthEditable) {
				_isWidthEditable = isWidthEditable;
				if (_isWidthEditable) {
					_leftHandle.SetMovable (true, new string[] {resizeHorizontalClass});
					_rightHandle.SetMovable (true, new string[] {resizeHorizontalClass});
					if (_isHeightEditable) {
						string[] addClasses = new string[]{};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topLeftHandle.SetMovable (true, new string[]{resizeUpLeftHandleClass});
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, new string[]{resizeUpRightHandleClass});
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, new string[]{resizeUpRightHandleClass});
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, new string[]{resizeUpLeftHandleClass});
					} else {
						string[] addClasses = new string[]{resizeHorizontalClass};
						string[] removeClasses = new string[]{resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
					}
				} else {
					_leftHandle.SetMovable (false, new string[]{}, new string[] {resizeHorizontalClass});
					_rightHandle.SetMovable (false, new string[]{}, new string[] {resizeHorizontalClass});
					if (_isHeightEditable) {
						string[] addClasses = new string[]{resizeVerticalClass};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
					} else {
						string[] addClasses = new string[]{};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (false, addClasses, removeClasses);
						_topRightHandle.SetMovable (false, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (false, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (false, addClasses, removeClasses);
					}
				}
			}
			/// <summary>
			/// Activates or deactivates the height for edition.
			/// </summary>
			/// <param name="isWidthEditable"><c>True</c> to mark the height of the area for edition.</param>
			public void SetHeightEditable (bool isHeightEditable) {
				_isHeightEditable = isHeightEditable;
				if (_isHeightEditable) {
					_topHandle.SetMovable (true, new string[] {resizeVerticalClass});
					_bottomHandle.SetMovable (true, new string[] {resizeVerticalClass});
					if (_isWidthEditable) {
						string[] addClasses = new string[]{};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topLeftHandle.SetMovable (true, new string[]{resizeUpLeftHandleClass});
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, new string[]{resizeUpRightHandleClass});
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, new string[]{resizeUpRightHandleClass});
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, new string[]{resizeUpLeftHandleClass});
					} else {
						string[] addClasses = new string[]{resizeVerticalClass};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
					}
				} else {
					_topHandle.SetMovable (false, new string[]{}, new string[] {resizeVerticalClass});
					_bottomHandle.SetMovable (false, new string[]{}, new string[] {resizeVerticalClass});
					if (_isWidthEditable) {
						string[] addClasses = new string[]{resizeHorizontalClass};
						string[] removeClasses = new string[]{resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (true, addClasses, removeClasses);
						_topRightHandle.SetMovable (true, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (true, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (true, addClasses, removeClasses);
					} else {
						string[] addClasses = new string[]{};
						string[] removeClasses = new string[]{resizeHorizontalClass, resizeVerticalClass, resizeUpLeftHandleClass, resizeUpRightHandleClass};
						_topLeftHandle.SetMovable (false, addClasses, removeClasses);
						_topRightHandle.SetMovable (false, addClasses, removeClasses);
						_bottomLeftHandle.SetMovable (false, addClasses, removeClasses);
						_bottomRightHandle.SetMovable (false, addClasses, removeClasses);
					}
				}
			}
			/// <summary>
			/// Draws the area rectangle according to a handle movement 
			/// (as a preview, not commiting the new values until the movement finishes).
			/// </summary>
			/// <param name="movingElem">Handle eliciting the edition.</param>
			/// <param name="delta">Delta movement of the handle, in UI coordinates.</param>
			/// <param name="canvas">Canvas containing the area.</param>
			public void PreviewAreaRect (MoveHandleGraphElement movingElem, Vector2 delta, TextureCanvas canvas) {
				_topLeftCorner = _elemRect.min;
				_bottomRightCorner = _elemRect.max;
				switch (movingElem.handleType) {
					case MoveHandleGraphElement.HandleType.Area:
						_topLeftCorner += delta;
						_bottomRightCorner += delta;
						break;
					case MoveHandleGraphElement.HandleType.TopLeft:
						if (_isWidthEditable)
							_topLeftCorner.x += delta.x;
						if (_isHeightEditable)
							_topLeftCorner.y += delta.y;
						break;
					case MoveHandleGraphElement.HandleType.TopRight:
						if (_isWidthEditable)
							_bottomRightCorner.x += delta.x;
						if (_isHeightEditable)
							_topLeftCorner.y += delta.y;						
						break;
					case MoveHandleGraphElement.HandleType.BottomLeft:
						if (_isWidthEditable)
							_topLeftCorner.x += delta.x;
						if (_isHeightEditable)
							_bottomRightCorner.y += delta.y;
						break;
					case MoveHandleGraphElement.HandleType.BottomRight:
						if (_isWidthEditable)
							_bottomRightCorner.x += delta.x;
						if (_isHeightEditable)
							_bottomRightCorner.y += delta.y;
						break;
					case MoveHandleGraphElement.HandleType.Top:
						_topLeftCorner.y += delta.y;
						break;
					case MoveHandleGraphElement.HandleType.Bottom:
						_bottomRightCorner.y += delta.y;
						break;
					case MoveHandleGraphElement.HandleType.Left:
						_topLeftCorner.x += delta.x;
						break;
					case MoveHandleGraphElement.HandleType.Right:
						_bottomRightCorner.x += delta.x;
						break;
				}
				_tmpRect = new Rect (_topLeftCorner.x, _topLeftCorner.y, _bottomRightCorner.x - _topLeftCorner.x, _bottomRightCorner.y - _topLeftCorner.y);
				_areaGraphElem.SetPosition (_tmpRect);
				if (hasPivot) {
					if (movingElem.handleType == MoveHandleGraphElement.HandleType.Pivot) {
						_tmpRect = movingElem.GetPosition ();
						_tmpRect.width = _pivotElemSize / canvas.currentZoom;
						_tmpRect.height = _tmpRect.width;
						_tmpRect.x += (_pivotElemHandleSize - _pivotElemSize) / canvas.currentZoom * 0.5f;
						_tmpRect.y += (_pivotElemHandleSize - _pivotElemSize) / canvas.currentZoom * 0.5f;
						_pivotGraphElem.SetPosition (_tmpRect);
						_pivotMove = true;
					} else {
						_tmpRect = PivotToPivotRect (pivot, _tmpRect, canvas);
						_pivotGraphElem.SetPosition (_tmpRect);
					}
				}
			}
			public bool ApplyPreviewAreaRect (MoveHandleGraphElement movingElem, TextureCanvas canvas) {
				_tmpRect = new Rect (_topLeftCorner.x, _topLeftCorner.y, _bottomRightCorner.x - _topLeftCorner.x, _bottomRightCorner.y - _topLeftCorner.y);
				if (_tmpRect != _elemRect || _pivotMove) {
					SetAreaRect (ElemRectToAreaRect (_tmpRect, canvas), canvas);
					if (hasPivot) {
						SetPivot (PivotRectToPivot (_pivotGraphElem.GetPosition (), canvas), canvas);
						_pivotMove = false;
					}
					return true;
				}
				return false;
			}
            public void SetAreaRect (Rect areaRect, TextureCanvas canvas) {
                this.rect = areaRect;
                this._elemRect = AreaRectToElemRect (areaRect, canvas);
                _areaGraphElem.SetPosition (this._elemRect);
				UpdateHandles (canvas);
            }
			public void SetPivot (Vector2 pivot, TextureCanvas canvas) {
				this.pivot = pivot;
				UpdatePivotRect (pivot, canvas);
				UpdateHandles (canvas);
			}
			private void UpdatePivotRect (Vector2 pivot, TextureCanvas canvas) {
				this._pivotRect = PivotToPivotRect (pivot, _elemRect, canvas);
				_pivotGraphElem.SetPosition (this._pivotRect);
			}
			public void SetAreaRectAndPivot (Rect areaRect, Vector2 pivot, TextureCanvas canvas) {
                this.rect = areaRect;
				this.pivot = pivot;
                this._elemRect = AreaRectToElemRect (areaRect, canvas);
                _areaGraphElem.SetPosition (this._elemRect);
				UpdatePivotRect (pivot, canvas);
				UpdateHandles (canvas);
            }
			private Rect AreaRectToElemRect (Rect areaRect, TextureCanvas canvas) {
				Rect _elemRect = new Rect ();
				_elemRect.x = areaRect.x * canvas.canvasWidth;
                _elemRect.height = areaRect.height * canvas.canvasHeight;
                _elemRect.y = canvas.canvasHeight - (areaRect.y * canvas.canvasHeight);
                _elemRect.y -= _elemRect.height;
                _elemRect.width = areaRect.width * canvas.canvasWidth;
				return _elemRect;
			}
			private Rect ElemRectToAreaRect (Rect elemRect, TextureCanvas canvas) {
				Rect _areaRect = new Rect ();
				_areaRect.x = elemRect.x / canvas.canvasWidth;
				_areaRect.y = 1f - ((elemRect.y + elemRect.height) / canvas.canvasHeight);
				_areaRect.width = elemRect.width / canvas.canvasWidth;
                _areaRect.height = elemRect.height / canvas.canvasHeight;                
				return _areaRect;
			}
			private Rect PivotToPivotRect (Vector2 pivot, Rect refRect, TextureCanvas canvas) {
				Rect _pivotRect = new Rect ();
				float size = _pivotElemSize / canvas.currentZoom;
				_pivotRect.x = refRect.x + refRect.width * pivot.x - size * 0.5f;
				_pivotRect.width = size;
				_pivotRect.y = refRect.y + refRect.height * (1f - pivot.y) - size * 0.5f;
				_pivotRect.height = size;
				return _pivotRect;
			}
			private Vector2 PivotRectToPivot (Rect refRect, TextureCanvas canvas) {
				Vector2 _pivot = new Vector2 ();
				if (_elemRect.width > 0 && _elemRect.height > 0) {
					float halfSize = _pivotElemSize / canvas.currentZoom * 0.5f;
					_pivot.x = (refRect.x - _elemRect.x + halfSize) / _elemRect.width;
					_pivot.y = (refRect.y - _elemRect.y + halfSize) / _elemRect.height;
					_pivot.y = 1f - _pivot.y;
				} else {
					_pivot.x = 0;
					_pivot.y = 0;
				}
				return _pivot;
			}
			public void OnZoom (TextureCanvas canvas) {
				if (hasPivot) {
					UpdatePivotRect (this.pivot, canvas);
				}
				UpdateHandles (canvas);
			}
			public void UpdateHandles (TextureCanvas canvas) {
				_cornerHRect = this._elemRect;

				_cornerHRect.width = canvasHandleSize / canvas.currentZoom;
				_cornerHRect.height = canvasHandleSize / canvas.currentZoom;

				// Top Left Handle
				_cornerHRect.x = _elemRect.x - (_cornerHRect.width * 0.5f);
				_cornerHRect.y = _elemRect.y - (_cornerHRect.height * 0.5f);
				_topLeftHandle.SetPosition (_cornerHRect);
				_topLeftHandle.limits.x = -_cornerHRect.width * 0.5f;
				_topLeftHandle.limits.y = -_cornerHRect.height * 0.5f;
				_topLeftHandle.limits.z = this._elemRect.max.x - _cornerHRect.width * 0.5f;
				_topLeftHandle.limits.w = this._elemRect.max.y - _cornerHRect.height * 0.5f;

				_borderHRect = _cornerHRect;
				_borderHRect.x += _cornerHRect.width;

				// Top Right Handle
				_cornerHRect.x = _elemRect.max.x - (_cornerHRect.width * 0.5f);
				_topRightHandle.SetPosition (_cornerHRect);
				_topRightHandle.limits.x = this._elemRect.min.x - _cornerHRect.width * 0.5f;
				_topRightHandle.limits.y = -_cornerHRect.height * 0.5f;
				_topRightHandle.limits.z = canvas.canvasWidth - _cornerHRect.width * 0.5f;
				_topRightHandle.limits.w = this._elemRect.max.y - _cornerHRect.height * 0.5f;

				_borderHRect.width = _cornerHRect.x - _borderHRect.x;
				_topHandle.SetPosition (_borderHRect);
				_topHandle.limits.x = -_cornerHRect.width * 0.5f;
				_topHandle.limits.y = -_cornerHRect.height * 0.5f;
				_topHandle.limits.z = canvas.canvasWidth;
				_topHandle.limits.w = this._elemRect.max.y - _cornerHRect.height * 0.5f;

				_cornerHRect.y = _elemRect.max.y - (_cornerHRect.height * 0.5f);
				_bottomRightHandle.SetPosition (_cornerHRect);
				_bottomRightHandle.limits.x = this._elemRect.min.x - _cornerHRect.width * 0.5f;
				_bottomRightHandle.limits.y = this._elemRect.min.y - _cornerHRect.height * 0.5f;
				_bottomRightHandle.limits.z = canvas.canvasWidth - _cornerHRect.width * 0.5f;
				_bottomRightHandle.limits.w = canvas.canvasHeight - _cornerHRect.height * 0.5f;


				_borderHRect.y = _cornerHRect.y;
				_bottomHandle.SetPosition (_borderHRect);
				_bottomHandle.limits.x = -_cornerHRect.width * 0.5f;
				_bottomHandle.limits.y = this._elemRect.min.y - _cornerHRect.width * 0.5f;
				_bottomHandle.limits.z = canvas.canvasWidth;
				_bottomHandle.limits.w = canvas.canvasHeight - _cornerHRect.height * 0.5f;

				_cornerHRect.x = _elemRect.x - (_cornerHRect.width * 0.5f);
				_bottomLeftHandle.SetPosition (_cornerHRect);
				_bottomLeftHandle.limits.x = -_cornerHRect.width * 0.5f;
				_bottomLeftHandle.limits.y = this._elemRect.min.y - _cornerHRect.height * 0.5f;
				_bottomLeftHandle.limits.z = this._elemRect.max.x - _cornerHRect.width * 0.5f;
				_bottomLeftHandle.limits.w = canvas.canvasHeight - _cornerHRect.height * 0.5f;

				_borderHRect.x = _elemRect.x - _cornerHRect.width * 0.5f;
				_borderHRect.y = _elemRect.y + _cornerHRect.height * 0.5f;
				_borderHRect.width = _cornerHRect.width;
				_borderHRect.height = _elemRect.height - _cornerHRect.height;
				_leftHandle.SetPosition (_borderHRect);
				_leftHandle.limits.x = -_cornerHRect.width * 0.5f;
				_leftHandle.limits.y = this._elemRect.min.y - _cornerHRect.width * 0.5f;
				_leftHandle.limits.z = this._elemRect.max.x - _cornerHRect.width * 0.5f;
				_leftHandle.limits.w = canvas.canvasHeight - _cornerHRect.height * 0.5f;

				_borderHRect.x = _elemRect.max.x - (_cornerHRect.width * 0.5f);
				_rightHandle.SetPosition (_borderHRect);
				_rightHandle.limits.x = this._elemRect.min.x - _cornerHRect.width * 0.5f;
				_rightHandle.limits.y = this._elemRect.min.y - _cornerHRect.width * 0.5f;
				_rightHandle.limits.z = canvas.canvasWidth - _cornerHRect.height * 0.5f;
				_rightHandle.limits.w = canvas.canvasHeight - _cornerHRect.height * 0.5f;

				// Area handle
				_areaHRect = this._elemRect;
				_areaHRect.x += _cornerHRect.width * 0.5f;
				_areaHRect.y += _cornerHRect.height * 0.5f;
				_areaHRect.width -= _cornerHRect.width;
				_areaHRect.height -= _cornerHRect.height;
				_areaHandle.SetPosition (_areaHRect);
				_areaHandle.limits.x = _cornerHRect.width * 0.5f;
				_areaHandle.limits.y = _cornerHRect.height * 0.5f;
				_areaHandle.limits.z = canvas.canvasWidth - _areaHRect.width - _cornerHRect.width * 0.5f;
				_areaHandle.limits.w = canvas.canvasHeight - _areaHRect.height - _cornerHRect.height * 0.5f;

				if (hasPivot) { 
					float size = _pivotElemHandleSize / canvas.currentZoom;
					_pivotHRect.x = _elemRect.x + _elemRect.width * pivot.x - size * 0.5f;
					_pivotHRect.width = size;
					_pivotHRect.y = _elemRect.y + _elemRect.height * (1f - pivot.y) - size * 0.5f;
					_pivotHRect.height = size;
					_pivotHandle.SetPosition (_pivotHRect);
					_pivotHandle.limits.x = _elemRect.min.x - size * 0.5f;
					_pivotHandle.limits.y = _elemRect.min.y - size * 0.5f;
					_pivotHandle.limits.z = _elemRect.max.x - size * 0.5f;
					_pivotHandle.limits.w = _elemRect.max.y - size * 0.5f;
				}
			}
            #endregion
        }
        #endregion

    	#region Texture Graph Elements
        public class TextureGraphElement : GraphElement {}
        public class AreaGraphElement : GraphElement {}
		public class MoveHandleGraphElement : GraphElement {
			public int areaId;
			public HandleType handleType = HandleType.Area;
			public Vector4 limits = Vector4.zero;
			public enum HandleType {
				Area,
				TopLeft,
				TopRight,
				BottomLeft,
				BottomRight,
				Top,
				Bottom,
				Left,
				Right,
				Pivot
			}
			public MoveHandleGraphElement (int areaId, HandleType handleType) {
				this.areaId = areaId;
				this.handleType = handleType;
				capabilities = Capabilities.Selectable | Capabilities.Movable;
			}
			public void SetMovable (bool isMovable) {
				SetMovable (isMovable, new string[0], new string[0]);
			}
			public void SetMovable (bool isMovable, string[] addClasses) {
				SetMovable (isMovable, addClasses, new string[0]);
			}
			public void SetMovable (bool isMovable, string[] addClasses, string[] removeClasses) {
				if (isMovable) {
					capabilities = Capabilities.Selectable | Capabilities.Movable;
				} else {
					capabilities = Capabilities.Selectable;
				}
				for (int i = 0; i < addClasses.Length; i++) {
					this.AddToClassList (addClasses [i]);
				}
				for (int i = 0; i < removeClasses.Length; i++) {
					this.RemoveFromClassList (removeClasses [i]);
				}
			}
		}
        #endregion

        #region Vars
		public float canvasSize = 2000f;
        public float canvasWidth = 2000f;
        public float canvasHeight = 2000f;
		public Rect guiRect = new Rect (0, 0, 1, 1);
		protected float currentZoom = 1f;
		protected Vector2 contentOffset = Vector2.zero;
		protected string debugInfo = string.Empty;
        protected Texture2D _targetTexture;
        protected float _targetTextureRatio = 0f;
        protected TextureGraphElement _targetTextureElem = new TextureGraphElement ();
        protected Dictionary<int, Area> _areas = new Dictionary<int, Area> ();
        #endregion

        #region Config Vars
		/// <summary>
		/// Path to the graph style.
		/// </summary>
		public virtual string graphViewStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/TextureCanvasStyle.uss"; }
		}
		private static string targetTextureClass = "target-texture";
        #endregion

		#region Delegates
		public delegate void OnZoomDelegate (float currentZoom, float previousZoom);
		public delegate void OnOffsetDelegate (Vector2 currentOffset, Vector2 previousOffset);
		public delegate void OnAreaDelegate (Area area);
		public OnZoomDelegate onZoomDone;
		public OnOffsetDelegate onPanDone;
		public OnAreaDelegate onBeforeEditArea;
		public OnAreaDelegate onEditArea;
		#endregion

        #region GUI Vars
		public VisualTreeAsset nodeXml;
        public StyleSheet nodeStyle;
		public StyleSheet graphViewStyle;
		/// <summary>
		/// GUI container.
		/// </summary>
		public VisualElement guiContainer;
		/// <summary>
		/// Name for the GUI container.
		/// </summary>
		private static string guiContainerName = "gui-container";
        #endregion

        #region Events
        void OnDestroy () {
            _targetTexture = null;
        }
        #endregion

        #region Init/Destroy
        public void Init (Vector2 offset, float zoom) {
			// Zoom
            this.SetupZoom (0.04f, ContentZoomer.DefaultMaxScale, 0.1f, zoom);
			currentZoom = zoom;
			this.viewTransform.scale = new Vector3 (zoom, zoom, zoom);

			// Offset
			contentOffset = offset;
			viewTransform.position = offset;

			// Manipulators.
            this.AddManipulator(new ContentDragger());
			StructureSelectionDragger selectionDragger = new StructureSelectionDragger ();
			selectionDragger.moveFilter = OnHandleMoveFilter;
			selectionDragger.onMove = OnHandleMove;
			this.AddManipulator(selectionDragger);
			
			/*
			this.contentContainer.AddManipulator (selectionDragger);
			this.contentViewContainer.AddManipulator (selectionDragger);
			*/
			this.AddManipulator(new ClickSelector());
            this.RegisterCallback<KeyDownEvent>(KeyDown);

			// Events.
			this.graphViewChanged = _GraphViewChanged;
            this.viewTransformChanged = _ViewTransformChanged;

			// Grid.
            GridBackground gridBackground = new GridBackground() { name = "Grid" };
			this.Add(gridBackground);
			gridBackground.SendToBack();


            // Texture Elem.
            this.Add (_targetTextureElem);
            _targetTextureElem.SetPosition (new Rect (0, 0, 100, 200));
			_targetTextureElem.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
			_targetTextureElem.AddToClassList (targetTextureClass);
            this.AddElement (_targetTextureElem);

			graphViewStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(graphViewStylePath);

			if (graphViewStyle != null) {
				this.styleSheets.Add (graphViewStyle);
			}

			guiContainer = new VisualElement ();
			guiContainer.name = guiContainerName;
			this.Add (guiContainer);
        }
		private Rect OnHandleMoveFilter (GraphElement element, Rect originalPos, Rect newPos) {
			if (element is MoveHandleGraphElement) {
				MoveHandleGraphElement moveHandle = (MoveHandleGraphElement)element;
				if (newPos.x < moveHandle.limits.x) {
					newPos.x = moveHandle.limits.x;
				}
				if (newPos.x > moveHandle.limits.z) {
					newPos.x = moveHandle.limits.z;
				}
				if (newPos.y < moveHandle.limits.y) {
					newPos.y = moveHandle.limits.y;
				}
				if (newPos.y > moveHandle.limits.w) {
					newPos.y = moveHandle.limits.w;
				}
			}
			return newPos;
		}
		private void OnHandleMove (GraphElement element, Rect originalPos, Rect newPos) {
			if (element is MoveHandleGraphElement) {
				MoveHandleGraphElement handleElement = (MoveHandleGraphElement)element;
				if (_areas.ContainsKey (handleElement.areaId)) {
					_areas [handleElement.areaId].PreviewAreaRect (handleElement, newPos.position - originalPos.position, this);
				}
			}
		}
        public void Show () {
            this.style.display = DisplayStyle.Flex;
        }
        public void Hide () {
            this.style.display = DisplayStyle.None;
        }
        /// <summary>
        /// Loads a texture on the canvas.
        /// </summary>
        /// <param name="texture">Texture2D instance to load.</param>
        /// <returns><c>True</c> if the texture has been loaded on the canvas.</returns>
        public bool SetTexture (Texture2D texture) {
            if (texture != _targetTexture) {
                _targetTexture = texture;
				if (_targetTexture != null) {
					_targetTextureElem.style.backgroundImage = _targetTexture;

					if (_targetTexture.height > 0 && _targetTexture.width > 0) {
						_targetTextureRatio = (float)_targetTexture.width / (float)_targetTexture.height;
					} else {
						_targetTextureRatio = 1f;
					}
					if (_targetTexture.width > _targetTexture.height) {
						canvasWidth = canvasSize;
						canvasHeight = canvasSize / _targetTextureRatio;
					} else {
						canvasWidth = canvasSize * _targetTextureRatio;
						canvasHeight = canvasSize;
					}
					_targetTextureElem.SetPosition (new Rect (0f, 0f, canvasWidth, canvasHeight));
				}
                return true;
            }
            return false;
        }
		public void ClearTexture () {
			_targetTexture = null;
			_targetTextureElem.style.backgroundImage = null;
		}
		/// <summary>
		/// Sets the content view of the canvas to center selection on the graph without affecting te pan value.
		/// </summary>
		/// <param name="offset">Content view offset.</param>
		public void SetContentViewOffset (Vector2 offset) {
			this.viewTransform.position = (Vector3)offset;
			contentOffset.x = offset.x;
			contentOffset.y = offset.y;
		}
		/// <summary>
		/// Build the contextual menu to show on the graph canvas.
		/// </summary>
		/// <param name="evt"></param>
		public override void BuildContextualMenu (ContextualMenuPopulateEvent evt) {
			//var position = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition - contentOffset);
			var position = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

			evt.menu.AppendAction("Re-center Texture", 
				(e) => { CenterTexture (); }, DropdownMenuAction.AlwaysEnabled);
        }
		/// <summary>
		/// Centers the texture to the available canvas rect.
		/// </summary>
		public void CenterTexture () {
			if (guiRect.width > 0 && guiRect.height > 0) {
				float rectRatio = guiRect.width / guiRect.height;
				if (rectRatio > 1f) {
					float newZoom = guiRect.height / canvasHeight;
					SetPanZoom (new Vector2 ((guiRect.width / newZoom - canvasWidth) * newZoom * 0.5f, 0f), newZoom);
				}
				// Taller texture.
				else {
					float newZoom = guiRect.width / canvasWidth;
					SetPanZoom (new Vector2 (0f, (guiRect.height / newZoom - canvasHeight) * newZoom * 0.5f), newZoom);
				}
			}
		}
        #endregion

        #region Graph Events
        private GraphViewChange _GraphViewChanged(GraphViewChange graphViewChange) {
			// Elements MOVED.
			if (graphViewChange.movedElements != null && graphViewChange.movedElements.Count > 0) {
				if (graphViewChange.movedElements[0] is MoveHandleGraphElement) {
					MoveHandleGraphElement moveHandle = (MoveHandleGraphElement)graphViewChange.movedElements[0];
					if (_areas.ContainsKey (moveHandle.areaId)) {
						onBeforeEditArea (_areas [moveHandle.areaId]);
						_areas [moveHandle.areaId].ApplyPreviewAreaRect (moveHandle, this);
						onEditArea (_areas [moveHandle.areaId]);
					}
				}
				/*
				List<StructureNode> movedNodes = new List<StructureNode> ();
				for (int i = 0; i < graphViewChange.movedElements.Count; i++) {
					movedNodes.Add (graphViewChange.movedElements [i] as StructureNode);
				}
				if (movedNodes.Count > 0) {
					onMoveNodes?.Invoke (movedNodes, graphViewChange.moveDelta);
				}
				*/
			}

			if (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Count > 0) {
				// Elements REMOVED (Nodes or edges).
				/*
				List<StructureNode> nodesToRemove = new List<StructureNode> ();
				List<Edge> edgesToRemove = new List<Edge> ();
				for (int i = 0; i < graphViewChange.elementsToRemove.Count; i++) {
					StructureNode pipelineNodeToRemove = graphViewChange.elementsToRemove [i] as StructureNode;
					if (pipelineNodeToRemove != null) {
						nodesToRemove.Add (pipelineNodeToRemove);
					}
					Edge edgeToRemove = graphViewChange.elementsToRemove [i] as Edge;
					if (edgeToRemove != null) {
						edgesToRemove.Add (edgeToRemove);
					}
				}
				if (nodesToRemove.Count > 0) {
					bool hasRemoved = RemoveNodes (nodesToRemove);
					if (!hasRemoved) {
						graphViewChange.elementsToRemove.Clear ();
					}
				} else if (edgesToRemove.Count > 0 && !removingEdgesFromRemoveNode) {
					bool hasRemoved = RemoveConnections (edgesToRemove);
					if (!hasRemoved) {
						graphViewChange.elementsToRemove.Clear ();
					}
				}
				*/
			}

			// Elements CONNECTED.
			if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0) {
				/*
				StructureNode parentNode;
				StructureNode childNode;
				bool isUpstream = false;
				if (graphViewChange.edgesToCreate [0].input.name.Equals (StructureNode.upChildrenPortName)) {
					isUpstream = true;
				} else {
					isUpstream = false;
				}
				if (isUpstream) {
					parentNode = graphViewChange.edgesToCreate [0].input.node as StructureNode;
					childNode = graphViewChange.edgesToCreate [0].output.node as StructureNode;
				} else {
					parentNode = graphViewChange.edgesToCreate [0].output.node as StructureNode;
					childNode = graphViewChange.edgesToCreate [0].input.node as StructureNode;
				}
                bool connectionAdded = AddConnectionInternal (parentNode, childNode);
                if (connectionAdded) {
                    SetEdgeUserData (graphViewChange.edgesToCreate [0], parentNode, childNode);
                } else {
                    graphViewChange.edgesToCreate.Clear ();
                }
				*/
			}
            
			return graphViewChange;
		}
		public void SetPanZoom (Vector2 pan, float zoom) {
			this.UpdateViewTransform (pan, new Vector3 (zoom, zoom, zoom));
			
		}
        private void _ViewTransformChanged (GraphView graphView) {
			// If zoom done.
			if (this.scale != currentZoom) {
				onZoomDone?.Invoke (this.scale, currentZoom);
				currentZoom = this.scale;
				var areasEnum = _areas.GetEnumerator ();
				while (areasEnum.MoveNext ()) {
					areasEnum.Current.Value.OnZoom (this);
				}
			}
			// If pan done.
			if ((Vector2)this.viewTransform.position != contentOffset) {
				onPanDone?.Invoke (this.viewTransform.position, contentOffset);
				//contentOffset = this.viewTransform.position;
				SetContentViewOffset (this.viewTransform.position);
			}
        }
		private void KeyDown(KeyDownEvent evt)
		{
		
		}
        #endregion

        #region Areas Management
		/// <summary>
		/// Returns an Area instance registered on this canvas.
		/// </summary>
		/// <param name="areaId">Area identifier.</param>
		/// <returns>Area instance if found, null otherwise.</returns>
		public Area GetArea (int areaId) {
			if (_areas.ContainsKey (areaId)) {
				return _areas[areaId];
			}
			return null;
		}
		/// <summary>
		/// Registers an Area to be managed by this Texture Canvas.
		/// </summary>
		/// <param name="areaId">Area identifier.</param>
		/// <param name="xPos">X position in GL coordinates (0-1).</param>
		/// <param name="yPos">Y position in GL coordinates (0-1).</param>
		/// <param name="width">Width in GL units (0-1)</param>
		/// <param name="height">Height in GL units (0-1)</param>
		/// <returns><c>True</c> if the area was registered.</returns>
        public bool RegisterArea (int areaId, float xPos = 0f, float yPos = 0f, float width = 1f, float height = 1f) {
            if (!_areas.ContainsKey (areaId)) {
                Rect rect = new Rect (xPos, yPos, width, height);
                Area area = new Area (areaId, rect, this);
				_areas.Add (areaId, area);
				area.AddElementsToCanvas (this);
                return true;
            }
            return false;
        }
		/// <summary>
		/// Registers an Area with pivot point to be managed by this Texture Canvas.
		/// </summary>
		/// <param name="areaId">Area identifier.</param>
		/// <param name="xPos">X position in GL coordinates (0-1).</param>
		/// <param name="yPos">Y position in GL coordinates (0-1).</param>
		/// <param name="width">Width in GL units (0-1)</param>
		/// <param name="height">Height in GL units (0-1)</param>
		/// <param name="xPos">X pivot position in GL coordinates (0-1).</param>
		/// <param name="yPos">Y pivot position in GL coordinates (0-1).</param>
		/// <returns><c>True</c> if the area was registered.</returns>
        public bool RegisterArea (int areaId, float xPos = 0f, float yPos = 0f, float width = 1f, float height = 1f, float pivotX = 0f, float pivotY = 0f) {
            if (!_areas.ContainsKey (areaId)) {
                Rect rect = new Rect (xPos, yPos, width, height);
				Vector2 pivot = new Vector2 (pivotX, pivotY);
                Area area = new Area (areaId, rect, pivot, this);
				_areas.Add (areaId, area);
				area.AddElementsToCanvas (this);
                return true;
            }
            return false;
        }
		/// <summary>
		/// Sets the Rect value on an Area instance managed by this Texture Canvas.
		/// </summary>
		/// <param name="areaId">Area identifier.</param>
		/// <param name="rect">Rect in GL coordinates (0-1).</param>
		/// <returns><c>True</c> if the Rect value was set.</returns>
		public bool SetAreaRect (int areaId, Rect rect) {
			if (_areas.ContainsKey (areaId)) {
				_areas[areaId].SetAreaRect (rect, this);
				return true;
			}
			return false;
		}
		/// <summary>
		/// Sets the Rect and Pivot values on an Area instance managed by this Texture Canvas.
		/// </summary>
		/// <param name="areaId">Area identifier.</param>
		/// <param name="rect">Rect in GL coordinates (0-1).</param>
		/// <param name="pivot">Pivot value in GL coordinates (0-1).</param>
		/// <returns><c>True</c> if the Rect value was set.</returns>
		public bool SetAreaRectAndPivot (int areaId, Rect rect, Vector2 pivot) {
			if (_areas.ContainsKey (areaId)) {
				_areas[areaId].SetAreaRectAndPivot (rect, pivot, this);
				return true;
			}
			return false;
		}
        #endregion

		#region Debug
		/// <summary>
		/// Get a string with debug information about this mesh view.
		/// </summary>
		/// <returns>String with debug information.</returns>
		public string GetDebugInfo () {
			debugInfo = string.Empty; 
			// Print offset and zoom.
			debugInfo += string.Format ("Offset: ({0}, {1})\n", contentOffset.x, contentOffset.y);
			debugInfo += string.Format ("Zoom: {0}\n", currentZoom);
			/*
			for (int i = 0; i < _meshes.Count; i++) {
				if (_meshes [i].isReadable) {
					debugInfo += string.Format ("Mesh {0}, submeshes: {1}, vertices: {2}, tris: {3}\n", i, _meshes [i].subMeshCount, _meshes [i].vertexCount, _meshes [i].triangles.Length);
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
			*/
			return debugInfo;
		}
		#endregion
    }
}
