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
			addRow("Alt + H", "Show this shortcut list.");
			addRow("Ctrl + P", "Create a new typing session with the contents of the clipBoard.");
			addRow("Ctrl + P", "Create a new typing session with the contents of the clipBoard.");
			addRow("Ctrl + N", "Add a new text.");
			addRow("Ctrl + E", "Edit the currently selected text.");
			addRow("Ctrl + D", "Delete the currently selected text.");
			addRow("Ctrl + R", "Restart the typing session.");
			addRow("Alt + S", "Toggle on/off: Select a random text every time a typing session is restarted.");
			addRow("Alt + S", "Toggle on/off: Select a random text every time a typing session is restarted.");
			addRow("Alt + T", "Change time limit.");
		}

		public void addRow(params string[] cellTexts)
		{
			int rowNumber = shortcutsTable.RowCount;
			shortcutsTable.addRow();
			for (int i = 0; i < cellTexts.Length; i++)
			{
				var cell = new TextBlock();
				cell.Text = cellTexts[i];
				cell.VerticalAlignment = VerticalAlignment.Center;
				cell.Foreground = new SolidColorBrush(Colors.White);
				cell.FontSize = 20;
				cell.Padding = new Thickness(7, 5, 20, 5);
				
				if (rowNumber == 0)
				{
					cell.FontStyle = Windows.UI.Text.FontStyle.Italic;
					cell.HorizontalAlignment = HorizontalAlignment.Center;
				}
				shortcutsTable.addCell(cell);
			}
		}
	}
}
