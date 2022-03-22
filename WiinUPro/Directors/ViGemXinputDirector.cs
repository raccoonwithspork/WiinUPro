using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Nefarius.ViGEm.Client.Targets;
// TODO once need for SCP is removed, replace the ScpControl button/axis enums
using ScpControl;

// change namespace to WiinUPro.Directors?
namespace WiinUPro
{
    public class ViGemXinputDirector
    {
        public const int MAX_INPUT_INSTANCES = 4;

        #region Access
        public static ViGemXinputDirector Access { get; protected set; }

        static ViGemXinputDirector()
        {
            Access = new ViGemXinputDirector();
            Access.Available = true;
        }
        #endregion

        protected ViGEmClient _vgClient;
        protected List<XInputController> _xInstances;

        public bool Available { get; protected set; }
        public int Instances { get { return _xInstances.Count; } }

        // Left motor is the larger one
        public delegate void RumbleChangeDelegate(byte leftMotor, byte rightMotor);

        public ViGemXinputDirector()
        {
            try
            {
                _vgClient = new ViGEmClient();
            }
            catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException)
            {
                Console.WriteLine("Could not create ViGEm client! ViGEm may not be installed!");
                // TODO report error to user (what is available in this app?)
            }
            // TODO initialize _xInstances only as needed?
            // TODO investigate allowing more than 4 devices?
            _xInstances = new List<XInputController>
            {
                new XInputController(_vgClient),
                new XInputController(_vgClient),
                new XInputController(_vgClient),
                new XInputController(_vgClient),
            };
        }

        // TODO add App_Exit (or other destructor?)

        public void SetButton(X360Button button, bool pressed)
        {
            SetButton(button, pressed, XInput_Device.Device_A);
        }

        public void SetButton(X360Button button, bool pressed, XInput_Device device)
        {
            _xInstances[(int)device - 1].SetInput(button, pressed);
        }

        public void SetAxis(X360Axis axis, float value)
        {
            SetAxis(axis, value, XInput_Device.Device_A);
        }

        public void SetAxis(X360Axis axis, float value, XInput_Device device)
        {
            _xInstances[(int)device - 1].SetInput(axis, value);
        }

        /// <summary>
        /// Connects up to the given device.
        /// Ex: If C is used then A, B, & C will be connected.
        /// </summary>
        /// <param name="device">The highest device to be connected.</param>
        /// <returns>If all connections are successful.</returns>
        public bool ConnectDevice(XInput_Device device)
        {
            // TODO simplify
            bool result = _xInstances[(int)device - 1].PluggedIn;

            if (!result)
            {
                result = _xInstances[(int)device - 1].Connect();
            }

            return result;
        }

        /// <summary>
        /// Disconnects down to the given device.
        /// Ex: If A is used then all of the devices will be disconnected.
        /// </summary>
        /// <param name="device">The lowest device to be disconnected.</param>
        /// <returns>If all devices were disconnected</returns>
        public bool DisconnectDevice(XInput_Device device)
        {
            // TODO simplify
            if (!_xInstances[(int)device - 1].PluggedIn)
            {
                return true;
            }
            else
            {
                return _xInstances[(int)device - 1].Disconnect();
            }
        }

        public bool IsConnected(XInput_Device device)
        {
            return _xInstances[(int)device - 1].PluggedIn;
        }

        public void Apply(XInput_Device device)
        {
            _xInstances[(int)device - 1].Update();
        }

        public void ApplyAll()
        {
            foreach (var xDev in _xInstances.ToArray())
            {
                if (xDev.PluggedIn)
                {
                    xDev.Update();
                }
            }
        }

        // TODO is this needed with ViGEm?
        public void SetModifier(int value)
        {
            //XInputBus.Modifier = value;
        }

        public void SubscribeToRumble(XInput_Device device, RumbleChangeDelegate callback)
        {
            _xInstances[(int)device - 1].RumbleEvent += callback;
        }

        public void UnSubscribeToRumble(XInput_Device device, RumbleChangeDelegate callback)
        {
            _xInstances[(int)device - 1].RumbleEvent -= callback;
        }

        // TODO improve name?
        public enum XInput_Device : int
        {
            // TODO change to 0-3?
            Device_A = 1,
            Device_B = 2,
            Device_C = 3,
            Device_D = 4
        }


        public struct XInputState
        {
            public bool A, B, X, Y;
            public bool Up, Down, Left, Right;
            public bool LB, RB, LS, RS;
            public bool Start, Back, Guide;

