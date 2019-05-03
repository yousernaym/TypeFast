using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TyperLib
{
	[Serializable]
	public class Record : ISerializable, IComparable
	{
		public int WPM { get; set; }
		public string TextTitle { get; set; }

		public Record(int wpm, string textTitle)
		{
			WPM = wpm;
			TextTitle = textTitle;
		}

		public Record(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "wpm")
					WPM = (int)entry.Value;
				else if (entry.Name == "textTitle")
					TextTitle = (string)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("wpm", WPM);
			info.AddValue("textTitle", TextTitle);
		}

		public static int reverseSort(Record x, Record y)
		{
			//return new ReverseComparer();
			return x.CompareTo(y) * -1;
		}

		public int CompareTo(object obj)
		{
			int wpm = ((Record)obj).WPM;
			if (WPM < wpm)
				return 1;
			else if (WPM > wpm)
				return -1;
			else
				return 0;
		}

		//class ReverseComparer : IComparer
		//{
		//	public int Compare(object x, object y)
		//	{
		//		return ((Record)x).CompareTo((Record)y) * -1;
		//	}
		//}
	}
}
