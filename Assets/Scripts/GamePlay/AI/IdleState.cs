
namespace GamePlay.AI
{
	public class IdleState : BaseState
	{
		private float idleDuration;
		private float minIdleTime = 0.5f;
		private float maxIdleTime = 2f;

		public override void Enter()
		{
			base.Enter();
			idleDuration = UnityEngine.Random.Range(minIdleTime, maxIdleTime);
		}

		public override void Execute(float deltaTime)
		{
			base.Execute(deltaTime);

			if (Controller.HasTargetEgg())
			{
				Controller.ChangeState(new ChaseState());
				return;
			}

			if (StateTime >= idleDuration)
			{
				Controller.ChangeState(new WanderState());
			}
		}
	}
}