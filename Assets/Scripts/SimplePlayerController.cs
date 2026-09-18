using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menggerakkan Player dengan WASD / Arrow (Input System baru).
/// Player memakai CharacterController supaya tidak menembus obstacle.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Kecepatan gerak Player (unit/detik)")]
    public float moveSpeed = 5f;

    [Tooltip("Kecepatan rotasi menghadap arah gerak")]
    public float turnSpeed = 10f;

    CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Vector2 input = ReadInput();
        Vector3 move = new Vector3(input.x, 0f, input.y);
        if (move.sqrMagnitude > 1f) move.Normalize();

        // SimpleMove sudah menerapkan gravitasi sederhana
        controller.SimpleMove(move * moveSpeed);

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }
    }

    static Vector2 ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f, y = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
        return new Vector2(x, y);
    }
}
