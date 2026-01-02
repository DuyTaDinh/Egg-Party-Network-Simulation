using System.Collections.Generic;
namespace GamePlay.UserInput
{
	public class CommandBuffer
	{
		private readonly Queue<ICommand> pendingCommands = new Queue<ICommand>();
		private readonly Stack<ICommand> executedCommands = new Stack<ICommand>();
		private readonly int maxHistory;

		public CommandBuffer(int maxHistorySize = 100)
		{
			maxHistory = maxHistorySize;
		}

		public void AddCommand(ICommand command)
		{
			pendingCommands.Enqueue(command);
		}

		public void ExecuteAll()
		{
			while (pendingCommands.Count > 0)
			{
				var command = pendingCommands.Dequeue();
				command.Execute();
                
				executedCommands.Push(command);
                
				while (executedCommands.Count > maxHistory)
				{
					var temp = new Stack<ICommand>();
					while (executedCommands.Count > 1)
					{
						temp.Push(executedCommands.Pop());
					}
					executedCommands.Pop();
					while (temp.Count > 0)
					{
						executedCommands.Push(temp.Pop());
					}
				}
			}
		}

		public bool TryUndo()
		{
			if (executedCommands.Count == 0) return false;
            
			var command = executedCommands.Pop();
			command.Undo();
			return true;
		}

		public void Clear()
		{
			pendingCommands.Clear();
			executedCommands.Clear();
		}

		public int PendingCount => pendingCommands.Count;
		public int HistoryCount => executedCommands.Count;
	}
}