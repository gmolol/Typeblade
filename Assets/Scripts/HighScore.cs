using UnityEngine;

// saves the best score and best grade with playerprefs so they stay after closing the game
public static class HighScores
{
    const string ScoreKey = "BestScore";
    const string GradeKey = "BestGrade";

    public static int BestScore => PlayerPrefs.GetInt(ScoreKey, 0);
    public static string BestGrade => PlayerPrefs.GetString(GradeKey, "");
    public static bool HasRecord => BestScore > 0 || BestGrade != "";

    // saves the run if its better. returns true if the score is a new best
    // score and grade are checked separately so you can beat one without the other
    public static bool Submit(int score, string grade)
    {
        bool newBestScore = score > BestScore;
        if (newBestScore) PlayerPrefs.SetInt(ScoreKey, score);

        if (Rank(grade) > Rank(BestGrade)) PlayerPrefs.SetString(GradeKey, grade);

        PlayerPrefs.Save();
        return newBestScore;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(ScoreKey);
        PlayerPrefs.DeleteKey(GradeKey);
        PlayerPrefs.Save();
    }

    // turns a grade into a number so they can be compared
    // f = 0, c = 1, b = 2, a = 3, s = 4, no record = -1
    static int Rank(string grade)
    {
        if (string.IsNullOrEmpty(grade)) return -1;
        return "FCBAS".IndexOf(grade);   // its position in the string is its rank
    }
}
