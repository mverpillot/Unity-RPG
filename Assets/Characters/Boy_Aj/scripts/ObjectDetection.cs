using UnityEngine;
using UnityEngine.UI;

public enum ObjectLayers
{
    NPC = 6,
    Object = 7
}

public class ObjectDetection : MonoBehaviour
{
    [SerializeField, Range(1f, 4f)] float detectionRadius = 3.5f;
    [SerializeField] LayerMask detectionMask;
    
    public static GameObject closestObject;

    void Update()
    {
        closestObject = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, detectionMask);

        if (hits.Length > 0)
        {
            float shortestDistance = Mathf.Infinity;

            foreach ( Collider hit in hits )
            {
                float hitDistance = Vector3.Distance(transform.position, hit.transform.position);

                if(hitDistance < shortestDistance)
                {
                    shortestDistance = hitDistance;
                    closestObject = hit.gameObject;
                }
            }
        }
    }
}
