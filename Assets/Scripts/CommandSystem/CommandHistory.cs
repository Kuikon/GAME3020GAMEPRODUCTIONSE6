using System.Collections.Generic;
using UnityEngine;

public class CommandHistory
{
    private readonly Stack<IBuildCommand> undo = new();
    private readonly Stack<IBuildCommand> redo = new();

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public bool Do(IBuildCommand cmd, bool debugLog = false)
    {
        if (cmd == null) return false;

        bool ok = cmd.Execute();
        if (!ok) return false;

        undo.Push(cmd);
        redo.Clear();

        if (debugLog) Debug.Log($"DO: {cmd.Name}");
        return true;
    }

    public void Undo(bool debugLog = false)
    {
        if (undo.Count == 0) return;

        var cmd = undo.Pop();
        cmd.Undo();
        redo.Push(cmd);

        if (debugLog) Debug.Log($"UNDO: {cmd.Name}");
    }

    public void Redo(bool debugLog = false)
    {
        if (redo.Count == 0) return;

        var cmd = redo.Pop();
        bool ok = cmd.Execute();
        if (ok) undo.Push(cmd);

        if (debugLog) Debug.Log($"REDO: {cmd.Name}");
    }

    public void Clear()
    {
        undo.Clear();
        redo.Clear();
    }
}