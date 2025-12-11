using UnityEngine;
using System.Collections;

// Changed from MonoBehaviour to just a regular class
public abstract class BaseNPC
{
    // Protected fields
    protected GameObject gameObject;
    protected Transform transform;
    protected Animator animator;
    protected CharacterController controller;
    
    // Public properties that can be set in child classes
    public float walkSpeed = 2f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 0.5f;
    
    protected bool isMoving = false;
    
    // Constructor to initialize references
    protected BaseNPC(GameObject npcObject)
    {
        this.gameObject = npcObject;
        this.transform = npcObject.transform;
        this.animator = npcObject.GetComponent<Animator>();
        this.controller = npcObject.GetComponent<CharacterController>();
        
        if (animator == null)
            Debug.LogError($"No Animator found on {gameObject.name}!");
        if (controller == null)
            Debug.LogError($"No CharacterController found on {gameObject.name}!");
    }
    
    protected IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        if (animator == null || controller == null) yield break;
        
        isMoving = true;
        animator.SetBool("isWalking", true);
        
        // Calculate direction
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        // Smooth rotation towards target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float rotationProgress = 0f;
            
            while (rotationProgress < 1f && Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                rotationProgress += Time.deltaTime * rotationSpeed;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationProgress);
                yield return null;
            }
        }
        
        // Movement loop
        while (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            if (controller.enabled)
            {
                // Recalculate direction each frame
                direction = (targetPosition - transform.position).normalized;
                direction.y = 0;
                
                // Apply gravity
                Vector3 gravity = Vector3.zero;
                if (!controller.isGrounded)
                {
                    gravity = Vector3.down * 9.81f * Time.deltaTime;
                }
                
                // Calculate movement
                Vector3 move = direction * walkSpeed * Time.deltaTime + gravity;
                controller.Move(move);
                
                // Keep looking at target while moving
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            yield return null;
        }
        
        // Stop walking
        isMoving = false;
        animator.SetBool("isWalking", false);
        
        // Small pause at destination
        yield return new WaitForSeconds(0.1f);
    }
    
    // Helper to wait for animation to complete
    protected IEnumerator WaitForAnimation(string stateName, float additionalTime = 0f)
    {
        if (animator == null) yield break;
        
        // Wait for animation to start
        yield return null;
        
        // Check if we're in the correct state
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        float timeout = 3f; // Safety timeout
        float timer = 0f;
        
        while (timer < timeout && !state.IsName(stateName))
        {
            state = animator.GetCurrentAnimatorStateInfo(0);
            timer += Time.deltaTime;
            yield return null;
        }
        
        // Wait for exit time (0.95 for your work animations) + additional time
        float waitTime = 0.95f * state.length + additionalTime;
        yield return new WaitForSeconds(waitTime);
    }
}