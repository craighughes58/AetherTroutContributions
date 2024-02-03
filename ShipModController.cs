/*****************************************************************************
// File Name : ShipModController.cs
// Author : Craig Hughes, Jacob Zydorowicz
// Creation Date : September 18, 2023
//
// Brief Description : This script handles the activation and upkeep of all
// modifications on the player's spaceship. It can change, change the position
// of, and use modifications the player has chosen
*****************************************************************************/
#region NAMESPACE IMPORTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#endregion
public class ShipModController : MonoBehaviour
{
    #region PRIVATE VARIABLES
    public static ShipModController Instance;

    //Represents if the first modification is charging
    //private bool _isPrimaryCharging = false;
    //How much charge the first modification currently has
    //private float _primaryCharge = 0; 
    //Represents if the second modification is charging
    //bool _isSecondaryCharging = false;
    //How much charge the secondcurrently has
    //float secondaryCharge = 0;

    //a list of bools that represents if the a modification is charging
    private bool _isCharging;
    //a list of floats that represents how much charge each modification currently has
    private float _currentCharge;
    //a list of floats that represents how much charge each modification can hold
    private float[] _maximumChargeList = new float[4];

    private bool[] _canUseMod = new bool[4];
    //what the current stamina is
    private float _currentPowerStamina;
    private float potentialStaminaUsage = 0f;
    [SerializeField] private float staminaUsed = 0f;
    private Coroutine rechargeCoroutine;
    //represents if the player is using a stamina mod
    //private bool _usingStamina = false;

    private Direction _currentModDirection;
    private Coroutine rotationCoroutine;

    
    //bool for if player is in solar current
    private bool isInCurrent;

    #region Serialized 
    [Header("MODS")]
    [Tooltip("Order does matter! positon 0 = left; 1 = up; 2 = right. Leave a space blank for no mod in that direction.")]
    [SerializeField] private ShipModBase[] _modScripts = new ShipModBase[4];
    [SerializeField] private GameObject[] _modCooldownIndicators = new GameObject[3];
    [SerializeField] private Image[] _modImageCooldownIndicators;
    public Image[] modImages;
    public AnimationCurve cooldownCurve;
    public GameObject reticleCooldownObject;
    public Image reticleCooldownImage;
    private Coroutine reticleCooldownRoutine;
    private float[] modRecharges = new float[4];
    // [Tooltip("Float in seconds")]
    // [SerializeField] private float[] _modCooldown = new float[4];
    // [Tooltip("0 for no max; nonzero for time in seconds")]
    // [SerializeField] private float[] _modMaxHoldTime = new float[4];

    public RectTransform modRotationTransform;
    public AnimationCurve modRoationAnimationCurve;
    public Color modImageDefaultColor;
    public Color modImageSelectedColor;
    [SerializeField] UnityEvent swappedMod;
    [SerializeField] UnityEvent outOfStamina;
    /*
    [Header("STAMINA BAR")]
    [Tooltip("The maximum amount of power up stamina the ship can hold")]
    [SerializeField] private float _maxPowerStamina;
    [Tooltip("How much time it takes for the stamina to start recharging")]
    [SerializeField] private float _powerStaminaRechargeDelay;
    [Tooltip("The multiplier applied to stamina while exhausting it")]
    [SerializeField] private int _staminaExhaustMultiplier;
    [Tooltip("The multiplier applied to stamina while recharging it")]
    [SerializeField] private int _staminaRechargeMultiplier;
    [Tooltip("The event used to trigger the sound effect of a mod switch")]*/
    [SerializeField] UnityEvent modSwitched;
    //UI
    //public Image barImage;
    //public RectTransform blackBar;
    //public RectMask2D blackMask;
    //public RectTransform whiteBar;
    //public RectMask2D whiteMask;
    public enum Direction
    {
        Left,
        Up,
        Right,
        Down
    }



    #endregion

    #endregion

    #region UNITY FUNCTIONS
    private void Awake()
    {
             //Singleton instance check
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        _currentCharge = 0;
        _isCharging = false;

        //populate supporting lists with base values\
        
        for (int i = 0; i < 4; i++)
        {
            if (_modScripts[i] == null)
            {
                _maximumChargeList[i] = 0;
            }
            else
            {
                _maximumChargeList[i] = _modScripts[i].maxCharge;
                _canUseMod[i] = true;
                _modCooldownIndicators[i].SetActive(false);
            }
        }
        foreach(Image modImage in modImages){
            modImage.color = modImageDefaultColor;
        }
        bool foundMod = false;
        for (int i = 0; i < 4; i++)
        {
            if(SwitchMod((Direction)i))
            {
                foundMod = true;
                break;
            }
        }
        if (!foundMod)
        {
            Debug.LogWarning("No mods are set on the player ship!");
        }
    }


