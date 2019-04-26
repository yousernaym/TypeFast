using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace TyperLib
{
	abstract public class Text
	{
		const uint KeyCode_Enter = 13;
		const uint KeyCode_Backspace = 8;
		const uint KeyCode_Escape = 27;

		protected string theText;
		public string TheText
		{
			get => theText;
			set
			{
				theText = value;

				//Change characters to space
				theText = theText.Replace('\n', ' ');
				theText = theText.Replace('\r', ' ');
				theText = theText.Replace('\t', ' ');
				theText = theText.Replace((char)160, ' '); //Convert non-breaking space to regular space

				//Replace repeating characters with single character
				Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
				theText = regex.Replace(theText, " ");
				//regex = new Regex("[-]{2,}", RegexOptions.None);
				//theText = regex.Replace(theText, "-");

				reset();
			}
		}

		protected int currentCharIndex;
		protected const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		protected LinkedList<Tuple<bool, char>> writtenChars;
		public int CorrectChars { get; private set; } = 0;
		public int IncorrectChars { get; private set; } = 0;
		public int TotalIncorrectChars { get; private set; } = 0;
		public int FixedChars => TotalIncorrectChars - IncorrectChars;
		
		public int Wpm => Math.Max((int)((CorrectChars - IncorrectChars * 3) / ElapsedTime.TotalMinutes), 0) / 5;
		public float Accuracy
		{
			get
			{
				float totalChars = (float)(writtenChars.Count);
				if (totalChars == 0)
					return 0;
				
				//Total correct characters can be less than 0 if there has been multiple incorrect attempts at the same character, so clamp at 0
				float totalCorrect = Math.Max(totalChars - TotalIncorrectChars, 0);
				return totalCorrect / totalChars * 100;
			}
		}
		Stopwatch stopwatch = new Stopwatch();
		public TimeSpan TimeLimit = new TimeSpan(0, 1, 0);
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
		bool isFinished => RemainingTime.Ticks == 0 || currentCharIndex >= theText.Length;
		Timer checkTimeTimer;
		public event EventHandler TimeChecked;
		public event EventHandler Finished;

		protected string writtenTextToDraw => theText == null ? "" : theText.Substring(startDrawChar, currentCharIndex - startDrawChar);
		protected string unwrittenTextToDraw => theText == null ? "" : theText.Substring(currentCharIndex, Math.Min(theText.Length - currentCharIndex, NumCharsFromCenter));

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

		public void loadText()
		{
			TheText = File.ReadAllText("textToType.txt");
		}

		public void typeChar(uint keyCode)
		{
			char c = (char)keyCode;
			//if ((args.KeyCode < ' ') || (args.KeyCode > '~'))       //Exit if its a non displayed character
				//return;
			if (isFinished || keyCode == KeyCode_Enter || keyCode == KeyCode_Escape || c == '\t')
				return;

			if (!stopwatch.IsRunning)
			{
				if (keyCode == KeyCode_Backspace)
					return;
				startTime();
			}

			if (c == KeyCode_Backspace)
			{
				if (currentCharIndex == 0)
					return;
				currentCharIndex--;
				if (writtenChars.First.Value.Item1) //if correct
					CorrectChars--;
				else
					IncorrectChars--;
				writtenChars.RemoveFirst();
			}
			else
			{
				char currentChar = theText[currentCharIndex++];
				bool isCorrect = currentChar == c;
				writtenChars.AddFirst(new Tuple<bool, char>(isCorrect, currentChar));
				if (isCorrect)
					CorrectChars++;
				else
				{
					IncorrectChars++;
					TotalIncorrectChars++;
				}
				if (currentCharIndex >= theText.Length)
					OnFinished();
			}
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

		protected void reset()
		{
			stopTime(true);
			writtenChars = new LinkedList<Tuple<bool, char>>();
			CorrectChars = IncorrectChars = TotalIncorrectChars = 0;
			currentCharIndex = 0;
		}
	}
}