using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.CustomControls
{
    //!!! Avalonia has no Slider.ValueChanged ?
    public class Slider2 : Slider, IStyleable
    {
        Type IStyleable.StyleKey => typeof(Slider);

        public Slider2() : base()
        {
            valueChangedSubscription = this.GetObservable(Slider2.ValueProperty).Subscribe(ValueChangedSubscriptionHandler);
        }

        private IDisposable valueChangedSubscription;

        private void ValueChangedSubscriptionHandler(double newValue)
        {
            ValueChanged?.Invoke(this, new RoutedEventArgs());
        }

        public event EventHandler<RoutedEventArgs> ValueChanged;
    }
}
