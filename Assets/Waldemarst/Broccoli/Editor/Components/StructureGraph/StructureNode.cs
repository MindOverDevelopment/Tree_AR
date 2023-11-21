using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

using Broccoli.Pipe;
using Broccoli.Utils;

namespace Broccoli.TreeNodeEditor
{
    public class StructureNode : Node
    {
        #region Vars
        public int id = -1;
        public bool isEnable = true;
        public int tag = 0;
        public Color mark = Color.clear;
        public StructurePort upstreamChildrenPort = null;
        public StructurePort upstreamParentPort = null;
        public StructurePort downstreamParentPort = null;
        public StructurePort downstreamChildrenPort = null;
        public bool tagColorEnabled = true;
        public new class UxmlFactory : UxmlFactory<StructureNode, VisualElement.UxmlTraits> {}
        public enum NodeType {
            Trunk = 0,
            Branch = 1,
            Sprout = 2,
            Root = 3
        }
        public NodeType nodeType = NodeType.Trunk;
        public enum NodeOrientation {
            Vertical = 0,
            Horizontal = 1,
        }
        public System.Object obj = null;
        public NodeOrientation nodeOrientation = NodeOrientation.Vertical;
        private static string verticalNodeClassName = "node-vertical";
        private static string horizontalNodeClassName = "node-horizontal";
        private static string enabledClassName = "node-enabled";
        private static string disabledClassName = "node-disabled";
        public static string upChildrenPortName = "up-children-port";
        public static string upParentPortName = "up-parent-port";
        public static string downChildrenPortName = "down-children-port";
        public static string downParentPortName = "down-parent-port";
        #endregion

        #region Tag Colors
        /* https://paletton.com/#uid=75o1o0klZeHeGndhjhBq9axvd6k
        #RED TONES
        Light	141, 68, 77
        Mid	    114, 40, 49
        Dark	84, 19, 27

        #OLIVE TONES
        Light	160, 146, 70
        Mid	    129, 115, 41
        Dark	94, 82, 18

        #BLUE TONES
        Light	55, 80, 104
        Mid	35, 59, 83
        Dark	20, 42, 64

        #GREEN TONES
        Light	90, 125, 58
        Mid	    67, 105, 33
        Dark	43, 75, 14
        */
        public static int totalTags = 12;
        private static Color tagColorDefault = Color.clear;
        private static Color[] tagColors = new Color[] {
            new Color (141f/255f, 68f/255f, 77f/255f),
            new Color (114f/255f, 40f/255f, 49f/255f),
            new Color (84f/255f, 19f/255f, 27f/255f),
            new Color (160f/255f, 146f/255f, 70f/255f),
            new Color (129f/255f, 115f/255f, 41f/255f),
            new Color (94f/255f, 82f/255f, 18f/255f),
            new Color (55f/255f, 80f/255f, 104f/255f),
            new Color (35f/255f, 59f/255f, 83f/255f),
            new Color (20f/255f, 42f/255f, 64f/255f),
            new Color (90f/255f, 125f/255f, 58f/255f),
            new Color (67f/255f, 105f/255f, 33f/255f),
            new Color (43f/255f, 75f/255f, 14f/255f)
        };
        private static string[] tagColorNames = new string[] {
            "Red Light",
            "Red Mid",
            "Red Dark",
            "Yellow Light",
            "Yellow Mid",
            "Yellow Dark",
            "Blue Light",
            "Blue Mid",
            "Blue Dark",
            "Green Light",
            "Green Mid",
            "Green Dark"
        };
        #endregion

        #region Delegates
        public delegate void OnSelectionDelegate (StructureNode node);
        public delegate void OnEnableDelegate (StructureNode node, bool enable);
        public delegate void OnTagDelegate (StructureNode node, int tagId, Color tagColor);
        public OnSelectionDelegate onSelected;
        public OnSelectionDelegate onUnselected;
        public OnEnableDelegate onEnable;
        public OnTagDelegate onSetTag;
        #endregion

