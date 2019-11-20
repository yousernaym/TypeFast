﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TyperLib
{
	using Records = List<Record>;
	public enum RecordType { RT_BestSessions, RT_BestTexts, RT_WorstTexts, RT_BestOfText};

	[Serializable]
	public class Texts : IEnumerable<TextEntry>
	{
		//TextEntries presets = new TextEntries();
		UserData userData = new UserData();
		public const int MaxRecords = 10;
		string userDataPath;
		//string presetsPath;
		public const int Version = 0;

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

		public Texts(string userDataDir, Stream presetsStream)
		{
			if (userDataDir != null)
				userDataPath = Path.Combine(userDataDir, "texts.ttl");
			//if (presetsDir != null)
				//presetsPath = Path.Combine(presetsDir, "presets.ttl");
			//saveUserData();

			bool userDataExists = File.Exists(userDataPath);
			if (userDataExists)
				loadUserData(userDataPath, true);
			loadUserData(presetsStream, false, userDataExists);
		}

		void loadUserData(string loadPath, bool loadRecords)
		{
			if (string.IsNullOrEmpty(loadPath))
				return;
			using (var stream = File.Open(loadPath, FileMode.Open))
			{
				loadUserData(stream, loadRecords);
			}
		}

		void loadUserData(Stream stream, bool loadRecords, bool onlyAddNewTexts = false)
		{
			if (stream == null)
				return;
			var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
			var data = (UserData)dcs.ReadObject(stream);

			foreach (var text in data.TextEntries)
			{
				if (!onlyAddNewTexts || text.Version > userData.SyncedWithVersion && !userData.TextEntries.containsKey(text.Title))
					userData.TextEntries.add(text);
			}
			if (loadRecords)
			{
				foreach (var rec in data.Records)
				{
					if (!userData.Records.Any(r => r.Id == rec.Id))
						addRecord(rec);
				}
			}
			userData.SyncedWithVersion = data.SyncedWithVersion;
		}

		public void saveUserData()
		{
			if (userDataPath == null)
				return;

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			while (true)
			{
				try
				{
					using (var stream = File.Open(userDataPath, FileMode.Create))
						saveUserData(stream, userData);
					return;
				}
				catch (IOException ex)
				{
					if (stopwatch.ElapsedMilliseconds > 2000)
						throw ex;
				}
			}
		}

		public void saveUserTexts(Stream stream)
		{
			var data = new UserData();
			data.TextEntries = userData.TextEntries;
			saveUserData(stream, data);
		}

		void saveUserData(Stream stream, UserData data)
		{
			var dcs = new DataContractSerializer(typeof(UserData), UserData.SerializeTypes);
			dcs.WriteObject(stream, data);
		}

		public void saveUserData(Stream stream)
		{
			saveUserData(stream, userData);
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

		public void select(string title)
		{
			Current = userData.TextEntries[title];
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

		public void addRecord(TypingSession session)
		{
			addRecord(new Record(session.Wpm, session.MaxWpm, session.MinWpm, session.MaxWpmText, session.MinWpmText, session.Accuracy, session.ElapsedTime, Current.Title, session.IsTextFinished, session.WrittenChars.Count));
		}

		public void addRecord(Record newRecord)
		{
			userData.Records.Add(newRecord);
			var currentTextRecords = getRecordsOfText(newRecord.TextTitle);
			if (currentTextRecords.Length <= MaxRecords * 2)
				return;

			//Create record lists sorted after every record element
			var sortedRecords = new Record[Enum.GetValues(typeof(RecordElem)).Length][];
			for (int i = 0; i < sortedRecords.GetLength(0); i++)
			{
				sortedRecords[i] = new Record[currentTextRecords.Length];
				Array.Copy(currentTextRecords, 0, sortedRecords[i], 0, currentTextRecords.Length);
				Record.sort(sortedRecords[i], (RecordElem)i, false);
			}

			//Search for a record that is not top or bottom ten in any category and remove it to keep record array size managable
			foreach (var record in currentTextRecords)
			{
				bool removeRecord = true;
				for (int i = 0; i < Enum.GetValues(typeof(RecordElem)).Length; i++)
				{
					//If record is higher than the MaxRecords first or lower than the MaxRecords last it can stay
					//eg., if there are 21 records and MaxRecords == 10, check if higher than [9] or lower than [11] (descending sort)
					//It can also stay if it IS [9] or [11]
					Record lowestHighRecord = sortedRecords[i][MaxRecords - 1];
					Record highestLowRecord = sortedRecords[i][currentTextRecords.Length - MaxRecords];
					RecordElem recElemToCompare = (RecordElem)i;
					if (record.Id == lowestHighRecord.Id || record.Id == highestLowRecord.Id ||
						record.recordElemIsGreaterThan(lowestHighRecord, recElemToCompare) || record.recordElemIsLessThan(highestLowRecord, recElemToCompare))
					{
						removeRecord = false;
						break;
					}
				}
				if (removeRecord)
				{
					userData.Records.Remove(record);
					return;
				}
			}
		}

		public Record[] getRecordsOfText(string title)
		{
			return userData.Records.Where(p => p.TextTitle == title).ToArray();
		}

		//public Record[] getRecordsOfText(string title, RecordElem recElem, bool ascendingSort)
		//{
		//	var textRecords = getRecordsOfText(title);
		//	Record.sort(textRecords, recElem, ascendingSort);
		//	return textRecords;
		//}

		public Record[] getRecords(bool filterTexts, RecordElem primarySort, bool ascendingSort, int count = 0)
		{ 
   	 		Record[] records = new Record[userData.Records.Count];
			userData.Records.CopyTo(records);
			Record.sort(records, primarySort, ascendingSort);

			if (filterTexts)
			{
				var dict = new Dictionary<string, Record>();
				foreach (var rec in records)
				{
					//Add record to dictionary if it doesn't already contain a record with this text title
					if (!dict.ContainsKey(rec.TextTitle))
						dict[rec.TextTitle] = rec;
				}
				records = new Record[dict.Count];
				int i = 0;
				foreach (var rec in dict)
					records[i++] = rec.Value;
				Record.sort(records, primarySort, ascendingSort);
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

		public void importUserData(Stream stream, bool importRecords)
		{
			loadUserData(stream, importRecords);
			saveUserData();

			//Update text of current selection in case it was altered
			if (Current != null && containsTitle(Current.Title))
				select(Current.Title);
		}

	}

	[Serializable]
	internal class UserData : ISerializable
	{
		internal int SyncedWithVersion { get; set; }
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
				if (entry.Name == "syncedWithVersion")
					SyncedWithVersion = (int)entry.Value;
				else if (entry.Name.StartsWith("textEntry_"))
					TextEntries.add((TextEntry)entry.Value);
				else if (entry.Name.StartsWith("record_"))
					Records.Add((Record)entry.Value);
			}
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("syncedWithVersion", Texts.Version);
			//Save individual texts and records in order to be able to shange data structure without changing file format.
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