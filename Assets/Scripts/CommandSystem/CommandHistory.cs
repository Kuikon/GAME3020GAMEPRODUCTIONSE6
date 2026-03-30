using System.Collections.Generic;

public sealed class CommandHistory
{
    private readonly Stack<IBuildCommand> undoStack = new Stack<IBuildCommand>();
    private readonly Stack<IBuildCommand> redoStack = new Stack<IBuildCommand>();

    // ⭐ ① playEffects対応
    public bool Do(IBuildCommand command, bool debugLogs = false, bool playEffects = true)
    {
        if (command == null)
            return false;

        bool ok = command.Do(debugLogs, playEffects);
        if (!ok)
            return false;

        undoStack.Push(command);
        redoStack.Clear();
        return true;
    }

    // ⭐ ① playEffects対応
    public void Undo(bool debugLogs = false, bool playEffects = true)
    {
        if (undoStack.Count == 0)
            return;

        IBuildCommand cmd = undoStack.Pop();
        cmd.Undo(debugLogs, playEffects);
        redoStack.Push(cmd);
    }

    // ⭐ ① playEffects対応
    public void Redo(bool debugLogs = false, bool playEffects = true)
    {
        if (redoStack.Count == 0)
            return;

        IBuildCommand cmd = redoStack.Pop();
        bool ok = cmd.Do(debugLogs, playEffects);

        if (ok)
            undoStack.Push(cmd);
    }

    // ⭐ ② Peek追加（超重要）
    public IBuildCommand PeekUndo()
    {
        if (undoStack.Count == 0)
            return null;

        return undoStack.Peek();
    }

    public IBuildCommand PeekRedo()
    {
        if (redoStack.Count == 0)
            return null;

        return redoStack.Peek();
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }
}