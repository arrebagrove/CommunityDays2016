using Xamarin.Forms;

namespace AdvancedPCLSample
{
	public partial class AdvancedPCLSamplePage : ContentPage
	{
		public AdvancedPCLSamplePage()
		{
			InitializeComponent();
			txtTest.Text = BaitAndSwitch.SampleClass.GetLibraryVersion();
		}
	}
}
