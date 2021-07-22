using System;
using System.Collections.Generic;
using System.Text;
using MvvmHelpers;
using System.Windows.Input;
using MvvmHelpers.Commands;
using MediaManager;
using System.Threading.Tasks;
using MonkeyCache.FileStore;
using System.IO;
using System.Net;
using Xamarin.Essentials;
using DotNetRocks.Models;

namespace DotNetRocks.ViewModels
{
    public class DetailPageViewModel : BaseViewModel
    {
        string CacheDir = "";
        string CachedFileName = "";
        string Mp3FileName = "";
        bool IsPaused = false;
        FileStream LocalFileStream = null;

        public DetailPageViewModel()
        {
            Barrel.ApplicationId = "mobile_dnr";
            CacheDir = FileSystem.CacheDirectory;
            CrossMediaManager.Current.PositionChanged += Current_PositionChanged;
            CrossMediaManager.Current.MediaItemFinished += Current_MediaItemFinished;
        }

        bool isReady = false;
        public bool IsReady { 
            get
            {
                return isReady;
            }
            set
            {
                SetProperty(ref isReady, value);
            }
        }

        private Show currentShow;
        public Show CurrentShow
        {
            get
            {
                return currentShow;
            }
            set
            {
                IsReady = false;
                SetProperty(ref currentShow, value);
                var uri = new Uri(CurrentShow.ShowDetails.File.Url);
                string DirectoryName = uri.Segments[uri.Segments.Length - 3];
                string FileNameOnly = Path.GetFileName(CurrentShow.ShowDetails.File.Url);
                Mp3FileName = DirectoryName.Substring(0, DirectoryName.Length - 1)
                        + FileNameOnly;
                CachedFileName = Path.Combine(CacheDir, Mp3FileName);
                // Does the file exist?
                if (System.IO.File.Exists(CachedFileName))
                {
                    // Yes! We are cached
                    IsCached = true;
                }
                IsReady = true;
            }
        }

        private void Current_PositionChanged(object sender, MediaManager.Playback.PositionChangedEventArgs e)
        {
            TimeSpan currentMediaPosition = CrossMediaManager.Current.Position;
            TimeSpan currentMediaDuration = CrossMediaManager.Current.Duration;
            if (IsPlaying)
            {
                if (currentMediaDuration.Hours == 0)
                {
                    CurrentStatus = $"{currentMediaPosition.Minutes:D1}:{currentMediaPosition.Seconds:D2}/{currentMediaDuration.Minutes:D1}:{currentMediaDuration.Seconds:D2}";
                }
                else
                {
                    if (currentMediaPosition.Hours == 0)
                    {
                        CurrentStatus = $"{currentMediaPosition.Minutes:D1}:{currentMediaPosition.Seconds:D2}/{currentMediaDuration.Hours:D1}:{currentMediaDuration.Minutes:D1}:{currentMediaDuration.Seconds:D2}";
                    }
                    else
                    {
                        CurrentStatus = $"{currentMediaPosition.Hours:D1}:{currentMediaPosition.Minutes:D2}:{currentMediaPosition.Seconds:D2}/{currentMediaDuration.Hours:D1}:{currentMediaDuration.Minutes:D1}:{currentMediaDuration.Seconds:D2}";
                    }
                }
                base.OnPropertyChanged("CurrentPosition");
            }
        }

        private void Current_MediaItemFinished(object sender, MediaManager.Media.MediaItemEventArgs e)
        {
            CurrentStatus = "";
            IsPlaying = false;
            if (LocalFileStream != null)
            {
                LocalFileStream.Dispose();
            }
        }

        private bool isPlaying;
        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            set
            {
                SetProperty(ref isPlaying, value);
            }
        }

        private ICommand play;
        public ICommand Play
        {
            get
            {
                if (play == null)
                {
                    play = new AsyncCommand(PerformPlay);
                }

                return play;
            }
        }

        private async Task PerformPlay()
        {
            IsPlaying = true;

            if (IsPaused)
            {
                await CrossMediaManager.Current.PlayPause();
            }
            else
            {
                if (!IsCached)
                {
                    // Not in cache. Play from URL
                    CurrentStatus = "Downloading...";
                    await CrossMediaManager.Current.Play(CurrentShow.ShowDetails.File.Url);
                    // Download the file to the cache
                    DownloadFile();
                }
                else
                {
                    // In the cache. Play local file
                    CurrentStatus = "Playing from Cache...";
                    LocalFileStream = System.IO.File.OpenRead(CachedFileName);
                    await CrossMediaManager.Current.Play(LocalFileStream, Mp3FileName);
                }
            }

            IsPaused = false;
        }

