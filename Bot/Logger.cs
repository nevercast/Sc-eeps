using System;

namespace Bot;

public class Logger : ILogger
{
  protected string Name { get; private set; } = "Global";

  private readonly IGame game = Inject<IGame>();

  public void Error(string message)
  {
    Console.WriteLine($"[{game.Time}] [{Name}] [ERROR] {message}");
    game.Notify($"[{Name}] [ERROR] {message}", groupInterval: 1);
  }

  public void Warn(string message)
  {
    Console.WriteLine($"[{game.Time}] [{Name}] [WARN] {message}");
    game.Notify($"[{Name}] [WARN] {message}", groupInterval: 5);
  }

  public void Info(string message)
  {
    Console.WriteLine($"[{game.Time}] [{Name}] [INFO] {message}");
  }

  public static ILogger For(string name)
  {
    return new Logger { Name = name };
  }
}
