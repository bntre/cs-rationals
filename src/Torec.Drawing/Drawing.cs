using System;
using System.Collections.Generic;
//using System.Linq;

namespace Torec.Drawing
{
    public class Viewport : IViewport {
        // used to transform from user space (user units) to image space (e.g. pixels)
        protected Point _imageSize;
        protected Point[] _bounds;
        //
        private int _dirX; // -1 or 1
        private int _dirY; // -1 or 1
        // secondary values - updated in Update()
        protected float _scaleX; // factor
        protected float _scaleY;
        protected Point _origin; // in user units
        //
        public Viewport(bool yUp = true)
            : this(100,100, 0,1, 0,1, yUp) 
        { }

        public Viewport(float sizeX, float sizeY, float x0, float x1, float y0, float y1, bool yUp = true) {
            _dirX = 1;
            _dirY = yUp ? -1 : 1;
            SetImageSize(sizeX, sizeY);
            SetUserBounds(x0,x1, y0,y1);
            Update();
        }

        protected void SetImageSize(Point size) {
            _imageSize = size;
        }
        protected void SetImageSize(float w, float h) {
            SetImageSize(new Point(w, h));
        }
        protected void SetUserBounds(Point[] bounds) {
            _bounds = bounds;
        }
        protected void SetUserBounds(float x0, float x1, float y0, float y1) {
            SetUserBounds(new[] { new Point(x0,y0), new Point(x1,y1) });
        }

        protected void Update() {
            // scale
            Point size = _bounds[1] - _bounds[0]; // size in user units
            _scaleX = _imageSize.X / size.X; // * _dirX here !!! 
            _scaleY = _imageSize.Y / size.Y;
            // origin
            _origin = new Point(
                _bounds[_dirX > 0 ? 0 : 1].X, 
                _bounds[_dirY > 0 ? 0 : 1].Y
            );
        }

        public Point GetImageSize() { return _imageSize; }
        public Point[] GetUserBounds() { return _bounds; }

        #region User -> Image coordinates
        public float ToImage(float size) { return size * Math.Abs(_scaleY); }
        public Point ToImage(Point p) {
            p -= _origin;
            p = new Point(
                p.X * _dirX * _scaleX,
                p.Y * _dirY * _scaleY
            );
            return p;
        }
        #endregion

        #region Image -> User coordinates
        public float ToUser(float size) { return size / _scaleY; }
        public Point ToUser(Point p) {
            p = new Point(
                p.X / (_dirX * _scaleX),
                p.Y / (_dirY * _scaleY)
            );
            p += _origin;
            return p;
        }
        #endregion
    }

    /*
    public class Viewport2 : IViewport {
        private Point _imageSize;
        private Point _userCenter;
        private Point _scaleAdditional; // used for "scaling" resize (depends on window size), not a saved setting
        private Point _scale;
        private float _scaleScalar; // additional scale considered

        public Viewport2() : this(100,100, 0,0, 50,-50) { }
        public Viewport2(float sizeX, float sizeY, float centerX, float centerY, float scaleX, float scaleY) {
            SetImageSize(sizeX, sizeY);
            SetCenter(centerX, centerY);
            SetAdditionalScale(1f, 1f);
            SetScale(scaleX, scaleY);
        }

        public Point GetImageSize() { return _imageSize; }
        public Point GetScale()     { return _scale; }
        public Point GetCenter()    { return _userCenter; }

        public Point[] GetUserBounds() {
            Point p0 = ToUser(new Point(0, 0));
            Point p1 = ToUser(_imageSize);
            bool px = p0.X <= p1.X;
            bool py = p0.Y <= p1.Y;
            return new[] {
                new Point(px ? p0.X : p1.X,  py ? p0.Y : p1.Y),
                new Point(px ? p1.X : p0.X,  py ? p1.Y : p0.Y)
            };
        }

        #region User -> Image coordinates
        public float ToImage(float size) { return size * _scaleScalar; }
        public Point ToImage(Point u) {
            Point scale = _scale.Mul(_scaleAdditional);
            return _imageSize/2 + (u - _userCenter).Mul(scale);
        }
        #endregion

        #region Image -> User coordinates
        public float ToUser(float size) { return size / _scaleScalar; }
        public Point ToUser(Point p) {
            Point scale = _scale.Mul(_scaleAdditional);
            return _userCenter + (p - _imageSize/2).Div(scale);
        }
        #endregion

        // Updating
        public void SetImageSize(float sizeX, float sizeY) {
            _imageSize.X = sizeX;
            _imageSize.Y = sizeY;
        }
        public void SetCenter(float centerX, float centerY) {
            _userCenter.X = centerX;
            _userCenter.Y = centerY;
        }
        public void SetCenterDelta(float centerDX, float centerDY) {
            Point scale = _scale.Mul(_scaleAdditional);
            _userCenter.X += centerDX / scale.X;
            _userCenter.Y += centerDY / scale.Y;
        }
        public void SetAdditionalScale(float scaleX, float scaleY) {
            _scaleAdditional.X = scaleX;
            _scaleAdditional.Y = scaleY;
            UpdateScaleScalar();
        }
        public void SetScale(float scaleX, float scaleY) {
            _scale.X = scaleX;
            _scale.Y = scaleY;
            UpdateScaleScalar();
        }
        public void SetScaleDelta(float scaleDX, float scaleDY, int mouseX, int mouseY) {
            Point mouseImagePos = new Point(mouseX, mouseY);
            Point mouseUserPos0 = ToUser(mouseImagePos);

            _scale.X *= scaleDX;
            _scale.Y *= scaleDY;
            UpdateScaleScalar();

            Point mouseUserPos1 = ToUser(mouseImagePos);
            _userCenter -= mouseUserPos1 - mouseUserPos0;
        }

        private void UpdateScaleScalar() {
            Point scale = _scale.Mul(_scaleAdditional);
            _scaleScalar = (float)Math.Sqrt(Math.Abs(scale.X * scale.Y));
        }
    }
    */

