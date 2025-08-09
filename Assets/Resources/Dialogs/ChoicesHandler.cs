using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChoicesHandler : MonoBehaviour
{
    [SerializeField] private GameObject panelChoices;
    [SerializeField] private GameObject containerPrefab;
    [SerializeField] private float fillSpeed = 1.5f;
    [SerializeField] private float clickResetDelay = 0.1f;

    private List<Button> generatedButtons = new List<Button>();

    private TaskCompletionSource<int> taskCompletionSource;

    private Coroutine currentFillCoroutine = null;
    private Image currentlyHoveredImage = null;

    public async Task<int> DisplayButtonsAsync(List<string> choices)
    {
        foreach (var btn in generatedButtons)
        {
            Destroy(btn.gameObject);
        }
        generatedButtons.Clear();
        panelChoices.SetActive(true);

        taskCompletionSource = new TaskCompletionSource<int>();

        for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            GameObject container = Instantiate(containerPrefab, panelChoices.transform);
            Button btn = container.GetComponentInChildren<Button>();

            btn.image.fillAmount = 0f;

            TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = choices[i];
            else
                btn.GetComponentInChildren<Text>().text = choices[i];

            generatedButtons.Add(btn);
            AddListeners(btn, index);
        }

        int selectedIndex = await taskCompletionSource.Task;

        return selectedIndex;
    }

    private void AddListeners(Button btn, int index)
    {
        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnButtonEnter((PointerEventData) data));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnButtonExit((PointerEventData) data));
        trigger.triggers.Add(exitEntry);

        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((data) => OnButtonClicked(index));
        trigger.triggers.Add(clickEntry);

        btn.onClick.AddListener(() => OnButtonClicked(index));
    }

    private void OnButtonClicked(int index)
    {
        if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
        {
            taskCompletionSource.SetResult(index);
            panelChoices.SetActive(false);
        }
    }

    public void OnButtonEnter(PointerEventData eventData)
    {
        Button btn = eventData.pointerEnter?.GetComponent<Button>();
        if (btn == null) return;

        Image newImage = btn.image;
        if (newImage == null) return;

        if (currentlyHoveredImage != null && currentlyHoveredImage != newImage)
        {
            StartFilling(currentlyHoveredImage, 0f);
        }

        currentlyHoveredImage = newImage;
        StartFilling(currentlyHoveredImage, 1f);
    }

    public void OnButtonExit(PointerEventData eventData)
    {
        Image exitedImage = eventData.pointerEnter?.GetComponentInChildren<Image>();
        if (exitedImage != null)
        {
            StartFilling(exitedImage, 0f);
            if (currentlyHoveredImage == exitedImage)
                currentlyHoveredImage = null;
        }
    }

    private void StartFilling(Image Img, float target)
    {
        if (currentFillCoroutine != null)
            StopCoroutine(currentFillCoroutine);

        currentFillCoroutine = StartCoroutine(FillTo(Img, target));
    }

    private IEnumerator FillTo(Image Img, float target)
    {
        float speed = 2f;
        while (!Mathf.Approximately(Img.fillAmount, target))
        {
            Img.fillAmount = Mathf.MoveTowards(Img.fillAmount, target, speed * Time.deltaTime);
            yield return null;
        }
    }
}

