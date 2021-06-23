/// Based on the tutorial by Oguzkonya at https://oguzkonya.com/creating-node-based-editor-unity/

using BobJeltes.AI.BehaviorTree;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BobJeltes.NodeEditor
{
    [Serializable]
    public class NodeView
    {
        public Node node;

        public Rect rect;
        public string title = "Node";
        public bool isDragged;
        public bool isSelected;
        public bool multiSelecting;
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;

        public GUIStyle style { get => isSelected ? selectedStyle : regularStyle; }
        public GUIStyle regularStyle;
        public GUIStyle selectedStyle;

        public Action<NodeView> OnClickNode;
        public Action<NodeView> OnClickUp;
        public Action<NodeView> OnRemoveNode;
        public Action<NodeView> OnDragNode;

        private Dictionary<Orientation, Vector2> orientationToSize = new Dictionary<Orientation, Vector2>
        {
            {Orientation.LeftRight, new Vector2(10f, 20f) },
            {Orientation.TopBottom, new Vector2(20f, 15f) }
        };

        public NodeView(Node node)
        {
            this.node = node;
            this.title = node.GetType().Name;
        }

        public NodeView(Rect rect, Orientation orientation, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle, GUIStyle outPointStyle, Action<NodeView> onClickNode, Action<NodeView, ConnectionPointType> onClickConnectionPoint, Action<NodeView> OnClickRemoveNode, Action<NodeView> onDragNode, Action<NodeView> onClickUp)
        {
            this.rect = rect;
            Vector2 connectionPointDimensions = orientationToSize[orientation];
            inPoint = new ConnectionPoint(ConnectionPointType.In, connectionPointDimensions, inPointStyle, onClickConnectionPoint);
            outPoint = new ConnectionPoint(ConnectionPointType.Out, connectionPointDimensions, outPointStyle, onClickConnectionPoint);
            regularStyle = nodeStyle;
            this.selectedStyle = selectedStyle;
            OnRemoveNode = OnClickRemoveNode;
            OnDragNode = onDragNode;
            OnClickNode = onClickNode;
            OnClickUp = onClickUp;
        }

        public void Draw(Orientation direction)
        {
            inPoint?.Draw(direction, this);
            outPoint?.Draw(direction, this);
            GUI.Box(rect, title, style);
        }

        public bool ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            Click();
                            e.Use();
                        }
                    }

                    if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                    {
                        ProcessContextMenu();
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (e.button == 0 && rect.Contains(e.mousePosition))
                    {
                        ClickUp();
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (e.button == 0 && isSelected)
                    {
                        Drag(e.delta, true);
                        e.Use();
                        return true;
                    }
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.LeftControl)
                        multiSelecting = true;
                    break;
                case EventType.KeyUp:
                    if (e.keyCode == KeyCode.LeftControl)
                        multiSelecting = false;
                    break;
                default:
                    break;
            }
            return false;
        }

        private void ProcessContextMenu()
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
            genericMenu.ShowAsContext();
        }

        private void Click()
        {
            isDragged = true;
            GUI.changed = true;
            OnClickNode?.Invoke(this);
        }

        private void ClickUp()
        {
            //Debug.Log("Click up on node " + rect.position);
            isDragged = false;
            OnClickUp?.Invoke(this);
        }

        private void OnClickRemoveNode()
        {
            OnRemoveNode?.Invoke(this);
        }

        public void Drag(Vector2 delta, bool invokeCallbacks)
        {
            rect.position += delta;
            if (invokeCallbacks)
                OnDragNode?.Invoke(this);
        }
    }
}