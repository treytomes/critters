namespace Critters;

struct GameTime
{
  public readonly TimeSpan TotalTime;
  public readonly TimeSpan ElapsedTime;
  public readonly int TotalFrames;

  public GameTime(TimeSpan totalTime, TimeSpan elapsedTime, int totalFrames = 0)
  {
    TotalTime = totalTime;
    ElapsedTime = elapsedTime;
    TotalFrames = totalFrames;
  }

  public float FramesPerSecond => (float)(TotalFrames / TotalTime.TotalSeconds);

  public GameTime Add(TimeSpan elapsedTime)
  {
    return new GameTime(TotalTime + elapsedTime, elapsedTime, TotalFrames + 1);
  }

  public GameTime Add(double elapsedSeconds)
  {
    return Add(TimeSpan.FromSeconds(elapsedSeconds));
  }
}
