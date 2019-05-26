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
		bool updatingText;
		bool programmaticItemSelection;
		IEnumerable<string> itemSource;
		public IEnumerable<string> ItemSource
		{
			get => itemSource;
			set
			{
				itemSource = value;
				buildFilteredList("");
			}
		}

		string selectedItem;
		public string SelectedItem
		{
			get => selectedItem == null ? "" : selectedItem;
			set
			{
				selectedItem = value;
				list.SelectionChanged -= List_SelectionChanged;
				list.SelectedItem = value;
				list.SelectionChanged += List_SelectionChanged;
				if (value != null)
					list.ScrollIntoView(selectedItem, ScrollIntoViewAlignment.Default);
			}
		}

		public int SelectedIndex
		{
			get => list.SelectedIndex;
			set
			{
				SelectedItem = (string)list.Items[value];
				//list.SelectedIndex = value;
				//list.SelectedItem = list.SelectedItem;
			}
		}

		public event EventHandler SelectionSubmitted;
		
		public ComboBoxEx()
		{
			this.InitializeComponent();
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			//buildFilteredList();
			//list.Visibility = Visibility.Visible;
			listPopup.IsOpen = true;
			//textBox.SelectionStart = 0;
			//textBox.SelectionLength = textBox.Text.Length;
			buildFilteredList("");
			textBox.PlaceholderText = textBox.Text;
			setText("");
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			onSelectionSubmitted();
		}

		private void buildFilteredList(string query)
		{
			query = query.ToLower();
			var matchingTexts = new LinkedList<string>();
			foreach (var item in ItemSource)
			{
				if (item.ToLower().Contains(query))
					matchingTexts.AddFirst(item);
			}
			if (matchingTexts.Count == 0)
				matchingTexts.AddFirst("No matching titles found.");
			list.ItemsSource = matchingTexts;
			programmaticItemSelection = true;
			//list.SelectedItem = selectedItem;
			SelectedItem = selectedItem;
			
			list.UpdateLayout();
			if (list.ActualWidth > 0)
				textBox.Width = list.ActualWidth;
			listPopup.VerticalOffset = -list.ActualHeight;
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			//if (!updatingText)
				buildFilteredList(textBox.Text);
			//updatingText = false;
		}

		private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Up)
			{
				if (SelectedIndex > 0)
					SelectedIndex--;
				else
					SelectedIndex = list.Items.Count - 1;
			}
			else if (e.Key == VirtualKey.Down)
			{
				if (SelectedIndex < list.Items.Count - 1)
					SelectedIndex++;
				else
					SelectedIndex = 0;
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
			//list.Visibility = Visibility.Collapsed;
			if (!listPopup.IsOpen)
				return;
			listPopup.IsOpen = false;
			setText(SelectedItem);
			SelectionSubmitted?.Invoke(this, new EventArgs());
			//updatingText = true;
		}

		private void setText(string text)
		{
			textBox.TextChanged -= TextBox_TextChanged;
			textBox.Text = text;
			textBox.TextChanged += TextBox_TextChanged;
		}

		private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (list.SelectedIndex == -1)
				return;
			selectedItem = (string)list.SelectedItem;
			//list.ScrollIntoView(list.SelectedItem, ScrollIntoViewAlignment.Default);
			//updatingText = true;
			//if (!programmaticItemSelection)
			//{
				setText(selectedItem);
				textBox.SelectionStart = textBox.Text.Length;
			//}
			programmaticItemSelection = false;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var ttv = listPopup.TransformToVisual(Window.Current.Content);
			Point popupPos = ttv.TransformPoint(new Point(0, 0));
			list.MaxHeight = popupPos.Y;
			list.UpdateLayout();
			if (list.ActualWidth > 0)
				textBox.Width = list.ActualWidth;
			listPopup.VerticalOffset = -list.ActualHeight;

		}

		private void ListPopup_Opened(object sender, object e)
		{
			list.UpdateLayout();
			if (list.ActualWidth > 0)
				textBox.Width = list.ActualWidth;
			listPopup.VerticalOffset = -list.ActualHeight;
		}
	}
}
