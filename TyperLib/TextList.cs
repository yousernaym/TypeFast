using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TyperLib
{
	using InternalTextList = SortedDictionary<string, string>;
	using RecordList = List<Record>;

	[Serializable]
	public class TextList : IEnumerable<TextEntry>
	{
		InternalTextList presetTexts = new InternalTextList();
		UserData userData = new UserData();
		readonly string path;

		public RecordList Records => userData.Records;
		public TextEntry Current { get; set; }
	
		public TextList(string dir) 
		{
			path = Path.Combine(dir, "textList");
			//save();
			if (File.Exists(path))
				load(path);
		}

		void load(string loadPath)
		{
			using (var stream = File.Open(loadPath, FileMode.Open))
			{
				var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
				userData = (UserData)dcs.ReadObject(stream);
			}
		}

		void save(string savePath = null)
		{
			if (savePath == null)
				savePath = path;
			string tempPath = savePath + "_";
			try
			{
				using (var stream = File.Open(tempPath, FileMode.Create))
				{
					var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
					dcs.WriteObject(stream, userData);
				}
			}
			catch
			{
				File.Delete(tempPath);
				throw;
			}
			File.Delete(savePath);
			File.Move(tempPath, savePath);
		}

		public void add(string title, string text)
		{
			if (userData.Texts.ContainsKey(title))
				throw new ArgumentException("There already exists a text with the specified title.");
			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentException("Title can't be empty.");
			userData.Texts.Add(title, text);
			save();
		}

		public void remove(string title)
		{
			var indeXable = userData.Texts.Keys.ToList();
			int currentIndex = indeXable.FindIndex(k => k == title);
			userData.Texts.Remove(title);
			if (title == Current.Title)
			{
				if (currentIndex >= userData.Texts.Count)
					currentIndex--;
				Current = new TextEntry(userData.Texts.ElementAt(currentIndex));
			}
			save();
		}

		public bool containsTitle(string title)
		{
			return userData.Texts.ContainsKey(title);
		}

		public IEnumerator<TextEntry> GetEnumerator()
		{
			//return ((IEnumerable<KeyValuePair<string, string>>)userList).GetEnumerator();
			foreach (var entry in userData.Texts)
				yield return new TextEntry(entry);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void resetFactoryTexts()
		{
			//userList = new ListType();
			foreach (var text in presetTexts)
				userData.Texts.Add(text.Key, text.Value);
		}

		public string select(string title)
		{
			Current = new TextEntry(title, userData.Texts[title]);
			return Current.Text;
		}

		public TextEntry selectRandom()
		{
			if (userData.Texts.Count == 0)
				return null;
			string randomTitle;
			do
			{
				randomTitle = userData.Texts.ElementAt(new Random().Next(userData.Texts.Count)).Key;
			} while (randomTitle == Current?.Title && userData.Texts.Count > 1);
			select(randomTitle);
			return Current;
		}

		public void addRecord(int wpm, string title)
		{
			userData.Records.Add(new Record(wpm, title));
			save();
		}

		public void removeCurrent()
		{
			remove(Current.Title);
		}
	}

	[Serializable]
	public class UserData : ISerializable
	{
		public InternalTextList Texts { get; set; }
		public RecordList Records { get; set; }
		static readonly public Type[] SerializeTypes = new Type[] { typeof(InternalTextList), typeof(RecordList) };

		public UserData()
		{
			Texts = new InternalTextList();
			Records = new RecordList();
		}

		public UserData(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "texts")
					Texts = (InternalTextList)entry.Value;
				else if (entry.Name == "records")
					Records = (RecordList)entry.Value;
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("texts", Texts);
			info.AddValue("records", Records);
		}
	}

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

	public class TextEntry
	{
		public string Title { get; set; }
		public string Text { get; set; }
		public TextEntry(string title, string text)
		{
			Title = title;
			Text = text;
		}
		public TextEntry(KeyValuePair<string, string> kvp)
		{
			Title = kvp.Key;
			Text = kvp.Value;
		}
	}

	//public class Records
	//{
	//	SortedList<int, string> records;
	//	public void add(int wpm, string textTitle)
	//	{
	//		records.Add(wpm, textTitle);
	//	}
	//}
}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";