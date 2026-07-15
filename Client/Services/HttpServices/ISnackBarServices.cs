using MudBlazor;
using Shared.Results;

namespace Client.Services.HttpServices
{
    public interface ISnackBarService
    {
        void ShowError(string message);
        void ShowMessage(Result result);
        void ShowSuccess(string message);
    }
    public class SnackBarService : ISnackBarService
    {
        private readonly ISnackbar _mudSnackbar;

        public SnackBarService(ISnackbar _snackBar)
        {
            this._mudSnackbar = _snackBar;
            this._mudSnackbar.Configuration.PositionClass = Defaults.Classes.Position.TopRight;
            this._mudSnackbar.Configuration.HideTransitionDuration = 1000;
            this._mudSnackbar.Configuration.NewestOnTop = true;
            this._mudSnackbar.Configuration.SnackbarVariant = Variant.Filled;
            this._mudSnackbar.Configuration.BackgroundBlurred = true;
        }

        public void ShowMessage(Result result)
        {
            if (string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            if (result.Succeeded)
            {
               
                ShowSuccess(result.Message);
            }
            else
            {
            
                ShowError(result.Message);
            }
        }
        public void ShowError(string message)
        {
            _mudSnackbar.Add(message, Severity.Error);
        }
        public void ShowSuccess(string message)
        {
            _mudSnackbar.Add(message, Severity.Success);
        }
    }
}