            public float LX, LY, LT;
            public float RX, RY, RT;

            // TODO improve effeciency by removing need for switch/case
            public bool this[X360Button btn]
            {
                set
                {
                    switch (btn)
                    {
                        case X360Button.A: A = value; break;
                        case X360Button.B: B = value; break;
                        case X360Button.X: X = value; break;
                        case X360Button.Y: Y = value; break;
                        case X360Button.LB: LB = value; break;
                        case X360Button.RB: RB = value; break;
                        case X360Button.LS: LS = value; break;
                        case X360Button.RS: RS = value; break;
                        case X360Button.Up: Up = value; break;
                        case X360Button.Down: Down = value; break;
                        case X360Button.Left: Left = value; break;
                        case X360Button.Right: Right = value; break;
                        case X360Button.Start: Start = value; break;
                        case X360Button.Back: Back = value; break;
                        case X360Button.Guide: Guide = value; break;
                        default: break;
                    }
                }

                get
                {
                    switch (btn)
                    {
                        case X360Button.A: return A;
                        case X360Button.B: return B;
                        case X360Button.X: return X;
                        case X360Button.Y: return Y;
                        case X360Button.LB: return LB;
                        case X360Button.RB: return RB;
                        case X360Button.LS: return LS;
                        case X360Button.RS: return RS;
                        case X360Button.Up: return Up;
                        case X360Button.Down: return Down;
                        case X360Button.Left: return Left;
                        case X360Button.Right: return Right;
                        case X360Button.Start: return Start;
                        case X360Button.Back: return Back;
                        case X360Button.Guide: return Back;
                        default: return false;
                    }
                }
            }

            // TODO improve effeciency by removing need for switch/case
            public float this[X360Axis axis]
            {
                set
                {
                    switch (axis)
                    {
                        case X360Axis.LX_Hi:
                            LX = value;
                            break;
                        case X360Axis.LX_Lo:
                            LX = -value;
                            break;
                        case X360Axis.LY_Hi:
                            LY = value;
                            break;
                        case X360Axis.LY_Lo:
                            LY = -value;
                            break;
                        case X360Axis.LT:
                            LT = value;
                            break;
                        case X360Axis.RX_Hi:
                            RX = value;
                            break;
                        case X360Axis.RX_Lo:
                            RX = -value;
                            break;
                        case X360Axis.RY_Hi:
                            RY = value;
                            break;
                        case X360Axis.RY_Lo:
                            RY = -value;
                            break;
                        case X360Axis.RT:
                            RT = value;
                            break;
                        default:
                            break;
                    }
                }

                get
                {
                    switch (axis)
                    {
                        case X360Axis.LX_Hi:
                        case X360Axis.LX_Lo:
                            return LX;
                        case X360Axis.LY_Hi:
                        case X360Axis.LY_Lo:
                            return LY;
                        case X360Axis.LT:
                            return LT;
                        case X360Axis.RX_Hi:
                        case X360Axis.RX_Lo:
                            return RX;
                        case X360Axis.RY_Hi:
                        case X360Axis.RY_Lo:
                            return RY;
                        case X360Axis.RT:
                            return RT;
                        default:
                            return 0;
                    }
                }
            }

            public void Reset()
            {
                A = B = X = Y = false;
                Up = Down = Left = Right = false;
                LB = RB = LS = RS = false;
                Start = Back = Guide = false;
                LX = LY = LT = 0;
                RX = RY = RT = 0;
            }
        }

        protected class XInputController
        {
            //public static int Modifier;

            protected XInputState _inputs;
            protected IXbox360Controller _controller;

            public event RumbleChangeDelegate RumbleEvent;
            public bool PluggedIn { get; protected set; }

            private float tempLX = -10;
            private float tempLY = -10;
            private float tempRX = -10;
            private float tempRY = -10;

            public XInputController(ViGEmClient client)
            {
                _inputs = new XInputState();
                _controller = client.CreateXbox360Controller();
                _controller.AutoSubmitReport = false;
                _controller.FeedbackReceived += FeedbackReceived;
            }

            public bool Connect()
            {
                if (!PluggedIn)
                {
                    _controller.Connect();
                    PluggedIn = true;
                    Update();
                }

                // return true iff successful
                return PluggedIn;
            }

            public bool Disconnect()
            {
                if (PluggedIn)
                {
                    _controller.Disconnect();
                    PluggedIn = false;
                    // TODO keep rumble on disconnect?
                    RumbleEvent?.Invoke(0, 0);
                }

                // return true iff successful
                return !PluggedIn;
            }

