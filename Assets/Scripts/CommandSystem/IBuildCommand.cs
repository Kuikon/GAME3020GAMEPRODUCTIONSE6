using UnityEngine;

public interface IBuildCommand
{
    string Name { get; }
    bool Do(bool debugLogs = false);
    void Undo(bool debugLogs = false);
}