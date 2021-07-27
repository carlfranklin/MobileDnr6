# The .NET Show Episode 8

###### Building a Mobile Podcast App Part 6

See more projects at https://github.com/carlfranklin/DotNetShow

Watch the video at https://youtu.be/EjUiMqporwI

All episodes are listed at https://thedotnetshow.com

## Overview

Starting with episode 2 of The .NET Show, I am building a mobile podcast app for my podcast, .NET Rocks! using Xamarin Forms. 

At this point our app has a home page that shows episodes. When the user taps the "Details" button, we show a detail page with sophisticated media control: Play, Pause, Stop, Go Back, and Go Forward.

In this episode we are going to start adding Playlist functionality. Here are the user stories:

- As a user, I want to create a new playlist, and give it a name.
- As a user, I want the app to automatically persist my playlists.
- As a user, I want to be able to retrieve a playlist from the list of playlists I have created.
- As a user, I want to delete an existing playlist.
- As a user, I want to select one or more episodes to add to a playlist.
- As a user, I want to be able to remove episodes from my playlist.
- As a user, I want to be able to move episodes in my playlist forward or backward in play order.
- As a user, I want the same level of audio control for a playlist as I have for a single episode: Play, Pause, Stop, Go Back, and Go Forward.

We will not get to all of these for this show, but we will implement creating new playlists, persisting them to the device, and also deleting playlists.

### Step 30: Add a PlayList model

To the *Models* folder, add the following: 

*PlayList.cs*:

```c#
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetRocks.Models
{
    public class PlayList
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public List<Show> Shows { get; set; } = new List<Show>();
    }
}
```

Notice that I'm defining the Id as a `Guid`. I'm done with auto-incrementing Integer primary keys. 

### Step 31: Add a new ViewModel for managing Playlists

To the *ViewModels* folder, add the following:

*PlayListManagerPageViewModel.cs*:

```c#
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
                string FileName = $"{CacheDir}/{existing.Id}.json";
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
```

Note this line in the constructor:

```c#
CacheDir = FileSystem.CacheDirectory + "/playlists";
```

If you recall, `FileSystem.CacheDirectory` abstracts the platform-specific folder where cached files are stored on the device. We use this folder for mp3 files that we download. 

In the case of playlists, we want to put them in a subfolder called */playlists*.

The next bit of code creates the folder on the device if it does not exist:

```c#
if (!Directory.Exists(CacheDir))
    Directory.CreateDirectory(CacheDir);
```

Next, we define a read-only property for the `PlayList` list:

```c#
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
```

If the private `ObservableCollection<PlayList>` doesn't exist, we create a new one, read all the *.json* files from the folder, de-serialize them, and add the `PlayList` objects to the list.

The next few methods are `private` because we're going to call them internally to manage the list. Let's look at `SavePlayLists()`:

```c#
private void SavePlayLists()
{
    foreach (var list in PlayLists)
    {
        string json = JsonConvert.SerializeObject(list);
        string FileName = $"{CacheDir}/{list.Id}.json";
        System.IO.File.WriteAllText(FileName, json);
    }
}
```

This method saves each `PlayList` object in the list, creating a file name from the `Id` which is unique, and only assigned when a new `PlayList` is created.

Next, take a look at `AddOrUpdatePlayList`:

```c#
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
```

This method handles creating and updating a `PlayList`.

First, we check to see if it already exists. If it exists we replace it in the list. If it does not exist, we set the Id property to a new `Guid` and add it to the list:

```c#
private Guid CreateGuid()
{
    var obj = new object();
    var rnd = new Random(obj.GetHashCode());
    var bytes = new byte[16];
    rnd.NextBytes(bytes);
    return new Guid(bytes);
}
```

No matter what happens, we call `SavePlayLists()` to rewrite the files in the cache.

Next, let's look at `DeletePlayList`:

