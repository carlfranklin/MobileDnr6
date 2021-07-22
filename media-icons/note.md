### About these images

I downloaded a .PNG file containing media buttons from 
https://pixabay.com/vectors/multimedia-controls-buttons-play-154187/

**Pixbay License (from their website):**
	*Free for commercial use*
	*No attribution required*

You can download the individual buttons at https://github.com/carlfranklin/MobileDnr5/tree/master/media-icons

This is the .NET 4 console app I wrote to extract the individual icons from the composite image:

```c#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace GenIcons
{
    class Program
    {
        static void Main(string[] args)
        {
            // Change to your path
            string path = @"D:\Repos\!MobileDnr\MobileDnr5\media-icons\";
            
            string composite = path + "multimedia-154187_1280.png";
            var img = Image.FromFile(composite);

            int top = 235;
            int width = 170;
            int height = 170;

            string rew = path + "rewind.png";
            var rect = new Rectangle(185, top, width, height);
            var rewindImg = Crop(img, rect);
            rewindImg.Save(rew, System.Drawing.Imaging.ImageFormat.Png);

            string ffwd = path + "ffwd.png";
            rect = new Rectangle(371, top, width, height);
            var ffwdImage = Crop(img, rect);
            ffwdImage.Save(ffwd, System.Drawing.Imaging.ImageFormat.Png);

            string play = path + "play.png";
            rect = new Rectangle(556, top, width, height);
            var playImg = Crop(img, rect);
            playImg.Save(play, System.Drawing.Imaging.ImageFormat.Png);

            string pause = path + "pause.png";
            rect = new Rectangle(741, top, width, height);
            var pauseImg = Crop(img, rect);
            pauseImg.Save(pause, System.Drawing.Imaging.ImageFormat.Png);

            string stop = path + "stop.png";
            rect = new Rectangle(927, top +2, width-2, height-2);
            var stopImg = Crop(img, rect);
            // I had to resize this one to 170x170.
            var resizedStopImg = (Image)(new Bitmap(stopImg, 170, 170));
            resizedStopImg.Save(stop, System.Drawing.Imaging.ImageFormat.Png);
            
        }

        static Image Crop(Image img, Rectangle Rect)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(Rect, bmpImage.PixelFormat);
        }
    }
}
```

