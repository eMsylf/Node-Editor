/// Based on the tutorial by Oguzkonya at https://oguzkonya.com/creating-node-based-editor-unity/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System;

namespace NodeEditor
{
    public enum Orientation { LeftRight, TopBottom }

    public class NodeBasedEditor : EditorWindow
    {
        public Orientation orientation = Orientation.TopBottom;

        private float toolbarHeight = 20f;
        private bool toolbarEnabled = true;
        private Rect toolbarToggle = new Rect(0, 0, 20, 20);
        private Rect toolbarToggleActive = new Rect(0, 20, 20, 20);
        private bool showSettings;
        private float toolbarSettingsHeight = EditorGUIUtility.singleLineHeight;
        private float toolbarSettingsWidth = 200;
        private GUIStyle toolbarSettingsStyle = new GUIStyle();

        private NodeStructure temp;
        public NodeStructure reference;
        private Connection connectionPreview = new Connection();
        string fileName = "New Node Structure";

        private GUIStyle nodeStyle;
        private GUIStyle selectedNodeStyle;
        private GUIStyle inPointStyle;
        private GUIStyle outPointStyle;
        
        private Node selectedNodeIn;
        private Node selectedNodeOut;
        private bool allowLoops;
        
        private Vector2 offset;
        private Vector2 drag;

        private float zoom = 1f;
        private float zoomMin = .5f;
        private float zoomMax = 2f;
        
        private bool drawingSelectionRect = false;
        private Rect selectionRect;
        private GUIStyle selectionRectStyle;
        
        private Color gridSmallColor = new Color(.5f, .5f, .5f, .2f);
        private Color gridLargeColor = new Color(.5f, .5f, .5f, .4f);
        
        public List<Node> SelectedNodes { get => temp.nodes.FindAll(x => x.isSelected); }

        private bool multiSelecting;
        private bool isDragging;

        [MenuItem("Tools/Bob Jeltes/Node Based Editor")]
        [MenuItem("Window/Bob Jeltes/Node Based Editor")]
        public static NodeBasedEditor OpenWindow()
        {
            NodeBasedEditor openedWindow = GetWindow<NodeBasedEditor>("Node Based Editor");
            openedWindow.saveChangesMessage = "This node structure has not been saved. Would you like to save?";
            return openedWindow;
        }

        internal virtual void OnEnable()
        {
            toolbarSettingsStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/dockarea back.png") as Texture2D;

            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node6.png") as Texture2D;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);

            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node6 on.png") as Texture2D;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

            string inSide;
            string outSide;
            RectOffset border;
            switch (orientation)
            {
                default:
                case Orientation.LeftRight:
                    inSide = "left";
                    outSide = "right";
                    border = new RectOffset(4, 4, 12, 12);
                    break;
                case Orientation.TopBottom:
                    inSide = "mid";
                    outSide = "mid";
                    border = new RectOffset(4, 4, 12, 12);
                    break;
            }
            
            inPointStyle = new GUIStyle();
            inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn " + inSide + ".png") as Texture2D;
            inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn " + inSide + " on.png") as Texture2D;
            inPointStyle.border = border;

            outPointStyle = new GUIStyle();
            outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn " + outSide + ".png") as Texture2D;
            outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn " + outSide + " on.png") as Texture2D;
            outPointStyle.border = border;

            selectionRectStyle = new GUIStyle();
            selectionRectStyle.normal.background = new Texture2D(1, 1);
            selectionRectStyle.normal.background.SetPixel(1, 1, new Color(.5f, .5f, .5f, .3f));
            //selectionRectStyle.border = new RectOffset();
            InitNodes();
            ClearConnectionSelection();
        }

        internal virtual void OnGUI()
        {
            DrawGrid(20 * zoom, gridSmallColor);
            DrawGrid(100 * zoom, gridLargeColor);

            DrawNodes(zoom);
            DrawConnections();

            DrawConnectionPreview(Event.current);
            
            DrawToolbar();

            ProcessNodeEvents(Event.current);
            ProcessEvents(Event.current);

            if (GUI.changed) Repaint();
        }

        #region File Management
        internal virtual bool UnsavedChangesCheck()
        {
            if (hasUnsavedChanges)
            {
                int result = EditorUtility.DisplayDialogComplex("Unsaved Changes Detected", saveChangesMessage, "Yes", "Cancel", "Discard");
                switch (result)
                {
                    case 0:
                        // Yes
                        SaveChanges();
                        break;
                    case 1:
                        // Cancel
                        return false;
                    case 2:
                        // Discard
                        break;
                    default:
                        break;
                }
            }
            return true;
        }

