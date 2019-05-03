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
	using InternalTexts = SortedDictionary<string, string>;
	using Records = List<Record>;
	public enum RecordType { RT_ALL, RT_BestTexts, RT_WorstTexts };
	
	[Serializable]
	public class Texts : IEnumerable<TextEntry>
	{
		InternalTexts presetTexts = new InternalTexts();
		UserData userData = new UserData();
		readonly string path;

		//public Records Records => userData.Records;
		public TextEntry Current { get; set; }
	
		public Texts(string dir) 
		{
			if (dir != null)
				path = Path.Combine(dir, "texts");
			//save();
			if (File.Exists(path))
				load(path);
		}

		void load(string loadPath)
		{
			if (string.IsNullOrEmpty(loadPath))
				return;
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
			if (string.IsNullOrEmpty(savePath))
				return;
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

		public Record[] getRecords(RecordType type, int count = 0)
		{
			Record[] records = new Record[userData.Records.Count];
			userData.Records.CopyTo(records);
			if (type == RecordType.RT_WorstTexts)
				Array.Sort(records, Record.reverseSort);
			else
				Array.Sort(records);

			if (type != RecordType.RT_ALL)
			{
				var dict = new Dictionary<string, Record>();
				foreach (var rec in records)
				{
					//Add record to dictionary if it doesn't already contain a record with this text title or if its recorD with this text title has a lower wpm
					if (!dict.ContainsKey(rec.TextTitle) || dict[rec.TextTitle].WPM < rec.WPM)
						dict[rec.TextTitle] = rec;
				}
				records = new Record[dict.Count];
				int i = 0;
				foreach (var rec in dict)
					records[i++] = rec.Value;
				if (type == RecordType.RT_WorstTexts)
					Array.Sort(records, Record.reverseSort);
				else
					Array.Sort(records);
			}
			var subArray = records;
			if (count > 0)
			{
				int actualCount = Math.Min(count, records.Length); //There may be fewer available items than requested. In that case return all availabLe items.
				subArray = new Record[actualCount];
				for (int i = 0; i < actualCount; i++)
					subArray[i] = records[i];
			}
			return subArray;
		}

		public void removeCurrent()
		{
			remove(Current.Title);
		}
	}

	[Serializable]
	public class UserData : ISerializable
	{
		public InternalTexts Texts { get; set; }
		public Records Records { get; set; }
		static readonly public Type[] SerializeTypes = new Type[] { typeof(InternalTexts), typeof(Records) };

		public UserData()
		{
			Texts = new InternalTexts();
			Records = new Records();
		}

		public UserData(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "texts")
					Texts = (InternalTexts)entry.Value;
				else if (entry.Name == "records")
					Records = (Records)entry.Value;
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("texts", Texts);
			info.AddValue("records", Records);
		}
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