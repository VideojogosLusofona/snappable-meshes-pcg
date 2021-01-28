using UnityEngine;
using UnityEditor;

namespace Array2DEditor
{
    [CustomEditor(typeof(Array2DBool))]
    public class Array2DBoolEditor : Array2DEditor
    {
        protected override int CellWidth => 12;
        protected override int CellHeight => 12;

        protected override void SetValue(SerializedProperty cell, int x, int y)
        {
            bool[,] defaultCells ={
        {true, true, true, true, true, true, true, true, true, true, true, true},
        {true, true, false, false, false, false, false, false, false, false, false, false},

        {true, false, true, false, false, false, false, false, false, false, false, false},

        {true, false, false, true, false, false, false, false, false, false, false, false},

        {true, false, false, false, true, false, false, false, false, false, false, false},

        {true, false, false, false, false, true, false, false, false, false, false, false},

        {true, false, false, false, false, false, true, false, false, false, false, false},

        {true, false, false, false, false, false, false, true, false, false, false, false},

        {true, false, false, false, false, false, false, false, true, false, false, false},

        {true, false, false, false, false, false, false, false, false, true, false, false},

        {true, false, false, false, false, false, false, false, false, false, true, false},

        {true, false, false, false, false, false, false, false, false, false, false, true},};


            cell.boolValue = default(bool);

            if (x < gridSize.vector2IntValue.x && y < gridSize.vector2IntValue.y)
            {
                cell.boolValue = defaultCells[x, y];
            }
        }



        /// <summary>
        /// Just an example, you can remove this.
        /// </summary>
        private int GetCountActiveCells()
        {
            bool[,] cells = (target as Array2DBool).GetCells();

            var count = 0;

            for (var x = 0; x < gridSize.vector2IntValue.x; x++)
            {
                for (var y = 0; y < gridSize.vector2IntValue.y; y++)
                {
                    count += (cells[x, y] ? 1 : 0);
                }
            }

            return count;
        }
    }
}