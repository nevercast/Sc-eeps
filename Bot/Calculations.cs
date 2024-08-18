using System;
using System.Collections.Generic;
using System.Linq;
using ScreepsDotNet.API.Arena;
using BodyPartType = ScreepsDotNet.API.World.BodyPartType;
using Goal = ScreepsDotNet.API.World.Goal;
using IGame = ScreepsDotNet.API.World.IGame;
using ISource = ScreepsDotNet.API.World.ISource;
using SearchPathOptions = ScreepsDotNet.API.World.SearchPathOptions;

namespace Bot;

public static class Calculations
{
    private static readonly IGame Game = Inject<IGame>();
    
    public record HaulerRequirements(
        int HarvesterEnergyPerTick,
        int HaulerEnergyCarryCapacity,
        RoomPosition[] DropPositions
    );

    public static int CalculateRequiredHaulers(IList<ISource> sources, HaulerRequirements requirements, BodyType<BodyPartType> haulerBody)
    {
        var totalHarvestRate = sources.Count * requirements.HarvesterEnergyPerTick;
        var roundTripCost = CalculateRoundTripCost(sources, requirements.DropPositions);
        var avgRoundTripCost = (float)roundTripCost / sources.Count;
        
        var averageSpeed = CalculateHaulerAverageSpeed(haulerBody, avgRoundTripCost);
        var tripDuration = avgRoundTripCost / averageSpeed;
        
        var energyPerHaulerPerTick = requirements.HaulerEnergyCarryCapacity / tripDuration;
        return (int)Math.Ceiling(totalHarvestRate / energyPerHaulerPerTick);
    }

    public static int CalculateRoundTripCost(IList<ISource> sources, RoomPosition[] destinations)
    {
        var pathFinder = Game.PathFinder;
        var goals = destinations.Select(dest => new Goal(dest, 1)).ToArray();
        var totalCost = sources.Sum(source => 
            pathFinder.Search(source.RoomPosition, goals, new SearchPathOptions()).Cost);
        return totalCost * 2; // Multiply by 2 for round trip
    }

    public static int CalculateHarvesterEnergyPerTick(BodyType<BodyPartType> body, int energyCapacity = 3000, int energyRegenerationTime = 300)
    {
        var workParts = body[BodyPartType.Work];
        var harvestPower = Game.Constants.Creep.HarvestPower;
        var maxSourceEnergy = energyCapacity / (float) energyRegenerationTime;
        return (int) Math.Min(workParts * harvestPower, maxSourceEnergy);
    }

    public static float CalculateHaulerAverageSpeed(BodyType<BodyPartType> body, float averageRoundTripCostInTicks)
    {
        var moveParts = body[BodyPartType.Move];
        var carryParts = body[BodyPartType.Carry];
        var emptySpeed = CalculateCreepSpeed(moveParts, 0, averageRoundTripCostInTicks);
        var fullSpeed = CalculateCreepSpeed(moveParts, carryParts, averageRoundTripCostInTicks);
        return (emptySpeed + fullSpeed) / 2;
    }

    private static float CalculateCreepSpeed(int moveParts, int filledCarryParts, float averageRoundTripCostInTicks)
    {
        var totalFatigue = filledCarryParts * 2 + averageRoundTripCostInTicks;
        float fatigueReduction = moveParts * 2;
        return Math.Min(1, fatigueReduction / totalFatigue);
    }
}
