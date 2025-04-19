using UnityEngine;

public class Movement : MonoBehaviour
{   
    public float moveSpeed = 0.5f;
    Rigidbody2D _playerBody;
    private Vector2 _moveInput;

    void Start()
    {
        _playerBody = GetComponent<Rigidbody2D>();
    }


    void Update()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");
        _moveInput.Normalize();
    }

    void FixedUpdate()
    {
        _playerBody.linearVelocity = _moveInput * moveSpeed;
    }

}
