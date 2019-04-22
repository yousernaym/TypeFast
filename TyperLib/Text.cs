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
		int correctChars = 0;
		int incorrectChars = 0;
		Stopwatch stopwatch = new Stopwatch();
		public TimeSpan TimeLimit = new TimeSpan(0, 0, 10);
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
			if (ElapsedTime.Ticks == 0)
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
			TheText = File.ReadAllText(Path.Combine("textToType.txt"));
		}

		public void typeChar(uint keyCode)
		{
			char c = (char)keyCode;
			if (isFinished || keyCode == KeyCode_Enter || c == '\t')
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
				writtenChars.RemoveFirst();
			}
			else
			{
				char currentChar = theText[currentCharIndex++];
				bool isCorrect = currentChar == c;
				writtenChars.AddFirst(new Tuple<bool, char>(isCorrect, currentChar));
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
			correctChars = incorrectChars = 0;
			currentCharIndex = 0;
		}
	}
}