using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TyperLib
{
	[Serializable]
	public class Record : ISerializable, IComparable
	{
		public int WPM { get; set; }
		public float Accuracy { get; set; }
		public TimeSpan Time { get; set; }
		public string TextTitle { get; set; }
		public bool IsTextFinished { get; set; }
		public int CharCount { get; set; }
		
		public Record(int wpm, float accuracy, TimeSpan time, string textTitle, bool isTextFinished, int charCount)
		{
			WPM = wpm;
			Accuracy = accuracy;
			Time = time;
			TextTitle = textTitle;
			IsTextFinished = isTextFinished;
			CharCount = charCount;
		}

		public Record(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "wpm")
					WPM = (int)entry.Value;
				else if (entry.Name == "accuracy")
					Accuracy = (float)entry.Value;
				else if (entry.Name == "time")
					Time = (TimeSpan)entry.Value;
				else if (entry.Name == "textTitle")
					TextTitle = (string)entry.Value;
				else if (entry.Name == "isTextFinished")
					IsTextFinished = (bool)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("wpm", WPM);
			info.AddValue("time", Time);
			info.AddValue("accuracy", Accuracy);
			info.AddValue("textTitle", TextTitle);
			info.AddValue("isTextFinished", IsTextFinished);
		}

		public static int reverseSort(Record x, Record y)
		{
			return x.CompareTo(y) * -1;
		}

		public int CompareTo(object obj)
		{
			Record rec = (Record)obj;
			if (WPM < rec.WPM)
				return 1;
			else if (WPM > rec.WPM)
				return -1;
			else //Same WPM, compare accuracy
			{
				if (Accuracy < rec.Accuracy)
					return 1;
				else if (Accuracy > rec.Accuracy)
					return -1;
				else //Same accuracy, compare time
				{
					var reverse = IsTextFinished && rec.IsTextFinished && CharCount == rec.CharCount ? -1 : 1; //If both records finished typing the whole text and both texts have the same length, shorter times are better, otherwise worse
					if (Time < rec.Time)
						return 1 * reverse;
					else if (Time > rec.Time)
						return -1 * reverse;
					return 0;
				}
			}
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
