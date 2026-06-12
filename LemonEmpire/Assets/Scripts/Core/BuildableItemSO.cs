using UnityEngine;

namespace LemonEmpire.Core
{
    public enum BuildCategory
    {
        Shelves,
        Cooling,
        Decor,
        Tables
    }

    [CreateAssetMenu(fileName = "NewBuildableItem", menuName = "LemonEmpire/Build Mode/Buildable Item")]
    public class BuildableItemSO : ScriptableObject
    {
        [Header("Identity & Category")]
        public string itemName = "New Item";
        public BuildCategory category = BuildCategory.Decor;

        [Header("Economics")]
        public float cost = 100f;
        public bool isUnique = false;

        [Header("Grid Layout (0.5m cells)")]
        public Vector2Int sizeInCells = new Vector2Int(1, 1);

        [Header("Prefabs")]
        public GameObject previewPrefab; // Semi-transparent or holographic material preview
        public GameObject realPrefab;    // Actual spawned interactive object

        [Header("UI Metadata")]
        public Sprite icon;
    }
}
