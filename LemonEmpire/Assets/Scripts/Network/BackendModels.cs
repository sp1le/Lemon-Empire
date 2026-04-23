using System;
using System.Collections.Generic;

namespace LemonEmpire.Network
{
    [Serializable]
    public class PlayerStateRequest
    {
        public int reaction_score;
        public int morale;
        public string outfit;
        public string state;
        
        // Multi-turn dialogue logic
        public int dialogue_step;
        public string player_reply;
        public int haggle_discount;
    }

    [Serializable]
    public class DialogOption
    {
        public int id;
        public string text;
        public string type; // "normal", "desperate", "aggressive"
    }

    [Serializable]
    public class NpcResponse
    {
        public string npc_greeting;
        public DialogOption[] player_options;
    }
}
