/*****************************************************************************
// File Name : SolarSailMod.cs
// Author : Craig Hughes, Jacob Zydorowicz, Anna Breuker
// Creation Date : September 20, 2023
//
// Brief Description : This script holds the functionality for a solar sail
// the solar sail is a mod that increases spead while decreasing manuverability
//
*****************************************************************************/
#region NAMESPACE IMPORTS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#endregion
public class SolarSailMod : ShipModBase, ClickMod
{
    #region Private Variables
    //reference to the Ship Health Controller on the ship
    private ShipHealthController _healthController;
    //if the sail is out
    private bool _isSailing = false;

    //ship movement values
    private float pitchTorque;
    private float yawTorque;
    private float maxThrust;
    #endregion

    #region Serialized Variables
    [Tooltip("How much the pitch is divided by when activated")]
    [SerializeField] private float _pitchYawDivisor;

    [Tooltip("How much the thrust is multiplied by when activated")]
    [SerializeField] private float _thrustMultiplier;

    [Tooltip("The maximum amount of time the player can be in the speed boost during use")]
    [SerializeField] float _maxBoostTime;
    private float _nextUseTime;

    [SerializeField] AudioSource activationSound;
    [SerializeField] AudioSource movementSound;
    #endregion

    #region Public Variables
    public static SolarSailMod Instance;
    #endregion

    #region UNITY FUNCTION
    private void Awake()
    {
        //checks singleton instance
        if (Instance == null)
        {
            Instance = this;
        }

        //sets default values
        _maxBoostTime = 3f;
        _nextUseTime = 0f;
    }

    void Start()
    {
        _healthController = GameObject.Find("Player Ship").GetComponent<ShipHealthController>();
    }

    private void Update()
    {
        //if sailing and the max duration or no forward input, then stop sailing
        if (_isSailing && (Time.time >= _nextUseTime || InputControllerBase.Instance.getThrustInput() <= 0))
            StopSail();
    }
    void FixedUpdate()
    {
        movementSound.pitch = 2 + (PlayerMovementController.Instance.getCurrentThrust() / (PlayerMovementController.Instance.GetThrust()));
    }
    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player") && !other.CompareTag("SolarCurrentSegment"))
        {
            StopSail();
        }
    }
    #endregion

    #region SOLAR SAIL FUNCTIONS
    /// <summary>
    /// This method is called when the player toggles the mod button
    /// this divides pitch and yaw by the divisor while multiplying the thrust
    /// </summary>
    public void OnClick()
    {
        if (!_isSailing && !PlayerMovementController.Instance.GetIsPowerSliding())
            StartSail();
        else if(_isSailing)
            StopSail();
    }

    /// <summary>
    /// called by other scripts to check if the sail is currently in use 
    /// </summary>
    /// <returns>If the ship is currently using the sail</returns>
    public bool GetIsSailing()
    {
        return _isSailing;
    }

    /// <summary>
    /// Sets values and effects of the solar sail when it is being used
    /// </summary>
    private void StartSail()
    {
        if(Time.timeScale != 0)
        {
            //Changes the ships movement values if the mod is being used
            if (!_isSailing)
            {
                pitchTorque = PlayerMovementController.Instance.GetPitchTorque() / _pitchYawDivisor;
                yawTorque = PlayerMovementController.Instance.GetYawTorque() / _pitchYawDivisor;
                maxThrust = PlayerMovementController.Instance.GetThrust() * _thrustMultiplier;
                _isSailing = true;
                _nextUseTime = Time.time + _maxBoostTime;
                PlayerMovementController.Instance.currentThrust = maxThrust;
            }

            PlayerMovementController.Instance.SetPitchTorque(pitchTorque);
            PlayerMovementController.Instance.SetYawTorque(yawTorque);
            PlayerMovementController.Instance.SetThrust(maxThrust);
            _healthController.SetUsingSail(true);
            activationSound.Play();
            movementSound.Play();
        }
    }

    /// <summary>
    /// Sets the values and effects for when the solar sail is not being used
    /// </summary>
    private void StopSail()
    {
        if (_isSailing)
        {
            //returns the ship movement values back to their original values when the mod is not in use
            pitchTorque = PlayerMovementController.Instance.GetPitchTorque() * _pitchYawDivisor;
            yawTorque = PlayerMovementController.Instance.GetYawTorque() * _pitchYawDivisor;
            maxThrust = PlayerMovementController.Instance.GetThrust() / _thrustMultiplier;
            _isSailing = false;
            ShipModController.Instance.SetModUsability(ShipModController.Direction.Left, false);
        }

        PlayerMovementController.Instance.SetPitchTorque(pitchTorque);
        PlayerMovementController.Instance.SetYawTorque(yawTorque);
        PlayerMovementController.Instance.SetThrust(maxThrust);

        _healthController.SetUsingSail(false);
        movementSound.Stop();
    }
    #endregion
}
