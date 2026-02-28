using System.Collections.Generic;

public class CompositeCommand : IBuildCommand
{
    private readonly List<IBuildCommand> children = new();
    public string Name { get; }

    public CompositeCommand(string name) => Name = name;

    public void Add(IBuildCommand cmd)
    {
        if (cmd != null) children.Add(cmd);
    }

    public bool Execute()
    {
        // “r’†‚Å¸”s‚µ‚½‚çAÀsÏ‚İ‚ğŠª‚«–ß‚·
        for (int i = 0; i < children.Count; i++)
        {
            if (!children[i].Execute())
            {
                for (int j = i - 1; j >= 0; j--)
                    children[j].Undo();
                return false;
            }
        }
        return children.Count > 0;
    }

    public void Undo()
    {
        for (int i = children.Count - 1; i >= 0; i--)
            children[i].Undo();
    }
}