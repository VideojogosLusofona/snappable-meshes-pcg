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
                backgroundColors = null;
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

        static GUIStyle[] backgroundColors;

        private void DisplayGrid(Rect inStartRect)
        {
            Rect startRect = inStartRect;

            startRect = EditorGUILayout.GetControlRect(true, 0f);
            startRect.width -= 16;

            string[]    colorNames = { "WHT", "RED", "GRN", "BLU", "CYN", "RNG", "YLW", "PNK", "PRP", "BRW", "BLK", "GRY" };
            Color32[] colors = { new Color32(255,255,255,255), new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 128, 255, 255),
                                   new Color32(0,255,255,255), new Color32(255, 128, 0, 255), new Color32(255, 255, 0, 255), new Color32(255, 128, 255, 255),
                                   new Color32(255,0,255,255), new Color32(136, 100, 32, 255), new Color32(0, 0, 0, 255), new Color32(128, 128, 128, 255) };
            Color32[] foregroundColors = { new Color32(0,0,0,255), new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 255),
                                           new Color32(0,0,0,255), new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 255), new Color32(0, 0, 0, 255),
                                           new Color32(0,0,0,255), new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 255) };

            if (backgroundColors == null)
            {
                backgroundColors = new GUIStyle[colorNames.Length];
                for (int i = 0; i < colorNames.Length; i++)
                {
                    backgroundColors[i] = new GUIStyle();
                    Texture2D tex = new Texture2D(2, 2);
                    tex.SetColor(colors[i]);
                    backgroundColors[i].normal.textColor = foregroundColors[i];
                    backgroundColors[i].normal.background = tex;
                    backgroundColors[i].alignment = TextAnchor.MiddleCenter;
                }
            }

            float   elemHeight = 16;
            float   initialColumn = 48;
            float   width = startRect.width;
            float   columnSize = (width - initialColumn) / gridSize.vector2IntValue.x;
            float   totalHeight = elemHeight + gridSize.vector2IntValue.y * elemHeight + elemHeight;
            float   padding = columnSize - 24;

            // Top line
            for (int x = 0; x < gridSize.vector2IntValue.x; x++)
            {
                Rect r = new Rect(startRect.x + initialColumn + columnSize * x, startRect.y, columnSize, elemHeight);
                backgroundColors[x].fixedWidth = columnSize;
                EditorGUI.LabelField(r, colorNames[x], backgroundColors[x]);
            }

            for (int y = 0; y < gridSize.vector2IntValue.y; y++)
            {
                Rect r = new Rect(startRect.x, startRect.y + (y + 1) * elemHeight, initialColumn, elemHeight);
                backgroundColors[y].fixedWidth = initialColumn;
                EditorGUI.LabelField(r, colorNames[y], backgroundColors[y]);

                for (int x = 0; x < gridSize.vector2IntValue.x; x++)
                {
                    r = new Rect(startRect.x + initialColumn + columnSize * x + padding / 2 + 6, startRect.y + (y + 1) * elemHeight, columnSize, elemHeight);
                    var row = GetRowAt(x);
                    EditorGUI.PropertyField(r, row.GetArrayElementAtIndex(y), GUIContent.none);
                }
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(totalHeight);
            EditorGUILayout.EndVertical();
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