using StdOttFramework.Hotkey;
using StdOttStandard.CommendlinePaser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AudioPlayerFrontend
{
    public class HotKeysBuilder : INotifyPropertyChanged
    {
        private HotKey toggle, next, previous, play, pause, restart;
        private HotKeys hotKeys;

        public HotKey Toggle
        {
            get => toggle;
            set
            {
                if (value == toggle) return;

                toggle = value;
                OnPropertyChanged(nameof(Toggle));
            }
        }

        public HotKey Next
        {
            get => next;
            set
            {
                if (value == next) return;

                next = value;
                OnPropertyChanged(nameof(Next));
            }
        }

        public HotKey Previous
        {
            get => previous;
            set
            {
                if (value == previous) return;

                previous = value;
                OnPropertyChanged(nameof(Previous));
            }
        }

        public HotKey Play
        {
            get => play;
            set
            {
                if (value == play) return;

                play = value;
                OnPropertyChanged(nameof(Play));
            }
        }

        public HotKey Pause
        {
            get => pause;
            set
            {
                if (value == pause) return;

                pause = value;
                OnPropertyChanged(nameof(Pause));
            }
        }

        public HotKey Restart
        {
            get => restart;
            set
            {
                if (value == restart) return;

                restart = value;
                OnPropertyChanged(nameof(Restart));
            }
        }

        public HotKeysBuilder WithHotKeys(HotKeys hotKeys)
        {
            this.hotKeys = hotKeys;

            return this;
        }

        public HotKeysBuilder WithArgs(IEnumerable<string> args)
        {
            Option hkToggleOpt = new Option("ht", "hotkey-toggle", "Hotkey to toggle between play and pause", false, -1, 2);
            Option hkNextOpt = new Option("hn", "hotkey-next", "Hotkey to set next Media", false, -1, 2);
            Option hkPreviousOpt = new Option("hp", "hotkey-previous", "Hotkey to set previous Media", false, -1, 2);
            Option hkPlayOpt = Option.GetLongOnly("hotkey-play", "Hotkey to set the play state to play", false, -1, 2);
            Option hkPauseOpt = Option.GetLongOnly("hotkey-pause", "Hotkey to set the play state to pause", false, -1, 2);
            Option hkRestartOpt = Option.GetLongOnly("hotkey-restart", "Hotkey to restart the current media", false, -1, 2);

            Options options = new Options(hkToggleOpt, hkNextOpt, hkPreviousOpt);
            OptionParseResult result = options.Parse(args);

            OptionParsed p;
            HotKey hk;

            if (result.TryGetFirstValidOptionParseds(hkToggleOpt, out p) && TryGetHotKey(p, out hk)) toggle = hk;
            if (result.TryGetFirstValidOptionParseds(hkNextOpt, out p) && TryGetHotKey(p, out hk)) next = hk;
            if (result.TryGetFirstValidOptionParseds(hkPreviousOpt, out p) && TryGetHotKey(p, out hk)) previous = hk;
            if (result.TryGetFirstValidOptionParseds(hkPlayOpt, out p) && TryGetHotKey(p, out hk)) play = hk;
            if (result.TryGetFirstValidOptionParseds(hkPauseOpt, out p) && TryGetHotKey(p, out hk)) pause = hk;
            if (result.TryGetFirstValidOptionParseds(hkRestartOpt, out p) && TryGetHotKey(p, out hk)) restart = hk;

            return this;
        }

        private bool TryGetHotKey(OptionParsed parsed, out HotKey hotKey)
        {
            hotKey = null;

            Key key;
            string keyString = parsed.Values[0];
            int allModifier = 0;

            if (!Enum.TryParse(keyString, true, out key)) return false;

            foreach (string modifierString in parsed.Values.Skip(1).Select(v => v.ToLower()))
            {
                KeyModifier modifier;

                if (Enum.TryParse(modifierString, true, out modifier)) allModifier += (int)modifier;
            }

            hotKey = new HotKey(key, (KeyModifier)allModifier);
            return true;
        }

        public HotKeysBuilder WithToggle(Key key, params KeyModifier[] modifiers)
        {
            toggle = GetHotKey(key, modifiers);

            return this;
        }

        public HotKeysBuilder WithNext(Key key, params KeyModifier[] modifiers)
        {
            next = GetHotKey(key, modifiers);

            return this;
        }

        public HotKeysBuilder WithPrevious(Key key, params KeyModifier[] modifiers)
        {
            previous = GetHotKey(key, modifiers);

            return this;
        }

        public HotKeysBuilder WithPlay(Key key, params KeyModifier[] modifiers)
        {
            play = GetHotKey(key, modifiers);

            return this;
        }

        public HotKeysBuilder WithPause(Key key, params KeyModifier[] modifiers)
        {
            pause = GetHotKey(key, modifiers);

            return this;
        }

        public HotKeysBuilder WithRestart(Key key, params KeyModifier[] modifiers)
        {
            restart = GetHotKey(key, modifiers);

            return this;
        }

        private HotKey GetHotKey(Key key, IEnumerable<KeyModifier> modifiers)
        {
            KeyModifier modifier = (KeyModifier)modifiers.Select(m => (int)m).Sum();

            return new HotKey(key, modifier);
        }

        public HotKeys Build()
        {
            HotKeys hotKeys = this.hotKeys ?? new HotKeys();

            if (toggle != null) hotKeys.Toggle = toggle;
            if (next != null) hotKeys.Next = next;
            if (previous != null) hotKeys.Previous = previous;
            if (play != null) hotKeys.Play = play;
            if (pause != null) hotKeys.Pause = pause;
            if (restart != null) hotKeys.Restart = restart;

            return hotKeys;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
