using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControllPanel
{
   public class XInputController
    {
        Controller controller;
        Gamepad gamepad;

        string oldState;

        public bool connected = false;
        public int deadband = 4000;

        public Point leftThumb, rightThumb = new Point(0, 0);
        public float leftTrigger, rightTrigger;


        public XInputController()
        {
            controller = new Controller(UserIndex.One);
            connected = controller.IsConnected;
        }

        // Call this method to update all class values
        public bool Update()
        {
            try
            {
                if (!connected)
                    return false;

                State state = controller.GetState();

                gamepad.ToString();


                gamepad = state.Gamepad;

                if (oldState == gamepad.ToString())
                    return false;


                leftThumb.X = (Math.Abs((float)gamepad.LeftThumbX) < deadband) ? 0 : (float)gamepad.LeftThumbX / short.MinValue * -100;
                leftThumb.Y = (Math.Abs((float)gamepad.LeftThumbY) < deadband) ? 0 : (float)gamepad.LeftThumbY / short.MaxValue * 100;
                rightThumb.X = (Math.Abs((float)gamepad.RightThumbX) < deadband) ? 0 : (float)gamepad.RightThumbX / short.MaxValue * 100;
                rightThumb.Y = (Math.Abs((float)gamepad.RightThumbY) < deadband) ? 0 : (float)gamepad.RightThumbY / short.MaxValue * 100;

                leftTrigger = gamepad.LeftTrigger;
                rightTrigger = gamepad.RightTrigger;

                oldState = gamepad.ToString();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
