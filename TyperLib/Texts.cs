﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TyperLib
{
	using Records = List<Record>;
	public enum RecordType { RT_ALL, RT_BestTexts, RT_WorstTexts };
	
	[Serializable]
	public class Texts : IEnumerable<TextEntry>
	{
		TextEntries presets = new TextEntries();
		UserData userData = new UserData();
		readonly string userDataPath;
		readonly string presetsPath;
		
		public TextEntry Current { get; set; }
		public int Count => userData.TextEntries.Count;
		public List<string> Titles
		{
			get
			{
				var titles = new List<string>();
				foreach (var text in this)
					titles.Add(text.Title);
				return titles;
			}
		}
		
		public Texts(string userDataDir, string presetsDir)
		{
			if (userDataDir != null)
				userDataPath = Path.Combine(userDataDir, "texts");
			if (presetsDir != null)
				presetsPath = Path.Combine(presetsDir, "presets.typertexts");
			//saveUserData();
			//loadUserData(presetsPath);
			
			if (File.Exists(userDataPath))
				loadUserData(userDataPath);
			else if (File.Exists(presetsPath))
				loadUserData(presetsPath);
		}

		public void loadUserData(string loadPath)
		{
			if (string.IsNullOrEmpty(loadPath))
				return;
			using (var stream = File.Open(loadPath, FileMode.Open))
			{
				loadUserData(stream);
			}
		}

		public void loadUserData(Stream stream)
		{
			var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
			var data = (UserData)dcs.ReadObject(stream);
			foreach (var text in data.TextEntries)
				userData.TextEntries.add(text);
			foreach (var rec in data.Records)
				userData.Records.Add(rec);
		}

		public void saveUserData()
		{
			string tempPath = userDataPath + "_";
			try
			{
				using (var stream = File.Open(tempPath, FileMode.Create))
				{
					saveStream(stream, userData);
				}
			}
			catch
			{
				File.Delete(tempPath);
				throw;
			}
			File.Delete(userDataPath);
			File.Move(tempPath, userDataPath);
		}

		public void saveUserTextsAsPresets(Stream stream)
		{
			var data = new UserData();
			data.TextEntries = userData.TextEntries;
			saveStream(stream, data);
		}

		void saveStream(Stream stream, UserData data)
		{
			var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
			dcs.WriteObject(stream, userData);
		}

		public void add(TextEntry entry)
		{
			if (userData.TextEntries.containsKey(entry.Title))
				throw new ArgumentException("There already exists a text with the specified title.");
			if (string.IsNullOrWhiteSpace(entry.Title))
				throw new ArgumentException("Title can't be empty.");
			userData.TextEntries.add(entry);
			saveUserData();
		}

		public void remove(string title)
		{
			int currentIndex = userData.TextEntries.indexOf(title);

			//Remove the text with this title
			userData.TextEntries.remove(title);

			//Remove all records for this title
			Record recordMatch;
			while ((recordMatch = userData.Records.Find((r) => r.TextTitle == title)) != null)
				userData.Records.Remove(recordMatch);

			if (title == Current.Title)
			{
				if (currentIndex >= userData.TextEntries.Count)
					currentIndex--;

				if (currentIndex == -1)
					Current = null;
				else
					Current = userData.TextEntries.ElementAt(currentIndex);
			}
			saveUserData();
		}

		public bool containsTitle(string title)
		{
			return userData.TextEntries.containsKey(title);
		}

		public IEnumerator<TextEntry> GetEnumerator()
		{
			foreach (var entry in userData.TextEntries)
				yield return entry;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void resetFactoryTexts()
		{
			//var data = load(presetsPath);
			//foreach (var text in data)
			//	userData.TextEntries.add(text);
		}

		public string select(string title)
		{
			Current = userData.TextEntries[title];
			return Current.Text;
		}

		public TextEntry selectRandom()
		{
			if (userData.TextEntries.Count == 0)
				return null;
			string randomTitle;
			do
			{
				randomTitle = userData.TextEntries.ElementAt(new Random().Next(userData.TextEntries.Count)).Title;
			} while (randomTitle == Current?.Title && userData.TextEntries.Count > 1);
			select(randomTitle);
			return Current;
		}

		public void addRecord(int wpm, float accuracy, string title)
		{
			userData.Records.Add(new Record(wpm, accuracy, title));
			saveUserData();
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

		public void restorePresets(Stream stream)
		{
			loadUserData(stream);
			saveUserData();
			Current = userData.TextEntries.ElementAt(userData.TextEntries.indexOf(Current.Title));
		}
	}

	[Serializable]
	internal class UserData : ISerializable
	{
		internal TextEntries TextEntries { get; set; } = new TextEntries();
		internal Records Records { get; set; } = new Records();
		static readonly public Type[] SerializeTypes = new Type[] { typeof(TextEntry), typeof(Record) };

		internal UserData()
		{
		}

		public UserData(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				//Save individual texts and records in order to be able to shange data structure without changing file format.
				if (entry.Name.StartsWith("textEntry_"))
				{
					var textEntry = (TextEntry)entry.Value;
					TextEntries.add(textEntry);
				}
				else if (entry.Name.StartsWith("records_"))
					Records.Add((Record)entry.Value);
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			foreach (var textEntry in TextEntries)
				info.AddValue("textEntry_"+textEntry.Title, textEntry);
			for (int i = 0; i < Records.Count; i++)
				info.AddValue("record_"+i, Records[i]);
		}
	}
}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";