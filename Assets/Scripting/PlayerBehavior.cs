using UnityEngine;
using UnityEngine.Events;

public class PlayerBehavior : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================    */
    // external links
    GameMaster                  _gameMasterScript;

    //  Allows an object to be assigned to which player it belongs.
    public enum PlayerType      {Player01, Player02}            // create options   
    public PlayerType           PlayerSelect;                   // display options on script
    
    //  Allows an object to be assigned wether it is : rock - paper - scizzor
    public enum HandType        {Rock, Paper, scizzor}          // contains hands
    public HandType             HandSelect;                     // displays/stores options on script in unity editor
        // Method that lets me access this information in another script.
        public HandType GetHand() {
            return this.HandSelect;
        }


    // Player stats
    float                       _PlayerMoveSpeed    =   5f;     // speed of the players movement.

    public bool                 isLoser             = false;                        // Has ze player lozed
    public bool                 canTransform        = false;

    // physics and collision
    Rigidbody2D                 _rb;                            // Physics interactions

    // Events

    /*  ========================================
                        UNITY METHODS
        ========================================    */

    void Awake()
    {
        
    }
    
    void Start()
    {
        _rb                 = GetComponent<Rigidbody2D>();                      // make sure that the physics are applied on the object(s)
        // NOTE TO SELF :   We dont call a game object, because Transform already touches the data of the object the script is on.
    }

    void Update()
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
        Vector3 _direction = Vector3.zero;      // empty container for to-be pressed direction

        // move vertical
        if (Input.GetKey(KeyCode.W)) {          // move up
            // transforms the player direction upwards by movement speed when W is pressed.
            _direction = Vector3.forward;
        }

        else if (Input.GetKey(KeyCode.S)) {      // move down
            // transforms the player direction upwards by movement speed when W is pressed.
            _direction = Vector3.back;
        }

        // move horizontal
        else if (Input.GetKey(KeyCode.A)) {      // move left
            // transforms the player direction upwards by movement speed when W is pressed.
            _direction = Vector3.left;
        }
        
        else if (Input.GetKey(KeyCode.D)) {      // move right
            // transforms the player direction upwards by movement speed when W is pressed.
            _direction = Vector3.right;
        }

        // moves the player based on said direction and movement speed
        transform.Translate(_direction * _PlayerMoveSpeed * Time.deltaTime);
    }  

    void ControlsArrowKeys()
    {
        // just read WASD arrow comments if you wanna know what this does. I aint writing that twice.
        Vector3 _direction = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow)) {          // move up
            _direction = Vector3.forward;
        }

        else if (Input.GetKey(KeyCode.DownArrow)) {      // move down
            _direction = Vector3.back;
        }

        else if (Input.GetKey(KeyCode.LeftArrow)) {      // move left
            _direction = Vector3.left;
        }
        
        else if (Input.GetKey(KeyCode.RightArrow)) {      // move right
            _direction = Vector3.right;
        }

        transform.Translate(_direction * _PlayerMoveSpeed * Time.deltaTime);
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
            Debug.Log($"[{PlayerSelect}] collided with Shoot - IsLoser: {isLoser}");

            // If the player is marked as the losing hand (by GameMaster)        
            if (isLoser == true)
            {
                // Pass 'this.PlayerSelect' so GameMaster knows WHICH player is dying
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
}   

