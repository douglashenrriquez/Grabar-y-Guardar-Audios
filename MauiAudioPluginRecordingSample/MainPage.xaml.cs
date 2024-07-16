using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Newtonsoft.Json;
using Plugin.Maui.Audio;

namespace MauiAudioPluginRecordingSample
{
    public partial class MainPage : ContentPage
    {
        readonly IAudioManager _audioManager;
        readonly IAudioRecorder _audioRecorder;
        string _fileName;

        public MainPage(IAudioManager audioManager)
        {
            InitializeComponent();

            _audioManager = audioManager;
            _audioRecorder = audioManager.CreateRecorder();
        }

        private async void OnStartRecordingClicked(object sender, EventArgs e)
        {
            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                
                await DisplayAlert("Permiso requerido", "Se necesita permiso para acceder al micrófono.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(FileNameEntry.Text) || string.IsNullOrWhiteSpace(DescriptionEntry.Text))
            {
                await DisplayAlert("Campos requeridos", "Por favor ingrese un nombre y una descripción para grabar el audio.", "OK");
                return;
            }
            _fileName = FileNameEntry.Text.Trim();

            if (!_audioRecorder.IsRecording)
            {
                await _audioRecorder.StartAsync();
                StartRecordingButton.IsVisible = false;
                StopRecordingButton.IsVisible = true;
            }
        }

        private async void OnStopRecordingClicked(object sender, EventArgs e)
        {
            if (_audioRecorder.IsRecording)
            {
                var recordedAudio = await _audioRecorder.StopAsync();
                var audioStream = recordedAudio.GetAudioStream();             
                string fileName = $"{_fileName}.wav";
                string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await audioStream.CopyToAsync(fileStream);
                }
                await DisplayAlert("Audio Guardado", $"El archivo de audio se ha guardado en {filePath}", "OK");
                await SendAudioInfoToApi(filePath);

                FileNameEntry.Text = string.Empty;
                DescriptionEntry.Text = string.Empty;
                StartRecordingButton.IsVisible = true;
                StopRecordingButton.IsVisible = false;
            }
        }

        private async void OnViewAudioListClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AudioListPage());
        }

        private async Task SendAudioInfoToApi(string filePath)
        {
            try
            {
                string description = DescriptionEntry.Text.Trim();

                using (HttpClient client = new HttpClient())
                {
                    var audioInfo = new
                    {
                        aduios = filePath,
                        fecha = DateTime.Now.ToString("yyyy-MM-dd"),
                        descripcion = description
                    };

                    var json = JsonConvert.SerializeObject(audioInfo);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.PostAsync(Config.Config.EndPointCreate, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Éxito", "Información del audio enviada a la API.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", $"Error al enviar información del audio a la API. StatusCode: {response.StatusCode}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Excepción: {ex.Message}", "OK");
            }
        }
    }
}
