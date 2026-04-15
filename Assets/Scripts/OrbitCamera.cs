using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Input")]
    [SerializeField] private InputActionReference lookAction;

    [Header("Distance / Height")]
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float height = 1.6f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 0.2f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Follow")]
    [SerializeField] private float followLerpSpeed = 12f;

    [Header("Cinematic")]
    [SerializeField] private float cinematicPositionLerpSpeed = 8f;
    [SerializeField] private float cinematicRotationLerpSpeed = 8f;

    public Transform CurrentTarget => target;

    private float yaw;
    private float pitch;
    private bool inputEnabled = true;

    private bool cinematicOverrideActive = false;
    private Vector3 cinematicPosition;
    private Quaternion cinematicRotation = Quaternion.identity;

    private Transform DrivenTransform => cameraTransform != null ? cameraTransform : transform;

    private void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = transform;

        Vector3 euler = DrivenTransform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
    }

    private void OnEnable()
    {
        if (lookAction != null && lookAction.action != null && inputEnabled)
            lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (lookAction != null && lookAction.action != null)
            lookAction.action.Disable();
    }

    public void SetTarget(Transform newTarget, bool resetAngles = false)
    {
        target = newTarget;

        if (resetAngles)
        {
            yaw = 0f;
            pitch = 10f;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (lookAction == null || lookAction.action == null)
            return;

        if (enabled)
            lookAction.action.Enable();
        else
            lookAction.action.Disable();
    }

    public void MoveToGoalShot(Vector3 worldPosition, Vector3 lookTarget, bool snapImmediately = false)
    {
        Vector3 forward = lookTarget - worldPosition;

        if (forward.sqrMagnitude < 0.0001f)
            forward = DrivenTransform.forward;
        else
            forward.Normalize();

        cinematicPosition = worldPosition;
        cinematicRotation = Quaternion.LookRotation(forward, Vector3.up);
        cinematicOverrideActive = true;

        SetInputEnabled(false);

        if (snapImmediately)
        {
            DrivenTransform.position = cinematicPosition;
            DrivenTransform.rotation = cinematicRotation;
        }
    }

    public void ClearGoalShot(bool enableInput = true)
    {
        cinematicOverrideActive = false;

        if (enableInput)
            SetInputEnabled(true);
    }

    private void LateUpdate()
    {
        Transform driven = DrivenTransform;

        if (cinematicOverrideActive)
        {
            driven.position = Vector3.Lerp(
                driven.position,
                cinematicPosition,
                cinematicPositionLerpSpeed * Time.deltaTime);

            driven.rotation = Quaternion.Slerp(
                driven.rotation,
                cinematicRotation,
                cinematicRotationLerpSpeed * Time.deltaTime);

            return;
        }

        if (target == null)
            return;

        if (!inputEnabled)
            return;

        Vector2 look = Vector2.zero;
        if (lookAction != null && lookAction.action != null)
            look = lookAction.action.ReadValue<Vector2>();

        yaw += look.x * mouseSensitivity;
        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 focusPoint = target.position + Vector3.up * height;
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;

        driven.position = Vector3.Lerp(
            driven.position,
            desiredPosition,
            followLerpSpeed * Time.deltaTime);

        driven.rotation = rotation;
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}