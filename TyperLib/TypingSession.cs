﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TyperLib
{
	public enum SessionType { Title, Temp, MomentaryWpm };
	[Serializable]
	public class TypingSession : ISerializable
	{
		enum RndEntity { Chars, Words, Lines, None };
		public enum KeyPressResult { NotTypable, Incorrect, Correct, DeleteIncorrect, DeleteCorrect };
		public const uint KeyCode_Backspace = 8;
		public const uint KeyCode_Space = 32;
		const int MomentaryWpmChars = 15;

		public string[] RndElements;
		int minWordLength;
		int maxWordLength;

		public TextEntry StartText { get; private set; }
		public TimeSpan TimeLimit { get; set; } = new TimeSpan(0, 1, 0);

		public TextEntry TextEntrySource { get; private set; }

		TextEntry textEntry = new TextEntry();
		public TextEntry TextEntry
		{
			get => textEntry;
			set
			{
				TextEntrySource = value;
				textEntry = new TextEntry(value);
				RndElements = null;
				minWordLength = maxWordLength = 1;
				Match match;
				bUpdateWordStats = true;
				if (value.Title == "A-Z" || value.Title == "Z-A")
					bUpdateWordStats = false;
				if ((match = Regex.Match(text, "__rnd [0-9]-?[0-9]?__")).Success)
				{
					if (int.TryParse(text[6].ToString(), out minWordLength))
					{
						if (!(text[7] == '-' && int.TryParse(text[8].ToString(), out maxWordLength)))
							maxWordLength = minWordLength;
						text = text.Substring(match.Length);
						text = text.Replace(" ", "");
						text = text.Replace("\n", "");
						text = text.Replace("\r", "");
						RndElements = text.Select(x => x.ToString()).ToArray(); //Create array with all characters in the string
						bUpdateWordStats = false;
					}
				}
				else if (text.StartsWith("__rnd ws__"))
					RndElements = text.Substring(10).Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
				else if (text.StartsWith("__rnd line__"))
					RndElements = text.Substring(10).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				else if (text.StartsWith("__bible__"))
				{
					text = Bible.getRandomText(10000);
					title = Bible.Currentverse.ToString();
				}
				else if((match = Regex.Match(text, "__rnd \".+\"__")).Success)
				{
					string delim = text.Substring(7, match.Length - 3 - 7);
					text = text.Substring(match.Length);
					RndElements = text.Split(new string[] { delim }, StringSplitOptions.None);
				}
				else if ((match = Regex.Match(text, "(?<=__slowest words )([0-9])+(?=__)")).Success)
				{
					int count = int.Parse(match.Value);
					RndElements = GlobalStats.getSlowestWordStrings(count);
					if (RndElements.Length == 0)
					{
						//No words have been typed yet
						RndElements = null;
						text = "";
					}
				}
				if (RndElements != null)
				{
					var rnd = new Random();
					var sb = new StringBuilder();
					int remainingCharsInWord = 0;
					while (sb.Length < 10000)
					{
						if (remainingCharsInWord == 0)
							remainingCharsInWord = rnd.Next(minWordLength, maxWordLength + 1);
						sb.Append(RndElements[rnd.Next(RndElements.Length)]);
						if (--remainingCharsInWord <= 0)
							sb.Append(' ');
					}
					text = sb.ToString();
				}
				reset();

				//Replace whitespace with space
				var chars = text.ToCharArray();
				text = text.Replace('\r', ' ');
				text = text.Replace('\t', ' ');
				text = text.Replace('\n', ' ');

				foreach (var charMapping in symbolMap)
					foreach (var source in charMapping.Value)
						text = text.Replace(source, charMapping.Key);
				if (textEntry.AsciiLetters)
				{
					foreach (var charMapping in letterMap)
						foreach (var source in charMapping.Value)
							text = text.Replace(source, charMapping.Key);

					text = text.Normalize(NormalizationForm.FormD);
					var sb = new StringBuilder();
					foreach (var c in text)
					{
						var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
						if (unicodeCategory != UnicodeCategory.NonSpacingMark)
							sb.Append(c);
					}

					text = sb.ToString().Normalize(NormalizationForm.FormC);
				}

				text = text.Trim();

				//Replace repeating spaces with single space
				text = Regex.Replace(text, "[ ]{2,}", " ");

				////Replace repeating line breaks with single break
				//text = Regex.Replace(text, "[\n]{2,}", " ");

				//text = text.Replace('\n', ' ');

			}
		}

		string title
		{
			get => textEntry.Title;
			set => textEntry.Title = value;
		}

		string text
		{
			get => textEntry.Text;
			set => textEntry.Text = value;
		}

		protected int currentCharIndex;
		public int CurrentChar => text[currentCharIndex];
		public const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		public LinkedList<WrittenChar> WrittenChars { get; private set; } = new LinkedList<WrittenChar>();
		public int CorrectChars { get; private set; } = 0;
		public int IncorrectChars { get; private set; } = 0;
		public int TotalIncorrectChars { get; private set; } = 0;
		public int FixedChars => TotalIncorrectChars - IncorrectChars;

		public bool MomentaryWpmSession { get; set; } = false; //The current session consists of the momentaryWpm text snippet of a previous session. This means the high/low wpm stats should stay like they were in that session and not be updated in this session.
		MomentaryWpm highWpm;
		public MomentaryWpm HighWpm => highWpm;
		LinkedList<MomentaryWpm> lowWpm;
		public MomentaryWpm LowWpm
		{
			get => lowWpm.Last();
			private set
			{
				lowWpm = new LinkedList<MomentaryWpm>();
				lowWpm.AddLast(value);
			}
		}

		public int Wpm
		{
			get
			{
				//If last typed chasactes was incorrect, don't apply WPM penalty for that character. Only apply pehalty if user keeps going without fixing.
				var adjustedIncorrectChars = IncorrectChars;
				if (IncorrectChars > 0 && !WrittenChars.First.Value.Correct)
					adjustedIncorrectChars--;

				return Math.Max((int)((CorrectChars - adjustedIncorrectChars * 2) / ElapsedTime.TotalMinutes), 0) / 5;
			}
		}
		public float Accuracy
		{
			get
			{
				if (WrittenChars.Count == 0)
					return 0;
				return (float)CorrectChars / (CorrectChars + TotalIncorrectChars) * 100;
			}
		}
		Stopwatch stopwatch = new Stopwatch();
		public TimeSpan RemainingTime
		{
			get
			{
				var dif = TimeLimit - ElapsedTime;
				if (dif.Ticks < 0)
					return new TimeSpan(0);
				else
					return dif;
			}
		}
		public string RemainingTimeString => RemainingTime.Minutes + ":" + RemainingTime.Seconds.ToString("d2");

		public TimeSpan ElapsedTime => stopwatch.Elapsed;
		public bool IsTextFinished => currentCharIndex >= text.Length;
		public bool IsFinished => IsTextFinished || RemainingTime.Ticks == 0;
		public bool IsRunning => stopwatch.IsRunning;
		public bool IsReset => !IsFinished && !IsRunning;

		Timer checkTimeTimer;
		Dictionary<string, string[]> symbolMap = new Dictionary<string, string[]>();
		Dictionary<string, string[]> letterMap = new Dictionary<string, string[]>();
		Stopwatch currentWordStopWatch = new Stopwatch();
		public GlobalStats GlobalStats => Texts.GlobalStats;
		public Texts Texts;
		bool bUpdateWordStats;

		public Bible Bible { get; set; }

		public event EventHandler TimeChecked;
		public event EventHandler Finished;
		
		//public string WrittenTextToDraw => text == null ? "" : text.Substring(startDrawChar, currentCharIndex - startDrawChar);
		public string UnwrittenTextToDraw => text == null ? "" : text.Substring(currentCharIndex, Math.Min(text.Length - currentCharIndex, NumCharsFromCenter));

		public TypingSession()
		{
			highWpm = new MomentaryWpm();
			LowWpm = new MomentaryWpm();
		}

		public TypingSession(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "startText")
					StartText = (TextEntry)entry.Value;
				else if (entry.Name == "timeLimit")
					TimeLimit = (TimeSpan)entry.Value;
			}
		}
		virtual public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("startText", MomentaryWpmSession ? null : TextEntrySource);
			info.AddValue("timeLimit", TimeLimit);
		}

		public void loadCharMap(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				string line;
				string section = null;
				while ((line = reader.ReadLine()) != null)
				{
					Match match;
					if ((match = Regex.Match(line, "\\[.*\\]")).Success)
					{
						section = match.Value;
						continue;
					}
					if (string.IsNullOrWhiteSpace(line))
						continue;
					var split = line.Split(new char[] { ',' }, 2);
					var sources = split[0].Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < sources.Length; i++)
						if (sources[i][0] == '#')
							sources[i] = ((char)int.Parse(sources[i].Substring(1))).ToString();

					var dest = split[1].Trim();
					if (dest.Count(c => c == '"') == 2)
						dest = dest.Replace("\"", "");

					if (section == "[Symbols]")
						symbolMap.Add(dest, sources);
					else if (section == "[Letters]")
						letterMap.Add(dest, sources);
				}
			}
		}
		protected virtual void OnTimeChecked()
		{
			TimeChecked?.Invoke(this, new EventArgs());
		}

		private void checkTime(object state)
		{
			OnTimeChecked();
			if (RemainingTime.Ticks == 0)
			{
				OnFinished();
			}
		}

		private void OnFinished()
		{
			stopTime(false);
			Finished?.Invoke(this, new EventArgs());
		}

		//public void loadText()
		//{
		//	TheText = File.ReadAllText("textToType.txt");
		//}

		public KeyPressResult typeChar(uint keyCode)
		{
			char c = (char)keyCode;
			if (IsFinished || keyCode < 32 && keyCode != KeyCode_Backspace)
				return KeyPressResult.NotTypable;

			if (!stopwatch.IsRunning)
			{
				if (keyCode == KeyCode_Backspace)
					return KeyPressResult.NotTypable;
				startTime();
			}
			try
			{
				//Backspace
				if (c == KeyCode_Backspace)
				{
					//Aiready at beginning?
					if (currentCharIndex == 0)
						return KeyPressResult.NotTypable;

					currentCharIndex--;
					bool isCorrect = WrittenChars.First.Value.Correct;
					WrittenChars.RemoveFirst();
					if (isCorrect)
					{
						CorrectChars--; //Correct char deleted
						return KeyPressResult.DeleteCorrect;
					}
					else
					{
						IncorrectChars--; //Inorrect char deleted
						return KeyPressResult.DeleteIncorrect;
					}
				}
				else //Not backspace
				{
					char currentChar = text[currentCharIndex++];
					bool isCorrect = currentChar == c;
					float charTime = (float)ElapsedTime.TotalSeconds;
					if (WrittenChars.Count == 0)
						charTime = 0;
					WrittenChars.AddFirst(new WrittenChar(isCorrect, currentChar, charTime));
					KeyPressResult result;
					if (isCorrect)
					{
						CorrectChars++;
						result = KeyPressResult.Correct;
					}
					else
					{
						IncorrectChars++;
						TotalIncorrectChars++;
						result = KeyPressResult.Incorrect;
					}
					if (currentCharIndex >= text.Length)
						OnFinished();

					updateWordStats();
					return result;
				}
			}
			finally
			{
				updateMomentaryWpm();
			}
		}

		void updateWordStats()
		{
			if (!bUpdateWordStats)
				return;
			if (WrittenChars.First().Char == ' ')
			{
				currentWordStopWatch.Reset();
				currentWordStopWatch.Start();
			}
			else if (text.Length == currentCharIndex || text[currentCharIndex] == ' ') //Next character is space or text is finished
			{
				string word = "";
				float wordStartTime = -1, wordStopTime = -1;
				foreach (var c in WrittenChars.TakeWhile((c) => c.Char != ' '))
				{
					if (!c.Correct)
						return;
					word = c.Char + word;
					wordStartTime = c.SecondsFromStart;
					if (wordStopTime == -1)
						wordStopTime = c.SecondsFromStart;
				}
				string lowerCaseWord = word.ToLower();
				if (word.Length < 2 || lowerCaseWord.StartsWith("abcdefg") || lowerCaseWord.EndsWith("gfedcba"))
					return;
				GlobalStats.addWord(word, (wordStopTime - wordStartTime) / 60);
			}
		}

		void startTime()
		{
			stopTime(true);
			stopwatch.Start();
			currentWordStopWatch.Reset();
			currentWordStopWatch.Start();
			checkTimeTimer = new Timer(checkTime, null, 0, 100);
		}

		void stopTime(bool reset)
		{
			if (reset)
				stopwatch.Reset();
			else
				stopwatch.Stop();
			checkTimeTimer?.Dispose();
			OnTimeChecked();
		}

		public void reset()
		{
			stopTime(true);
			WrittenChars = new LinkedList<WrittenChar>();
			CorrectChars = IncorrectChars = TotalIncorrectChars = 0;
			currentCharIndex = 0;
			if (!MomentaryWpmSession)
			{
				highWpm = new MomentaryWpm();
				LowWpm = new MomentaryWpm();
			}
		}

		//Call for every new character and for every time check.
		//Always check last <MomentaryWpmChars> chars and check against current max/min Wpm.
		public void updateMomentaryWpm()
		{
			if (MomentaryWpmSession)
				return;
			float elapsedTimeS = (float)ElapsedTime.TotalSeconds;
			if (WrittenChars.Count < MomentaryWpmChars)
			{
				highWpm = new MomentaryWpm();
				lowWpm = new LinkedList<MomentaryWpm>();
				lowWpm.AddLast(new MomentaryWpm());
				return;
			}
			int correctChars = 0, incorrectChars = 0;
			string textSnippet = "";
			int numChars = 0;
			float firstCharTime = elapsedTimeS, lastCharTime = 0;
			foreach (var writtenChar in WrittenChars.ToList())  //ToList() makes it thread-safe because why not, even though it should not matter as long as this function is run on the same thread as typeChar.
			{
				if (writtenChar.Correct)
					correctChars++;
				else
					incorrectChars++;
				textSnippet = writtenChar.Char + textSnippet;
				lastCharTime = writtenChar.SecondsFromStart;
				if (++numChars == MomentaryWpmChars)
					break;
			}
			//If last typed chasactes was incorrect, don't apply WPM penalty for that character. Only apply pehalty if user keeps going without fixing.
			var adjustedIncorrectChars = incorrectChars;
			if (incorrectChars > 0 && !WrittenChars.First.Value.Correct)
				adjustedIncorrectChars--;

			int wpm = (int)(Math.Max((correctChars - adjustedIncorrectChars * 2) / ((firstCharTime - lastCharTime) / 60), 0) / 5);

			if (wpm > highWpm.Wpm)
			{
				highWpm.Wpm = wpm;
				highWpm.TextSnippet = textSnippet;
			}
			updateLowWpm(wpm, textSnippet, WrittenChars.Count);

			int avgWpm = Wpm;
			if (lowWpm.Last().Wpm > avgWpm)
				lowWpm.Last.Value.Wpm = avgWpm;
			if (highWpm.Wpm < avgWpm)
				highWpm.Wpm = avgWpm;

		}

		private void updateLowWpm(int wpm, string textSnippet, int totalTextLenght)
		{
			var wpmNode = lowWpm.Last;
			while (wpmNode.Value.TotalTextLength > totalTextLenght)
				wpmNode = wpmNode.Previous;
				
			if (wpmNode.Next != null)
				lowWpm.Remove(wpmNode.Next);
			
			if (wpm < wpmNode.Value.Wpm || wpmNode.Value.Wpm == -1)
			{
				MomentaryWpm newWpm;
				if (wpmNode.Value.TotalTextLength == totalTextLenght)
					newWpm = wpmNode.Value;
				else
				{
					newWpm = new MomentaryWpm();
					lowWpm.AddLast(newWpm);
				}
				newWpm.Wpm = wpm;
				newWpm.TextSnippet = textSnippet;
				newWpm.TotalTextLength = totalTextLenght;
			}
		}

		public void setHighLowWpm(int highWpm, string highSnippet, int lowWpm, string lowSnippet)
		{
			HighWpm.Wpm = highWpm;
			HighWpm.TextSnippet = highSnippet;
			LowWpm.Wpm = lowWpm;
			LowWpm.TextSnippet = lowSnippet;
		}
	}

	public class MomentaryWpm
	{
		public int Wpm { get; set; } = -1;
		public string TextSnippet { get; set; } = "";
		public int TotalTextLength { get; set; } = 0;
	}
}

public class WrittenChar
{
	bool correct;
	char character;
	float secondsFromStart;
	public bool Correct => correct;
	public char Char => character;
	public float SecondsFromStart => secondsFromStart;
	public WrittenChar(bool correct, char character, float secondsFromStart)
	{
		this.correct = correct;
		this.character = character;
		this.secondsFromStart = secondsFromStart;
	}
}