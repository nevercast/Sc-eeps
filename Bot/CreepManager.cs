using System;
using System.Linq;

namespace Bot;

public class CreepManager
{
  private readonly IGame _game = Inject<IGame>();

  public void Tick()
  {
    foreach (var creep in _game.Creeps.Values)
    {
      switch (creep.GetCreepRole())
      {
        case CreepRole.BootstrapHarvester:
          BootstrapHarvester.ExecuteHarvester((IBootstrapHarvester)creep);
          break;
        case CreepRole.Harvester:
          HandleHarvester(creep);
          break;
        case CreepRole.Hauler:
          HandleHauler(creep);
          break;
        case CreepRole.Upgrader:
          HandleUpgrader(creep);
          break;
        case CreepRole.Builder:
          HandleBuilder(creep);
          break;
      }
    }
  }

    private static void HandleHarvester(ICreep creep)
  {
    var source = creep.Room!.Find<ISource>().FirstOrDefault() ?? throw new TerminateCreepException(creep, "No source found.");
    if (creep.Harvest(source) == CreepHarvestResult.NotInRange)
    {
      creep.MoveTo(source.RoomPosition);
    }
  }

  private static void HandleHauler(ICreep creep)
  {
    var target = creep.Room!.Find<IStructure>().Where((structure) =>
      structure is IStructureSpawn spawn && spawn.Store.GetFreeCapacity(ResourceType.Energy) > 0
      || structure is IStructureExtension extension && extension.Store.GetFreeCapacity(ResourceType.Energy) > 0
    ).FirstOrDefault() ?? throw new TerminateCreepException(creep, "No target found.");
    if (creep.Transfer(target, ResourceType.Energy) == CreepTransferResult.NotInRange)
    {
      creep.MoveTo(target.RoomPosition);
    }
  }

  private static void HandleUpgrader(ICreep creep)
  {
    if (creep.Room!.Controller is IStructureController controller)
    {
      if (creep.UpgradeController(controller) == CreepUpgradeControllerResult.NotInRange)
      {
        creep.MoveTo(controller.RoomPosition);
      }
    }
  }

  private static void HandleBuilder(ICreep creep)
  {
    var constructionSite = creep.Room!.Find<IConstructionSite>().FirstOrDefault() ?? throw new TerminateCreepException(creep, "No construction site found.");
    if (creep.Build(constructionSite) == CreepBuildResult.NotInRange)
    {
      creep.MoveTo(constructionSite.RoomPosition);
    }
  }

}