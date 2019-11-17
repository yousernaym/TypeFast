﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TyperLib;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TyperUWP
{
 	public sealed partial class RecordsView : UserControl
	{
		const int
			NumRecords = Texts.MaxRecords,
			Rows = NumRecords,
			Columns = 6,
			WpmCol = 0,
			MinWpmCol = 1,
			MaxWpmCol = 2,
			AccCol = 3,
			TimeCol = 4,
			TextCol = 5;
		
		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		bool ascendingSort = false;
		RecordElem primarySort;
		Texts texts;

		public delegate void TextTitleClickEH(RecordsView recordsView, TextTitleClickEventArgs e);
		public event TextTitleClickEH TextTitleClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			table.init(new string[] { "WPM", "Max WPM", "Min WPM", "Acc %", "Time", "Text" }, 31, 18);
			table.PrimarySortCol = WpmCol;
			primarySort = columnToRecordElem(table.PrimarySortCol);
			table.Sort += Table_Sort;
			for (int r = 0; r < Rows; r++)
			{
				table.addRow();
				for (int c = 0; c < Columns; c++)
				{
					//if (c == WpmCol)
					//{
					//	cell.FontWeight = FontWeights.Bold;
					//}
					if (c == TextCol || c == MaxWpmCol || c == MinWpmCol)
					{
						var cell = new HyperlinkButton();
						//Duplicate init code---------
						cell.HorizontalAlignment = c == TextCol ? HorizontalAlignment.Left : HorizontalAlignment.Right;
						cell.VerticalAlignment = VerticalAlignment.Center;
						cell.Padding = new Thickness(20, 5, 7, 5);
						if (c == TextCol)
							cell.Padding = new Thickness(7, 5, 7, 5);
						cell.FontSize = 18;
						//----------------------------
						cell.Click += createSessionBtn_Click;
						table.addCell(cell);
					}
					else
					{
						var cell = new TextBlock();
						
						//Duplicate init code---------
						cell.HorizontalAlignment = c == WpmCol || c == AccCol ? HorizontalAlignment.Right : HorizontalAlignment.Left;
						cell.VerticalAlignment = VerticalAlignment.Center;
						cell.Padding = new Thickness(20, 5, 7, 5);
						if (c == TextCol)
							cell.Padding = new Thickness(7, 5, 7, 5);
						cell.FontSize = 18;
						//----------------------------
						cell.Foreground = new SolidColorBrush(Colors.White);
						table.addCell(cell);
					}
				}
			}
		}

		private void Table_Sort(object sender, SortEventArgs e)
		{
			ascendingSort = e.Ascend;
			primarySort = columnToRecordElem(e.Column);
			syncGrid();
		}

		private void createSessionBtn_Click(object sender, RoutedEventArgs e)
		{
			string tag = (string)((HyperlinkButton)sender).Tag;
			bool tempSessionText = int.Parse(tag[0].ToString()) == 1;
			string textOrTitle = tag.Substring(1);			
			TextTitleClick?.Invoke(this, new TextTitleClickEventArgs(textOrTitle, tempSessionText));
		}

		private void MaxMinWpm_Click(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			throw new NotImplementedException();
		}

		public void syncGrid(Texts texts)
		{
			this.texts = texts;
			syncGrid();
		}

		private void filterCb_Click(object sender, RoutedEventArgs e)
		{
			syncGrid();
		}

		bool showSecondFractions(TimeSpan time)
		{
			return time.TotalSeconds < 10;
		}

		void syncGrid()
		{ 
			var records = texts.getRecords((bool)filterCb.IsChecked, primarySort, ascendingSort, NumRecords);

			for (int i = 0; i < NumRecords; i++)
			{
				if (i < records.Length)
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = records[i].Wpm.ToString();
					table.getCell<TextBlock>(i + 1, AccCol).Text = records[i].Accuracy.ToString("0.0");
					var format = "m\\:ss";
					if (showSecondFractions(records[i].Time))
						format += "\\.ff";
					table.getCell<TextBlock>(i + 1, TimeCol).Text = records[i].Time.ToString(format);
					setLinkText(i + 1, TextCol, records);
					setLinkText(i + 1, MaxWpmCol, records);
					setLinkText(i + 1, MinWpmCol, records);
				}
				else
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = "";
					table.getCell<TextBlock>(i + 1, AccCol).Text = "";
					table.getCell<TextBlock>(i + 1, TimeCol).Text = "";
					setLinkText(i + 1, TextCol, null);
					setLinkText(i + 1, MaxWpmCol, null);
					setLinkText(i + 1, MinWpmCol, null);
				}
			}
		}

		private void setLinkText(int row, int col, Record[] records)
		{
			var cell = table.getCell<HyperlinkButton>(row, col);
			int recordIndex = row - 1;
			if (records == null)
			{
				cell.Content = "";
				cell.SetValue(AutomationProperties.NameProperty, "");
			}
			else
			{
				if (col == TextCol)
				{
					cell.Content = records[recordIndex].TextTitle;
					cell.Tag = "0" + cell.Content.ToString();
					var timeText = records[recordIndex].Time.ToSpeechString(showSecondFractions(records[recordIndex].Time));
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].TextTitle}. {records[recordIndex].Wpm} words per minute. {records[recordIndex].Accuracy} percent accuracy. {timeText}.");
				}
				else if (col == MaxWpmCol)
				{
					cell.Content = records[recordIndex].MaxWpm.ToString();
					cell.Tag = "1" + records[recordIndex].MaxWpmText;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].MaxWpm} max words per minute.");
				}
				else if (col == MinWpmCol)
				{
					cell.Content = records[recordIndex].MinWpm.ToString();
					cell.Tag = "1" + records[recordIndex].MinWpmText;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].MinWpm} min words per minute.");
				}
			}
		}

		RecordElem columnToRecordElem(int col)
		{
			RecordElem recElem;
			if (col == WpmCol)
				recElem = RecordElem.Wpm;
			else if (col == AccCol)
				recElem = RecordElem.Acc;
			else if (col == TimeCol)
				recElem = RecordElem.Time;
			else if (col == MaxWpmCol)
				recElem = RecordElem.MaxWpm;
			else /*if (col == MinWpmCol)*/
				recElem = RecordElem.MinWpm;
			return recElem;
		}
	}

	public class TextTitleClickEventArgs : EventArgs
	{
		public string TextOrTitle { get; set; }
		public bool TempSession { get; set; }
		public TextTitleClickEventArgs(string textOrTitle, bool tempSession) 
		{
			TextOrTitle = textOrTitle;
			TempSession = tempSession;
		}
	}
}
