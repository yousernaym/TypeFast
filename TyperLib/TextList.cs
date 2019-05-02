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

	[Serializable]
	public class TextList : IEnumerable<TextEntry>
	{
		InternalTextList presetTexts = new InternalTextList();
		UserData userData = new UserData();
		readonly string path;

		public SortedList<int, string> Records => userData.Records;
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
			} while (randomTitle == Current?.Title);
			select(randomTitle);
			return Current;
		}

		public void addRecord(int wpm, string title)
		{
			userData.Records.Add(wpm, title);
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
		public SortedList<int,string> Records { get; set; }
		static readonly public Type[] SerializeTypes = new Type[] { typeof(InternalTextList), typeof(SortedList<int, string>) };

	public UserData()
		{
			Texts = new InternalTextList();
			Records = new SortedList<int, string>();
		}

		public UserData(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "texts")
					Texts = (InternalTextList)entry.Value;
				else if (entry.Name == "records")
					Records = (SortedList<int, string>)entry.Value;
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