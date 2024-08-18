using System.Linq;

namespace Bot;

public class Hauler
{
    private static readonly ILogger Logger = Bot.Logger.For(typeof(Hauler));
    private static readonly IGame Game = Inject<IGame>();

    private class HaulerState
    {
        public bool IsGoingToPickup { get; init; }
    }

    public static void ExecuteHauler(ICreep hauler)
    {
        Logger.Info("Executing Hauler");
        switch (hauler.GetUserData<HaulerState>()?.IsGoingToPickup)
        {
            case true:
                Pickup(hauler);
                break;
            case false:
                Dropoff(hauler);
                break;
            case null:
                Init(hauler);
                break;
        }
    }

    private static void Init(ICreep hauler)
    {
        Logger.Info("Initializing Hauler");
        var source = hauler.GetSource();
        if (source == null)
        {
            source = AssignSourceToHauler(hauler);
            if (source == null)
            {
                Logger.Warn("No source available for hauler");
                return;
            }

            hauler.SetSource(source);
        }

        hauler.SetUserData(new HaulerState { IsGoingToPickup = true });
    }

    private static void Pickup(ICreep hauler)
    {
        Logger.Info("Executing Pickup");
        var source = hauler.GetSource();
        if (source == null)
        {
            Init(hauler);
            return;
        }

        var droppedResources = hauler.Room?.Find<IResource>()
            .Where(r => r.ResourceType == ResourceType.Energy &&
                        r.RoomPosition.Position.CartesianDistanceTo(source.RoomPosition.Position) < 5)
            .OrderByDescending(r => r.Amount).FirstOrDefault();
        if (droppedResources != null)
        {
            if (hauler.Pickup(droppedResources) == CreepPickupResult.NotInRange)
            {
                hauler.MoveTo(droppedResources.RoomPosition,
                    new MoveToOptions(FindPathOptions: new FindPathOptions(range: 2)));
            }
        }
        else
        {
            hauler.MoveTo(source.RoomPosition,
                new MoveToOptions(FindPathOptions: new FindPathOptions(range: 2)));
        }

        if (hauler.Store.GetFreeCapacity(ResourceType.Energy) == 0)
        {
            hauler.SetUserData(new HaulerState { IsGoingToPickup = false });
        }
    }

    private static void Dropoff(ICreep hauler)
    {
        Logger.Info("Executing Dropoff");
        var dropoffTarget = FindOptimalDropoffTarget(hauler);
        if (dropoffTarget == null)
        {
            Logger.Warn("No dropoff target found for hauler");
            return;
        }

        hauler.Room?.Visual.Line(hauler.RoomPosition.Position, dropoffTarget.RoomPosition.Position,
            new LineVisualStyle(color: Color.FromNorm(0.62, 0.2, 0.95), lineStyle: LineStyle.Dashed)
        );

        if (dropoffTarget is IStructureController controller &&
            hauler.UpgradeController(controller) == CreepUpgradeControllerResult.NotInRange ||
            hauler.Transfer(dropoffTarget, ResourceType.Energy) == CreepTransferResult.NotInRange)
        {
            hauler.MoveTo(dropoffTarget.RoomPosition);
        }

        if (hauler.Store.GetUsedCapacity(ResourceType.Energy) == 0)
        {
            hauler.SetUserData(new HaulerState { IsGoingToPickup = true });
        }
    }

    private static ISource? AssignSourceToHauler(ICreep hauler)
    {
        var room = hauler.Room;
        if (room == null) return null;
        var sources = room.Find<ISource>().ToList();
        var creeps = Game.Creeps.Values.Where(c => Equals(c.Room, room)).ToList();
        foreach (var source in from source in sources
                 let assignedHaulers =
                     creeps.Count(c => c.GetCreepRole() == CreepRole.Hauler && c.GetSource() == source)
                 let requiredHaulers = CalculateRequiredHaulersForSource(source, room)
                 where assignedHaulers < requiredHaulers
                 select source)
        {
            return source;
        }

        return sources.FirstOrDefault(); // Assign to first source if all are equally occupied
    }

    private static int CalculateRequiredHaulersForSource(ISource source, IRoom room)
    {
        var harvesterBody = SpawnManager.GetBodyForRole(CreepRole.Harvester, room.Controller?.Level ?? 1);
        var haulerBody = SpawnManager.GetBodyForRole(CreepRole.Hauler, room.Controller?.Level ?? 1);
        var harvesterEnergyPerTick = Calculations.CalculateHarvesterEnergyPerTick(harvesterBody);
        var haulerCarryCapacity = haulerBody[BodyPartType.Carry] * Game.Constants.Creep.CarryCapacity;
        var dropPositions = new[]
        {
            room.Find<IStructureSpawn>().FirstOrDefault()?.RoomPosition ?? room.Controller!.RoomPosition
        };
        var haulerRequirements =
            new Calculations.HaulerRequirements(harvesterEnergyPerTick, haulerCarryCapacity, dropPositions);
        var requiredHaulers = Calculations.CalculateRequiredHaulers(new[] { source }, haulerRequirements, haulerBody);
        Logger.Info($"CalculateRequiredHaulers ${source}: ${requiredHaulers}");
        return requiredHaulers;
    }

    private static IStructure? FindOptimalDropoffTarget(ICreep hauler)
    {
        var room = hauler.Room;
        if (room == null) return null;

        // Priority order: Spawn, Extensions, Storage, Container
        var spawn = room.Find<IStructureSpawn>().FirstOrDefault(s => s.Store.GetFreeCapacity(ResourceType.Energy) > 0);
        if (spawn != null)
        {
            Logger.Info($"FindOptimalDropoffTarget 1.${spawn}");
            return spawn;
        }

        var extension = room.Find<IStructureExtension>()
            .FirstOrDefault(e => e.Store.GetFreeCapacity(ResourceType.Energy) > 0);
        if (extension != null)
        {
            Logger.Info($"FindOptimalDropoffTarget 2.${extension}");
            return extension;
        }

        var storage = room.Find<IStructureStorage>().FirstOrDefault();
        if (storage != null && storage.Store.GetFreeCapacity(ResourceType.Energy) > 0)
        {
            Logger.Info($"FindOptimalDropoffTarget 3.${storage}");
            return storage;
        }

        var container = room.Find<IStructureContainer>().Where(c => c.Store.GetFreeCapacity(ResourceType.Energy) > 0)
            .OrderBy(c => hauler.RoomPosition.Position.LinearDistanceTo(c.RoomPosition.Position)).FirstOrDefault();
        if (container != null)
        {
            Logger.Info($"FindOptimalDropoffTarget 4.${container}");
            return container;
        }

        Logger.Info($"FindOptimalDropoffTarget 5.${room.Controller}");
        // If no valid targets, return the controller (to drop near it)
        return room.Controller;
    }
}