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
		Record[] records;
		const int
			NumRecords = Texts.MaxRecords,
			Rows = NumRecords,
			Columns = 6,
			WpmCol = 0,
			HighWpmCol = 1,
			MinWpmCol = 2,
			AccCol = 3,
			TimeCol = 4,
			TextCol = 5;
				
		Record.PrimarySortType primarySort;
		Texts texts;
		TypingSession typingSession;

		public delegate void TextTitleClickEH(RecordsView recordsView, RecordsLinkClickEventArgs e);
		public event TextTitleClickEH TextTitleClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			table.init(new string[] { "WPM", "HiWPM", "LoWPM", "Acc %", "Time", "Text" }, 7, 18);
			table.PrimarySortCol = WpmCol;
			primarySort = columnToSortType(table.PrimarySortCol);
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
					if (c == TextCol || c == HighWpmCol || c == MinWpmCol)
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
						cell.HorizontalAlignment = HorizontalAlignment.Right;
						cell.TextAlignment = TextAlignment.Right;
						cell.VerticalAlignment = VerticalAlignment.Center;
						cell.Padding = new Thickness(20, 5, 7, 5);
						cell.FontSize = 18;
						//----------------------------
						setCellWidth(c, cell);
						cell.Foreground = new SolidColorBrush(Colors.White);
						table.addCell(cell);
					}
				}
			}
			table.applyStyle();
		}

		void setCellWidth(int c, TextBlock cell)
		{
			if (c == TimeCol || c == AccCol || c == WpmCol)
			{
				if (c == TimeCol)
					cell.Text = "0:00.00";
				else if (c == AccCol)
					cell.Text = "100.0";
				else if (c == WpmCol)
					cell.Text = "000";
				cell.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				cell.Width = cell.DesiredSize.Width;
				cell.Text = "";
			}
		}

		private void Table_Sort(object sender, SortEventArgs e)
		{
			primarySort = columnToSortType(e.Column);
			syncData();
		}

		private void createSessionBtn_Click(object sender, RoutedEventArgs e)
		{
			string tag = (string)((HyperlinkButton)sender).Tag;
			string textOrTitle = tag.Substring(1);
			int recordsIndex = tag[0] - 1;
			bool momentaryWpmSession = true;
			if (recordsIndex < 0)
			{
				recordsIndex = 0; //Just to avoid out of range exception. The data at the index is irrelevant if momentaryWpmSession is false.
				momentaryWpmSession = false;
			}
			var record = records[recordsIndex];
			TextTitleClick?.Invoke(this, new RecordsLinkClickEventArgs(textOrTitle, momentaryWpmSession, record.HighWpm, record.HighWpmSnippet, record.LowWpm, record.LowWpmSnippet));
		}

		public void syncData(Texts texts, TypingSession typingSession)
		{
			this.texts = texts;
			this.typingSession = typingSession;
			syncData();
		}

		async private void clearBtn_Click(object sender, RoutedEventArgs e)
		{
			var cd = new ContentDialog { PrimaryButtonText = "Yes", CloseButtonText = "No", Content = "Are you sure you want to clear all stats?" };
			ContentDialogResult result = await cd.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				texts.clearStats();
				syncData();
			}
		}

		private void TopTexts_Click(object sender, RoutedEventArgs e)
		{
			bottomTextsCb.IsChecked = false;
			syncData();
		}

		private void BottomTexts_Click(object sender, RoutedEventArgs e)
		{
			topTextsCb.IsChecked = false;
			syncData();
		}

		static bool showSecondFractions(TimeSpan time)
		{
			return time.TotalSeconds < 10;
		}

		bool? bottomTexts
		{
			get
			{
				if ((bool)bottomTextsCb.IsChecked)
					return true;
				else if ((bool)topTextsCb.IsChecked)
					return false;
				else
					return null;
			}
		}
		void syncData()
		{ 
			records = texts.getRecords(bottomTexts, primarySort, NumRecords);

			for (int i = 0; i < NumRecords; i++)
			{
				if (i < records.Length)
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = records[i].Wpm.ToString();
					var accCell = table.getCell<TextBlock>(i + 1, AccCol);
					accCell.Foreground = getAccuracyCol(records[i].Accuracy);
					accCell.Text = records[i].Accuracy.ToString("0.0");
					var format = "m\\:ss";
					if (showSecondFractions(records[i].Time))
						format += "\\.ff";
					table.getCell<TextBlock>(i + 1, TimeCol).Text = records[i].Time.ToString(format);
					setLinkText(i + 1, TextCol, records);
					setLinkText(i + 1, HighWpmCol, records);
					setLinkText(i + 1, MinWpmCol, records);
				}
				else
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = "";
					table.getCell<TextBlock>(i + 1, AccCol).Text = "";
					table.getCell<TextBlock>(i + 1, TimeCol).Text = "";
					setLinkText(i + 1, TextCol, null);
					setLinkText(i + 1, HighWpmCol, null);
					setLinkText(i + 1, MinWpmCol, null);
				}
			}
			totalWordsTbk.Text = "Words: " + typingSession.GlobalStats.TotalWords;
			uniqueWordsTbk.Text = "Unique words: " + typingSession.GlobalStats.UniqueWords;
			//avgWordWpm.Text = "Avg top wpm: " + typingSession.GlobalStats.AvgTopWpm;
		}

		private void setLinkText(int row, int col, Record[] records)
		{
			var cell = table.getCell<HyperlinkButton>(row, col);
			int recordIndex = row - 1;
			if (records == null || col == HighWpmCol && records[recordIndex].HighWpm < 0 || col == MinWpmCol && records[recordIndex].LowWpm < 0)
			{
				ToolTipService.SetToolTip(cell, null);
				cell.Content = "";
				cell.SetValue(AutomationProperties.NameProperty, "");
				cell.IsTabStop = false;
			}
			else
			{
				ToolTip toolTip = new ToolTip();
				cell.IsTabStop = true;
				if (col == TextCol)
				{
					toolTip = null;
					cell.Content = records[recordIndex].TextTitle;
					char c = (char)0;
					cell.Tag = c.ToString() + cell.Content.ToString();
					var timeText = records[recordIndex].Time.ToSpeechString(showSecondFractions(records[recordIndex].Time));
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].TextTitle}. {records[recordIndex].Wpm} words per minute. {records[recordIndex].Accuracy} percent accuracy. {timeText}.");
				}
				else if (col == HighWpmCol)
				{
					toolTip.Content = records[recordIndex].HighWpmSnippet;
					cell.Content = records[recordIndex].HighWpm < 0 ? "" : records[recordIndex].HighWpm.ToString();
					char c = (char)(recordIndex + 1);
					cell.Tag = c.ToString() + records[recordIndex].HighWpmSnippet;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].HighWpm}");
				}
				else if (col == MinWpmCol)
				{
					toolTip.Content = records[recordIndex].LowWpmSnippet;
					cell.Content = records[recordIndex].LowWpm.ToString();
					char c = (char)(recordIndex + 1);
					cell.Tag = c.ToString() + records[recordIndex].LowWpmSnippet;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].LowWpm}");
				}
				ToolTipService.SetToolTip(cell, toolTip);
			}
		}

		static Record.PrimarySortType columnToSortType(int col)
		{
			Record.PrimarySortType primarySortType;
			if (col == WpmCol)
				primarySortType = Record.PrimarySortType.Wpm;
			else if (col == HighWpmCol)
				primarySortType = Record.PrimarySortType.MaxWpm;
			else if (col == MinWpmCol)
				primarySortType = Record.PrimarySortType.MinWpm;
			else
				throw new ArgumentException("Parameter must match element of Record.PrimarySortType", nameof(col));
			return primarySortType;
		}

		public static SolidColorBrush getAccuracyCol(float acc)
		{
			acc =(float)Math.Round(acc, 1); //float.ToString rounds to nearest more significant decimal, so do the same rounding here so that the number displayed matches the color
			if (acc < 92)
				return new SolidColorBrush(Colors.Red);
			else if (acc < 95)
				//return new SolidColorBrush(Color.FromArgb(255, 255, 50, 70));
				return new SolidColorBrush(Colors.Orange);
				else if (acc < 97)
				return new SolidColorBrush(Colors.Yellow);
			else
				return new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
				//return new SolidColorBrush(Colors.LawnGreen);
		}
	}

	public class RecordsLinkClickEventArgs : EventArgs
	{
		public string TextOrTitle { get; set; }
		public bool MomentaryWpmClicked { get; set; }
		public int HighWpm { get; set; }
		public string HighWpmSnippet { get; set; }
		public int LowWpm { get; set; }
		public string LowWpmSnippet { get; set; }

		public RecordsLinkClickEventArgs(string textOrTitle, bool momentaryWpmClicked, int highWpm = 0, string highWpmSnippet = null, int lowWpm = 0, string lowWpmSnippet = null)
		{
			TextOrTitle = textOrTitle;
			MomentaryWpmClicked = momentaryWpmClicked;
			HighWpm = highWpm;
			HighWpmSnippet = highWpmSnippet;
			LowWpm = lowWpm;
			LowWpmSnippet = lowWpmSnippet;
		}
	}
}
