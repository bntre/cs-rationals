using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.CustomControls
{
    //!!! Avalonia has no TextBox.TextChanged ? https://github.com/AvaloniaUI/Avalonia/issues/418
    public class TextBox2 : TextBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(TextBox);

        public TextBox2() : base() {
            textChangedSubscription = this.GetObservable(TextBox2.TextProperty).Subscribe(TextChangedSubscriptionHandler);
        }

        private IDisposable textChangedSubscription;

        private void TextChangedSubscriptionHandler(string newText) {
            TextChanged?.Invoke(this, new RoutedEventArgs());
        }

        public event EventHandler<RoutedEventArgs> TextChanged;
    }
}
