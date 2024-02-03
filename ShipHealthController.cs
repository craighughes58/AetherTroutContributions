/*****************************************************************************
// File Name : ShipHealthController.cs
// Author : Craig Hughes, Anna Breuker, Parker DeVenney, Caleb Kahn
// Creation Date : Ocotber 2, 2023
//
// Brief Description : This is the script that handles the ship's ability to
// take damage, restore itself, check for a loss, Optional velocity damage
// can be toggled on and off
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ShipHealthController : MonoBehaviour
{

    #region Private Variables
    // Instance reference
    public static ShipHealthController Instance;

    //represents if the player is using the solar sail
    //mitigates veloctiy damage
    private bool _usingSail = false;
    //reference to the rigidbody on the ship
    private Rigidbody _playerRB;
    //prevnts multiple collisions at once
    private bool _hasCollided = false;
    #region Serialized Variables

    [Header("HEALTH")]
    [Tooltip("The amount of health the player has currently")]
    [SerializeField] private int _health;
    [Tooltip("The maximum amount of health the player can have")]
    [SerializeField] private int _maxHealth;
    private int healthGoal;
    private float regenHealth;
    private bool inHealthBarCoroutine = false;
    [HideInInspector] public bool dead = false;

    [Header("VELOCITY DAMAGE")]

    [Tooltip("Toggles velocity damage on and off")]
    [SerializeField] private bool _hasVelocityDamage;

    [Tooltip("The speeds at which damage is increased")]
    [SerializeField] private List<float> _velocityThresholds;

    [Tooltip("The damage caused after each velocity threshold")]
    [SerializeField] private List<int> _velocityDamage;

    [Header("Healthbar")]
    public Image barImage;
    public RectTransform blackBar;
    public RectMask2D blackMask;
    public RectTransform whiteBar;
    public RectMask2D whiteMask;
    public AnimationCurve crackAnimationCurve;

    private Coroutine crackCoroutine;

    [Header("References")]
    // Reference to explosion particle effect
    public GameObject explosion;
    public GameObject shipModel;
    public Image deathFadeImage;
    public Image screenCrackImage;
    [SerializeField] AudioSource collisionSound;

    [SerializeField] UnityEvent DeathEvent;
    #endregion

    #endregion

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _health = _maxHealth;
        healthGoal = _maxHealth;
        SetHealtBar();
        _playerRB = GetComponent<Rigidbody>();
    }

    // Temp debugging tool to deal damage to ship
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            TakeDamage(150);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            GainHealth(20);
        }

        //if (_health < _maxHealth)
        //{
        //    //Debug.Log("Health Regening");
        //    RegenerateHealth(1);
        //}

    }

    #region Collisions

    /// <summary>
    /// This method handles the triggers coming from the health object
    /// whenever a health pickup is collided with teh ship then gains health equal to
    /// how much health is being held in the object
    /// </summary>
    /// <param name="other">The Object causing the trigger</param>
    private void OnTriggerEnter(Collider other)
    {
        //healthpack
        if (other.gameObject.tag.Equals("Health") && other.gameObject.GetComponent<PickupController>().GetPickupReady())
        {
            GainHealth(other.gameObject.GetComponent<PickupController>().GetRestorationAmount());
        }
    }

    /// <summary>
    /// This script handles two types of collision
    /// if vecolcity damage is turned on it will calculate how much damage to inflict based on how fast the ship is going
    /// if the player ran into a damaging object then it will call that script to calculate the damage output
    /// </summary>
    /// <param name="collision">The object the script is colliding with</param>
    private void OnCollisionEnter(Collision collision)
    {
        //velocity
        if(_hasVelocityDamage && !_hasCollided)
        {
            _hasCollided = true;
            HandleVelocityDamage(PlayerMovementController.Instance.currentThrust);
            collisionSound.Play();
        }
        //damage object
        if(collision.gameObject.GetComponent<DamageBehaviour>() != null)
        {
            //Debug.Log(_health);
            TakeDamage(collision.gameObject.GetComponent<DamageBehaviour>().CalculateDamage());
        }
        StartCoroutine(ResetCollision());
    }

    IEnumerator CrackCoroutine(int amount)
    {
        screenCrackImage.gameObject.SetActive(true);
        Color color = screenCrackImage.color;
        float maxAlpha = Mathf.Max(1f, amount / 15);

        float totalTime = Mathf.Sqrt(amount * 3);
        float timer = 0f;
        while (timer < totalTime)
        {
            color.a = crackAnimationCurve.Evaluate(timer / totalTime) * maxAlpha;
            screenCrackImage.color = color;
            yield return null;
            timer += Time.deltaTime;
        }

        screenCrackImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// called after a collision causes damage
    /// sets _hasCollided to false  so that the player can take damage again
    /// </summary>
    /// <returns>.01 seconds</returns>
    private IEnumerator ResetCollision()
    {
        _hasCollided = false;
        yield return new WaitForSeconds(.01f);
    }
    #endregion

    #region Damage Handler


    /// <summary>
    /// This script decreases health by the amount given to it
    /// if the amount reduces anything less than or equal to zero it 
    /// calls check death
    /// </summary>
    /// <param name="amount">The amount of damage being taken</param>
    public void TakeDamage(int amount)
    {
        healthGoal -= amount;
        CameraController.instance.ScreenShake(amount / 5f);
        RumbleManager.instance.rumblePulse(0.1f, 0.1f, 1f);

        if (crackCoroutine != null)
        {
            StopCoroutine(crackCoroutine);
        }
        crackCoroutine = StartCoroutine(CrackCoroutine(amount));

        if(healthGoal <= 0)
        {
            healthGoal = 0;
        }
        
        if (!dead && !inHealthBarCoroutine)
        {
            StartCoroutine(MoveHealth());
        }
    }

    public void RegenerateHealth(int healthIncreasePerSecond)
    {
        regenHealth += Time.deltaTime * healthIncreasePerSecond;
        if (regenHealth >= 1)
        {
            int floor = Mathf.FloorToInt(regenHealth);
            GainHealth(floor);
            regenHealth -= floor;
        }
    }

    /// <summary>
    /// This script increases health by the amount given to it
    /// if the amount + the current health is greater than max health it is set to max health
    /// </summary>
    /// <param name="amount">The amount of health being gained</param>
    public void GainHealth(int amount)
    {
        if (healthGoal + amount > _maxHealth)
        {
            healthGoal = _maxHealth;
        }
        else
        {
            healthGoal += amount;
        }

        if (!dead && _health != _maxHealth && !inHealthBarCoroutine)
        {
            StartCoroutine(MoveHealth());
        }
    }

    /// <summary>
    /// This function kills the ship instantly. Used for Level 2 crushers.
    /// </summary>
    public void InstantDeath()
    {
        HandleDeath();
    }

    IEnumerator MoveHealth()
    {
        inHealthBarCoroutine = true;
        SetHealtBar();
        yield return new WaitForSeconds(.25f);
        float timeDiff = 0f;
        while (healthGoal != _health)
        {
            timeDiff += Time.deltaTime * 30f;

            int diff = (int)timeDiff;
            timeDiff -= diff;
            if (_health > healthGoal)
            {
                _health = Mathf.Max(healthGoal, _health - diff);
            }
            else
            {
                _health = Mathf.Min(healthGoal, _health + diff);
            }
            SetHealtBar();

            if (_health <= 0)
            {
                _health = 0;
                StartCoroutine(HandleDeath());
                break;
            }
            yield return null;
        }
        inHealthBarCoroutine = false;
    }

    private void SetHealtBar()
    {
        //.89 = 668px / 750px
        //0.05466 = .89 / 2
        //637.5 = 750 * .85 scale

        if (healthGoal <= _health)
        {
            float amount = _health * 0.8906667f / _maxHealth + 0.0546667f;
            barImage.fillAmount = amount;
            blackBar.sizeDelta = new Vector2(164f, amount * 750f + 10f);
            whiteBar.sizeDelta = new Vector2(164f, amount * 750f + 10f);
            blackMask.padding = new Vector4(0f, amount * 637.5f - 10f, 0f, 0f);
            whiteMask.padding = new Vector4(0f, (healthGoal * 0.8906667f / _maxHealth + 0.0546667f) * 637.5f - 10f, 0f, 0f);
        }
        else
        {
            float amount = healthGoal * 0.8906667f / _maxHealth + 0.0546667f;
            barImage.fillAmount = amount;
            blackBar.sizeDelta = new Vector2(164f, amount * 750f + 10f);
            whiteBar.sizeDelta = new Vector2(164f, amount * 750f + 10f);
            blackMask.padding = new Vector4(0f, amount * 750f - 10f, 0f, 0f);
            whiteMask.padding = new Vector4(0f, (_health * 0.8906667f / _maxHealth + 0.0546667f) * 637.5f - 10f, 0f, 0f);
        }
    }

    /// <summary>
    /// This script handles the damage the player would take from velocity
    /// this goes through the list of velocity thresholds and then assigns damage 
    /// according to what threshold they passed
    /// if they would die from a collision and theyre above 1 health they go down to 1 health instead
    /// </summary>
    /// <param name="currentVelocity">The current velocity of the player</param>
    private void HandleVelocityDamage(float currentVelocity)
    {
        //the smallest velocity should always be at zero
        if(_velocityThresholds[0] > currentVelocity/* || _usingSail*/)
        {
            return;
        }
        //determine how fast the player went 
        for (int i = _velocityThresholds.Count - 1; i >= 0; i--)
        {
            if(currentVelocity >= _velocityThresholds[i])
            {
                //deal damage accordingly
                //if the player would be dealt damage that takes them to zero then it is instead set to 1
                if(_velocityDamage[i] >= _health)
                {
                    if(_health == 1)
                    {
                        TakeDamage(1);
                    }
                    else
                    {
                        TakeDamage(_health - 1);
                    }
                }
                else
                {
                    TakeDamage(_velocityDamage[i]);
                }
               
            }
        }
    }
    #endregion

    #region Death

    /// <summary>
    /// This script is called when health reaches zero
    /// it calls the save system to restart 
    /// </summary>
    public IEnumerator HandleDeath()
    {
        //Debug.Log("Explode!");

        // Instantiate explosion here...
        dead = true;
        _playerRB.constraints = RigidbodyConstraints.FreezeAll;
        Instantiate(explosion, transform.position, transform.rotation);
        shipModel.SetActive(false);
        _playerRB.velocity = Vector3.zero;
        DeathEvent?.Invoke();
        yield return new WaitForSecondsRealtime(1f);

        //Black Fade
        deathFadeImage.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < .33f)
        {
            deathFadeImage.color = new Color(0f, 0f, 0f, timer * 3f);
            yield return null;
            timer += Time.deltaTime;
        }
        deathFadeImage.color = Color.black;

        //Handle Death
        //SceneManager.LoadScene("HubAndTutorialScene"); // TEMPORARY UNTIL CHECKPOINTS ARE IN
        shipModel.SetActive(true);
        PlayerLoadData.Instance.LoadPlayerPosition();
        _playerRB.velocity = Vector3.zero;
        //SaveLoad.Instance.LoadPlayerPosition();
        healthGoal = _maxHealth;
        _health = _maxHealth;
        SetHealtBar();
        _playerRB.constraints = RigidbodyConstraints.None;
        if (crackCoroutine != null)
        {
            StopCoroutine(crackCoroutine);
            screenCrackImage.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(.1f);

        //Undo fade
        timer = .33f;
        while (timer > 0f)
        {
            deathFadeImage.color = new Color(0f, 0f, 0f, timer * 3f);
            yield return null;
            timer -= Time.deltaTime;
        }
        deathFadeImage.gameObject.SetActive(true);
        dead = false;
    }
    #endregion

    #region  Getters Setters

    public void SetUsingSail(bool val)
    {
        _usingSail = val;
    }

    public int GetHealth()
    {
        return _health;
    }

    #endregion

}
