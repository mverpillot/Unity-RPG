using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    [SerializeField, Range(5f, 50f)] float movementSpeed = 20f;
    [SerializeField, Range(1.5f, 5f)] float sprintMultiplier = 1.5f;
    [SerializeField, Range(2.5f, 3.5f)] float jumpMultiplier = 3.5f;
    [SerializeField, Range(10f, 200f)] int rotationSpeed = 50;

    Animator animator;
    Rigidbody body;
    Camera cam;
    NavMeshAgent agent;
    EventDatabase eventsData;

    public InputSystem_Actions actions;
    bool isReading = false;
    bool isAnimating = false;
    float sprintSpeed = 0f;
    float speedMultiplier = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
        cam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        actions = new InputSystem_Actions();
        eventsData = EventController.EventDB;
    }

    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        actions.Player.Enable();
    }

    void OnDisable()
    {
        actions.Player.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Move();
        Jump();
        Look();
        Interact();
        ToggleFreezeMovements();
    }

    void Move()
    {
        if (!IsGrounded()) return;
        Vector2 movement = actions.Player.Move.ReadValue<Vector2>();
        sprintSpeed = actions.Player.Sprint.ReadValue<float>() * sprintMultiplier + 1;
        speedMultiplier = movement.y > 0 ? movement.y * sprintSpeed : movement.y;



        animator.SetFloat("movement_speed", speedMultiplier);
        animator.SetBool("is_moving", Mathf.Abs(speedMultiplier) > 0);

        transform.Translate(Vector3.forward * speedMultiplier * movementSpeed * Time.deltaTime);
    }

    void Jump()
    {
        bool isGrounded = IsGrounded();

        if (actions.Player.Jump.triggered && isGrounded)
        {
            animator.SetTrigger("jump");
            StartCoroutine(ApplyDelayedJumpForce(0.5f));
        }

        animator.SetBool("in_air", !isGrounded);
    }

    void Look()
    {
        if (!isReading)
        {
            Vector2 movement = actions.Player.Look.ReadValue<Vector2>();

            transform.Rotate(Vector3.up * movement.x * rotationSpeed * Time.deltaTime);
        }
    }

    void Interact()
    {
        GameObject Object = ObjectDetection.closestObject;
        bool isInteracting = actions.Player.Interact.triggered;

        if (Object && IsGrounded() && isInteracting && !isReading)
        {
            if(Object.layer == (int) ObjectLayers.Object)
            {
                Object_Behavior objectData = Object.GetComponent<Object_Behavior>();

                bool hasAnimation = EventController.Instance.TriggerObjectInteractionEvent(objectData);

                if (hasAnimation)
                {
                    animator.SetTrigger("interact");
                    isAnimating = true;
                }
            }

            if (Object.layer == (int) ObjectLayers.NPC)
            {
                EventController.Instance.TriggerNpcInteractionEvent(Object);
            }
        }

        if(isInteracting && isReading)
        {

            if (Object.layer == (int)ObjectLayers.Object)
            {
                Object_Behavior objectData = Object.GetComponent<Object_Behavior>();

                bool hasAnimation = EventController.Instance.TriggerObjectInteractionEvent(objectData);

                if(isAnimating)
                {
                    animator.SetTrigger("cancel");
                    isAnimating = false;
                }
            }
            if (Object.layer == (int)ObjectLayers.NPC)
            {
                if (DialogController.Instance.HasMoreDialogs())
                {
                    DialogController.Instance.NextDialog();
                } else
                {
                    EventController.Instance.TriggerNpcInteractionEvent(Object);
                }
            }
        }
    }

    void ToggleFreezeMovements()
    {
        isReading = DialogController.Instance.isReading;
        if (isReading || isAnimating)
        {
            actions.Player.Disable();
            actions.Player.Interact.Enable();
        } else
        {
            actions.Player.Enable();
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(transform.position, 0.5f, LayerMask.GetMask("Ground"));
    }

    IEnumerator ApplyDelayedJumpForce(float delay)
    {
        Vector3 jumpDirection = Vector3.up + (transform.forward * speedMultiplier);

        yield return new WaitForSeconds(delay);
        body.AddForce(jumpDirection * jumpMultiplier, ForceMode.Impulse);
    }
}
