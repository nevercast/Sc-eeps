namespace Bot;

public enum CreepRole {
    /// <summary>
    /// A creep that is responsible for harvesting energy from sources in early game.
    /// </summary>
    BootstrapHarvester,
    /// <summary>
    /// A creep that is responsible for harvesting energy from sources.
    /// </summary>
    Harvester,
    /// <summary>
    /// A creep that is responsible for transporting energy from sources to spawn, extensions, or whichever structure is the room's elected energy sink.
    /// </summary>
    Hauler,
    /// <summary>
    /// A creep that is responsible for upgrading the controller. 🙌
    /// </summary>
    Upgrader,
    /// <summary>
    /// A creep that is responsible for building structures.
    /// </summary>
    Builder,
    /// <summary>
    /// A creep that is responsible for repairing structures.
    /// </summary>
    Repairer,
    /// <summary>
    /// A creep that is responsible for defending the room.
    /// </summary>
    Defender,
    /// <summary>
    /// A creep that is responsible for scouting.
    /// </summary>
    Scout,
    /// <summary>
    /// A creep that is responsible for claiming or reserving a room.
    /// </summary>
    Claimer,
}