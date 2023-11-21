using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor;

using Broccoli.Pipe;
using Broccoli.Factory;
using Broccoli.TreeNodeEditor;
using Broccoli.Base;

namespace Broccoli.BroccoEditor
{
    public class SproutLabMappingPanel {
        #region Vars
        bool isInit = false;
        static string containerName = "mappingPanel";
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
		private VisualTreeAsset panelXml;
		/// <summary>
		/// Side panel style.
		/// </summary>
		private StyleSheet panelStyle;
		/// <summary>
		/// Path to the side panel xml.
		/// </summary>
		private string panelXmlPath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabMappingPanelView.uxml"; }
		}
		/// <summary>
		/// Path to the side style.
		/// </summary>
		private string panelStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/SproutLabPanelStyle.uss"; }
		}
		private static List<string> optionItems = new List<string> {"Stem", "Sprout A", "Sprout B"};
		private static List<string> optionCrownItems = new List<string> {"Stem", "Sprout A", "Sprout B", "Crown"};
		private Action<VisualElement, int> bindOptionItem = (e, i) => (e as Label).text = optionItems[i];
		private Action<VisualElement, int> bindOptionCrownItem = (e, i) => (e as Label).text = optionCrownItems[i];
		private static string optionsListName = "options-list";
		private static string stemContainerName = "container-stem";
		private static string sproutAContainerName = "container-sprout-a";
		private static string sproutBContainerName = "container-sprout-b";
		private static string crownContainerName = "container-sprout-crown";

		private static string sproutATintColorName = "sprout-a-tint-color";
		private static string sproutATintName = "sprout-a-tint";
		private static string sproutATintModeName = "sprout-a-tint-mode";
		private static string sproutATintModeInvertName = "sprout-a-tint-mode-invert";
		private static string sproutATintVarianceName = "sprout-a-tint-variance";
		private static string sproutAShadeName = "sprout-a-shade";
		private static string sproutASaturationName = "sprout-a-saturation";
		private static string sproutASaturationModeName = "sprout-a-saturation-mode";
		private static string sproutASaturationModeInvertName = "sprout-a-saturation-mode-invert";
		private static string sproutASaturationVarianceName = "sprout-a-saturation-variance";
		private static string sproutAMetallicName = "sprout-a-metallic";
		private static string sproutAGlossinessName = "sprout-a-glossiness";
		private static string sproutASubsurfaceName = "sprout-a-subsurface";

		private static string sproutBTintColorName = "sprout-b-tint-color";
		private static string sproutBTintName = "sprout-b-tint";
		private static string sproutBTintModeName = "sprout-b-tint-mode";
		private static string sproutBTintModeInvertName = "sprout-b-tint-mode-invert";
		private static string sproutBTintVarianceName = "sprout-b-tint-variance";
		private static string sproutBShadeName = "sprout-b-shade";
		private static string sproutBSaturationName = "sprout-b-saturation";
		private static string sproutBSaturationModeName = "sprout-b-saturation-mode";
		private static string sproutBSaturationModeInvertName = "sprout-b-saturation-mode-invert";
		private static string sproutBSaturationVarianceName = "sprout-b-saturation-variance";
		private static string sproutBMetallicName = "sprout-b-metallic";
		private static string sproutBGlossinessName = "sprout-b-glossiness";
		private static string sproutBSubsurfaceName = "sprout-b-subsurface";

		private static string sproutCrownTintColorName = "sprout-crown-tint-color";
		private static string sproutCrownTintName = "sprout-crown-tint";
		private static string sproutCrownTintModeName = "sprout-crown-tint-mode";
		private static string sproutCrownTintModeInvertName = "sprout-crown-tint-mode-invert";
		private static string sproutCrownTintVarianceName = "sprout-crown-tint-variance";
		private static string sproutCrownShadeName = "sprout-crown-shade";
		private static string sproutCrownSaturationName = "sprout-crown-saturation";
		private static string sproutCrownSaturationModeName = "sprout-crown-saturation-mode";
		private static string sproutCrownSaturationModeInvertName = "sprout-crown-saturation-mode-invert";
		private static string sproutCrownSaturationVarianceName = "sprout-crown-saturation-variance";
		private static string sproutCrownMetallicName = "sprout-crown-metallic";
		private static string sproutCrownGlossinessName = "sprout-crown-glossiness";
		private static string sproutCrownSubsurfaceName = "sprout-crown-subsurface";

		private ListView optionsList;
		private VisualElement stemContainer;
		private VisualElement sproutAContainer;
		private VisualElement sproutBContainer;
		private VisualElement sproutCrownContainer;

		private ColorField sproutATintColorElem;
		private MinMaxSlider sproutATintElem;
		private EnumField sproutATintModeElem;
		private Toggle sproutATintModeInvertElem;
		private Slider sproutATintVarianceElem;
		private MinMaxSlider sproutAShadeElem;
		private MinMaxSlider sproutASaturationElem;
		private EnumField sproutASaturationModeElem;
		private Toggle sproutASaturationModeInvertElem;
		private Slider sproutASaturationVarianceElem;
		private Slider sproutAMetallicElem;
		private Slider sproutAGlossinessElem;
		private Slider sproutASubsurfaceElem;

		private ColorField sproutBTintColorElem;
		private MinMaxSlider sproutBTintElem;
		private EnumField sproutBTintModeElem;
		private Toggle sproutBTintModeInvertElem;
		private Slider sproutBTintVarianceElem;
		private MinMaxSlider sproutBShadeElem;
		private MinMaxSlider sproutBSaturationElem;
		private EnumField sproutBSaturationModeElem;
		private Toggle sproutBSaturationModeInvertElem;
		private Slider sproutBSaturationVarianceElem;
		private Slider sproutBMetallicElem;
		private Slider sproutBGlossinessElem;
		private Slider sproutBSubsurfaceElem;

		private ColorField sproutCrownTintColorElem;
		private MinMaxSlider sproutCrownTintElem;
		private EnumField sproutCrownTintModeElem;
		private Toggle sproutCrownTintModeInvertElem;
		private Slider sproutCrownTintVarianceElem;
		private MinMaxSlider sproutCrownShadeElem;
		private MinMaxSlider sproutCrownSaturationElem;
		private EnumField sproutCrownSaturationModeElem;
		private Toggle sproutCrownSaturationModeInvertElem;
		private Slider sproutCrownSaturationVarianceElem;
		private Slider sproutCrownMetallicElem;
		private Slider sproutCrownGlossinessElem;
		private Slider sproutCrownSubsurfaceElem;
        #endregion

        #region Constructor
        public SproutLabMappingPanel (SproutLabEditor sproutLabEditor) {
            Initialize (sproutLabEditor);
        }
        #endregion

        #region Init
		public void SproutSelected () {
			if (sproutLabEditor.snapSettings.hasCrown) {
				optionsList.bindItem = bindOptionCrownItem;
				optionsList.itemsSource = optionCrownItems;
				optionsList.selectedIndex = 0;
			} else {
				optionsList.bindItem = bindOptionItem;
				optionsList.itemsSource = optionItems;
			}
			#if UNITY_2021_2_OR_NEWER
			optionsList.Rebuild ();
			#else
			optionsList.Refresh ();
			#endif
		}
		private void OnSelectionChanged(IEnumerable<object> selectedItems) {
			stemContainer.style.display = DisplayStyle.None;
			sproutAContainer.style.display = DisplayStyle.None;
			sproutBContainer.style.display = DisplayStyle.None;
			sproutCrownContainer.style.display = DisplayStyle.None;
			if (optionsList.selectedIndex == 1 ) {
				sproutAContainer.style.display = DisplayStyle.Flex;
			} else if (optionsList.selectedIndex == 2) {
				sproutBContainer.style.display = DisplayStyle.Flex;
			} else if (optionsList.selectedIndex == 3) {
				sproutCrownContainer.style.display = DisplayStyle.Flex;
			} else {
				stemContainer.style.display = DisplayStyle.Flex;
			}
		}
        public void Initialize (SproutLabEditor sproutLabEditor) {
			this.sproutLabEditor = sproutLabEditor;
            if (!isInit) {
                // Start the container UIElement.
                container = new VisualElement ();
                container.name = containerName;

				// Load the VisualTreeAsset from a file 
				panelXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panelXmlPath);

				// Create a new instance of the root VisualElement
				container.Add (panelXml.CloneTree());

				// Init List and Containers.
				optionsList = container.Q<ListView> (optionsListName);
				stemContainer = container.Q<VisualElement> (stemContainerName);
				sproutAContainer = container.Q<VisualElement> (sproutAContainerName);
				sproutBContainer = container.Q<VisualElement> (sproutBContainerName);
				sproutCrownContainer = container.Q<VisualElement> (crownContainerName);

				// The "makeItem" function will be called as needed
				// when the ListView needs more items to render
				Func<VisualElement> makeItem = () => new Label();

				optionsList.makeItem = makeItem;
				optionsList.bindItem = bindOptionItem;
				optionsList.itemsSource = optionItems;
				#if UNITY_2021_2_OR_NEWER
				optionsList.Rebuild ();
				#else
				optionsList.Refresh ();
				#endif
				
				#if UNITY_2022_2_OR_NEWER
				optionsList.selectionChanged -= OnSelectionChanged;
				optionsList.selectionChanged += OnSelectionChanged;
				#else
				optionsList.onSelectionChange -= OnSelectionChanged;
				optionsList.onSelectionChange += OnSelectionChanged;
				#endif
				optionsList.selectedIndex = 0;


				InitializeSproutStyleA ();
				InitializeSproutStyleB ();
				InitializeSproutStyleCrown ();

				isInit = true;

                RefreshValues ();
            }
        }
		void InitializeSproutStyleA () {
			// SPROUT A.
			// Saturation Range.
			sproutASaturationElem = container.Q<MinMaxSlider> (sproutASaturationName);
			sproutASaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorSaturation = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutASaturationElem);

			// Saturation Mode.
			sproutASaturationModeElem = container.Q<EnumField> (sproutASaturationModeName);
			sproutASaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutASaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutASaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutASaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutASaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutASaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutASaturationModeInvertElem = container.Q<Toggle> (sproutASaturationModeInvertName);
			sproutASaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutASaturationVarianceElem = container.Q<Slider> (sproutASaturationVarianceName);
			sproutASaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutASaturationVarianceElem);

			// Tint Color.
			sproutATintColorElem = container.Q<ColorField> (sproutATintColorName);
			sproutATintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutATintElem = container.Q<MinMaxSlider> (sproutATintName);
			sproutATintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorTint = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutATintElem);

			// Tint Mode.
			sproutATintModeElem = container.Q<EnumField> (sproutATintModeName);
			sproutATintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutATintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutATintModeInvertElem.style.display = DisplayStyle.None;
						sproutATintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutATintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutATintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutATintModeInvertElem = container.Q<Toggle> (sproutATintModeInvertName);
			sproutATintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutATintVarianceElem = container.Q<Slider> (sproutATintVarianceName);
			sproutATintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutATintVarianceElem);
			
			// Shade
			sproutAShadeElem = container.Q<MinMaxSlider> (sproutAShadeName);
			sproutAShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorShade = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (0);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutAShadeElem);

			// Metallic Slider.
			sproutAMetallicElem = container.Q<Slider> (sproutAMetallicName);
			sproutAMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutAMetallicElem);

			// Glossiness Slider.
			sproutAGlossinessElem = container.Q<Slider> (sproutAGlossinessName);
			sproutAGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutAGlossinessElem);

			// Subsurface Slider.
			sproutASubsurfaceElem = container.Q<Slider> (sproutASubsurfaceName);
			sproutASubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleA.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutASubsurfaceElem);
		}
		void InitializeSproutStyleB () {
			// SPROUT B.
			// Saturation Range.
			sproutBSaturationElem = container.Q<MinMaxSlider> (sproutBSaturationName);
			sproutBSaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorSaturation = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBSaturationElem);

			// Saturation Mode.
			sproutBSaturationModeElem = container.Q<EnumField> (sproutBSaturationModeName);
			sproutBSaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutBSaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutBSaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutBSaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBSaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBSaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutBSaturationModeInvertElem = container.Q<Toggle> (sproutBSaturationModeInvertName);
			sproutBSaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutBSaturationVarianceElem = container.Q<Slider> (sproutBSaturationVarianceName);
			sproutBSaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBSaturationVarianceElem);

			// Tint Color.
			sproutBTintColorElem = container.Q<ColorField> (sproutBTintColorName);
			sproutBTintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutBTintElem = container.Q<MinMaxSlider> (sproutBTintName);
			sproutBTintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorTint = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBTintElem);

			// Tint Mode.
			sproutBTintModeElem = container.Q<EnumField> (sproutBTintModeName);
			sproutBTintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutBTintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutBTintModeInvertElem.style.display = DisplayStyle.None;
						sproutBTintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutBTintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutBTintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutBTintModeInvertElem = container.Q<Toggle> (sproutBTintModeInvertName);
			sproutBTintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutBTintVarianceElem = container.Q<Slider> (sproutBTintVarianceName);
			sproutBTintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBTintVarianceElem);
			
			// Shade
			sproutBShadeElem = container.Q<MinMaxSlider> (sproutBShadeName);
			sproutBShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (1);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutBShadeElem);

			// Metallic Slider.
			sproutBMetallicElem = container.Q<Slider> (sproutBMetallicName);
			sproutBMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBMetallicElem);

			// Glossiness Slider.
			sproutBGlossinessElem = container.Q<Slider> (sproutBGlossinessName);
			sproutBGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBGlossinessElem);

			// Subsurface Slider.
			sproutBSubsurfaceElem = container.Q<Slider> (sproutBSubsurfaceName);
			sproutBSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutBSubsurfaceElem);
		}
		void InitializeSproutStyleCrown () {
			// SPROUT B.
			// Saturation Range.
			sproutCrownSaturationElem = container.Q<MinMaxSlider> (sproutCrownSaturationName);
			sproutCrownSaturationElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorSaturation = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorSaturation = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownSaturationElem);

			// Saturation Mode.
			sproutCrownSaturationModeElem = container.Q<EnumField> (sproutCrownSaturationModeName);
			sproutCrownSaturationModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform);
			sproutCrownSaturationModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode = (BranchDescriptorCollection.SproutStyle.SproutSaturationMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
						sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownSaturationVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownSaturationVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Invert.
			sproutCrownSaturationModeInvertElem = container.Q<Toggle> (sproutCrownSaturationModeInvertName);
			sproutCrownSaturationModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutSaturationMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Saturation Variance.
			sproutCrownSaturationVarianceElem = container.Q<Slider> (sproutCrownSaturationVarianceName);
			sproutCrownSaturationVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownSaturationVarianceElem);

			// Tint Color.
			sproutCrownTintColorElem = container.Q<ColorField> (sproutCrownTintColorName);
			sproutCrownTintColorElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.colorTint = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Range.
			sproutCrownTintElem = container.Q<MinMaxSlider> (sproutCrownTintName);
			sproutCrownTintElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorTint = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorTint = newVal.y;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownTintElem);

			// Tint Mode.
			sproutCrownTintModeElem = container.Q<EnumField> (sproutCrownTintModeName);
			sproutCrownTintModeElem.Init (BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform);
			sproutCrownTintModeElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode = (BranchDescriptorCollection.SproutStyle.SproutTintMode)evt.newValue;
					if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
						sproutCrownTintModeInvertElem.style.display = DisplayStyle.None;
						sproutCrownTintVarianceElem.style.display = DisplayStyle.None;
					} else {
						sproutCrownTintModeInvertElem.style.display = DisplayStyle.Flex;
						sproutCrownTintVarianceElem.style.display = DisplayStyle.Flex;
					}
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Invert.
			sproutCrownTintModeInvertElem = container.Q<Toggle> (sproutCrownTintModeInvertName);
			sproutCrownTintModeInvertElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutTintMode = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});

			// Tint Variance.
			sproutCrownTintVarianceElem = container.Q<Slider> (sproutCrownTintVarianceName);
			sproutCrownTintVarianceElem?.RegisterValueChangedCallback(evt => {
				if (listenGUIEvents && evt.newValue != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintVariance = evt.newValue;
					OnEdit (false, false, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownTintVarianceElem);
			
			// Shade
			sproutCrownShadeElem = container.Q<MinMaxSlider> (sproutCrownShadeName);
			sproutCrownShadeElem?.RegisterValueChangedCallback(evt => {
				Vector2 newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade = newVal.x;
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade = newVal.y;
					OnEdit (true);
					UpdateShade (2);
				}
			});
			SproutLabEditor.SetupMinMaxSlider (sproutCrownShadeElem);

			// Metallic Slider.
			sproutCrownMetallicElem = container.Q<Slider> (sproutCrownMetallicName);
			sproutCrownMetallicElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownMetallicElem);

			// Glossiness Slider.
			sproutCrownGlossinessElem = container.Q<Slider> (sproutCrownGlossinessName);
			sproutCrownGlossinessElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness = newVal;
					OnEdit (true, true, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownGlossinessElem);

			// Subsurface Slider.
			sproutCrownSubsurfaceElem = container.Q<Slider> (sproutCrownSubsurfaceName);
			sproutCrownSubsurfaceElem?.RegisterValueChangedCallback(evt => {
				float newVal = evt.newValue;
				if (listenGUIEvents && newVal != evt.previousValue) {
					OnBeforeEdit ();
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface = newVal;
					OnEdit (true, true, false, false, true);
				}
			});
			SproutLabEditor.SetupSlider (sproutCrownSubsurfaceElem);
		}
		private void OnBeforeEdit () {
			sproutLabEditor.onBeforeEditBranchDescriptor?.Invoke (
			sproutLabEditor.selectedSnapshot, sproutLabEditor.branchDescriptorCollection);
		}
		private void OnEdit (
			bool updatePipeline = false, 
			bool updateCompositeMaterials = false, 
			bool updateAlbedoMaterials = false,
			bool updateExtrasMaterials = false,
			bool updateSubsurfaceMaterials = false)
		{
			// If a LOD is selected, return to geometry view.
			if (sproutLabEditor.selectedLODView != 0) {
				sproutLabEditor.ShowPreviewMesh ();
			}
			
			sproutLabEditor.onEditBranchDescriptor?.Invoke (
				sproutLabEditor.selectedSnapshot, sproutLabEditor.branchDescriptorCollection);
			if (updatePipeline) {
				sproutLabEditor.ReflectChangesToPipeline ();
			}
			if (updateCompositeMaterials) {
				UpdateCompositeMaterials ();
			}
			if (updateAlbedoMaterials) {
				UpdateAlbedoMaterials ();
			}
			if (updateExtrasMaterials) {
				UpdateExtrasMaterials ();
			}
			if (updateSubsurfaceMaterials) {
				UpdateSubsurfaceMaterials ();
			}
			sproutLabEditor.sproutSubfactory.sproutCompositeManager.Clear ();
		}
		void UpdateShade (int styleIndex) {
			int subMeshIndex;
			int subMeshCount;
			float minShade;
			float maxShade;
			BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
			if (styleIndex == 0) {
				subMeshIndex = snapshot.sproutASubmeshIndex;
				subMeshCount = snapshot.sproutASubmeshCount;
				minShade = sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorShade;
				maxShade = sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorShade;
			} else if (styleIndex == 1) {
				subMeshIndex = snapshot.sproutBSubmeshIndex;
				subMeshCount = snapshot.sproutBSubmeshCount;
				minShade = sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade;
				maxShade = sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade;
			} else {
				subMeshIndex = snapshot.sproutCrownSubmeshIndex;
				subMeshCount = snapshot.sproutCrownSubmeshCount;
				minShade = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade;
				maxShade = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade;
			}
			for (int i = subMeshIndex; i < subMeshIndex + subMeshCount; i++) {
				Broccoli.Component.SproutMapperComponent.UpdateShadeVariance (
				sproutLabEditor.sproutSubfactory.snapshotTreeMesh,
				minShade, maxShade, i);
			}
		}
		void UpdateCompositeMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateCompositeMaterials (sproutLabEditor.currentPreviewMaterials,
					sproutLabEditor.branchDescriptorCollection.sproutStyleA,
					sproutLabEditor.branchDescriptorCollection.sproutStyleB,
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown,
					snapshot.sproutASubmeshIndex,
					snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
			}
		}
		void UpdateAlbedoMaterials () {
			Material[] mats = null;
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_ALBEDO) {
				mats = sproutLabEditor.currentPreviewMaterials;
			} else if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				mats = sproutLabEditor.meshPreview.secondPassMaterials;
			}
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_ALBEDO || sproutLabEditor.currentMapView == SproutLabEditor.VIEW_COMPOSITE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateAlbedoMaterials (
					mats,
					sproutLabEditor.branchDescriptorCollection.sproutStyleA,
					sproutLabEditor.branchDescriptorCollection.sproutStyleB,
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown,
					sproutLabEditor.branchDescriptorCollection.branchColorShade,
					sproutLabEditor.branchDescriptorCollection.branchColorSaturation,
					snapshot.sproutASubmeshIndex,
					snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
			}
		}
		void UpdateExtrasMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_EXTRAS) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateExtrasMaterials (
					sproutLabEditor.currentPreviewMaterials, 
					sproutLabEditor.branchDescriptorCollection.sproutStyleA,
					sproutLabEditor.branchDescriptorCollection.sproutStyleB,
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown,
					snapshot.sproutASubmeshIndex,
					snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
			}
		}
		void UpdateSubsurfaceMaterials () {
			if (sproutLabEditor.currentMapView == SproutLabEditor.VIEW_SUBSURFACE) {
				BranchDescriptor snapshot = sproutLabEditor.selectedSnapshot;
				sproutLabEditor.sproutSubfactory.UpdateSubsurfaceMaterials (
					sproutLabEditor.currentPreviewMaterials, 
					sproutLabEditor.branchDescriptorCollection.sproutStyleA,
					sproutLabEditor.branchDescriptorCollection.sproutStyleB,
					sproutLabEditor.branchDescriptorCollection.sproutStyleCrown,
					sproutLabEditor.branchDescriptorCollection.branchColorSaturation,
					snapshot.sproutASubmeshIndex,
					snapshot.sproutBSubmeshIndex,
					snapshot.sproutCrownSubmeshIndex);
			}
		}
		public void RefreshValues () {
			if (sproutLabEditor.branchDescriptorCollection != null) {
				listenGUIEvents = false;

				RefreshStyleAValues ();
				RefreshStyleBValues ();
				RefreshStyleCrownValues ();

				listenGUIEvents = true;
			}
		}
		private void RefreshStyleAValues () {
			// SATURATION SPROUT A
			sproutASaturationElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorSaturation,
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutASaturationElem, sproutASaturationElem.value);
			sproutASaturationModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutASaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutASaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutASaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutASaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutASaturationModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.invertSproutSaturationMode;
			sproutASaturationVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutSaturationVariance;
			// TINT SPROUT A
			sproutATintColorElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.colorTint;
			sproutATintElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorTint,
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutATintElem, sproutATintElem.value);
			sproutATintModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutATintModeInvertElem.style.display = DisplayStyle.None;
				sproutATintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutATintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutATintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutATintModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.invertSproutTintMode;
			sproutATintVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.sproutTintVariance;
			// SHADE A
			sproutAShadeElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.minColorShade,
				sproutLabEditor.branchDescriptorCollection.sproutStyleA.maxColorShade);
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutAMetallicElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.metallic;
			SproutLabEditor.RefreshSlider (sproutAMetallicElem, sproutLabEditor.branchDescriptorCollection.sproutStyleA.metallic);
			sproutAGlossinessElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.glossiness;
			SproutLabEditor.RefreshSlider (sproutAGlossinessElem, sproutLabEditor.branchDescriptorCollection.sproutStyleA.glossiness);
			sproutASubsurfaceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleA.subsurface;
			SproutLabEditor.RefreshSlider (sproutASubsurfaceElem, sproutLabEditor.branchDescriptorCollection.sproutStyleA.subsurface);
		}
		private void RefreshStyleBValues () {
			// SATURATION SPROUT B
			sproutBSaturationElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorSaturation,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutBSaturationElem, sproutBSaturationElem.value);
			sproutBSaturationModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutBSaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutBSaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBSaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBSaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBSaturationModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutSaturationMode;
			sproutBSaturationVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutSaturationVariance;
			// TINT SPROUT A
			sproutBTintColorElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.colorTint;
			sproutBTintElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorTint,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutBTintElem, sproutBTintElem.value);
			sproutBTintModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutBTintModeInvertElem.style.display = DisplayStyle.None;
				sproutBTintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutBTintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutBTintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutBTintModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.invertSproutTintMode;
			sproutBTintVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.sproutTintVariance;
			// SHADE A
			sproutBShadeElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.minColorShade,
				sproutLabEditor.branchDescriptorCollection.sproutStyleB.maxColorShade);
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutBMetallicElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic;
			SproutLabEditor.RefreshSlider (sproutBMetallicElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.metallic);
			sproutBGlossinessElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness;
			SproutLabEditor.RefreshSlider (sproutBGlossinessElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.glossiness);
			sproutBSubsurfaceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface;
			SproutLabEditor.RefreshSlider (sproutBSubsurfaceElem, sproutLabEditor.branchDescriptorCollection.sproutStyleB.subsurface);
		}
		private void RefreshStyleCrownValues () {
			// SATURATION SPROUT CROWN
			sproutCrownSaturationElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorSaturation,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorSaturation);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownSaturationElem, sproutCrownSaturationElem.value);
			sproutCrownSaturationModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationMode == BranchDescriptorCollection.SproutStyle.SproutSaturationMode.Uniform) {
				sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownSaturationVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownSaturationModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownSaturationVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutCrownSaturationModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutSaturationMode;
			sproutCrownSaturationVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutSaturationVariance;
			// TINT SPROUT A
			sproutCrownTintColorElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.colorTint;
			sproutCrownTintElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorTint,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorTint);
			SproutLabEditor.RefreshMinMaxSlider (sproutCrownTintElem, sproutCrownTintElem.value);
			sproutCrownTintModeElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode;
			if (sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintMode == BranchDescriptorCollection.SproutStyle.SproutTintMode.Uniform) {
				sproutCrownTintModeInvertElem.style.display = DisplayStyle.None;
				sproutCrownTintVarianceElem.style.display = DisplayStyle.None;
			} else {
				sproutCrownTintModeInvertElem.style.display = DisplayStyle.Flex;
				sproutCrownTintVarianceElem.style.display = DisplayStyle.Flex;
			}
			sproutCrownTintModeInvertElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.invertSproutTintMode;
			sproutCrownTintVarianceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.sproutTintVariance;
			// SHADE A
			sproutCrownShadeElem.value = new Vector2 (
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.minColorShade,
				sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.maxColorShade);
			// METALLIC, GLOSSINESS, SUBSURFACE SPROUT A
			sproutCrownMetallicElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic;
			SproutLabEditor.RefreshSlider (sproutCrownMetallicElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.metallic);
			sproutCrownGlossinessElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness;
			SproutLabEditor.RefreshSlider (sproutCrownGlossinessElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.glossiness);
			sproutCrownSubsurfaceElem.value = sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface;
			SproutLabEditor.RefreshSlider (sproutCrownSubsurfaceElem, sproutLabEditor.branchDescriptorCollection.sproutStyleCrown.subsurface);
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