        private ICommand pause;
        public ICommand Pause
        {
            get
            {
                if (pause == null)
                {
                    pause = new AsyncCommand(PerformPause);
                }

                return pause;
            }
        }

        public async Task PerformPause()
        {
            IsPlaying = false;
            IsPaused = true;
            CurrentStatus = "";
            await CrossMediaManager.Current.Pause();
        }

        private ICommand rewind;
        public ICommand Rewind
        {
            get
            {
                if (rewind == null)
                {
                    rewind = new Command(PerformRewind);
                }

                return rewind;
            }
        }

        public void PerformRewind()
        {
            var TenSeconds = TimeSpan.FromSeconds(10);
            if (CrossMediaManager.Current.Position > TenSeconds)
                CrossMediaManager.Current.SeekTo(CrossMediaManager.Current.Position.Subtract(TenSeconds));
            else
                CrossMediaManager.Current.SeekToStart();
        }

        public string CurrentPositionString
        {
            get
            {
                TimeSpan currentMediaPosition = CrossMediaManager.Current.Position;
                if (IsPlaying)
                {
                    var value = "";
                    if (currentMediaPosition.Hours == 0)
                    {
                        value = $"{currentMediaPosition.Minutes:D1}:{currentMediaPosition.Seconds:D2}";
                    }
                    else
                    {
                        value = $"{currentMediaPosition.Hours:D1}:{currentMediaPosition.Minutes:D2}:{currentMediaPosition.Seconds:D2}";
                    }
                    return value;
                }
                else
                    return "";
            }
        }


        public double CurrentPosition
        {
            get
            {
                if (CrossMediaManager.Current == null)
                    return (double)0;
                else
                {
                    if (CrossMediaManager.Current.Position.TotalMilliseconds == 0)
                        return (double)0;
                    else
                        return CrossMediaManager.Current.Position.TotalMilliseconds
                            / CrossMediaManager.Current.Duration.TotalMilliseconds;
                }
            }
            set
            {
                var newPosition = TimeSpan.FromMilliseconds(value * CrossMediaManager.Current.Duration.TotalMilliseconds);
                CrossMediaManager.Current.SeekTo(newPosition);
            }
        }

        private ICommand fastforward;
        public ICommand FastForward
        {
            get
            {
                if (fastforward == null)
                {
                    fastforward = new Command(PerformFastForward);
                }

                return fastforward;
            }
        }

        public void PerformFastForward()
        {
            var TenSeconds = TimeSpan.FromSeconds(10);
            var LastTen = CrossMediaManager.Current.Duration.Subtract(TenSeconds);
            if (CrossMediaManager.Current.Position < LastTen)
                CrossMediaManager.Current.SeekTo(CrossMediaManager.Current.Position.Add(TenSeconds));
          }


        public void DownloadFile()
        {
            var Uri = new Uri(CurrentShow.ShowDetails.File.Url);

            WebClient webClient = new WebClient();
            using (webClient)
            {
                webClient.DownloadDataCompleted += (s, e) =>
                {
                    try
                    {
                        System.IO.File.WriteAllBytes(CachedFileName, e.Result);
                        IsCached = true;
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                    }
                };

                webClient.DownloadDataAsync(Uri);
            }
        }


        private ICommand stop;
        public ICommand Stop
        {
            get
            {
                if (stop == null)
                {
                    stop = new AsyncCommand(PerformStop);
                }
                return stop;
            }
        }

        public async Task PerformStop()
        {
            IsPlaying = false;
            CurrentStatus = "";
            await CrossMediaManager.Current.Stop();

            if (LocalFileStream != null)
            {
                LocalFileStream.Dispose();
            }
        }

        private string currentStatus;
        public string CurrentStatus
        {
            get => currentStatus;
            set => SetProperty(ref currentStatus, value);
        }

        private bool isCached;
        public bool IsCached
        {
            get => isCached;
            set => SetProperty(ref isCached, value);
        }
    }
}
