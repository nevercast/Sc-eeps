using System.Linq;

namespace Bot;

public static class Harvester
{
    private static readonly ILogger Logger = Bot.Logger.For(typeof(Harvester));
    
    public static void ExecuteHarvester(ICreep harvester)
    {
        Harvest(harvester);
    }
    
    private static void Harvest(ICreep harvester)
    {
        Logger.Info("Executing Harvest");
        var source = harvester.GetSource();
        if (source is null)
        {
            Logger.Info("Reserving a source");
            var closestSource = harvester.Room?.GetUnreservedSource();
            if (closestSource is null) return;
            harvester.SetSource(closestSource);
            closestSource.SetSourceReservation(new SourceExtensions.SourceReservation(harvester));
            source = closestSource;
        }
        
        if (harvester.Harvest(source) == CreepHarvestResult.NotInRange)
        {
            harvester.MoveTo(source.RoomPosition);
        }
    }
}