using System;
using System.Linq;

namespace Bot;

public class NeverBot() : IBot
{
    private readonly IGame game = Inject<IGame>();
    private readonly CreepManager creepManager = new();

    public void Loop()
    {
        creepManager.Tick();

        foreach (var room in game.Rooms.Values.Where(room => room.Controller?.My == true).SelectMany(room => room.Find<IStructureSpawn>()))
        {
            HandleSpawn(room);
        }
    }

    private void HandleSpawn(IStructureSpawn room)
    {
        if (room.Spawning == null)
        {
            var body = new[] { BodyPartType.Move, BodyPartType.Work, BodyPartType.Carry };
            if (room.SpawnCreep(body, $"{game.Time}", new SpawnCreepOptions(dryRun: true)) == SpawnCreepResult.Ok)
            {
                var memory = game.CreateMemoryObject();
                memory.SetValue("role", (int) CreepRole.BootstrapHarvester);
                room.SpawnCreep(body, $"Roughneck{game.Time}", new SpawnCreepOptions(memory: memory));
            }
        }
    }
}
