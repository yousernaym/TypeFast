using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TyperLib
{
	internal class TextEntries : IEnumerable<TextEntry>
	{
		SortedDictionary<string, TextEntry> entries = new SortedDictionary<string, TextEntry>();
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
		public TextEntry(TextEntry source = null)
		{
			if (source == null)
				Title = Text = "";
			else
			{
				Title = source.Title;
				Text = source.Text;
			}
		}

		public TextEntry(string title, string text)
		{
			Title = title;
			Text = text;
		}

		public TextEntry(SerializationInfo info, StreamingContext context)
		{
			foreach (var entry in info)
			{
				if (entry.Name == "title")
					Title = (string)entry.Value;
				else if (entry.Name == "text")
					Text = (string)entry.Value;
			}
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("title", Title);
			info.AddValue("text", Text);
		}
	}
}