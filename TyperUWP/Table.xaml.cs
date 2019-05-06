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
	enum TablePart { TP_Header = 1, TP_Body = 2, TP_All = 3}
	public sealed partial class Table : UserControl
	{
		public int ColumnCount { get; private set; }
		public int RowCount { get; private set; } 
		
		Border[] headerCells;
		Border[,] cells;

		public Table(int cols, int rows)
		{
			this.InitializeComponent();
			ColumnCount = cols;
			RowCount = rows;
			headerCells = new Border[cols];
			cells = new Border[cols, rows];
			for (int r = 0; r <= rows; r++) //Add an extra row for the header
				grid.RowDefinitions.Add(new RowDefinition());
			for (int c = 0; c < cols; c++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				headerCells[c] = new Border();
				Grid.SetColumn(headerCells[c], c);
			}
			setHeaderBackground(new SolidColorBrush((Color)Application.Current.Resources["PrimaryColor"]));

			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < cols; c++)
				{
					var cell = new Border();
					//cellBorder.Padding = new Thickness(10);
					if (c != cols - 1)
					{
						cell.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
						cell.BorderThickness = new Thickness(0, 0, 1, 0);
					}
					
					grid.Children.Add(cell);
					Grid.SetRow(cell, r + 1);
					Grid.SetColumn(cell, c + 1);
					cells[c, r] = cell;
				}
			}
			byte rowBrightness = (byte)25;
			setBackground(new SolidColorBrush(Color.FromArgb(255, rowBrightness, rowBrightness, rowBrightness)), new SolidColorBrush(Colors.Black));
		}

		void setCell(int col, int row, UIElement element)
		{
			cells[col, row].Child = element;
		}

		UIElement getCell(int col, int row)
		{
			return cells[col, row].Child;
		}

		void setHeaderCell(int col, UIElement element)
		{
			cells[col, 0].Child = element;
		}

		UIElement getCell(int col)
		{
			return cells[col, 0].Child;
		}

		//void setHeaderLabels(params string[] labels)
		//{
		//	for (int i = 0; i < labels.Length; i++)
		//		headerCells[i].Text = labels[i];
		//}

		void setHeaderBackground(Brush value)
		{
			foreach (var cell in headerCells)
				cell.Background = value;
		}

		void setBackground(Brush value1, Brush value2 = null)
		{
			int mod = value2 == null ? 1 : 2;
			for (int r = 0; r < RowCount; r++)
			{
				var color = r % mod == 0 ? value1 : value2;
				for (int c = 0; c < RowCount; c++)
				{
					cells[r, c].Background = color;
				}
			}

		}

		bool compareTablePartFlags(TablePart value1, TablePart value2)
		{
			return ((int)value1 & (int)value2) > 0;
			
		}

		
	}

}
