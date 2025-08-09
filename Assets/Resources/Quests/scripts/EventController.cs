using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TMPro;
using UnityEngine;

[System.Serializable]
public class EventDatabase
{
    public int currentChapter;
    public int activeEventIndex;
    public List<int> validators;
    public List<int> questGivers;
    public List<EventNode> events;
    public List<EventNode> currentChapterEvents;
    public EventNode activeEvent;
}

[System.Serializable]
public class EventController : MonoBehaviour
{
    public static EventDatabase EventDB;
    public static EventController Instance;

    [SerializeField] 
    public List<AudioEntry> AudioSources;
    
    AudioSource Audio;
    Dictionary<AudioClips, AudioClip> audioDictionary;
    SaveData SaveData;
    TextMeshProUGUI QuestName;
    TextMeshProUGUI QuestDescription;

    void Awake()
    {
        Instance = this;

        Audio = GetComponentInChildren<AudioSource>();
        QuestName = GameObject.Find("QuestName").GetComponent<TextMeshProUGUI>();
        QuestDescription = GameObject.Find("QuestDescription").GetComponent<TextMeshProUGUI>();

        SaveData = SaveSystem.LoadGame();
        LoadSFX();
        LoadEventData();
        SetCurrentChapterEvents();
        SetCurrentEvent();
    }

    private void Start()
    {
        DisplayCurrentEvent();
    }

    void LoadSFX()
    {
        audioDictionary = new Dictionary<AudioClips, AudioClip>();

        foreach (var entry in AudioSources)
        {
            if (!audioDictionary.ContainsKey(entry.key))
                audioDictionary.Add(entry.key, entry.value);
        }
    }

    void LoadEventData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Quests/event_data");
        if (jsonFile == null)
        {
            Debug.LogError("JSON file not found!");
            return;
        }

