/*****************************************************************************
// File Name : LaserBatteryBehaviour.cs
// Author : Craig Hughes, Lucas Johnson
// Creation Date : November 11, 2023
// Brief Description : This script holds the functionality of the laser that 
// chases the player and freezes if hit by a laser
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LaserBatteryBehaviour : MonoBehaviour
{

    #region Serialized variables
    [Tooltip("How fast the laser rotates")]
    [SerializeField]
    private float _rotationSpeed = 30f;

    [Tooltip("The range of the raycast")]
    [SerializeField]
    private float _raycastRange;

    [Tooltip("The layer that the laser will ignore")]
    [SerializeField]
    private LayerMask mask;

    [Header("DAMAGE")]
    [Tooltip("How much damage the player is dealt")]
    [SerializeField] private int _damageToPlayer;
    [Tooltip("How frequently  the player is damaged")]
    [SerializeField] private float _damageInterval;

    [Header("BEAM VISUALS")]
    [Tooltip("The material for when the beam is rotating")]
    [SerializeField] private Material beamSearchingVisual;
    [Tooltip("The material for when the beam is locked on target")]
    [SerializeField] private Material beamLockedOnVisual;

    #endregion

    #region Private Variables

    private bool _isRotating = false;

    public Vector3[] _possibleRotations = new Vector3[4];
    public Vector3 _currentPossibleRotation;
    public int _currentPossibleRotationIndex = 0;

    //reference to the object's line renderer
    private LineRenderer _lineRender;

    //reference to the reactor the beam is hitting so you can turn it off
    private BatteryReactiveBehaviour _reactor;
    private bool _reactorCalled = false;

    //prevents the player from being damaged multiple times
    private bool _isDamaging = false;

    #endregion

    [SerializeField] UnityEvent laserHitPlayer;
    [SerializeField] UnityEvent laserHitBattery;

    // Start is called before the first frame update
    void Start()
    {
        _lineRender = GetComponent<LineRenderer>();
        _lineRender.SetPosition(0, transform.forward * 13f + transform.position);
        _lineRender.SetPosition(1, transform.forward * _raycastRange + transform.position);

        GetPossibleRotations();

        _currentPossibleRotation = _possibleRotations[1];
        _currentPossibleRotationIndex = 1;
    }

    private void GetPossibleRotations()
    {
        GameObject refCube = Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), transform.position + transform.right, Quaternion.identity);

        // Forward
        _possibleRotations[0] = transform.localRotation.eulerAngles;

        // Right
        transform.LookAt(refCube.transform, transform.up);
        _possibleRotations[1] = transform.localRotation.eulerAngles;
        transform.rotation = Quaternion.Euler(_possibleRotations[0]);

        // Back
        refCube.transform.position = transform.position + (-transform.forward * 10);
        transform.LookAt(refCube.transform, transform.up);
        _possibleRotations[2] = transform.localRotation.eulerAngles;
        transform.rotation = Quaternion.Euler(_possibleRotations[0]);

        // Left
        refCube.transform.position = transform.position + (-transform.right * 10);
        transform.LookAt(refCube.transform, transform.up);
        _possibleRotations[3] = transform.localRotation.eulerAngles;
        transform.rotation = Quaternion.Euler(_possibleRotations[0]); 

        Destroy(refCube); // Remove Cube
    }

    /// <summary>
    /// if the player is within range rotate to face it
    /// otherwise turn off the laser pointer
    /// </summary>
    void Update()
    {
        _lineRender.SetPosition(0, transform.forward * 13f + transform.position);
        // Handle Rotation
        if (_isRotating)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(_currentPossibleRotation), _rotationSpeed * Time.deltaTime);
            _lineRender.SetPosition(1, transform.forward * _raycastRange + transform.position);

            if (transform.rotation == Quaternion.Euler(_currentPossibleRotation))
            {
                SetNewTargeRotation();
                _isRotating = false;
            }
        }

        // Handle Raycast Interaction
        RaycastHit Hit;
        if (Physics.Raycast(transform.position, transform.forward, out Hit, _raycastRange, mask))//this section checks if the raycast hits a collider and if the collider is the player
        {
            _lineRender.SetPosition(1, Hit.point);
            
            if (Hit.collider.tag.Equals("Player"))//collider is player
            {
                StartCoroutine(DealDamageToPlayer());
            }
            else if (Hit.collider.gameObject.GetComponent<BatteryReactiveBehaviour>() && !_reactorCalled)
            {
                _reactor = Hit.collider.GetComponent<BatteryReactiveBehaviour>();
                // if the thing the beam hits is a mirror, make sure the input beam is set for it
                if (_reactor.gameObject.GetComponent<MirrorCrystal>() != null)
                {
                    _reactor.gameObject.GetComponent<MirrorCrystal>()._inputBeamObject = gameObject;
                }
                _lineRender.material = beamLockedOnVisual;
                _reactor.ReactToBattery();
                laserHitBattery.Invoke();
                _reactorCalled = true;
                _lineRender.SetPosition(1, Hit.point);
            }

            if (!Hit.collider.tag.Equals("Player"))
            {
                StopCoroutine(DealDamageToPlayer());
                _isDamaging = false;
                _lineRender.SetPosition(1, Hit.point);
            }

            if(_reactor && !Hit.collider.gameObject.GetComponent<BatteryReactiveBehaviour>())
            {
                _reactor.EndReaction();
                _lineRender.material = beamSearchingVisual;
                _reactor = null;
                _reactorCalled = false;
            }

            return;
        }
        
        if(_reactor)
        {
            _reactor.EndReaction();
            _lineRender.material = beamSearchingVisual;
            _reactor = null;
            _reactorCalled = false;
        }

        if (_isDamaging)
        {
            StopCoroutine(DealDamageToPlayer());
            _isDamaging = false;
            _lineRender.SetPosition(1, transform.forward * _raycastRange + transform.position);
        }
    }

    private void SetNewTargeRotation()
    {
        if (_currentPossibleRotationIndex < _possibleRotations.Length - 1)
        {
            _currentPossibleRotationIndex++;
        }
        else 
        { 
            _currentPossibleRotationIndex = 0;
        }

        _currentPossibleRotation = _possibleRotations[_currentPossibleRotationIndex];
    }


    /// <summary>
    /// When a pair of lasers hits the battery
    /// check if it's cooled off and can restart 
    /// </summary>
    /// <param name="other">The object colliding with this object</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Laser"))
        {
            _isRotating = true;
        }
    }


    #region Damage
    private IEnumerator DealDamageToPlayer()
    {
        if(_isDamaging)
        {
            yield break;
        }

        _isDamaging = true;
        while (_isDamaging)
        {
            laserHitPlayer.Invoke();
            ShipHealthController.Instance.TakeDamage(_damageToPlayer);
            if(ShipHealthController.Instance.GetHealth() <= 0)
            {
                _isDamaging = false;
                break;
            }
            yield return new WaitForSeconds(_damageInterval);
        }
        _isDamaging = false;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position,  transform.forward * _raycastRange);
        Gizmos.DrawRay(transform.position, -transform.forward * _raycastRange);
        Gizmos.DrawRay(transform.position,  transform.right   * _raycastRange);
        Gizmos.DrawRay(transform.position, -transform.right   * _raycastRange);
    }
}