        #region Constructors
        public StructureNode () {}
        public StructureNode (StructureNode.NodeType nodeType, NodeOrientation nodeOrientation) {
            StructureNodeConstructor (nodeType, nodeOrientation);
        }
        public StructureNode (StructureNode.NodeType nodeType, NodeOrientation nodeOrientation, VisualTreeAsset nodeXml) : base (AssetDatabase.GetAssetPath(nodeXml)) {
            StructureNodeConstructor (nodeType, nodeOrientation);
        }
        private void StructureNodeConstructor (StructureNode.NodeType nodeType, NodeOrientation nodeOrientation) {
            this.nodeType = nodeType;
            this.nodeOrientation = nodeOrientation;
            Orientation portOrientation;
            Direction direction;
            if (nodeOrientation == NodeOrientation.Vertical) {
                portOrientation = Orientation.Vertical;
            } else {
                portOrientation = Orientation.Horizontal;
            }
            bool addUpstreamChildrenPort = false;
            bool addUpstreamParentPort = false;
            bool addDownstreamChildrenPort = false;
            bool addDownstreamParentPort = false;
            switch (nodeType) {
                case NodeType.Trunk:
                    addUpstreamChildrenPort = true;
                    addDownstreamChildrenPort = true;
                    break;
                case NodeType.Branch:
                    addUpstreamParentPort = true;
                    addUpstreamChildrenPort = true;
                    break;
                case NodeType.Root:
                    addDownstreamChildrenPort = true;
                    addDownstreamParentPort = true;
                    break;
                case NodeType.Sprout:
                    addUpstreamParentPort = true;
                    break;
            }
            if (addUpstreamChildrenPort) {
                direction = (nodeOrientation==NodeOrientation.Vertical?Direction.Input:Direction.Output);
                upstreamChildrenPort = StructurePort.Create<Edge> (portOrientation, direction, Port.Capacity.Multi, typeof(StructureNode));
                upstreamChildrenPort.name = upChildrenPortName;
                if (nodeOrientation == NodeOrientation.Vertical)
                    inputContainer.Add (upstreamChildrenPort);
                else
                    outputContainer.Add (upstreamChildrenPort);
            }
            if (addDownstreamChildrenPort) {
                direction = (nodeOrientation==NodeOrientation.Vertical?Direction.Output:Direction.Input);
                downstreamChildrenPort = StructurePort.Create<Edge> (portOrientation, direction, Port.Capacity.Multi, typeof(StructureNode));
                downstreamChildrenPort.name = downChildrenPortName;
                if (nodeOrientation == NodeOrientation.Vertical)
                    outputContainer.Add (downstreamChildrenPort);
                else
                    inputContainer.Add (downstreamChildrenPort);
            }
            if (addUpstreamParentPort) {
                direction = (nodeOrientation==NodeOrientation.Vertical?Direction.Output:Direction.Input);
                upstreamParentPort = StructurePort.Create<Edge> (portOrientation, direction, Port.Capacity.Single, typeof(StructureNode));
                upstreamParentPort.name = upParentPortName;
                if (nodeOrientation == NodeOrientation.Vertical)
                    outputContainer.Add (upstreamParentPort);
                else
                    inputContainer.Add (upstreamParentPort);
            }
            if (addDownstreamParentPort) {
                direction = (nodeOrientation==NodeOrientation.Vertical?Direction.Input:Direction.Output);
                downstreamParentPort = StructurePort.Create<Edge> (portOrientation, direction, Port.Capacity.Single, typeof(StructureNode));
                downstreamParentPort.name = downParentPortName;
                if (nodeOrientation == NodeOrientation.Vertical)
                    inputContainer.Add (downstreamParentPort);
                else
                    outputContainer.Add (downstreamParentPort);
            }
        }
        #endregion

