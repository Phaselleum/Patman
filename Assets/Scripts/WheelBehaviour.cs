using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelBehaviour : MonoBehaviour
{
    /// <summary>
    /// Is this wheel in contact with the ground?
    /// </summary>
    public bool isMakingContact;
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        isMakingContact = true;
    }
    
    private void OnCollisionExit2D(Collision2D other)
    {
        isMakingContact = false;
    }
}
