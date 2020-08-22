using System;
using System.Collections.Generic;
//using System.Linq;

//!!! Move this module out of Drawing because WindowInput may be used for other purposes (sound etc)

using Torec.Drawing;

namespace Torec.Input
{
    public interface IImageInput
    {
        // handle user interaction. return true to request image update
        bool OnSize(double newWidth, double newHeight);
        bool OnMouseMove(double x, double y, WindowInput.Buttons buttons);

        // get updated image
        Image GetImage();
    }

}
