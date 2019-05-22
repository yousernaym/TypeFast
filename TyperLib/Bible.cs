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
		public abstract class Element<T>
		{
			Random rnd = new Random();
			protected T content = new T;
			Element<T> Next;
			public Element(Element<T> previous)
			{
				previous.Next = this;
			}

			public int getRandomIndex()
			{
				return rnd.Next(0, items.Count);
			}
		}
		public class Book : Element<Chapter>
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
		public class Chapter : Element<string>
		{
			public List<string> Verses => items;
			public Chapter(XElement chapterTag, Chapter previous) : base(previous)
			{
				foreach (var verseTag in chapterTag.Descendants("VERS"))
				{
					var prevVerse = Verses.Count > 0 ? Verses[Verses.Count - 1] : previous.Verses[previous.Verses.Count - 1];
					Verses.Add(new Verse(verseTag.Value, prevVerse));
				}
			}
		}

		public class Verse
		{
			public string Text;
			Verse Nex
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

		public static string getRandomText(int numVerses)
		{
			var bookNumber = rnd.Next(0, Books.Count);
			var chapterNumber = rnd.Next(0, Books.Count);

			var book = Books[bookNumber];
			var chapter = book.Chapters[)];
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