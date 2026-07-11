
using System.Globalization;

//namespace Client.Services.EquipmentManagers
//{
//    public class CssStyleService
//    {
//        private readonly CameraService _camera;

//        public CssStyleService(CameraService camera)
//        {
//            _camera = camera;
//        }

//        public string WorkspaceCssClass => _camera.IsPanning ? "pfd-workspace is-panning" : "pfd-workspace";

//        public string WorkspaceBackgroundStyle => string.Create(
//            CultureInfo.InvariantCulture,
//            $"background-position: {Math.Round(_camera.PanX)}px {Math.Round(_camera.PanY)}px; background-size: {100 * _camera.Zoom}px {100 * _camera.Zoom}px, {100 * _camera.Zoom}px {100 * _camera.Zoom}px, {20 * _camera.Zoom}px {20 * _camera.Zoom}px, {20 * _camera.Zoom}px {20 * _camera.Zoom}px;");
//    }
//}