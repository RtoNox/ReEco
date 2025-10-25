using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTrigger : MonoBehaviour
{
    [Header("Base Settings")]
    public string interactionKey = "E";
    public GameObject interactionHint;
    
    private bool playerInRange = false;
    private PlayerInventory playerInventory;
    
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey) && playerInventory != null)
        {
            playerInventory.ReturnToBase();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInventory = other.GetComponent<PlayerInventory>();
            
            if (interactionHint != null)
            {
                interactionHint.SetActive(true);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerInventory = null;
            
            if (interactionHint != null)
            {
                interactionHint.SetActive(false);
            }
        }
    }
}