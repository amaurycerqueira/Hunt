using System;
using System.IO;
using System.Threading.Tasks;
using Hunt.Common;
using Lottie.Forms;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Xamarin.Forms;

namespace Hunt.Mobile.Common
{
	public partial class TreasureDetailsPage : BaseContentPage<TreasureDetailsViewModel>
	{
		public TreasureDetailsPage()
		{
			if(IsDesignMode)
			{
				ViewModel.SetGame(App.Instance.CurrentGame);
				ViewModel.Treasure = App.Instance.CurrentGame.Treasures[2];
			}

			InitializeComponent();
		}

		public TreasureDetailsPage(Game game, Treasure treasure)
		{
			ViewModel.Treasure = treasure;
			ViewModel.SetGame(game);

			InitializeComponent();
			heroImage.GestureRecognizers.Add(new TapGestureRecognizer((obj) => HeroImageClicked()));
		}

		void TakePhotoClicked(object sender, EventArgs e)
		{
			DisplayCameraView();
		}

		async void DisplayCameraView()
		{
			ViewModel.Photo = await TakePhotoAsync();
		}

		async void SubmitClicked(object sender, EventArgs e)
		{
			if(ViewModel.Photo == null)
			{
				Hud.Instance.ShowToast("Please take a photo of an object");
				return;
			}

			try
			{
				var success = await ViewModel.AnalyzePhotoForAcquisition();
				if(success)
				{
					ViewModel.OnTreasureAcquired?.Invoke(ViewModel.Game);
				}
				else
				{
					ViewModel.Reset();
					Hud.Instance.ShowToast("Solid effort but incorrect. Please try again.");
					return;
				}
			}
			catch(Exception ex)
			{
				Hud.Instance.ShowToast(ex.Message, NoticationType.Error);
			}
		}

		async public Task<byte[]> TakePhotoAsync()
		{
			if(!CrossMedia.Current.IsCameraAvailable)
			{
				Hud.Instance.ShowToast("This device does not have a supported camera.");
				return null;
			}

			var options = new StoreCameraMediaOptions
			{
				CompressionQuality = 80,
				PhotoSize = PhotoSize.Medium,
			};

			var file = await CrossMedia.Current.TakePhotoAsync(options);

			if(file == null)
				return null;

			var input = file.GetStream();
			byte[] buffer = new byte[16 * 1024];
			using(var ms = new MemoryStream())
			{
				int read;
				while((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}

				return ms.ToArray();
			}
		}

		public async Task PlayAnimation()
		{
			var animation = new AnimationView
			{
				Animation = "checkmark_circle.json",
				WidthRequest = 160,
				HeightRequest = 160,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};

			Device.BeginInvokeOnMainThread(() =>
			{
				Hud.Instance.Show("Nice work!", animation);
				animation.Loop = false;
				animation.Play();
			});

			await Task.Delay(2500);
			await Hud.Instance.Dismiss(true);
		}

		async void HeroImageClicked()
		{
			if(heroImage.Source == null)
				return;

			await Navigation.PushModalAsync(new FullSizeImagePage(ViewModel.HeroImageSource));
		}
	}
}