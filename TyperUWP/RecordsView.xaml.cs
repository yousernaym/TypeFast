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
		const int NumRecords = 6, Rows = NumRecords + 1, Columns = 3;
		TextBlock[,] gridCells = new TextBlock[Columns, Rows];
		RecordType CurrentRecordType = RecordType.RT_ALL;
		public RecordsView()
		{
			this.InitializeComponent();
			for (int j = 0; j < Rows; j++)
			{
				var row = new RowDefinition();
				row.Height = new GridLength(0, GridUnitType.Auto);
				grid.RowDefinitions.Add(row);
				for (int i = 0; i < Columns; i++)
				{
					var column = new ColumnDefinition();
					column.Width = new GridLength(0, GridUnitType.Auto);
					grid.ColumnDefinitions.Add(column);
					var cellBorder = new Border();
					cellBorder.BorderBrush = new SolidColorBrush(Colors.Gray);
					cellBorder.BorderThickness = new Thickness(1);

					var cell = new TextBlock();
					cell.Foreground = new SolidColorBrush(Colors.White);
					cellBorder.Child = gridCells[i, j] = cell;

					grid.Children.Add(cellBorder);
					Grid.SetRow(cellBorder, j);
					Grid.SetColumn(cellBorder, i);
				}
			}
			gridCells[1, 0].Text = "WPM";
			gridCells[2, 0].Text = "Text";
		}

		public void syncGrid(Texts texts)
		{
			var records = texts.getRecords(CurrentRecordType, NumRecords);

			for (int i = 0; i < Rows - 1; i++)
			{
				if (i < records.Length)
				{
					gridCells[1, i + 1].Text = records[i].WPM.ToString();
					gridCells[2, i + 1].Text = records[i].TextTitle;
				}
				else
				{
					gridCells[1, i + 1].Text = "";
					gridCells[2, i + 1].Text = "";
				}
			}
		}
	}
}
