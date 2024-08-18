using System.Linq;

namespace Bot;

public static class RoomExtensions
{
    private static readonly ILogger Logger = Bot.Logger.For(typeof(RoomExtensions));
    
    public static ISource? GetUnreservedSource(this IRoom room)
    {
        Logger.Info("GetUnreservedSource");
        Logger.Info($"GetUnreservedSource in {room.Name}");
        var result = room.Find<ISource>().FirstOrDefault(source => !source.IsReserved());
        Logger.Info($"GetUnreservedSource resolved to {result?.RoomPosition}");
        return result;
    }
}