    #endregion

    #region updates
    // FixedUpdate is called once every unity time step
    private void FixedUpdate()
    {
        //every fixed update the passive mods are called
        foreach(ShipModBase sm in _modScripts)
        {
            if(sm is PassiveMod)
            {
                ((PassiveMod)sm).OnPassive();
            }
        }
    }

    //called every frame
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.J))
        {
            SingleExhaust(2f);
        }*/
        UpdateCharges();
    }

    /// <summary>
    /// This method is called every frame and checks if there are any 
    /// charges that need to be updated. If there is, the method will
    /// add charge to it and then set it so that it doesn't go over
    /// </summary>
    private void UpdateCharges()
    {
        //update charge
        if (_isCharging)
        {
            //add charge
            _currentCharge += Time.deltaTime;
            //prevent the charge from going over max
            _currentCharge = Mathf.Clamp(_currentCharge, 0f, _maximumChargeList[(int) _currentModDirection]);
        }
    }
    #endregion

    #region Inputs



    /// <summary>
    /// This method will handle the mods that use click and hold/release
    /// to function. first it checks for errors, then it will check if 
    /// it is a click call or a hold call. If it's a click call then
    /// it will call onClick and end. If it's a hold it will update the
    /// proper variables that hold onto hold/release data. When release 
    /// is held it will reset all variables used in the process
    /// </summary>
    /// <param name="obj">the state of the button being pressed</param>
    /// <param name="position">what mod is being used</param>
    public void ActivateMod(InputAction.CallbackContext obj)
    {
        int position = (int) _currentModDirection;
        //catch that prevents errors

        if (_modScripts[position] == null || !_canUseMod[position] || PauseMenuController.isPaused || InputControllerBase.Instance.GetInShip())
        {
            return;
        }
        //is a click
        else if (_modScripts[position] is ClickMod)
        {
            //clicked
            if (!obj.started && obj.performed)
            {
                    ((ClickMod)_modScripts[position]).OnClick();
                    StartCoroutine(ModCooldown((Direction) position));

            }
        }
        //is a hold down
        else if (_modScripts[position] is HoldMod)
        {
            //holding down
            if (!obj.started && obj.performed)
            {
                //has a max hold time
                if (_modScripts[position].maxHoldTime != 0)
                {
                    _isCharging = true;
                    StartCoroutine(TurnOffModAfterTime((Direction) position, _modScripts[position].maxHoldTime));
                    ((HoldMod)_modScripts[position]).OnHold();
                }
                //doesn't have a max hold time
                else
                {
                    _isCharging = true;
                    ((HoldMod)_modScripts[position]).OnHold();
                }
            }
            //releasing
            else if(_currentCharge > 0)
            {
                ReleaseMod((Direction) position);
            }

        }
    }
    public void ReleaseMod(Direction modDirection)
    {
        _isCharging = false;
        ((HoldMod)_modScripts[(int)modDirection]).OnRelease(_currentCharge);
        _currentCharge = 0f;
        StartCoroutine(ModCooldown(modDirection));
    }
    public void ReleaseMod(ShipModBase modScript)
    {
        if (_modScripts.Contains(modScript))
        {
            int index = Array.IndexOf(_modScripts, modScript);
            _isCharging = false;
            ((HoldMod)modScript).OnRelease(_currentCharge);
            //_currentCharge = 0f;
            StartCoroutine(ModCooldown((Direction) index));
        }
        else
        {
            throw new Exception("_modScripts of ShipModController does not have element " + modScript.name);
        }
    }

    IEnumerator ModCooldown(Direction direction)
    {
        
        int dir = (int)direction;
        yield return new WaitUntil(() => _canUseMod[dir] == false);
        _modCooldownIndicators[dir].SetActive(true);
        modRecharges[dir] = 0f;
        if (reticleCooldownRoutine != null)
        {
            StopCoroutine(reticleCooldownRoutine);
        }
        reticleCooldownRoutine = StartCoroutine(ReticleCooldown(dir));

        Color color = _modImageCooldownIndicators[dir].color;
        float cooldown = _modScripts[dir].cooldown;
        while(modRecharges[dir] < cooldown)
        {
            color.a = cooldownCurve.Evaluate(modRecharges[dir] / cooldown);
            _modImageCooldownIndicators[dir].color = color;
            yield return null;
            modRecharges[dir] += Time.deltaTime;
        }

        modRecharges[dir] = 0f;
        _canUseMod[dir] = true;
        _modCooldownIndicators[dir].SetActive(false);
    }

    IEnumerator ReticleCooldown(int direction)
    {
        if (!_modCooldownIndicators[direction].activeSelf)
        {
            reticleCooldownObject.SetActive(false);
            //yield break;
        }
        else
        {
            reticleCooldownObject.SetActive(true);
            float cooldown = _modScripts[direction].cooldown;
            while (_modCooldownIndicators[direction].activeSelf)
            {
                reticleCooldownImage.fillAmount = modRecharges[direction] / cooldown;
                yield return null;
            }
            reticleCooldownObject.SetActive(false);
        }
    }

    IEnumerator TurnOffModAfterTime(Direction direction, float time)
    {
        float timer = time;
        while(timer > 0 && _isCharging)
        {
            if (!(isInCurrent && _modScripts[(int)direction] is SolarSailMod))
            {
                timer -= Time.deltaTime;
            }
            yield return null;
        }
        if(_isCharging && _modScripts[(int) direction] != null && direction == _currentModDirection && _modScripts[(int) _currentModDirection] is HoldMod)
        {
            //reset
            _isCharging = false;
            ((HoldMod)_modScripts[(int) direction]).OnRelease(_currentCharge);
            _currentCharge = 0f;
            StartCoroutine(ModCooldown(direction));
        }
    }
    public bool IsChargingMod(){
        return _isCharging;
    }

    #endregion

    #region Changing Mods
    public bool SwitchMod(Direction modDirection)
    {
        //doesn't switch if a mod is being held
        if(_isCharging) return true;

        //signals to the method caller that there is no mod set for modDirection
        if (_modScripts[(int)modDirection] == null || modDirection == _currentModDirection) return false;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }
        rotationCoroutine = StartCoroutine(ModRotationCoroutine(modDirection));
        if (reticleCooldownRoutine != null)
        {
            StopCoroutine(reticleCooldownRoutine);
        }
        reticleCooldownRoutine = StartCoroutine(ReticleCooldown((int)modDirection));

        _currentModDirection = modDirection;

        modSwitched.Invoke();
        return true;
    }

    IEnumerator ModRotationCoroutine(Direction direction)
    {
        modImages[(int)_currentModDirection].color = modImageDefaultColor;
        float startRotation = (modRotationTransform.localRotation.eulerAngles.z + 360f) % 360f;
        float targetRotation = ((int)direction) * -90f + 360f;
        float rotateAmount = Mathf.Abs(targetRotation - startRotation) < 179f ? targetRotation - startRotation : Mathf.Abs(targetRotation - startRotation) > 181f ? 
            ((targetRotation+180f) % 360f) - ((startRotation+180f) % 360f) : 90 - startRotation < 0 ? 180f : -180f;

        float timer = 0f;
        while (timer < .1f)
        {
            modRotationTransform.localRotation = Quaternion.Euler(0f, 0f, startRotation + (modRoationAnimationCurve.Evaluate(timer / .1f) * rotateAmount));
            yield return null;
            timer += Time.deltaTime;
        }
        modRotationTransform.localRotation = Quaternion.Euler(0f, 0f, targetRotation);
        modImages[(int)_currentModDirection].color = modImageSelectedColor;
        swappedMod.Invoke();
    }

    /// <summary>
    /// called when stamina reaches zero
    /// goes through each stamina mod and turns it off
    /// sets active stamina mods to zero
    /// </summary>
    private void TurnOffAllStaminaMods()
    {
        // Debug.Log("Turn off all stamina mods");
        potentialStaminaUsage = 0f;
        // SetStaminaBar();
        if(_modScripts[(int) _currentModDirection] != null && _modScripts[(int) _currentModDirection] is HoldMod)
        {
            //reset
            _isCharging = false;
            ((HoldMod)_modScripts[(int) _currentModDirection]).OnRelease(_currentCharge);
            _currentCharge = 0f;
            StartCoroutine(ModCooldown(_currentModDirection));
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SolarCurrentSegment"))
        {
            isInCurrent = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SolarCurrentSegment"))
        {
            isInCurrent = false;
        }
    }

    #endregion

    #region Getters
    /*
     * Getters for isUsingMod0 and isUsingMod1 moved to InputControllerBase
     */
    public Direction GetCurrentModDirection()
    {
        return _currentModDirection; 
    }

    public void SetModUsability(Direction modDirection, bool modCanBeUsed)
    {
        _canUseMod[(int)modDirection] = modCanBeUsed;
    }
    #endregion

}
