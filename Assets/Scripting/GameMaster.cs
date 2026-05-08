using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================    */

        // links and stuff
        public static GameMaster Instance;
        
        [Header ("External Links")]
        [SerializeField] UIMaster _UiMasterScript; 

        // point count per player
        int _points1    =   0;              //  Player 1
        int _points2    =   0;              //  Player 2
        int pointsMax   =   3;              //  Max amount of points
            public (int p1, int p2) Getpoints() 
            {
                // Lets another method get the value stored in points
                return (_points1, _points2);
            }

        [Header ("Spawnpoints")]
        [SerializeField] Transform _spawnPointPlayer1;                          // contains in-game location of spawn-point (player 01)
        [SerializeField] Transform _spawnPointPlayer2;                          // contains in-game location of spawn point (player 02)


        [Header ("List of prefabs")]
        // prefab lists
        public List<GameObject> Player1Prefabs = new List<GameObject>();        // player_1 rock,paper,scissors prefabs from assets
        public List<GameObject> Player2Prefabs = new List<GameObject>();        // player_2 rock,paper,scissors prefabs from assets
        public List<Transform> ShootSpawnPoints = new List<Transform>();       //  transform information from the spawnpoints  
        public GameObject _shootObject;                                        //  Prefab to shooot stuff with
        

        GameObject _selectedPlayer1 = null;                 //  set to null when the method runs
        GameObject _selectedPlayer2 = null;                 //  Keeps it clean for re-runs of the method or later use
        GameObject _currentPlayerInstance1;                 //  instance of the spawned prefab
        GameObject _currentPlayerInstance2;                 //  instance of the spawned prefab   
        GameObject _currentShootInstance;                   //  Instance of the shoot object spawned

        // timer
        float _phase2Timer = 0f;
            public float GetTimer() 
            {
                // Lets another method get the value stored in points
                return (_phase2Timer);
            }
    
        // variables needed for the shoot mechanic in phase 3 of the gameplay loop
        [Header("Phase 3")]
        float _phase3TimerDelay = 50f;                                          //  time it takes to spawn                                      
        bool _isGunSpawning = false;
        bool _isPhase2Active = false;
        int _shootRandomIndex;

    /*  ========================================
                        UNITY VARIABLES
        ========================================    */
    void Awake()
    {
        // makes sure only 1 GameMaster runs at a time. useful because all information here is centralized and should not exist doubly.
        Instance = this;

    }

    void Start()
    {
        // makes sure that a list of prefabs is available at the start of the game
        if(Player1Prefabs == null) Player1Prefabs = new List<GameObject>();
        if(Player2Prefabs == null) Player2Prefabs = new List<GameObject>();

        // make sure that the prefab for the shoot is also available
        if(_shootObject == null) _shootObject = new GameObject();

        UIMaster.Instance.DisableUI_TimerText();
    }

    void Update()
    {
        Timer();
    }

    /*  ========================================
                        ROUND START
        ========================================    */


    /*  ========================================
                        PHASE 1
        ========================================    */

    public void PlayerToHandRNG()       //  launches on button press
    {
        // disable UI text element (looks weird if enabled)
        UIMaster.Instance.DisableUI_MatchReroll();

        // Creates a GameObject that will contain the randomly selected RPS (rock, paper, scissors) for each player
        _selectedPlayer1 = null;                  //  set to null when the method runs
        _selectedPlayer2 = null;                  //  Keeps it clean for re-runs of the method or later use

        // randomly select a prefab for player 01
        // NOTE TO SELF :   Prefabs have in the inspector, an option to toggle which type they are, but its also in the name
        if (Player1Prefabs.Count > 0)
        {
            int _randomIndex = Random.Range(0, Player1Prefabs.Count);
            _selectedPlayer1 = Player1Prefabs[_randomIndex];
        }
        else
        {
            Debug.LogWarning("GameMaster: No prefabs assigned to Player 1 in Inspector!");
            return;
        }    

        // randomly select a prefab for player 02
        if (Player2Prefabs.Count > 0)
        {
            int _randomIndex = Random.Range(0, Player2Prefabs.Count);
            _selectedPlayer2 = Player2Prefabs[_randomIndex];
        }
        else
        {
            Debug.LogWarning("GameMaster: No prefabs assigned to Player 2 in Inspector!");
            return;
        }

        // Get the playerBehavior component from the assigned player prefab so we can get its PlayerType and HandType information
        var _componentRef1 = _selectedPlayer1.GetComponent<PlayerBehavior>();
        var _componentRef2 = _selectedPlayer2.GetComponent<PlayerBehavior>(); 

         if (_componentRef1 == null || _componentRef2 == null)      // safety check
        {
            Debug.LogError("One of the prefabs is missing the PlayerBehavior script attached to it!");
            return;
        }

        // Get which hand the prefab has assigned from the component on each prefab
        PlayerBehavior.HandType _hand1 = _componentRef1.GetHand(); 
        PlayerBehavior.HandType _hand2 = _componentRef2.GetHand();
   
        if (_componentRef1 != null && _componentRef2 != null)   // quick safety check to see if they are not empty
        {
            Debug.Log($"Spawned P1={_hand1}, P2={_hand2}");
        }

        // Compare hands
        if (_hand1 == _hand2)
        {
            // enable relevant UI text bit under the button
            UIMaster.Instance.EnableUI_MatchReroll();
            return;     // stop executing the method
        }

        //  Writes the type of hand to the UI.
        UIMaster.Instance.WriteHandType(_hand1, _hand2);
  
        // Caulculate the winning hand using the RPS Winner method
        PlayerBehavior.HandType _winninghand = GetRPSWinner(_componentRef1.HandSelect, _componentRef2.HandSelect);
        // check if player one is the loser
        if      (_componentRef1.HandSelect != _winninghand)
        {
            _componentRef1.isLoser = true;

        } 
        // check if player 2 is the loser
        else if (_componentRef2.HandSelect != _winninghand)
        {
            _componentRef2.isLoser = true;
        }

        // after all calculation and setupn has been done, start the gameplay loop proper
        StartPhase2();

    }

    PlayerBehavior.HandType GetRPSWinner(PlayerBehavior.HandType _h1, PlayerBehavior.HandType _h2)
    {
        // Rock beats Scissors
        if (_h1 == PlayerBehavior.HandType.Rock && _h2 == PlayerBehavior.HandType.scizzor)      return _h1;
        else if (_h2 == PlayerBehavior.HandType.Rock && _h1 == PlayerBehavior.HandType.scizzor) return _h2;

        // Paper beats Rock  
        else if (_h1 == PlayerBehavior.HandType.Paper && _h2 == PlayerBehavior.HandType.Rock)   return _h1;
        else if (_h2 == PlayerBehavior.HandType.Paper && _h1 == PlayerBehavior.HandType.Rock)   return _h2;

        // Scissors beats Paper
        else if (_h1 == PlayerBehavior.HandType.scizzor && _h2 == PlayerBehavior.HandType.Paper) return _h1;
        else if (_h2 == PlayerBehavior.HandType.scizzor && _h1 == PlayerBehavior.HandType.Paper) return _h2;

        // If you reach here, it's impossible (tie was already prevented earlier)
        Debug.LogError("GetRPSWinner: Should never be called with tied hands");
        return _h1;
    }


    /*  ========================================
                        PHASE 2
        ========================================    */

    public void StartPhase2() {

        // first, spawn players
        SpawnPlayers(_selectedPlayer1, _selectedPlayer2);

        // set timer to 60
        _phase2Timer = 60f;

        // mark second phase as active
        _isPhase2Active = true; 

        // second, disable UI
        UIMaster.Instance.Disable_Phase1Parent();
        UIMaster.Instance.EnableUI_TimerText();
    }


    public void SpawnPlayers(GameObject p1Prefab, GameObject p2Prefab)
    {
    // --- Phase 1: Spawn Player 1 ---
        if (p1Prefab != null)
        {
            _currentPlayerInstance1 = Instantiate(p1Prefab); 

            // Snap to SpawnPointPlayer1 position and rotation
            if (_spawnPointPlayer1 != null)
            {
                _currentPlayerInstance1.transform.position = _spawnPointPlayer1.position; 
                _currentPlayerInstance1.transform.rotation = _spawnPointPlayer1.rotation; 
            }

            // Apply Rigidbody
            Rigidbody2D rb = _currentPlayerInstance1.GetComponent<Rigidbody2D>();
            if (rb == null) _currentPlayerInstance1.AddComponent<Rigidbody2D>();
        }

        // --- Phase 2: Spawn Player 2 ---
        if (p2Prefab != null)
        {
            _currentPlayerInstance2 = Instantiate(p2Prefab); 

            // Snap to SpawnPointPlayer2 position and rotation
            if (_spawnPointPlayer2 != null)
            {
                _currentPlayerInstance2.transform.position = _spawnPointPlayer2.position; 
                _currentPlayerInstance2.transform.rotation = _spawnPointPlayer2.rotation; 
            }

            // Apply Rigidbody
            Rigidbody2D rb = _currentPlayerInstance2.GetComponent<Rigidbody2D>();
            if (rb == null) _currentPlayerInstance2.AddComponent<Rigidbody2D>();
        }

        Debug.Log("Spawning Complete.");        
    }
    
    // timer method
    public void Timer()
    {
        // Safety Check: Only run timer if we are officially in Phase 2
        if (!_isPhase2Active) return;

        // set the current amount of seconds to the variable container
        _phase2Timer -= Time.deltaTime;

        // with a grace area of 2 seconds, spawn the shoot object when the timer has eclipsed the delay
        if (!_isGunSpawning && _phase2Timer <= (_phase3TimerDelay - 2f))
        {
            SpawnShoot();
        }

        if (_phase2Timer <= 0f)
        {
            EndOfRound();
        }        
    }

    

    /*  ========================================
                        PHASE 3
        ========================================    */

    // spawn the prefab that will let the losing hand fight back
    void SpawnShoot()
    {
        // Prevent spawning a second gun for this round
        if (_isGunSpawning) 
        {
            return; 
        }

        if (ShootSpawnPoints.Count == 0)
        {
            Debug.LogWarning("GameMaster: No shoot spawn points assigned!");
            _isGunSpawning = true; // Fail safely so it doesn't loop spamming warnings
            return;
        }

        _shootRandomIndex = Random.Range(0, ShootSpawnPoints.Count);

        _currentShootInstance = null;

        if (_shootObject != null)
        {
            // Instantiate at the random spawn point
            _currentShootInstance = Instantiate(_shootObject, 
                                                ShootSpawnPoints[_shootRandomIndex].position, 
                                                ShootSpawnPoints[_shootRandomIndex].rotation);
            
            Debug.Log("Shoot Object Spawned.");
            
            // Set flag to true so it only spawns once this round
            _isGunSpawning = true; 
            
            // Note: You need to attach behavior to _currentShootInstance here later
            // For now, we just spawned the container/gun object.

        }
        else
        {
            Debug.LogWarning("GameMaster : No shoot object prefab assigned in inspector");
        }
    }

