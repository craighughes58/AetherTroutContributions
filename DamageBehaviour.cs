/*****************************************************************************
// File Name : DamageBehaviour.cs
// Author : Craig Hughes
// Creation Date : Ocotber 2, 2023
//
// Brief Description : This script holds the amount of damage an object can 
// inflict on a player. The player then calls this script to calculate the
// total damage and send it back to the healthcontroller
*****************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageBehaviour : MonoBehaviour
{
    #region Private Variables

    #region Serialized Variables
    [Tooltip("How much damage the object gives the player")]
    [SerializeField] private int _baseDamage;
    #endregion
    #endregion


    /// <summary>
    /// Place holder method that just returns base damage
    /// </summary>
    /// <returns>how much damage the object inflicts</returns>
    public int CalculateDamage()
    {
        return _baseDamage;
    }
}
