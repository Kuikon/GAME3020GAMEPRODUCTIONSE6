public interface IBuildCommand
{
    string Name { get; }

    bool Do(bool debugLogs = false, bool playEffects = true);
    void Undo(bool debugLogs = false, bool playEffects = true);
}