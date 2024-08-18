
namespace ScreepsDotNet;

public class Looper
{
  private readonly IGame _game = Inject<IGame>();
  private readonly IBot _bot = Inject<IBot>();

  public void Tick()
  {
    _game.Tick();
    _bot.Loop();
  }
}