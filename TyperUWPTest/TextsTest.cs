
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
			texts.addRecord(100, 90, "title1");
			texts.addRecord(50, 95, "title2");
			texts.addRecord(50, 100, "title4");
			texts.addRecord(200, 10, "title3");
			texts.addRecord(250, 50, "title3");

			//Get 3 records
			var records = texts.getRecords(RecordType.RT_ALL, 3);
			//Verify that we got 3 records
			Assert.IsTrue(records.Length == 3);
			//Verify that the records are sorted highest to lowest wpm
			for (int i = 0; i < records.Length - 1; i++)
				Assert.IsTrue(records[i].WPM >= records[i + 1].WPM);

			//Get 2 records with unique text titles
			records = texts.getRecords(RecordType.RT_BestTexts, 2);
			//Check that we got no more than 2 records
			Assert.IsTrue(records.Length <= 2);
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
			//Check that we got no more than 4 records
			Assert.IsTrue(records.Length <= 4);
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

			//Check that the first (worst) 2 records are 50
			Assert.AreEqual(records[0].WPM, 50);
			Assert.AreEqual(records[1].WPM, 50);

			//Check that the first (worst) record has an accuracy of 95, since 95 is lower than 100
			Assert.AreEqual(records[0].Accuracy, 95);
			//Check that the second worst record has an accuracy of 100
			Assert.AreEqual(records[1].Accuracy, 100);

			//Get all records, highest to lowest
			records = texts.getRecords(RecordType.RT_ALL, 0);
			//Check that we got all 5
			Assert.IsTrue(records.Length == 5);
			//Check that last 2 records are 50
			Assert.AreEqual(records[3].WPM, 50);
			Assert.AreEqual(records[4].WPM, 50);
			//Check that the second last record has an accuracy of 100
			Assert.AreEqual(records[3].Accuracy, 100);
			//Check that the last record has an accuracy of 95, since 95 is lower than 100
			Assert.AreEqual(records[4].Accuracy, 95);
		}
	}
}
