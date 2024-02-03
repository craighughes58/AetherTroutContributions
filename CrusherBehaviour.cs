/*****************************************************************************
// File Name : CrusherBehaviour.cs
// Author : Craig Hughes
// Creation Date : January 21, 2023
// Brief Description :This script manages the jaws of the crusher and destroys
// objects being touched by all jaw objects. This will also move all jaws
// towards the start and end point
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrusherBehaviour : MonoBehaviour
{

    #region Private Variables
    //if the array is moving in or out
    private bool _isCrushing = false;
    //represents if the two are together or retracting
    private bool _hasCrushed = false;
    //
    private bool _hasActivatedCoroutine = false;
    //how many times the two jaws have collided in a row
    private int _wallCollisions = 0;
    //
    private bool _paused = true;

    private bool _isShaking = false;

    #endregion

    #region Serialized Variables

    [Tooltip("The time before the crusher starts crushing")]
    [SerializeField] private float _startDelayTime;

    [Tooltip("The names of the objects that can't be destroyed by the crusher")]
    [SerializeField] private List<string> _exceptionNames;

    [Header("Jaws")]
    [Tooltip("MAX 2 The moving objects that crushes external objects")]
    [SerializeField] private List<GameObject> _jaws;

    [Tooltip("How fast all the jaws moves to Crush")]
    [SerializeField] private float _crushingSpeed;

    [Tooltip("How fast all the jaws moves to retract")]
    [SerializeField] private float _retractionSpeed;

    [Tooltip("the time in between changing states")]
    [SerializeField] private float _delayTime;

    [Tooltip("The safe positon of each jaw")]
    [SerializeField] private List<Vector3> _startPositions;

    [Tooltip("the crushing position of each jaw")]
    [SerializeField] private List<Vector3> _endPositions;


    [Tooltip("The position of the reatractors")]
    [SerializeField] private List<Transform> _retractors;

    [Header("Visuals")]

    [Tooltip("The object in between the crusher that shows up before it hits")]
    [SerializeField] private GameObject _VisualCommunication;

    [Tooltip("")]
    [SerializeField] private float _shakeAmt;

    #endregion
    private void Start()
    {
        StartCoroutine(DelayCrush());
        _VisualCommunication.SetActive(false);

    }

    private IEnumerator DelayCrush()
    {
        yield return new WaitForSeconds(_startDelayTime + _delayTime);
        _paused = false;
    }

    #region Movement
    private void FixedUpdate()
    {
        if (_paused)
        {
            return;
        }
        //Moving In
        if (_isCrushing)
        {
            for(int i = 0; i < _jaws.Count; i++)
            {
                //_jaws[i].transform.localPosition = Vector3.MoveTowards(transform.localPosition, _endPositions[i], _crushingSpeed);_endPositions[i]
                _jaws[i].GetComponent<Rigidbody>().MovePosition(Vector3.MoveTowards(_jaws[i].transform.position, _endPositions[i], _crushingSpeed));
            }
        }
        //Moving Out
        else
        {
            for (int i = 0; i < _jaws.Count; i++)
            {
                // _jaws[i].transform.localPosition = Vector3.MoveTowards(transform.localPosition, _startPositions[i], _retractionSpeed); 
                _jaws[i].GetComponent<Rigidbody>().MovePosition(Vector3.MoveTowards(_jaws[i].transform.position, _retractors[i].position, _retractionSpeed));
            }
        }
    }

    public void ChangeDirection()
    {
        _wallCollisions++;
        if(_wallCollisions >= 2)
        {
            _hasCrushed = true;
            if (!_hasActivatedCoroutine)
            {
                StartCoroutine(DelayChangeDirection());
                _hasActivatedCoroutine = true;
            }
            _wallCollisions = 0;
        }
    }

    private IEnumerator DelayChangeDirection()
    {
        _VisualCommunication.SetActive(!_VisualCommunication.activeInHierarchy);
        if(!_isCrushing)
        {
            StartCoroutine(StartShakingWalls());
        }
        yield return new WaitForSeconds(_delayTime);
        _isShaking = false;
        _isCrushing = !_isCrushing;
        if(_isCrushing)
        {
            _hasCrushed = false;
        }
        _hasActivatedCoroutine = false;
    }

    private IEnumerator StartShakingWalls()
    {
        yield return new WaitForSeconds(_delayTime/2f);
        _isShaking = true;
        Vector3 firstPos;
        while (_isShaking)
        {
            foreach(GameObject g in _jaws)
            {
                firstPos = g.transform.position;
                g.transform.position = g.transform.position + (Random.insideUnitSphere * _shakeAmt);
                yield return new WaitForSeconds(.01f);
                g.transform.position = firstPos;
            }
        }
        _isShaking = false;

    }
    #endregion

    #region Destruction

    public void CompareJaws()
    {
        List<GameObject> jaw1Collisions = _jaws[0].GetComponent<CrusherJawBehaviour>().GetCollisions();
        List<GameObject> jaw2Collisions = _jaws[1].GetComponent<CrusherJawBehaviour>().GetCollisions();

        List<GameObject> collectiveCollisions = new List<GameObject>();
        CheckPlayer(jaw1Collisions,jaw2Collisions);
        //find duplicates
        foreach(GameObject G in jaw1Collisions)
        {
            if (jaw2Collisions.Contains(G) && canDestroy(G.name))
            {
                collectiveCollisions.Add(G);
            }
        }
        //remove
        for(int i = 0; i < collectiveCollisions.Count; i++)
        {

            jaw1Collisions.Remove(collectiveCollisions[i]);
            jaw2Collisions.Remove(collectiveCollisions[i]);
            //destroy
            Destroy(collectiveCollisions[i]);
        }
    }


    private bool canDestroy(string name)
    {
        if(_exceptionNames.Contains(name))
        {
            return false;
        }
        return true;
    }

    private void CheckPlayer(List<GameObject> jaw1Collisions, List<GameObject> jaw2Collisions)
    {
        if(_hasCrushed)
        {
            return;
        }
        if(jaw1Collisions.Count > 0 && jaw1Collisions[jaw1Collisions.Count -1].name.Contains("Player") || jaw2Collisions.Count > 0 && jaw2Collisions[jaw2Collisions.Count - 1].name.Contains("Player"))
        {
            ShipHealthController.Instance.InstantDeath();
        }

    }

    #endregion
}
