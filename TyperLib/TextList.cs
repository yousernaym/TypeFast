using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace TyperLib
{
	using System.Collections;
	using ListType = Dictionary<string, string>;

	[Serializable]
	public class TextList : IEnumerable<TextEntry>
	{
		ListType presetList = new ListType();
		ListType userList = new ListType();
		readonly string path;

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
			userList.Remove(title);
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
				yield return new TextEntry { Title = entry.Key, Text = entry.Value };
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		void resetFactoryTexts()
		{
			userList = new ListType();
			foreach (var text in presetList)
				userList.Add(text.Key, text.Value);
		}

		public string getText(string titLe)
		{
			return userList[titLe];
		}

		
	}

	public class TextEntry
	{
		public string Title;
		public string Text;
	}

}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";