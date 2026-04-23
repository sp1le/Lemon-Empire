using System;
using System.Collections.Generic;
using UnityEngine;

namespace LemonEmpire.Trading
{
    [Serializable]
    public class DialogueLine
    {
        public string text;
        public float minReaction;
        public float maxReaction = 999f;
        public float minMorale;
        public float maxMorale = 999f;
        public float reactionBonus;
        public string effect;
        public bool isDesperate;
    }

    [Serializable]
    public class DialogueData
    {
        public List<DialogueLine> greetings;
        public List<DialogueLine> player_replies;
        public List<DialogueLine> success;
        public List<DialogueLine> fail;
    }

    public class DialogueDatabase : MonoBehaviour
    {
        public static DialogueDatabase Instance { get; private set; }

        private DialogueData _data;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadData();
        }

        private void LoadData()
        {
            var json = Resources.Load<TextAsset>("dialogue_lines");
            if (json != null)
            {
                _data = JsonUtility.FromJson<DialogueData>(json.text);
            }
            else
            {
                Debug.LogError("DialogueDatabase: dialogue_lines.json not found in Resources!");
                _data = new DialogueData();
            }
        }

        public DialogueLine GetGreeting(float reaction)
        {
            return GetBestLine(_data.greetings, reaction);
        }

        public List<DialogueLine> GetPlayerReplies(float morale)
        {
            var result = new List<DialogueLine>();
            if (_data.player_replies == null) return result;

            foreach (var line in _data.player_replies)
            {
                if (morale >= line.minMorale && morale <= line.maxMorale)
                {
                    result.Add(line);
                }
            }

            if (result.Count > 3)
            {
                while (result.Count > 3)
                {
                    result.RemoveAt(UnityEngine.Random.Range(0, result.Count));
                }
            }

            return result;
        }

        public DialogueLine GetSuccessLine(float reaction, bool desperate = false)
        {
            if (desperate && _data.success != null)
            {
                foreach (var line in _data.success)
                {
                    if (line.isDesperate) return line;
                }
            }
            return GetBestLine(_data.success, reaction);
        }

        public DialogueLine GetFailLine(float reaction)
        {
            if (_data.fail == null || _data.fail.Count == 0) return null;

            var candidates = new List<DialogueLine>();
            foreach (var line in _data.fail)
            {
                if (reaction <= line.maxReaction)
                    candidates.Add(line);
            }

            if (candidates.Count == 0) return _data.fail[0];
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private DialogueLine GetBestLine(List<DialogueLine> lines, float reaction)
        {
            if (lines == null || lines.Count == 0) return null;

            var candidates = new List<DialogueLine>();
            foreach (var line in lines)
            {
                if (reaction >= line.minReaction)
                {
                    candidates.Add(line);
                }
            }

            if (candidates.Count == 0) return lines[0];
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }
}
