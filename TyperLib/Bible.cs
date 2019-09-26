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
		Random rnd = new Random();
		protected List<T> items = new List<T>();
		public Collection<T> Next { get; private set; }
		public Collection<Collection<T>> Parent { get; private set; }
		public int Number { get; private set; }
		public Collection(int number, Collection<T> previous)
		{
			Number = number;
			if (previous != null)
				previous.Next = this;
		}
		public T getRandomItem(int minValue = 0, int maxValue = int.MaxValue)
		{
			return items[rnd.Next(0, Math.Min(maxValue, items.Count))];
		}
		public T getItem(int i)
		{
			return items[i];
		}

	}
	public class Bible : Collection<Book>
	{
		public List<Book> Books => items;

		public Verse Currentverse { get; private set; }

		public Bible(Stream xmlStream) : base(-1, null)
		{
			var content = XElement.Load(xmlStream);
			foreach (var bookTag in content.Descendants("BIBLEBOOK"))
			{
				var prevBook = Books.Count > 0 ? Books[Books.Count - 1] : null;
				Books.Add(new Book(bookTag, this, Books.Count, prevBook));
			}
		}
		public string getRandomText(int length)
		{
			var book = getRandomItem(0, 4);
			var chapter = book.getRandomItem();
			var verse = Currentverse = chapter.getRandomItem();
			//book = items[59];
			//chapter = book.getItem(0);
			//verse = chapter.getItem(14);
			var text = new StringBuilder("");
			while (text.Length + verse.Text.Length < length && verse.Next != null)
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
		public Bible Bible { get; private set; }
		public Book(XElement bookTag, Bible bible, int bookNumber, Book previous) : base(bookNumber, previous)
		{
			Name = bookTag.Attribute("bname").Value;
			ShortName = bookTag.Attribute("bsname").Value;
			foreach (var chapterTag in bookTag.Descendants("CHAPTER"))
			{
				var prevChapter = Chapters.Count > 0 ? Chapters[Chapters.Count - 1] : previous?.Chapters[previous.Chapters.Count - 1];
				Chapters.Add(new Chapter(chapterTag, this, Chapters.Count + 1, prevChapter));
			}
		}
	}
	public class Chapter : Collection<Verse>
	{
		public List<Verse> Verses => items;
		public Book Book {get; set; }
		public Chapter(XElement chapterTag, Book book, int chapterNumber, Chapter previous) : base(chapterNumber, previous)
		{
			foreach (var verseTag in chapterTag.Descendants("VERS"))
			{
				Book = book;
				var prevVerse = Verses.Count > 0 ? Verses[Verses.Count - 1] : previous?.Verses[previous.Verses.Count - 1];
				Verses.Add(new Verse(verseTag.Value, this, Verses.Count + 1, prevVerse));
			}
		}
	}

	public class Verse : Collection<string>
	{
		public string Text => items[0];
		public Chapter Chapter { get; private set; }
		public Verse(string text, Chapter chapter, int verseNumber, Verse previous) : base(verseNumber, previous)
		{
			Chapter = chapter;
			items.Add(text);
		}
		new public string ToString()
		{
			return $"{Chapter.Book.ShortName} {Chapter.Number}:{Number}";
		}
	}
}