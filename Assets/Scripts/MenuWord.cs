using UnityEngine;
using UnityEngine.Events;
using TMPro;

// a menu option you can type (start, options, retry etc). same idea as wordattack but for menus
public class MenuWord : MonoBehaviour
{
    public string word = "start";
    public TMP_Text label;
    public UnityEvent onCompleted;   // what happens when its typed, set in the inspector

    public int Progress { get; private set; }
    public bool IsComplete => Progress == word.Length;

    void Start()
    {
        ResetWord();
    }

    // right letter moves forward, wrong letter returns false
    public bool TryType(char c)
    {
        if (c != word[Progress]) return false;
        Progress++;
        UpdateLabel();
        return true;
    }

    public void ResetWord()
    {
        Progress = 0;
        UpdateLabel();
    }

    // typed letters turn green
    void UpdateLabel()
    {
        label.text = "<color=green>" + word.Substring(0, Progress) + "</color>"
        + word.Substring(Progress);
    }
}
