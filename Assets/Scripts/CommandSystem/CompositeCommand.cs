using System.Collections.Generic;

public sealed class CompositeCommand : IBuildCommand
{
    private readonly List<IBuildCommand> commands = new List<IBuildCommand>();

    public string Name { get; }

    public CompositeCommand(string name)
    {
        Name = name;
    }

    public void Add(IBuildCommand command)
    {
        if (command != null)
            commands.Add(command);
    }

    public bool Do(bool debugLogs = false, bool playEffects = true)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Do(debugLogs, playEffects))
                continue;

            for (int j = i - 1; j >= 0; j--)
                commands[j].Undo(debugLogs, playEffects);

            return false;
        }

        return true;
    }

    public void Undo(bool debugLogs = false, bool playEffects = true)
    {
        for (int i = commands.Count - 1; i >= 0; i--)
            commands[i].Undo(debugLogs, playEffects);
    }
}