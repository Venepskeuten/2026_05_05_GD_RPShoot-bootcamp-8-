using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class UIMaster : MonoBehaviour
{
  
    /*  ========================================
                        VARIABLES
        ========================================    */
    public static UIMaster Instance;


    [Header ("UI   -   Phase1_parents")]
    public GameObject           UI_Phase1_Parent;

    // Match reroll
    [Header ("UI   -   Phase1_Reroll")]
    public TextMeshProUGUI      UI_Phase1_txt_MatchRerollText;
    public GameObject           UI_Phase1_txt_MatchRerollObj;
    
    [Header ("UI   -   Phase1_Display Player Hand")]
    // Player display
    public TextMeshProUGUI      UI_Phase1_txt_PlayerDisplay_01Text;
    public TextMeshProUGUI      UI_Phase1_txt_PlayerDisplay_02Text;

    [Header ("UI   -   Phase1_Display points per player")]
    public TextMeshProUGUI      UI_Phase1_txt_PointsDisplay_01Text;
    public TextMeshProUGUI      UI_Phase1_txt_PointsDisplay_02Text;

    [Header ("UI   -   Phase2")]
    public TextMeshProUGUI      UI_Phase2_txt_TimerText;
    public GameObject           UI_Phase2_txt_TimerObj;

    /*  ========================================
                        UNITY METHODS
        ========================================    */

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // disables MatchReroll as there really is no reason to have it enabled unless it needs to
        DisableUI_MatchReroll();
        
        // just update the points UI to say 0
        UpdatePoints();
    }

    void Update()
    {
        // updates the timer
        UpdateTimer();
    }


    /*  ========================================
                        PHASE 1 METHODS
        ========================================    */
    
    // disable/enable the phase 1 parent UI object
    public void EnableUI_Phase1Parent()
    {
        UI_Phase1_Parent.SetActive(true);
    }

    public void Disable_Phase1Parent()
    {
        UI_Phase1_Parent.SetActive(false);
    }

    // disable/enable MatchReroll text object
    public void EnableUI_MatchReroll()
    {
        UI_Phase1_txt_MatchRerollObj.SetActive(true);
    }

    public void DisableUI_MatchReroll()
    {
        UI_Phase1_txt_MatchRerollObj.SetActive(false);
    }

    public void WriteHandType(PlayerBehavior.HandType _p1HandText, PlayerBehavior.HandType _p2HandText)
    {
        UI_Phase1_txt_PlayerDisplay_01Text.text = $"P1  :   {_p1HandText}";
        UI_Phase1_txt_PlayerDisplay_02Text.text = $"P2  :   {_p2HandText}";
    }
    
    public void UpdatePoints()
    {
        var scores = GameMaster.Instance.Getpoints(); // returns tuple
        Debug.Log($"P1: {scores.p1}, P2: {scores.p2}");

        UI_Phase1_txt_PointsDisplay_01Text.text = $"{scores.p1}";
        UI_Phase1_txt_PointsDisplay_02Text.text = $"{scores.p2}";
    }

        /*  ========================================
                        PHASE 2 METHODS
        ========================================    */

    // enable/disable the timer
    public void EnableUI_TimerText()
    {
        UI_Phase2_txt_TimerObj.SetActive(true);
    }

    public void DisableUI_TimerText()
    {
        UI_Phase2_txt_TimerObj.SetActive(false);
    }

    public void UpdateTimer()
    {
        var _timerData = GameMaster.Instance.GetTimer();    // gets active timer data

        UI_Phase2_txt_TimerText.text = $"{_timerData}";
    }    
}


