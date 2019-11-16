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
		const int
			NumRecords = Texts.MaxRecordsPerText,
			Rows = NumRecords,
			Columns = 4,
			WpmCol = 0,
			AccCol = 1,
			TimeCol = 2,
			TextCol = 3;
		
		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		RecordType currentRecordType = RecordType.RT_BestSessions;
		int sortCol = WpmCol;
		bool ascendingSort = false;
		Texts texts;

		public delegate void TextTitleClickEH(RecordsView recordsView, TextTitleClickEventArgs e);
		public event TextTitleClickEH TextTitleClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			table.init(new string[] { "WPM", "Acc %", "Time", "Text" }, 7, 18);
			table.Sort += Table_Sort;
			for (int r = 0; r < Rows; r++)
			{
				table.addRow();
				for (int c = 0; c < Columns; c++)
				{
					var cell = new TextBlock();
					cell.HorizontalAlignment = c == WpmCol || c == AccCol ? HorizontalAlignment.Right : HorizontalAlignment.Left;
					cell.VerticalAlignment = VerticalAlignment.Center;
					cell.Foreground = new SolidColorBrush(Colors.White);
					cell.FontSize = 18;
					cell.Padding = new Thickness(20, 5, 7, 5);
					//cell.ContextFlyout = recordCM;
					if (c == TextCol)
						cell.Padding = new Thickness(7, 5, 7, 5);

					if (c == WpmCol)
					{
						cell.FontWeight = FontWeights.Bold;
					}
					else if (c == TextCol)
					{
						var link = new Hyperlink();
						var run = new Run();
						link.Inlines.Add(run);
						cell.Inlines.Add(link);
						link.Click += TextTitle_Click;
					}

					table.addCell(cell);
				}
			}
			//table.getCell<TextBlock>(0, WpmCol).Text = "WPM";
			//table.getCell<TextBlock>(0, AccCol).Text = "Acc %";
			//table.getCell<TextBlock>(0, TimeCol).Text = "Time";
			//table.getCell<TextBlock>(0, TextCol).Text = "Text";
		}

		private void Table_Sort(object sender, SortEventArgs e)
		{
			ascendingSort = e.Ascend;
			Record.PrimarySort = (RecordElem)e.Column;
			syncGrid();
		}

		private void TextTitle_Click(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			var title = ((Run)sender.Inlines[0]).Text;
			TextTitleClick?.Invoke(this, new TextTitleClickEventArgs(title));
		}

		public void syncGrid(Texts texts)
		{
			this.texts = texts;
			syncGrid();
		}

		bool showSecondFractions(TimeSpan time)
		{
			return time.TotalSeconds < 10;
		}

		void syncGrid()
		{ 
			var records = texts.getRecords(currentRecordType, ascendingSort, NumRecords);

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
				}
				else
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = "";
					table.getCell<TextBlock>(i + 1, AccCol).Text = "";
					table.getCell<TextBlock>(i + 1, TimeCol).Text = "";
					setLinkText(i + 1, TextCol, null);
				}
			}
		}

		private void setLinkText(int row, int col, Record[] records)
		{
			var textBlock = table.getCell<TextBlock>(row, col);
			var titleLink = (Hyperlink)textBlock.Inlines[0];
			var titleRun = (Run)titleLink.Inlines[0];

			int recordIndex = row - 1;
			if (records == null)
			{
				titleRun.Text = "";
				titleLink.SetValue(AutomationProperties.NameProperty, "");
			}
			else
			{
				titleRun.Text = records[recordIndex].TextTitle;
				var timeText = records[recordIndex].Time.ToSpeechString(showSecondFractions(records[recordIndex].Time));
				titleLink.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].TextTitle}. {records[recordIndex].Wpm} words per minute. {records[recordIndex].Accuracy} percent accuracy. {timeText}.");

			}
		}

		private void AllRBtn_Click(object sender, RoutedEventArgs e)
		{
			currentRecordType = RecordType.RT_BestSessions;
			syncGrid();
		}

		private void BestTextsRBtn_Click(object sender, RoutedEventArgs e)
		{
			currentRecordType = RecordType.RT_BestTexts;
			syncGrid();
		}

		private void WorstTextsRBtn_Click(object sender, RoutedEventArgs e)
		{
			currentRecordType = RecordType.RT_WorstTexts;
			syncGrid();
		}
	}

	public class TextTitleClickEventArgs : EventArgs
	{
		public string Title { get; set; }

		public TextTitleClickEventArgs(string title) 
		{
			Title = title;
		}
	}
}
