using UnityEngine;
using UnityEngine.InputSystem;

public class EditModeCameraFollow : MonoBehaviour
{
    public enum CameraMode
    {
        Near,
        Fixed
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Build";
    [SerializeField] private string lookActionName = "LookRoom";
    [SerializeField] private string lookHoldActionName = "LookHold";

    [Header("Target")]
    [SerializeField] private Transform target;
    [Header("Ground Center Target")]
    [SerializeField] private Transform groundCenterTarget;
    [Header("Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.Fixed;

    [Header("Near Camera")]
    [SerializeField] private Vector3 nearOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float nearFollowLerpSpeed = 12f;

    [Header("Fixed Camera")]
    [SerializeField] private Transform fixedCameraPoint;
    [SerializeField] private float fixedLerpSpeed = 8f;

    [Header("Look")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 10f;
    [SerializeField] private float yawSpeed = 180f;
    [SerializeField] private float pitchSpeed = 120f;
    [SerializeField] private float pitchMin = -60f;
    [SerializeField] private float pitchMax = 75f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorWhileLooking = true;
    [SerializeField] private bool hideCursorWhileLooking = true;

    [Header("Layer Control")]
    [SerializeField] private string boyLayerName = "Boy";

    private int boyLayer;
    private int originalCullingMask;
    private Camera cam;

    private InputActionMap map;
    private InputAction lookAction;
    private InputAction lookHoldAction;

    private bool inputEnabled = true;
    private bool wasLookingLastFrame = false;
    private bool cameraControlEnabledInZone = false;

    // cached fixed pose
    private Vector3 cachedFixedPosition;
    private Quaternion cachedFixedRotation;
    private bool hasCachedFixedPose = false;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("[EditModeCameraFollow] InputActionAsset is missing.", this);
            enabled = false;
            return;
        }

        map = inputActions.FindActionMap(actionMapName, true);
        lookAction = map.FindAction(lookActionName, true);
        lookHoldAction = map.FindAction(lookHoldActionName, true);

        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
        cam = GetComponent<Camera>();
        if (cam != null)
            originalCullingMask = cam.cullingMask;

        boyLayer = LayerMask.NameToLayer(boyLayerName);
    }

    private void OnEnable()
    {
        if (map != null)
            map.Enable();
    }

    private void OnDisable()
    {
        RestoreCursor();

        if (map != null)
            map.Disable();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
            RestoreCursor();
    }

    public void SetNearMode()
    {
        currentMode = CameraMode.Near;
        hasCachedFixedPose = false;

        if (target != null && groundCenterTarget != null)
        {
            // first guess near position
            Vector3 desiredNearPos = target.position + nearOffset;

            Vector3 dir = groundCenterTarget.position - desiredNearPos;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                Vector3 euler = lookRot.eulerAngles;

                yaw = euler.y;
                pitch = NormalizePitch(euler.x);
                pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
            }
        }
        else
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
        }

        if (cam != null && boyLayer >= 0)
        {
            cam.cullingMask &= ~(1 << boyLayer);
        }
    }

    public void SetFixedMode()
    {
        currentMode = CameraMode.Fixed;
        CacheFixedPose();
        RestoreCursor();

        // 👇 SHOW BOY AGAIN
        if (cam != null)
        {
            cam.cullingMask = originalCullingMask;
        }
    }

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;

        if (mode == CameraMode.Fixed)
        {
            CacheFixedPose();
            RestoreCursor();
        }
        else
        {
            hasCachedFixedPose = false;
        }
    }

    public void SetCameraControlInZone(bool enabled)
    {
        cameraControlEnabledInZone = enabled;

        if (!enabled)
            RestoreCursor();
    }

    public bool IsCameraControlInZone()
    {
        return cameraControlEnabledInZone;
    }

    private void LateUpdate()
    {
        if (target == null && currentMode == CameraMode.Near)
            return;

        bool canLook = inputEnabled && cameraControlEnabledInZone;
        bool isLooking = canLook && IsLookHeld();

        if (currentMode == CameraMode.Near && isLooking)
            UpdateLook();

        UpdateCursorState(isLooking);
        UpdateCameraTransform();

        wasLookingLastFrame = isLooking;
    }

    private bool IsLookHeld()
    {
        if (lookHoldAction == null)
            return false;

        return lookHoldAction.IsPressed();
    }

    private void UpdateLook()
    {
        if (lookAction == null)
            return;

        Vector2 look = lookAction.ReadValue<Vector2>();

        yaw += look.x * yawSpeed * Time.unscaledDeltaTime;
        pitch -= look.y * pitchSpeed * Time.unscaledDeltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void UpdateCameraTransform()
    {
        if (currentMode == CameraMode.Near)
            UpdateNearCamera();
        else
            UpdateFixedCamera();
    }

    private void UpdateNearCamera()
    {
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // camera offset relative to camera rotation
        Vector3 desiredPos = target.position + rotation * nearOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            nearFollowLerpSpeed * Time.unscaledDeltaTime
        );

        transform.rotation = rotation;
    }
    private void UpdateFixedCamera()
    {
        if (!hasCachedFixedPose)
            CacheFixedPose();

        if (!hasCachedFixedPose)
            return;

        transform.position = cachedFixedPosition;
        transform.rotation = cachedFixedRotation;
    }

    private void CacheFixedPose()
    {
        if (fixedCameraPoint == null)
        {
            Debug.LogWarning("[EditModeCameraFollow] FixedCameraPoint is not assigned.", this);
            return;
        }

        cachedFixedPosition = fixedCameraPoint.position;
        cachedFixedRotation = fixedCameraPoint.rotation;
        hasCachedFixedPose = true;
    }

    private void UpdateCursorState(bool isLooking)
    {
        if (!lockCursorWhileLooking && !hideCursorWhileLooking)
            return;

        if (!cameraControlEnabledInZone || currentMode != CameraMode.Near)
        {
            RestoreCursor();
            return;
        }

        if (isLooking && !wasLookingLastFrame)
        {
            if (lockCursorWhileLooking)
                Cursor.lockState = CursorLockMode.Locked;

            if (hideCursorWhileLooking)
                Cursor.visible = false;
        }
        else if (!isLooking && wasLookingLastFrame)
        {
            RestoreCursor();
        }
    }

    private void RestoreCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private float NormalizePitch(float xAngle)
    {
        if (xAngle > 180f)
            xAngle -= 360f;

        return xAngle;
    }
}