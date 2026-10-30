using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;

// lets you pick menu options by typing them instead of clicking
public class TypedMenu : MonoBehaviour
{
    public MenuWord[] words;          // every typeable word on screen
    public GameObject blockingPanel;  // typing is ignored while this is open

    private MenuWord target;          // the word youre currently typing
    private StringBuilder typed = new StringBuilder();

    // onTextInput gives the actual characters typed, collect them and handle them in update
    void OnEnable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;
    }

    void OnTextInput(char c)
    {
        typed.Append(c);
    }

    void Update()
    {
        // empty the buffer every frame (even when blocked so old letters dont pile up)
        string input = typed.ToString();
        typed.Clear();

        if (blockingPanel != null && blockingPanel.activeSelf) return;

        if (Keyboard.current.backspaceKey.wasPressedThisFrame) ClearTarget();   // backspace = start over

        foreach (char raw in input)
        {
            if (!char.IsLetter(raw)) continue;

            char c = char.ToLower(raw);

            // no word picked yet, lock onto the one starting with this letter
            if(target == null) target = FindWordStartingWith(c);
            if (target == null) continue;

            if (!target.TryType(c))
            {
                Debug.Log("Typo: " + c);
                continue;
            }

            if (target.IsComplete)
            {
                MenuWord done = target;
                ClearTarget();
                done.onCompleted.Invoke();   // runs whatever the word does (start game, options etc)
                return;
            }
        }
    }

    MenuWord FindWordStartingWith(char c)
    {
        foreach (MenuWord w in words)
            if (w.word[0] == c) return w;
        return null;
    }

    void ClearTarget()
    {
        if (target != null) target.ResetWord();
        target = null;
    }


}
