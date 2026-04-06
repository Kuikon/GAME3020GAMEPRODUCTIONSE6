using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class BoyEditMover : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Build";

    [SerializeField] private string moveActionName = "Move";       // Vector2
    [SerializeField] private string boostActionName = "Boost";     // Button

    [Header("Move")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float boostMultiplier = 1.8f;
    [SerializeField] private float facingRotateSpeed = 20f;

    [Header("References")]
    [SerializeField] private Transform cameraYawReference;

    [Header("Physics")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private bool freezeYPosition = true;

    private InputActionMap map;
    private InputAction moveAction;
    private InputAction boostAction;

    private bool inputEnabled = true;

    private Vector3 desiredVelocity;
    private float facingYaw;

    public float CurrentYaw => facingYaw;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("[BoyEditMover] InputActionAsset is missing.");
            enabled = false;
            return;
        }

        map = inputActions.FindActionMap(actionMapName, true);
        moveAction = map.FindAction(moveActionName, true);
        boostAction = map.FindAction(boostActionName, true);

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("[BoyEditMover] Rigidbody not found.");
            enabled = false;
            return;
        }

        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (freezeYPosition)
        {
            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ |
                RigidbodyConstraints.FreezePositionY;
        }
        else
        {
            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;
        }

        facingYaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        if (map != null)
            map.Enable();
    }

    private void OnDisable()
    {
        if (map != null)
            map.Disable();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!inputEnabled)
        {
            desiredVelocity = Vector3.zero;

            if (rb != null)
                rb.linearVelocity = Vector3.zero;
        }
    }

    public void SetFacingYaw(float yaw)
    {
        facingYaw = yaw;
    }

    private void Update()
    {
        if (!inputEnabled)
            return;
        UpdateAnimation();
        UpdateDesiredVelocity();
    }

    private void FixedUpdate()
    {
        if (!inputEnabled)
            return;

        ApplyRotation();
        MoveWithRigidbody();
    }

    private void ApplyRotation()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();

        if (move.sqrMagnitude > 0.01f)
        {
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
                Quaternion yawRot = Quaternion.Euler(0f, facingYaw, 0f);
                forward = yawRot * Vector3.forward;
                right = yawRot * Vector3.right;
            }

            Vector3 moveDir = forward * move.y + right * move.x;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                moveDir.Normalize();
                facingYaw = Quaternion.LookRotation(moveDir, Vector3.up).eulerAngles.y;
            }
        }

        Quaternion current = rb.rotation;
        Quaternion desired = Quaternion.Euler(0f, facingYaw, 0f);

        Quaternion next = Quaternion.Slerp(
            current,
            desired,
            facingRotateSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(next);
    }

    private void UpdateDesiredVelocity()
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
            Quaternion yawRot = Quaternion.Euler(0f, facingYaw, 0f);
            forward = yawRot * Vector3.forward;
            right = yawRot * Vector3.right;
        }

        Vector3 moveDir = forward * move.y + right * move.x;

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        float speed = moveSpeed;
        if (boostAction != null && boostAction.IsPressed())
            speed *= boostMultiplier;

        desiredVelocity = moveDir * speed;
    }

    private void MoveWithRigidbody()
    {
        Vector3 velocity = desiredVelocity;

        if (!freezeYPosition)
            velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }
    public Vector2 GetMoveInput()
    {
        if (moveAction == null)
            return Vector2.zero;

        return moveAction.ReadValue<Vector2>();
    }
    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float speed = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z).magnitude;

        animator.SetFloat("Speed", speed);
    }
}