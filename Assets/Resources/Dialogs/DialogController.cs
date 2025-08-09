using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DialogueType
{
    simple = 1,
    choice = 2
}

public class DialogController : MonoBehaviour
{
    public static DialogController Instance;

    [Header("Dialog Settings")]
    [SerializeField] public GameObject DialogBox;
    public TextMeshProUGUI DialogText;
    public float typeSpeed = 0.01f;

    [HideInInspector]
    public bool isReading;
    List<DialogueLine> currentDialogues;
    DialogueLine currentLine;
    string currentLineId = "";
    public bool dialogFinished = false;
    public bool lineFinished = false;

    private int choiceIndex = -1;

    void Awake()
    {
        Instance = this;
    }

    public void DisplayDialog(List<DialogueLine> dialogues)
    {
        /* On lance la première ligne de dialogue. */
        if(!DialogBox.activeSelf)
        {
            currentDialogues = dialogues;
            isReading = true;

            DialogBox.SetActive(true);
            SetNextLine();
            StartTyping(currentLine.text);
        }
    }

    public void ResetDialog()
    {
        DialogBox.SetActive(false);
        dialogFinished = false;
        currentLineId = "";
        lineFinished = false;
        isReading = false;
    }

    private void SetNextLine()
    {
        lineFinished = false;

        /* Si l'id est vide, on lance le premier dialogue de la ligne,
         * sinon on prend l'id de la propriété nextline qui contient l'id du dialogue suivant
         */
        if(currentLineId == "")
        {
            currentLineId = currentDialogues[0].lineId;
        } else
        {
            currentLineId = currentLine.nextLine[0];
        }

        currentLine = currentDialogues.Find(e => e.lineId == currentLineId);

        StartTyping(currentLine.text);
    } 

    public bool HasMoreDialogs()
    {
        int linesCount = currentLine.nextLine.Count;

        return linesCount > 0;
    }

    public void NextDialog()
    {
        if(lineFinished && !dialogFinished)
        {
            SetNextLine();
        }
    }

    private void StartTyping(string fullText)
    {
        StopAllCoroutines();
        StartCoroutine(TypeText(fullText));
    }

    /* Si le dialogue en cours contient des options de choix
    * - On lance le handler correspondant
    * - On récupère l'index du choix du joueur
    * - On en détermine l'id du prochain dialogue et on relance
    * - "quit" est le mot-clé choisi pour arrêter le dialogue après un choix
    */
    private async void DisplayChoices()
    {
        ChoicesHandler choicesHandler = GetComponent<ChoicesHandler>();
        choiceIndex = await choicesHandler.DisplayButtonsAsync(currentLine.choices);

        currentLineId = currentLine.nextLine[choiceIndex];
        currentLine = currentDialogues.Find(e => e.lineId == currentLineId);

        if (currentLineId != "quit")
        {
            StartTyping(currentLine.text);
        }
        else
        {
            ResetDialog();
        }
    }

    private IEnumerator TypeText(string text)
    {
        DialogText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            DialogText.text += text[i];
            if (i == text.Length - 1)
            {
                if (currentLine.type != DialogueType.choice)
                {
                    lineFinished = true;
                } else
                {
                    DisplayChoices();
                }

                if (currentLine.nextLine.Count == 0)
                {
                    dialogFinished = true;
                }
                StopAllCoroutines();
                yield break;
            }

            yield return new WaitForSeconds(typeSpeed);
        }
    }
}