    public class Viewport3 : IViewport
    {
        // primary settings
        private Point _imageSize;
        private Point _userCenter = new Point(0f, 0f);
        private Point _scaleSaved = new Point(1f, 1f);

        // secondary (updated)
        private Point _imageInitialSize = Point.Empty;
        private Point _scaleAdditional = new Point(1f, 1f); // depends on window client size, used for window "scaling resize", not a saved setting
        private Point _scale; // _scaleSaved * _scaleAdditional; [user point] * _scale = [image point]
        private float _scaleScalar; // scalar value of _scale

        private void UpdateScale(bool imageSizeChanged = false) {
            if (imageSizeChanged) {
                _scaleAdditional.X =  (float)Math.Sqrt(_imageSize.X * _imageInitialSize.X) / 2;
                _scaleAdditional.Y = -(float)Math.Sqrt(_imageSize.Y * _imageInitialSize.Y) / 2; //!!! we flip here
            }
            _scale = _scaleSaved.Mul(_scaleAdditional);
            _scaleScalar = (float)Math.Sqrt(Math.Abs(_scale.X * _scale.Y));
        }

        public Point GetImageSize() { return _imageSize; }
        public Point GetScaleSaved() { return _scaleSaved; }
        public Point GetUserCenter() { return _userCenter; }

        public void SetImageSize(float sizeX, float sizeY) {
            _imageSize.X = sizeX;
            _imageSize.Y = sizeY;
            if (_imageInitialSize.IsEmpty()) {
                _imageInitialSize = _imageSize;
            }
            UpdateScale(true);
        }
        public void SetScaleSaved(float scaleX, float scaleY) {
            _scaleSaved.X = scaleX;
            _scaleSaved.Y = scaleY;
            UpdateScale();
        }
        public void SetUserCenter(float userCenterX, float userCenterY) {
            _userCenter.X = userCenterX;
            _userCenter.Y = userCenterY;
        }

        public void MoveOrigin(Point imageDelta) {
            _userCenter += imageDelta.Div(_scale);
        }

        public void AddScale(float linearDelta, bool straight, Point pointerPos) {
            Point mouseUserPos0 = ToUser(pointerPos);

            float d = (float)Math.Exp(linearDelta);
            _scaleSaved.X *= straight ? d : (1f/d);
            _scaleSaved.Y *= d;
            UpdateScale();

            Point mouseUserPos1 = ToUser(pointerPos);
            _userCenter -= mouseUserPos1 - mouseUserPos0;
        }

        #region IViewport methods (used by Image)
        public Point[] GetUserBounds() {
            Point p0 = ToUser(new Point(0, 0));
            Point p1 = ToUser(_imageSize);
            bool px = p0.X <= p1.X;
            bool py = p0.Y <= p1.Y;
            return new[] {
                new Point(px ? p0.X : p1.X,  py ? p0.Y : p1.Y),
                new Point(px ? p1.X : p0.X,  py ? p1.Y : p0.Y)
            };
        }
        public float ToImage(float size) { return size * _scaleScalar; }
        public Point ToImage(Point u) { return _imageSize/2 + (u - _userCenter).Mul(_scale); }
        public float ToUser(float size) { return size / _scaleScalar; }
        public Point ToUser(Point p) { return _userCenter + (p - _imageSize/2).Div(_scale); }
        #endregion

    }


    public static class Utils
    {
        public static Point[] ToImage(IViewport viewport, Point[] ps) {
            var res = new Point[ps.Length];
            for (int i = 0; i < ps.Length; ++i) {
                res[i] = viewport.ToImage(ps[i]);
            }
            return res;
        }
    }
}
