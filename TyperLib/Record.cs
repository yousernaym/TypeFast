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
		public float Accuracy { get; set; }

		public Record(int wpm, float accuracy, string textTitle)
		{
			WPM = wpm;
			Accuracy = accuracy;
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
				else if (entry.Name == "accuracy")
					Accuracy = (float)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("wpm", WPM);
			info.AddValue("textTitle", TextTitle);
			info.AddValue("accuracy", Accuracy);
		}

		public static int reverseSort(Record x, Record y)
		{
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
