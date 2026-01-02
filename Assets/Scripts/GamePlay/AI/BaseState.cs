
namespace GamePlay.AI
{
	public abstract class BaseState : IState
	{
		protected AIController Controller { get; private set; }
		protected float StateTime { get; private set; }

		public void SetController(AIController controller)
		{
			Controller = controller;
		}

		public virtual void Enter()
		{
			StateTime = 0;
		}

		public virtual void Execute(float deltaTime)
		{
			StateTime += deltaTime;
		}

		public virtual void Exit() { }

		public virtual bool CanTransitionTo(IState newState)
		{
			return true;
		}
	}
}