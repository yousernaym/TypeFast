using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Windows.Storage;

namespace TyperUWP
{
	using ListType = Dictionary<string, string>;

	[Serializable]
	public class TextList
	{
		readonly string localFolder = ApplicationData.Current.LocalFolder.Path;
		ListType presetList = new ListType();
		ListType userList = new ListType();
		readonly string presetPath;
		readonly string userPath;

		public TextList()
		{
			presetPath = Path.Combine(localFolder, "presetTexts");
			userPath = Path.Combine(localFolder, "userTexts");
			//save();
			if (File.Exists(userPath))
				userList = load(userPath);
		}

		ListType load(string path)
		{
			using (var stream = File.Open(path, FileMode.Open))
			{
				var dcs = new DataContractSerializer(typeof(ListType));
				return (ListType)dcs.ReadObject(stream);
			}
		}

		void save()
		{
			string tempPath = userPath + "_";
			using (var stream = File.Open(tempPath, FileMode.Create))
			{
				var dcs = new DataContractSerializer(typeof(ListType));
				var list = new ListType();
				dcs.WriteObject(stream, list);
				userList = list;
			}
			File.Delete(userPath);
			File.Move(tempPath, userPath);
		}

		//public TextList(SerializationInfo info, StreamingContext context)
		//{
		//	foreach (var entry in info)
		//	{
		//		if (entry.Name == "list")
		//			list = (Dictionary<string, string>)entry.Value;
		//	}
		//}

		//public void GetObjectData(SerializationInfo info, StreamingContext context)
		//{
		//	info.AddValue("list", userList);
		//}

		internal void add(string title, string text)
		{
			if (userList.ContainsKey(title))
				throw new ArgumentException("There already exists a text with the specified title.");
			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentException("Title can't be empty.");
			userList.Add(title, text);
			save();
		}

		internal bool containsTitle(string title)
		{
			return userList.ContainsKey(title);
		}
	}
}