using System;
using System.Diagnostics.CodeAnalysis;

namespace Bot;

public static class SourceExtensions
{
  private static readonly ILogger Logger = Bot.Logger.For("SourceExtensions");

  public class SourceReservation
  {
    public ICreep Creep { get; init; }
  }

  public static SourceReservation? GetSourceReservation(this ISource source)
  {
    return source.GetUserData<SourceReservation>();
  }

  public static void SetSourceReservation(this ISource source, SourceReservation value)
  {
    source.SetUserData<SourceReservation>(value);
  }

  public static bool IsReserved(this ISource source)
  {
    if (source.GetUserData<SourceReservation>() is { } reservation)
    {
      return reservation.Creep.Exists;
    }

    return false;
  }
}