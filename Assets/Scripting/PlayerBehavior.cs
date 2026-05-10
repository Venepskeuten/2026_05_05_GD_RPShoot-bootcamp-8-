using UnityEngine;
using UnityEngine.Events;

public class PlayerBehavior : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================
        PlayerBehavior lives on every player object in the scene — both the
        Rock/Paper/Scissors prefabs and the gun/shoot object the loser transforms into.
        It handles movement, collision responses, and shooting.

        GameMaster.SpawnPlayers() instantiates the RPS prefabs which have this script.
        GameMaster.TransformPlayer() instantiates the gun object and manually configures
        this script's fields (PlayerSelect, canShoot, _rb etc.) before Start() runs.
        ========================================    */

    // ---- EXTERNAL LINKS ----
    [Header("Player data")]
    GameMaster _gameMasterScript;   // Kept for potential direct inspector link; GameMaster.Instance is used in practice

    // ---- PLAYER IDENTITY ----
    // PlayerType determines which keybinds this object responds to (WASD or Arrows).
    // Set in the Inspector on each prefab, and re-assigned by GameMaster.TransformPlayer()
    // when the player transforms into the gun object so controls carry over.
    public enum PlayerType  { Player01, Player02 }
    public PlayerType       PlayerSelect;       // Which player controls this object
    public PlayerType       shooterType;        // Stored on the gun object — read by ProjectileBehavior to credit the right player

    // ---- HAND TYPE ----
    // Each RPS prefab has its HandType set in the Inspector.
    // GameMaster.PlayerToHandRNG() reads this via GetHand() to resolve the winner
    // WITHOUT writing anything back to the prefab (read-only use).
    public enum HandType    { Rock, Paper, scizzor }
    public HandType         HandSelect;         // This object's RPS hand — set in Inspector per prefab

        // Read-only accessor so GameMaster can get HandSelect without accessing it directly.
        // Used in PlayerToHandRNG() to compare hands before spawning into the scene.
        public HandType GetHand() {
            return this.HandSelect;
        }

    // ---- MOVEMENT ----
    float _PlayerMoveSpeed = 5f;    // Units per second. Applies to both WASD and Arrow key players.

    // ---- STATE FLAGS ----
    // isLoser is set by GameMaster.StartPhase2() on the INSTANCE (never the prefab).
    // It gates whether touching the Weapon pickup triggers a transformation.
    public bool isLoser         = false;

    // canTransform: reserved for future use
    public bool canTransform    = false;

    // canShoot: set to true by GameMaster.TransformPlayer() after the loser transforms.
    // Checked every frame in Update() — only then does Space fire a projectile.
    public bool canShoot        = false;

    // hasTransformed: prevents OnCollisionEnter from calling TransformPlayer() more than once
    // if multiple collision events fire in the same frame (common in Unity physics).
    public bool hasTransformed  = false;

    // ---- PHYSICS ----
    // _rb is assigned in Start(). GameMaster.TransformPlayer() also injects it directly
    // when creating the gun object, so movement works immediately without waiting for Start().
    public Rigidbody _rb;

    // ---- PROJECTILE ----
    // _projectilePrefab is set in the Inspector on each RPS prefab and carried over
    // to the gun object by GameMaster.TransformPlayer() via oldBehavior._projectilePrefab.
    [Header("Projectile")]
    public GameObject   _projectilePrefab;      // The projectile to fire — must have a Collider and ProjectileBehavior
    public float        _projectileSpeed = 10f; // How fast the projectile travels


    /*  ========================================
                        UNITY METHODS
        ========================================    */

    void Awake()
    {
        // Reserved for future pre-Start initialization
    }
    
    void Start()
    {
        // Grab the Rigidbody on this object.
        // Note: if this object was set up by GameMaster.TransformPlayer(), _rb was already
        // injected manually, but reassigning it here is harmless.
        _rb = GetComponent<Rigidbody>();

        // Lock position on Y (no floating) and rotation on X/Z (no tipping over).
        // Y rotation is intentionally left free so MoveRotation() works in ControlsWASDKeys/ControlsArrowKeys.
        _rb.constraints = RigidbodyConstraints.FreezePositionY
                        | RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Check every frame: if this object has been flagged as a shooter and Space is pressed,
        // fire a projectile. canShoot is set by GameMaster.TransformPlayer() after transformation.
        if (canShoot && Input.GetKeyDown(KeyCode.Space))
        {
            Shoot();
        }
        // Note: GetKeyDown must stay in Update() — it only registers for one frame per press
        // and would be missed if placed in FixedUpdate() which runs at a different rate.
    }

    void FixedUpdate()
    {
        // Movement uses Rigidbody.MovePosition/MoveRotation which must run in FixedUpdate
        // to stay in sync with the physics engine. Calling it in Update() can cause jitter
        // and conflicts when Rigidbody constraints are active.
        MovePlayers();
    }


    /*  ========================================
                        PLAYER MOVEMENT
        ========================================
        Movement is split into three layers for modularity:
        MovePlayers() routes to the correct control scheme based on PlayerSelect.
        ControlsWASDKeys() and ControlsArrowKeys() handle the actual input and physics calls.
        This makes it easy to swap or add keybind schemes later without touching core logic.
        ========================================    */

    // Router: checks which player this is and delegates to the correct control method.
    // Called every FixedUpdate tick.
    void MovePlayers()
    {
        if      (PlayerSelect == PlayerType.Player01) ControlsWASDKeys();
        else if (PlayerSelect == PlayerType.Player02) ControlsArrowKeys();
    }

    // Handles movement and rotation for Player 1 using WASD keys.
    // Uses MovePosition (physics-safe translation) and MoveRotation (physics-safe rotation).
    // Rotation snaps to 90-degree intervals so the player always faces the direction they move,
    // which in turn makes transform.forward correct for Shoot().
    void ControlsWASDKeys()
    {
        Vector3     _direction      = Vector3.zero;
        Quaternion  _targetRotation = _rb.rotation; // Default: hold current rotation if no key is pressed

        if      (Input.GetKey(KeyCode.W)) { _direction = Vector3.forward;  _targetRotation = Quaternion.Euler(0, 0,   0); }
        else if (Input.GetKey(KeyCode.S)) { _direction = Vector3.back;     _targetRotation = Quaternion.Euler(0, 180, 0); }
        else if (Input.GetKey(KeyCode.A)) { _direction = Vector3.left;     _targetRotation = Quaternion.Euler(0, 270, 0); }
        else if (Input.GetKey(KeyCode.D)) { _direction = Vector3.right;    _targetRotation = Quaternion.Euler(0, 90,  0); }

        if (_direction != Vector3.zero)
        {
            _rb.MovePosition(_rb.position + _direction * _PlayerMoveSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(_targetRotation);
        }
    }

    // Handles movement and rotation for Player 2 using Arrow keys.
    // Identical logic to ControlsWASDKeys() — see that method for detail.
    void ControlsArrowKeys()
    {
        Vector3     _direction      = Vector3.zero;
        Quaternion  _targetRotation = _rb.rotation;

        if      (Input.GetKey(KeyCode.UpArrow))    { _direction = Vector3.forward;  _targetRotation = Quaternion.Euler(0, 0,   0); }
        else if (Input.GetKey(KeyCode.DownArrow))  { _direction = Vector3.back;     _targetRotation = Quaternion.Euler(0, 180, 0); }
        else if (Input.GetKey(KeyCode.RightArrow)) { _direction = Vector3.left;     _targetRotation = Quaternion.Euler(0, 270, 0); }
        else if (Input.GetKey(KeyCode.LeftArrow))  { _direction = Vector3.right;    _targetRotation = Quaternion.Euler(0, 90,  0); }

        if (_direction != Vector3.zero)
        {
            _rb.MovePosition(_rb.position + _direction * _PlayerMoveSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(_targetRotation);
        }
    }


    /*  ========================================
                        COLLISION
        ========================================
        Two separate collision events are handled here:

        1. Weapon pickup ("Weapon" tag): if this player is the loser, touching the
           pickup triggers transformation into the gun object via GameMaster.TransformPlayer().

        2. Player-to-player contact: if this object hits another PlayerBehavior object,
           CompareHand() is called to check if this object holds the winning hand.
           Only the winner's script calls WaitWhichPlayerWereYouAgain() — the loser's
           CompareHand() call simply finds no match and does nothing.
        ========================================    */

    void OnCollisionEnter(Collision collision)
    {
        // ---- Weapon pickup collision ----
        if (collision.gameObject.CompareTag("Weapon"))
        {
            // Only the loser is allowed to pick this up.
            // hasTransformed prevents double-triggering if physics fires multiple contacts.
            if (isLoser && !hasTransformed)
            {
                // 1. Lock the flag immediately to block any repeat calls this frame
                hasTransformed = true;

                // 2. Destroy the pickup object so no one else can grab it
                Destroy(collision.gameObject);

                // 3. Tell GameMaster to replace this player with the gun object.
                //    GameMaster.TransformPlayer() is linked here — it reads PlayerSelect
                //    to know which instance (_currentPlayerInstance1/2) to destroy and replace.
                GameMaster.Instance.TransformPlayer(PlayerSelect);
            }
        }

        // ---- Player-to-player collision ----
        var _otherPlayer = collision.gameObject.GetComponent<PlayerBehavior>();

        if (_otherPlayer != null)
        {
            // Compare this player's hand against the other player's hand.
            // Both players fire this event, but only the one with the winning hand
            // will find a match inside CompareHand() and call WaitWhichPlayerWereYouAgain().
            CompareHand(this.HandSelect, _otherPlayer.HandSelect);
        }
    }

    // Checks if this object's hand beats the opponent's hand.
    // If a win condition is found, calls WaitWhichPlayerWereYouAgain() to award a point.
    // If no condition matches, this method does nothing — the other player's call will handle it.
    void CompareHand(HandType _myHand, HandType _otherHand)
    {
        Debug.Log($"CompareHand: my hand={_myHand}, opponent hand={_otherHand}");

        // Rock beats Scissors
        if (_myHand == HandType.Rock && _otherHand == HandType.scizzor)
        {
            Debug.Log("CompareHand: Rock wins.");
            WaitWhichPlayerWereYouAgain();
        }

        // Scissors beats Paper
        if (_myHand == HandType.scizzor && _otherHand == HandType.Paper)
        {
            Debug.Log("CompareHand: Scissors wins.");
            WaitWhichPlayerWereYouAgain();
        }

        // Paper beats Rock
        if (_myHand == HandType.Paper && _otherHand == HandType.Rock)
        {
            Debug.Log("CompareHand: Paper wins.");
            WaitWhichPlayerWereYouAgain();
        }
    }

    // Awards a point to whichever player THIS script belongs to, then ends the round.
    // Only called when this object is confirmed to have the winning hand.
    // Uses PlayerSelect to route to the correct point method in GameMaster.
    void WaitWhichPlayerWereYouAgain()
    {
        // Award the point to the winning player via GameMaster.
        // GameMaster.AddPointToPlayer01/02 increments the score and logs it.
        if      (PlayerSelect == PlayerType.Player01) GameMaster.Instance.AddPointToPlayer01();
        else if (PlayerSelect == PlayerType.Player02) GameMaster.Instance.AddPointToPlayer02();

        // Signal GameMaster that the round is over.
        // GameMaster.EndOfRound() handles cleanup, UI reset, and win condition check.
        GameMaster.Instance.EndOfRound();
    }


    /*  ========================================
                        SHOOTING
        ========================================
        Shoot() is only active on the gun object (canShoot == true).
        It instantiates the projectile prefab, configures its physics, stamps it
        with the shooter's PlayerType (so ProjectileBehavior knows who gets the point),
        and ignores collision with the shooter to prevent self-hits.
        ========================================    */

    // Fires a projectile in the direction this object is currently facing.
    // Called from Update() when canShoot is true and Space is pressed.
    // Linked to ProjectileBehavior — the spawned projectile carries shooterType,
    // which ProjectileBehavior uses to call the correct AddPointToPlayer method.
    void Shoot()
    {
        // 1. Safety check — if no prefab was assigned (or carried over from the old player), abort
        if (_projectilePrefab == null)
        {
            Debug.LogWarning("[Shoot] _projectilePrefab is null. Assign it in the Inspector or ensure TransformPlayer copied it.");
            return;
        }

        // 2. Determine fire direction from the object's current facing direction.
        //    This works correctly because ControlsWASDKeys/ControlsArrowKeys keep the
        //    rotation updated to match the last movement direction via MoveRotation().
        Vector3 shootDir = transform.forward;

        // 3. Instantiate the projectile at this object's current position and rotation
        GameObject projObj = Instantiate(_projectilePrefab, transform.position, transform.rotation);

        // 4. Ensure the projectile has a Rigidbody for physics-driven movement
        Rigidbody rbProj = projObj.GetComponent<Rigidbody>();
        if (rbProj == null)
        {
            rbProj = projObj.AddComponent<Rigidbody>();
            rbProj.useGravity   = false;    // Projectile should fly straight, not arc downward
            rbProj.isKinematic  = false;    // Must be non-kinematic to receive velocity
            rbProj.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Prevents tunneling through thin colliders
        }

        // 5. Apply velocity directly so the projectile moves immediately this frame
        rbProj.linearVelocity = shootDir * _projectileSpeed;

        // 6. Tag it so Cleanup() in GameMaster can find and destroy any stray projectiles at round end
        projObj.tag = "Projectile";

        // 7. Stamp the shooter's identity onto the projectile.
        //    ProjectileBehavior.OnCollisionEnter reads shooterType to know who gets the point
        //    when this projectile hits a player.
        var projBehavior = projObj.GetComponent<ProjectileBehavior>();
        if (projBehavior == null) projBehavior = projObj.AddComponent<ProjectileBehavior>();
        projBehavior.shooterType = this.PlayerSelect;

        // 8. Ignore collision between this projectile and the player who fired it.
        //    Without this, the projectile would immediately hit the shooter on spawn.
        Physics.IgnoreCollision(projObj.GetComponent<Collider>(), GetComponent<Collider>());
    }
}