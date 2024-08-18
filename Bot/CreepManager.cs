using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot;

public class CreepManager
{
  private readonly IGame _game = Inject<IGame>();
  private readonly ILogger _logger = Logger.For(typeof (CreepManager));

  private readonly Dictionary<CreepRole, Type> _roleMap = new()
  {
    { CreepRole.BootstrapHarvester, typeof(BootstrapHarvester) }
  };
    

  public void Tick()
  {
    _logger.Info("Ticking CreepManager");
    foreach (var creep in _game.Creeps.Values)
    {
      switch (creep.GetCreepRole())
      {
        case CreepRole.BootstrapHarvester:
          _logger.Info("Execute BootstrapHarvester Creep");
          BootstrapHarvester.ExecuteHarvester(creep);
          break;
        case CreepRole.Harvester:
          Harvester.ExecuteHarvester(creep);
          break;
        case CreepRole.Hauler:
          Hauler.ExecuteHauler(creep);
          break;
        case CreepRole.Upgrader:
          HandleUpgrader(creep);
          break;
        case CreepRole.Builder:
          HandleBuilder(creep);
          break;
        case CreepRole.Repairer:
        case CreepRole.Defender:
        case CreepRole.Scout:
        case CreepRole.Claimer:
        default:
          throw new ArgumentOutOfRangeException();
      }
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