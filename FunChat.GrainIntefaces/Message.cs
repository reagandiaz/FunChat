using System;

namespace FunChat.GrainIntefaces
{
	[Serializable]
	public class Message
	{
		public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
		public string Author { get; set; }
		public string Text { get; set; }

		public Message()
		{
		}

		public Message(string msg)
		{
			Text = msg;
		}
	}
}