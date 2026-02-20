using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Build";

    [SerializeField] private string moveActionName = "Move";   // Vector2 (WASD)
    [SerializeField] private string lookActionName = "Look";   // Vector2 (Mouse delta)
    [SerializeField] private string upActionName = "Up";     // Button (E)
    [SerializeField] private string downActionName = "Down";   // Button (Q)
    [SerializeField] private string boostActionName = "Boost"; // Button (Shift)

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float boostMultiplier = 3f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private bool holdRightMouseToLook = true;
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    private InputActionMap map;
    private InputAction moveAction, lookAction, upAction, downAction, boostAction;

    private bool inputEnabled = true;
    private float pitch;

    private void Awake()
    {
        map = inputActions.FindActionMap(actionMapName, true);

        moveAction = map.FindAction(moveActionName, true);
        lookAction = map.FindAction(lookActionName, true);
        upAction = map.FindAction(upActionName, true);
        downAction = map.FindAction(downActionName, true);
        boostAction = map.FindAction(boostActionName, true);

        // ‰ŠúPitch
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
    }

    private void OnEnable()
    {
        map.Enable();
    }

    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    private void Update()
    {
        if (!inputEnabled) return;

        // ===== Look =====
        bool canLook = !holdRightMouseToLook || (Mouse.current != null && Mouse.current.rightButton.isPressed);
        if (canLook)
        {
            Vector2 look = lookAction.ReadValue<Vector2>() * lookSensitivity;

            float yawDelta = look.x;
            pitch = Mathf.Clamp(pitch - look.y, pitchMin, pitchMax);

            float yaw = transform.eulerAngles.y + yawDelta;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // ===== Move =====
        Vector2 move = moveAction.ReadValue<Vector2>();

        float vertical = 0f;
        if (upAction.IsPressed()) vertical += 1f;
        if (downAction.IsPressed()) vertical -= 1f;

        Vector3 dir = transform.forward * move.y
                    + transform.right * move.x
                    + Vector3.up * vertical;

        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = moveSpeed * (boostAction.IsPressed() ? boostMultiplier : 1f);


        transform.position += dir * speed * Time.unscaledDeltaTime;
    }
}
