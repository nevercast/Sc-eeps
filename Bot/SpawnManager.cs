using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot;

public class SpawnManager
{
    private static readonly IGame Game = Inject<IGame>();
    private static readonly ILogger Logger = Bot.Logger.For(typeof(SpawnManager));

    public static CreepRole? GetNextSpawnRole(IStructureSpawn spawn)
    {
        if (!spawn.Exists || !spawn.My) return null;
        var room = spawn.Room;
        if (room == null) return null;
        var sources = room.Find<ISource>().ToList();
        var roomCreeps = Game.Creeps.Values.Where(c => Equals(c.Room, room)).ToList();
        if (roomCreeps.Count == 0) return CreepRole.BootstrapHarvester;
        var creepCounts = roomCreeps.GroupBy(creep => creep.GetCreepRole())
            .ToDictionary(group => group.Key, group => group.Count());
        var harvesterCount = creepCounts.GetValueOrDefault(CreepRole.Harvester, 0);
        var haulerCount = creepCounts.GetValueOrDefault(CreepRole.Hauler, 0);
        if (harvesterCount == 0 && haulerCount == 0)
        {
            return CreepRole.BootstrapHarvester;
        }

        if (harvesterCount < sources.Count && (haulerCount > harvesterCount || harvesterCount == 0))
        {
            return CreepRole.Harvester;
        }

        // Calculate hauler requirements
        var harvesterBody = GetBodyForRole(CreepRole.Harvester, room.Controller?.Level ?? 1);
        var haulerBody = GetBodyForRole(CreepRole.Hauler, room.Controller?.Level ?? 1);
        var harvesterEnergyPerTick = Calculations.CalculateHarvesterEnergyPerTick(harvesterBody);
        var haulerCarryCapacity = haulerBody[BodyPartType.Carry] * Game.Constants.Creep.CarryCapacity;
        var dropPositions = new[] { spawn.RoomPosition };
        var haulerRequirements = new Calculations.HaulerRequirements(
            harvesterEnergyPerTick, haulerCarryCapacity, dropPositions);
        var requiredHaulers = Calculations.CalculateRequiredHaulers(sources, haulerRequirements, haulerBody);
        if (haulerCount < requiredHaulers)
        {
            return CreepRole.Hauler;
        }

        // Fallback
        return CreepRole.Upgrader;
    }

    public static BodyType<BodyPartType> GetBodyForRole(CreepRole role, int rcl)
    {
        var energyCapacity = CalculateEnergyCapacity(rcl);
        return role switch
        {
            CreepRole.BootstrapHarvester => new BodyType<BodyPartType>([
                (BodyPartType.Work, 1), (BodyPartType.Carry, 1), (BodyPartType.Move, 1)
            ]),
            CreepRole.Harvester => GetHarvesterBody(energyCapacity),
            CreepRole.Hauler => GetHaulerBody(energyCapacity),
            CreepRole.Upgrader => GetUpgraderBody(energyCapacity),
            _ => throw new ArgumentException("Body not configured for role", nameof(role))
        };
    }

    private static BodyType<BodyPartType> CreateBodyFromSequence(IReadOnlyList<BodyPartType> sequence,
        int energyCapacity, IReadOnlyList<BodyPartType>? head = null, IReadOnlyList<BodyPartType>? tail = null)
    {
        var bodyCosts = new Dictionary<BodyPartType, int>
        {
            { BodyPartType.Move, 50 },
            { BodyPartType.Work, 100 },
            { BodyPartType.Carry, 50 },
            { BodyPartType.Attack, 80 },
            { BodyPartType.RangedAttack, 150 },
            { BodyPartType.Heal, 250 },
            { BodyPartType.Tough, 10 },
            { BodyPartType.Claim, 600 }
        };
        var body = new List<BodyPartType>();
        var remainingEnergy = energyCapacity;

        // Function to add parts from a sequence
        bool AddPartsFromSequence(IReadOnlyList<BodyPartType> seq)
        {
            foreach (var part in seq)
            {
                if (remainingEnergy < bodyCosts[part]) return false;
                body.Add(part);
                remainingEnergy -= bodyCosts[part];
            }

            return true;
        }

        // Add head sequence
        if (head != null && !AddPartsFromSequence(head))
            return new BodyType<BodyPartType>(body.GroupBy(part => part).Select(group => (group.Key, group.Count()))
                .ToArray());

        // Calculate tail cost
        var tailCost = tail?.Sum(part => bodyCosts[part]) ?? 0;

        // Add main sequence
        var index = 0;
        while (remainingEnergy >= bodyCosts[sequence[index % sequence.Count]] + tailCost)
        {
            var part = sequence[index % sequence.Count];
            body.Add(part);
            remainingEnergy -= bodyCosts[part];
            index++;
        }

        // Add tail sequence
        if (tail != null) AddPartsFromSequence(tail);
        return new BodyType<BodyPartType>(body.GroupBy(part => part).Select(group => (group.Key, group.Count()))
            .ToArray());
    }

