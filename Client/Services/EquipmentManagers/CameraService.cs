
using Microsoft.AspNetCore.Components.Web;
using System;

//namespace Client.Services.EquipmentManagers
//{
//    public class CameraService
//    {
//        private bool _isPanning;
//        private double _lastPanMouseX;
//        private double _lastPanMouseY;

//        public double Zoom { get; set; } = 1.0;
//        public double PanX { get; set; } = 0;
//        public double PanY { get; set; } = 0;
//        public double GlobalScale { get; set; } = 0.7;

//        public bool IsPanning => _isPanning;

//        public string CameraTransform => string.Create(
//            System.Globalization.CultureInfo.InvariantCulture,
//            $"translate({Math.Round(PanX)}px, {Math.Round(PanY)}px) scale({Zoom}) scale({GlobalScale})");

//        public Action? OnNotifyUI;

//        public void StartPan(MouseEventArgs e, bool isMovingAny, bool isConnectionModeActive)
//        {
//            if (!isMovingAny && !isConnectionModeActive && (e.Button == 0 || e.Button == 1))
//            {
//                _isPanning = true;
//                _lastPanMouseX = e.ClientX;
//                _lastPanMouseY = e.ClientY;
//                OnNotifyUI?.Invoke();
//            }
//        }

//        public void Pan(MouseEventArgs e)
//        {
//            if (_isPanning)
//            {
//                PanX += (e.ClientX - _lastPanMouseX);
//                PanY += (e.ClientY - _lastPanMouseY);
//                _lastPanMouseX = e.ClientX;
//                _lastPanMouseY = e.ClientY;
//                OnNotifyUI?.Invoke();
//            }
//        }

//        public void EndPan()
//        {
//            _isPanning = false;
//            OnNotifyUI?.Invoke();
//        }

//        public void ZoomAt(double dY, double pX, double pY)
//        {
//            double zF = dY > 0 ? 0.9 : 1.1;
//            double nZ = Zoom * zF;
//            if (nZ < 0.2) nZ = 0.2;
//            if (nZ > 3.0) nZ = 3.0;
//            double lX = (pX - PanX) / Zoom;
//            double lY = (pY - PanY) / Zoom;
//            Zoom = nZ;
//            PanX = pX - (lX * Zoom);
//            PanY = pY - (lY * Zoom);
//            OnNotifyUI?.Invoke();
//        }

//        public void ZoomToFit(double screenWidth, double screenHeight, double minX, double maxX, double minY, double maxY)
//        {
//            double contentWidth = maxX - minX;
//            double contentHeight = maxY - minY;
//            double padding = 100;
//            double scaleX = (screenWidth - padding) / contentWidth;
//            double scaleY = (screenHeight - padding) / contentHeight;
//            Zoom = Math.Min(scaleX, scaleY);
//            Zoom = Math.Clamp(Zoom, 0.5, 1.2);
//            double effectiveScale = Zoom * GlobalScale;
//            PanX = (screenWidth - (contentWidth * effectiveScale)) / 2 - (minX * effectiveScale);
//            PanY = (screenHeight - (contentHeight * effectiveScale)) / 2 - (minY * effectiveScale);
//            OnNotifyUI?.Invoke();
//        }

//        public void Reset()
//        {
//            PanX = 0;
//            PanY = 0;
//            Zoom = 1.0;
//            OnNotifyUI?.Invoke();
//        }
//    }
//}