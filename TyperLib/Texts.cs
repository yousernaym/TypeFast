using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;

namespace TyperLib
{
	public enum RecordType { RT_BestSessions, RT_BestTexts, RT_WorstTexts, RT_BestOfText};

	[Serializable]
	public class Texts : IEnumerable<TextEntry>
	{
		//TextEntries presets = new TextEntries();
		UserData userData = new UserData();
		public const int MaxRecords = 10;
		string userDataPath;
		//string presetsPath;
		public const int Version = 1;

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
				userDataPath = Path.Combine(userDataDir, "texts.tft");
			//if (presetsDir != null)
				//presetsPath = Path.Combine(presetsDir, "presets.tft");
			//saveUserData();

			bool userDataExists = File.Exists(userDataPath);
			try
			{
				if (userDataExists)
					loadUserData(userDataPath, true);
			}
			catch (Exception ex) when(ex is SerializationException || ex is XmlException)
			{
				//Ignore corrupt file. A new will be created.
			}
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

			//Specify texts (string) that should not be loaded if version (int) is less than current version
			//This means that texts removed from an updated version of the app will be removed from the user data, but if the user creates a text with the same name, this text will not be removed since it will have the higher version number.
			Tuple<string, int>[] removedTexts = { new Tuple<string, int>("Common words", 0) }; 
			foreach (var text in data.TextEntries)
			{
				if (removedTexts.Any(t => t.Item1 == text.Title && t.Item2 >= text.Version) || onlyAddNewTexts && (text.Version <= userData.SyncedWithVersion || userData.TextEntries.containsKey(text.Title)))
					continue;
				userData.TextEntries.add(text);
			}
			if (loadRecords)
			{
				foreach (var rec in data.Records)
				{
					if (!userData.TextEntries.containsKey(rec.TextTitle) || userData.Records.Any(r => r.Id == rec.Id))
						continue;
					addRecord(rec, false);
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
				catch (IOException)
				{
					if (stopwatch.ElapsedMilliseconds > 2000)
						return;
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

		public void clearRecords()
		{
			userData.Records.Clear();
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
			addRecord(new Record(session.Wpm, session.HighWpm.Wpm, session.LowWpm.Wpm, session.HighWpm.TextSnippet, session.LowWpm.TextSnippet, session.Accuracy, session.ElapsedTime, Current.Title, session.IsTextFinished, session.WrittenChars.Count), true);
		}

		//removeHidden == true searches for a record that is guaranteed to not be visible regardless of primary sort column.
		//More Specifically it searches for a record whose max/min/avg wpm are all below top ten for the current text.
		//Pass false for better performanc if loading records from file.
		public void addRecord(Record newRecord, bool removeHidden)
		{
			userData.Records.Add(newRecord);
			var currentTextRecords = getRecordsOfText(newRecord.TextTitle);
			if (currentTextRecords.Length <= MaxRecords)
				return;

			//Create record lists sorted after every primary sort type
			var sortedRecords = new Record[Enum.GetValues(typeof(Record.PrimarySortType)).Length][];
			for (int i = 0; i < sortedRecords.Length; i++)
			{
				sortedRecords[i] = new Record[currentTextRecords.Length];
				Array.Copy(currentTextRecords, 0, sortedRecords[i], 0, currentTextRecords.Length);
				Record.sort(sortedRecords[i], (Record.PrimarySortType)i, false);
			}

			//Search for a record to remove
			foreach (var record in currentTextRecords)
			{
				bool removeRecord = true;
				for (int i = 0; i < Enum.GetValues(typeof(Record.PrimarySortType)).Length; i++)
				{
					//If record is higher than the MaxRecords first it can stay.
					//eg., if there are 11 records and MaxRecords == 10, check if it's higher than [9] (descending sort)
					//It can also stay if it IS [9]
					Record lowestHighRecord = sortedRecords[i][MaxRecords - 1];
					Record.PrimarySortType typeToCompare = (Record.PrimarySortType)i;
					if (record.Id == lowestHighRecord.Id || record.primarySortTypeIsGreaterThan(lowestHighRecord, typeToCompare))
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

		public Record[] getRecords(bool ?ascendingTexts, Record.PrimarySortType primarySort, int count = 0)
		{ 
   	 		Record[] records = new Record[userData.Records.Count];
			userData.Records.CopyTo(records);
			Record.sort(records, primarySort, false);

			if (ascendingTexts != null)
			{
				var dict = new Dictionary<string, Record>();
				foreach (var rec in records)
				{
					//Add record to dictionary if it doesn't already contain a record with this text title
					if (!dict.ContainsKey(rec.TextTitle))
						dict[rec.TextTitle] = rec;
				}
				records = new Record[dict.Count].ToArray();
				int i = 0;
				foreach (var rec in dict)
					records[i++] = rec.Value;
				Record.sort(records, primarySort, (bool)ascendingTexts);
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
}


//var assembly = IntrospectionExtensions.GetTypeInfo(typeof(LoadResourceText)).Assembly;
//Stream stream = assembly.GetManifestResourceStream("WorkingWithFiles.PCLTextResource.txt");
//string text = "";