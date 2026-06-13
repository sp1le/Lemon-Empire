using System.Collections.Generic;
using UnityEngine;

namespace LemonEmpire.Core
{
    public class BuildGrid
    {
        public const float HalfWidth = 11.84f; // Shop floor inside baseboards size is 23.68x23.68 (-11.84 to 11.84)
        public const int GridSize = 48;     // 48 cells
        public const float CellSize = HalfWidth * 2f / GridSize; // ~0.4933333f

        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();

        public void Clear()
        {
            _occupiedCells.Clear();
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            // Center is (0,0) -> maps to index (24, 24)
            int x = Mathf.FloorToInt((worldPos.x + HalfWidth) / CellSize);
            int z = Mathf.FloorToInt((worldPos.z + HalfWidth) / CellSize);
            return new Vector2Int(x, z);
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float x = (gridPos.x * CellSize) - HalfWidth + (CellSize * 0.5f);
            float z = (gridPos.y * CellSize) - HalfWidth + (CellSize * 0.5f);
            return new Vector3(x, -1.30f, z); // Walkable floor height is approx -1.30f
        }

        public bool IsValidCell(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GridSize && cell.y >= 0 && cell.y < GridSize;
        }

        public bool IsCellOccupied(Vector2Int cell)
        {
            if (!IsValidCell(cell)) return true; // Outside bounds is treated as occupied
            
            // Left hall lock check
            Vector3 worldPos = GridToWorld(cell);
            if (!UpgradeManager.IsLeftHallUnlocked && worldPos.x < 0f)
            {
                return true; // Blocked if Left Hall is locked
            }

            return _occupiedCells.Contains(cell);
        }

        public bool AreCellsOccupied(IEnumerable<Vector2Int> cells)
        {
            foreach (var cell in cells)
            {
                if (IsCellOccupied(cell)) return true;
            }
            return false;
        }

        public void OccupyCells(IEnumerable<Vector2Int> cells)
        {
            foreach (var cell in cells)
            {
                if (IsValidCell(cell))
                {
                    _occupiedCells.Add(cell);
                }
            }
        }

        public void FreeCells(IEnumerable<Vector2Int> cells)
        {
            foreach (var cell in cells)
            {
                _occupiedCells.Remove(cell);
            }
        }

        public void OccupyGridFromCollider(Collider col)
        {
            if (col == null) return;
            Bounds b = col.bounds;

            int minX = Mathf.FloorToInt((b.min.x + HalfWidth) / CellSize);
            int maxX = Mathf.CeilToInt((b.max.x + HalfWidth) / CellSize) - 1;
            int minZ = Mathf.FloorToInt((b.min.z + HalfWidth) / CellSize);
            int maxZ = Mathf.CeilToInt((b.max.z + HalfWidth) / CellSize) - 1;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var cell = new Vector2Int(x, z);
                    if (IsValidCell(cell))
                    {
                        _occupiedCells.Add(cell);
                    }
                }
            }
        }
    }
}
