
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
			texts = new Texts("");
		}

		[TestMethod]
        public void getRecords()
        {
			texts.addRecord(100, "title1");
			texts.addRecord(50, "title2");
			texts.addRecord(50, "title4");
			texts.addRecord(200, "title3");
			texts.addRecord(250, "title3");

			var records = texts.getRecords(RecordType.RT_ALL, 3);
			Assert.AreEqual(records.Length, 3);
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM >= records[i + 1].WPM);

			records = texts.getRecords(RecordType.RT_BestTexts, 2);
			Assert.AreEqual(records.Length, 2);
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM >= records[i + 1].WPM);
			var dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.WPM);
			}

			records = texts.getRecords(RecordType.RT_WorstTexts, 4);
			Assert.AreEqual(records.Length, 4);
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM <= records[i + 1].WPM);
			dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.WPM);
			}
		}
	}
}
