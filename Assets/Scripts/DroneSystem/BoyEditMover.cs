using UnityEngine;
using UnityEngine.InputSystem;

public class BoyEditMover : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Build";

    [SerializeField] private string moveActionName = "Move";       // Vector2
    [SerializeField] private string lookActionName = "LookRoom";   // Vector2
    [SerializeField] private string boostActionName = "Boost";     // Button

    [Header("Move")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float boostMultiplier = 1.8f;
    [SerializeField] private float rotateSpeed = 120f;

    [Header("References")]
    [SerializeField] private Transform cameraYawReference;

    private InputActionMap map;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction boostAction;

    private bool inputEnabled = true;
    private float yaw;

    public float CurrentYaw => yaw;

    private void Awake()
    {
        map = inputActions.FindActionMap(actionMapName, true);

        moveAction = map.FindAction(moveActionName, true);
        lookAction = map.FindAction(lookActionName, true);
        boostAction = map.FindAction(boostActionName, true);

        yaw = transform.eulerAngles.y;
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

    private void Update()
    {
        if (!inputEnabled)
            return;

        UpdateRotation();
        UpdateMovement();
    }

    private void UpdateRotation()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();

        yaw += look.x * rotateSpeed * Time.unscaledDeltaTime;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void UpdateMovement()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();

        Vector3 forward;
        Vector3 right;

        if (cameraYawReference != null)
        {
            forward = cameraYawReference.forward;
            right = cameraYawReference.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();
        }
        else
        {
            forward = transform.forward;
            right = transform.right;
        }

        Vector3 moveDir = forward * move.y + right * move.x;

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        float speed = moveSpeed;
        if (boostAction.IsPressed())
            speed *= boostMultiplier;

        transform.position += moveDir * speed * Time.unscaledDeltaTime;
    }
}