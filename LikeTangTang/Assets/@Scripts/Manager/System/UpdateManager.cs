using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public interface ITickable
{
    void Tick(float _deltaTime);
}
public class UpdateManager : MonoBehaviour
{
    private readonly List<ITickable> tickable = new();
    private readonly List<ITickable> toAdd = new();
    private readonly List<ITickable> toRemove = new();

    private bool isPaused = false;

    public void PauseTicking(bool _pause)
    {
        isPaused = _pause;
    }
    public void Register(ITickable _tickable)
    {
        if (!tickable.Contains(_tickable)) toAdd.Add(_tickable);
    }

    public void Unregister(ITickable _tickable)
    {
        toRemove.Add(_tickable);
    }

    void Update()
    {
        if (isPaused) return;
        float deltaTime = Time.deltaTime;

        foreach (var t in toAdd)
            if (!tickable.Contains(t)) tickable.Add(t);
        toAdd.Clear();

        foreach (var t in toRemove) tickable.Remove(t);
        toRemove.Clear();

        for (int i = tickable.Count - 1; i >= 0; i--)
        {
            ITickable tick = tickable[i];
            if (tick is Component component)
            {
                if (component == null)
                {

                    tickable.RemoveAt(i);
                    continue;
                }
            }
            tick.Tick(deltaTime);
        }

    }

    public void Clear()
    {
        PauseTicking(false);
        tickable.Clear();
        toAdd.Clear();
        toRemove.Clear();
    }
}
