using UnityEngine;

public class ConveyorScroll : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 1.0f;
    [SerializeField] private Vector2 scrollDirection = Vector2.left; // �� ����
    [SerializeField] private int materialIndex = 1; // �� threads
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 2.0f;      // �^�ԑ���
    [SerializeField] private Vector3 moveDirection = Vector3.left; // �� ���ɗ���錩���ڂƓ�������
    [SerializeField] private string targetTag = "Player";
    private Renderer rend;
    private Material mat;
    private Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
        mat = rend.materials[materialIndex]; // �� �������}�e���A��
    }

    void Update()
    {
        offset += scrollDirection.normalized * scrollSpeed * Time.deltaTime;
        mat.SetTextureOffset("_BaseMap", offset);
    }
    void OnCollisionStay(Collision collision)
    {
        RobotController robot = collision.collider.GetComponent<RobotController>();
        if (!robot) return;

        Debug.Log($"HIT: {collision.collider.name}");

        Vector3 v = moveDirection.normalized * moveSpeed;
        robot.SetConveyorVelocity(v);
    }

}
