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
		new public double MinWidth
		{
			get => textBox.MinWidth;
			set
			{
				textBox.MinWidth = list.MinWidth = value;
			}
		}

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

		string submittedItem;
		string selectedItem;
		public string SelectedItem
		{
			get => selectedItem == null ? "" : selectedItem;
			set
			{
				selectedItem = value;
				list.SelectedItem = value;
				if (value != null)
					list.ScrollIntoView(selectedItem, ScrollIntoViewAlignment.Default);
			}
		}

		void setSelection(string value)
		{
			list.SelectionChanged -= List_SelectionChanged;
			SelectedItem = value;
			list.SelectionChanged += List_SelectionChanged;
		}

		public int SelectedIndex
		{
			get => list.SelectedIndex;
			set => SelectedItem = (string)list.Items[value];
		}

		public event EventHandler SelectionSubmitted;
		
		public ComboBoxEx()
		{
			this.InitializeComponent();
		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			listPopup.IsOpen = true;
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
			//if (matchingTexts.Count == 0)
			//	matchingTexts.AddFirst("No matching titles found.");
			list.ItemsSource = matchingTexts;
			//setSelection(selectedItem);
			list.SelectionChanged -= List_SelectionChanged;
			SelectedItem = selectedItem;
			list.SelectionChanged += List_SelectionChanged;

			list.UpdateLayout();
			if (list.ActualWidth > 0)
				textBox.Width = list.ActualWidth;
			listPopup.VerticalOffset = -list.ActualHeight;
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			buildFilteredList(textBox.Text);
		}

		private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Up)
			{
				if (SelectedIndex > 0)
					highlightItem(SelectedIndex-1);
				else
					highlightItem(list.Items.Count - 1);
			}
			else if (e.Key == VirtualKey.Down)
			{
				if (SelectedIndex < list.Items.Count - 1)
					highlightItem(SelectedIndex + 1);
				else
					highlightItem(0);
			}
			else if (e.Key == VirtualKey.Enter)
				onSelectionSubmitted();
			else if (e.Key == VirtualKey.Escape)
			{
				SelectedItem = submittedItem;
				//setText(SelectedItem);
				listPopup.IsOpen = false;
			}

		}

		private void highlightItem(int index)
		{
			var tempSubmittedItem = submittedItem;
			SelectedIndex = index;
			submittedItem = tempSubmittedItem;
		}

		void onSelectionSubmitted()
		{
			if (!listPopup.IsOpen)
				return;
			submittedItem = selectedItem;
			listPopup.IsOpen = false;
			setText(SelectedItem);
			SelectionSubmitted?.Invoke(this, new EventArgs());
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
			submittedItem = selectedItem;
			setText(selectedItem);
			textBox.SelectionStart = textBox.Text.Length;
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
