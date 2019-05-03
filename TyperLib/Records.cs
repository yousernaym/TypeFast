//using System.Collections.Generic;
//using System.Runtime.Serialization;

//namespace TyperLib
//{
//	public class Records : ISerializable
//	{
//		List<Record> records;

//		public Records()
//		{
//			this.records = new List<Record>();
//		}

//		public Records(SerializationInfo info, StreamingContext context)
//		{
//			foreach (var entry in info)
//			{
//				if (entry.Name == "records")
//					records = (List<Record>)entry.Value;
//			}
//		}

//		public void GetObjectData(SerializationInfo info, StreamingContext context)
//		{
//			info.AddValue("records", records);
//		}

//		internal void add(int wpm, string textTitle)
//		{
//			records.Add(new Record(wpm, textTitle));
//		}

//		public Records[] getTop(bool uniqueTexts, int count = 0)
//		{

//		}

//		public Records[] getBottom(bool uniqueTexts, int count = 0)
//		{

//		}
//	}
//}