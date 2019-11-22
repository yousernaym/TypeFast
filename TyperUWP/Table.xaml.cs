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

		public Border getCellBorder(int r, int c)
		{
			return rows[r][c];
		}

		public event EventHandler<SortEventArgs> Sort;

		List<List<Border>> rows = new List<List<Border>>();
		Brush rowBackground1 = new SolidColorBrush(Colors.Black);
		Brush rowBackground2 = new SolidColorBrush(Color.FromArgb(255, 20, 20, 20));
		Brush headerBackground = new SolidColorBrush((Color)Application.Current.Resources["PrimaryColor"]);
		Brush verticalLineBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
		Brush sortColVerticalLineBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));

		HeaderCell[] headerCells;
		public int NumCols => headerCells.Length;
		public int NumRows => rows.Count;

		int primarySortCol = 0;
		public int PrimarySortCol
		{
			get => primarySortCol;
			set
			{
				headerCells[primarySortCol].SortDirIcon.Visibility = Visibility.Collapsed;
				headerCells[value].SortDirIcon.Visibility = Visibility.Visible;
				((Button)rows[0][primarySortCol].Child).BorderThickness = new Thickness(2);
				((Button)rows[0][value].Child).BorderThickness = new Thickness(0);
				updateSortCol(primarySortCol, false);
				updateSortCol(value, true);
				primarySortCol = value;
				applyHeaderBackground();
			}
		}

		readonly Brush sortHeaderCellBorderBrush;

		public Table()
		{
			this.InitializeComponent();
			sortHeaderCellBorderBrush = (Brush)Application.Current.Resources["panelBorderGradientHighContrast"];
		}

		public void init(string[] headerStrings, uint sortFlags, int fontSize)
		{
			rows = new List<List<Border>>();
			headerCells = new HeaderCell[headerStrings.Length];
			addRow();
			int col = 0;
			foreach (var str in headerStrings)
			{
				headerCells[col] = new HeaderCell();
				bool sort = (sortFlags & 1) == 1;
				if (sort)
				{
					var btn = new Button();
					var icon = new FontIcon();
					icon.FontFamily = new FontFamily("Segoe MDL2 Assets");
					icon.FontSize = 10;
					icon.Visibility = Visibility.Collapsed;
					headerCells[col].SortDirIcon = icon;
					headerCells[col].Ascend = null;
					var content = new StackPanel();
					content.Children.Add(icon);
					var tb = new TextBlock();
					tb.Text = str;
					content.Children.Add(tb);
					btn.Content = content;

					btn.Padding = new Thickness(3, 0, 3, 2);
					btn.Background = new SolidColorBrush(Colors.Transparent);
					btn.BorderThickness = new Thickness(2);
					btn.BorderBrush = sortHeaderCellBorderBrush;
					btn.HorizontalAlignment = HorizontalAlignment.Stretch;
					btn.VerticalAlignment = VerticalAlignment.Stretch;
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
			PrimarySortCol = 0;
		}

		private void SortBtn_Click(object sender, RoutedEventArgs e)
		{
			int col = (int)((Button)sender).Tag;
			SortEventArgs args = new SortEventArgs();
			if (PrimarySortCol == col)
				headerCells[col].Ascend = !headerCells[col].Ascend;
			
			PrimarySortCol = col;
			args.Ascend = headerCells[col].Ascend;
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
			for (int i = 0; i < NumCols; i++)
			{
				var cell = rows[0][i];
				cell.Background = headerBackground;
				if (i == primarySortCol)
				{
					cell.Background = new SolidColorBrush(Color.FromArgb(255, 5, 10, 60));
				}
			}
		}

		void applyRowBackgrounds()
		{
			int mod = rowBackground2 == null ? 1 : 2;
			int startRow = headerBackground == null ? 0 : 1;
			for (int r = startRow; r < NumRows; r++)
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
			for (int r = 0; r < NumRows; r++)
			{
				for (int c = 0; c < rows[r].Count; c++)
				{
					var cell = rows[r][c];
					cell.BorderBrush = verticalLineBrush;
					cell.BorderThickness = new Thickness(0, 0, c == NumCols - 1 ? 0 : 1, 0);
				}
			}
		}

		void updateSortCol(int col, bool isSortCol)
		{
			foreach (var row in rows)
			{
				var cell = row[col];
				//const int normalThickness = 1, sortColThikness = 1;
				//int leftThickness = 0, rightThickness = normalThickness;
				//if (primarySortCol == col || primarySortCol == col + 1)
				//{
				//	rightThickness = sortColThikness;
				//	cell.BorderBrush = sortColVerticalLineBrush;
				//}

				//if (primarySortCol == col && col == 0)
				//	leftThickness = sortColThikness;
				//if (col == NumCols - 1)
				//	rightThickness = primarySortCol == col ? sortColThikness : normalThickness;

				if (cell.Child is TextBlock)
				{
					var child = (TextBlock)cell.Child;
					child.FontWeight = isSortCol ? FontWeights.SemiBold : FontWeights.Normal;
				}
				else if (cell.Child is HyperlinkButton)
				{
					var child = (HyperlinkButton)cell.Child;
					child.FontWeight = isSortCol ? FontWeights.SemiBold : FontWeights.Normal;
				}
			}
		}

		public void applyStyle()
		{
			applyHeaderBackground();
			applyRowBackgrounds();
			applyVerticalLineBrush();
			updateSortCol(primarySortCol, true);
		}

		bool compareTablePartFlags(TablePart value1, TablePart value2)
		{
			return ((int)value1 & (int)value2) > 0;
		}
	}

	public class SortEventArgs
	{
		public bool? Ascend;
		public int Column;
	}

	class HeaderCell
	{
		bool ?ascend = null;
		public bool? Ascend
		{
			get => ascend;
			set
			{
				ascend = value;
				if (ascend == null)
					SortDirIcon.Glyph = "";
				else
					SortDirIcon.Glyph = (bool)ascend ? "\uE70E" : "\uE70D";
				//SortDirIcon.Glyph = ascend ? "\uE96D" : "\uE96E"; 
			}
		}
		public FontIcon SortDirIcon;
	}
}
