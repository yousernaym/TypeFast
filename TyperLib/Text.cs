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
		protected string theText;
		public string TheText
		{
			get => theText;
			set
			{
				theText = value;
				theText = theText.Replace('\n', ' ');
				theText = theText.Replace('\r', ' ');
				theText = theText.Replace('\t', ' ');
				theText = theText.Replace((char)160, ' '); //Convert non-breaking space to regular space
				Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
				theText = regex.Replace(theText, " ");
				reset();
			}
		}
		
		protected int currentCharIndex;
		protected const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		protected LinkedList<Tuple<bool, char>> writtenChars;
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
		bool isFinished => RemainingTime.Ticks == 0;
		Timer checkTimeTimer;
		public event EventHandler TimeChecked;

		protected string writtenTextToDraw => theText == null ? "" : theText.Substring(startDrawChar, currentCharIndex - startDrawChar);
		protected string unwrittenTextToDraw => theText == null ? "" : theText.Substring(currentCharIndex, Math.Min(theText.Length - currentCharIndex, NumCharsFromCenter));

		protected virtual void OnTimeChecked(EventArgs e)
		{
			TimeChecked?.Invoke(this, e);
		}

		private void checkTime(object state)
		{
			OnTimeChecked(new EventArgs());
			if (ElapsedTime.Ticks == 0)
			{
				stopTime();
			}
		}

		public void loadText()
		{
			TheText = File.ReadAllText(Path.Combine("textToType.txt"));
		}

		public void typeChar(uint keyCode)
		{
			char c = (char)keyCode;
			if (isFinished || keyCode == 13 || c == '\t')
				return;
			if (!stopwatch.IsRunning)
				startTime();
			if (c == 8) //Backspace
			{
				if (currentCharIndex == 0)
					return;
				currentCharIndex--;
				writtenChars.RemoveFirst();
			}
			else
			{
				if (currentCharIndex >= theText.Length)
					return;
				char currentChar = theText[currentCharIndex++];
				bool isCorrect = currentChar == c;
				writtenChars.AddFirst(new Tuple<bool, char>(isCorrect, currentChar));
			}
		}

		void startTime()
		{
			stopTime();
			stopwatch.Start();
			checkTimeTimer = new Timer(checkTime, null, 0, 100);
		}

		void stopTime()
		{
			stopwatch.Reset();
			checkTimeTimer?.Dispose();
			OnTimeChecked(new EventArgs());
		}

		protected void reset()
		{
			stopTime();
			writtenChars = new LinkedList<Tuple<bool, char>>();
			currentCharIndex = 0;
		}
	}
}