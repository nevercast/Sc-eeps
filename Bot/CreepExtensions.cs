using System;
using System.Diagnostics.CodeAnalysis;

namespace Bot;

public static class CreepExtensions
{
  private static readonly ILogger Logger = Bot.Logger.For("CreepExtensions");

  public static CreepRole GetCreepRole(this ICreep creep)
  {
    var inMemoryRole = creep.GetUserData<CreepConfiguration>()?.Role;
    if (inMemoryRole != null)
    {
      return inMemoryRole.Value;
    }

    if (creep.Memory.TryGetInt("role", out var role)) return (CreepRole)role;
    Logger.Error($"Creep {creep.Name} has no role.");
    throw new TerminateCreepException(creep, "Creep has no role.");
  }
  
  // ReSharper disable once MemberCanBePrivate.Global
  [return: NotNullIfNotNull("defaultValue")]
  public static string? TryGetString(this IMemoryObject memory, string key, string? defaultValue = null)
  {
    return memory.TryGetString(key, out var value) ? value : defaultValue;
  }

  public static ISource? GetSource<T>(this T creep) where T : ICreep, IWithTargetSource
  {
    var source = creep.GetUserData<ISource>();
    if (source != null)
    {
      return source;
    }

    var sourceId = creep.Memory.TryGetString("source");
    if (sourceId == null)
    {
      return null;
    }

    var game = Inject<IGame>();
    return game.GetObjectById<ISource>(sourceId);
  }
  
  // public static RoomObj
}