using System;

namespace Bot;

public class Logger : ILogger
{

  private Logger()
  {
  }

  private string Name { get; init; } = "Global";
  private readonly IGame _game = Inject<IGame>();

  public void Error(string message)
  {
    Console.WriteLine($"[{_game.Time}] [{Name}] [ERROR] {message}");
    _game.Notify($"[{Name}] [ERROR] {message}", groupInterval: 1);
  }

  public void Warn(string message)
  {
    Console.WriteLine($"[{_game.Time}] [{Name}] [WARN] {message}");
    _game.Notify($"[{Name}] [WARN] {message}", groupInterval: 5);
  }

  public void Info(string message)
  {
    Console.WriteLine($"[{_game.Time}] [{Name}] [INFO] {message}");
  }

  public static ILogger For(string name)
  {
    return new Logger { Name = name };
  }

  public static ILogger For(Type type)
  {
    return new Logger { Name = type.Name };
  }
}
