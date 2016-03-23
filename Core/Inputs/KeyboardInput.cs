// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Framefield.Core.Inputs
{

    public class KeyboardInput : IDisposable
    {
        public class KeyEventArgs : EventArgs
        {
            public Keys Keys { get; private set; }
            public SharpDX.RawInput.KeyState KeyState { get; private set; }
            public KeyEventArgs(Keys keys, SharpDX.RawInput.KeyState keyState) 
            {
                KeyState = keyState;
                Keys = keys;
            }
        }
        public delegate void KeyDelegate(object o, KeyEventArgs e);

        public event KeyDelegate KeyPressedEvent;
        public event KeyDelegate KeyReleasedEvent;

        public KeyboardInput()
        {
            SharpDX.RawInput.Device.KeyboardInput += HandleKeyboardInput;
        }

        public bool IsKeyDown(Keys key)
        {
            KeyState keyState;
            if (_keysStates.TryGetValue((int) key, out keyState))
            {
                return keyState.IsKeyDown;
            }
            return false;
        }

        public bool IsKeyDown(string key)
        {
            try
            {
                return IsKeyDown((Keys) _keysConverter.ConvertFromString(key));
            }
            catch (Exception)
            {
            }
            return false;
        }

        public bool IsKeyUp(Keys key)
        {
            KeyState keyState;
            if (_keysStates.TryGetValue((int) key, out keyState))
            {
                return keyState.IsKeyUp;
            }
            return true;
        }

        public bool IsKeyUp(string key)
        {
            try
            {
                return IsKeyUp((Keys) _keysConverter.ConvertFromString(key));
            }
            catch (Exception)
            {
            }
            return false;
        }

        public void Dispose()
        {
            SharpDX.RawInput.Device.KeyboardInput -= HandleKeyboardInput;
        }

        private void HandleKeyboardInput(object sender, SharpDX.RawInput.KeyboardInputEventArgs e)
        {
            var newKeyState = new KeyState() { CurrentState = e.State };

            KeyState lastKeyState;

            try
            {
                if (_keysStates.TryGetValue((int) e.Key, out lastKeyState))
                {
                    newKeyState.PreviousState = lastKeyState.CurrentState;
                    if (newKeyState.IsKeyPressed && KeyPressedEvent != null)
                        KeyPressedEvent(this, new KeyEventArgs(e.Key, e.State));
                    if (newKeyState.IsKeyReleased && KeyReleasedEvent != null)
                        KeyReleasedEvent(this, new KeyEventArgs(e.Key, e.State));
                }
                else
                {
                    if (KeyPressedEvent != null)
                        KeyPressedEvent(this, new KeyEventArgs(e.Key, e.State));
                }
            }
            catch (Exception exception)
            {
                Logger.Error("Handling keyboard event failed with exception: {0}", exception);
            }
            _keysStates[(int) e.Key] = newKeyState;
        }

        private class KeyState
        {
            public bool IsKeyDown { get { return EvalIsKeyDown(CurrentState); } }
            public bool IsKeyUp { get { return EvalIsKeyUp(CurrentState); } }
            public bool IsKeyPressed { get { return EvalIsKeyUp(PreviousState) && EvalIsKeyDown(CurrentState); } }
            public bool IsKeyReleased { get { return EvalIsKeyDown(PreviousState) && EvalIsKeyUp(CurrentState); } }

            private static bool EvalIsKeyDown(SharpDX.RawInput.KeyState keyState)
            {
                return keyState == SharpDX.RawInput.KeyState.KeyDown ||
                       keyState == SharpDX.RawInput.KeyState.SystemKeyDown;
            }

            private static bool EvalIsKeyUp(SharpDX.RawInput.KeyState keyState)
            {
                return keyState == SharpDX.RawInput.KeyState.KeyUp ||
                       keyState == SharpDX.RawInput.KeyState.SystemKeyUp;
            }

            public SharpDX.RawInput.KeyState CurrentState { get; set; }
            public SharpDX.RawInput.KeyState PreviousState { get; set; }
        }

        private readonly KeysConverter _keysConverter = new KeysConverter();
        private readonly Dictionary<int, KeyState> _keysStates = new Dictionary<int, KeyState>();
    }
}
