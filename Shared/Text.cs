﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace TyperShared
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
			}
		}
		
		protected int currentCharIndex;
		protected const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		protected LinkedList<Tuple<bool, char>> writtenChars;
		Stopwatch stopwatch;
		public TimeSpan TimeLimit;
		public TimeSpan RemainingTime => TimeLimit -
		public TimeSpan ElapsedTime => stopwatch.Elapsed;

		protected string writtenTextToDraw => theText == null ? "" : theText.Substring(startDrawChar, currentCharIndex - startDrawChar);
		protected string unwrittenTextToDraw => theText == null ? "" : theText.Substring(currentCharIndex, Math.Min(theText.Length - currentCharIndex, NumCharsFromCenter));

		public void loadText()
		{
			TheText = File.ReadAllText(Path.Combine("textToType.txt"));
		}

		public void typeChar(char c)
		{
			if (c == 8)
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

		protected void reset()
		{
			stopwatch.Reset();
			writtenChars = new LinkedList<Tuple<bool, char>>();
			currentCharIndex = 0;
		}

		protected void start()
		{
			stopwatch.Start();
		}
	}
}