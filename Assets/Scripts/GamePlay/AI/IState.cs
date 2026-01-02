namespace GamePlay.AI
{
	public interface IState
	{
		void Enter();
		void Execute(float deltaTime);
		void Exit();
		bool CanTransitionTo(IState newState);
	}
}