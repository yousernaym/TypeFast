using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
	public sealed partial class Help : UserControl
	{
		public Help()
		{
			this.InitializeComponent();

			addRow("Shortcut", "Description");
			addRow("Alt + H", "Show this list.");
			addRow("Alt + T", "Change time limit.");
			addRow("Alt + E", "Show records.");
			addRow("Ctrl + F", "Change font settings.");
			addRow("Ctrl + R", "Restart the typing session.");
			addRow("Alt + R", "Toggle whether to select a random text when restarting the typing session.");
			addRow("Ctrl + T", "Open text list.");
			addRow("Ctrl + N", "Add new text.");
			addRow("Ctrl + E", "Edit the currently selected text.");
			addRow("Ctrl + D", "Delete the currently selected text.");
		}

		public void addRow(params string[] cellTexts)
		{
			int rowNumber = shortcutsTable.RowCount;
			shortcutsTable.addRow();
			for (int colNumber = 0; colNumber < cellTexts.Length; colNumber++)
			{
				var cell = new TextBlock();
				cell.Text = cellTexts[colNumber];
				cell.VerticalAlignment = VerticalAlignment.Center;
				cell.Foreground = new SolidColorBrush(Colors.White);
				cell.FontSize = 20;
				cell.Padding = new Thickness(7, 7, 20, 7);
				cell.TextWrapping = TextWrapping.Wrap;

				if (rowNumber == 0)
				{
					cell.FontStyle = Windows.UI.Text.FontStyle.Italic;
					cell.HorizontalAlignment = HorizontalAlignment.Center;
				}
				if (colNumber == 1)
					cell.MaxWidth = 600;
				shortcutsTable.addCell(cell);
			}
		}
	}
}
