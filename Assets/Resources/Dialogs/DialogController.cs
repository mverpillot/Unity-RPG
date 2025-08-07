using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogController : MonoBehaviour
{
    public static DialogController Instance;

    [SerializeField] public GameObject DialogBox;
    public TextMeshProUGUI DialogText;
    public List<DialogueLine> currentDialogues;
    public DialogueLine currentLine;
    public bool dialogFinished = false;
    public string currentLineId = "";
    public bool lineFinished = false;

    public float typeSpeed = 0.01f;
    public bool isReading;

    void Awake()
    {
        Instance = this;
    }

    public void DisplayDialog(List<DialogueLine> dialogues)
    {
        if(DialogBox.activeSelf)
        {
            ResetDialog();
        } else
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
        if(currentLineId == "")
        {
            currentLineId = currentDialogues[0].lineId;
        } else
        {
            /* TO DO CHOICES */
            //if(currentLine.type == DialogueType.choice)
            //{
            //    currentLineId = DisplayChoices();
            //} else
            //{
            //    currentLineId = currentLine.nextLine[0];
            //}
            currentLineId = currentLine.nextLine[0];
        }

        currentLine = currentDialogues.Find(e => e.lineId == currentLineId);

        if (currentLineId != "quit") {
            StartTyping(currentLine.text);
        } else
        {
            ResetDialog();
        }
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

    private string DisplayChoices()
    {
        return "";
    }

    private IEnumerator TypeText(string text)
    {
        DialogText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            DialogText.text += text[i];

            if (i == text.Length - 1)
            {
                lineFinished = true;
                if(currentLine.nextLine.Count == 0)
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
