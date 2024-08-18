using System.Linq;

namespace Bot;

public static class BootstrapHarvester
{
    private static IGame _game = Inject<IGame>();
    
    private class BootstrapHarvesterState
    {
        public bool IsHarvesting { get; init;}
    }
    
    public static void ExecuteHarvester(IBootstrapHarvester harvester)
    {
        switch (harvester.GetUserData<BootstrapHarvesterState>()?.IsHarvesting)
        {
            case true: Harvest(harvester);
                break;
            case false: Deposit(harvester);
                break;
            case null: Init(harvester);
                break;
        }
    }

    private static void Init(IBootstrapHarvester harvester)
    {
        var closestSource = harvester.GetUnreservedSource();
        if (closestSource is null) return;
        harvester.SetSource(closestSource);
        harvester.SetUserData(new BootstrapHarvesterState { IsHarvesting = true });
        closestSource.SetSourceReservation(new SourceExtensions.SourceReservation()
        {
            Creep = harvester
        });
    }

    private static void Deposit(IBootstrapHarvester harvester)
    {
        var depositTarget = harvester.Room?.Find<IStructureSpawn>().FirstOrDefault();
        if (depositTarget is null) return;

        if (harvester.Transfer(depositTarget, ResourceType.Energy) == CreepTransferResult.NotInRange)
        {
            harvester.MoveTo(depositTarget.RoomPosition);
        }
    }

    private static void Harvest(IBootstrapHarvester harvester)
    {
        var source = harvester.TargetSource;
        if (source is null)
        {
            harvester.SetUserData<BootstrapHarvesterState>(null /* reinit */);
            return;
        }
        
        if (harvester.Harvest(source) == CreepHarvestResult.NotInRange)
        {
            harvester.MoveTo(source.RoomPosition);
        }

        if (harvester.Store.GetFreeCapacity(ResourceType.Energy) == 0)
        {
            harvester.SetUserData(new BootstrapHarvesterState { IsHarvesting = false });
        }
    }
}