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
			NumRecords = 6,
			Rows = NumRecords + 1,
			Columns = 3,
			WpmCol = 0,
			AccCol = 1,
			TextCol = 2;
		
		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		RecordType currentRecordType = RecordType.RT_ALL;
		Texts texts;

		public delegate void TextTitleClickEH(RecordsView recordsView, TextTitleClickEventArgs e);
		public event TextTitleClickEH TextTitleClick;
		
		public RecordsView()
		{
			this.InitializeComponent();
			
			for (int r = 0; r < Rows; r++)
			{
				table.addRow();
				for (int c = 0; c < Columns; c++)
				{
					var cell = new TextBlock();
					cell.HorizontalAlignment = c == WpmCol || c == AccCol ? HorizontalAlignment.Right : HorizontalAlignment.Left;
					cell.VerticalAlignment = VerticalAlignment.Center;
					cell.Foreground = new SolidColorBrush(Colors.White);
					cell.FontSize = 20;
					cell.Padding = new Thickness(10, 5, 10, 5);

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

					if (r == 0)
					{
						cell.FontStyle = Windows.UI.Text.FontStyle.Italic;
						cell.HorizontalAlignment = HorizontalAlignment.Center;
					}
					table.addCell(r, cell);
				}
			}
			table.getCell<TextBlock>(0, WpmCol).Text = "WPM";
			table.getCell<TextBlock>(0, AccCol).Text = "Acc %";
			table.getCell<TextBlock>(0, TextCol).Text = "Text";
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

		void syncGrid()
		{ 
			var records = texts.getRecords(currentRecordType, NumRecords);

			for (int i = 0; i < NumRecords; i++)
			{
				if (i < records.Length)
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = records[i].WPM.ToString();
					table.getCell<TextBlock>(i + 1, AccCol).Text = records[i].Accuracy.ToString("0.0");
					setLinkText(table.getCell<TextBlock>(i + 1, TextCol), records[i].TextTitle);
				}
				else
				{
					table.getCell<TextBlock>(i + 1, WpmCol).Text = "";
					table.getCell<TextBlock>(i + 1, AccCol).Text = "";
					setLinkText(table.getCell<TextBlock>(i + 1, TextCol), "");
				}
			}
		}

		private void setLinkText(TextBlock textBlock, string text)
		{
			var titleLink = (Hyperlink)textBlock.Inlines[0];
			var titleRun = (Run)titleLink.Inlines[0];
			titleRun.Text = text;
		}

		private void AllRBtn_Click(object sender, RoutedEventArgs e)
		{
			currentRecordType = RecordType.RT_ALL;
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
