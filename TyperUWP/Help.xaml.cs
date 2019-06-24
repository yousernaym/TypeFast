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
			addRow(statsTable, "Name", "Description");
			addRow(statsTable, "Correct", "Number of correctly typed characters.");
			addRow(statsTable, "Incorrect", "Number of incorrectly typed characters left uncorrected.");
			addRow(statsTable, "Fixed", "Number of corrected characters.");
			addRow(statsTable, "WPM", "Words Per Minute. Assumes 5 characters per word. Incorrect charaacters left uncorrected incur a penalty to encourage correcting mistakes.");
			addRow(statsTable, "Accuracy", "Percentage of correct characters relative to the total number of characters. Total number of characters = Correct + Incorrect + Fixed.");

			addRow(shortcutsTable, "Shortcut", "Description");
			addRow(shortcutsTable, "Alt + H", "Show this page.");
			addRow(shortcutsTable, "Alt + T", "Change time limit.");
			addRow(shortcutsTable, "Alt + R", "Show records.");
			addRow(shortcutsTable, "Ctrl + F", "Change font settings.");
			addRow(shortcutsTable, "Ctrl + V", "Start new typing session with clipbbard contents.");
			addRow(shortcutsTable, "Ctrl + P", "Practice specific characters.");
			addRow(shortcutsTable, "Alt + E", "Restart typing session.");
			addRow(shortcutsTable, "Alt + A", "Restart typing session with a random text.");
			addRow(shortcutsTable, "Ctrl + T", "Open text list.");
			addRow(shortcutsTable, "Ctrl + N", "Add new text.");
			addRow(shortcutsTable, "Ctrl + E", "Edit the currently selected text.");
			addRow(shortcutsTable, "Ctrl + D", "Delete the currently selected text.");
		}

		public void addRow(Table table, params string[] cellTexts)
		{
			int rowNumber = table.RowCount;
			table.addRow();
			for (int colNumber = 0; colNumber < cellTexts.Length; colNumber++)
			{
				var cell = new TextBlock();
				cell.Text = cellTexts[colNumber];
				cell.VerticalAlignment = VerticalAlignment.Center;
				cell.Foreground = new SolidColorBrush(Colors.White);
				cell.FontSize = 18;
				cell.Padding = new Thickness(7, 7, 20, 7);
				cell.TextWrapping = TextWrapping.Wrap;

				if (rowNumber == 0)
				{
					cell.FontStyle = Windows.UI.Text.FontStyle.Italic;
					cell.HorizontalAlignment = HorizontalAlignment.Center;
				}
				if (colNumber == 1)
					cell.MaxWidth = 600;
				table.addCell(cell);
			}
		}
	}
}
