using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TyperLib;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
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
			Columns = 2,
			WpmCol = 0,
			TextCol = 1;

		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		RecordType currentRecordType = RecordType.RT_ALL;
		Texts texts;
		
		public RecordsView()
		{
			this.InitializeComponent();
			for (int r = 0; r < Rows; r++)
			{
				var row = new RowDefinition();
				//row.Height = new GridLength(0, GridUnitType.Auto);
				grid.RowDefinitions.Add(row);
				for (int c = 0; c < Columns; c++)
				{
					var column = new ColumnDefinition();
					//column.MinWidth = c == WpmCol ? 50 : 100;
					grid.ColumnDefinitions.Add(column);
					var cellBorder = new Border();
					byte rowBrightness = (byte)(25 * ((r + 1) % 2));
					cellBorder.Background = new SolidColorBrush(Color.FromArgb(255, rowBrightness, rowBrightness, rowBrightness));
					if (r == 0)
						cellBorder.Background = new SolidColorBrush(Color.FromArgb(255, 0, 20, 80));
					cellBorder.Padding = new Thickness(10);

					var cell = new TextBlock();
					cell.HorizontalAlignment = c == WpmCol ? HorizontalAlignment.Right : HorizontalAlignment.Left;
					cell.VerticalAlignment = VerticalAlignment.Center;
					if (c == WpmCol)
					{
						cellBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
						cellBorder.BorderThickness = new Thickness(0, 0, 1, 0);
					}

					cell.Foreground = new SolidColorBrush(Colors.White);
					cell.FontSize = 20;
					if (r == 0)
					{
						cell.FontStyle = Windows.UI.Text.FontStyle.Italic;
						cell.HorizontalAlignment = HorizontalAlignment.Center;
					}
					cellBorder.Child = gridCells[c, r] = cell;

					grid.Children.Add(cellBorder);
					Grid.SetRow(cellBorder, r);
					Grid.SetColumn(cellBorder, c);
				}
			}
			gridCells[WpmCol, 0].Text = "WPM";
			gridCells[TextCol, 0].Text = "Text";
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
					gridCells[WpmCol, i + 1].Text = records[i].WPM.ToString();
					gridCells[TextCol, i + 1].Text = records[i].TextTitle;
				}
				else
				{
					gridCells[WpmCol, i + 1].Text = "";
					gridCells[TextCol, i + 1].Text = "";
				}
			}
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
}
