using System;
using System.Runtime.Serialization;

namespace TyperUWP
{
	[Serializable]
	internal class Settings : ISerializable
	{
		public int FontSize { get; set; }
		public string FontFamily { get; set; }
		public Brush 
		public Settings(SerializationInfo info, StreamingContext context)
		{

		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}
	}
}