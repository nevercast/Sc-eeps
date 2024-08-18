using System;
using System.Linq;

namespace Bot;

public class NeverBot() : IBot
{
    private readonly IGame _game = Inject<IGame>();
    private readonly CreepManager _creepManager = new();

    public void Loop()
    {
        _creepManager.Tick();
        foreach (var room in _game.Rooms.Values.Where(room => room.Controller?.My == true)
                     .SelectMany(room => room.Find<IStructureSpawn>()))
        {
            HandleSpawn(room);
        }
    }

    private void HandleSpawn(IStructureSpawn room)
    {
        if (room.Spawning != null) return;
        var body = new[] { BodyPartType.Move, BodyPartType.Work, BodyPartType.Carry };
        if (room.SpawnCreep(body, $"{_game.Time}", new SpawnCreepOptions(dryRun: true)) != SpawnCreepResult.Ok) return;
        var memory = _game.CreateMemoryObject();
        memory.SetValue("role", (int)CreepRole.BootstrapHarvester);
        room.SpawnCreep(body, $"Roughneck{_game.Time}", new SpawnCreepOptions(memory: memory));
    }
}