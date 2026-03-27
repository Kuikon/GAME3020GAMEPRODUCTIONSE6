using System.Collections.Generic;

public sealed class CommandHistory
{
    private readonly Stack<IBuildCommand> undoStack = new Stack<IBuildCommand>();
    private readonly Stack<IBuildCommand> redoStack = new Stack<IBuildCommand>();

    public bool Do(IBuildCommand command, bool debugLogs = false)
    {
        if (command == null)
            return false;

        bool ok = command.Do(debugLogs);
        if (!ok)
            return false;

        undoStack.Push(command);
        redoStack.Clear();
        return true;
    }

    public void Undo(bool debugLogs = false)
    {
        if (undoStack.Count == 0)
            return;

        IBuildCommand cmd = undoStack.Pop();
        cmd.Undo(debugLogs);
        redoStack.Push(cmd);
    }

    public void Redo(bool debugLogs = false)
    {
        if (redoStack.Count == 0)
            return;

        IBuildCommand cmd = redoStack.Pop();
        bool ok = cmd.Do(debugLogs);

        if (ok)
            undoStack.Push(cmd);
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }
}