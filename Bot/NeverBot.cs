using System;
using System.Linq;

namespace Bot;

public class NeverBot() : IBot
{
    private readonly IGame _game = Inject<IGame>();
    private readonly CreepManager _creepManager = new();
    private readonly SpawnManager _spawnManager = new();

    public void Loop()
    {
        _creepManager.Tick();
        foreach (var spawn in _game.Rooms.Values.Where(room => room.Controller?.My == true)
                     .SelectMany(room => room.Find<IStructureSpawn>()))
        {
            _spawnManager.HandleSpawn(spawn);
        }
    }
}