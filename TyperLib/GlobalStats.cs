using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TyperLib
{
	[Serializable]
	public class GlobalStats
	{
		public GlobalStats()
		{

		}
		Dictionary<string, WordStats> words = new Dictionary<string, WordStats>();
		public int TotalWords;
		public int UniqueWords => words.Count();
		public int AvgTopWpm
		{
			get
			{
				int count = words.Count();
				return count == 0 ? 0 : words.Sum((w) => w.Value.TopWpm) / words.Count();
			}
		}

		public GlobalStats(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "words")
					words = (Dictionary<string, WordStats>)entry.Value;
				if (entry.Name == "totalWords")
					TotalWords = (int)entry.Value;
			}
		}

		virtual public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("words", words);
			info.AddValue("totalWords", words);
		}
		
		public void addWord(string word, float minutes)
		{
			int wpm = (int)(word.Length / (5.0f * minutes));
			 if (!words.ContainsKey(word))
				words.Add(word, new WordStats(wpm, word));
			else
				words[word].addResult(wpm);
			TotalWords++;
		}

		public string[] getSlowestWordStrings(int count)
		{
			return getSlowestWords(count).Select((x) => x.Word).ToArray();
		}

		public WordStats[] getSlowestWords(int count)
		{
			var slowSorted = words.ToArray();
			Array.Sort(slowSorted, (a, b) =>
			{
				if (a.Value.TopWpm < b.Value.TopWpm)
					return -1;
				else if (a.Value.TopWpm > b.Value.TopWpm)
					return 1;
				else
					return 0;
			});
			return slowSorted.Select((x) => x.Value).Take(count).ToArray();
		}

		public void clear()
		{
			words.Clear();
			TotalWords = 0;
		}
	}

	[Serializable]
	public class WordStats
	{
		public int TopWpm { private set; get; }
		public int Count { private set; get; }
		public string Word { private set; get; }
		public WordStats(int wpm, string word)
		{
			TopWpm = wpm;
			Word = word;
			Count = 1;
		}
		public WordStats(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "topWpm")
					TopWpm = (int)entry.Value;
				if (entry.Name == "count")
					Count = (int)entry.Value;

			}
		}

		virtual public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("topWpm", TopWpm);
			info.AddValue("count", Count);
		}

		public void addResult(int wpm)
		{
			if (TopWpm < wpm)
				TopWpm = wpm;
			Count++;
		}
	}
}
