namespace Bot;

/// <summary>
/// Logs console messages in Screeps.
/// </summary>
public interface ILogger {
  void Info(string message);
  void Warn(string message);
  void Error(string message);
}