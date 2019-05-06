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
		//public int ColumnCount => cells.Count;
		public int RowCount => rows.Count;

		public Brush RowBackground1
		{
			get => rowBackground1;
			set
			{
				rowBackground1 = value;
				applyRowBackgrounds();
			}
		}

		public Brush RowBackground2
		{
			get => rowBackground2;
			set
			{
				rowBackground2 = value;
				applyRowBackgrounds();
			}
		}

		public Brush HeaderBackground
		{
			get => headerBackground;
			set
			{
				headerBackground = value;
				applyHeaderBackground();
			}
		}

		public Brush VerticalLineBrush
		{
			get => verticalLineBrush;
			set
			{
				verticalLineBrush = value;
				applyVerticalLineBrush();
			}
		}

		List<List<Border>> rows = new List<List<Border>>();
		Brush rowBackground1 = new SolidColorBrush(Colors.Black);
		Brush rowBackground2 = new SolidColorBrush(Color.FromArgb(255, 25, 25, 25));
		Brush headerBackground = new SolidColorBrush((Color)Application.Current.Resources["PrimaryColor"]);
		Brush verticalLineBrush = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));
		
		public Table()
		{
			this.InitializeComponent();
		}

		public void addRow(List<UIElement> row = null)
		{
			rows.Add(new List<Border>());
			grid.RowDefinitions.Add(new RowDefinition());
			if (row == null)
				return;
			foreach (var cell in row)
				addCell(rows.Count - 1, cell);
		}

		public void addCell(int row, UIElement cell)
		{
			var border = new Border();
			border.Child = cell;
			rows[row].Add(border);
			if (rows[row].Count > grid.ColumnDefinitions.Count)
				grid.ColumnDefinitions.Add(new ColumnDefinition());
			grid.Children.Add(border);
			Grid.SetRow(border, row);
			Grid.SetColumn(border, rows[row].Count - 1);

			applyStyle();
		}

		public void setCell(int row, int col, UIElement element)
		{
			rows[row][col].Child = element;
		}

		public T getCell<T>(int row, int col) where T : UIElement
		{
			return (T)rows[row][col].Child;
		}

		void applyHeaderBackground()
		{
			foreach (var cell in rows[0])
				cell.Background = headerBackground;
		}

		void applyRowBackgrounds()
		{
			int mod = rowBackground2 == null ? 1 : 2;
			for (int r = 1; r < RowCount; r++)
			{
				var color = (r - 1) % mod == 0 ? rowBackground1 : rowBackground2;
				for (int c = 0; c < rows[r].Count; c++)
				{
					rows[r][c].Background = color;
				}
			}
		}


		void applyVerticalLineBrush()
		{
			for (int r = 0; r < RowCount; r++)
			{
				for (int c = 0; c < rows[r].Count; c++)
				{
					var cell = rows[r][c];
					cell.BorderBrush = verticalLineBrush;
					cell.BorderThickness = new Thickness(0, 0, c == rows[r].Count - 1 ? 0 : 1, 0);
				}
			}
		}

		void applyStyle()
		{
			applyHeaderBackground();
			applyRowBackgrounds();
			applyVerticalLineBrush();
		}

		bool compareTablePartFlags(TablePart value1, TablePart value2)
		{
			return ((int)value1 & (int)value2) > 0;
		}
	}
}
