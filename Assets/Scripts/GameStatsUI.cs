using UnityEngine;
using TMPro;

public class GameStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI timerText;
    
    [Header("Format")]
    public string timerFormat = "{0:00}:{1:00}";
    
    // Static kill counter - accessible from anywhere
    public static int KillCount { get; private set; } = 0;
    
    private float elapsedTime = 0f;
    
    void Start()
    {
        // Reset stats at game start
        KillCount = 0;
        elapsedTime = 0f;
        UpdateUI();
    }
    
    void Update()
    {
        // Update timer
        elapsedTime += Time.deltaTime;
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (killCountText != null)
        {
            killCountText.text = KillCount.ToString();
        }
        
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = string.Format(timerFormat, minutes, seconds);
        }
    }
    
    // Call this from EnemyDamage.Die()
    public static void AddKill()
    {
        KillCount++;
    }
    
    // Reset stats (call when restarting level)
    public static void ResetStats()
    {
        KillCount = 0;
    }
}