public void TransformPlayer(PlayerBehavior.PlayerType dyingPlayerType)
{
    GameObject targetObj = null;
    
    // 1. Identify which specific object is losing
    if (dyingPlayerType == PlayerBehavior.PlayerType.Player01)
    {
        targetObj = _currentPlayerInstance1;
    }
    else if (dyingPlayerType == PlayerBehavior.PlayerType.Player02) 
    {
        targetObj = _currentPlayerInstance2;
    }

    // 2. Safety check before destroy
    if (targetObj != null)
    {
        // Get position and data from OLD player
        Vector3 deathPos = targetObj.transform.position;
        
        // CRITICAL: Copy control scheme FROM old player TO new gun object
        var oldBehavior = targetObj.GetComponent<PlayerBehavior>();
        
        Destroy(targetObj); 

        // 3. Instantiate shoot object at the location of the destroyed player
        _currentShootInstance = Instantiate(_shootObject, deathPos, Quaternion.identity);
        
        // 4. Attach PlayerBehavior component to preserve data
        var newBehavior = _currentShootInstance.GetComponent<PlayerBehavior>();
        if (newBehavior == null) 
        {
            newBehavior = _currentShootInstance.AddComponent<PlayerBehavior>();
        }

        // Copy the control scheme from the old player!
        newBehavior.PlayerSelect = dyingPlayerType;  // WASD vs Arrows
        newBehavior.HandSelect = oldBehavior.HandSelect; // Keep RPS hand type if you want
    }
}


    /*  ========================================
                        ROUND END
        ========================================    */
    public void EndOfRound()
    {

        // check if the max amount of points has been reaches
        AreWeDoneYet();
        Cleanup();
        
        // update points UI
        UIMaster.Instance.UpdatePoints();
        UIMaster.Instance.DisableUI_TimerText();

        //re-launch UI
        UIMaster.Instance.EnableUI_Phase1Parent();
    }    

    // add a point to player 1
    public void AddPointToPlayer01()
    {
        _points1    ++;
        Debug.Log ($"player one now has {_points1} point(s)");

    }

    // add a point to player 2
    public void AddPointToPlayer02()
    {
        _points2    ++;
        Debug.Log ($"player two now has {_points2} point(s)");
    }

    // Clean up various data after the roud has been completed to set it to a base state
