using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;

    private Rigidbody playerRigidbody;
    private Vector2 moveInput;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = ReadKeyboardInput();
    }

    private void FixedUpdate()
    {
        // Vector2 입력을 Unity의 3D 방향으로 변환한다.
        Vector3 moveDirection = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        // 기존의 위아래 속도는 유지하면서 X, Z 방향으로 이동한다.
        Vector3 currentVelocity = playerRigidbody.linearVelocity;

        playerRigidbody.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            currentVelocity.y,
            moveDirection.z * moveSpeed
        );

        // 움직이고 있을 때만 이동 방향을 바라본다.
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(moveDirection, Vector3.up);

            Quaternion nextRotation =
                Quaternion.Slerp(
                    playerRigidbody.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                );

            playerRigidbody.MoveRotation(nextRotation);
        }
    }

    private Vector2 ReadKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        float horizontal = 0f;
        float vertical = 0f;

        // 왼쪽과 오른쪽
        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        // 앞과 뒤
        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        Vector2 input = new Vector2(horizontal, vertical);

        // 대각선 이동이 더 빨라지는 것을 방지한다.
        return Vector2.ClampMagnitude(input, 1f);
    }
}