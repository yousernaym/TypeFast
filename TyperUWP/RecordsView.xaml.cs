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
			NumRecords = Texts.MaxRecords,
			Rows = NumRecords,
			Columns = 6,
			MaxWpmCol = 0,
			WpmCol = 1,
			MinWpmCol = 2,
			AccCol = 3,
			TimeCol = 4,
			TextCol = 5;
				
		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		Record.PrimarySortType primarySort;
		Texts texts;

		public delegate void TextTitleClickEH(RecordsView recordsView, TextTitleClickEventArgs e);
		public event TextTitleClickEH TextTitleClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			table.init(new string[] { "HiWPM", "WPM", "LoWPM", "Acc %", "Time", "Text" }, 7, 18);
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
			syncGrid();
		}

		private void createSessionBtn_Click(object sender, RoutedEventArgs e)
		{
			string tag = (string)((HyperlinkButton)sender).Tag;
			bool tempSessionText = int.Parse(tag[0].ToString()) == 1;
			string textOrTitle = tag.Substring(1);			
			TextTitleClick?.Invoke(this, new TextTitleClickEventArgs(textOrTitle, tempSessionText));
		}

		public void syncGrid(Texts texts)
		{
			this.texts = texts;
			syncGrid();
		}

		private void TopTexts_Click(object sender, RoutedEventArgs e)
		{
			bottomTextsCb.IsChecked = false;
			syncGrid();
		}

		private void BottomTexts_Click(object sender, RoutedEventArgs e)
		{
			topTextsCb.IsChecked = false;
			syncGrid();
		}

		bool showSecondFractions(TimeSpan time)
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
		void syncGrid()
		{ 
			var records = texts.getRecords(bottomTexts, primarySort, NumRecords);

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
			if (records == null || col == MaxWpmCol && records[recordIndex].MaxWpm < 0 || col == MinWpmCol && records[recordIndex].MinWpm < 0)
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
					cell.Content = records[recordIndex].TextTitle;
					cell.Tag = "0" + cell.Content.ToString();
					var timeText = records[recordIndex].Time.ToSpeechString(showSecondFractions(records[recordIndex].Time));
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].TextTitle}. {records[recordIndex].Wpm} words per minute. {records[recordIndex].Accuracy} percent accuracy. {timeText}.");
				}
				else if (col == MaxWpmCol)
				{
					toolTip.Content = records[recordIndex].MaxWpmText;
					cell.Content = records[recordIndex].MaxWpm < 0 ? "" : records[recordIndex].MaxWpm.ToString();
					cell.Tag = "1" + records[recordIndex].MaxWpmText;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].MaxWpm} max words per minute.");
				}
				else if (col == MinWpmCol)
				{
					toolTip.Content = records[recordIndex].MinWpmText;
					cell.Content = records[recordIndex].MinWpm.ToString();
					cell.Tag = "1" + records[recordIndex].MinWpmText;
					cell.SetValue(AutomationProperties.NameProperty, $"{records[recordIndex].MinWpm} min words per minute.");
				}
				ToolTipService.SetToolTip(cell, toolTip);
			}
		}

		Record.PrimarySortType columnToSortType(int col)
		{
			Record.PrimarySortType primarySortType;
			if (col == WpmCol)
				primarySortType = Record.PrimarySortType.Wpm;
			else if (col == MaxWpmCol)
				primarySortType = Record.PrimarySortType.MaxWpm;
			else if (col == MinWpmCol)
				primarySortType = Record.PrimarySortType.MinWpm;
			else
				throw new ArgumentException("Parameter col must mathc element of Record.PrimarySortType", "col");
			return primarySortType;
		}

		public static SolidColorBrush getAccuracyCol(float acc)
		{
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