void Cleanup()
{
    if (_currentPlayerInstance1 != null) Destroy(_currentPlayerInstance1);
    if (_currentPlayerInstance2 != null) Destroy(_currentPlayerInstance2);
    
    // CRITICAL: Destroy the transformed gun object too
    if (_currentShootInstance != null) Destroy(_currentShootInstance);

    // Clear references used in PlayerToHandRNG to avoid memory leaks or stale logic
    _selectedPlayer1 = null; 
    _selectedPlayer2 = null;
    _currentPlayerInstance1 = null;  // Set these to null after destroy too
    _currentPlayerInstance2 = null;
    _currentShootInstance = null;

    _isGunSpawning = false;
    _shootRandomIndex = 0;

    // set timer back to 0
    _phase2Timer = 0f;

    // mark second phase being active as false
    _isPhase2Active = false;
}


    /*  ========================================
                        GAME END
        ========================================    */

    // check if the game win condition has been reached
    void AreWeDoneYet()
    {
        if (_points1 >= pointsMax)
        {
            Debug.Log ("GameOver, Player1 has won");
            SceneManager.LoadScene("ScreenGameOver");

        }

        if (_points2 >= pointsMax)
        {
            Debug.Log ("GameOver, Player2 has won");
            SceneManager.LoadScene("ScreenGameOver");
        }
    }     
}
