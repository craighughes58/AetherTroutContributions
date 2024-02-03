/*****************************************************************************
// File Name : ShipModBase.cs
// Author : Craig Hughes
// Creation Date : September 18, 2023
//
// Brief Description : This is the foundation for all ship mods. This 
// maintains the structure for all mods and their different subtypes.
// Mod types mandates are implemented here 
*****************************************************************************/

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class ShipModBase : MonoBehaviour
{
    //every mod has to have a maximum charge for upkeep purposes
    //If there is no charge, set it to zero
    public float maxCharge = 0f;
    //represents if when this mod is used if it needs stamina to function
    // public bool usesStamina = false;
    //represents if there stamina is used in a burst or over time. 0 = over time everythign else = in burst
    // public float staminaBurst = 0;
    [Tooltip("Float in seconds")]
    public float cooldown;
    [Tooltip("0 for no max; nonzero for time in seconds")]
    public float maxHoldTime;

}
/// <summary>
/// These powerups are called from the fixed update of the ship mod controller
/// They are passive abilities that are constantly active
/// </summary>
public interface PassiveMod
{
    public abstract void OnPassive();

}

/// <summary>
/// This powerup is called by clicking a button 
/// it is a one for one ratio to clicking and having the 
/// event happen
/// </summary>
public interface ClickMod
{
    public abstract void OnClick();
}

/// <summary>
/// This powerup must have a button constantly held down to function
/// once held down it will have a constant effect until released
/// </summary>
public interface HoldMod
{
    public abstract void OnHold();
    public abstract void OnRelease(float charge);


}
