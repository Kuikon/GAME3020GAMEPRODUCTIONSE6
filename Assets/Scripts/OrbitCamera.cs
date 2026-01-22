using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 0.2f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    public Transform CurrentTarget => target;

    private float yaw;
    private float pitch;

    private bool inputEnabled = true;

    private void OnEnable()
    {
        // カーソルのロック/表示は GameModeManager に任せる
        lookAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
    }

    public void SetTarget(Transform newTarget, bool resetAngles = false)
    {
        target = newTarget;

        if (resetAngles)
        {
            yaw = 0f;
            pitch = 0f;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (enabled) lookAction.action.Enable();
        else lookAction.action.Disable();
    }

    private void LateUpdate()
    {
        if (!target) return;
        if (!inputEnabled) return;

        transform.position = target.position;

        Vector2 look = lookAction.action.ReadValue<Vector2>();

        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
