using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace TyperLib
{
	public class Collection<T>
	{
		static Random rnd = new Random();
		protected List<T> items = new List<T>();
		public Collection<T> Next { get; private set; }
		public Collection<Collection<T>> Parent { get; private set; }
		public Collection(Collection<T> previous)
		{
			if (previous != null)
				previous.Next = this;
		}
		public T getRandom()
		{
			return items[rnd.Next(0, items.Count)];
		}
	}
	public class Bible : Collection<Book>
	{
		public List<Book> Books => items;

		public Verse CurrentWerse { get; private set; }

		public Bible(Stream xmlStream) : base(null)
		{
			var content = XElement.Load(xmlStream);
			foreach (var bookTag in content.Descendants("BIBLEBOOK"))
			{
				var prevBook = Books.Count > 0 ? Books[Books.Count - 1] : null;
				Books.Add(new Book(bookTag, prevBook));
			}
		}
		public string getRandomText(int length)
		{
			var book = getRandom();
			var chapter = book.getRandom();
			CurrentWerse = chapter.getRandom();
			var text = new StringBuilder("");
			while (text.Length < 10000 && verse.Next != null)
			{
				text.Append(verse.Text);
				text.Append(' ');
				verse = (Verse)verse.Next;
			}
			return text.ToString();
		}
	}

	public class Book : Collection<Chapter>
	{
		public string Name { get; private set; }
		public string ShortName { get; private set; }
		public List<Chapter> Chapters => items;
		public Book(XElement bookTag, Book previous) : base(previous)
		{
			Name = bookTag.Attribute("bname").Value;
			ShortName = bookTag.Attribute("bsname").Value;
			foreach (var chapterTag in bookTag.Descendants("CHAPTER"))
			{
				var prevChapter = Chapters.Count > 0 ? Chapters[Chapters.Count - 1] : previous?.Chapters[previous.Chapters.Count - 1];
				Chapters.Add(new Chapter(chapterTag, prevChapter));
			}
		}
	}
	public class Chapter : Collection<Verse>
	{
		public List<Verse> Verses => items;
		public Chapter(XElement chapterTag, Chapter previous) : base(previous)
		{
			foreach (var verseTag in chapterTag.Descendants("VERS"))
			{
				var prevVerse = Verses.Count > 0 ? Verses[Verses.Count - 1] : previous?.Verses[previous.Verses.Count - 1];
				Verses.Add(new Verse(verseTag.Value, Verses.Count, this, prevVerse));
			}
		}
	}

	public class Verse : Collection<string>
	{
		public string Text => items[0];
		public Verse(string text, Chapter chapter, int verseNumber, Verse previous) : base(previous)
		{
			items.Add(text);
		}
	}
}