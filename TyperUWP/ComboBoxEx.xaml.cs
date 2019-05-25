using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
	public sealed partial class ComboBoxEx : UserControl
	{
		bool updatingTextFromSelection;
		IEnumerable<string> itemSource;
		public IEnumerable<string> ItemSource
		{
			get => itemSource;
			set
			{
				itemSource = value;
				buildFilteredList();
			}
		}

		public string SelectedItem
		{
			get => (string)list.SelectedItem;
			set => list.SelectedItem = value;
		}

		public event EventHandler SelectionSubmitted;
		
		public ComboBoxEx()
		{
			this.InitializeComponent();
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			//buildFilteredList();
			list.Visibility = Visibility.Visible;
			textBox.SelectionStart = 0;
			textBox.SelectionLength = textBox.Text.Length;
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			onSelectionSubmitted();
		}

		private void buildFilteredList()
		{
				var matchingTexts = new LinkedList<string>();
				foreach (var item in ItemSource)
				{
					if (item.ToLower().Contains(textBox.Text.ToLower()))
						matchingTexts.AddFirst(item);
				}
				if (matchingTexts.Count == 0)
					matchingTexts.AddFirst("No matching titles found.");
				list.ItemsSource = matchingTexts;
			
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!updatingTextFromSelection)
				buildFilteredList();
			updatingTextFromSelection = false;
		}

		private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Up)
			{
				if (list.SelectedIndex > 0)
					list.SelectedIndex--;
			}
			else if (e.Key == VirtualKey.Down)
			{
				if (list.SelectedIndex < list.Items.Count - 1)
					list.SelectedIndex++;
			}
			else if (e.Key == VirtualKey.Enter)
				onSelectionSubmitted();

		}

		private void List_ItemClick(object sender, ItemClickEventArgs e)
		{
			onSelectionSubmitted();
		}

		void onSelectionSubmitted()
		{
			list.Visibility = Visibility.Collapsed;
			SelectionSubmitted?.Invoke(this, new EventArgs());
			textBox.PlaceholderText = textBox.Text;
			textBox.Text = "";
		}

		private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (list.SelectedIndex == -1)
				return;
			updatingTextFromSelection = true;
			textBox.Text = SelectedItem;
			textBox.SelectionStart = textBox.Text.Length;
		}
	}
}
