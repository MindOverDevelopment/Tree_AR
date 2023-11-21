using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

using Broccoli.Pipe;
using Broccoli.TreeNodeEditor;
using Broccoli.Base;

namespace Broccoli.BroccoEditor
{
    public class SproutLabSettingsPanel {
        #region Vars
        bool isInit = false;
        static string containerName = "settingsPanel";
		SproutLabEditor sproutLabEditor = null;
		public bool requiresRepaint = false;
		bool listenGUIEvents = true;
        #endregion

        #region GUI Vars
        /// <summary>
        /// Container for the UI.
        /// </summary>
        VisualElement container;
        /// <summary>
        /// Rect used to draw the components in this panel.
        /// </summary>
        Rect rect;
		/// <summary>
		/// Side panel xml.
		/// </summary>
		private VisualTreeAsset settingsPanelXml;
		/// <summary>
		/// Side panel style.
		/// </summary>
		private StyleSheet settingsPanelStyle;
		/// <summary>
		/// Path to the side panel xml.
		/// </summary>
		private string settingsPanelXmlPath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabSettingsPanelView.uxml"; }
		}
		/// <summary>
		/// Path to the side style.
		/// </summary>
		private string settingsPanelStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabPanelStyle.uss"; }
		}
		private static List<string> settingsItems = new List<string> {"Canvas", "Gizmos"};
		private static string settingsListName = "settings-list";
		private static string canvasContainerName = "container-canvas";
		private static string gizmosContainerName = "container-gizmos";
		private static string bgColorName = "var-preview-bg-color";
		private static string planeSizeName = "var-preview-plane-size";
		private static string planeTintName = "var-preview-plane-tint";
		private static string gizmos3dName = "var-gizmos-3d";
		private static string gizmos3dSizeName = "var-gizmos-3d-size";
		private static string gizmosOutlineWidthName = "var-gizmos-outline-width";
		private static string gizmosOutlineAlphaName = "var-gizmos-outline-alpha";
		private static string gizmosColorName = "var-gizmos-color";
		private static string gizmosLineWidthName = "var-gizmos-line-width";
		private static string gizmosUnitSizeName = "var-gizmos-unit-size";
		private static string showRulerName = "var-show-ruler";
		private static string rulerColorName = "var-ruler-color";
		private ListView settingsList;
		private VisualElement canvasContainer;
		private VisualElement gizmosContainer;
		private ColorField bgColorElem;
		private Slider planeSizeElem;
		private ColorField planeTintElem;
		private Toggle gizmos3dElem;
		private Slider gizmos3dSizeElem;
		private Slider gizmosOutlineWidthElem;
		private Slider gizmosOutlineAlphaElem;
		private ColorField gizmosColorElem;
		private Slider gizmosLineWidthElem;
		private Slider gizmosUnitSizeElem;
		private Toggle showRulerElem;
		private ColorField rulerColorElem;
        #endregion

        #region Constructor
        public SproutLabSettingsPanel (SproutLabEditor sproutLabEditor) {
            Initialize (sproutLabEditor);
        }
        #endregion

        #region Init
		private void OnSelectionChanged(IEnumerable<object> selectedItems) {
			canvasContainer.style.display = DisplayStyle.None;
			gizmosContainer.style.display = DisplayStyle.None;
			if (settingsList.selectedIndex == 0) {
				canvasContainer.style.display = DisplayStyle.Flex;
			} else {
				gizmosContainer.style.display = DisplayStyle.Flex;
			}
		}
        public void Initialize (SproutLabEditor sproutLabEditor) {
			this.sproutLabEditor = sproutLabEditor;
            if (!isInit) {
                // Start the container UIElement.
                container = new VisualElement ();
                container.name = containerName;
                //sproutLabEditor.rootVisualElement.Add (container);

				// Load the VisualTreeAsset from a file 
				settingsPanelXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(settingsPanelXmlPath);

				// Create a new instance of the root VisualElement
				container.Add (settingsPanelXml.CloneTree());

				// Init List and Containers.
				settingsList = container.Q<ListView> (settingsListName);
				canvasContainer = container.Q<VisualElement> (canvasContainerName);
				gizmosContainer = container.Q<VisualElement> (gizmosContainerName);
				// The "makeItem" function will be called as needed
				// when the ListView needs more items to render
				Func<VisualElement> makeItem = () => new Label();
				settingsList.makeItem = makeItem;
				Action<VisualElement, int> bindSettingItem = (e, i) => (e as Label).text = settingsItems[i];
				settingsList.bindItem = bindSettingItem;
				settingsList.itemsSource = settingsItems;
				#if UNITY_2021_2_OR_NEWER
				settingsList.Rebuild ();
				#else
				settingsList.Refresh ();
				#endif
				
				#if UNITY_2022_2_OR_NEWER
				settingsList.selectionChanged -= OnSelectionChanged;
				settingsList.selectionChanged += OnSelectionChanged;
				#else
				settingsList.onSelectionChange -= OnSelectionChanged;
				settingsList.onSelectionChange += OnSelectionChanged;
				#endif
				settingsList.selectedIndex = 0;

				// Init the Bg Color field.
				bgColorElem = container.Q<ColorField> (bgColorName);
				bgColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.bgColor) {
						sproutLabEditor.branchDescriptorCollection.bgColor = evt.newValue;
						sproutLabEditor.meshPreview.backgroundColor = sproutLabEditor.branchDescriptorCollection.bgColor;
					}
				});

				// Init plane size field.
				planeSizeElem = container.Q<Slider> (planeSizeName);
				planeSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.planeSize) {
						sproutLabEditor.branchDescriptorCollection.planeSize = evt.newValue;
						SproutLabEditor.RefreshSlider (planeSizeElem, sproutLabEditor.branchDescriptorCollection.planeSize);
						Vector3 planeSize = new Vector3 (sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize);
						sproutLabEditor.meshPreview.SetPlaneMesh (planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeTint);
					}
				});
				SproutLabEditor.SetupSlider (planeSizeElem);

				// Init plane tint field.
				planeTintElem = container.Q<ColorField> (planeTintName);
				planeTintElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.planeTint) {
						sproutLabEditor.branchDescriptorCollection.planeTint = evt.newValue;
						Vector3 planeSize = new Vector3 (sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeSize);
						sproutLabEditor.meshPreview.SetPlaneMesh (planeSize, 
							sproutLabEditor.branchDescriptorCollection.planeTint);
					}
				});

				// Init gizmos color field.
				gizmosColorElem = container.Q<ColorField> (gizmosColorName);
				gizmosColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosColor) {
						sproutLabEditor.branchDescriptorCollection.gizmosColor = evt.newValue;
					}
				});

				// Init gizmo line width field.
				gizmosLineWidthElem = container.Q<Slider> (gizmosLineWidthName);
				#if UNITY_2020_2_OR_NEWER
				gizmosLineWidthElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosLineWidth) {
						sproutLabEditor.branchDescriptorCollection.gizmosLineWidth = evt.newValue;
						SproutLabEditor.RefreshSlider (gizmosLineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosLineWidth);
					}
				});
				SproutLabEditor.SetupSlider (gizmosLineWidthElem);
				#else
				gizmosLineWidthElem.style.display = DisplayStyle.None;
				#endif

				// Init gizmo unit size field.
				gizmosUnitSizeElem = container.Q<Slider> (gizmosUnitSizeName);
				gizmosUnitSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosUnitSize) {
						sproutLabEditor.branchDescriptorCollection.gizmosUnitSize = evt.newValue;
						SproutLabEditor.RefreshSlider (gizmosUnitSizeElem, sproutLabEditor.branchDescriptorCollection.gizmosUnitSize);
					}
				});
				SproutLabEditor.SetupSlider (gizmosUnitSizeElem);

				// Gizmo outline width field.
				gizmosOutlineWidthElem = container.Q<Slider> (gizmosOutlineWidthName);
				gizmosOutlineWidthElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth) {
						sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth = evt.newValue;
						SproutLabEditor.RefreshSlider (gizmosOutlineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth);
					}
				});
				SproutLabEditor.SetupSlider (gizmosOutlineWidthElem);

				// Gizmo outline alpha field.
				gizmosOutlineAlphaElem = container.Q<Slider> (gizmosOutlineAlphaName);
				gizmosOutlineAlphaElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha) {
						sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha = evt.newValue;
						SproutLabEditor.RefreshSlider (gizmosOutlineAlphaElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha);
					}
				});
				SproutLabEditor.SetupSlider (gizmosOutlineAlphaElem);

				// 3D gizmo.
				gizmos3dElem = container.Q<Toggle> (gizmos3dName);
				gizmos3dElem?.RegisterValueChangedCallback (evt => {
					sproutLabEditor.branchDescriptorCollection.showAxisGizmo = evt.newValue;
					sproutLabEditor.meshPreview.showAxis = evt.newValue;
				});

				// 3D gizmo size.
				gizmos3dSizeElem = container.Q<Slider> (gizmos3dSizeName);
				gizmos3dSizeElem?.RegisterValueChangedCallback(evt => {
					float newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.axisGizmoSize) {
						sproutLabEditor.branchDescriptorCollection.axisGizmoSize = evt.newValue;
						SproutLabEditor.RefreshSlider (gizmos3dSizeElem, sproutLabEditor.branchDescriptorCollection.axisGizmoSize);
						sproutLabEditor.meshPreview.axisGizmoSize = evt.newValue;
					}
				});
				SproutLabEditor.SetupSlider (gizmos3dSizeElem);

				// Show Ruler.
				showRulerElem = container.Q<Toggle> (showRulerName);
				showRulerElem?.RegisterValueChangedCallback (evt => {
					sproutLabEditor.branchDescriptorCollection.showRuler = evt.newValue;
					sproutLabEditor.meshPreview.showRuler = evt.newValue;
					if (evt.newValue) {
						rulerColorElem.style.display = DisplayStyle.Flex;
					} else {
						rulerColorElem.style.display = DisplayStyle.None;
					}
				});

				// Init ruler color field.
				rulerColorElem = container.Q<ColorField> (rulerColorName);
				rulerColorElem?.RegisterValueChangedCallback(evt => {
					Color newVal = evt.newValue;
					if (listenGUIEvents && newVal != sproutLabEditor.branchDescriptorCollection.rulerColor) {
						sproutLabEditor.branchDescriptorCollection.rulerColor = evt.newValue;
						sproutLabEditor.meshPreview.rulerColor = evt.newValue;
					}
				});


				isInit = true;

                RefreshValues ();
            }
        }
		public void RefreshValues () {
			if (sproutLabEditor.branchDescriptorCollection != null) {
				listenGUIEvents = false;
				bgColorElem.value = sproutLabEditor.branchDescriptorCollection.bgColor;
				SproutLabEditor.RefreshSlider (planeSizeElem, sproutLabEditor.branchDescriptorCollection.planeSize);
				planeTintElem.value = sproutLabEditor.branchDescriptorCollection.planeTint;
				gizmosColorElem.value = sproutLabEditor.branchDescriptorCollection.gizmosColor;
				gizmosLineWidthElem.value = sproutLabEditor.branchDescriptorCollection.gizmosLineWidth;
				gizmosUnitSizeElem.value = sproutLabEditor.branchDescriptorCollection.gizmosUnitSize;
				gizmos3dElem.value = sproutLabEditor.branchDescriptorCollection.showAxisGizmo;
				gizmos3dSizeElem.value = sproutLabEditor.branchDescriptorCollection.axisGizmoSize;
				sproutLabEditor.meshPreview.showAxis = sproutLabEditor.branchDescriptorCollection.showAxisGizmo;
				showRulerElem.value = sproutLabEditor.branchDescriptorCollection.showRuler;
				sproutLabEditor.meshPreview.showRuler = sproutLabEditor.branchDescriptorCollection.showRuler;
				rulerColorElem.value = sproutLabEditor.branchDescriptorCollection.rulerColor;
				sproutLabEditor.meshPreview.rulerColor = sproutLabEditor.branchDescriptorCollection.rulerColor;
				if (sproutLabEditor.branchDescriptorCollection.showRuler) {
					rulerColorElem.style.display = DisplayStyle.Flex;
				} else {
					rulerColorElem.style.display = DisplayStyle.None;
				}
				gizmosOutlineWidthElem.value = sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth;
				SproutLabEditor.RefreshSlider (gizmosOutlineWidthElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineWidth);
				gizmosOutlineAlphaElem.value = sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha;
				SproutLabEditor.RefreshSlider (gizmosOutlineAlphaElem, sproutLabEditor.branchDescriptorCollection.gizmosOutlineAlpha);
				listenGUIEvents = true;
			}
		}
		public void Attach () {
			if (!this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Add (container);
			}
		}
		public void Detach () {
			if (this.sproutLabEditor.rootVisualElement.Contains (container)) {
				this.sproutLabEditor.rootVisualElement.Remove (container);
			}
		}
		#endregion

		#region Side Panel 
		public void Repaint () {
			RefreshValues ();
			requiresRepaint = false;
		}
		private void SetupSlider (Slider slider, bool isInt = false) {
			slider?.RegisterValueChangedCallback(evt => {
				RefreshSlider (slider, evt.newValue, isInt);
			});
		}
		private void SetupMinMaxSlider (MinMaxSlider minMaxSlider, bool isInt = false) {
			minMaxSlider?.RegisterValueChangedCallback(evt => {
				RefreshMinMaxSlider (minMaxSlider, evt.newValue, isInt);
			});
		}
		private void RefreshSlider (Slider slider, float value, bool isInit = false) {
			Label info = slider.Q<Label>("info");
			if (info != null) {
				if (isInit) {
					info.text = string.Format ("{0:00}", Mathf.Round (value));
				} else {
					info.text = string.Format ("{0:00.00}", value);
				}
			}
		}
		private void RefreshMinMaxSlider (MinMaxSlider minMaxSlider, Vector2 value, bool isInit = false) {
			Label info = minMaxSlider.Q<Label>("info");
			if (info != null) {
				if (isInit) {
					info.text = string.Format ("{0:00}/{1:00}", Mathf.Round (value.x), Mathf.Round (value.y));
				} else {
					info.text = string.Format ("{0:00.00}/{1:00.00}", value.x, value.y);
				}
			}
		}
		public void OnUndoRedo () {
			//LoadSidePanelFields (selectedVariationGroup);
		}
        #endregion

        #region Draw
        public void SetVisible (bool visible) {
            if (visible) {
                container.style.display = DisplayStyle.Flex;
            } else {
                container.style.display = DisplayStyle.None;
            }
        }
        /// <summary>
        /// Sets the draw area for the components.
        /// </summary>
        /// <param name="refRect">Rect to draw the componentes.</param>
        public void SetRect (Rect refRect) {
            if (Event.current.type != EventType.Repaint) return;
            rect = refRect;
            container.style.marginTop = refRect.y;
            container.style.height = refRect.height;
        }
        #endregion
    }
}
