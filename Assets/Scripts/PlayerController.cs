﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public Rigidbody2D _rigidbody;
    [SerializeField] public float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float obstacleCheckRadius;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool isFacingRight;
    [SerializeField] private Transform raycastPosition;
    [SerializeField] private float rayDistance;
    private Vector2 _raycastDirection;
    private Transform _instruction;
    int mask = 1 << 8;
    public bool grounded, goLeft, goRight;
    private enum InstructionSet
    {
        Idle,
        Left,
        Right,
        Jump,
        Push,
    }
    
    private InstructionSet _currentInstruction;
    private void Awake()
    {
        Instance = this;

    }
        private void Start()
    {
        grounded = false;
        _currentInstruction = InstructionSet.Idle;
        _rigidbody = GetComponent<Rigidbody2D>();
        InstructionManager.Instance.OnInstructionGiven += DragObject_OnInstructionGiven;
        _raycastDirection = isFacingRight ? Vector2.right : Vector2.left;
        // Debug.Log(_raycastDirection);
        // Debug.Log(Vector2.left);
        // Debug.Log(Vector2.right);
    }

    private void OnDisable()
    {
        InstructionManager.Instance.OnInstructionGiven -= DragObject_OnInstructionGiven;
    }

    private void DragObject_OnInstructionGiven(object sender, EventArgs e)
    {
        Transform instructionPoint = transform.Find("instructionPoint");
        
        if (instructionPoint.childCount == 1)
        {
            _instruction = instructionPoint.GetChild(0);
            string instructionName = _instruction.GetComponent<InstructionTypeHolder>().instructionType.nameString;
            if (instructionName == InstructionSet.Left.ToString())
            {
                _currentInstruction = InstructionSet.Left;
            }
            else if (instructionName == InstructionSet.Right.ToString())
            {
                _currentInstruction = InstructionSet.Right;
            }
            else if (instructionName == InstructionSet.Jump.ToString())
            {
                _currentInstruction = InstructionSet.Jump;
            }
            else if (instructionName == InstructionSet.Push.ToString())
            {
                _currentInstruction = InstructionSet.Push;
            }
        }
        else
        {
            _currentInstruction = InstructionSet.Idle;
            Debug.Log("No Instructions Found");
        }
    }

    private void Update()
    {
       
        if (LevelShiftHandler.Instance.startGame  && grounded)
        {

            MoveRight();
            this.transform.GetChild(0).transform.Rotate(0, 0, 1);


        }
        if (LevelShiftHandler.Instance.startGame && grounded && goLeft)
        {

            MoveLeft();

        }
        else if (goRight) {
            MoveRight();
        }




        //switch (_currentInstruction)
        //{
        //    case InstructionSet.Idle:
        //    {
        //        Stop();
        //        break;
        //    }
        //    case InstructionSet.Left:
        //    {
        //        MoveLeft();
        //        break;
        //    }
        //    case InstructionSet.Right:
        //    {
        //        MoveRight();
        //        break;
        //    }
        //    case InstructionSet.Jump:
        //    {
        //        Jump();
        //        break;
        //    }
        //    case InstructionSet.Push:
        //    {
        //        Push();
        //        break;
        //    }
        //    default:
        //        break;
        //}
    }

    private bool CheckForObstacle()
    {
        // Collider2D collider2D= Physics2D.OverlapCircle(transform.position, obstacleCheckRadius, whatIsObstacle);
        RaycastHit2D[] raycastHitArray = Physics2D.RaycastAll(raycastPosition.position, _raycastDirection, rayDistance);
        foreach (RaycastHit2D raycastHit in raycastHitArray)
        {
            if (raycastHit.collider.CompareTag("Moveable"))
            {
                _instruction.GetComponent<DragObject>().RestorePosition();
                _currentInstruction = InstructionSet.Idle;

                return true;
            }
        }

        return false;
    }

    private void Push()
    {
        RaycastHit2D[] raycastHitArray = Physics2D.RaycastAll(raycastPosition.position, _raycastDirection, rayDistance);
        foreach (RaycastHit2D raycastHit in raycastHitArray)
        {
            if (raycastHit.collider.CompareTag("Moveable"))
            {
                raycastHit.collider.GetComponent<IMoveable>().Move(_raycastDirection);
                _rigidbody.velocity = new Vector2(_raycastDirection.x * moveSpeed, _rigidbody.velocity.y);
                return;
            }
        }
        _instruction.GetComponent<DragObject>().RestorePosition();
        _currentInstruction = InstructionSet.Idle;
    }
    
    private void MoveLeft()
    {
        if (isFacingRight)
        {
            Flip();
            _raycastDirection = Vector2.left;
        }

        if (!CheckForObstacle())
        {
            _rigidbody.velocity = new Vector2(-1 * moveSpeed, _rigidbody.velocity.y);    
        }
        
    }

    private void MoveRight()
    {
        if (!isFacingRight)
        {
            Flip();
            _raycastDirection = Vector2.right;
        }

        if (!CheckForObstacle())
        {
            _rigidbody.velocity = new Vector2(1 * moveSpeed, _rigidbody.velocity.y);    
        }
    }

    private void Stop()
    {
        _rigidbody.velocity = new Vector2(0 * moveSpeed, _rigidbody.velocity.y);
    }
    
    private void Movement()
    {
        
        float moveInput = Input.GetAxisRaw("Horizontal");

        _rigidbody.velocity = new Vector2(moveInput * moveSpeed, _rigidbody.velocity.y);

        if (moveInput > 0 && !isFacingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && isFacingRight)
        {
            Flip();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }
    }
    
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Transform playerBody = transform.Find("body").GetComponent<Transform>();
        Transform playerHat = transform.Find("hat").GetComponent<Transform>();
        Vector2 scale = playerBody.localScale;
        scale.x *= -1;
        playerBody.localScale = scale;
        playerHat.localScale = scale;
    }

    private void Jump()
    {
        bool isGrounded = Physics2D.OverlapCircle(transform.position, groundCheckRadius, whatIsGround);
        // Debug.Log(isGrounded);
        if (isGrounded)
        {
            if (isFacingRight)
            {
                _rigidbody.velocity = new Vector2(1 * moveSpeed / 2, jumpForce);
            }
            else
            {
                _rigidbody.velocity = new Vector2(-1 * moveSpeed / 2, jumpForce);
            }
                
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Spring"))
        {
            _rigidbody.velocity = Vector2.up * jumpForce;
            print("Jump");
        }
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.collider.CompareTag("Spike"))
        {
            UIManager.Instance.ShowCompletionMessage();
            

            Destroy(this.gameObject);
            Time.timeScale = 0;
        }

        if (other.collider.CompareTag("Door"))
        {
            UIManager.Instance.ShowCompletionMessage("Level Complete");
            Time.timeScale = 0;
        }
        if(other.gameObject.CompareTag("Left"))
        {
            goLeft = true;
            goRight = false;
            LevelShiftHandler.Instance.spring.SetActive(false);
        }
        if (other.gameObject.CompareTag("Right"))
        {
            goLeft = false;
            goRight = true;
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            grounded = true;
        }
    }
    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.collider.CompareTag("Ground"))
        {
            grounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(raycastPosition.position, _raycastDirection * rayDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, groundCheckRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);
        Gizmos.color = Color.green;
    }
}
