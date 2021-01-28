/*
 * Arthur Cousseau, 2019
 * https://www.linkedin.com/in/arthurcousseau/
 * Please share this if you enjoy it! :)
*/

using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Array2DEditor
{
    public abstract class Array2DEditor : Editor
    {
        private const int marginY = 5;

        protected SerializedProperty gridSize;
        protected SerializedProperty cells;

        private Rect lastRect;
        protected Vector2Int newGridSize;
        private bool gridSizeChanged = false;
        private bool wrongSize = false;

        private Vector2 cellSize;

        private Vector2 scrollPos;

        private MethodInfo boldFontMethodInfo = null;

        /// <summary>
        /// In pixels.
        /// </summary>
        protected virtual int CellWidth => 16;

        /// <summary>
        /// In pixels;
        /// </summary>
        protected virtual int CellHeight => 16;

        protected abstract void SetValue(SerializedProperty cell, int x, int y);

        protected virtual void OnEndInspectorGUI()
        {
             if (GUILayout.Button("Reset to Defaults"))
             {
                 InitNewGrid(new Vector2(12,12));
             }
        }


        void OnEnable()
        {
            gridSize = serializedObject.FindProperty("gridSize");
            cells = serializedObject.FindProperty("cells");
            
            newGridSize = gridSize.vector2IntValue;

            cellSize = new Vector2(CellWidth, CellHeight);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update(); // Always do this at the beginning of InspectorGUI.

            using (var h = new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                SetBoldDefaultFont(gridSizeChanged);
                
                GUILayout.Label("Guide / Tentative");
                GUI.enabled = true;

                
                EditorGUI.EndChangeCheck();
            }

            if (wrongSize)
            {
                EditorGUILayout.HelpBox("Wrong size.", MessageType.Error);
            }

            EditorGUILayout.Space();

            if (Event.current.type == EventType.Repaint)
            {
                lastRect = GUILayoutUtility.GetLastRect();
            }

            DisplayGrid(lastRect);

            OnEndInspectorGUI();

            serializedObject
                .ApplyModifiedProperties(); // Apply changes to all serializedProperties - always do this at the end of OnInspectorGUI.
        }
        private void InitNewGrid(Vector2 newSize)
        {
            cells.ClearArray();

            for (var x = 0; x < newSize.x; x++)
            {
                cells.InsertArrayElementAtIndex(x);
                var row = GetRowAt(x);

                for (var y = 0; y < newSize.y; y++)
                {
                    row.InsertArrayElementAtIndex(y);

                    SetValue(row.GetArrayElementAtIndex(y), x, y);
                }
            }

            gridSize.vector2IntValue = newGridSize;
        }

        private void DisplayGrid(Rect startRect)
        {
            string[] colorNames = {"WHT","RED","GRN","BLU","CYN","RNG","YLW","PNK","PRP","BRW","BLK","GRY"};
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);
            {
                EditorGUILayout.BeginHorizontal();
                
                 for(int x = 0; x < gridSize.vector2IntValue.x; x++)
                 {
                    if(x == 0)
                    {
                        GUILayout.Space(26);
                    }
                    GUILayout.Space(cellSize.x);
                    EditorGUILayout.LabelField(colorNames[x], GUILayout.Width(32));

                 }
                EditorGUILayout.EndHorizontal();
                for (var x = 0; x < gridSize.vector2IntValue.x; x++)
                {
                
                    var row = GetRowAt(x);

                    using (var h = new EditorGUILayout.HorizontalScope())
                    {
                        for (var y = 0; y < gridSize.vector2IntValue.y; y++)
                        {
                            
                            
                            EditorGUILayout.BeginHorizontal();

                            if(y == 0)
                                EditorGUILayout.LabelField(colorNames[x], GUILayout.Width(32));
                            
                            EditorGUILayout.PropertyField(row.GetArrayElementAtIndex(y), GUIContent.none,
                                GUILayout.Width(cellSize.x), GUILayout.Height(cellSize.y));
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    GUILayout.Space(marginY);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        protected SerializedProperty GetRowAt(int idx)
        {
            return cells.GetArrayElementAtIndex(idx).FindPropertyRelative("row");
        }

        private void SetBoldDefaultFont(bool value)
        {
            if (boldFontMethodInfo == null)
                boldFontMethodInfo = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont",
                    BindingFlags.Static | BindingFlags.NonPublic);

            boldFontMethodInfo.Invoke(null, new[] {value as object});
        }
    }
}