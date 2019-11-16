using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TyperLib
{
	public enum RecordElem { Wpm, Time, Acc, MinWpm, MaxWpm };
	[Serializable]
	public class Record : ISerializable, IComparable, IEquatable<Record>
	{
		public static RecordElem PrimarySort = RecordElem.Wpm;
		public int Wpm { get; set; }
		public TimeSpan Time { get; set; }
		float accuracy;
		public float Accuracy
		{
			get => accuracy;
			set
			{
				accuracy = (float)Math.Round(value, 1);
			}
		}
		public int MinWpm { get; set; }
		public int MaxWpm { get; set; }

		public string TextTitle { get; set; }
		public bool IsTextFinished { get; set; }
		public int CharCount { get; set; }
		public int Id { get; private set; }

		public Record(int wpm, float accuracy, TimeSpan time, string textTitle, bool isTextFinished, int charCount)
		{
			Wpm = wpm;
			Accuracy = accuracy;
			Time = time;
			TextTitle = textTitle;
			IsTextFinished = isTextFinished;
			CharCount = charCount;
			Id = new Random().Next();
		}

		public Record(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "wpm")
					Wpm = (int)entry.Value;
				else if (entry.Name == "accuracy")
					Accuracy = (float)entry.Value;
				else if (entry.Name == "time")
					Time = (TimeSpan)entry.Value;
				else if (entry.Name == "textTitle")
					TextTitle = (string)entry.Value;
				else if (entry.Name == "isTextFinished")
					IsTextFinished = (bool)entry.Value;
				else if (entry.Name == "charCount")
					CharCount = (int)entry.Value;
				else if (entry.Name == "id")
					Id = (int)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("wpm", Wpm);
			info.AddValue("time", Time);
			info.AddValue("accuracy", Accuracy);
			info.AddValue("textTitle", TextTitle);
			info.AddValue("isTextFinished", IsTextFinished);
			info.AddValue("charCount", CharCount);
			info.AddValue("id", Id);
		}

		public static int reverseSort(Record x, Record y)
		{
			return x.CompareTo(y) * -1;
		}

		public int CompareTo(object obj)
		{
			Record rec = (Record)obj;
			int compResult = 0;
			
			//Start with the primary sort type...	
			if (PrimarySort == RecordElem.Wpm)
				compResult = Wpm.CompareTo(rec.Wpm);
			else if (PrimarySort == RecordElem.Time)
				compResult = Time.CompareTo(rec.Time);
			else if (PrimarySort == RecordElem.Acc)
				compResult = Accuracy.CompareTo(rec.Accuracy);
			else if (PrimarySort == RecordElem.MinWpm)
				compResult = MinWpm.CompareTo(rec.MinWpm);
			else if (PrimarySort == RecordElem.MaxWpm)
				compResult = MaxWpm.CompareTo(rec.MaxWpm);

			//...then go through all of them in case of equality
			if (compResult == 0)
			{
				compResult = Wpm.CompareTo(rec.Wpm);
				if (compResult == 0) //Same wpm, compare time
				{
					var reverse = IsTextFinished && rec.IsTextFinished && CharCount == rec.CharCount ? -1 : 1; //If both records finished typing the whole text and both texts have the same length, shorter times are better, otherwise worse
					var roundedTime = Math.Round(Time.TotalSeconds, Time.TotalSeconds < 10 ? 2 : 0);

					var roundedTime2 = Math.Round(rec.Time.TotalSeconds, rec.Time.TotalSeconds < 10 ? 2 : 0);
					compResult = roundedTime.CompareTo(roundedTime2) * reverse;
					if (compResult == 0) //Same time, compare accuracy
					{
						compResult = Accuracy.CompareTo(rec.Accuracy);
						if (compResult == 0)
						{
							compResult = MinWpm.CompareTo(rec.MinWpm);
							if (compResult == 0)
								return MaxWpm.CompareTo(rec.MaxWpm);
							else
								return compResult;
						}
						else
							return compResult;
					}
					else 
						return compResult;
				}
				else
					return compResult;
			}
			else 
				return compResult;
		}

		public bool Equals(Record other)
		{
			return other != null && Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as Record);
		}

		public override int GetHashCode()
		{
			return Id;
		}

		static public bool operator<(Record rec1, Record rec2)
		{
			return rec1.CompareTo(rec2) == 1;
		}

		static public bool operator >(Record rec1, Record rec2)
		{
			return rec1.CompareTo(rec2) == -1;
		}

		static public bool operator <=(Record rec1, Record rec2)
		{
			return rec1.CompareTo(rec2) >= 0;
		}

		static public bool operator >=(Record rec1, Record rec2)
		{
			return rec1.CompareTo(rec2) <= 0 ;
		}


	}
}
