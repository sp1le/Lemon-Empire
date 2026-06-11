using UnityEngine;

namespace LemonEmpire.Core
{
    public enum DailyTrend
    {
        Normal,
        HeatWave,
        PartyNight,
        KidDay,
        Marathon
    }

    public static class GameEventManager
    {
        public static DailyTrend CurrentTrend = DailyTrend.Normal;

        public static void RandomizeTrend()
        {
            System.Array values = System.Enum.GetValues(typeof(DailyTrend));
            CurrentTrend = (DailyTrend)values.GetValue(Random.Range(0, values.Length));
            Debug.Log($"[GameEventManager] Today's trend is: {CurrentTrend}");
        }
    }
}
