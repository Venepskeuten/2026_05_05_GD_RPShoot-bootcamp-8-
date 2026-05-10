using UnityEngine;
using TMPro;
using System.Threading.Tasks;

/*  ========================================
                UI MASTER
    ========================================
    UIMaster is the single point of contact for all UI changes in the game.
    No other script touches UI elements directly — they call methods here instead.
    This keeps UI logic isolated so changing a layout only requires edits in one place.

    GameMaster calls into UIMaster at every phase transition:
        Phase 1 start  → EnableUI_Phase1Parent(), DisableUI_TimerText()
        Phase 2 start  → Disable_Phase1Parent(), EnableUI_TimerText()
        Round end      → UpdatePoints(), DisableUI_TimerText(), EnableUI_Phase1Parent()

    UIMaster polls GameMaster each frame in UpdateTimer() to display the live countdown.
    ========================================    */

public class UIMaster : MonoBehaviour
{

    /*  ========================================
                        VARIABLES
        ========================================    */

    // Singleton reference — allows GameMaster (and any other script) to call UI methods
    // via UIMaster.Instance without needing a direct inspector link to this object.
    public static UIMaster Instance;

    // ---- PHASE 1 UI ----
    // The Phase 1 parent groups all hand-selection screen elements under one object
    // so the entire screen can be shown/hidden with a single SetActive call.
    [Header ("UI   -   Phase1_parents")]
    public GameObject           UI_Phase1_Parent;

    // The MatchReroll text is shown when both players rolled the same hand.
    // It tells players to press the button again. Hidden by default.
    [Header ("UI   -   Phase1_Reroll")]
    public TextMeshProUGUI      UI_Phase1_txt_MatchRerollText;
    public GameObject           UI_Phase1_txt_MatchRerollObj;

    // Displays the hand type (Rock/Paper/Scissors) assigned to each player this round.
    // Updated by WriteHandType(), called from GameMaster.PlayerToHandRNG().
    [Header ("UI   -   Phase1_Display Player Hand")]
    public TextMeshProUGUI      UI_Phase1_txt_PlayerDisplay_01Text;
    public TextMeshProUGUI      UI_Phase1_txt_PlayerDisplay_02Text;

    // Displays each player's current score.
    // Updated by UpdatePoints(), called from GameMaster.EndOfRound().
    [Header ("UI   -   Phase1_Display points per player")]
    public TextMeshProUGUI      UI_Phase1_txt_PointsDisplay_01Text;
    public TextMeshProUGUI      UI_Phase1_txt_PointsDisplay_02Text;

    // ---- PHASE 2 UI ----
    // The timer text and its parent object. The parent controls visibility;
    // the text is updated every frame by UpdateTimer() while Phase 2 is active.
    [Header ("UI   -   Phase2")]
    public TextMeshProUGUI      UI_Phase2_txt_TimerText;
    public GameObject           UI_Phase2_txt_TimerObj;


    /*  ========================================
                        UNITY METHODS
        ========================================    */

    void Awake()
    {
        // Register as the singleton so other scripts can call UIMaster.Instance.<method>
        Instance = this;
    }

    void Start()
    {
        // 1. Hide the re-roll prompt — it should only appear after a tie roll
        DisableUI_MatchReroll();
        
        // 2. Show "0" for both players' scores at game start.
        //    Calls GameMaster.Getpoints() to read the current (0, 0) scores.
        UpdatePoints();
    }

    void Update()
    {
        // Poll GameMaster for the current timer value and update the display.
        // UpdateTimer() is self-gating — it does nothing if the timer object is inactive.
        UpdateTimer();
    }


    /*  ========================================
                        PHASE 1 METHODS
        ========================================
        These methods control the Phase 1 (hand-selection) screen.
        Called by GameMaster at phase transitions and by PlayerToHandRNG on tie rolls.
        ========================================    */

    // Shows the entire Phase 1 UI panel.
    // Called by GameMaster.EndOfRound() to return to the hand-selection screen after a round.
    public void EnableUI_Phase1Parent()
    {
        UI_Phase1_Parent.SetActive(true);
    }

