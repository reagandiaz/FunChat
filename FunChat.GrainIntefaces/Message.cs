using System;

namespace FunChat.GrainIntefaces
{
	[Serializable]
	public class Message
	{
		public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
		public string Author { get; set; }
		public string Text { get; set; }
		public string Channel { get; set; }

		public Message()
		{
		}

		public Message(string author, string msg)
		{
			Author = author;
			Text = msg;
		}
	}
}