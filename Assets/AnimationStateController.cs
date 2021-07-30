using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationStateController : MonoBehaviour
{

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //if w is pressed
        if (Input.GetKey("w"))
        {
            if (Input.GetKey("left shift"))
            {
                animator.SetBool("IsRunning", true);
            }
            else
            {
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsWalking", true);
            }
            
        }

        if (!Input.GetKey("w"))
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }

        if (Input.GetKey("r"))
        {
            animator.SetBool("IsDancing", true);
        }
        if (!Input.GetKey("r"))
        {
            animator.SetBool("IsDancing", false);
        }

        if (Input.GetKey("d"))
        {
            animator.SetBool("IsRight", true);
        }
        if (!Input.GetKey("d"))
        {
            animator.SetBool("IsRight", false);
        }

        if (Input.GetKey("a"))
        {
            animator.SetBool("IsLeft", true);
        }
        if (!Input.GetKey("a"))
        {
            animator.SetBool("IsLeft", false);
        }

        if (Input.GetKey("s"))
        {
            animator.SetBool("IsReverse", true);
        }
        if (!Input.GetKey("s"))
        {
            animator.SetBool("IsReverse", false);
        }

    }
}
