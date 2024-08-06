namespace Bot;

public static class CreepExtensions
{
  private static readonly ILogger logger = Logger.For("CreepExtensions");

  public static CreepRole GetCreepRole(this ICreep creep)
  {
    var inMemoryRole = creep.GetUserData<CreepConfiguration>()?.Role;
    if (inMemoryRole != null)
    {
      return inMemoryRole.Value;
    }

    if (!creep.Memory.TryGetInt("role", out var role))
    {
      logger.Error($"Creep {creep.Name} has no role.");
      throw new TerminateCreepException(creep, "Creep has no role.");
    }

    return (CreepRole)role;
  }

  public static ISource? GetSource<T>(this T creep) where T : ICreep, IWithTargetSource
  {
    var source = creep.GetUserData<ISource>();
    if (source != null)
    {
      return source;
    }

    creep.Memory.TryGetString("source", out var sourceId);
    if (sourceId == null)
    {
      return null;
    }

    var game = Inject<IGame>();
    return game.GetObjectById<ISource>(sourceId);
  }
}