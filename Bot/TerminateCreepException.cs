using System;

namespace Bot;

/// <summary>
/// Exception thrown when a creep should be terminated.
/// Usually occurs when the creeps configuration is invalid.
/// </summary>
public class TerminateCreepException(ICreep creep, string message) : Exception(message)
{
  public ICreep Creep { get; private set; } = creep;
}