using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;

namespace TyperLib
{
	[Serializable]
	public class TypingSession : ISerializable
	{
		enum RndEntity { Chars, Words, Lines, None };
		const uint KeyCode_Backspace = 8;

		string[] rndElements;
		int minWordLength;
		int maxWordLength;

		public string StartText { get; private set; }
		public bool Shuffle { get; set; } = false;
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
				
				rndElements = null;
				minWordLength = maxWordLength = 1;
				Match match;
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
						rndElements = text.Select(x => x.ToString()).ToArray();
					}
				}
				else if (text.StartsWith("__rnd ws__"))
					rndElements = text.Substring(10).Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
				else if (text.StartsWith("__rnd br__"))
					rndElements = text.Substring(10).Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
				else if (text.StartsWith("__bible__"))
				{
					text = Bible.getRandomText(10000);
					title = Bible.Currentverse.ToString();
				}

				if (rndElements != null)
				{
					var rnd = new Random();
					var sb = new StringBuilder();
					int remainingCharsInWord = 0;
					while (sb.Length < 10000)
					{
						if (remainingCharsInWord == 0)
							remainingCharsInWord = rnd.Next(minWordLength, maxWordLength + 1);
						sb.Append(rndElements[rnd.Next(rndElements.Length)]);
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

				//text = text.Replace((char)160, ' '); //Convert non-breaking space to regular space
				//text = text.Replace('“', '"');
				//text = text.Replace('”', '"');
				//text = text.Replace('‘', '\'');
				//text = text.Replace('’', '\'');
				//text = text.Replace(((char)8212).ToString(), "--"); //Convert wide non-ascii hyphen
				text = text.Trim();
				
				//Replace repeating spaces with single space
				text = Regex.Replace(text, "[ ]{2,}", " ");

				//Replace repeating line breaks with single break
				text = Regex.Replace(text, "[\n]{2,}", " ");
		
				text = text.Replace('\n', ' ');

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
		public const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		public LinkedList<Tuple<bool, char>> WrittenChars { get; private set; } = new LinkedList<Tuple<bool, char>>();
		public int CorrectChars { get; private set; } = 0;
		public int IncorrectChars { get; private set; } = 0;
		public int TotalIncorrectChars { get; private set; } = 0;
		public int FixedChars => TotalIncorrectChars - IncorrectChars;

		public int Wpm
		{
			get
			{
				//If last typed chasactes was incorrect, don't apply WPM penalty for that character. Only apply pehalty if user keeps going without fixing.
				var adjustedIncorrectChars = IncorrectChars;
				if (IncorrectChars > 0 && !WrittenChars.First.Value.Item1)
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
		Dictionary<string, string[]> symbolMap;
		Dictionary<string, string[]> letterMap;
		
		public Bible Bible { get; set; }

		public event EventHandler TimeChecked;
		public event EventHandler Finished;

		//public string WrittenTextToDraw => text == null ? "" : text.Substring(startDrawChar, currentCharIndex - startDrawChar);
		public string UnwrittenTextToDraw => text == null ? "" : text.Substring(currentCharIndex, Math.Min(text.Length - currentCharIndex, NumCharsFromCenter));

		public TypingSession()
		{

		}

		public TypingSession(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "startText")
				{
					StartText = (string)entry.Value;
					Shuffle = StartText == null;
				}
				else if (entry.Name == "timeLimit")
					TimeLimit = (TimeSpan)entry.Value;
			}
		}
		virtual public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("startText", Shuffle ? null : TextEntrySource.Title);
			info.AddValue("timeLimit", TimeLimit);
		}

		public void createCharMap(StringReader charMapReader)
		{
			string line;
			string section = null;
			while ((line = charMapReader.ReadLine()) != null)
			{
				Match match;
				if ((match = Regex.Match(line, "[.*]")).Success)
					section = match.Value;
				var split = line.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
				var sources = split[0].Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries);
				var dest = split[1].Trim();
				if (section == "[Symbols]")
					symbolMap.Add(dest, sources);
				else if (section == "[Letters]")
					letterMap.Add(dest, sources);
				else
					throw new FormatException("Incorrect section: " + section);

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

		public bool typeChar(uint keyCode)
		{
			char c = (char)keyCode;
			if (IsFinished || keyCode < 32 && keyCode != KeyCode_Backspace)
				return false;

			if (!stopwatch.IsRunning)
			{
				if (keyCode == KeyCode_Backspace)
					return false;
				startTime();
			}

			//Backspace
			if (c == KeyCode_Backspace)
			{
				//Aiready at beginning?
				if (currentCharIndex == 0)
					return false;

				currentCharIndex--;
				if (WrittenChars.First.Value.Item1)
					CorrectChars--; //Correct char deleted
				else
					IncorrectChars--; //Inorrect char deleted
				WrittenChars.RemoveFirst();
			}
			else //Not backspace
			{
				char currentChar = text[currentCharIndex++];
				bool isCorrect = currentChar == c;
				WrittenChars.AddFirst(new Tuple<bool, char>(isCorrect, currentChar));
				if (isCorrect)
					CorrectChars++;
				else
				{
					IncorrectChars++;
					TotalIncorrectChars++;
				}
				if (currentCharIndex >= text.Length)
					OnFinished();
			}
			return true;
		}

		void startTime()
		{
			stopTime(true);
			stopwatch.Start();
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
			WrittenChars = new LinkedList<Tuple<bool, char>>();
			CorrectChars = IncorrectChars = TotalIncorrectChars = 0;
			currentCharIndex = 0;
		}
	}
}
