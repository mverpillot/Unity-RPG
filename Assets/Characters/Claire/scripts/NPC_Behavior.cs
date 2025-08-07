using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPC_Behavior : MonoBehaviour
{
    private Camera mainCam;
    private Canvas UICanvas;
    private Image InteractionButton;
    private string questId;
    private List<QuestObjective> objectives;

    [Header("NPC Parameters")]
    public int npcId;
    public string defaultDialogue;
    public List<QuestObjective> questObjectives;

    private void Awake()
    {
        mainCam = Camera.main;
        UICanvas = GetComponentInChildren<Canvas>();
        InteractionButton = GetComponentInChildren<Image>(true);
    }

    private void Start()
    {
        SetEventNpcs();
    }

    private void LateUpdate()
    {
        ToggleUI();
    }

    void ToggleUI()
    {
        Vector3 camPos = mainCam.transform.position;
        camPos.y = 1;

        UICanvas.transform.LookAt(camPos);

        if(gameObject == ObjectDetection.closestObject)
        {
            Image InteractionButton = GetComponentInChildren<Image>();

            InteractionButton.enabled = true;
        } else
        {
            InteractionButton.enabled = false;
        }            
    }

    public static void SetEventNpcs()
    {
        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("NPC");
        List<int> npcValidatorIds = EventController.EventDB.validators;
        List<int> npcQuestGiverIds = EventController.EventDB.questGivers;

        foreach (GameObject npc in allNPCs)
        {
            NPC_Behavior NpcData = npc.GetComponent<NPC_Behavior>();
            TextMeshProUGUI NameTag = npc.GetComponentInChildren<TextMeshProUGUI>();

            NameTag.text = npc.name;

            if (npcValidatorIds.Contains(NpcData.npcId) || npcQuestGiverIds.Contains(NpcData.npcId))
            {
                MeshRenderer QuestIcon = npc.transform.Find("QuestIcon").GetComponent<MeshRenderer>();

                QuestIcon.enabled = true;
            }
        }
    }
}

