namespace Critters.Core;

/// <summary>  
/// Provides data for disposal events.  
/// </summary>  
public class DisposalEventArgs : EventArgs
{
	/// <summary>  
	/// Gets the stage of disposal.  
	/// </summary>  
	public DisposalStage Stage { get; }

	/// <summary>  
	/// Gets the object being disposed.  
	/// </summary>  
	public object Sender { get; }

	/// <summary>  
	/// Initializes a new instance of the <see cref="DisposalEventArgs"/> class.  
	/// </summary>  
	/// <param name="stage">The stage of disposal.</param>  
	/// <param name="sender">The object being disposed.</param>  
	public DisposalEventArgs(DisposalStage stage, object sender)
	{
		Stage = stage;
		Sender = sender;
	}
}