using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

using Broccoli.Base;
using Broccoli.Pipe;

namespace Broccoli.BroccoEditor
{
    public class PopupListComponent  {
        /*
        1. Create Popup VisualElement.
        2. Set title.
        3. Bind close function (button and escape).
        4. Call method on Show with position parameter.
        */
        #region Vars
		public bool alignLeft = true;
		public string title {
			get { return titleElem.text; }
			set { titleElem.text = value; }
		}
		public bool isOpen {
			get { return popupContainer.style.display == DisplayStyle.Flex; }
		}
        public float width {
            get { return modalElem.style.width.value.value; }
			set { modalElem.style.width = new StyleLength (value); }
        }
        public float height {
            get { return modalElem.style.height.value.value; }
			set { modalElem.style.height = new StyleLength (value); }
        }
        #endregion

		#region Delegates
		public delegate void OnPopupEvent (VisualElement popupContainer);
		public OnPopupEvent onBeforeOpen;
		public OnPopupEvent onOpen;
		public OnPopupEvent onBeforeClose;
		public OnPopupEvent onClose;
		#endregion

        #region GUI Vars
        public VisualElement popupContainer = null; 
		public VisualElement modalElem = null;
		public VisualElement modalBgElem = null;
		public VisualTreeAsset popupXml;
		public Label titleElem;
		public Button closeBtnElem;
		public ListView listElem;
		public static string popupContainerName = "popup-container";
		public static string popupModalName = "popup-modal";
		public static string popupModalBgName = "popup-modal-bg";
		private static string titleName = "popup-list-title";
		private static string closeBtnName = "popup-list-close-btn";
		private static string listName = "popup-list";
        #endregion

        #region Events
        #endregion

        #region Constructor
        public PopupListComponent (VisualElement parent) {
            string xmlFullPath = ExtensionManager.extensionPath + "Editor/Resources/GUI/PopupListView.uxml";
            popupXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(xmlFullPath);
            if (popupXml != null) {
                popupContainer = new VisualElement ();
				popupContainer.focusable = true; 

				// Popup.
                popupContainer.Add (popupXml.CloneTree ());

				// Modal (Dialog)
				modalElem = popupContainer.Q<VisualElement> (popupModalName);
				modalElem.style.backgroundColor = new Color (0.25f, 0.25f, 0.25f);
				modalElem.RemoveFromHierarchy ();

				//Modal Bg.
				modalBgElem = popupContainer.Q<VisualElement> (popupModalBgName);
				modalBgElem.RegisterCallback<MouseDownEvent> (evt => Close () );
				modalBgElem.RemoveFromHierarchy ();

				popupContainer.Add (modalBgElem); 
				popupContainer.Add (modalElem);

				// List.
				listElem = popupContainer.Q<ListView> (listName);

				popupContainer.style.position = UnityEngine.UIElements.Position.Absolute;
				popupContainer.style.right = 0; popupContainer.style.left = 0;
				popupContainer.style.top = 0; popupContainer.style.bottom = 0;
				popupContainer.style.display = DisplayStyle.None;
				popupContainer.name = popupContainerName;
				parent.Add (popupContainer);

				titleElem = popupContainer.Q<Label> (titleName);

				closeBtnElem = popupContainer.Q<Button> (closeBtnName);
				closeBtnElem.clicked -= Close;
				closeBtnElem.clicked += Close;
				popupContainer.RegisterCallback<AttachToPanelEvent> (evt => popupContainer.Focus());
				popupContainer.RegisterCallback<KeyDownEvent> (evt => {if (evt.keyCode == KeyCode.Escape) Close (); } );
            } else {
                Debug.LogWarning (string.Format ("Could not load popup element, looking for an xml file at {0}", xmlFullPath));
            }
        }
        #endregion

		#region Functionality

		public void Open (Vector2 offset) {
			onBeforeOpen?.Invoke (popupContainer);
			float modalWidth = modalElem.style.width.value.value;
			float modalHeight = modalElem.style.height.value.value;
			modalElem.style.left = offset.x - (alignLeft?modalWidth:0);
			modalElem.style.top = offset.y - modalHeight;
			popupContainer.style.display = DisplayStyle.Flex;
			popupContainer.Focus ();
			onOpen?.Invoke (popupContainer);
		}
		public void Close () {
			onBeforeClose?.Invoke (popupContainer);
			popupContainer.style.display = DisplayStyle.None;
			onClose?.Invoke (popupContainer);
		}
		#endregion

        #region Init/Destroy
		private void OnDestroy () {
		}
		private void OnDisable () {
		}
        #endregion
    }
}
