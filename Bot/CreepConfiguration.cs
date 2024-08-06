namespace Bot;

/// <summary>
/// Data attached to a creep that describes its purpose.
/// </summary>
public class CreepConfiguration
{
  public CreepRole Role { get; set; }
  public bool? IsIdle { get; set; }
  // TODO: targets?
}