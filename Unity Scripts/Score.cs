using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score :MonoBehaviour
{
    public static Score Instance { get; private set; }
    private int TotalScore { get; set; }
    private int PerfectTotal { get; set; }
    private int CoolTotal { get; set; }
    private int PassableTotal { get; set; }
    private int BadTotal { get; set; }
    private int MissTotal { get; set; }

    private void Awake()
    {
        // Ensure that only one instance of the DanceScore exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // This keeps the object alive between scenes
            ResetScore();

        }
        else
        {
            Destroy(gameObject);  // Destroy any duplicate instances
        }
    }

    public void AddFeedback(string feedbackType, int scoreIncrement)
    {
        switch (feedbackType)
        {
            case "เพอร์เฟกต์":
                PerfectTotal++;
                break;
            case "กำลังดี":
                CoolTotal++;
                break;
            case "พอไปได้":
                PassableTotal++;
                break;
            case "แย่หน่อย":
                BadTotal++;
                break;
            case "พลาด":
                MissTotal++;
                break;
        }
        TotalScore += scoreIncrement;
    }


    public void ResetScore()
    {
        TotalScore = 0;
        PerfectTotal = 0;
        CoolTotal = 0;
        PassableTotal = 0;
        BadTotal = 0;
        MissTotal = 0;
    }
    public int GetTotalScore() {  return TotalScore; }
    public int GetPerfectTotal()
    {
        return PerfectTotal;
    }

    public int GetCoolTotal() { return CoolTotal; }
    public int GetPassableTotal()
    {
        return PassableTotal;
    }
    public int GetBadTotal()
    {
        return BadTotal;
    }
    public int GetMissTotal()
    {
        return MissTotal;
    }
}
