public sealed class BuildContext
{
    public GridManager Grid { get; }
    public BuildRaycaster Raycaster { get; }
    public BuildPlacementSolver Solver { get; }
    public BuildSpawner Spawner { get; }
    public BuildPlacementRules Rules { get; }
    public CommandHistory History { get; }
    public ObjectsDatabaseSO Database { get; }
    public BuildPreview Preview { get; }
    public DroneService Drone { get; }

    public BuildContext(
        GridManager grid,
        BuildRaycaster raycaster,
        BuildPlacementSolver solver,
        BuildSpawner spawner,
        BuildPlacementRules rules,
        CommandHistory history,
        ObjectsDatabaseSO database,
        BuildPreview preview,
        DroneService drone)
    {
        Grid = grid;
        Raycaster = raycaster;
        Solver = solver;
        Spawner = spawner;
        Rules = rules;
        History = history;
        Database = database;
        Preview = preview;
        Drone = drone;
    }
}