    private static BodyType<BodyPartType> GetHarvesterBody(int energyCapacity)
    {
        return CreateBodyFromSequence(new[] { BodyPartType.Work, }, energyCapacity,
            head: new[] { BodyPartType.Move, BodyPartType.Work }, tail: new[] { BodyPartType.Move });
    }

    private static BodyType<BodyPartType> GetHaulerBody(int energyCapacity)
    {
        return CreateBodyFromSequence(
            new[] { BodyPartType.Move, BodyPartType.Carry, BodyPartType.Carry, BodyPartType.Move, }, energyCapacity,
            head: new[] { BodyPartType.Move, BodyPartType.Carry }, tail: new[] { BodyPartType.Move });
    }
    
    private static BodyType<BodyPartType> GetUpgraderBody(int energyCapacity)
    {
        return CreateBodyFromSequence(
            new[] { BodyPartType.Move, BodyPartType.Carry, BodyPartType.Work, }, energyCapacity,
            head: new[] { BodyPartType.Move, BodyPartType.Carry, BodyPartType.Work }, tail: new[] { BodyPartType.Move });
    }

    private static int CalculateEnergyCapacity(int rcl)
    {
        return rcl switch
        {
            1 => 300,
            2 => 550,
            3 => 800,
            4 => 1300,
            5 => 1800,
            6 => 2300,
            7 => 5600,
            8 => 12900,
            _ => 300
        };
    }

    public void HandleSpawn(IStructureSpawn spawn)
    {
        if (spawn.Spawning != null) return;
        var nextRole = SpawnManager.GetNextSpawnRole(spawn);
        if (nextRole == null) return;
        var rcl = spawn.Room?.Controller?.Level ?? 1;
        var body = SpawnManager.GetBodyForRole(nextRole.Value, rcl);
        if (spawn.SpawnCreep(body, $"{nextRole}{Game.Time}", new SpawnCreepOptions(dryRun: true)) !=
            SpawnCreepResult.Ok)
        {
            Logger.Warn($"Couldn't spawn {nextRole} ({body}).");
            return;
        }

        ;
        var memory = Game.CreateMemoryObject();
        memory.SetValue("role", (int)nextRole);

        // Add additional memory based on role
        switch (nextRole)
        {
            case CreepRole.Harvester:
            case CreepRole.Hauler:
                var source = AssignSourceToCreep(spawn.Room!, nextRole.Value);
                if (source != null)
                {
                    memory.SetValue(CreepExtensions.CreepMemoryKeySource, source.Id);
                }

                break;
            // Add cases for other roles as needed
        }

        spawn.SpawnCreep(body, $"{nextRole}{Game.Time}", new SpawnCreepOptions(memory: memory));
        Logger.Info($"Spawning {nextRole} with body: {string.Join(",", body)}");
    }

    private ISource? AssignSourceToCreep(IRoom room, CreepRole role)
    {
        var sources = room.Find<ISource>().ToList();
        var creeps = Game.Creeps.Values.Where(c => Equals(c.Room, room)).ToList();
        var harvesterBody = GetBodyForRole(CreepRole.Harvester, room.Controller?.Level ?? 1);
        var haulerBody = GetBodyForRole(CreepRole.Hauler, room.Controller?.Level ?? 1);
        var harvesterEnergyPerTick = Calculations.CalculateHarvesterEnergyPerTick(harvesterBody);
        var haulerCarryCapacity = haulerBody[BodyPartType.Carry] * Game.Constants.Creep.CarryCapacity;
        var dropPositions = new[]
        {
            room.Find<IStructureSpawn>().FirstOrDefault()?.RoomPosition ?? room.Controller!.RoomPosition
        };
        var haulerRequirements = new Calculations.HaulerRequirements(
            harvesterEnergyPerTick, haulerCarryCapacity, dropPositions);
        var requiredHaulersPerSource = Calculations.CalculateRequiredHaulers(sources, haulerRequirements, haulerBody) /
                                       sources.Count;
        foreach (var source in sources)
        {
            var creepsAssignedToSource = creeps.Count(c => c.GetSource() == source);
            switch (role)
            {
                case CreepRole.Harvester when creepsAssignedToSource == 0:
                    return source;
                case CreepRole.Hauler when creepsAssignedToSource < requiredHaulersPerSource:
                    return source;
            }
        }

        return null;
    }
}