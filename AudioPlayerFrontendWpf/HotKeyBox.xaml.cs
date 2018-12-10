using StdOttFramework.Hotkey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AudioPlayerFrontend
{
    /// <summary>
    /// Interaktionslogik für HotKeyBox.xaml
    /// </summary>
    public partial class HotKeyBox : UserControl
    {
        public static readonly DependencyProperty HotKeyProperty =
            DependencyProperty.Register("HotKey", typeof(HotKey), typeof(HotKeyBox),
                new PropertyMetadata(null, new PropertyChangedCallback(OnHotKeyPropertyChanged)));

        private static void OnHotKeyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var s = (HotKeyBox)sender;
            var value = (HotKey)e.NewValue;

            s.SetText();
        }

        private bool isChanging;
        private Key? changeKey;
        private KeyModifier changeModifier;

        public HotKey HotKey
        {
            get { return (HotKey)GetValue(HotKeyProperty); }
            set { SetValue(HotKeyProperty, value); }
        }

        public HotKeyBox()
        {
            InitializeComponent();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key != Key.System ? e.Key : e.SystemKey;

            isChanging = true;

            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    changeModifier = Set(changeModifier, KeyModifier.Ctrl);
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    changeModifier = Set(changeModifier, KeyModifier.Alt);
                    break;

                case Key.LWin:
                case Key.RWin:
                    changeModifier = Set(changeModifier, KeyModifier.Win);
                    break;

                case Key.LeftShift:
                case Key.RightShift:
                    changeModifier = Set(changeModifier, KeyModifier.Shift);
                    break;

                default:
                    changeKey = key;
                    isChanging = false;

                    if (HotKey == null || (HotKey.Key != key || HotKey.KeyModifiers != changeModifier))
                    {
                        HotKey = new HotKey(key, changeModifier);
                    }
                    break;
            }

            SetText();

            e.Handled = true;
        }

        private KeyModifier Set(KeyModifier combination, KeyModifier add)
        {
            return (KeyModifier)((int)combination | (int)add);
        }

        private void Grid_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            Key key = e.Key != Key.System ? e.Key : e.SystemKey;

            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    changeModifier = Unset(changeModifier, KeyModifier.Ctrl);
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    changeModifier = Unset(changeModifier, KeyModifier.Alt);
                    break;

                case Key.LWin:
                case Key.RWin:
                    changeModifier = Unset(changeModifier, KeyModifier.Win);
                    break;

                case Key.LeftShift:
                case Key.RightShift:
                    changeModifier = Unset(changeModifier, KeyModifier.Shift);
                    break;

                default:
                    if (changeKey.HasValue && changeKey.Value == key) changeKey = null;
                    break;
            }

            SetText();

            e.Handled = true;
        }

        private KeyModifier Unset(KeyModifier combination, KeyModifier sub)
        {
            return (KeyModifier)((int)combination & ~(int)sub);
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            SetText();

            tbx.SelectAll();
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            isChanging = false;
            SetText();

            tbx.Select(0, 0);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetText();

            if (tbx.IsFocused) tbx.SelectAll();
            else tbx.Select(0, 0);
        }

        private void SetText()
        {
            Key? key;
            KeyModifier modifiers;

            if (isChanging)
            {
                key = changeKey;
                modifiers = changeModifier;
            }
            else
            {
                key = HotKey?.Key;
                modifiers = HotKey?.KeyModifiers ?? KeyModifier.None;
            }

            string text = string.Join(" + ", SeperateKeyModifier(modifiers).Select(GetName));

            if (key.HasValue)
            {
                if (text.Length > 0) text += " + ";

                text += GetName(key.Value);
            }

            tbx.Text = text;
        }

        private IEnumerable<KeyModifier> SeperateKeyModifier(KeyModifier input)
        {
            foreach (KeyModifier modifier in Enum.GetValues(typeof(KeyModifier)))
            {
                if (((int)input & (int)modifier) > 0) yield return modifier;
            }
        }

        private string GetName<T>(T enumValue)
        {
            return Enum.GetName(typeof(T), enumValue);
        }
    }
}
