/*****************************************************************************
// File Name : CrusherJawBehaviour.cs
// Author : Craig Hughes
// Creation Date : January 21, 2023
// Brief Description : This is the moving object of the crusher that keeps
// track of what it is colliding with.
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class CrusherJawBehaviour : MonoBehaviour
{

    #region private variables
    private List<GameObject> _collisions = new List<GameObject>();
    #endregion

    #region
    [SerializeField] UnityEvent CollisionDetected;
    [SerializeField] UnityEvent WalLDetected;
    [SerializeField] UnityEvent RetractionDetected;
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        
    }


    #region

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Jaw"))
        {
            WalLDetected.Invoke();
            return;
        }
        else if(collision.gameObject.name.Contains("Retractor"))
        {
            RetractionDetected.Invoke();
        }
        //add if not already in
        else if(!_collisions.Contains(collision.gameObject))
        {
            _collisions.Add(collision.gameObject);
            CollisionDetected.Invoke();
        }
        
    }

    private void OnCollisionExit(Collision collision)
    {
        //remove if in
        if (_collisions.Contains(collision.gameObject))
        {
            _collisions.Remove(collision.gameObject);
        }
    }

    #endregion

    #region Getters and Setters

    public List<GameObject> GetCollisions()
    {
        return _collisions;
    }
    #endregion


}