        EventDB = JsonUtility.FromJson<EventDatabase>(jsonFile.text);
    }

    private void SetCurrentChapterEvents()
    {
        string fieldName = $"chapter{SaveData.currentChapter}";
        List<EventNode> chapterEvents = EventDB.events.FindAll(e => e.chapter == fieldName);

        if (chapterEvents != null)
        {
            EventDB.currentChapterEvents = chapterEvents;
        } else
        {
            Debug.LogWarning($"Champ '{fieldName}' introuvable dans EventDatabase");
        }
    }

    private void SetCurrentEvent()
    {
        PlayerQuestProgress pendingMainQuest = SaveData.activeQuests.Find(quest => quest.isMainQuest && quest.status == EventStatus.Pending);

        if (pendingMainQuest != null) {
            EventDB.activeEvent = EventDB.currentChapterEvents.Find(e => e.eventId == pendingMainQuest.mainQuestEventId);
            EventDB.validators.Add(EventDB.activeEvent.questGiven.validatorId);
        } else
        {
            GetNextQuestNpcId();
        }

        NPC_Behavior.SetEventNpcs();
        DisplayCurrentEvent();
    }

    private void SetActiveQuest(GameObject Npc)
    {
        int npcId = Npc.GetComponent<NPC_Behavior>().npcId;

        EventNode nextEvent = EventDB.events
            .Where(e => e.npcId == npcId && e.status == EventStatus.Available && e.chapter == $"chapter{SaveData.currentChapter}")
            .FirstOrDefault();  

        List<QuestObjective> objectives = Npc.GetComponent<NPC_Behavior>().questObjectives
            .Where(objective => objective.relatedQuestId == nextEvent.questGiven.questId)
            .ToList();

        PlayerQuestProgress nextQuest = new PlayerQuestProgress {
            mainQuestEventId = nextEvent.eventId,
            questId = nextEvent.questGiven.questId,
            isMainQuest = true,
            status = EventStatus.Pending,
            objectives = objectives
        };

        SaveData.activeQuests.Add(nextQuest);

        SetCurrentEvent();
        

        if (audioDictionary.TryGetValue(AudioClips.QuestReceived, out AudioClip audioSource))
        {
            Audio.PlayOneShot(audioSource);
        }

        DialogController.Instance.ResetDialog();
    }

    public void TriggerNpcInteractionEvent(GameObject Npc)
    {
        List<DialogueLine> dialogs = null;
        Action callback = null;
        int npcId = Npc.GetComponent<NPC_Behavior>().npcId;

        bool isValidator = EventDB.validators.Contains(npcId);
        bool isQuestGiver = EventDB.questGivers.Contains(npcId);

        /* Le NPC valide-t-il une quête en cours ? */
        if (isValidator)
        {
            if (EventDB.activeEvent.questGiven.status == EventStatus.Completed)
            {
                dialogs = EventDB.activeEvent?.questGiven?.completedDialogueLines;
                callback = () => ValidateCurrentEvent();
            }
            else
            {
                dialogs = EventDB.activeEvent?.questGiven?.waitingDialogueLines;
            }
        }
        /* Le NPC a-t-il une quête à donner ? */
        else if (isQuestGiver)
        {
            dialogs = GetQuestGiverDialog(Npc);
            callback = () => SetActiveQuest(Npc);
        }
        /* Dialogues par défaut */
        else
        {
            dialogs = new List<DialogueLine>() { new DialogueLine { text = Npc.GetComponent<NPC_Behavior>().defaultDialogue } };
            callback = () => DialogController.Instance.ResetDialog();
        }

        TriggerDialog(dialogs, callback);
    }

    private void IncrementObjective(List<QuestObjective> questObjectives, string objectId)
    {
        bool questIsCompleted = true;
        foreach (var objective in questObjectives)
        {
            if (objective.objective != null && objective.objective.id == objectId && objective.missingAmount > 0)
            {
                objective.currentAmount++;
                objective.missingAmount = Mathf.Max(0, objective.requiredAmount - objective.currentAmount);
            }

            if(objective.missingAmount > 0)
            {
                questIsCompleted = false;
            }
        }

        if(questIsCompleted)
        {
            DisplayCurrentEvent(true);
            EventDB.activeEvent.questGiven.status = EventStatus.Completed;
            EventNode activeQuest = EventDB.events
                .Where(e => e.questGiven.questId == EventDB.activeEvent.questGiven.questId)
                .FirstOrDefault();            

            activeQuest.questGiven.status = EventStatus.Completed;

            PlayerQuestProgress activeQuestSave = SaveData.activeQuests
                .Where(e => e.questId == EventDB.activeEvent.questGiven.questId)
                .FirstOrDefault();

            activeQuestSave.status = EventStatus.Completed;

            if (audioDictionary.TryGetValue(AudioClips.QuestReceived, out AudioClip audioSource))
            {
                Audio.PlayOneShot(audioSource);
            }
        }

        DialogController.Instance.ResetDialog();
    }

    public bool TriggerObjectInteractionEvent(Object_Behavior gameObject)
    {
        List<QuestObjective> questObjectives = SaveData.activeQuests
            .Where(q => q.questId == gameObject.id)
            .SelectMany(q => q.objectives)
            .ToList();

        DialogueLine objectDescription;
        List<DialogueLine> dialogueLine = new List<DialogueLine>();
        string questId = EventDB.activeEvent?.questGiven.questId;
        string objectId = gameObject.id;

        if(questId != objectId || questObjectives == null || questObjectives.Count == 0)
        {
            objectDescription = new DialogueLine { text = gameObject.description };
            dialogueLine.Add(objectDescription);

            TriggerDialog(dialogueLine, () => DialogController.Instance.ResetDialog());
            return false;
        }

        objectDescription = new DialogueLine { text = gameObject.questDescription };
        dialogueLine = new List<DialogueLine>() { objectDescription };

        TriggerDialog(dialogueLine, () => IncrementObjective(questObjectives, gameObject.id));

        return true;
    }

    private void TriggerDialog(List<DialogueLine> dialogs, Action callback = null)
    {
        /* Si c'est la dernière ligne de dialogue => On lance la fonction de callback (activer/valider la quête) */
        if (DialogController.Instance.isReading && !DialogController.Instance.HasMoreDialogs())
        {
            callback?.Invoke();
        }
        else
        {
            /* Sinon (si c'est la première ligne de dialogue) affiche le panneau de de dialogue */
            if (dialogs == null || dialogs.Count == 0)
            {
                Debug.LogWarning("No dialog lines to display.");
                callback?.Invoke();
                return;
            }

            DialogController.Instance.DisplayDialog(dialogs);
        }
    }

    public void ValidateCurrentEvent()
    {
        /* On enregistre la complétion de quête */
        EventDB.activeEvent.questGiven.status = EventStatus.Completed;
        EventDB.validators.Remove(EventDB.activeEvent.questGiven.validatorId);
        SaveData.completedEvents.Add(EventDB.activeEvent.eventId, EventDB.activeEvent);
        DialogController.Instance.ResetDialog();

        /* SFX de victoire */
        if(audioDictionary.TryGetValue(AudioClips.QuestComplete, out AudioClip audioSource))
        {
            Audio.PlayOneShot(audioSource);
        }

        /* S'il n'y a plus de quêtes dans le chapitre en cours, on charge les events du chapitre suivant */
        EventNode nextEvent = EventDB.events
                .Where(e => e.status == EventStatus.Available && e.chapter == $"chapter{EventDB.currentChapter}")
                .FirstOrDefault();

        if (nextEvent == null)
        {
            SaveData.currentChapter++;
            EventDB.currentChapter++;
            SetCurrentChapterEvents();
        }

        SetCurrentEvent();

        /* TO DO : Uncomment to save progress */
        //SaveSystem.SaveGame(SaveData);        
    }

    private void DisplayCurrentEvent(bool questIsCompleted = false)
    {
        if (EventDB.activeEvent != null)
        {
            QuestName.text = EventDB.activeEvent.questGiven.title;
            QuestDescription.text = questIsCompleted ? EventDB.activeEvent.questGiven.return_description : EventDB.activeEvent.questGiven.short_description;
        }
    }

    public List<EventNode> GetCurrentChapterEvents()
    {
        return EventDB.currentChapterEvents;
    }

    public EventNode GetEventByID(string id)
    {
        return EventDB.currentChapterEvents.Find(e => e.eventId == id);
    }

    private void GetNextQuestNpcId()
    {
        EventDB.activeEvent = null;
        EventDB.questGivers = EventDB.events
                                .Where(e => e.status == EventStatus.Available && !SaveData.completedEvents.ContainsKey(e.eventId))
                                .Select(e => e.npcId)
                                .ToList();
    }

    private List<DialogueLine> GetQuestGiverDialog(GameObject Npc)
    {
        NPC_Behavior NpcData = Npc.GetComponent<NPC_Behavior>();

        List<DialogueLine> dialogues = EventDB.events
                    .Where(e => e.npcId == NpcData.npcId && !SaveData.completedEvents.ContainsKey(e.eventId))
                    .SelectMany(e => e.dialogueLines)
                    .ToList();

        if(dialogues.Count == 0)
        {
            return new List<DialogueLine>() { new DialogueLine { text = NpcData.defaultDialogue } };
        } else
        {
            return dialogues;
        }
    }
}

public class SaveSystem
{
    static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("✅ Sauvegarde terminée !");
    }

    public static SaveData LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("Aucune sauvegarde trouvée dans " + SavePath + ".");
            return new SaveData();
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        Debug.Log("✅ Chargement terminé !");
        return data;
    }
}
