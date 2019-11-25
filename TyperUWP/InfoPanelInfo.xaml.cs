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
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TyperUWP
{
	public sealed partial class InfoPanelInfo : UserControl
	{
		Run linkRun = new Run();
		Run textRun = new Run();
		Hyperlink link = new Hyperlink();

		public Brush ForeGround
		{
			get => labelTbl.Foreground;
			set => labelTbl.Foreground = valueTbl.Foreground = value;
		}
		public string Label
		{
			get => labelTbl.Text;
			set => labelTbl.Text = value;
		}
		
		string value = "";
		public string Value
		{
			get => value;
			set
			{
				this.value = value;
				setText();
			}
		}
		public event EventHandler<HyperlinkClickEventArgs> LinkClick;

		bool isHyper = false;
		public bool IsHyper
		{
			get => isHyper;
			set
			{
				isHyper = value;
				setText();
			}
		}

		new public Thickness Margin
		{
			get => panel.Margin;
			set => panel.Margin = value;
		}

		string valueToolTip;
		public string ValueToolTip
		{
			get => valueToolTip;
			set
			{
				valueToolTip = value;
				var newToolTip = new ToolTip();
				newToolTip.Content = valueToolTip;
				ToolTipService.SetToolTip(valueTbl, newToolTip);
			}
		}

		public InfoPanelInfo()
		{
			this.InitializeComponent();
			link.Inlines.Add(linkRun);
			valueTbl.Inlines.Add(link);
			valueTbl.Inlines.Add(textRun);
			link.Click += link_Clkck;
		}

		private void link_Clkck(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			LinkClick?.Invoke(sender, args);
		}

		void setText()
		{
			if (isHyper)
			{
				textRun.Text = "";
				linkRun.Text = value;
				link.IsTabStop = !string.IsNullOrEmpty(value);
			}
			else
			{
				textRun.Text = value;
				linkRun.Text = "";
				link.IsTabStop = false;
			}
		}
	}
}
