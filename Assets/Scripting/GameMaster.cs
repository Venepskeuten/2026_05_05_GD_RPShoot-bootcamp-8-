using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================
        GameMaster is the central brain of the game. All game state lives here:
        scores, spawned player instances, the timer, and phase tracking.
        Other scripts (PlayerBehavior, UIMaster) call back into GameMaster
        rather than managing state themselves, keeping all logic centralized.
        ========================================    */

        // Singleton reference — allows any script to reach GameMaster via GameMaster.Instance
        // without needing a direct inspector link. Every script in the project uses this.
        public static GameMaster Instance;
        
        [Header ("External Links")]
        [SerializeField] UIMaster _UiMasterScript;      // Direct inspector link to UIMaster (backup reference, Instance is preferred)

        // ---- SCORING ----
        // Points are tracked here and never on the player objects themselves.
        // PlayerBehavior and ProjectileBehavior call AddPointToPlayer01/02 to increment these.
        int _points1    =   0;              // Running point total for Player 1
        int _points2    =   0;              // Running point total for Player 2
        int pointsMax   =   3;              // First to this many points wins the match

            // Read-only accessor for scores.
            // UIMaster.UpdatePoints() calls this to display current scores without being
            // able to accidentally modify them.
            public (int p1, int p2) Getpoints() 
            {
                return (_points1, _points2);
            }

        // ---- SPAWN POINTS ----
        // Set in the Inspector. These are empty GameObjects placed in the scene
        // whose Transform positions are used as player spawn locations each round.
        [Header ("Spawnpoints")]
        [SerializeField] Transform _spawnPointPlayer1;      // Where Player 1 appears at the start of each round
        [SerializeField] Transform _spawnPointPlayer2;      // Where Player 2 appears at the start of each round


        // ---- PREFAB LISTS ----
        // These lists are populated in the Inspector with the Rock/Paper/Scissors prefabs
        // for each player. A random one is picked each round by PlayerToHandRNG().
        // IMPORTANT: Never modify components on these directly at runtime — they are asset
        // references, not scene instances. Modifying them writes to the file on disk.
        [Header ("List of prefabs")]
        public List<GameObject> Player1Prefabs = new List<GameObject>();    // P1's Rock, Paper, Scissors prefabs
        public List<GameObject> Player2Prefabs = new List<GameObject>();    // P2's Rock, Paper, Scissors prefabs
        public List<Transform> ShootSpawnPoints = new List<Transform>();    // Possible spawn locations for the Weapon pickup
        public GameObject _shootObject;                                     // The Weapon/gun prefab the loser transforms into

        // ---- RUNTIME INSTANCE REFERENCES ----
        // These hold references to the actual cloned objects currently in the scene.
        // They are set during SpawnPlayers() and cleared in Cleanup() at round end.
        GameObject _selectedPlayer1 = null;         // The prefab asset randomly chosen for P1 this round
        GameObject _selectedPlayer2 = null;         // The prefab asset randomly chosen for P2 this round
        GameObject _currentPlayerInstance1;         // The live P1 clone currently in the scene
        GameObject _currentPlayerInstance2;         // The live P2 clone currently in the scene
        GameObject _currentShootInstance;           // The live Weapon/gun clone currently in the scene

        // ---- TIMER ----
        // Counts down during Phase 2. Drives the Phase 3 weapon spawn and round timeout.
        float _phase2Timer = 0f;

            // Read-only accessor for the timer value.
            // UIMaster.UpdateTimer() polls this every frame to display the countdown.
            public float GetTimer() 
            {
                return (_phase2Timer);
            }
    
        // ---- PHASE 3 STATE ----
        [Header("Phase 3")]
        float _phase3TimerDelay = 43f;      // How many seconds into Phase 2 the Weapon pickup spawns (60 - 43 = at the 17s mark)
        bool _isGunSpawning     = false;    // Prevents the weapon from spawning more than once per round
        bool _isPhase2Active    = false;    // Gate flag — Timer() and SpawnShoot() only run while this is true
        int _shootRandomIndex;              // Stores which spawn point index was randomly selected for the weapon


    /*  ========================================
                        UNITY METHODS
        ========================================
        Standard Unity lifecycle methods. Awake sets up the singleton.
        Start validates inspector assignments. Update drives the timer each frame.
        ========================================    */

    void Awake()
    {
        // Register this object as the singleton so other scripts can reach it via GameMaster.Instance.
        // Only one GameMaster should ever exist — having two would cause conflicting game state.
        Instance = this;
    }

    void Start()
    {
        // 1. Ensure prefab lists exist even if the inspector was left empty
        if(Player1Prefabs == null) Player1Prefabs = new List<GameObject>();
        if(Player2Prefabs == null) Player2Prefabs = new List<GameObject>();

        // 2. Fallback: if the shoot prefab was forgotten in the inspector, create a blank object
        //    so the rest of the code doesn't null-crash. A warning will appear at spawn time.
        if(_shootObject == null) _shootObject = new GameObject();

        // 3. Hide the timer UI at game start — it only shows during Phase 2
        //    Linked to UIMaster.DisableUI_TimerText() which calls SetActive(false) on the timer object.
        UIMaster.Instance.DisableUI_TimerText();
    }

    void Update()
    {
        // Tick the round timer every frame.
        // Timer() is self-gating — it does nothing if _isPhase2Active is false.
        Timer();
    }


    /*  ========================================
                        PHASE 1
        ========================================
        Phase 1 is the hand-selection screen. The player presses a UI button which
        calls PlayerToHandRNG(). This randomly assigns Rock/Paper/Scissors to each
        player, resolves who wins, and triggers Phase 2 if a clear winner exists.
        The loser flag is set on the SPAWNED INSTANCES (not the prefab assets)
        inside StartPhase2(), after Instantiate() has already run.
        ========================================    */

    // Entry point for starting a round. Called by a UI Button's OnClick event.
    public void PlayerToHandRNG()
    {
        // 1. Hide the "same hand, re-roll" message from any previous attempt
        //    Linked to UIMaster — this controls the MatchReroll text object's visibility.
        UIMaster.Instance.DisableUI_MatchReroll();

        // 2. Clear any previously selected prefabs to avoid carrying stale data into a new round
        _selectedPlayer1 = null;
        _selectedPlayer2 = null;

        // 3. Randomly pick a prefab from P1's list (Rock, Paper, or Scissors)
        //    Each prefab has its HandType set in the Inspector via PlayerBehavior.HandSelect.
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

        // 4. Same random selection for P2
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

        // 5. Read the HandType from each selected prefab via PlayerBehavior.GetHand().
        //    NOTE: We are only READING from the prefab here, never writing.
        //    Writing to _componentRef here would corrupt the prefab asset on disk.
        var _componentRef1 = _selectedPlayer1.GetComponent<PlayerBehavior>();
        var _componentRef2 = _selectedPlayer2.GetComponent<PlayerBehavior>(); 

        if (_componentRef1 == null || _componentRef2 == null)
        {
            Debug.LogError("One of the prefabs is missing the PlayerBehavior script!");
            return;
        }

        PlayerBehavior.HandType _hand1 = _componentRef1.GetHand(); 
        PlayerBehavior.HandType _hand2 = _componentRef2.GetHand();
   
        Debug.Log($"Selected hands — P1={_hand1}, P2={_hand2}");

        // 6. If both players rolled the same hand, show the re-roll prompt and abort.
        //    The player must press the button again. Linked to UIMaster.EnableUI_MatchReroll().
        if (_hand1 == _hand2)
        {
            UIMaster.Instance.EnableUI_MatchReroll();
            return;
        }

        // 7. Display the chosen hand names in the Phase 1 UI.
        //    Linked to UIMaster.WriteHandType() which updates the two player hand label texts.
        UIMaster.Instance.WriteHandType(_hand1, _hand2);
  
        // 8. Determine the winning hand using the RPS ruleset
        PlayerBehavior.HandType _winninghand = GetRPSWinner(_componentRef1.HandSelect, _componentRef2.HandSelect);

        // 9. Proceed to Phase 2, passing the winning hand so the loser can be flagged
        //    on the spawned instances (not on the prefab assets).
        StartPhase2(_winninghand);
    }

    // Pure logic helper — takes two HandTypes and returns which one wins per RPS rules.
    // Called only by PlayerToHandRNG(). Ties are already prevented before this is called.
    PlayerBehavior.HandType GetRPSWinner(PlayerBehavior.HandType _h1, PlayerBehavior.HandType _h2)
    {
        // Rock beats Scissors
        if      (_h1 == PlayerBehavior.HandType.Rock    && _h2 == PlayerBehavior.HandType.scizzor) return _h1;
        else if (_h2 == PlayerBehavior.HandType.Rock    && _h1 == PlayerBehavior.HandType.scizzor) return _h2;

        // Paper beats Rock  
        else if (_h1 == PlayerBehavior.HandType.Paper   && _h2 == PlayerBehavior.HandType.Rock)   return _h1;
        else if (_h2 == PlayerBehavior.HandType.Paper   && _h1 == PlayerBehavior.HandType.Rock)   return _h2;

        // Scissors beats Paper
        else if (_h1 == PlayerBehavior.HandType.scizzor && _h2 == PlayerBehavior.HandType.Paper)  return _h1;
        else if (_h2 == PlayerBehavior.HandType.scizzor && _h1 == PlayerBehavior.HandType.Paper)  return _h2;

        Debug.LogError("GetRPSWinner: reached unreachable code — tie should have been caught earlier.");
        return _h1;
    }


    /*  ========================================
                        PHASE 2
        ========================================
        Phase 2 is the active gameplay phase. Players move around the arena
        trying to collide with each other. The winner is decided by their RPS hand.
        A timer counts down; when it expires the round ends with no point awarded.
        The weapon pickup spawns partway through this phase, triggering Phase 3.
        ========================================    */

    // Transitions the game from the hand-selection screen into live gameplay.
    // Accepts the winning hand so it can mark the loser on the live instance,
    // not on the prefab asset (which would corrupt it permanently).
    public void StartPhase2(PlayerBehavior.HandType winningHand)
    {
        // 1. Instantiate both player prefabs into the scene at their spawn points.
        //    After this call, _currentPlayerInstance1 and _currentPlayerInstance2 are live objects.
        SpawnPlayers(_selectedPlayer1, _selectedPlayer2);

        // 2. NOW it is safe to write to PlayerBehavior — these are instances, not prefab assets.
        //    Mark whichever instance has the losing hand. PlayerBehavior.isLoser is read later
        //    in PlayerBehavior.OnCollisionEnter to decide if the player should transform.
        var instance1Behavior = _currentPlayerInstance1.GetComponent<PlayerBehavior>();
        var instance2Behavior = _currentPlayerInstance2.GetComponent<PlayerBehavior>();

        if (instance1Behavior.HandSelect != winningHand) instance1Behavior.isLoser = true;
        else if (instance2Behavior.HandSelect != winningHand) instance2Behavior.isLoser = true;

        // 3. Set the countdown timer. Timer() in Update() will now begin counting down.
        _phase2Timer = 60f;

        // 4. Open the gate flag so Timer() and SpawnShoot() are allowed to run
        _isPhase2Active = true;

        // 5. Swap the UI from Phase 1 (button screen) to Phase 2 (timer display).
        //    Linked to UIMaster — hides the hand-select panel, shows the countdown.
        UIMaster.Instance.Disable_Phase1Parent();
        UIMaster.Instance.EnableUI_TimerText();
    }

    // Spawns both player prefabs as new scene instances and places them at their spawn points.
    // Called by StartPhase2(). Populates _currentPlayerInstance1 and _currentPlayerInstance2.
    public void SpawnPlayers(GameObject p1Prefab, GameObject p2Prefab)
    {
        // 1. Instantiate P1 and move the clone to its designated spawn point
        if (p1Prefab != null)
        {
            _currentPlayerInstance1 = Instantiate(p1Prefab); 

            if (_spawnPointPlayer1 != null)
            {
                _currentPlayerInstance1.transform.position = _spawnPointPlayer1.position; 
                _currentPlayerInstance1.transform.rotation = _spawnPointPlayer1.rotation; 
            }

            // Ensure a Rigidbody exists for physics-based movement (MovePosition / MoveRotation)
            Rigidbody rb = _currentPlayerInstance1.GetComponent<Rigidbody>();
            if (rb == null) _currentPlayerInstance1.AddComponent<Rigidbody>();
        }

        // 2. Same process for P2
        if (p2Prefab != null)
        {
            _currentPlayerInstance2 = Instantiate(p2Prefab); 

            if (_spawnPointPlayer2 != null)
            {
                _currentPlayerInstance2.transform.position = _spawnPointPlayer2.position; 
                _currentPlayerInstance2.transform.rotation = _spawnPointPlayer2.rotation; 
            }

            Rigidbody rb = _currentPlayerInstance2.GetComponent<Rigidbody>();
            if (rb == null) _currentPlayerInstance2.AddComponent<Rigidbody>();
        }

        Debug.Log("SpawnPlayers: Both players instantiated.");        
    }
    
    // Called every frame from Update(). Counts down the Phase 2 timer and triggers
    // downstream phase transitions. Does nothing unless _isPhase2Active is true.
    public void Timer()
    {
        // Gate: only run during Phase 2
        if (!_isPhase2Active) return;

        // Decrement the timer in real time
        _phase2Timer -= Time.deltaTime;

        // Trigger the weapon pickup spawn once the timer falls below the delay threshold.
        // _isGunSpawning prevents this from firing more than once per round.
        if (!_isGunSpawning && _phase2Timer <= (_phase3TimerDelay - 2f))
        {
            SpawnShoot();
        }

        // If the timer runs out with no winner, end the round with no point awarded
        if (_phase2Timer <= 0f)
        {
            EndOfRound();
        }        
    }


    /*  ========================================
                        PHASE 3
        ========================================
        Phase 3 activates when the weapon pickup spawns in the arena.
        The losing player can touch this pickup to transform into a shooter object,
        then fire projectiles at the winning player for a chance to steal the round.
        TransformPlayer() is called by PlayerBehavior.OnCollisionEnter when the
        loser touches the Weapon-tagged object.
        ========================================    */

    // Instantiates the Weapon pickup at a random spawn point in the arena.
    // Called once per round by Timer() when the countdown crosses the delay threshold.
    void SpawnShoot()
    {
        // 1. Guard: if already spawned this round, do nothing
        if (_isGunSpawning) return;

        // 2. Guard: if no spawn points were assigned in the inspector, warn and bail
        if (ShootSpawnPoints.Count == 0)
        {
            Debug.LogWarning("GameMaster: No shoot spawn points assigned!");
            _isGunSpawning = true;  // Set true to prevent repeated warnings every frame
            return;
        }

        // 3. Pick a random spawn point from the list
        _shootRandomIndex = Random.Range(0, ShootSpawnPoints.Count);
        _currentShootInstance = null;

        // 4. Instantiate the weapon pickup at the chosen spawn point
        if (_shootObject != null)
        {
            _currentShootInstance = Instantiate(
                _shootObject, 
                ShootSpawnPoints[_shootRandomIndex].position, 
                ShootSpawnPoints[_shootRandomIndex].rotation
            );
            
            Debug.Log("SpawnShoot: Weapon pickup spawned.");

            // 5. Lock the flag so this method cannot fire again this round
            _isGunSpawning = true; 
        }
        else
        {
            Debug.LogWarning("GameMaster: _shootObject prefab not assigned in inspector.");
        }
    }

    // Destroys the losing player's current prefab and replaces it with the shoot object.
    // Called by PlayerBehavior.OnCollisionEnter when the loser touches the Weapon pickup.
    // The new object inherits the player's control scheme and is flagged as able to shoot.
    public void TransformPlayer(PlayerBehavior.PlayerType dyingPlayerType)
    {
        GameObject targetObj = null;
        
        // 1. Resolve which scene instance belongs to the dying player
        if      (dyingPlayerType == PlayerBehavior.PlayerType.Player01) targetObj = _currentPlayerInstance1;
        else if (dyingPlayerType == PlayerBehavior.PlayerType.Player02) targetObj = _currentPlayerInstance2;

        // 2. Safety check — if the instance is already gone, abort to avoid null errors
        if (targetObj == null) return;

        // 3. Capture data from the old object BEFORE destroying it
        Vector3 deathPos    = targetObj.transform.position;         // Spawn the new object here
        var oldBehavior     = targetObj.GetComponent<PlayerBehavior>();

        // 4. Destroy the old RPS player object — it no longer exists in the scene
        Destroy(targetObj);

        // 5. Instantiate the shoot/gun prefab at the position the old player occupied
        _currentShootInstance = Instantiate(_shootObject, deathPos, Quaternion.identity);
            
        // 6. Get or add PlayerBehavior on the new object so it can move and shoot.
        //    The shoot prefab may or may not already have PlayerBehavior attached.
        var newBehavior = _currentShootInstance.GetComponent<PlayerBehavior>();
        if (newBehavior == null) newBehavior = _currentShootInstance.AddComponent<PlayerBehavior>();

        // 7. Transfer the projectile prefab reference from the old player.
        //    Without this, Shoot() in PlayerBehavior would find _projectilePrefab null and abort.
        if (oldBehavior._projectilePrefab != null)
        {
            newBehavior._projectilePrefab = oldBehavior._projectilePrefab;
        }

        // 8. Copy the player identity and hand type so controls and RPS data carry over.
        //    PlayerSelect determines whether WASD or Arrow keys are used (see PlayerBehavior.MovePlayers).
        newBehavior.PlayerSelect    = dyingPlayerType;
        newBehavior.HandSelect      = oldBehavior.HandSelect;
        newBehavior.shooterType     = dyingPlayerType;  // Used by ProjectileBehavior to credit the correct player

        // 9. Enable shooting on the new object — this flag is checked in PlayerBehavior.Update()
        newBehavior.canShoot = true;
            
        // 10. Set up the Rigidbody directly on the GameObject (not through newBehavior._rb,
        //     which won't be assigned until Start() runs next frame).
        //     Constraints match the player setup: no Y movement, no X/Z rotation tipping.
        Rigidbody rb = _currentShootInstance.GetComponent<Rigidbody>();
        if (rb == null) rb = _currentShootInstance.AddComponent<Rigidbody>();
        rb.useGravity   = false;
        rb.constraints  = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        // 11. Manually inject the Rigidbody reference so PlayerBehavior can use it immediately
        //     without waiting for its own Start() to run on the next frame.
        newBehavior._rb = rb;
    }


    /*  ========================================
                        ROUND END
        ========================================
        EndOfRound() is the single exit point for all round-ending events:
        - A player collision win (called from PlayerBehavior.WaitWhichPlayerWereYouAgain)
        - A projectile hit (called from ProjectileBehavior.OnCollisionEnter)
        - The timer running out (called from Timer())
        It checks for a match winner, cleans up the scene, and returns to Phase 1.
        ========================================    */

    // Central round-end handler. Called from PlayerBehavior, ProjectileBehavior, and Timer().
    // Points should already be added before this is called — this method only finalizes the round.
    public void EndOfRound()
    {
        // 1. Check if a player has hit the point limit — loads the game over scene if so.
        //    This runs BEFORE Cleanup so scores are still valid when checked.
        AreWeDoneYet();

        // 2. Destroy all scene objects from this round and reset all state flags
        Cleanup();
        
        // 3. Refresh the score display with the latest point values.
        //    Linked to UIMaster.UpdatePoints() which reads from GameMaster.Getpoints().
        UIMaster.Instance.UpdatePoints();

        // 4. Hide the Phase 2 timer UI
        UIMaster.Instance.DisableUI_TimerText();

        // 5. Show the Phase 1 button screen so a new round can begin
        UIMaster.Instance.EnableUI_Phase1Parent();
    }    

    // Increments P1's score by 1.
    // Called by PlayerBehavior.WaitWhichPlayerWereYouAgain() or ProjectileBehavior.OnCollisionEnter()
    // when a win condition for P1 is confirmed.
    public void AddPointToPlayer01()
    {
        _points1++;
        Debug.Log($"AddPointToPlayer01: P1 now has {_points1} point(s)");
    }

    // Increments P2's score by 1.
    // Called by PlayerBehavior.WaitWhichPlayerWereYouAgain() or ProjectileBehavior.OnCollisionEnter()
    // when a win condition for P2 is confirmed.
    public void AddPointToPlayer02()
    {
        _points2++;
        Debug.Log($"AddPointToPlayer02: P2 now has {_points2} point(s)");
    }

    // Destroys all live scene objects from the current round and resets every state variable
    // back to its default so the next round starts from a clean slate.
    // Called at the end of every EndOfRound().
    void Cleanup()
    {
        // 1. Destroy any projectiles still flying through the scene.
        //    Projectiles are tagged "Projectile" in PlayerBehavior.Shoot() — this sweeps all of them.
        foreach (GameObject proj in GameObject.FindGameObjectsWithTag("Projectile"))
        {
            Destroy(proj);
        }

        // 2. Destroy the player instances and the weapon/gun object if they still exist
        if (_currentPlayerInstance1 != null) Destroy(_currentPlayerInstance1);
        if (_currentPlayerInstance2 != null) Destroy(_currentPlayerInstance2);
        if (_currentShootInstance   != null) Destroy(_currentShootInstance);

        // 3. Null all instance references so nothing else can touch destroyed objects
        _selectedPlayer1        = null;
        _selectedPlayer2        = null;
        _currentPlayerInstance1 = null;
        _currentPlayerInstance2 = null;
        _currentShootInstance   = null;

        // 4. Reset all phase state flags and the timer
        _isGunSpawning  = false;    // Allows the weapon to spawn again next round
        _shootRandomIndex = 0;
        _phase2Timer    = 0f;
        _isPhase2Active = false;    // Stops Timer() from running between rounds
    }


    /*  ========================================
                        GAME END
        ========================================
        Called at the start of every EndOfRound(). If a player's score has reached
        the maximum, the game over scene is loaded immediately.
        ========================================    */

    // Checks both scores against pointsMax and loads the game over scene if either player has won.
    // Runs before Cleanup() in EndOfRound() so scores are still valid when evaluated.
    void AreWeDoneYet()
    {
        if (_points1 >= pointsMax)
        {
            Debug.Log("AreWeDoneYet: Player 1 has won the match.");
            SceneManager.LoadScene("ScreenGameOver");
            return;
        }

        if (_points2 >= pointsMax)
        {
            Debug.Log("AreWeDoneYet: Player 2 has won the match.");
            SceneManager.LoadScene("ScreenGameOver");
            return;
        }
    }     
}