        #region Node Ops
        public void InitializeNode (StyleSheet nodeStyle)
        {
            //This was a big part of the issue, right here. In custom nodes, this doesn't get called automatically.
            //Short of supplying your own stylesheet that covers all the bases, this needs to be explicitly called to give a node visible attributes.
            UseDefaultStyling();
            if (nodeStyle != null) {
                this.styleSheets.Add (nodeStyle);
            }
    
            VisualElement contentsElement = this.Q<VisualElement>("contents");
            VisualElement titleElement = this.Q<VisualElement>("title");
            VisualElement inputHeader = this.Q<VisualElement>("input");
            Toggle nodeToggle = this.Q<Toggle>("node-toggle");
            VisualElement icon = this.Q<VisualElement>("icon");

            // Set Icon.
            if (icon != null) {
                switch (nodeType) {
                    case StructureNode.NodeType.Trunk:
                        icon.style.backgroundImage = new StyleBackground (GUITextureManager.GetNodeBgTrunk ());
                        break;
                    case StructureNode.NodeType.Branch:
                        icon.style.backgroundImage = new StyleBackground (GUITextureManager.GetNodeBgBranch ());
                        break;
                    case StructureNode.NodeType.Sprout:
                        icon.style.backgroundImage = new StyleBackground (GUITextureManager.GetNodeBgSprout ());
                        break;
                    case StructureNode.NodeType.Root:
                        icon.style.backgroundImage = new StyleBackground (GUITextureManager.GetNodeBgRoot ());
                        break;
                }
            }
            this.tooltip = nodeType.ToString ();
            
            // Enable/Disable
            if (nodeToggle != null) {
                nodeToggle.value = isEnable;
                if (isEnable) {
                    this.AddToClassList (enabledClassName);
                } else {
                    this.AddToClassList (disabledClassName);
                }
                nodeToggle.RegisterCallback<ChangeEvent<bool>>(x => SetEnableNode (x.newValue));
            }

            // Direction.
            if (nodeOrientation == NodeOrientation.Vertical) {
                this.AddToClassList (verticalNodeClassName);
            } else {
                this.AddToClassList (horizontalNodeClassName);
            }

            if (tagColorEnabled)
                SetTag (tag);
        
            MarkDirtyRepaint();
        }

        public override void OnSelected () {
            base.OnSelected ();
            onSelected?.Invoke (this);
        }

        public override void OnUnselected() {
            base.OnUnselected ();
            onUnselected?.Invoke (this);
        }

        public bool SetEnableNode (bool enable) {
            if (isEnable != enable) {
                isEnable = enable;
                RefreshNode (isEnable);
                onEnable?.Invoke (this, enable);
                return true;
            }
            return false;
        }
        public void SetMark (Color markColor) {
            this.mark = markColor;
            VisualElement markElem = this.Q<VisualElement>("mark");
            if (markElem != null) {
                markElem.style.backgroundColor = markColor;
            }
        }
        
        public void SetTag (int tag) {
            this.tag = tag;
            Color tagColor = GetTagColor (tag);
            VisualElement input = this.Q<VisualElement>("input");
            VisualElement output = this.Q<VisualElement>("output");
            if (input != null && nodeType != NodeType.Trunk) {
                input.style.backgroundColor = tagColor;
            }
            if (output != null) {
                output.style.backgroundColor = tagColor;
            }
            onSetTag?.Invoke (this, tag, tagColor);
        }
        public void RefreshNode (bool enable) {
            if (enable) {
                this.AddToClassList (enabledClassName);
                this.RemoveFromClassList (disabledClassName);
            } else {
                this.AddToClassList (disabledClassName);
                this.RemoveFromClassList (enabledClassName);
            }
            Toggle nodeToggle = this.Q<Toggle>("node-toggle");
            if (nodeToggle != null) {
                nodeToggle.value = enable;
            }
        }
        #endregion