    // Hides the entire Phase 1 UI panel.
    // Called by GameMaster.StartPhase2() when transitioning into live gameplay.
    public void Disable_Phase1Parent()
    {
        UI_Phase1_Parent.SetActive(false);
    }

    // Shows the "Same hand — re-roll" message.
    // Called by GameMaster.PlayerToHandRNG() when both players rolled an identical hand.
    public void EnableUI_MatchReroll()
    {
        UI_Phase1_txt_MatchRerollObj.SetActive(true);
    }

    // Hides the "Same hand — re-roll" message.
    // Called on Start() and at the beginning of each PlayerToHandRNG() call
    // to clear any leftover message from the previous attempt.
    public void DisableUI_MatchReroll()
    {
        UI_Phase1_txt_MatchRerollObj.SetActive(false);
    }

    // Updates the hand-type labels to show what each player rolled this round.
    // Called by GameMaster.PlayerToHandRNG() after a valid (non-tie) roll.
    // Takes HandType enums directly from PlayerBehavior — the enum name becomes the display string.
    public void WriteHandType(PlayerBehavior.HandType _p1HandText, PlayerBehavior.HandType _p2HandText)
    {
        UI_Phase1_txt_PlayerDisplay_01Text.text = $"P1  :   {_p1HandText}";
        UI_Phase1_txt_PlayerDisplay_02Text.text = $"P2  :   {_p2HandText}";
    }
    
    // Refreshes both score displays with the current values from GameMaster.
    // Called by GameMaster.EndOfRound() after every round, and once on Start() to show "0 / 0".
    // Linked to GameMaster.Getpoints() which returns the scores as a tuple.
    public void UpdatePoints()
    {
        var scores = GameMaster.Instance.Getpoints();
        Debug.Log($"UpdatePoints: P1={scores.p1}, P2={scores.p2}");

        UI_Phase1_txt_PointsDisplay_01Text.text = $"{scores.p1}";
        UI_Phase1_txt_PointsDisplay_02Text.text = $"{scores.p2}";
    }


    /*  ========================================
                        PHASE 2 METHODS
        ========================================
        These methods control the timer display shown during Phase 2.
        The timer object is hidden during Phase 1 and shown once Phase 2 starts.
        ========================================    */

    // Shows the timer UI object.
    // Called by GameMaster.StartPhase2() when live gameplay begins.
    public void EnableUI_TimerText()
    {
        UI_Phase2_txt_TimerObj.SetActive(true);
    }

    // Hides the timer UI object.
    // Called by GameMaster.EndOfRound() when the round ends so it doesn't show on the Phase 1 screen.
    // Also called on Start() to ensure it begins hidden.
    public void DisableUI_TimerText()
    {
        UI_Phase2_txt_TimerObj.SetActive(false);
    }

    // Called every frame from Update(). Reads the current timer value from GameMaster
    // and updates the text display. Also applies a color warning when time is running low.
    // Self-gating: only updates if the timer GameObject is currently active in the scene.
    // Linked to GameMaster.GetTimer() which exposes the private _phase2Timer value.
    public void UpdateTimer()
    {
        // 1. Read the current countdown value from GameMaster
        var _timerData = GameMaster.Instance.GetTimer();
        
        // 2. Only update if the timer object is visible — avoids writing to inactive UI elements
        if (UI_Phase2_txt_TimerObj?.activeInHierarchy == true)
        {
            // 3. Display the remaining seconds formatted to 2 decimal places
            UI_Phase2_txt_TimerText.text = $"{_timerData:F2}s";
            
            // 4. Change text color to red as a visual warning when under 15 seconds
            if (_timerData <= 15f && _timerData > 0f)
            {
                UI_Phase2_txt_TimerText.color = Color.red;
            }
            // 5. Snap back to white in the final 3 seconds for a flashing effect
            else if (_timerData <= 3f && _timerData > 0f)
            {
                UI_Phase2_txt_TimerText.color = Color.white;
            }
        }
    }
}