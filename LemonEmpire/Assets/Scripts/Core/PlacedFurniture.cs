using System.Collections.Generic;
using UnityEngine;

namespace LemonEmpire.Core
{
    public class PlacedFurniture : MonoBehaviour
    {
        public BuildableItemSO itemSO;
        public List<Vector2Int> occupiedCells = new List<Vector2Int>();
        public float purchaseCost;

        public void Setup(BuildableItemSO so, List<Vector2Int> cells, float cost)
        {
            itemSO = so;
            occupiedCells = new List<Vector2Int>(cells);
            purchaseCost = cost;
        }
    }
}
