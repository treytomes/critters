namespace Critters.States;

/// <summary>  
/// Represents the type of state transition that occurred.  
/// </summary>  
public enum TransitionType
{
	/// <summary>  
	/// A new state was entered.  
	/// </summary>  
	Enter,

	/// <summary>  
	/// A state was left.  
	/// </summary>  
	Leave,

	/// <summary>  
	/// All states were cleared.  
	/// </summary>  
	ClearAll
}
