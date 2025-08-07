using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Object_Behavior : MonoBehaviour
{
    public enum RenderingLayers
    {
        Nothing = 0,
        Everything = ~0,
        Default = 1 << 0,
        Outline = 1 << 1
    }

    public enum InteractionTypes
    {
        Touch,
        Drain
    }

    private Camera mainCam;

    [Header("Object Parameters")]
    public string id;
    public string description;
    public string questDescription;
    public InteractionTypes interactionType;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        ToggleUI();
    }

    void ToggleUI()
    {
        if (gameObject == ObjectDetection.closestObject)
        {
            Vector3 camPos = mainCam.transform.position;
            camPos.y = 1;

            SetOutline(GetComponent<Renderer>());
        }
        else
        {
            RemoveOutline(GetComponent<Renderer>());
        }
    }

    void SetOutline(Renderer rend)
    {
        rend.renderingLayerMask |= (uint)RenderingLayers.Outline;
    }

    void RemoveOutline(Renderer rend)
    {
        rend.renderingLayerMask &= ~(uint)RenderingLayers.Outline;
    }
}


