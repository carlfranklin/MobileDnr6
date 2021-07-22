using DotNetRocks.Models;
using System;
using System.Collections.Generic;
using MvvmHelpers;
using System.Windows.Input;
using MvvmHelpers.Commands;
using MediaManager;
using System.Threading.Tasks;
using MonkeyCache.FileStore;
using System.IO;
using System.Net;
using Xamarin.Essentials;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.ObjectModel;

namespace DotNetRocks.ViewModels
{
    public class PlayListManagerPageViewModel : BaseViewModel
    {
        string CacheDir = "";
        private ObservableCollection<PlayList> playLists;

        public PlayListManagerPageViewModel()
        {
            Barrel.ApplicationId = "mobile_dnr";
            CacheDir = FileSystem.CacheDirectory + "/playlists";
            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);
            }
        }

        public ObservableCollection<PlayList> PlayLists
        {
            get
            {
                if (playLists == null)
                {
                    playLists = new ObservableCollection<PlayList>();
                    var playListJsonFiles = Directory.GetFiles(CacheDir, "*.json");
                    foreach (var file in playListJsonFiles)
                    {
                        var json = System.IO.File.ReadAllText(file);
                        var list = JsonConvert.DeserializeObject<PlayList>(json);
                        playLists.Add(list);
                    }
                }
                return playLists;
            }
        }

        private ICommand newPlayList;
        public ICommand NewPlayList
        {
            get
            {
                if (newPlayList == null)
                {
                    newPlayList = new AsyncCommand<string>(CreateNewPlayList);
                }

                return newPlayList;
            }
        }

        public async Task CreateNewPlayList(string Name)
        {
            await Task.Delay(0);
            var playList = new PlayList() { Name = Name, DateCreated = DateTime.Now };
            AddOrUpdatePlayList(playList);
            base.OnPropertyChanged("PlayLists");
        }

        private ICommand delete;
        public ICommand Delete
        {
            get
            {
                if (delete == null)
                {
                    delete = new AsyncCommand<string>(PerformDeletePlayList);
                }

                return delete;
            }
        }

        public async Task PerformDeletePlayList(string Name)
        {
            await Task.Delay(0);
            var existing = (from x in PlayLists where x.Name == Name select x).FirstOrDefault();
            if (existing != null)
            {
                DeletePlayList(existing);
            }
            base.OnPropertyChanged("PlayLists");
        }

        private void AddOrUpdatePlayList(PlayList playList)
        {
            var existing = (from x in PlayLists where x.Id == playList.Id select x).FirstOrDefault();
            if (existing != null)
            {
                var index = PlayLists.IndexOf(existing);
                PlayLists[index] = playList;
            }
            else
            {
                playList.Id = PlayLists.Count + 1;
                PlayLists.Add(playList);
            }
            // Save to disk
            SavePlayLists();
        }

        private void DeletePlayList(PlayList playList)
        {
            var existing = (from x in PlayLists where x.Name == playList.Name select x).FirstOrDefault();
            if (existing != null)
            {
                string FileName = $"{CacheDir}/{existing.Name}.json";
                if (System.IO.File.Exists(FileName))
                {
                    System.IO.File.Delete(FileName);
                }
                PlayLists.Remove(existing);
                SavePlayLists();
            }
        }

        private void SavePlayLists()
        {
            foreach (var list in PlayLists)
            {
                string json = JsonConvert.SerializeObject(list);
                string FileName = $"{CacheDir}/{list.Name}.json";
                System.IO.File.WriteAllText(FileName, json);
            }
        }
    }
}
