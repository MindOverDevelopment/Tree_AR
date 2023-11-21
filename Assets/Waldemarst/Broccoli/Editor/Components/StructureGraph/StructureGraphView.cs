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
    public class StructureGraphView : GraphView {
		#region StructureEdge Class
		public class StructureEdge {
			public StructureNode parentNode = null;
			public StructureNode childNode = null;
		}
		#endregion

        #region Vars
		protected float currentZoom = 1f;
		//protected Vector3 contentOffset = Vector3.zero;
        public StructureNode.NodeOrientation nodeOrientation = StructureNode.NodeOrientation.Vertical;
        protected bool isDirty = true;
        protected bool removingEdgesFromRemoveNode = false;
		protected Vector2 contentOffset = Vector2.zero;
		protected string debugInfo = string.Empty;
        #endregion

        #region Config Vars
        public bool isUniqueTrunk = true;
		public bool addTrunkEnabled = false;
		public bool removeTrunkEnabled = false;
		public bool toggleTrunkEnabled = false;
		public bool taggingEnabled = false;
		/// <summary>
		/// List of allowed node types on this graph.
		/// </summary>
		/// <value>List of allowed nodes.</value>
		private StructureNode.NodeType[] _nodeTypes = new StructureNode.NodeType[]{
			StructureNode.NodeType.Trunk,
			StructureNode.NodeType.Branch,
			StructureNode.NodeType.Sprout,
			StructureNode.NodeType.Root
		};
		/// <summary>
		/// List of allowed node types on this graph.
		/// </summary>
		/// <value>List of allowed nodes.</value>
        public virtual StructureNode.NodeType[] nodeTypes {
            get { return _nodeTypes; }
        }
		/// <summary>
		/// Path to the node xml.
		/// </summary>
		public virtual string nodeXmlPath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/StructureNodeView.uxml"; }
		}
		/// <summary>
		/// Path to the node style.
		/// </summary>
		public virtual string nodeStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/StructureNodeViewStyle.uss"; }
		}
		/// <summary>
		/// Path to the graph style.
		/// </summary>
		public virtual string graphViewStylePath {
			get { return ExtensionManager.extensionPath + "Editor/Resources/GUI/StructureGraphViewStyle.uss"; }
		}
        #endregion

		#region Delegates
		public delegate void OnZoomDelegate (float currentZoom, float previousZoom);
		public delegate void OnOffsetDelegate (Vector2 currentOffset, Vector2 previousOffset);
		public delegate void OnNodeDelegate (StructureNode node);
		public delegate void OnNodePosDelegate (StructureNode node, Vector2 nodePosition);
		public delegate void OnNodeDuplicatePosDelegate (StructureNode node, StructureNode originalNode, Vector2 nodePosition);
        public delegate void OnEnableNodeDelegate (StructureNode node, bool enable);
		public delegate void OnTagNodeDelegate (StructureNode node, int tagId, Color tagColor);
		public delegate void OnMoveNodesDelegate (List<StructureNode> nodes, Vector2 delta);

		public delegate void OnRemoveNodesDelegate (List<StructureNode> nodes);
		public delegate void OnEdgeDelegate (StructureNode parentNode, StructureNode childNode);
		public delegate void OnEdgesDelegate (List<Edge> edges);
		public delegate void OnDelegate ();
		public OnZoomDelegate onZoomDone;
		public OnOffsetDelegate onPanDone;
		public OnNodeDelegate onSelectNode;
		public OnNodeDelegate onDeselectNode;
		public OnMoveNodesDelegate onMoveNodes;
		public OnNodePosDelegate onBeforeAddNode;
		public OnNodePosDelegate onAddNode;
		public OnNodeDuplicatePosDelegate onDuplicateNode;
		public OnRemoveNodesDelegate onBeforeRemoveNodes;
		public OnRemoveNodesDelegate onRemoveNodes;
		public OnEdgeDelegate onBeforeAddConnection;
		public OnEdgeDelegate onAddConnection;
		public OnEdgesDelegate onBeforeRemoveConnections;
		public OnEdgesDelegate onRemoveConnections;
		public OnEnableNodeDelegate onBeforeEnableNode;
		public OnEnableNodeDelegate onEnableNode;
		public OnTagNodeDelegate onSetNodeTag;
		#endregion

		#region Messages
		private static string MSG_REMOVE_CONNECTIONS_TITLE = "Remove Connections";
		private static string MSG_REMOVE_CONNECTIONS_MESSAGE = "Are you sure you want to remove the selected connections?";
		private static string MSG_REMOVE_CONNECTIONS_OK = "Yes, Remove them";
		private static string MSG_REMOVE_CONNECTIONS_CANCEL = "Cancel";
		private static string MSG_REMOVE_NODES_TITLE = "Remove Elements";
		private static string MSG_REMOVE_NODES_MESSAGE = "Are you sure you want to remove the selected elements?";
		private static string MSG_REMOVE_NODES_OK = "Yes, Remove them";
		private static string MSG_REMOVE_NODES_CANCEL = "Cancel";
		#endregion

        #region GUI Vars
		/// <summary>
		/// Id to nodes dictionary.
		/// </summary>
		Dictionary<int, StructureNode> idToNode = new Dictionary<int, StructureNode> ();
        Dictionary<int, StructureNode> idToTrunkNode = new Dictionary<int, StructureNode> ();
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
        #endregion

        #region Init/Destroy
        public void Init (Vector2 offset, float zoom) {
			// Zoom
            this.SetupZoom (0.05f, ContentZoomer.DefaultMaxScale, 0.05f, zoom);
			currentZoom = zoom;
			this.viewTransform.scale = new Vector3 (zoom, zoom, zoom);

			// Offset
			contentOffset = offset;
			viewTransform.position = offset;

			// Manipulators.
            this.AddManipulator(new ContentDragger());
			StructureSelectionDragger selectionDragger = new StructureSelectionDragger ();
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

			nodeXml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(nodeXmlPath);
			nodeStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(nodeStylePath);
			graphViewStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(graphViewStylePath);

			if (graphViewStyle != null) {
				this.styleSheets.Add (graphViewStyle);
			}

			guiContainer = new VisualElement ();
			guiContainer.name = guiContainerName;
			this.Add (guiContainer);
        }
		/// <summary>
		/// Sets the content view of the canvas to center selection on the graph without affecting te pan value.
		/// </summary>
		/// <param name="offset">Content view offset.</param>
		public void SetContentViewOffset (Vector2 offset) {
			this.viewTransform.position = (Vector3)offset;
			/*
			this.contentViewContainer.style.left = offset.x;
			this.contentViewContainer.style.top = offset.y;
			*/
			contentOffset.x = offset.x;
			contentOffset.y = offset.y;
		}
        public void ClearElements () {
            ClearEdges ();
            ClearNodes ();
        }
		/// <summary>
		/// Build the contextual menu to show on the graph canvas.
		/// </summary>
		/// <param name="evt"></param>
		public override void BuildContextualMenu (ContextualMenuPopulateEvent evt) {
			//var position = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition - contentOffset);
			var position = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            // On GraphView area.
			if (evt.target is GraphView) {
				if (addTrunkEnabled) {
					evt.menu.AppendAction("Add Trunk Node", 
						(e) => { AddNode (StructureNode.NodeType.Trunk, position); }, (e) => { return OptionStatusAddTrunkNode (); });
				}
                evt.menu.AppendAction("Add Branch Node", 
					(e) => { AddNode (StructureNode.NodeType.Branch, position); }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Add Root Node", 
					(e) => { AddNode (StructureNode.NodeType.Root, position); }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Add Sprout Node", 
					(e) => { AddNode (StructureNode.NodeType.Sprout, position); }, DropdownMenuAction.AlwaysEnabled);
			} else if (evt.target is Node) {
				StructureNode targetNode = evt.target as StructureNode;
				if (!removeTrunkEnabled && !(targetNode.nodeType == StructureNode.NodeType.Trunk)) {
					evt.menu.AppendAction("Duplicate Node", (e) => { DuplicateNode (targetNode, position); });
					evt.menu.AppendAction("Remove Node", (e) => { RemoveNodes (new List<StructureNode> () {targetNode}); });
				}
				if (taggingEnabled) {
					evt.menu.AppendSeparator ();
					if (targetNode.tag > 0) {
						evt.menu.AppendAction("Remove Tag", (e) => { SetNodeTag (0, targetNode); });
					}
					for (int i = 1; i <= StructureNode.totalTags; i++) {
						var tagId = i;
						evt.menu.AppendAction("Set Tag/Tag " + i, (e) => { SetNodeTag (tagId, targetNode); });
					}
				}
			}
        }
        DropdownMenuAction.Status OptionStatusAddTrunkNode () {
            if (isUniqueTrunk && idToTrunkNode.Count > 0) {
                return DropdownMenuAction.Status.Disabled;
            }
            return DropdownMenuAction.Status.Normal;
        }
		public override List<Port> GetCompatiblePorts (Port startPort, NodeAdapter nodeAdapter) {
			List<Port> _ports = ports.ToList ();
			return ports.ToList().Where(endPort =>
						endPort.direction != startPort.direction &&
						endPort.node != startPort.node &&
						endPort.portType == startPort.portType)
			.ToList();
		}
        #endregion

        #region Node Ops
		/// <summary>
		/// Gets a node by id if it exists.
		/// </summary>
		/// <param name="id">Id of the node.</param>
		/// <returns>Node instance or null if not found.</returns>
		public StructureNode GetNode (int id) {
			if (idToNode.ContainsKey (id)) {
				return idToNode [id];
			}
			return null;
		}
        /// <summary>
        /// Clear all the nodes on this graph.
        /// </summary>
        public void ClearNodes () {
            var nodesEnumerator = idToNode.GetEnumerator ();
            while (nodesEnumerator.MoveNext ()) {
                this.RemoveElement (nodesEnumerator.Current.Value);
            }
            idToNode.Clear ();
            idToTrunkNode.Clear ();
        }
        /// <summary>
        /// Clear all the edges on this graph.
        /// </summary>
        public void ClearEdges () {
			List<Edge> _edges = edges.ToList ();
			for (int i = 0; i < _edges.Count; i++) {
				RemoveElement (_edges [i]);
			}
			_edges.Clear ();
		}
        /// <summary>
        /// Creates a structure node.
        /// </summary>
        /// <returns></returns>
        public StructureNode CreateNode (int nodeId, StructureNode.NodeType nodeType, bool isEnable) {
            StructureNode structureNode;
            if (nodeXml == null) {
                structureNode = new StructureNode (nodeType, nodeOrientation) { id = nodeId, name = "structure-node", 
                    isEnable = isEnable, nodeOrientation = nodeOrientation };
            } else {
                structureNode = new StructureNode (nodeType, nodeOrientation, nodeXml) { id = nodeId, name = "structure-node", 
                    isEnable = isEnable, nodeOrientation = nodeOrientation };
            }
            structureNode.nodeType = nodeType;
			if (!toggleTrunkEnabled && structureNode.nodeType == StructureNode.NodeType.Trunk) {
				Toggle nodeToggle = structureNode.Q<Toggle>("node-toggle");
				if (nodeToggle != null) {
					nodeToggle.style.display = DisplayStyle.None;
				}
			}
            structureNode.InitializeNode (nodeStyle);
            return structureNode;
        }
        private bool CanAddNode (StructureNode.NodeType nodeType) {
			if (nodeTypes.Contains(nodeType)) {
				if (nodeType == StructureNode.NodeType.Trunk && isUniqueTrunk && idToTrunkNode.Count > 0) {
					return false;
				}
			}
            return true;
        }
		private bool CanRemoveNodes (List<StructureNode> nodesToRemove) {
			bool canRemove = false;
			if (nodesToRemove != null && nodesToRemove.Count > 0) {
				canRemove = true;
				if (!removeTrunkEnabled) {
					for (int i = 0; i < nodesToRemove.Count; i++) {
						if (nodesToRemove [i].nodeType == StructureNode.NodeType.Trunk) {
							canRemove = false;
							break;
						}
					}
				}
			}
			return canRemove;
		}
        private int GetNodeId (StructureNode.NodeType nodeType) {
            int nodeId = 0;
            while (idToNode.ContainsKey (nodeId)) {
                nodeId += 1;
            }
            return nodeId;
        }
        private bool RegisterNode (StructureNode node) {
            if (!idToNode.ContainsKey (node.id)) {
                idToNode.Add (node.id, node);
                if (node.nodeType == StructureNode.NodeType.Trunk) {
                    idToTrunkNode.Add (node.id, node);
                }
                return true;
            }
            return false;
        }
        private bool DeregisterNode (StructureNode node) {
            if (idToNode.ContainsKey (node.id)) {
                idToNode.Remove (node.id);
                if (node.nodeType == StructureNode.NodeType.Trunk && idToTrunkNode.ContainsKey (node.id)) {
                    idToTrunkNode.Remove (node.id);
                }
            }
            return false;
        }
		/// <summary>
		/// Replaces the id of a node, updating the graph indexed data.
		/// </summary>
		/// <param name="oldNodeId">Old node id to replace.</param>
		/// <param name="newNodeId">New node id.</param>
		/// <returns><c>True if the replacement was successfull.</c></returns>
		public bool ReplaceNodeId (int oldNodeId, int newNodeId) {
			if (idToNode.ContainsKey (oldNodeId) && !idToNode.ContainsKey (newNodeId)) {
				StructureNode node = idToNode [oldNodeId];
				node.id = newNodeId;
				idToNode.Remove (oldNodeId);
				idToNode.Add (newNodeId, node);
				if (idToTrunkNode.ContainsKey (oldNodeId)) {
					idToTrunkNode.Remove (oldNodeId);
					idToTrunkNode.Add (newNodeId, node);
				}
				return true;
			}
			return false;
		}
        private void RemoveNodeEdges (StructureNode node) {
            if (node != null) {
                if (node.upstreamChildrenPort != null) {
                    DeleteElements (node.upstreamChildrenPort.connections);
                    node.upstreamChildrenPort?.DisconnectAll ();
                }
                if (node.upstreamParentPort != null) {
                    DeleteElements (node.upstreamParentPort.connections);
                    node.upstreamParentPort?.DisconnectAll ();
                }
                if (node.downstreamChildrenPort != null) {
                    DeleteElements (node.downstreamChildrenPort.connections);
                    node.downstreamChildrenPort?.DisconnectAll ();
                }
                if (node.downstreamParentPort != null) {
                    DeleteElements (node.downstreamParentPort.connections);
                    node.downstreamParentPort?.DisconnectAll ();
                }
                MarkDirtyRepaint ();
            }
        }
		private bool CanConnect (StructureNode.NodeType parentNodeType, StructureNode.NodeType childNodeType) {
			bool canConnect = false;
			switch (parentNodeType) {
				case StructureNode.NodeType.Trunk:
					if (childNodeType != StructureNode.NodeType.Trunk) canConnect = true;
					break;
				case StructureNode.NodeType.Branch:
					if (childNodeType == StructureNode.NodeType.Branch || 
						childNodeType == StructureNode.NodeType.Sprout) canConnect = true;
					break;
				case StructureNode.NodeType.Root:
					if (childNodeType == StructureNode.NodeType.Root) canConnect = true;
					break;
				case StructureNode.NodeType.Sprout:
					break;
			}
			return canConnect;
		}
		private Edge CreateEdge (StructureNode parentNode, StructureNode childNode) {
			Edge edge = null;
			// TRUNK.
			if (parentNode.nodeType == StructureNode.NodeType.Trunk) {
				if (childNode.nodeType == StructureNode.NodeType.Branch ||
					childNode.nodeType == StructureNode.NodeType.Sprout)
				{
					edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					/*
					if (nodeOrientation == StructureNode.NodeOrientation.Vertical)
						edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					else
						edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					*/
				} else if (childNode.nodeType == StructureNode.NodeType.Root) {
					edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					/*
					if (nodeOrientation == StructureNode.NodeOrientation.Vertical)
						edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					else
						edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					*/
				}
			}
			// UPSTREAM.
			else if (parentNode.nodeType == StructureNode.NodeType.Branch ||
				parentNode.nodeType == StructureNode.NodeType.Sprout)
			{
				if (childNode.nodeType == StructureNode.NodeType.Branch ||
					childNode.nodeType == StructureNode.NodeType.Sprout)
				{
					edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					/*
					if (nodeOrientation == StructureNode.NodeOrientation.Vertical)
						edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					else
						edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					*/
				}
			}
			// DOWNSTREAM.
			else if (parentNode.nodeType == StructureNode.NodeType.Root) {
				if (childNode.nodeType == StructureNode.NodeType.Root) {
					edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					/*
					if (nodeOrientation == StructureNode.NodeOrientation.Vertical)
						edge = parentNode.downstreamChildrenPort.ConnectTo (childNode.downstreamParentPort);
					else
						edge = parentNode.upstreamChildrenPort.ConnectTo (childNode.upstreamParentPort);
					*/
				}
			}
			if (edge != null) {
				AddElement (edge);
			}
			return edge;
		}
        public StructureNode AddNode (StructureNode.NodeType nodeType, Vector2 nodePosition, bool isEnable = true) {
            int nodeId = GetNodeId (nodeType);
            return AddNode (nodeType, nodeId, nodePosition, isEnable);
        }
        public StructureNode AddNode (StructureNode.NodeType nodeType, int nodeId, Vector2 nodePosition, bool isEnable = true, bool isDuplicate = false) {
			StructureNode addedNode = null;
            if (CanAddNode (nodeType)) {
                if (nodeId >= 0) {
                    StructureNode structureNode = CreateNode (nodeId, nodeType, isEnable);
                    if (structureNode != null && !idToNode.ContainsKey (nodeId)) {
                        onBeforeAddNode?.Invoke (structureNode, nodePosition);
                        this.AddElement (structureNode);
                        structureNode.onSelected -= OnSelectNodeInternal;
                        structureNode.onSelected += OnSelectNodeInternal;
                        structureNode.onUnselected -= OnDeselectNodeInternal;
                        structureNode.onUnselected += OnDeselectNodeInternal;
                        structureNode.onEnable -= OnEnableNodeInternal;
                        structureNode.onEnable += OnEnableNodeInternal;
						structureNode.onSetTag -= OnSetNodeTagInternal;
                        structureNode.onSetTag += OnSetNodeTagInternal;
                        structureNode.SetPosition (new Rect(
                            nodePosition.x, 
                            nodePosition.y, 
                            0, 0));
                        RegisterNode (structureNode);
						if (!isDuplicate)
                        	onAddNode?.Invoke (structureNode, nodePosition);
                        isDirty = true;
						addedNode = structureNode;
                    }
                }
            }
            return addedNode;
		}
		public bool DuplicateNode (StructureNode targetStructureNode, Vector2 nodePosition) {
			if (targetStructureNode != null) {
				int nodeId = GetNodeId (targetStructureNode.nodeType);
				StructureNode duplicatedNode = AddNode (targetStructureNode.nodeType, nodeId, nodePosition, true, true);
				if (duplicatedNode != null) {
					onDuplicateNode?.Invoke (duplicatedNode, targetStructureNode, nodePosition);
				}
			}
			return false;
		}
        public bool RemoveNodes (List<StructureNode> nodesToRemove, bool overrideConfirm = false) {
			if (CanRemoveNodes (nodesToRemove)) {
				if (overrideConfirm ||
					EditorUtility.DisplayDialog (MSG_REMOVE_NODES_TITLE, 
					MSG_REMOVE_NODES_MESSAGE, 
					MSG_REMOVE_NODES_OK, 
					MSG_REMOVE_NODES_CANCEL)) 
				{
					onBeforeRemoveNodes?.Invoke (nodesToRemove);
                    removingEdgesFromRemoveNode = true;
					for (int i = 0; i < nodesToRemove.Count; i++) {
                        DeregisterNode (nodesToRemove [i]);
                        RemoveNodeEdges (nodesToRemove [i]);
						RemoveElement (nodesToRemove [i]);
					}
                    removingEdgesFromRemoveNode = false;
					onRemoveNodes?.Invoke (nodesToRemove);
					isDirty = true;
					return true;
				}
			}
			return false;
		}
		public bool AddConnection (int parentNodeId, int childNodeId) {
			StructureNode parentNode = null;
			StructureNode childNode = null;
			idToNode.TryGetValue (parentNodeId, out parentNode);
			idToNode.TryGetValue (childNodeId, out childNode);
			if (parentNode != null && childNode != null) {
				bool connectionAdded = AddConnectionInternal (parentNode, childNode);
				if (connectionAdded) {
					Edge edge = CreateEdge (parentNode, childNode);
					SetEdgeUserData (edge, parentNode, childNode);
				}
				return connectionAdded;
			}
			return false;
		}
		private bool AddConnectionInternal (StructureNode parentNode, StructureNode childNode) {
			if (parentNode != null && childNode != null) {
                bool canConnect = CanConnect (parentNode.nodeType, childNode.nodeType);
                if (canConnect) {
					onBeforeAddConnection?.Invoke (parentNode, childNode);
					onAddConnection?.Invoke (parentNode, childNode);
					isDirty = true;
                    return true;
				}
			}
            return false;
		}
		public bool RemoveConnections (List<Edge> edgesToRemove, bool overrideConfirm = false) {
			if (edgesToRemove.Count > 0) {
				if (overrideConfirm ||
					EditorUtility.DisplayDialog (MSG_REMOVE_CONNECTIONS_TITLE, 
					MSG_REMOVE_CONNECTIONS_MESSAGE, 
					MSG_REMOVE_CONNECTIONS_OK, 
					MSG_REMOVE_CONNECTIONS_CANCEL))
				{
					onBeforeRemoveConnections?.Invoke (edgesToRemove);
					onRemoveConnections?.Invoke (edgesToRemove);
					isDirty = true;
					return true;
				}
			}
			return false;
		}
		public bool SetNodeEnabled (int nodeId, bool enabled) {
			if (idToNode.ContainsKey (nodeId)) {
				return idToNode [nodeId].SetEnableNode (enabled);
			}
			return false;
		}
		public void SetNodeMark (int nodeId, Color markColor) {
			if (idToNode.ContainsKey (nodeId)) {
				idToNode [nodeId].SetMark (markColor);
			}
		}
		public void SetNodeTag (int nodeTag, StructureNode node) {
			node.SetTag (nodeTag);
		}
		/// <summary>
		/// Gets the color of a node tag.
		/// </summary>
		/// <param name="tag">Tag identifier.</param>
		/// <returns>Color assigned to the tag.</returns>
		public StyleColor GetTagColor (int tag) {
            return StructureNode.GetTagColor (tag);
        }
        #endregion

        #region Graph Events
        private GraphViewChange _GraphViewChanged(GraphViewChange graphViewChange) {
			// Elements MOVED.
			if (graphViewChange.movedElements != null) {
				List<StructureNode> movedNodes = new List<StructureNode> ();
				for (int i = 0; i < graphViewChange.movedElements.Count; i++) {
					movedNodes.Add (graphViewChange.movedElements [i] as StructureNode);
				}
				if (movedNodes.Count > 0) {
					onMoveNodes?.Invoke (movedNodes, graphViewChange.moveDelta);
				}
			}

			if (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Count > 0) {
			// Elements REMOVED (Nodes or edges).
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
			}

			// Elements CONNECTED.
			if (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Count > 0) {
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
			}
            
			return graphViewChange;
		}
        private void _ViewTransformChanged (GraphView graphView) {
			// If zoom done.
			if (this.scale != currentZoom) {
				onZoomDone?.Invoke (this.scale, currentZoom);
				currentZoom = this.scale;
			}
			// If pan done.
			if ((Vector2)this.viewTransform.position != contentOffset) {
				onPanDone?.Invoke (this.viewTransform.position, contentOffset);
				//contentOffset = this.viewTransform.position;
				SetContentViewOffset (this.viewTransform.position);
			}
        }
		private void OnSelectNodeInternal (StructureNode pipelineNode) {
			onSelectNode?.Invoke (pipelineNode);
		}
		private void OnDeselectNodeInternal (StructureNode pipelineNode) {
			onDeselectNode?.Invoke (pipelineNode);
		}
		private void OnEnableNodeInternal (StructureNode node, bool enable) {
			onBeforeEnableNode?.Invoke (node, enable);
			node.isEnable = enable;
			onEnableNode?.Invoke (node, enable);
		}
		private void OnSetNodeTagInternal (StructureNode node, int tagId, Color tagColor) {
			onSetNodeTag?.Invoke (node, tagId, tagColor);
		}
		private void SetEdgeUserData (Edge edge, StructureNode parentNode, StructureNode childNode) {
			StructureEdge structureEdge = new StructureEdge () { parentNode = parentNode, childNode = childNode};
			edge.userData = structureEdge;
		}
		private void KeyDown(KeyDownEvent evt)
		{
		
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
			debugInfo += string.Format ("Nodes: {0}\n", idToNode.Count);
			var nodeIt = idToNode.GetEnumerator ();
			while (nodeIt.MoveNext ()) {
				int nodeId = nodeIt.Current.Key;
				StructureNode node = nodeIt.Current.Value;
				debugInfo += string.Format ("  Node[{0}] pos({1}, {2}), type: {3}\n", nodeId, node.GetPosition().x, node.GetPosition().y, node.nodeType);
			}
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
