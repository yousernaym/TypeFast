using System;
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
		Record[] textRecords;
		WordStats[] wordRecords;
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
				
		Record.PrimarySortType primaryTextSort;
		Texts texts;
		TypingSession typingSession;
		
		public delegate void TextTitleClickEH(RecordsView recordsView, CreateSessionLinkClickEventArgs e);
		public event TextTitleClickEH CreateSessionLinkClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			initTextsTable();
			initWordsTable();
		}

		void initTextsTable()
		{
			textsTable.init(new string[] { "WPM", "HiWPM", "LoWPM", "Acc %", "Time", "Text" }, 7, 18);
			textsTable.PrimarySortCol = WpmCol;
			primaryTextSort = columnToSortType(textsTable.PrimarySortCol);
			textsTable.Sort += Table_Sort;
			for (int r = 0; r < Rows; r++)
			{
				textsTable.addRow();
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
						cell.Click += textRecordsBtn_Click;
						textsTable.addCell(cell);
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
						textsTable.addCell(cell);
					}
				}
			}
			textsTable.applyStyle();
		}

		void initWordsTable()
		{
			wordsTable.init(new string[] { "WPM", "Word" }, 1, 18);
			wordsTable.PrimarySortCol = WpmCol;
			for (int r = 0; r < Rows; r++)
			{
				wordsTable.addRow();
				for (int c = 0; c < 2; c++)
				{
					if (c == 1)
					{
						var cell = new HyperlinkButton();
						//Duplicated init code---------
						cell.HorizontalAlignment = HorizontalAlignment.Left;
						cell.VerticalAlignment = VerticalAlignment.Center;
						cell.Padding = new Thickness(7, 5, 7, 5);
						cell.FontSize = 18;
						//----------------------------
						cell.Click += wordRecordsBtn_Click;
						wordsTable.addCell(cell);
					}
					else
					{
						var cell = new TextBlock();
						//Duplicated init code---------
						cell.HorizontalAlignment = HorizontalAlignment.Right;
						cell.TextAlignment = TextAlignment.Right;
						cell.VerticalAlignment = VerticalAlignment.Center;
						cell.Padding = new Thickness(20, 5, 7, 5);
						cell.FontSize = 18;
						//----------------------------
						setCellWidth(c, cell);
						cell.Foreground = new SolidColorBrush(Colors.White);
						wordsTable.addCell(cell);
					}
				}
			}
			wordsTable.applyStyle();
		}

		static void setCellWidth(int c, TextBlock cell) 
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
			primaryTextSort = columnToSortType(e.Column);
			syncData();
		}

		private void textRecordsBtn_Click(object sender, RoutedEventArgs e)
		{
			string tag = (string)((HyperlinkButton)sender).Tag;
			string textOrTitle = tag.Substring(1);
			int recordsIndex = tag[0] - 1;
			SessionType sessionType = SessionType.MomentaryWpm;
			if (recordsIndex < 0)
			{
				recordsIndex = 0; //Just to avoid out of range exception. The data at the index is irrelevant if not a momentaryWpmSession.
				sessionType = SessionType.Title;
			}
			var record = textRecords[recordsIndex];
			CreateSessionLinkClick?.Invoke(this, new CreateSessionLinkClickEventArgs(textOrTitle, sessionType, record.HighWpm, record.HighWpmSnippet, record.LowWpm, record.LowWpmSnippet));
		}

		private void wordRecordsBtn_Click(object sender, RoutedEventArgs e)
		{
			string word = (string)((HyperlinkButton)sender).Content;
			CreateSessionLinkClick?.Invoke(this, new CreateSessionLinkClickEventArgs(word, SessionType.Temp, 0, null, 0, null));
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
		
		private void fastestTextsCb_Click(object sender, RoutedEventArgs e)
		{
			slowestTextsCb.IsChecked = false;
			slowestWordsCb.IsChecked = false;
			syncData();
		}

		private void slowestTextsCb_Click(object sender, RoutedEventArgs e)
		{
			fstestTextsCb.IsChecked = false;
			slowestWordsCb.IsChecked = false;
			syncData();
		}

		private void slowestWordsCb_Click(object sender, RoutedEventArgs e)
		{
			fstestTextsCb.IsChecked = false;
			slowestTextsCb.IsChecked = false;
			syncData();
		}

		static bool showSecondFractions(TimeSpan time)
		{
			return time.TotalSeconds < 10;
		}

		Texts.RecordSortMethod textRecordSortMethod
		{
			get
			{
				if ((bool)slowestTextsCb.IsChecked)
					return Texts.RecordSortMethod.SlowestTexts;
				else if ((bool)fstestTextsCb.IsChecked)
					return Texts.RecordSortMethod.FastestTexts;
				else
					return Texts.RecordSortMethod.FastestSessions;
			}
		}

		void syncData()
		{
			if ((bool)slowestWordsCb.IsChecked)
				syncWordRecords();
			else
				syncTextRecords();
			totalWordsTbk.Text = "Words: " + typingSession.GlobalStats.TotalWords;
			uniqueWordsTbk.Text = "Unique words: " + typingSession.GlobalStats.UniqueWords;
			//avgWordWpm.Text = "Avg top wpm: " + typingSession.GlobalStats.AvgTopWpm;
		}

		void syncTextRecords()
		{
			textsTable.Visibility = Visibility.Visible;
			wordsTable.Visibility = Visibility.Collapsed;
			textRecords = texts.getRecords(textRecordSortMethod, primaryTextSort, NumRecords);

			for (int i = 0; i < NumRecords; i++)
			{
				if (i < textRecords.Length)
				{
					textsTable.getCell<TextBlock>(i + 1, WpmCol).Text = textRecords[i].Wpm.ToString();
					var accCell = textsTable.getCell<TextBlock>(i + 1, AccCol);
					accCell.Foreground = getAccuracyCol(textRecords[i].Accuracy);
					accCell.Text = textRecords[i].Accuracy.ToString("0.0");
					var format = "m\\:ss";
					if (showSecondFractions(textRecords[i].Time))
						format += "\\.ff";
					textsTable.getCell<TextBlock>(i + 1, TimeCol).Text = textRecords[i].Time.ToString(format);
					setLinkText(i + 1, TextCol, textRecords);
					setLinkText(i + 1, HighWpmCol, textRecords);
					setLinkText(i + 1, MinWpmCol, textRecords);
				}
				else
				{
					textsTable.getCell<TextBlock>(i + 1, WpmCol).Text = "";
					textsTable.getCell<TextBlock>(i + 1, AccCol).Text = "";
					textsTable.getCell<TextBlock>(i + 1, TimeCol).Text = "";
					setLinkText(i + 1, TextCol, null);
					setLinkText(i + 1, HighWpmCol, null);
					setLinkText(i + 1, MinWpmCol, null);
				}
			}
		}

		void syncWordRecords()
		{
			textsTable.Visibility = Visibility.Collapsed;
			wordsTable.Visibility = Visibility.Visible;
			wordRecords = texts.GlobalStats.getSlowestWords(NumRecords);
			for (int i = 0; i < NumRecords; i++)
			{
				var wpmCell = wordsTable.getCell<TextBlock>(i + 1, WpmCol);
				var wordCell = wordsTable.getCell<HyperlinkButton>(i + 1, 1);
				if (i < wordRecords.Length)
				{
					wpmCell.Text = wordRecords[i].TopWpm.ToString();
					wordCell.Content = wordRecords[i].Word;
					wordCell.SetValue(AutomationProperties.NameProperty, wordCell.Content);
				}
				else
				{
					wpmCell.Text = "";
					ToolTipService.SetToolTip(wordCell, null);
					wordCell.Content = "";
					wordCell.SetValue(AutomationProperties.NameProperty, "");
					wordCell.IsTabStop = false;
				}
			}
	}

		private void setLinkText(int row, int col, Record[] records)
		{
			var cell = textsTable.getCell<HyperlinkButton>(row, col);
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

	public class CreateSessionLinkClickEventArgs : EventArgs
	{
		public string TextOrTitle { get; set; }
		public SessionType SessionType { get; set; }
		public int HighWpm { get; set; }
		public string HighWpmSnippet { get; set; }
		public int LowWpm { get; set; }
		public string LowWpmSnippet { get; set; }

		public CreateSessionLinkClickEventArgs(string textOrTitle, SessionType sessionType, int highWpm = 0, string highWpmSnippet = null, int lowWpm = 0, string lowWpmSnippet = null)
		{
			TextOrTitle = textOrTitle;
			SessionType = sessionType;
			HighWpm = highWpm;
			HighWpmSnippet = highWpmSnippet;
			LowWpm = lowWpm;
			LowWpmSnippet = lowWpmSnippet;
		}
	}
}
