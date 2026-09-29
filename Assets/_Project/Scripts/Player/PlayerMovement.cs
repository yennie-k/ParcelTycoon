using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Input")]
    [SerializeField] private VirtualJoystick joystick;

    private Rigidbody playerRigidbody;
    private Vector2 moveInput;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector2 keyboardInput = ReadKeyboardInput();

        Vector2 joystickInput =
            joystick != null
                ? joystick.Direction
                : Vector2.zero;

        // 조이스틱을 움직이고 있으면 조이스틱 입력을 우선한다.
        // 조이스틱이 멈춰 있으면 키보드 입력을 사용한다.
        if (joystickInput.sqrMagnitude > 0.001f)
        {
            moveInput = joystickInput;
        }
        else
        {
            moveInput = keyboardInput;
        }
    }

    private void FixedUpdate()
    {
        Vector3 moveDirection = new Vector3(
            moveInput.x,
            0f,
            moveInput.y
        );

        Vector3 currentVelocity = playerRigidbody.linearVelocity;

        playerRigidbody.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            currentVelocity.y,
            moveDirection.z * moveSpeed
        );

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

        return Vector2.ClampMagnitude(input, 1f);
    }
}