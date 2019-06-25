using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace TyperLib
{
	internal class TextEntries : IEnumerable<TextEntry>
	{
		SortedDictionary<string, TextEntry> entries = new SortedDictionary<string, TextEntry>(StringComparer.CurrentCultureIgnoreCase);
		//SortedDictionary<string, TextEntry> entries = new SortedDictionary<string, TextEntry>(StringComparer.Create(new CultureInfo("sv-SE"), true));
		public int Count => entries.Count;
		internal void add(TextEntry entry)
		{
			entries[entry.Title] = entry;
		}

		public IEnumerator<TextEntry> GetEnumerator()
		{
			foreach (var entry in entries)
				yield return entry.Value;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		internal bool containsKey(string title)
		{
			if (title == null)
				return false;
			return entries.ContainsKey(title);
		}

		internal int indexOf(string title)
		{
			var indeXable = entries.Keys.ToList();
			return indeXable.FindIndex(k => k == title);
		}

		internal void remove(string title)
		{
			entries.Remove(title);
		}

		internal TextEntry this[string title]
		{
			get => entries[title];
		}
	}

	[Serializable]
	public class TextEntry : ISerializable
	{
		public string Title { get; set; }
		public string Text { get; set; }
		public bool AsciiLetters { get; set; }
		public TextEntry(TextEntry source = null)
		{
			if (source == null)
			{
				Title = Text = "";
				AsciiLetters = false;
			}
			else
			{
				Title = source.Title;
				Text = source.Text;
				AsciiLetters = source.AsciiLetters;
			}
		}

		public TextEntry(string title, string text, bool asciiLetters)
		{
			Title = title;
			Text = text;
			AsciiLetters = asciiLetters;
		}

		public TextEntry(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "title")
					Title = (string)entry.Value;
				else if (entry.Name == "text")
					Text = (string)entry.Value;
				else if (entry.Name == "asciiLetters")
					AsciiLetters = (bool)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("title", Title);
			info.AddValue("text", Text);
			info.AddValue("asciiLetters", AsciiLetters);
		}
	}
}