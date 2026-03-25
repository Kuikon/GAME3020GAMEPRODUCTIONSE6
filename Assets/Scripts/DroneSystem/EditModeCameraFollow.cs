using UnityEngine;
using UnityEngine.InputSystem;

public class EditModeCameraFollow : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Build";
    [SerializeField] private string lookActionName = "LookRoom";

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float followLerpSpeed = 12f;

    [Header("Look")]
    [SerializeField] private float pitch = 10f;
    [SerializeField] private float pitchSpeed = 120f;
    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 75f;

    [Header("Mode")]
    [SerializeField] private bool firstPersonLike = true;
    [SerializeField] private float backDistance = 2.2f;
    [SerializeField] private float heightOffset = 1.6f;

    private InputActionMap map;
    private InputAction lookAction;

    private BoyEditMover boyEditMover;
    private bool inputEnabled = true;

    private void Awake()
    {
        map = inputActions.FindActionMap(actionMapName, true);
        lookAction = map.FindAction(lookActionName, true);

        if (target != null)
            boyEditMover = target.GetComponent<BoyEditMover>();
    }

    private void OnEnable()
    {
        map.Enable();
    }

    private void OnDisable()
    {
        map.Disable();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (inputEnabled)
            UpdatePitch();

        UpdateCameraTransform();
    }

    private void UpdatePitch()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();

        pitch -= look.y * pitchSpeed * Time.unscaledDeltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void UpdateCameraTransform()
    {
        float yaw = target.eulerAngles.y;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 desiredPos;

        if (firstPersonLike)
        {
            desiredPos = target.position + new Vector3(0f, heightOffset, 0f);
        }
        else
        {
            Vector3 headPos = target.position + new Vector3(0f, heightOffset, 0f);
            desiredPos = headPos - rotation * Vector3.forward * backDistance;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            followLerpSpeed * Time.unscaledDeltaTime
        );

        transform.rotation = rotation;
    }
}