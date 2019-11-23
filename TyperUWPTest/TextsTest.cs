
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
			texts = new Texts(null, null);
		}

		[TestMethod]
        public void getRecords()
        {
			var time = TimeSpan.FromSeconds(60);
			texts.addRecord(new Record(100, 120, 30, "", "", 90, time, "title1", false, 0), false);
			texts.addRecord(new Record(50, 120, 30, "", "", 95, time, "title2", false, 0), false);
			texts.addRecord(new Record(50, 120, 30, "", "", 100, time, "title4", false, 0), false);
			texts.addRecord(new Record(200, 120, 30, "", "", 10, time, "title3", false, 0), false);
			texts.addRecord(new Record(250, 120, 30, "", "", 50, time, "title3", false, 0), false);
			texts.addRecord(new Record(250, 120, 30, "", "", 50, time + TimeSpan.FromSeconds(1), "title3", false, 0), false);
			
			//Get 3 records
			var records = texts.getRecords(null, Record.PrimarySortType.Wpm, 3);
			//Verify that we got 3 records
			Assert.IsTrue(records.Length == 3);
			//Verify that the records are sorted highest to lowest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].Wpm >= records[i + 1].Wpm);

			//Get 2 records with unique text titles
			records = texts.getRecords(false, Record.PrimarySortType.Wpm,  2);
			//Check that we got no more than 2 records
			Assert.IsTrue(records.Length <= 2);
			//Check that the records are sorted highest to lowest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].Wpm >= records[i + 1].Wpm);

			//Check that every text title is unique
			var dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.Wpm);
			}

			//Get 4 worst records with unique text titles
			records = texts.getRecords(true, Record.PrimarySortType.Wpm, 4);
			//Check that we got no more than 4 records
			Assert.IsTrue(records.Length <= 4);
			//Check that the records are sorted lowest to highest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].Wpm <= records[i + 1].Wpm);

			//Check that every text title is unique
			dict = new Dictionary<string, int>();
			foreach (var rec in records)
			{
				Assert.IsFalse(dict.ContainsKey(rec.TextTitle));
				dict.Add(rec.TextTitle, rec.Wpm);
			}

			//Check that the first (worst) 2 records are 50
			Assert.AreEqual(50, records[0].Wpm);
			Assert.AreEqual(50, records[1].Wpm);

			//Check that the first (worst) record has an accuracy of 95, since 95 is lower than 100
			Assert.AreEqual(95, records[0].Accuracy);
			//Check that the second worst record has an accuracy of 100
			Assert.AreEqual(100, records[1].Accuracy);

			//Get all records, highest to lowest
			records = texts.getRecords(null, Record.PrimarySortType.Wpm, 0);
			//Check that we got all 6
			Assert.IsTrue(records.Length == 6);
			//Check that last 2 records are 50
			Assert.AreEqual(50, records[4].Wpm);
			Assert.AreEqual(50, records[5].Wpm);
			//Check that the second last record has an accuracy of 100
			Assert.AreEqual(100, records[4].Accuracy);
			//Check that the last record has an accuracy of 95, since 95 is lower than 100
			Assert.AreEqual(95, records[5].Accuracy);
			//Check that the first record has a time of 61 since 61 is greater than 60
			Assert.AreEqual(TimeSpan.FromSeconds(61), records[0].Time);
		}
	}
}
