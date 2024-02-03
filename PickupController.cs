/*****************************************************************************
// File Name : PickupController.cs
// Author : Craig Hughes
// Creation Date : Ocotber 2, 2023
//
// Brief Description : This script handles health pickups. it holds how much
// health a pickup restores as well as the functionality to have it recharge
// the health pickup and make it reappear
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupController : MonoBehaviour
{

    #region Private Variables
    //represents if the pickup is currently ready
    private bool _pickupReady = true;
    //reference to the material renderer on the object
    private MeshRenderer _pickupRenderer;
    #region Serialized Variables

    [Tooltip("how much health the object restores")]
    [SerializeField] private int _restorationAmount;

    [Header("RECHARGE")]
    [Tooltip("Represents if the pickup can come back")]
    [SerializeField] private bool _isRechargable;
    [Tooltip("How much times it takes for the pickup to comeback")]
    [SerializeField] private float _rechargeTime;
    [Tooltip("The material that appears when the health pack is inactive")]
    [SerializeField] private Material _inactiveMaterial;
    [Tooltip("The material that appears when the health pack is active")]
    [SerializeField] private Material _activeMaterial;
    #endregion

    #endregion
    // Start is called before the first frame update
    private void Start()
    {
        _pickupRenderer = GetComponent<MeshRenderer>();
    }
    #region Collisions


    /// <summary>
    /// Called when recharging healthpack gives player health and stops
    /// waits an aloted amount of time to simulate recharging then 
    /// makes the pickup active again
    /// </summary>
    /// <returns>the time before the pickup is ready to collect again</returns>
    private IEnumerator RestorePickup()
    {
        yield return new WaitForSeconds(_rechargeTime);
        _pickupReady = true;
        //visually turn on the object
        _pickupRenderer.material = _activeMaterial;
        gameObject.SetActive(true);

    }

    #endregion

    #region Getters and Setters

    public bool GetPickupReady()
    {
        return _pickupReady;
    }

    /// <summary>
    /// This script is called when the player collides with a pickup
    /// if it recharges then it swaps tom the inactive material and starts the recharge coroutine
    /// otherwise it destroys itself
    /// lastly it returns the health it gives to the player
    /// </summary>
    /// <returns></returns>
    public int GetRestorationAmount()
    {
        if ( _isRechargable && _pickupReady)
        {
            _pickupReady = false;
            //visually turn off the object
            gameObject.SetActive(false);
            _pickupRenderer.material = _inactiveMaterial;
            //call coroutine
            StartCoroutine(RestorePickup());

        }
        //not rechargable
        else if (!_isRechargable)
        {
            Destroy(gameObject);
        }
        return _restorationAmount;
    }
    #endregion
}
