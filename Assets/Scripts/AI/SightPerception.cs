using UnityEngine;

public class SightPerception : MonoBehaviour
{
    [SerializeField] private bool isDetected = false;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private GameObject detectionObject;
    private Vector3 targetDirection;

    private void Update()
    {
        ActivateDetection();
    }

    private void ActivateDetection()
    {
        targetDirection = detectionObject.transform.position - transform.position;
        if (Vector3.Dot(transform.forward, Vector3.Normalize(targetDirection)) > 0)
        {
            RaycastHit hit;
            Gizmos.DrawRay(transform.position, targetDirection);
            if (Physics.Raycast(transform.position, targetDirection, out hit, detectionRadius))
            {
                if (hit.collider.gameObject == detectionObject) // Need test based on a component or a tag 
                {
                    isDetected = true;
                    return;
                }
            }
        }
        isDetected = false;
    }
}
