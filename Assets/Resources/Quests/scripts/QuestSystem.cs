using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum DialogueType
{
    simple,
    choice
}

[System.Serializable]
public class DialogueLine
{
    public string lineId;
    public string speakerName;
    public DialogueType type;
    public List<string> choices;
    [TextArea] public string text;
    public Emotion emotion;
    public AudioClip voiceOver;
    public float duration = 3f;
    public List<string> nextLine = new List<string>();
}

[System.Serializable]
public enum Emotion
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Surprised,
    Curious
}

[System.Serializable]
public enum EventStatus
{
    Not_Available,
    Available,
    Pending,
    Completed
}

[System.Serializable]
public enum AudioClips
{
    QuestReceived,
    QuestComplete
}

[System.Serializable]
public class AudioEntry
{
    public AudioClips key;
    public AudioClip value;
}

[System.Serializable]
public class QuestCondition
{
    public string requiredQuestId;
    public bool mustBeCompleted;
}

[System.Serializable]
public class QuestObjective
{
    public Object_Behavior objective;
    public string relatedQuestId;
    public string description;
    public string questDescription;
    public bool showAmounts;
    public int requiredAmount;
    public int currentAmount;
    public int missingAmount;
}

[System.Serializable]
public class QuestData
{
    public string questId;
    public string title;
    public string short_description;
    public string long_description;
    public string return_description;
    public List<QuestObjective> objectives;
    public List<DialogueLine> waitingDialogueLines;
    public List<DialogueLine> completedDialogueLines;
    public EventStatus status;
    public bool isMainQuest;
    public int validatorId;
}

[System.Serializable]
public class QuestReward
{
    public int xp;
    public int gold;
    //public List<Item> items;
}

[System.Serializable]
public class EventNode
{
    public string eventId;
    public int npcId;
    public string chapter;
    public EventStatus status;
    public List<QuestCondition> conditions;
    public List<DialogueLine> dialogueLines;
    public QuestData questGiven;
    public QuestReward reward;
    public UnityEvent onEventComplete; // Pour déclencher animation, FX, etc.
}

[System.Serializable]
public class PlayerQuestProgress
{
    public string mainQuestEventId;
    public string questId;
    public bool isMainQuest;
    public EventStatus status;
    public List<QuestObjective> objectives;
}

[System.Serializable]
public class ObjectiveProgress
{
    public string objectiveId;
    public int currentAmount;
}

[System.Serializable]
public class SaveData
{
    public int currentChapter = 1;
    public List<PlayerQuestProgress> activeQuests = new List<PlayerQuestProgress>();
    public Dictionary<string, EventNode> completedEvents = new Dictionary<string, EventNode>();
    // + autres infos (XP, position, inventaire...)
}

public class RequireTagAttribute : PropertyAttribute
{
    public string RequiredTag { get; }

    public RequireTagAttribute(string tag)
    {
        RequiredTag = tag;
    }
}

