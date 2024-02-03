/*****************************************************************************
// File Name : AxolotlBehaviour.cs
// Author : Craig Hughes
// Creation Date : November 30, 2023
// Brief Description : This script controls how the axolotl moves between
// nodes as well as the collision
//there will be different movement styles and collision styles
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AxolotlBehaviour : MonoBehaviour
{

    #region Private Variables
    //Reference to the rigidbody on the object
    private Rigidbody _rb;

    //Represents if the player caught the axolotl
    private bool _isCaught = false;

    //represents if the axolotl is going to different
    private bool _isReversed = false;

    //represents if the movement is paused or not
    private bool _pausedMovement = false;

    //the list index of the current 
    private int _currentEndPosition = 0;


    #region Serialized Variables

    [Tooltip("The object in the mouth of the axolotl")]
    [SerializeField] private Transform _heldObject;

    [Tooltip("The positions in the world that the axolotl will move towards")]
    [SerializeField] private Transform[] _positionNodes;

    [Header("Movement Information")]

    [Tooltip("How fast the axolotl Moves")]
    [SerializeField] private float _speed;

    [Header("Movement Styles")]
    [Tooltip("The axolotl will move from first node to last then back to first")]
    [SerializeField] private bool _circlingMovement;

    [Tooltip("The axolotl will move from the first to last node then go in reverse from last to first")]
    [SerializeField] private bool _BackandForthMovement;

    [Tooltip("The axolotl will move to random points")]
    [SerializeField] private bool _randomMovement;

    [Header("Shrinking Information")]

    [Tooltip("How much the axolotl shrinks in every interval")]
    [SerializeField] private float _shrinkAmount;

    [Tooltip("The time between shrinks")]
    [SerializeField] private float _shrinkIntervalsTime;

    [Tooltip("The scale the axolotl is shrinking to")]
    [SerializeField] private float _shrinkTarget;

    [Tooltip("Event that is called when the axolotl is caught")]
    [SerializeField] UnityEvent onCatch;

    [SerializeField] UnityEvent onShrink;

    [SerializeField] UnityEvent Blep;
    #endregion
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    
    // Update is called once per fixed frame
    private void FixedUpdate()
    {
        MoveAxolotl();
    }

    #region Movement

    /// <summary>
    /// Called every fixed update
    /// moves the axolotl towards the
    /// </summary>
    private void MoveAxolotl()
    {
        if(_currentEndPosition < 0 || _currentEndPosition > _positionNodes.Length -1 || _isCaught || _pausedMovement)
        {
            return;
        }
        //Move the axolotl
        _rb.MovePosition(Vector3.MoveTowards(transform.position, _positionNodes[_currentEndPosition].position, _speed));
        //rotate axolotl
        transform.LookAt(_positionNodes[_currentEndPosition]);
    }

    private IEnumerator PauseMovement()
    {
        _pausedMovement = true;
        yield return new WaitForSeconds(5f);
        _pausedMovement = false;
    }
    #endregion

    #region Collisions and Triggers

    /// <summary>
    /// if the axolotl triggers with a node it will then determine the next node
    /// </summary>
    /// <param name="other">The object that is being triggered with</param>
    private void OnTriggerEnter(Collider other)
    {
        switch (other.gameObject.tag)
        {
            case "AxolotlNode":
                DetermineNextNode();
                break;
            case "RepulseGun":
                StartCoroutine(PauseMovement());
                break;
        }

    }

    /// <summary>
    /// if the axolotl collides with the player it will activate get caught
    /// </summary>
    /// <param name="collision">The object that is being collided with</param>
    private void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Player":
                GetCaught();
                break;
        }

    }

    #endregion

    #region Determining Next End Point

    /// <summary>
    /// Called when the axolotl collides with a node 
    /// determines what movement style is being used 
    /// then calls the iterator for that movement style
    /// </summary>
    private void DetermineNextNode()
    {
        if (_circlingMovement)
        {
            IterateCircleMovement();
        }
        else if (_BackandForthMovement)
        {
            IterateBackAndForthMovement();
        }
        else if (_randomMovement)
        {
            IterateRandomMovement();
        }
    }

    /// <summary>
    /// Called from all iteration types except random
    /// if it's reversed it removes one from the index
    /// otherwise it adds one to the index
    /// </summary>
    private void IterateCurrentEndPosition()
    {
        if (_isReversed)
        {
            _currentEndPosition--;
        }
        else
        {
            _currentEndPosition++;
        }
    }

    /// <summary>
    /// called from Determine Next Node
    /// Iterates the current end position
    /// if the axolotl is at the end it will go back to the beginning
    /// if it's at the end it will go back to the start
    /// </summary>
    private void IterateCircleMovement()
    {
        IterateCurrentEndPosition();
        if(_currentEndPosition < 0)
        {
            _currentEndPosition = _positionNodes.Length - 2;
        }
        if(_currentEndPosition >= _positionNodes.Length)
        {
            _currentEndPosition = 0;
        }
        print(_currentEndPosition);
    }

    /// <summary>
    /// called from Determine Next Node
    /// Iterates the current end position
    /// if the axolotl is at the end or the beginning, it will reverse its movement 
    /// </summary>
    private void IterateBackAndForthMovement()
    {
        IterateCurrentEndPosition();
        if(_currentEndPosition >= _positionNodes.Length)
        {
            _isReversed = true;
            _currentEndPosition = _positionNodes.Length - 2;
        }
        else if(_currentEndPosition < 0)
        {
            _isReversed = false;
            _currentEndPosition = 1;
        }
    }

    /// <summary>
    /// called from Determine Next Node
    /// Gives the end position index a random point in the nodes
    /// </summary>
    private void IterateRandomMovement()
    {
        int previousPosition = _currentEndPosition;
        while(_currentEndPosition == previousPosition)
        {
            _currentEndPosition = Random.Range(0, _positionNodes.Length);
        }
    }

    #endregion


    #region Feedback Methods
    /// <summary>
    ///Called when the axolotl is caught 
    ///shrinks the axolotl a set a mount N times until 
    ///it reaches its target scale
    /// </summary>
    private IEnumerator Shrink()
    {
        while(transform.localScale.x > _shrinkTarget)
        {
            transform.localScale -= new Vector3(_shrinkAmount, _shrinkAmount, _shrinkAmount);
            yield return new WaitForSeconds(_shrinkIntervalsTime);
        }
        onShrink.Invoke();
        yield return null;
    }

    /// <summary>
    /// Called when the player catches the axolotl
    /// it will set it to caught, change its pose, make the sound that signifies 
    /// it being caught, then shrink
    /// </summary>
    private void GetCaught()
    {
        _isCaught = true;
        onCatch?.Invoke();

        //remove held object
        if (_heldObject != null)
        {
            _heldObject.parent = null;
        }
        //change pose
        //make noise happen
        //shrink
        StartCoroutine(Shrink());
    }
    #endregion

    #region Getters and Setters

    /// <summary>
    /// called from external and internal scripts
    /// sets all movement types to false and then determines what movement style
    /// to turn back on based on the string passed through
    /// </summary>
    /// <param name="movementChange">the name of the new movement type wanted</param>
    public void SetMovementStyle(string movementChange)
    {
        _circlingMovement = false;
        _BackandForthMovement = false;
        _randomMovement = false;
        switch (movementChange)
        {
            case "circle":
                _circlingMovement = true;
                break;
            case "backandforth":
                _BackandForthMovement = true;
                break;
            case "random":
                _randomMovement = true;
                break;
        }
    }

    public bool HasBeenCaught()
    {
        return _isCaught;
    }
    #endregion
}