            // TODO remove need to keep separate state and just send button/axis updates directly to the controller?
            public void SetInput(X360Button button, bool state)
            {
                _inputs[button] = state;// || _inputs[button];
            }

            // TODO remove need to keep separate state and just send button/axis updates directly to the controller?
            public void SetInput(X360Axis axis, float value)
            {
                switch (axis)
                {
                    // TODO what are Hi vs Lo used for? do we need the temp* fields?
                    case X360Axis.LX_Hi:
                    case X360Axis.LX_Lo:
                        if (value > tempLX)
                        {
                            tempLX = value;
                            _inputs[axis] = value;
                        }
                        break;

                    case X360Axis.LY_Hi:
                    case X360Axis.LY_Lo:
                        if (value > tempLY)
                        {
                            tempLY = value;
                            _inputs[axis] = value;
                        }
                        break;

                    case X360Axis.RX_Hi:
                    case X360Axis.RX_Lo:
                        if (value > tempRX)
                        {
                            tempRX = value;
                            _inputs[axis] = value;
                        }
                        break;

                    case X360Axis.RY_Hi:
                    case X360Axis.RY_Lo:
                        if (value > tempRY)
                        {
                            tempRY = value;
                            _inputs[axis] = value;
                        }
                        break;

                    default:
                        _inputs[axis] = value;// == 0 ? _inputs[axis] : value;
                        break;
                }
            }

            protected void FeedbackReceived(object sender, Xbox360FeedbackReceivedEventArgs e)
            {
                RumbleEvent?.Invoke(e.LargeMotor, e.SmallMotor);
            }

            public void Update()
            {
                // TODO check if current state is same as previous state and skip sending report if no updates needed?

                if (!PluggedIn)
                {
                    return;
                }

                // TODO do we need these 'temps'? why do they exist? to prevent excessive updates (i.e. consolidate multiple axis updates into one)?
                // reset temps
                tempLX = -10;
                tempLY = -10;
                tempRX = -10;
                tempRY = -10;

                _controller.SetButtonState(Xbox360Button.Start, _inputs.Start);
                _controller.SetButtonState(Xbox360Button.Back, _inputs.Back);
                _controller.SetButtonState(Xbox360Button.Guide, _inputs.Guide);

                _controller.SetButtonState(Xbox360Button.A, _inputs.A);
                _controller.SetButtonState(Xbox360Button.B, _inputs.B);
                _controller.SetButtonState(Xbox360Button.X, _inputs.X);
                _controller.SetButtonState(Xbox360Button.Y, _inputs.Y);

                _controller.SetButtonState(Xbox360Button.Up, _inputs.Up);
                _controller.SetButtonState(Xbox360Button.Down, _inputs.Down);
                _controller.SetButtonState(Xbox360Button.Left, _inputs.Left);
                _controller.SetButtonState(Xbox360Button.Right, _inputs.Right);

                _controller.SetButtonState(Xbox360Button.LeftShoulder, _inputs.LB);
                _controller.SetButtonState(Xbox360Button.RightShoulder, _inputs.RB);

                _controller.SetSliderValue(Xbox360Slider.LeftTrigger, GetRawTrigger(_inputs.LT));
                _controller.SetSliderValue(Xbox360Slider.RightTrigger, GetRawTrigger(_inputs.RT));

                _controller.SetButtonState(Xbox360Button.LeftThumb, _inputs.LS);
                _controller.SetButtonState(Xbox360Button.RightThumb, _inputs.RS);

                _controller.SetAxisValue(Xbox360Axis.LeftThumbX, GetRawAxis(_inputs.LX));
                _controller.SetAxisValue(Xbox360Axis.LeftThumbY, GetRawAxis(_inputs.LY));
                _controller.SetAxisValue(Xbox360Axis.RightThumbX, GetRawAxis(_inputs.RX));
                _controller.SetAxisValue(Xbox360Axis.RightThumbY, GetRawAxis(_inputs.RY));

                _controller.SubmitReport();
            }

            public short GetRawAxis(float axis)
            {
                if (axis > 1.0f)
                {
                    return short.MaxValue;
                }
                else if (axis < -1.0f)
                {
                    return -short.MaxValue;
                }

                return (short)(axis * short.MaxValue);
            }

            public byte GetRawTrigger(float trigger)
            {
                if (trigger > 1.0f)
                {
                    return 0xFF;
                }
                else if (trigger < 0.0f)
                {
                    return 0;
                }

                return (byte)(trigger * 0xFF);
            }
        }

    }
}