        #region Util
        public static Color GetTagColor (int tag) {
            Color tagColor = tagColorDefault;
            if (tag > 0 && tag <= tagColors.Length) {
                tagColor = tagColors [tag -1];
            }
            return tagColor;
        }
        public static string GetTagColorName (int tag) {
            if (tag > 0 && tag <= tagColorNames.Length) {
                return tagColorNames [tag - 1];
            }
            return string.Empty;
        }
        #endregion
    }
    public class StructurePort : Port {
        public StructurePort (Orientation portOrientation, Direction portDirection, Capacity portCapacity, System.Type type) :
            base(portOrientation, portDirection, portCapacity, type) {}
        public static new StructurePort Create<TEdge>(Orientation orientation, Direction direction, Capacity capacity, System.Type type) where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            var port = new StructurePort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;
            private List<Edge> m_EdgesToCreate;
            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();

                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position) {}
            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);

                // We can't just add these edges to delete to the m_GraphViewChange
                // because we want the proper deletion code in GraphView to also
                // be called. Of course, that code (in DeleteElements) also
                // sends a GraphViewChange.
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.input.connections)
                        if (edgeToDelete != edge)
                            m_EdgesToDelete.Add(edgeToDelete);
                if (edge.output.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.output.connections)
                        if (edgeToDelete != edge)
                            m_EdgesToDelete.Add(edgeToDelete);
                if (m_EdgesToDelete.Count > 0)
                    graphView.DeleteElements(m_EdgesToDelete);

                var edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
                }

                foreach (Edge e in edgesToCreate)
                {
                    graphView.AddElement(e);
                    edge.input.Connect(e);
                    edge.output.Connect(e);
                }
            }
        }

        public class EdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
        {
            readonly EdgeDragHelper m_EdgeDragHelper;
            Edge m_EdgeCandidate;
            private bool m_Active;
            Vector2 m_MouseDownPosition;

            internal const float k_ConnectionDistanceTreshold = 10f;

            public EdgeConnector(IEdgeConnectorListener listener)
            {
                m_EdgeDragHelper = new StructureEdgeDragHelper<TEdge>(listener);
                m_Active = false;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            }

            public override EdgeDragHelper edgeDragHelper => m_EdgeDragHelper;

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
                target.RegisterCallback<KeyDownEvent>(OnKeyDown);
                target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }

            protected virtual void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (!CanStartManipulation(e))
                {
                    return;
                }

                var graphElement = target as Port;
                if (graphElement == null)
                {
                    return;
                }

                m_MouseDownPosition = e.localMousePosition;

                m_EdgeCandidate = new TEdge();
                m_EdgeDragHelper.draggedPort = graphElement;
                m_EdgeDragHelper.edgeCandidate = m_EdgeCandidate;

                if (m_EdgeDragHelper.HandleMouseDown(e))
                {
                    m_Active = true;
                    target.CaptureMouse();

                    e.StopPropagation();
                }
                else
                {
                    m_EdgeDragHelper.Reset();
                    m_EdgeCandidate = null;
                }
            }

            void OnCaptureOut(MouseCaptureOutEvent e)
            {
                m_Active = false;
                if (m_EdgeCandidate != null)
                    Abort();
            }

            protected virtual void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active) return;

                m_EdgeDragHelper.HandleMouseMove(e);
                m_EdgeCandidate.candidatePosition = e.mousePosition;
                m_EdgeCandidate.UpdateEdgeControl();
                e.StopPropagation();
            }

            protected virtual void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !CanStopManipulation(e))
                    return;

                if (CanPerformConnection(e.localMousePosition))
                    m_EdgeDragHelper.HandleMouseUp(e);
                else
                    Abort();

                m_Active = false;
                m_EdgeCandidate = null;
                target.ReleaseMouse();
                e.StopPropagation();
            }

            private void OnKeyDown(KeyDownEvent e)
            {
                if (e.keyCode != KeyCode.Escape || !m_Active)
                    return;

                Abort();

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }

            void Abort()
            {
                var graphView = target?.GetFirstAncestorOfType<GraphView>();
                graphView?.RemoveElement(m_EdgeCandidate);

                m_EdgeCandidate.input = null;
                m_EdgeCandidate.output = null;
                m_EdgeCandidate = null;

                m_EdgeDragHelper.Reset();
            }

            bool CanPerformConnection(Vector2 mousePosition)
            {
                return Vector2.Distance(m_MouseDownPosition, mousePosition) > k_ConnectionDistanceTreshold;
            }
        }
    }
}
