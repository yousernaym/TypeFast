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
	using Records = List<Record>;
	public enum RecordType { RT_BestSessions, RT_BestTexts, RT_WorstTexts, RT_BestOfText};
	
	[Serializable]
	public class Texts : IEnumerable<TextEntry>
	{
		TextEntries presets = new TextEntries();
		UserData userData = new UserData();
		const int MaxRecordsPerText = 6;
		readonly string userDataPath;
		//readonly string presetsPath;
		
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
		
		public Texts(string userDataDir, Action loadPresets)
		{
			if (userDataDir != null)
				userDataPath = Path.Combine(userDataDir, "texts.tts");
			//if (presetsDir != null)
			//	presetsPath = Path.Combine(presetsDir, "presets.tts");
			//saveUserData();

			if (File.Exists(userDataPath))
				loadUserData(userDataPath, true);
			else
				loadPresets();
			//else if (File.Exists(presetsPath))
			//loadUserData(presetsPath);
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

		void loadUserData(Stream stream, bool loadRecords)
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
			if (userDataPath == null)
				return;
			//string tempPath = userDataPath + "_";
			string tempPath = userDataPath;
			//try
			//{
				using (var stream = File.Open(tempPath, FileMode.Create))
				{
					saveUserData(stream, userData);
				}
			//}
			//catch
			//{
			//	File.Delete(tempPath);
			//	throw;
			//}
			//File.Delete(userDataPath);
			//File.Move(tempPath, userDataPath);
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
			addRecord(new Record(session.Wpm, session.Accuracy, session.ElapsedTime, Current.Title, session.IsTextFinished, session.WrittenChars.Count));
			if (session.WrittenChars.Count == 0)
				throw new Exception();
		}

		public void addRecord(Record rec)
		{
			var records = getRecordsOfText(rec.TextTitle);
			if (records.Length < MaxRecordsPerText || rec > records[records.Length - 1])
			{
				//If the maximum number of records afready has been recorded, remove the lowest record.
				if (records.Length >= MaxRecordsPerText)
					userData.Records.Remove(records[records.Length - 1]);

				//Add the new record
				userData.Records.Add(rec);

				//Save to disk
				saveUserData();
			}
		}

		public Record[] getRecordsOfText(string title, int count = 0)
		{
			var textRecords = userData.Records.Where(p => p.TextTitle == title).ToArray();
			Array.Sort(textRecords);
			return textRecords.ToArray();
		}

		public Record[] getRecords(RecordType type, int count = 0)
		{
			Record[] records = new Record[userData.Records.Count];
			userData.Records.CopyTo(records);
			Array.Sort(records);

			if (type != RecordType.RT_BestSessions)
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

		public void importUserData(Stream stream, bool importRecords)
		{
			loadUserData(stream, importRecords);
			saveUserData();

			//Update text of current selection in case it was altered
			if (Current != null)
				select(Current.Title);
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
				else if (entry.Name.StartsWith("record_"))
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