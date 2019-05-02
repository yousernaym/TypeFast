using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TyperLib
{
	using System.Collections;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using ListType = Dictionary<string, string>;

	[Serializable]
	public class TextList : IEnumerable<TextEntry>
	{
		ListType presetList = new ListType();
		ListType userList = new ListType();
		readonly string path;

		public TextEntry Current { get; set; }
		
		public TextList(string dir) 
		{
			path = Path.Combine(dir, "textList");
			//save();
			if (File.Exists(path))
				userList = load(path);
		}

		ListType load(string loadPath)
		{
			using (var stream = File.Open(loadPath, FileMode.Open))
			{
				var dcs = new DataContractSerializer(typeof(ListType));
				return (ListType)dcs.ReadObject(stream);
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
					var dcs = new DataContractSerializer(typeof(ListType));
					dcs.WriteObject(stream, userList);
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
			if (userList.ContainsKey(title))
				throw new ArgumentException("There already exists a text with the specified title.");
			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentException("Title can't be empty.");
			userList.Add(title, text);
			save();
		}

		public void remove(string title)
		{
			var indeXable = userList.Keys.ToList();
			int currentIndex = indeXable.FindIndex(k => k == title);
			userList.Remove(title);
			if (title == Current.Title)
			{
				if (currentIndex >= userList.Count)
					currentIndex--;
				Current = new TextEntry(userList.ElementAt(currentIndex));
			}
			save();
		}

		public bool containsTitle(string title)
		{
			return userList.ContainsKey(title);
		}

		public IEnumerator<TextEntry> GetEnumerator()
		{
			//return ((IEnumerable<KeyValuePair<string, string>>)userList).GetEnumerator();
			foreach (var entry in userList)
				yield return new TextEntry(entry);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		void resetFactoryTexts()
		{
			//userList = new ListType();
			foreach (var text in presetList)
				userList.Add(text.Key, text.Value);
		}

		public string select(string title)
		{
			Current = new TextEntry(title, userList[title]);
			return Current.Text;
		}

		public TextEntry selectRandom()
		{
			string randomTitle;
			do
			{
				randomTitle = userList.ElementAt(new Random().Next(userList.Count)).Key;
			} while (randomTitle == Current?.Title);
			select(randomTitle);
			return Current;
		}

		public void removeCurrent()
		{
			remove(Current.Title);
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

}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";