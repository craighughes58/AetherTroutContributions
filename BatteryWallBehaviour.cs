/*****************************************************************************
// File Name : BatteryWallBehaviour.cs
// Author : Craig Hughes
// Creation Date : November 9, 2023
// Brief Description : This Script holds the functionality for recieving
// laser inputs and opening/closing a door  
*****************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BatteryWallBehaviour : BatteryReactiveBehaviour
{

    #region Private Variables
    //reference to the rigidbody on the moving part of the wall
    private List<Rigidbody> _doorRigidBodies = new List<Rigidbody>();
    //Where the door moves when it's not reacting
    private List<Vector3> _startingPositions = new List<Vector3>();

    //Represents if the wall is opening or closing
    private bool _isActive = false;

    #region Serialized Variables

    [Header("POSITIONS AND SPEED")]
    [Tooltip("How fast the door moves between positions")]
    [SerializeField] private float _speed;

    [Tooltip("Where the door moves when it's reacting")]
    [SerializeField] private List<Vector3> _endPositions;

    [Header("DOOR INFO")]
    [Tooltip("The moving parts of the wall")]
    [SerializeField] private List<GameObject> _doorRef;
    [Tooltip("Do we want to delete the doors")]
    [SerializeField] private bool destroyDoorsOnActive;

    [Header("MATERIALS INFO")]
    [Tooltip("The material of the object when it is not being reacted with by the laser")]
    [SerializeField] private Material _offMaterial;
    [Tooltip("The material of the object when it is being reacted with by the laser")]
    [SerializeField] private Material _onMaterial;
    [Tooltip("The material of the door when it is not being reacted with by the laser")]
    [SerializeField] private Material _doorOffMaterial;
    [Tooltip("The material of the door when it is being reacted with by the laser")]
    [SerializeField] private Material _doorOnMaterial;
    #endregion
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<MeshRenderer>().material = _offMaterial;
        foreach(GameObject door in _doorRef)
        {
            _doorRigidBodies.Add(door.GetComponent<Rigidbody>());
            _startingPositions.Add(door.transform.position);
        }
    }


    /// <summary>
    /// Called when the laser hits the object
    /// starts opening the door
    /// </summary>
    public override void ReactToBattery()
    {
        _isActive = true;
        GetComponent<MeshRenderer>().material = _onMaterial;
        if(destroyDoorsOnActive)
        {
            DestroyDoors();
        }
        else
        {
            StartCoroutine(OpenDoor());
        }
        
        
    }
    /// <summary>
    /// Called when the laser exits hitting the object
    /// starts closing the door
    /// </summary>
    public override void EndReaction()
    {
        _isActive = false;
        GetComponent<MeshRenderer>().material = _offMaterial;
        StartCoroutine(CloseDoor());
    }

    /// <summary>
    /// Using the rigid body it will move every object in the list towards its end point
    /// </summary>
    /// <returns>The time in between the door moving more</returns>
    private IEnumerator OpenDoor()
    {
        for (int i = 0; i < _doorRigidBodies.Count; i++)
        {
            if (_doorRigidBodies[i].gameObject.name.Contains("Door_Spike"))
            {
                Material[] mats = _doorRigidBodies[i].gameObject.GetComponent<MeshRenderer>().materials;
                mats[1] = _doorOnMaterial;
                _doorRigidBodies[i].gameObject.GetComponent<MeshRenderer>().materials = mats;
            }
        }

        while (_isActive)
        {
            for(int i  = 0; i < _doorRigidBodies.Count ; i++)
            {
                _doorRigidBodies[i].MovePosition(Vector3.MoveTowards(_doorRef[i].transform.position, _endPositions[i], _speed));
            }
            yield return new WaitForSeconds(.05f);
        }
    }

    /// <summary>
    /// Using the rigid body it will move every object in the list towards its starting point
    /// </summary>
    /// <returns>The time in between moving the door more</returns>
    private IEnumerator CloseDoor()
    {
        for (int i = 0; i < _doorRigidBodies.Count; i++)
        {
            if (_doorRigidBodies[i].gameObject.name.Contains("Door_Spike"))
            {
                Material[] mats = _doorRigidBodies[i].gameObject.GetComponent<MeshRenderer>().materials;
                mats[1] = _doorOffMaterial;
                _doorRigidBodies[i].gameObject.GetComponent<MeshRenderer>().materials = mats;
            }
        }

        while (!_isActive)
        {
            for (int i = 0; i < _doorRigidBodies.Count; i++)
            {
                if (_doorRigidBodies[i].gameObject.name.Contains("Door_Spike"))
                {
                    _doorRigidBodies[i].gameObject.GetComponent<MeshRenderer>().materials[1] = _doorOffMaterial;
                }
                _doorRigidBodies[i].MovePosition(Vector3.MoveTowards(_doorRef[i].transform.position, _startingPositions[i], _speed));
            }
            yield return new WaitForSeconds(.05f);
        }
    }

    /// <summary>
    /// Instead of opening the doors, destroy them
    /// </summary>
    private void DestroyDoors()
    {
        if(_doorRef.Count == 0)
        {
            return;
        }

        for(int i = 0; i < _doorRef.Count; ++i)
        {
            Destroy(_doorRef[i]);
        }
    }
}
