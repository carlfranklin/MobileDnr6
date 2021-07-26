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
using Xamarin.Forms;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.ObjectModel;

namespace DotNetRocks.ViewModels
{
    public class PlayListManagerPageViewModel : BaseViewModel
    {
        string CacheDir = "";
        
        public PlayListManagerPageViewModel()
        {
            Barrel.ApplicationId = "mobile_dnr";
            CacheDir = FileSystem.CacheDirectory + "/playlists";
            if (!Directory.Exists(CacheDir))
                Directory.CreateDirectory(CacheDir);
        }

        private ObservableCollection<PlayList> playLists;
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
                    newPlayList = new AsyncCommand(PerformNewPlayList);
                }

                return newPlayList;
            }
        }

        public ContentPage Page { get; set; }

        public async Task PerformNewPlayList()
        {
            string Name = await Page.DisplayPromptAsync("New PlayList", "Enter a name:");
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
                    delete = new AsyncCommand<Guid>(PerformDeletePlayList);
                }

                return delete;
            }
        }

        public async Task PerformDeletePlayList(Guid Id)
        {
            await Task.Delay(0);
            var existing = (from x in PlayLists where x.Id == Id select x).FirstOrDefault();
            if (existing != null)
            {
                DeletePlayList(existing);
            }
            base.OnPropertyChanged("PlayLists");
        }

        private void SavePlayLists()
        {
            foreach (var list in PlayLists)
            {
                string json = JsonConvert.SerializeObject(list);
                string FileName = $"{CacheDir}/{list.Id}.json";
                System.IO.File.WriteAllText(FileName, json);
            }
        }

        private void AddOrUpdatePlayList(PlayList playList)
        {
            var existing = (from x in PlayLists 
                            where x.Id == 
                            playList.Id select x).FirstOrDefault();
            if (existing != null)
            {
                var index = PlayLists.IndexOf(existing);
                PlayLists[index] = playList;
            }
            else
            {
                playList.Id = CreateGuid();
                PlayLists.Add(playList);
            }
            // Save to disk
            SavePlayLists();
        }

        private Guid CreateGuid()
        {
            var obj = new object();
            var rnd = new Random(obj.GetHashCode());
            var bytes = new byte[16];
            rnd.NextBytes(bytes);
            return new Guid(bytes);
        }

        private void DeletePlayList(PlayList playList)
        {
            var existing = (from x in PlayLists 
                            where x.Id == playList.Id 
                            select x).FirstOrDefault();
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

    }
}
