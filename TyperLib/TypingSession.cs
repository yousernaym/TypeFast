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
	public class TypingSession
	{
		enum RndEntity { Chars, Words, Lines, None };
		const uint KeyCode_Enter = 13;
		const uint KeyCode_Backspace = 8;
		const uint KeyCode_Escape = 27;

		TextEntry textEntry = new TextEntry();
		string[] rndElements;
		int rndWordLength;

		public string StartText { get; private set; }
		public bool Shuffle { get; set; } = true;
		public TimeSpan TimeLimit { get; set; } = new TimeSpan(0, 1, 0);


		public TextEntry TextEntry
		{
			get => textEntry;
			set
			{
				textEntry = new TextEntry(value);

				//Replace whitespace with space
				text = text.Replace('\r', ' ');
				text = text.Replace('\t', ' ');
				text = text.Replace((char)160, ' '); //Convert non-breaking space to regular space

				text = text.Replace('“', '"');
				text = text.Replace('”', '"');
				text = text.Replace('‘', '\'');
				text = text.Replace('’', '\'');

				text = text.Replace(((char)8212).ToString(), "--"); //Convert wide non-ascii hyphen to two ascii hyphens
				text = text.Trim();

				//Replace repeating spaces with single space
				Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
				text = regex.Replace(text, " ");

				//Replace repeating line breaks with single break
				regex = new Regex("[\n]{2,}", RegexOptions.None);
				text = regex.Replace(text, " ");

				rndElements = null;
				rndWordLength = -1; //-1 means a space is inserted  between everry element
				if (text.StartsWith("__rnd__"))
				{
					text = text.Replace(" ", "");
					text = text.Replace("\n", "");
					rndElements = text.Substring(7).Select(x => x.ToString()).ToArray();
					rndWordLength = 0; //0 means a space is inserted between a random number of elements
				}
				else if (text.StartsWith("__rndws__"))
					rndElements = text.Substring(9).Split(' ', '\n');
				else if (text.StartsWith("__rndbr__"))
					rndElements = text.Substring(9).Split('\n');

				reset();
				text = text.Replace('\n', ' ');

			}
		}

		string text
		{
			get => textEntry.Text;
			set => textEntry.Text = value;
		}

		protected int currentCharIndex;
		public const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		public LinkedList<Tuple<bool, char>> WrittenChars { get; private set; }
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

				return Math.Max((int)((CorrectChars - adjustedIncorrectChars * 3) / ElapsedTime.TotalMinutes), 0) / 5;
			}
		}
		public float Accuracy
		{
			get
			{
				float totalChars = (float)(WrittenChars.Count);
				if (totalChars == 0)
					return 0;

				//Total correct characters can be less than 0 if there has been multiple incorrect attempts at the same character, so clamp at 0
				float totalCorrect = Math.Max(totalChars - TotalIncorrectChars, 0);
				return totalCorrect / totalChars * 100;
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

		Timer checkTimeTimer;
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
			info.AddValue("startText", Shuffle ? null : TextEntry.Title);
			info.AddValue("timeLimit", TimeLimit);
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
			if (IsFinished || keyCode == KeyCode_Enter || keyCode == KeyCode_Escape || c == '\t')
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

			if (rndElements != null)
			{
				var rnd = new Random();
				var sb = new StringBuilder();
				while (sb.Length < 10000)
				{
					if (rndWordLength == 0)
						rndWordLength = rnd.Next(1, 7);
					sb.Append(rndElements[rnd.Next(rndElements.Length)]);
					if (--rndWordLength <= 0)
						sb.Append(' ');
				}
				text = sb.ToString();
			}
		}

		public virtual void applyStyle()
		{

		}
	}
}
