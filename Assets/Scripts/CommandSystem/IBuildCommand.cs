using UnityEngine;

public interface IBuildCommand
{
    string Name { get; }
    bool Execute();   
    void Undo();    
}