namespace GamePlay.UserInput
{
	public interface ICommand
	{
		void Execute();
		void Undo();
		float Timestamp { get; }
	}
}