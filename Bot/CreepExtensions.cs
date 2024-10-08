using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Bot;

public static class CreepExtensions
{
  private static readonly ILogger Logger = Bot.Logger.For(typeof(CreepExtensions));
  public static string CreepMemoryKeyRole = "role";
  public static string CreepMemoryKeySource = "source";

  public static CreepRole GetCreepRole(this ICreep creep)
  {
    var inMemoryRole = creep.GetUserData<CreepConfiguration>()?.Role;
    if (inMemoryRole != null)
    {
      return inMemoryRole.Value;
    }

    if (creep.Memory.TryGetInt(CreepMemoryKeyRole, out var role)) return (CreepRole)role;
    Logger.Error($"Creep {creep.Name} has no role.");
    throw new TerminateCreepException(creep, "Creep has no role.");
  }

  public static void SetCreepRole(this ICreep creep, CreepRole role)
  {
    creep.SetUserData(new CreepConfiguration()
    {
      Role = role,
      IsIdle = true
    });
    
    creep.Memory.SetValue(CreepMemoryKeyRole, (int) role);
  }
  
  // ReSharper disable once MemberCanBePrivate.Global
  [return: NotNullIfNotNull("defaultValue")]
  public static string? TryGetString(this IMemoryObject memory, string key, string? defaultValue = null)
  {
    return memory.TryGetString(key, out var value) ? value : defaultValue;
  }

  public static ISource? GetSource<T>(this T creep) where T : ICreep
  {
    var source = creep.GetUserData<ISource>();
    if (source != null)
    {
      return source;
    }

    var sourceId = creep.Memory.TryGetString(CreepMemoryKeySource);
    if (sourceId == null)
    {
      return null;
    }

    var game = Inject<IGame>();
    var obj = game.GetObjectById<ISource>(sourceId);
    if (obj == null)
    {
      return null;
    }
    SetSource(creep, obj);
    return obj;
  }
  
  public static void SetSource<T>(this T creep, ISource source) where T : ICreep
  {     
    creep.SetUserData(source);
    SetSource(creep, source.Id);
  }

  public static void SetSource<T>(this T creep, ObjectId objectId) where T : ICreep
  { 
    creep.Memory.SetValue(CreepMemoryKeySource, objectId);
  }
}