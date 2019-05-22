using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace TyperLib
{
	static public class Bible
	{
		public class ColleCtion<T>
		{
			Random rnd = new Random();
			protected List<T> items = new List<T>();
			ColleCtion<T> Next;
			public ColleCtion(ColleCtion<T> previous)
			{
				previous.Next = this;
			}
			public T getRandom()
			{
				return items[rnd.Next(0, items.Count)];
			}
		}
		public class Books : ColleCtion<Book>
		{
			public Books(Books previous) : base(previous)
			{
			}
		}
		public class Book : ColleCtion<Chapter>
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
					var prevChapter = Chapters.Count > 0 ? Chapters[Chapters.Count - 1] : previous.Chapters[previous.Chapters.Count - 1];
					Chapters.Add(new Chapter(chapterTag, prevChapter));
				}
			}
		}
		public class Chapter : ColleCtion<Verse>
		{
			public List<Verse> Verses => items;
			public Chapter(XElement chapterTag, Chapter previous) : base(previous)
			{
				foreach (var verseTag in chapterTag.Descendants("VERS"))
				{
					var prevVerse = Verses.Count > 0 ? Verses[Verses.Count - 1] : previous.Verses[previous.Verses.Count - 1];
					Verses.Add(new Verse(verseTag.Value, prevVerse));
				}
			}
		}

		public class Verse : ColleCtion<string>
		{
			public string Text => items[0];
			public Verse(string text, Verse previous) : base(previous)
			{
				items.Add(text);
			}
		}

		static public List<Book> Books { get; private set; }
		static XElement content;
		
		static public void Init(Stream xmlStream)
		{
			content = XElement.Load(xmlStream);
			Books = new List<Book>();
			foreach (var bookTag in content.Descendants("BIBLEBOOK"))
			{
				var prevBook = Books.Count > 0 ? Books[Books.Count - 1] : null;
				Books.Add(new Book(bookTag, prevBook));
			}
		}

		public static string getRandomText(int numVerses)
		{
			var book = Books
			var verse = chapter.Verses[rnd.Next(0, chapter.Verses.Count)];]

			StringBuilder text = "";
			while (text.Length < 10000)
			{
				text.Append(verse);
				verse = getNextVerse(bookNumber, );
			}
		}
	}
}