        [Shortcut("Node Based Editor/New node structure", KeyCode.N, ShortcutModifiers.Alt)]
        public static void NewFile_Shortcut()
        {
            if (focusedWindow.GetType() == typeof(NodeBasedEditor))
                (focusedWindow as NodeBasedEditor).NewFile();
        }

        internal virtual void NewFile()
        {
            if (!UnsavedChangesCheck()) return;
            fileName = "New Node Structure";
            reference = null;
            temp.nodes = new List<Node>();
            temp.connections = new List<Connection>();
            hasUnsavedChanges = false;
        }

        [Shortcut("Node Based Editor/Save node structure", defaultKeyCode: KeyCode.S, defaultShortcutModifiers: ShortcutModifiers.Alt)]
        public static void SaveChanges_Shortcut()
        {
            // Is dit netjes? Mag dit? Kan dit fout gaan?
            // Kan zijn dat als de editor derivet en niet de directe type is, dat de if-statement false returnt.
            Debug.Log("");
            if (focusedWindow.GetType() == typeof(NodeBasedEditor))
                (focusedWindow as NodeBasedEditor).SaveChanges();
        }

        public override void SaveChanges()
        {
            if (!hasUnsavedChanges) return;
            if (reference == null)
            {
                reference = CreateInstance<NodeStructure>();
            }

            reference = temp.Copy();
            EditorUtility.SetDirty(reference);

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Nodes"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Nodes");
            }
            if (string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(reference)))
            {
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Resources/Nodes/{fileName}.asset");
                AssetDatabase.CreateAsset(reference, assetPath);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = reference;
            EditorGUIUtility.PingObject(Selection.activeObject);
            // TODO: testen of dit nodig is & testen of dit een grote performance impact heeft met veel assets
            //AssetDatabase.Refresh();

            Debug.Log($"{this} saved successfully to file {reference.name}", reference);
            base.SaveChanges();
        }

        public bool Load(NodeStructure structure)
        {
            if (!UnsavedChangesCheck()) return false;
            temp.nodes?.Clear();
            temp.connections?.Clear();
            if (structure == null) return true;
            Debug.Log("Found " + structure.nodes.Count + " nodes");
            Debug.Log("Found " + structure.connections.Count + " connections");

            temp = structure.Copy();
            reference = structure;
            InitNodes();
            ClearConnectionSelection();
            return true;
        }

        internal virtual void InitNodes()
        {
            if (temp == null) temp = CreateInstance<NodeStructure>();
            foreach (var node in temp.nodes)
            {
                node.regularStyle = nodeStyle;
                node.selectedStyle = selectedNodeStyle;
                node.OnDragNode = OnDragNode;
                node.OnRemoveNode = OnClickRemoveNode;
                node.OnClickNode = OnClickNode;
                node.OnClickUp = OnClickUpNode;
                node.inPoint.OnClickConnectionPoint = OnClickConnectionPoint;
                node.outPoint.OnClickConnectionPoint = OnClickConnectionPoint;

                node.isDragged = false;
                node.isSelected = false;
                node.multiSelecting = false;
            }
            if (temp.connections == null) temp.connections = new List<Connection>();
            foreach (var connection in temp.connections)
            {
                // TODO: Upon saving and loading, pass an index to the connection that belongs to the connected node.
                // When initializing the nodes, get the indices stored in the Connection to reconnect them to the proper node in the new list
                connection.OnClickRemoveConnection = OnClickRemoveConnection;
            }
            hasUnsavedChanges = false;
        }
        #endregion

        #region Draw functions
        internal virtual void DrawGrid(float spacing, Color color)
        {
            int widthDivs = Mathf.CeilToInt(position.width / spacing);
            int heightDivs = Mathf.CeilToInt(position.height / spacing);

            Handles.BeginGUI();
            Handles.color = color;

            offset += drag * .5f;
            Vector3 newOffset = new Vector3(offset.x % spacing, offset.y % spacing, 0);
            for (int i = 0; i < widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(
                        spacing * i, 
                        -spacing, 
                        0) + newOffset, 
                    new Vector3(
                        spacing * i, 
                        position.height, 
                        0f) + newOffset);
            }

            for (int j = 0; j < heightDivs; j++)
            {
                Handles.DrawLine(
                    new Vector3(
                        -spacing, 
                        spacing * j, 
                        0) + newOffset, 
                    new Vector3(
                        position.width,
                        spacing * j,
                        0) + newOffset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        public void DrawToolbar()
        {
            if (toolbarEnabled)
            {
                if (GUI.Button(toolbarToggleActive, "^", EditorStyles.miniButton)) toolbarEnabled = false;
            }
            else
            {
                if (GUI.Button(toolbarToggle, "v", EditorStyles.miniButton)) toolbarEnabled = true;
                return;
            }

            Rect menuBarRect = new Rect(0, 0, position.width, toolbarHeight);
            GUILayout.BeginArea(menuBarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (reference == null) 
                fileName = EditorGUILayout.TextField(fileName, GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
            NodeStructure oldReference = reference;
            EditorGUI.BeginChangeCheck();
            NodeStructure newReference = (NodeStructure)EditorGUILayout.ObjectField(reference, typeof(NodeStructure), false, GUILayout.MinWidth(50), GUILayout.MaxWidth(150));
            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log("New file selected");
                if (Load(newReference))
                {
                    reference = newReference;
                }
                else
                {
                    reference = oldReference;
                }
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                Debug.Log("Save");
                SaveChanges();
            }
            if (reference == null && !hasUnsavedChanges)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                NewFile();
            }
            GUI.enabled = true;
            //GUILayout.FlexibleSpace();

            //if (GUILayout.Button("Settings", EditorStyles.toolbarDropDown, GUILayout.Width(70)))
            //{
            //    showSettings = !showSettings;
            //    Debug.Log("Show settings: " + showSettings);
            //}
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            // TODO: Re-enable settings dropdown
            //if (showSettings)
            //{
                //Rect toolbarSettingsRect = new Rect(position.width - toolbarSettingsWidth - 5, EditorGUIUtility.singleLineHeight + 5, toolbarSettingsWidth, toolbarSettingsHeight * 1 + 5);
                //GUILayout.BeginArea(toolbarSettingsRect, toolbarSettingsStyle);
                //GUILayout.BeginHorizontal();
                //GUILayout.Label("Zoom");
                //zoom = GUILayout.HorizontalSlider(zoom, zoomMin, zoomMax);
                //GUILayout.EndHorizontal();

                //GUILayout.EndArea();
            //}

            // Unsaved changes message
        }

        internal virtual void DrawNodes(float zoom)
        {
            if (temp.nodes == null)
                return;

            for (int i = 0; i < temp.nodes.Count; i++)
            {
                temp.nodes[i].Draw(orientation);
            }
        }

        internal virtual void DrawConnections()
        {
            if (temp.connections == null) return;
            for (int i = 0; i < temp.connections.Count; i++)
            {
                temp.connections[i].Draw(orientation);
            }
        }

        internal virtual void DrawConnectionPreview(Event e)
        {
            if (selectedNodeIn != null && selectedNodeOut == null)
            {
                connectionPreview.Draw(selectedNodeIn.inPoint.rect.center, e.mousePosition, orientation, false);
                GUI.changed = true;
            }

            if (selectedNodeOut != null && selectedNodeIn == null)
            {
                connectionPreview.Draw(e.mousePosition, selectedNodeOut.outPoint.rect.center, orientation, false);
                GUI.changed = true;
            }
        }
        #endregion

        #region Editor events
        internal virtual void ProcessEvents(Event e)
        {
            drag = Vector2.zero;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (selectedNodeIn == null && selectedNodeOut != null)
                        {
                            Node newNode = OnClickAddNode(e.mousePosition, true);
                            CreateConnection(newNode, selectedNodeOut);
                        }
                        if (selectedNodeIn != null && selectedNodeOut == null)
                        {
                            Node newNode = OnClickAddNode(e.mousePosition, true);
                            CreateConnection(selectedNodeIn, newNode);
                        }
                        OnClickBackground();
                    }

                    if (e.button == 1)
                    {
                        if (selectedNodeIn != null || selectedNodeOut != null)
                        {
                            ClearConnectionSelection();
                        }
                        else
                        {
                            ProcessContextMenu(e.mousePosition);
                        }
                    }

                    break;

                case EventType.MouseDrag:
                    // TODO: Selection rect
                    //if (Tools.current == Tool.Rect)
                    //{
                    //    if (!drawingSelectionRect)
                    //    {
                    //        selectionRect = new Rect(e.mousePosition, new Vector2());
                            
                    //        Debug.Log("Start new rect");
                    //        drawingSelectionRect = true;
                    //    }
                    //    else
                    //    {
                    //        selectionRect.size += e.delta;
                    //        // Draw selection rect
                    //        Debug.Log("Draw selection rect");
                    //        GUI.Box(selectionRect, "Aaaaaa", selectionRectStyle);
                    //    }
                    //    GUI.changed = true;
                    //}
                    if (e.button == 2 || Tools.viewToolActive)
                    {
                        OnDragView(e.delta);
                    }
                    break;
                case EventType.MouseUp:
                    drawingSelectionRect = false;
                    break;
                // TODO: make zoom affect nodes
                //case EventType.ScrollWheel:
                //    zoom = Mathf.Clamp(zoom - e.delta.y * .1f, zoomMin, zoomMax);
                //    //Debug.Log("Adjust zoom: " + zoom);
                //    GUI.changed = true;
                //    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.S)
                        Tools.current = Tool.Rect;
                    if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                    {
                        Debug.Log("Delete");
                        for (int i = 0; i < temp.nodes.Count; i++)
                        {
                            Node node = temp.nodes[i];
                            if (node.isSelected)
                            {
                                OnClickRemoveNode(node);
                                i--;
                                GUI.changed = true;
                            }
                        }
                    }
                    if (e.keyCode == KeyCode.Space)
                    {
                        ProcessContextMenu(e.mousePosition);
                    }
                    if (e.keyCode == KeyCode.LeftControl)
                    {
                        multiSelecting = true;
                    }
                    break;
                case EventType.KeyUp:
                    if (e.keyCode == KeyCode.LeftControl)
                    {
                        multiSelecting = false;
                    }
                    break;
            }
        }

        internal virtual void OnDragView(Vector2 delta)
        {
            drag = delta;
            if (temp.nodes != null)
            {
                for (int i = 0; i < temp.nodes.Count; i++)
                {
                    temp.nodes[i].Drag(delta, false);
                }
            }

            GUI.changed = true;
        }
        #endregion

        #region Node Events
        internal virtual void ProcessNodeEvents(Event e)
        {
            if (temp.nodes == null) return;

            for (int i = temp.nodes.Count - 1; i >= 0; i--)
            {
                bool guiChanged = temp.nodes[i].ProcessEvents(e);

                if (guiChanged) GUI.changed = true;
            }
        }

        internal virtual void ProcessContextMenu(Vector2 mousePosition)
        {
            Debug.Log("Show context menu");
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition, true));
            //genericMenu.AddItem(new GUIContent("Action"), true, () => Debug.Log("Actions"));
            //genericMenu.AddItem(new GUIContent("Action/Wait"), false, () => Debug.Log("Ayy"));
            genericMenu.ShowAsContext();
        }

        // TODO: Implement
        //[Shortcut("Node Based Editor/New Node", KeyCode.Space)]
        //public static void AddNode()
        //{
        //    if (focusedWindow.GetType() == typeof(NodeBasedEditor))
        //    {
        //        if (mouseOverWindow.GetType() == typeof(NodeBasedEditor))
        //        {
        //            NodeBasedEditor editor = (focusedWindow as NodeBasedEditor);
        //            editor.ProcessContextMenu(new Vector2(editor.position.width * .5f, editor.position.height * .5f));
        //        }
        //    }
        //}

        internal virtual Node OnClickAddNode(Vector2 mousePosition, bool centered)
        {
            Debug.Log("Add node at " + mousePosition);
            if (temp.nodes == null) temp.nodes = new List<Node>();
            Node node = new Node(mousePosition, new Vector2(200, 50), orientation, nodeStyle, selectedNodeStyle, inPointStyle, outPointStyle, OnClickNode, OnClickConnectionPoint, OnClickRemoveNode: OnClickRemoveNode, onDragNode: OnDragNode, OnClickUpNode);
            if (centered)
            {
                node.rect.x -= node.rect.width * .5f;
                node.rect.y -= node.rect.height * .5f;
            }
            temp.nodes.Add(node);
            hasUnsavedChanges = true;
            return node;
        }

        internal virtual void OnClickRemoveNode(Node node)
        {
            if (temp.connections != null)
            {
                List<Connection> connectionsToRemove = new List<Connection>();

                for (int i = 0; i < temp.connections.Count; i++)
                {
                    if (temp.connections[i].inNode == node || temp.connections[i].outNode == node)
                    {
                        connectionsToRemove.Add(temp.connections[i]);
                    }
                }

                for (int i = 0; i < connectionsToRemove.Count; i++)
                {
                    temp.connections.Remove(connectionsToRemove[i]);
                }
                // TODO: Is this necessary? Doesn't the list get disposed of once the if-statement is exited?
                connectionsToRemove = null;
            }

            temp.nodes.Remove(node);
            hasUnsavedChanges = true;
        }

        internal virtual void OnDragNode(Node node)
        {
            Debug.Log("Drag " + Event.current.delta);
            StartDrag();
            if (temp.nodes.Count > 1)
                SelectedNodes.ForEach(x => {
                    if (x != node)
                        x.Drag(Event.current.delta, false);
                    });
            hasUnsavedChanges = true;
        }

        internal virtual void StartDrag()
        {
            isDragging = true;
        }

        internal virtual void EndDrag()
        {
            isDragging = false;
        }

        internal virtual void OnClickNode(Node node)
        {
            if (multiSelecting)
            {
                if (!SelectedNodes.Contains(node)) SelectNode(node);
                else DeselectNode(node);
                return;
            }
            if (node.isSelected)
            {
                return;
            }
            DeselectAllNodes();
            SelectNode(node);
        }

        internal virtual void OnClickUpNode(Node node)
        {
            Debug.Log("Click up on node " + node.rect.position);
            if (!multiSelecting && !isDragging && SelectedNodes.Count > 1)
            {
                DeselectAllNodes();
                SelectNode(node);
            }

            EndDrag();
        }

        internal virtual void SelectNode(Node node)
        {
            node.isSelected = true;
            GUI.changed = true;
        }

        internal virtual void DeselectNode(Node node)
        {
            node.isSelected = false;
            GUI.changed = true;
        }

        internal virtual void DeselectAllNodes()
        {
            // TODO: Is it faster to cache the SelectedNodes list?
            //List<NodeVisual> selectedNodes = SelectedNodes;
            //Debug.Log($"Deselect {selectedNodes.Count} nodes");
            //selectedNodes.ForEach(node => node.isSelected = false);
            //GUI.changed = true;
            Debug.Log($"Deselect {SelectedNodes.Count} nodes");
            SelectedNodes.ForEach(node => node.isSelected = false);
            GUI.changed = true;
        }

        [Shortcut("Node Based Editor/Select All", KeyCode.A, ShortcutModifiers.Alt)]
        public static void SelectAllNodes_Shortcut()
        {
            if (focusedWindow.GetType() == typeof(NodeBasedEditor))
                (focusedWindow as NodeBasedEditor).SelectAllNodes();
        }

        protected virtual void SelectAllNodes()
        {
            temp.nodes.ForEach(node => SelectNode(node));
            Repaint();
        }

        protected virtual void OnClickBackground()
        {
            ClearConnectionSelection();
            DeselectAllNodes();
        }
        #endregion

        #region Connection Events
        internal virtual void OnClickConnectionPoint(Node node, ConnectionPointType type)
        {
            switch (type)
            {
                case ConnectionPointType.In:
                    selectedNodeIn = node;
                    if (selectedNodeOut == null) return;
                    if (!allowLoops)
                    {
                        // TODO Check if the node would be connected downward with itself
                    }
                    break;
                case ConnectionPointType.Out:
                    selectedNodeOut = node;
                    if (selectedNodeIn == null) return;
                    if (!allowLoops)
                    {
                        // TODO Check if the node would be connected upward with itself
                    }
                    break;
                default:
                    break;
            }
            if (selectedNodeOut != selectedNodeIn)
            {
                if (temp.connections.Exists(c => c.inNode == selectedNodeIn && c.outNode == selectedNodeOut))
                    Debug.LogWarning("A connection has already been established between these two nodes.");
                else 
                    CreateConnection(selectedNodeIn, selectedNodeOut);
            }
            ClearConnectionSelection();
        }

        internal virtual void OnClickRemoveConnection(Connection connection)
        {
            temp.connections.Remove(connection);
            hasUnsavedChanges = true;
        }

        internal virtual void CreateConnection(Node nodeIn, Node nodeOut)
        {
            if (temp.connections == null)
                temp.connections = new List<Connection>();
            temp.connections.Add(new Connection(nodeIn, nodeOut, OnClickRemoveConnection));
            hasUnsavedChanges = true;
        }

        // TODO: new method of handling connections where the parent node keeps track of what children are connected to it
        //internal virtual void CreateConnection(Node parent, Node child)
        //{
        //    parent.childNodes.Add(child);
        //    hasUnsavedChanges = true;
        //}

        internal virtual void ClearConnectionSelection()
        {
            selectedNodeIn = null;
            selectedNodeOut = null;
        }
        #endregion
    }
}