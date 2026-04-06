using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Boy";
    [SerializeField] private EditModeCameraFollow editCamera;

    private int insideCount = 0;

    private void Awake()
    {
        if (editCamera == null)
            editCamera = FindFirstObjectByType<EditModeCameraFollow>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform root = other.transform.root;
        Debug.Log("[CameraTriggerZone] Hit: " + other.name);

        if (!root.CompareTag(playerTag))
            return;

        insideCount++;

        if (editCamera != null)
        {
            editCamera.SetNearMode();
            editCamera.SetCameraControlInZone(true);
            Debug.Log("[CameraTriggerZone] Near ON");
        }
        else
        {
            Debug.LogWarning("[CameraTriggerZone] EditModeCameraFollow not found.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform root = other.transform.root;

        if (!root.CompareTag(playerTag))
            return;

        insideCount--;
        if (insideCount < 0)
            insideCount = 0;

        if (insideCount == 0)
        {
            if (editCamera != null)
            {
                editCamera.SetFixedMode();
                editCamera.SetCameraControlInZone(false);
                Debug.Log("[CameraTriggerZone] Fixed ON");
            }
            else
            {
                Debug.LogWarning("[CameraTriggerZone] EditModeCameraFollow not found.");
            }
        }
    }
}