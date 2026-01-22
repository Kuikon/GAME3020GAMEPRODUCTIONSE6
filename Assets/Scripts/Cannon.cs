using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class Cannon : MonoBehaviour
{
    // =========================================================
    // Inspector
    // =========================================================

    [Header("Fire")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float firePower = 25f;

    [Header("Pitch (Barrel)")]
    [SerializeField] private float pitchAngle = 30f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 75f;
    [SerializeField] private float pitchSpeed = 60f;

    [Header("Yaw (Base)")]
    [SerializeField] private Transform cannonBase;
    [SerializeField] private float yawSpeed = 120f;
    [SerializeField] private InputActionReference yawAction;

    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;
    [SerializeField] private InputActionReference pitchAction;

    [Header("Impulse")]
    [SerializeField] private float impulseDuration = 0.3f;

    [Header("Trajectory Preview")]
    [SerializeField] private int trajectoryPoints = 30;
    [SerializeField] private float timeStep = 0.1f;
    [SerializeField] private LayerMask groundMask;

    [Header("Barrel Visual")]
    [SerializeField] private Transform barrel;
    [SerializeField] private Transform barrelPivot;
    [SerializeField] private OrbitCamera orbitCam;
    [SerializeField] private Transform cannonCameraPoint; // 大砲視点用

    private Transform previousCameraTarget;
    // =========================================================
    // Runtime
    // =========================================================

    private RobotController currentPlayer;
    private Rigidbody playerRb;
    private bool isHolding;

    private LineRenderer line;

    // =========================================================
    // Unity Lifecycle
    // =========================================================

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.enabled = false;
        line.useWorldSpace = true;
    }

    private void OnEnable()
    {
        BindInput(true);
    }

    private void OnDisable()
    {
        BindInput(false);
    }

    private void FixedUpdate()
    {
        if (!isHolding) return;

        HandleYaw();
        LockPlayerToCannon();
        HandlePitch();

        UpdateBarrelVisual();
        UpdateTrajectoryPreview();
    }

    // =========================================================
    // Input Binding
    // =========================================================

    private void BindInput(bool enable)
    {
        if (fireAction != null)
        {
            if (enable)
            {
                fireAction.action.Enable();
                fireAction.action.performed += OnFirePressed;
            }
            else
            {
                fireAction.action.performed -= OnFirePressed;
                fireAction.action.Disable();
            }
        }

        if (pitchAction != null)
        {
            if (enable) pitchAction.action.Enable();
            else pitchAction.action.Disable();
        }

        if (yawAction != null)
        {
            if (enable) yawAction.action.Enable();
            else yawAction.action.Disable();
        }
    }

    // =========================================================
    // Trigger
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (isHolding) return;

        RobotController player = other.GetComponent<RobotController>();
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (player == null || rb == null) return;

        HoldPlayer(player, rb);
    }

    // =========================================================
    // State Control
    // =========================================================

    private void HoldPlayer(RobotController player, Rigidbody rb)
    {
        currentPlayer = player;
        playerRb = rb;
        isHolding = true;

        // -----------------------------
        if (orbitCam != null)
        {
            // 元のカメラターゲット（＝通常はプレイヤー）を保存
            previousCameraTarget = orbitCam.CurrentTarget;

            // 大砲視点があるならそちらへ
            if (cannonCameraPoint != null)
                orbitCam.SetTarget(cannonCameraPoint, true);
            else
                // なければ「今入ったプレイヤー」をそのまま見る
                orbitCam.SetTarget(player.transform, true);
        }
        line.enabled = true;
        UpdateTrajectoryPreview();
    }

    private void ReleasePlayer()
    {
        if (currentPlayer != null)
            currentPlayer.SetInputEnabled(true);

        currentPlayer = null;
        playerRb = null;
    }

    // =========================================================
    // Fire
    // =========================================================

    private void OnFirePressed(InputAction.CallbackContext ctx)
    {
        if (!isHolding || currentPlayer == null) return;
        Fire();
    }

    private void Fire()
    {
        isHolding = false;
        line.enabled = false;

        // ★ 外部インパルス開始（時間なし）
        currentPlayer.ApplyExternalImpulse();

        Vector3 direction = GetFireDirection().normalized;
        playerRb.linearVelocity = direction * firePower;

        // ★ 拘束解除（操作はまだ戻らない）
        ReleasePlayer();

        // ★ カメラは即元に戻す
        if (orbitCam != null && previousCameraTarget != null)
            orbitCam.SetTarget(previousCameraTarget, false);

        previousCameraTarget = null;
    }



    // =========================================================
    // Cannon Control
    // =========================================================

    private void HandleYaw()
    {
        if (cannonBase == null || yawAction == null) return;

        float mouseX = yawAction.action.ReadValue<float>();
        float yawDelta = mouseX * yawSpeed * Time.fixedDeltaTime;

        cannonBase.Rotate(Vector3.up, yawDelta, Space.World);
    }

    private void HandlePitch()
    {
        if (pitchAction == null) return;

        float input = pitchAction.action.ReadValue<float>();
        pitchAngle += input * pitchSpeed * Time.fixedDeltaTime;
        pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
    }

    private void LockPlayerToCannon()
    {
        if (currentPlayer == null || playerRb == null) return;

        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        currentPlayer.transform.position = firePoint.position;
        currentPlayer.transform.rotation = firePoint.rotation;
    }

    // =========================================================
    // Direction / Visual
    // =========================================================

    private Vector3 GetFireDirection()
    {
        if (barrelPivot == null)
            return firePoint != null ? firePoint.forward : Vector3.forward;

        Vector3 axis = barrelPivot.right;
        Vector3 baseForward = barrelPivot.forward;

        return Quaternion.AngleAxis(pitchAngle, axis) * baseForward;
    }

    private void UpdateBarrelVisual()
    {
        if (barrel == null) return;

        Vector3 euler = barrel.localEulerAngles;
        euler.x = pitchAngle;
        barrel.localEulerAngles = euler;
    }

    // =========================================================
    // Trajectory Preview
    // =========================================================

    private void UpdateTrajectoryPreview()
    {
        if (line == null || firePoint == null) return;

        Vector3 startPos = firePoint.position;
        Vector3 velocity = GetFireDirection().normalized * firePower;

        line.positionCount = trajectoryPoints;

        Vector3 previous = startPos;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i * timeStep;

            Vector3 point =
                startPos +
                velocity * t +
                0.5f * Physics.gravity * t * t;

            line.SetPosition(i, point);

            if (i > 0 && Physics.Linecast(previous, point, out RaycastHit hit, groundMask))
            {
                line.SetPosition(i, hit.point);
                line.positionCount = i + 1;
                break;
            }

            previous = point;
        }
    }
}
