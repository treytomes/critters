namespace Critters.AI;

interface ICloneable<T> : ICloneable
{
	new T Clone();
}