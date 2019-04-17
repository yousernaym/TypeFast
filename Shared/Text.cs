using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
				currentCharIndex = 0;
				theText = theText.Replace("\n", "");
				theText = theText.Replace("\r", "");
				Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
				theText = regex.Replace(theText, " ");
				writtenChars = new LinkedList<Tuple<bool, char>>();
			}
		}
		
		protected int currentCharIndex;
		protected const int NumCharsFromCenter = 100;
		protected int startDrawChar => Math.Max(currentCharIndex - NumCharsFromCenter, 0);
		protected LinkedList<Tuple<bool, char>> writtenChars;
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
	}
}