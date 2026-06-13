using UnityEngine;

namespace LemonEmpire.Core
{
    public static class UpgradeManager
    {
        public static bool IsLeftHallUnlocked = false;
        public static bool HasJukebox = false;
        public static bool HasSnackVending = false;
        public static bool HasNeonSigns = false;
        public static bool HasTrashBin = false;
        public static bool HasDoubleCooler = false;
        public static bool HasPremiumShelves = false;
        public static int WarehouseUpgradeLevel = 0;

        public static bool TryPurchaseUpgrade(string name, int cost)
        {
            if (EconomyManager.Instance == null) return false;

            bool isPurchased = false;
            switch (name)
            {
                case "Разширение зала (Левое крыло)":
                case "IsLeftHallUnlocked":
                case "LeftHall":
                    isPurchased = IsLeftHallUnlocked;
                    break;
                case "Музыкальный автомат (Jukebox)":
                case "HasJukebox":
                case "Jukebox":
                    isPurchased = HasJukebox;
                    break;
                case "Торговый автомат (Snacks)":
                case "HasSnackVending":
                case "Snacks":
                    isPurchased = HasSnackVending;
                    break;
                case "Неоновая вывеска (Neon)":
                case "HasNeonSigns":
                case "Neon":
                    isPurchased = HasNeonSigns;
                    break;
                case "Уличная мусорка для коробок":
                case "HasTrashBin":
                case "TrashBin":
                    isPurchased = HasTrashBin;
                    break;
                case "Двойной холодильник":
                case "HasDoubleCooler":
                case "DoubleCooler":
                    isPurchased = HasDoubleCooler;
                    break;
                case "Премиум-полки":
                case "HasPremiumShelves":
                case "PremiumShelves":
                    isPurchased = HasPremiumShelves;
                    break;
                case "Модернизация склада I":
                case "WarehouseUpgrade1":
                    isPurchased = WarehouseUpgradeLevel >= 1;
                    break;
                case "Модернизация склада II":
                case "WarehouseUpgrade2":
                    isPurchased = WarehouseUpgradeLevel >= 2;
                    break;
                case "Модернизация склада III":
                case "WarehouseUpgrade3":
                    isPurchased = WarehouseUpgradeLevel >= 3;
                    break;
                default:
                    return false;
            }

            if (isPurchased) return false;

            if (EconomyManager.Instance.TrySpend(cost))
            {
                switch (name)
                {
                    case "Разширение зала (Левое крыло)":
                    case "IsLeftHallUnlocked":
                    case "LeftHall":
                        IsLeftHallUnlocked = true;
                        break;
                    case "Музыкальный автомат (Jukebox)":
                    case "HasJukebox":
                    case "Jukebox":
                        HasJukebox = true;
                        break;
                    case "Торговый автомат (Snacks)":
                    case "HasSnackVending":
                    case "Snacks":
                        HasSnackVending = true;
                        break;
                    case "Неоновая вывеска (Neon)":
                    case "HasNeonSigns":
                    case "Neon":
                        HasNeonSigns = true;
                        break;
                    case "Уличная мусорка для коробок":
                    case "HasTrashBin":
                    case "TrashBin":
                        HasTrashBin = true;
                        break;
                    case "Двойной холодильник":
                    case "HasDoubleCooler":
                    case "DoubleCooler":
                        HasDoubleCooler = true;
                        break;
                    case "Премиум-полки":
                    case "HasPremiumShelves":
                    case "PremiumShelves":
                        HasPremiumShelves = true;
                        break;
                    case "Модернизация склада I":
                    case "WarehouseUpgrade1":
                        WarehouseUpgradeLevel = 1;
                        break;
                    case "Модернизация склада II":
                    case "WarehouseUpgrade2":
                        WarehouseUpgradeLevel = 2;
                        break;
                    case "Модернизация склада III":
                    case "WarehouseUpgrade3":
                        WarehouseUpgradeLevel = 3;
                        break;
                }
                return true;
            }
            return false;
        }
    }
}
