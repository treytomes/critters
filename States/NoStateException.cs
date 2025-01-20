namespace Critters.States;

class NoStateException : Exception
{
  public NoStateException()
    : base("There is no state.")
  {
  }
}
