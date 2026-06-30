using System.Collections.Generic;

public interface ISpawnRequest
{
    SpawnRequestType Type { get; }
    float SpawnTime { get; }
    bool BlocksMovement { get; }
    SpawnSafetyMode SafetyMode { get; }
}

public interface ISpawnRequestSource<TRequest> where TRequest : ISpawnRequest
{
    List<TRequest> BuildSpawnRequests(float spawnTime);
}

public interface ISpawnExecutor<in TRequest> where TRequest : ISpawnRequest
{
    bool CanExecuteSpawn(TRequest request);
    bool ExecuteSpawn(TRequest request);
}

public interface ISpawnTimer
{
    float GetNextSpawnDelay();
}
