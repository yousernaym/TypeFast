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
		public int RowCount => rows.Count;

		new public Brush BorderBrush
		{
			get => grid.BorderBrush;
			set => grid.BorderBrush = value;
		}

		new public Thickness BorderThickness
		{
			get => grid.BorderThickness;
			set => grid.BorderThickness = value;
		}

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

		public event EventHandler<SortEventArgs> Sort;

		List<List<Border>> rows = new List<List<Border>>();
		Brush rowBackground1 = new SolidColorBrush(Colors.Black);
		Brush rowBackground2 = new SolidColorBrush(Color.FromArgb(255, 25, 25, 25));
		Brush headerBackground = new SolidColorBrush((Color)Application.Current.Resources["PrimaryColor"]);
		Brush verticalLineBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

		Column[] columns;
		public int PrimarySortCol = 0;

		public Table()
		{
			this.InitializeComponent();
		}

		public void init(string[] headerStrings, uint sortFlags, int fontSize)
		{
			columns = new Column[headerStrings.Length];
			addRow();
			int col = 0;
			foreach (var str in headerStrings)
			{
				columns[col] = new Column();
				bool sort = (sortFlags & 1) == 1;
				if (sort)
				{
					var btn = new Button();
					btn.Content = str;
					btn.HorizontalAlignment = HorizontalAlignment.Stretch;
					btn.VerticalAlignment = VerticalAlignment.Center;
					btn.FontStyle = FontStyle.Italic;
					btn.FontSize = fontSize;
					btn.Tag = col;
					btn.Click += SortBtn_Click;
					addCell(btn);
				}
				else
				{
					TextBlock tBlock = new TextBlock();
					tBlock.Text = str;
					tBlock.HorizontalAlignment = HorizontalAlignment.Center;
					tBlock.VerticalAlignment = VerticalAlignment.Center;
					tBlock.FontStyle = FontStyle.Italic;
					tBlock.FontSize = fontSize;
					addCell(tBlock);
				}
				sortFlags >>= 1;
				col++;
			}
		}

		private void SortBtn_Click(object sender, RoutedEventArgs e)
		{
			int col = (int)((Button)sender).Tag;
			SortEventArgs args = new SortEventArgs();
			if (PrimarySortCol == col)
				columns[col].Ascend = !columns[col].Ascend;
			
			PrimarySortCol = col;
			args.Ascend = columns[col].Ascend;
			args.Column = col;
			Sort?.Invoke(sender, args);
		}

		public void addRow(List<UIElement> row = null)
		{
			rows.Add(new List<Border>());
			grid.RowDefinitions.Add(new RowDefinition());
			if (row == null)
				return;
			foreach (var cell in row)
				addCell(cell);
		}

		public void addCell(UIElement cell, int row = -1)
		{
			if (row < 0)
				row = rows.Count - 1;
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
			int startRow = headerBackground == null ? 0 : 1;
			for (int r = startRow; r < RowCount; r++)
			{
				var color = (r - startRow) % mod == 0 ? rowBackground1 : rowBackground2;
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

	public class SortEventArgs
	{
		public bool Ascend;
		public int Column;
	}

	class Column
	{
		public bool Ascend = false;
	}
}