```c#
private void DeletePlayList(PlayList playList)
{
    var existing = (from x in PlayLists 
                    where x.Id == playList.Id 
                    select x).FirstOrDefault();
    if (existing != null)
    {
        string FileName = $"{CacheDir}/{existing.Id}.json";
        if (System.IO.File.Exists(FileName))
        {
            System.IO.File.Delete(FileName);
        }
        PlayLists.Remove(existing);
        SavePlayLists();
    }
}
```

As before, we're looking up the playlist, and if it exists we delete the file, remove it from the list, and re-save the list.

Next, we're going to add a couple commands: one to create a play list, and one to delete a playlist.

Add the following code:

```c#
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
```

Notice the `Page.DisplayPromptAsync` call. This is a method on the `Page` class in Xamarin Forms that displays an input dialog box. The issue is that the ViewModel doesn't know about the Page, so in order to give it access, I've created this property:

```c#
public ContentPage Page { get; set; }
```

Now, where does this `Page` property get set? In the View's code-behind class, just like we did with the `DetailPage`.  We'll add that code after we create the view.

I'm next creating a new `PlayList` from the specified name, and adding it to the list.

Note that I'm calling `OnPropertyChanged` so the UI knows to read the `PlayLists` property again.

Next, add this code to support deletes:

```c#
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
```

Now we're ready to create a new view for this ViewModel.

### Step 32 - Add a new PlayList Manager Page

To the *Views* folder, add a new `ContentPage` called *PlayListManagerPage.xaml*:

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://xamarin.com/schemas/2014/forms"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:DotNetRocks"
             xmlns:viewmodels="clr-namespace:DotNetRocks.ViewModels"
             xmlns:dxcv="http://schemas.devexpress.com/xamarin/2014/forms/collectionview"
             x:Class="DotNetRocks.Views.PlayListManagerPage">

    <ContentPage.BindingContext>
        <viewmodels:PlayListManagerPageViewModel/>
    </ContentPage.BindingContext>
    
    <ContentPage.Content>
        <StackLayout>
            <Button Text="Add New" Command="{Binding NewPlayList}"/>
            <dxcv:DXCollectionView x:Name="MyCollectionView" 
                               ItemsSource="{Binding PlayLists}">
                <dxcv:DXCollectionView.ItemTemplate>
                    <DataTemplate>
                        <StackLayout>
                            <Label>
                                <Label.FormattedText>
                                    <FormattedString>
                                        <Span Text="{Binding Name}"/>
                                        <Span Text=" ("/>
                                        <Span Text="{Binding Shows.Count}"/>
                                        <Span Text=") "/>
                                        <Span Text="{Binding DateCreated,
                                                    StringFormat='{d}'}" />
                                    </FormattedString>
                                </Label.FormattedText>
                            </Label>
                            <StackLayout Orientation="Horizontal">
                                <Button Text="Delete" 
                                        Command="{Binding Delete,
                                        Source={RelativeSource AncestorType=
                                        {x:Type viewmodels:PlayListManagerPageViewModel}}}" 
                                        CommandParameter="{Binding Id}" />
                            </StackLayout>
                            <Line Stroke="Gray" X1="0" X2="500" StrokeThickness="2" 
                                  Margin="0,10,0,10" />
                        </StackLayout>
                    </DataTemplate>
                </dxcv:DXCollectionView.ItemTemplate>
                <dxcv:DXCollectionView.Margin>
                    <OnIdiom x:TypeArguments="Thickness" Phone="10,10,10,10" 
                             Tablet="71,0,0,0"/>
                </dxcv:DXCollectionView.Margin>
            </dxcv:DXCollectionView>
        </StackLayout>
    </ContentPage.Content>
