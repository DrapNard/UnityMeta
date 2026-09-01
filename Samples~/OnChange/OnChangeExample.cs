using UnityEngine;

public sealed class OnChangeExample : MonoBehaviour
{
    [OnHealthChanged(Channel = "combat-ui")]
    public int health = 100;

    public void HandleHealthChanged(int oldValue, int newValue, string channel)
    {
        Debug.Log(
            "[" + channel + "] health changed from " + oldValue + " to " + newValue + ".",
            this);
    }

    private void Start()
    {
        health = 75; // notifies once
        health = 75; // no notification: EqualityComparer<int>.Default says equal
        health = 25; // notifies again
    }
}
