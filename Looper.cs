
namespace ScreepsDotNet;

public class Looper
{
  private readonly IGame game = Inject<IGame>();
  private readonly IBot bot = Inject<IBot>();

  public void Tick()
  {
    game.Tick();
    bot.Loop();
  }
}