</ContentPage>
```

Add the following to *PlayListManagerPage.xaml.cs*:

```c#
protected override async void OnAppearing()
{
    await Task.Delay(0);
    var viewModel = (PlayListManagerPageViewModel)BindingContext;
    viewModel.Page = this;
}
```

You'll need this:

```c#
using DotNetRocks.ViewModels;
```

Whenever this page is displayed, this code sets the ViewModel's `Page` property to `this`, which represents the page itself.

Before we run this, we have to add this page to the routes in the constructor of *AppShell.xaml.cs*:

```c#
public AppShell()
{
    InitializeComponent();
    Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
    Routing.RegisterRoute(nameof(DetailPage), typeof(DetailPage));
    Routing.RegisterRoute(nameof(PlayListManagerPage), 
                          typeof(PlayListManagerPage));
}
```

Now, in *AppShell.xaml*, at the bottom we will add a new `FlyoutItem` for our page so we can access it from the menu. ***However***, we do not need to access teh `DetailPage` from the menu, so we'll remove that one. 

You'll be left with only these two `FlyoutItem`s at the bottom of *AppShell.xaml*. 

Replace this:

```xaml
<FlyoutItem Title="PlayList Manager" Icon="icon_about.png">
    <ShellContent Route="PlayListManagerPage" ContentTemplate="{DataTemplate local:PlayListManagerPage}" />
</FlyoutItem>
```

with this:

```xaml
<FlyoutItem Title="PlayList Manager" Icon="icon_about.png">
    <ShellContent Route="PlayListManagerPage" ContentTemplate="{DataTemplate local:PlayListManagerPage}" />
</FlyoutItem>
```

Just to be clear, you *always* have to register the route for a page if you want to navigate to it, but you only need to define `FlyoutItem`s for pages that you want to access from the menu.

Now we can run this to test adding and deleting `PlayList` items, which are persisted to disk. Try adding and deleting PlayLists. 

<img src="ModalDialogs Images/image-20210723154134638.png" alt="image-20210723154134638" style="zoom:60%;" />

<img src="ModalDialogs Images/image-20210723154204908.png" alt="image-20210723154204908" style="zoom:60%;" />

<img src="ModalDialogs Images/image-20210723154236804.png" alt="image-20210723154236804" style="zoom:60%;" />

<img src="ModalDialogs Images/image-20210723154259036.png" alt="image-20210723154259036" style="zoom:60%;" />

Now let's look through the Page Xaml.

At the top we define our `BindingContext`:

```xaml
<ContentPage.BindingContext>
    <viewmodels:PlayListManagerPageViewModel/>
</ContentPage.BindingContext>
```

Inside the main `StackLayout`, we define the **Add New** button.

```xaml
<Button Text="Add New" Command="{Binding NewPlayList}" />
```

Next we have a `DXCollectionView` bound to the `PlayLists` property of the ViewModel.

Inside the `ItemTemplate` I'm introducing you to the `FormattedText` property of the Label:

```xaml
<Label>
    <Label.FormattedText>
        <FormattedString>
            <Span Text="{Binding Name}"/>
            <Span Text=" ("/>
            <Span Text="{Binding Shows.Count}"/>
            <Span Text=") "/>
            <Span Text="{Binding DateCreated, StringFormat='{d}'}" />
        </FormattedString>
    </Label.FormattedText>
</Label>
```

It should be pretty self-explanatory. It produces a string like this:

```
Azure (0) 7/23/21 3:52:41 PM
```

Next, I'm creating a horizontal `StackLayout` to house the buttons that let the user act on individual `PlayList`s. We're starting with **Delete**, but we'll have an **Edit** button in there before too long:

```xaml
<StackLayout Orientation="Horizontal">
    <Button Text="Delete" 
            Command="{Binding Delete,
                     Source={RelativeSource AncestorType=
                     {x:Type viewmodels:PlayListManagerPageViewModel}}}" 
            CommandParameter="{Binding Id}" />
</StackLayout>
```

If you recall, items in the `DXCollectionView.ItemTemplate` are bound to the `PlayList`. So, in order to bind to the ViewModel, we have to set the `Source` property of the `Command`.

Finally, each item is separated by a light gray line:

```xaml
<Line Stroke="Gray" X1="0" X2="500" StrokeThickness="2" 
                                  Margin="0,10,0,10" />
```

That's where we will stop today. Next time, we'll start working on the playlist itself: Adding, updating, and deleting episodes. Also, we'll let the user re-order the episodes.
