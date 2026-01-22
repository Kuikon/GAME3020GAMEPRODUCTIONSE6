using UnityEngine;

[CreateAssetMenu(menuName = "ToyBot/RobotStats_Proto")]
public class RobotStats : ScriptableObject
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public bool canDoubleJump = true;
}
