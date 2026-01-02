using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform body;
    [SerializeField] private float speed, sens, jumpSpeed, gravity;
    private float horizontal, vertical, inputx, inputy, xRotation;
    private Vector3 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        body = GetComponent<Transform>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
   // Update is called once per frame
    void Update()
    {
        Movement();
        Look();
    }

    private void Movement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2.0f;
        }
        
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * speed * Time.deltaTime);


        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpSpeed * -2f * gravity);
        }

        
    }

    private void Look()
    {
        inputx = Input.GetAxis("Mouse X") * sens * Time.deltaTime;
        inputy = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;

        body.Rotate(Vector3.up * inputx);
        xRotation -= inputy;
        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}



