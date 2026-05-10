using UnityEngine;
using UnityEngine.Events;

public class PlayerBehavior : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================    */
    

    // external links
    [Header("Player data")]
    GameMaster                  _gameMasterScript;

    //  Allows an object to be assigned to which player it belongs.
    public enum PlayerType      {Player01, Player02}            // create options   
    public PlayerType           PlayerSelect;                   // display options on script
    public PlayerType           shooterType;                    // stores who fired a projectile

    //  Allows an object to be assigned wether it is : rock - paper - scizzor
    public enum HandType        {Rock, Paper, scizzor}          // contains hands
    public HandType             HandSelect;                     // displays/stores options on script in unity editor
        // Method that lets me access this information in another script.
        public HandType GetHand() {
            return this.HandSelect;
        }


    // Player stats
    float                       _PlayerMoveSpeed            =   5f;             // speed of the players movement.


    public bool                 isLoser             = false;                        // Has ze player lozed
    public bool                 canTransform        = false;
    public bool                 canShoot            = false;
    public bool hasTransformed = false;

    // physics and collision
    public Rigidbody                 _rb;                            // Physics interactions


    [Header("Projectile")]
    public GameObject           _projectilePrefab;              // Assign in Inspector
    public float                _projectileSpeed = 10f;         // Speed of projectile


    // Events

    /*  ========================================
                        UNITY METHODS
        ========================================    */

    void Awake()
    {
        
    }
    
    void Start()
    {
        _rb                 = GetComponent<Rigidbody>();                      // make sure that the physics are applied on the object(s)
        // NOTE TO SELF :   We dont call a game object, because Transform already touches the data of the object the script is on.
    }

    void Update()
    {

        // if the player has been marked as being able to shoot and presses the space button
        if (canShoot && Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }   
    }

    void FixedUpdate()
    {
        // constantly keeps track of player inputs
        MovePlayers();            
    }     
    /*  ========================================
                        PLAYER MOVEMENT
        ========================================    */

    void MovePlayers()
    {
        // checks for player 1 and controls movements
        if (PlayerSelect == PlayerType.Player01)
        {            
            // checks for WASD inputs and moves the player accordingly
            ControlsWASDKeys();

            // NOTE TO SELF :   I could do the logic of controlWASDkeys in here, but I feel like i could keep it more modular if i ever wanted to add more keybind possibilities or switch them around or whatever.
        } 
        // checks for player 2 and controls movements
        else if (PlayerSelect == PlayerType.Player02) 
        {
            // checks for arrow key inputs and moves the player accordingly
            ControlsArrowKeys();
        }
    }      

    void ControlsWASDKeys()
    {
        Vector3 _direction = Vector3.zero;
        Quaternion _targetRotation = _rb.rotation; // default: don't rotate

        if      (Input.GetKey(KeyCode.W)) { _direction = Vector3.forward;  _targetRotation = Quaternion.Euler(0, 0, 0);   }
        else if (Input.GetKey(KeyCode.S)) { _direction = Vector3.back;     _targetRotation = Quaternion.Euler(0, 180, 0); }
        else if (Input.GetKey(KeyCode.A)) { _direction = Vector3.left;     _targetRotation = Quaternion.Euler(0, 270, 0); }
        else if (Input.GetKey(KeyCode.D)) { _direction = Vector3.right;    _targetRotation = Quaternion.Euler(0, 90, 0);  }

        if (_direction != Vector3.zero)
        {
            _rb.MovePosition(_rb.position + _direction * _PlayerMoveSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(_targetRotation);
        }
    }

    void ControlsArrowKeys()
    {
        Vector3 _direction = Vector3.zero;
        Quaternion _targetRotation = _rb.rotation; // default: don't rotate

        if      (Input.GetKey(KeyCode.UpArrow))     { _direction = Vector3.forward;  _targetRotation = Quaternion.Euler(0, 0, 0);   }
        else if (Input.GetKey(KeyCode.DownArrow))   { _direction = Vector3.back;     _targetRotation = Quaternion.Euler(0, 180, 0); }
        else if (Input.GetKey(KeyCode.RightArrow))  { _direction = Vector3.left;     _targetRotation = Quaternion.Euler(0, 270, 0); }
        else if (Input.GetKey(KeyCode.LeftArrow))   { _direction = Vector3.right;    _targetRotation = Quaternion.Euler(0, 90, 0);  }

        if (_direction != Vector3.zero)
        {
            _rb.MovePosition(_rb.position + _direction * _PlayerMoveSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(_targetRotation);
        }
    }

    /*  ========================================
                        PLAYER colission
        ========================================    */


    void OnCollisionEnter(Collision collision)
    {

        // find other player and get information from the other PlayerBehavior script
        var _otherPlayer = collision.gameObject.GetComponent<PlayerBehavior>();

        // check if the player collides with the shoot object.
        if (collision.gameObject.CompareTag("Weapon"))
        {
            if (isLoser && !hasTransformed)
            {
                hasTransformed = true;
                Destroy(collision.gameObject); // destroy the pickup
                GameMaster.Instance.TransformPlayer(PlayerSelect);
            }
        }

        // only compare after hitting the other player. Basically a safety check
        if (_otherPlayer != null)
        {
            // compare hand types to see which one wins.
            CompareHand(this.HandSelect, _otherPlayer.HandSelect);            
        }
        
    }

    void CompareHand(HandType _myHand, HandType _otherHand)
    {
        Debug.Log ($"Other players hand = {_otherHand}");

        // rock > scizzors
        if (_myHand == HandType.Rock)
        {
            // win condition
            if (_otherHand == HandType.scizzor)
            {
                Debug.Log ("Rock has won round");
                WaitWhichPlayerWereYouAgain();
            }
        }

        // scizzors > paper
        if (_myHand == HandType.scizzor)
        {
            // win condition
            if (_otherHand == HandType.Paper)
            {
                Debug.Log ("Scissors has won round");
                WaitWhichPlayerWereYouAgain();
            } 
        }

        // paper > rock
        if (_myHand == HandType.Paper)
        {
            // win condition
            if (_otherHand == HandType.Rock)
            {
                Debug.Log ("Paper has won round");
                WaitWhichPlayerWereYouAgain();
            } 
        }
    }

    void WaitWhichPlayerWereYouAgain()      // the result of me being an idiot. Bruteforcing shit.
    {   
        // Does a double check which player you are and based on that, gives a point to either player 01 or player 02
        
        if (PlayerSelect == PlayerType.Player01)
        {
            GameMaster.Instance.AddPointToPlayer01();
        }

        else if (PlayerSelect == PlayerType.Player02)
        {
            GameMaster.Instance.AddPointToPlayer02();
        }

        // runs back to gamemaster and ends the round
        GameMaster.Instance.EndOfRound();
    }

    /*  ========================================
                        SHOOTING MECHANICS
        ========================================    */    

    void Shoot()
    {
        // 1. Safety Check: Ensure Prefab exists
        if (_projectilePrefab == null) 
        {
            Debug.LogWarning("[Shoot] Projectile Prefab not assigned in Inspector!");
            return;
        }

        // 2. Get current facing direction (standard for 3D shooters)
        Vector3 shootDir = transform.forward; 
        
        // 3. Instantiate projectile at player position with matching rotation
        GameObject projObj = Instantiate(_projectilePrefab, transform.position, transform.rotation);
        
        // 4. Get Rigidbody component from the new object
        Rigidbody rbProj = projObj.GetComponent<Rigidbody>();

        // 5. Add Rigidbody if missing (ensure it can move)
        if (rbProj == null) 
        {
            rbProj = projObj.AddComponent<Rigidbody>();
            
            // CRITICAL: Use 'useGravity' for 3D, NOT gravityScale
            rbProj.useGravity = false; // Disables falling (projectile flies straight)
            
            // Ensure we don't have kinematic constraints preventing movement
            rbProj.isKinematic = false;
        }

        // 6. Apply velocity manually (bypasses standard physics forces)
        rbProj.linearVelocity = shootDir * _projectileSpeed;

        projObj.tag = "Projectile";

        // Stamp shooter identity onto the projectile
        var projBehavior = projObj.GetComponent<ProjectileBehavior>();
        if (projBehavior == null) projBehavior = projObj.AddComponent<ProjectileBehavior>();
        projBehavior.shooterType = this.PlayerSelect;

        // Ignore collision between projectile and the player who fired it
        Physics.IgnoreCollision(projObj.GetComponent<Collider>(), GetComponent<Collider>());
    }


}   

