
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TyperLib;

namespace TyperUWPTest
{
    [TestClass]
    public class TextsTest
    {
		Texts texts;
		[TestInitialize]
		public void init()
		{
			texts = new Texts(null);
		}

		[TestMethod]
        public void getRecords()
        {
			texts.addRecord(100, "title1");
			texts.addRecord(50, "title2");
			texts.addRecord(50, "title4");
			texts.addRecord(200, "title3");
			texts.addRecord(250, "title3");

			//Get 3 records
			var records = texts.getRecords(RecordType.RT_ALL, 3);
			//Verify that we got 3 records
			Assert.AreEqual(records.Length, 3);
			//Verify that the records are sorted highest to lowest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM >= records[i + 1].WPM);

			//Get 2 records with unique text titles
			records = texts.getRecords(RecordType.RT_BestTexts, 2);
			//Check that we got 2 records
			Assert.AreEqual(records.Length, 2);
			//Check that the records are sorted highest to lowest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM >= records[i + 1].WPM);

			//Check that every text title is unique
			var dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.WPM);
			}

			//Get 4 worst records with unique text titles
			records = texts.getRecords(RecordType.RT_WorstTexts, 4);
			//Check that we got 4 records
			Assert.AreEqual(records.Length, 4);
			//Check that the records are sorted lowest to highest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM <= records[i + 1].WPM);

			//Check that every text title is unique
			dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.WPM);
			}
		}
	}
}
