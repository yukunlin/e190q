﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using EARTHLib;
using System.Drawing.Drawing2D;
using System.IO;
using Microsoft.DirectX.DirectInput;
using System.Threading;



namespace DrRobot.JaguarControl
{
    public partial class JaguarCtrl : Form
    {
        # region Form Variables
        DrRobotRobotConnection drRobotConnect = null;
        public RobotConfig robotCfg = null;
        Navigation navigation = null;

        public RobotConfig.RobotConfigTableRow jaguarSetting = null;
        private const string configFile = "c:\\DrRobotAppFile\\OutDoorRobotConfig.xml";
        private const double TEMPERATURE_TH = 60.0;
        public Image testImage;
        double[] robotCornerAngles = { Math.PI / 6, -Math.PI / 6, Math.PI + Math.PI / 6, Math.PI - Math.PI / 6 };
        double[] robotTrackAngles = { Math.PI / 4, Math.PI - Math.PI / 4, Math.PI + Math.PI / 4, -Math.PI / 4 };
        Point[] robotCorners = new Point[4];
        Point[] trackCorners = new Point[4];
        # endregion

        #region Graphics Variables
        GoogleEarth gEarth = new GoogleEarth();
        public double mapResolution;
        public float metersToPixels = 10;
        private double zoomConstant = 2.0;
        private static int paneWidth = 484;
        private static int paneHeight = 415;
        private static int xMin = 11; 
        private static int yMin = 41;
        private static int xMax = paneWidth + xMin; 
        private static int yMax = paneHeight + yMin;
        private static int xCenter = xMin + paneWidth / 2;
        private static int yCenter = yMin + paneHeight / 2;
        private static int constxCenter = xCenter;
        private static int constyCenter = yCenter;

        private static Point oPoint = new Point(xMin, yMin);
        private static Pen blackPen = new Pen(Color.Black, 1);
        private static Pen whitePen = new Pen(Color.White, 10);
        private static Pen thinWhitePen = new Pen(Color.White, 1);
        private static Pen pinkPen = new Pen(Color.White, 2);
        private static Pen goldPen = new Pen(Color.Gold, 1);
        private static Pen greenPen = new Pen(Color.Green,5);
        private static Pen trackPen = new Pen(Brushes.LightGray);
        private static double cellWidth = 1.0; // in meters, mapResolution is in metersToPixels
        #endregion

        #region Motor Variables 
        public class MotorData
        {
            public int pwmOutput = 0;
            public int encodeSpeed = 0;
            public int encoderPos = 0;
            public int encoderDir = 0;
        }

        //private Robot RobotVars = new Robot();
        //here is arm motor array, 0 for front arm, 1 for rear arm
        private MotorData[] armMotor = new MotorData[2];
        private int[] armEncoder = new int[2]{0,0};
        private int[] preArmEncoder = new int[2]{0,0};
        private bool firstEncoderData = true;
        private const int ARM_CIRCLE_CNT = 1140;         // 5*4*285 /5 = 1140
        private double[] armPosAngle = new double[2]{0,0};
        private double[] preArmPos = new double[2]{0,0};
        private int[] armResetPos = new int[2] { 0, 0 };
        private double[] armPosStart = new double[2] { 0, 0 };

        public MotorData leftFrontWheelMotor = new MotorData();
        public MotorData rightFrontWheelMotor = new MotorData();
        public MotorData leftRearWheelMotor = new MotorData();
        public MotorData rightRearWheelMotor = new MotorData();

        private short zeroOutput = 16383;
        private short maxPosOutput = 32767;

        private int forwardPower = 0;
        private int turnPower = 0;

        private double[] resTable = new double[25]{114660,84510,62927,47077,35563,27119,20860,16204,12683,10000,
                        7942,6327,5074,4103,3336,2724,2237,1846,1530,1275,1068,899.3,760.7,645.2,549.4};
        private double[] tempTable = new double[25] { -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80, 85, 90, 95, 100 };
        private const double FULLAD = 4095;

        private double stuckVelTH = 50;
        private double stuckAcc = 0;
        private double stuckPWMTH = 12000;
        private double stuckAccTH = 50;
        #endregion

        #region Joystick Control Variables
        private bool forceStop = false;
        private bool protectMotorTemp = false;
        private bool protectMotorStuck = false;
        private const int NOCONTROL = -32768;
        int armCmd1 = 0;
        int armCmd2 = 0;
        int exeTime = 1000;
        private bool lightOn = false;
        private int expandIO = 0xff;
        private const short LEFTARMCHANNEL = 0;
        private const short RIGHTARMCHANNEL = 1;
        public int forwardVel, turnVel;

        private const short LEFTWHEELCHANNEL = 3;           //3
        private const short RIGHTWHEELCHANNEL = 4;          //4
        public static JoystickState[] joyState = new JoystickState[2]; //maybe we need second Joystick in future
        private static int[] preSetButton = new int[2]{-1,-1};  //no any button pressed
        private static int[] joyButtonCnt = new int[2]{0,0};
        private const int JOYDELAY = 10;
        
        private Device[] applicationDevice = new Device[2];

        private bool blnJoyStick = true;
        private const int MAXPWM = 16384;
        private const int MINPWM = 4000;
        private const int INIPWM = 16384;
        private int MOTDIR = 1;
     
        private const int CMD_INT_TIME = 10;
        private bool armJoy2 = false;
        private bool armJoy1 = false;

        private const short armChannel1 = 0;
        private const short armChannel2 = 1;
        private bool lightCtrl = true;
        private const int ARM_DELAY_CNT = 40;
        private const int ARM_VEL1 = 200;
        private const int ARM_VEL2 = 1000;

        public const int SIMULATOR = 0;
        public const int HARDWARE = 1;
        public int experimentMode = SIMULATOR;
        public int MANUAL = 0;
        public int AUTONOMOUS = 1;

        public int AUTOMODE = 0;
        public int FLYTOPOINT = 0;
        public int TRACKTRAJ = 1;
        public int CIRCLE = 2;
        
        public int controlMode = 0;
        private Thread sensorThread;
        public bool runSensorThread;
        #endregion

        /////////////////////////////////////////////////////////////////
        
        #region Form functions
        
        public JaguarCtrl()
        {
            simulatedJaguar = new AxDDrRobotSentinel_Simulator();
            navigation = new Navigation(this);
            drRobotConnect = new DrRobotRobotConnection(this);
            drRobotConnect.connectRobot();
            robotCfg = drRobotConnect.robotConfig;
            jaguarSetting = (RobotConfig.RobotConfigTableRow)robotCfg.RobotConfigTable.Rows[0];
            InitializeComponent();

            // Setup graphics for simulation
            SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.JaguarCtrl_Paint);
            panelGE.Visible = false;
            mapResolution = trackBarZoom.Value * zoomConstant;

            // Start Simulated Sensor Update Thread
            runSensorThread = true;
            sensorThread = new Thread(runSensorLoop);
            sensorThread.Start();
        }

        public bool Simulating()
        {
            if (experimentMode == SIMULATOR)
                return true;
            else
                return false;
        }

        public void runSensorLoop()
        {
            // Wait
            Thread.Sleep(1000);

            while (runSensorThread)
            {
                if (Simulating())
                {
                    //Update Real Encoder Measurements
                    leftFrontWheelMotor.encoderPos = simulatedJaguar.GetEncoderPulse4();
                    leftFrontWheelMotor.encodeSpeed = simulatedJaguar.GetEncoderSpeed4();
                    leftFrontWheelMotor.encoderDir = simulatedJaguar.GetEncoderDir4();

                    rightFrontWheelMotor.encoderPos = simulatedJaguar.GetEncoderPulse5();
                    rightFrontWheelMotor.encodeSpeed = simulatedJaguar.GetEncoderSpeed5();
                    rightFrontWheelMotor.encoderDir = simulatedJaguar.GetEncoderDir5();

                    UpdateFormEncoderData();
                }
                lock (navigation.thisLock)
                {
                    Animate();
                }
                Thread.Sleep(50);
            }
        }

        private void JaguarCtrl_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
        }

        private void Animate()
        {
            double xToUse = navigation._x;
            double yToUse = navigation._y;
            double thetaToUse = navigation._theta;

            if (!Simulating())
            {
                xToUse = navigation.x_est;
                yToUse = navigation.y_est;
                thetaToUse = navigation.t_est;
            }


            xCenter = (int)(constxCenter - xToUse * mapResolution);
            yCenter = (int)(constyCenter + yToUse * mapResolution);
            


            /*
            int yMinS = (int) (yMin - navigation.y_est*mapResolution);
            int yMaxS = (int) (yMax - navigation.y_est*mapResolution);

            int xMinS = (int) (xMin + navigation.x_est*mapResolution);
            int xMaxS = (int) (xMax + navigation.x_est*mapResolution);

            */

            int yMinS = (int) (yMin );
            int yMaxS = (int) (yMax );
            int xMinS = (int) (xMin );
            int xMaxS = (int) (xMax );
            // Create bitmap to write to            
            Bitmap gBuffer= new Bitmap(paneWidth, paneHeight);
            using (Graphics g = Graphics.FromImage(gBuffer))
            {
                // Smooth?
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.SmoothingMode = SmoothingMode.HighQuality;

                // Paint background of bitmap
                g.FillRectangle(Brushes.Black, new Rectangle(xMin, yMin, paneWidth, paneHeight));

                // Add Grid
                int numXLines = (int)(0.5 * paneWidth / (mapResolution * cellWidth)) + 1;
                int numYLines = (int)(0.5 * paneHeight / (mapResolution * cellWidth)) + 1;
                for (int i = 0; i < numXLines; i++)
                {
                    float Xp = (float)(xCenter + i * mapResolution * cellWidth);
                    float Xm = (float)(xCenter - i * mapResolution * cellWidth);
                    g.DrawLine(goldPen, Xp, yMinS, Xp, yMaxS);
                    g.DrawLine(goldPen, Xm, yMinS, Xm, yMaxS);
                }
                for (int i = 0; i < numYLines; i++)
                {
                    float Yp = (float)(yCenter + i * mapResolution * cellWidth);
                    float Ym = (float)(yCenter - i * mapResolution * cellWidth);
                    g.DrawLine(goldPen, xMinS, Yp, xMaxS, Yp);
                    g.DrawLine(goldPen, xMinS, Ym, xMaxS, Ym);
                }

                // Draw walls
                int sizeWall = navigation.map.numMapSegments;
                for (int i = 0; i < sizeWall; i++)
                {
                    double x1 = navigation.map.mapSegmentCorners[i, 0, 0];
                    double y1 = navigation.map.mapSegmentCorners[i, 0, 1];
                    double x2 = navigation.map.mapSegmentCorners[i, 1, 0];
                    double y2 = navigation.map.mapSegmentCorners[i, 1, 1];

                    int x1s = (int) Math.Round(xCenter + x1*mapResolution);
                    int y1s = (int) Math.Round(yCenter - y1*mapResolution);
                    int x2s = (int) Math.Round(xCenter + x2*mapResolution);
                    int y2s = (int) Math.Round(yCenter - y2*mapResolution);
                    g.DrawLine(greenPen, x1s, y1s, x2s, y2s);
                }

                // Draw Axis
                g.DrawLine(thinWhitePen, xCenter, yCenter, (float)(xCenter + 1.5 * mapResolution), yCenter);
                g.DrawLine(thinWhitePen, (float)(xCenter + 1.4 * mapResolution), (float)(yCenter + 0.1 * mapResolution),
                    (float)(xCenter + 1.5 * mapResolution), (float)(yCenter + 0.0 * mapResolution));
                g.DrawLine(thinWhitePen, (float)(xCenter + 1.4 * mapResolution), (float)(yCenter - 0.1 * mapResolution),
                    (float)(xCenter + 1.5 * mapResolution), (float)(yCenter + 0.0 * mapResolution));

                
                g.DrawLine(thinWhitePen, xCenter, yCenter, xCenter, (float)(yCenter - 1.5 * mapResolution));
                g.DrawLine(thinWhitePen, (float)(xCenter - 0.1 * mapResolution), (float)(yCenter - 1.4 * mapResolution),
                    (float)(xCenter + 0.0 * mapResolution), (float)(yCenter - 1.5 * mapResolution));
                g.DrawLine(thinWhitePen, (float)(xCenter + 0.1 * mapResolution), (float)(yCenter - 1.4 * mapResolution),
                    (float)(xCenter + 0.0 * mapResolution), (float)(yCenter - 1.5 * mapResolution));

                // UPDATE WITH PF X_EST, Y_EST?????????????????????????????????????????
                // Draw Robot
                int xShift = (int)(mapResolution * xToUse);
                int yShift = (int)(mapResolution * yToUse);

                // Draw laser range finder
                double startAngle = startAng - Math.PI/2;
                var listOfXs = new List<double>();
                var listOfYs = new List<double>();

                for (int i = 0; i < navigation.LaserData.Length; i = i + navigation.pf.SENSORSTEP)
                {
                    var angle = startAngle + i*stepAng;
                    double distance = navigation.LaserData[i];
                    if (distance < 250)
                        distance = Map.MAXLASERDISTANCE;
                    else
                        distance /= 1000.0;

                    listOfXs.Add(distance * Math.Cos(angle) * mapResolution);
                    listOfYs.Add(distance * Math.Sin(angle) * mapResolution);
                }

                var heading = thetaToUse;
                // rotate end points by robot heading
                for (int i = 0; i < listOfXs.Count; i++)
                {
                    var newX = listOfXs[i]*Math.Cos(heading) - listOfYs[i]*Math.Sin(heading);
                    var newY = listOfXs[i]*Math.Sin(heading) + listOfYs[i]*Math.Cos(heading);

                    g.DrawLine(pinkPen, (int)(xCenter + xShift),        (int)(yCenter - yShift),
                                        (int)(xCenter + xShift + newX), (int)(yCenter - newY - yShift) );

                }

                double robotDiagnol = 0.25 * mapResolution;
                for (int i = 0; i < 4; i++)
                {
                    robotCorners[i].X = (int)(xCenter + xShift + robotDiagnol * Math.Cos(thetaToUse + robotCornerAngles[i]));
                    robotCorners[i].Y = (int)(yCenter - yShift - robotDiagnol * Math.Sin(thetaToUse + robotCornerAngles[i]));
                }

                g.FillPolygon(Brushes.DarkSlateGray, robotCorners);
            
                // Draw Tracks
                double trackDiagnol = 0.35 * mapResolution;
                trackPen.Width = (int)(8 * mapResolution / 100);
            
                for (int i = 0; i < 2; i++)
                {
                    trackCorners[2 * i].X = (int)(xCenter + xShift + robotDiagnol * Math.Cos(thetaToUse + robotTrackAngles[2 * i]));
                    trackCorners[2 * i].Y = (int)(yCenter - yShift - robotDiagnol * Math.Sin(thetaToUse + robotTrackAngles[2 * i]));
                    trackCorners[2 * i+1].X = (int)(xCenter + xShift + robotDiagnol * Math.Cos(thetaToUse + robotTrackAngles[2 * i+1]));
                    trackCorners[2 * i+1].Y = (int)(yCenter - yShift - robotDiagnol * Math.Sin(thetaToUse + robotTrackAngles[2 * i+1]));
                    g.DrawLine(trackPen, trackCorners[2 * i], trackCorners[2 * i + 1]);
                }

                // Draw Laser on top
                double laserDiagonal = 0.15*mapResolution;
                int laserDiameter = (int)(0.08*mapResolution);
                int X_laser = (int)(xCenter + xShift + laserDiagonal * Math.Cos(thetaToUse) - laserDiameter / 2);
                int Y_laser = (int)(yCenter - yShift - laserDiagonal * Math.Sin(thetaToUse) - laserDiameter / 2);
                g.FillEllipse(Brushes.LightGray, X_laser, Y_laser, laserDiameter, laserDiameter);


                // Draw particles
                double avgWeights = 1.0 / navigation.numParticles;

                foreach (var particle in navigation.pf.particles)
                {
                    int xPar = (int) (xCenter + particle.x*mapResolution);
                    int yPar = (int) (yCenter - particle.y*mapResolution);
                    int xLine = (int) (xCenter + (0.1*Math.Cos(particle.t) + particle.x)*mapResolution);
                    int yLine = (int) (yCenter - (0.1*Math.Sin(particle.t) + particle.y)*mapResolution);

                    double radius = 0.04;

                    int xCorner = (int) (xCenter + (particle.x - radius) *mapResolution);
                    int yCorner = (int) (yCenter - (particle.y + radius)*mapResolution);


                    // TODO: CHECK COLORING SCHEME
                    int blueN = (int) Math.Min(avgWeights/particle.w * 20, 255) ;

                    var colors = Color.FromArgb(255-blueN/2, 0, blueN);
                    var pen = new Pen(colors, 2);
                    var brush = new SolidBrush(colors);

                    g.DrawLine(pen, xLine, yLine, xPar, yPar);
                    g.FillEllipse(brush, xCorner, yCorner, (int) (2*radius*mapResolution), (int) (2*radius*mapResolution));
                }

                // Draw state estimate
                double[] stateEst = navigation.pf.EstimatedState();
                double xEst = stateEst[0];
                double yEst = stateEst[1];
                double tEst = stateEst[2];

                int xParEst = (int)(xCenter + xEst * mapResolution);
                int yParEst = (int)(yCenter - yEst * mapResolution);
                int xLineEst = (int)(xCenter + (0.3 * Math.Cos(tEst) + xEst) * mapResolution);
                int yLineEst = (int)(yCenter - (0.3 * Math.Sin(tEst) + yEst) * mapResolution);

                double radEst = 0.1;

                int xCorEst = (int)(xCenter + (xEst - radEst) * mapResolution);
                int yCorEst = (int)(yCenter - (yEst + radEst) * mapResolution);

                var colorsEst = Color.GreenYellow;
                var penEst = new Pen(colorsEst, 3);

                Pen drawMag = new Pen(Color.GreenYellow);
                g.DrawLine(penEst, xLineEst, yLineEst, xParEst, yParEst);
                g.DrawEllipse(penEst, xCorEst, yCorEst, (int)(2 * radEst * mapResolution), (int)(2 * radEst * mapResolution));



                // Draw the bitmap to the form
                this.CreateGraphics().DrawImageUnscaled(gBuffer, 0, 0);
            }
        }
        

        private void JaguarCtrl_Shown(object sender, EventArgs e)
        {
            drRobotConnect.Hide ();
            this.Focus();
        }

        private void JaguarCtrl_FormClosed(object sender, FormClosedEventArgs e)
        {
            myAMC.Stop();
            try
            {
                if (drRobotConnect != null)
                    drRobotConnect.Close();
            }
            catch{}
        }

        private void JaguarCtrl_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 2; i++)
            {
                armMotor[i] = new MotorData();
            }
            realJaguar.connectRobot (jaguarSetting.RobotID);

            //Initialize Robot Variables and Google Earth
            //gEarth.Initialize(this);

            if (Simulating())
                checkBoxHardware.Checked = false;
            else
                checkBoxHardware.Checked = true;
        
            //for Axis camera
            try
            {
                // Set properties, deciding what url completion to use by MediaType.
                myAMC.MediaUsername = jaguarSetting.CameraUser;
                myAMC.MediaPassword = jaguarSetting.CameraPWD;
                myAMC.MediaType = "mjpeg";
                myAMC.MediaURL = CompleteURL(jaguarSetting.CameraIP + ":" + jaguarSetting .CameraPort , "mjpeg");
                myAMC.AudioConfigURL = "http://" + jaguarSetting.CameraIP + ":" + jaguarSetting .CameraPort + "/axis-cgi/view/param.cgi?camera=1&action=list&group=Audio,AudioSource.A0";
                myAMC.ToolbarConfiguration = "default, + audiocontrol,+ rec";
                myAMC.AudioReceiveStop();
 
                // Start the streaming
                myAMC.Play();
                myAMC.AudioReceiveURL = "http://" + jaguarSetting.CameraIP + ":" + jaguarSetting .CameraPort + "/axis-cgi/audio/receive.cgi";
                myAMC.Volume = 0;

                //myAMC.AudioReceiveStart();
                myAMC.AudioTransmitURL = "http://" + jaguarSetting.CameraIP + ":" + jaguarSetting.CameraPort + "/axis-cgi/audio/transmit.cgi";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to play stream: " + ex.Message);
            }
            //for joystick control
            if (InitDirectInput())
                tmrJoyStickPoll.Enabled = true;

            //for GPS/IMU communication
            if ((jaguarSetting.IMUIP !="0.0.0.0") && (jaguarSetting .GPSIP != "0.0.0.0"))
                startComm ();

            // for laser scan
            if ((jaguarSetting.LaserRangeIP != "0.0.0.0") && (jaguarSetting.LaserRangePort != 0))
            {
                startCommLaser();
                btnLaserScan.Enabled = true;
            }
            else
                btnLaserScan.Enabled = false;

            drawLaserBackground();
            MOTDIR = jaguarSetting .MotorDir;
            btnDisArm.Visible = false;
            pictureBoxStuckDetect.BackColor = Color.Green;
            btnScan.Enabled = false;
        }

        private void JaguarCtrl_FormClosing(object sender, FormClosingEventArgs e)
        {

            //kill the navigation thread
            runSensorThread = false;
            sensorThread.Abort();

            //kill the connection form
            drRobotConnect.DrRobotRobotConnection_Kill(); 
            drRobotConnect.Close();

            //kill the navigation thread
            navigation.runThread = false;

            tmrDisplay.Enabled = false;
            tmrDrawing.Enabled = false;
            tmrJoyStickPoll.Enabled = false;
            stopCommunication();
            stopCommunicationLaser();
            if ((startRecord) || (SW != null))
                SW.Close();

            //we save google earth start point here
            //gEarth.Close(this, robotCfg, configFile);
            //Thread.Sleep(1000);
            
        }
        #endregion

        #region Axis Camera Control
        string CompleteURL(string theMediaURL, string theMediaType)
        {
            string anURL = theMediaURL;
            if (!anURL.EndsWith("/")) anURL += "/";

            if (theMediaType == "mjpeg")
            {
                anURL += "axis-cgi/mjpg/video.cgi";
            }
            else if (theMediaType == "mpeg4")
            {
                anURL += "mpeg4/media.amp";
            }
            else if (theMediaType == "mpeg2-unicast")
            {
                anURL += "axis-cgi/mpeg2/video.cgi";
            }
            else if (theMediaType == "mpeg2-multicast")
            {
                anURL += "axis-cgi/mpeg2/video.cgi";
            }

            anURL = CompleteProtocol(anURL, theMediaType);
            return anURL;
        }

        string CompleteProtocol(string theMediaURL, string theMediaType)
        {
            if (theMediaURL.IndexOf("://") >= 0) return theMediaURL;

            string anURL = theMediaURL;

            if (theMediaType == "mjpeg")
            {
                anURL = "http://" + anURL;
            }
            else if (theMediaType == "mpeg4")
            {
                anURL = "axrtsphttp://" + anURL;
            }
            else if (theMediaType == "mpeg2-unicast")
            {
                anURL = "http://" + anURL;
            }
            else if (theMediaType == "mpeg2-multicast")
            {
                anURL = "axsdp" + anURL;
            }

            return anURL;
        }

        #endregion

        #region GoogleEarth function

        //this is a "Look At" function
        private void btnSetMapCenter_Click(object sender, EventArgs e)
        {
            navigation._trajX.AddLast(double.Parse(txtStartLat.Text));
            navigation._trajY.AddLast(double.Parse(txtStartLong.Text));
            navigation._trajT.AddLast(double.Parse(txtStartTheta.Text));
        }
        
        private void trackBarZoom_Scroll(object sender, EventArgs e) 
        {
            // TrackBarZoom ranges from 1 to 100
            // MapResolution is in pixels / meters
            // Lets limit between 5x5 meters to 500x500 meters
            // so lets multiply by 5
            mapResolution = trackBarZoom.Value*zoomConstant;
            this.Invalidate();
        }
 
        #endregion

        #region Real Jaguar ActiveX event
        
        
        private void realJaguar_StandardSensorEvent(object sender, EventArgs e)
        {
            double vol = (double)realJaguar.GetSensorBatteryAD2() / 4095 * 34.498;
            
            //to protect Li-Po Battery, you'd better shut down when battery is lower than 22.2V (3.7V*6)
            if (vol < 22.2)
                lblBatVol.ForeColor = Color.Red;
            else
                lblBatVol.ForeColor = Color.Black;
            lblBatVol.Text = vol.ToString("0.00");

            //here is the board voltage 5V, not display on the UI
            vol = (double)realJaguar.GetSensorBatteryAD1() / 4095 * 9;
            
        }

        private void realJaguar_MotorSensorEvent(object sender, EventArgs e)
        {
            forwardPower  = realJaguar.GetMotorPWMValue4();   //actually is forward PWM power
            turnPower = realJaguar.GetMotorPWMValue5();  //actually is tunning PWM power
        }

        delegate void SetTextDelegate(string encol, string encor, string vell, string velr);

        private void UpdateFormEncoderData(string encol, string encor, string vell, string velr)
        {
            lblEncoderPos1.Text = leftFrontWheelMotor.encoderPos.ToString();    // 
            lblEncoderPos2.Text = rightFrontWheelMotor.encoderPos.ToString();    // 
            lblVel1.Text = (MOTDIR * leftFrontWheelMotor.encodeSpeed * leftFrontWheelMotor.encoderDir).ToString();
            lblVel2.Text = (-MOTDIR * rightFrontWheelMotor.encodeSpeed * rightFrontWheelMotor.encoderDir).ToString();
            lblEncoderPos4.Text = navigation._x.ToString();
            lblVel4.Text = navigation._y.ToString();
            lblTemp4.Text = navigation._theta.ToString();
        }

        private void UpdateFormEncoderData()
        {
            string encl = leftFrontWheelMotor.encoderPos.ToString();
            string encr = rightFrontWheelMotor.encoderPos.ToString();
            string vell = (MOTDIR * leftFrontWheelMotor.encodeSpeed * leftFrontWheelMotor.encoderDir).ToString();
            string velr = (-MOTDIR * rightFrontWheelMotor.encodeSpeed * rightFrontWheelMotor.encoderDir).ToString();

            if (InvokeRequired)
            {
                Invoke(new SetTextDelegate(UpdateFormEncoderData), encl, encr, vell, velr);
            }
            else
            {
                lblEncoderPos1.Text = encl;    // 
                lblEncoderPos2.Text = encr;    // 
                lblVel1.Text = vell;
                lblVel2.Text = velr;
                lblEncoderPos4.Text = navigation._x.ToString();
                lblVel4.Text = navigation._y.ToString();
                lblTemp4.Text = navigation._theta.ToString();
            }
        }

        private void realJaguar_CustomSensorEvent(object sender, EventArgs e)
        {
            //here is temperature detect
            // For a motor, if output PWM is very big but encoder speed is very low or zero, it usually means the motor 
            // works in a stuck state, the current will be very big, the motor will be damaged in a moment, so please be 
            // very carefull at these information, especialy if temperature of a motor is getting much higher(above 60 degree), 
            // stop the motor for a while to protect the motor

            // now the temperature is connect to Extended AD port

            double tempM1 = Trans2Temperature((double)realJaguar.GetCustomAD5());
            tempM1 = double.Parse(tempM1.ToString("0.00"));

            double tempM2 = Trans2Temperature((double)realJaguar.GetCustomAD6());
            tempM2 = double.Parse(tempM2.ToString("0.00"));

            double tempM3 = Trans2Temperature((double)realJaguar.GetCustomAD7());
            tempM3 = double.Parse(tempM3.ToString("0.00"));

            double tempM4 = Trans2Temperature((double)realJaguar.GetCustomAD8());
            tempM4 = double.Parse(tempM4.ToString("0.00"));

            //Update Real Encoder Measurements
            if (!Simulating())
            {
                leftFrontWheelMotor.encoderPos = realJaguar.GetEncoderPulse4();
                leftFrontWheelMotor.encodeSpeed = realJaguar.GetEncoderSpeed4();
                leftFrontWheelMotor.encoderDir = realJaguar.GetEncoderDir4();

                rightFrontWheelMotor.encoderPos = realJaguar.GetEncoderPulse5();
                rightFrontWheelMotor.encodeSpeed = realJaguar.GetEncoderSpeed5();
                rightFrontWheelMotor.encoderDir = realJaguar.GetEncoderDir5();
                UpdateFormEncoderData();
            }

            
            //here can read back left wheel motor and right wheel motor encoder information
            if (checkBoxMotorProtect.Checked)
            {
                if ((tempM1 > TEMPERATURE_TH) || (tempM2 > TEMPERATURE_TH) )
                    protectMotorTemp = true;
                else
                    protectMotorTemp = false;
            }
            else
                protectMotorTemp = false;

            if (tempM1 > TEMPERATURE_TH)
                lblTemp1.ForeColor = Color.Red;
            else
                lblTemp1.ForeColor = Color.Black;
            if (tempM2 > TEMPERATURE_TH)
                lblTemp2.ForeColor = Color.Red;
            else
                lblTemp2.ForeColor = Color.Black;

            // Update form with encoder data
            lblTemp1.Text = tempM1.ToString("0.00");
            lblTemp2.Text = tempM2.ToString("0.00");
            
           
            //stuck detect here
            if ((Math.Abs(forwardPower - 16384) > stuckPWMTH) || (Math.Abs(turnPower - 16384) > stuckPWMTH))
            {
                if (((Math.Abs(leftFrontWheelMotor.encodeSpeed) < stuckVelTH))
                || ((Math.Abs(rightFrontWheelMotor.encodeSpeed) < stuckVelTH)))
                    stuckAcc++;
                else
                {
                    stuckAcc--;
                    if (stuckAcc < 0) stuckAcc = 0;
                }
            }
            else
            {
                stuckAcc--;
                if (stuckAcc < 0) stuckAcc = 0;
            }
            if (stuckAcc >= stuckAccTH)
            {
                stuckAcc = stuckAccTH;
                pictureBoxStuckDetect.BackColor = Color.Red;
                if (checkBoxMotorProtect.Checked)
                    protectMotorStuck = true;
            }
            else if (stuckAcc <= (stuckAccTH - 50))
            {
                pictureBoxStuckDetect.BackColor = Color.Green;
                if (checkBoxMotorProtect.Checked)
                {
                    protectMotorStuck = false ;
                }
            }

            //here we record encoder with GPS/IMU data
            string recTemp = "#M,";
            if (startRecord)
            {
                recTemp += leftFrontWheelMotor.encoderPos.ToString() + "," + rightFrontWheelMotor.encoderPos.ToString();
                SW.WriteLine(recTemp);
                recordCnt++;
                if (recordCnt > MAXFILELEN)
                {
                    recordCnt = 0;
                    SW.Close();

                    //open next file
                    SW = File.CreateText(fileNme + fileCnt.ToString() + ".txt");
                    fileCnt++;
                }
            }
        }

        private double Trans2Temperature(double adValue)
        {
            //for new temperature sensor
            double tempM = 0;
            double k = (adValue / FULLAD);
            double resValue = 0;
            if (k != 1)
                resValue = 10000 * k / (1 - k);      //AD value to resistor
            else
                resValue = resTable[0];


            int index = -1;
            if (resValue >= resTable[0])       //too lower
                tempM = -20;
            else if (resValue <= resTable[24])
                tempM = 100;
            else
            {
                for (int i = 0; i < 24; i++)
                {
                    if ((resValue <= resTable[i]) && (resValue >= resTable[i + 1]))
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                    tempM = tempTable[index] + (resValue - resTable[index]) / (resTable[index + 1] - resTable[index]) * (tempTable[index + 1] - tempTable[index]);
                else
                    tempM = 0;
            }
            return tempM;
        }

        #endregion

        #region Joystick control
        private bool InitDirectInput()
        {
            // Enumerate joysticks in the system.
            int i = 0;
            foreach (DeviceInstance instance in Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
            {
                // Create the device.  Just pick the first one
                applicationDevice[i] = new Device(instance.InstanceGuid);
                i++;
            }

            if ( (null == applicationDevice[0]))
            {
                // MessageBox.Show("Unable to create a joystick device.", "No joystick found");
                return false;
            }

            i = 0;
            // Set the data format to the c_dfDIJoystick pre-defined format.
            applicationDevice[i].SetDataFormat(DeviceDataFormat.Joystick);
            // Set the cooperative level for the device.
            applicationDevice[i].SetCooperativeLevel(this, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
            // Enumerate all the objects on the device.
            foreach (DeviceObjectInstance d in applicationDevice[i].Objects)
            {
                // For axes that are returned, set the DIPROP_RANGE property for the
                // enumerated axis in order to scale min/max values.

                if ((0 != (d.ObjectId & (int)DeviceObjectTypeFlags.Axis)))
                {
                    // Set the range for the axis.
                    applicationDevice[i].Properties.SetRange(ParameterHow.ById, d.ObjectId, new InputRange(0, +10000));
                }
            }
            
            
            return true;
        }

        private void GetData()
        {
            //first device for Arm control, second for wheel control
            // Make sure there is a valid device.
            
                if (null == applicationDevice[0])
                    return;
                try
                {
                    // Poll the device for info.
                    applicationDevice[0].Poll();
                }
                catch (InputException inputex)
                {
                    if ((inputex is NotAcquiredException) || (inputex is InputLostException))
                    {
                        // Check to see if either the app
                        // needs to acquire the device, or
                        // if the app lost the device to another
                        // process.
                        try
                        {
                            // Acquire the device.
                            applicationDevice[0].Acquire();
                        }
                        catch (InputException)
                        {
                            // Failed to acquire the device.
                            // This could be because the app
                            // doesn't have focus.
                            realJaguar.DcMotorPwmNonTimeCtrAll(INIPWM, INIPWM, NOCONTROL, INIPWM , INIPWM , NOCONTROL);
                            return;
                        }
                    }

                } //catch(InputException inputex)

                // Get the state of the device.
                try 
                { 
                    joyState[0] = applicationDevice[0].CurrentJoystickState; }
                    // Catch any exceptions. None will be handled here, 
                    // any device re-aquisition will be handled above.  
                    // this is a place we can add control code -wf
                catch (InputException)
                {
                    realJaguar.DcMotorPwmNonTimeCtrAll(INIPWM, INIPWM, NOCONTROL, INIPWM , INIPWM , NOCONTROL);
                    return;
                }

                if (blnJoyStick)
                {  
                    int[] pov = joyState[0].GetPointOfView();
                    if (pov[0] == 9000)
                    {
                        // show axis camera in big panel
                        SetCameraSize(true);
                    }
                    else if (pov[0] == 27000)
                    {
                        //axis camera original size
                        SetCameraSize(false);
                    }
                    else if (pov[0] == 18000)
                    {
                        if (lightCtrl)
                        {
                            lightCtrl = false;
                            SetFrontLight();
                        }
                    }
                    else if (pov[0] == 0)
                        SetIniPos();
                    else
                        lightCtrl = true;
 
                    if ((!protectMotorTemp) && (!protectMotorStuck ))
                        UpdateWheelControl(); //consider also slowing wheels here
                    else
                    {
                        //stop first
                        realJaguar.DcMotorPwmNonTimeCtrAll(INIPWM, INIPWM, NOCONTROL, INIPWM, INIPWM, NOCONTROL);
                    }
                    
                    if (!forceStop) UpdateArmControl();
                }
        }

        private void UpdateWheelControl()
        {
            
            int x = (joyState[0].Y - 5000);
            int y = -(joyState[0].Z- 5000);

            double forwardP = 0;
            double turnP = 0;
            if  (( Math.Abs(x) > 1000) || (Math.Abs(y) > 1000))
            {
                forwardP = (double)x / 5000 * 100;
                if ((x > 0) && (Math.Abs(x) > 300))
                    y = -y;
                
                turnP = (double)y / 5000 * 100;
                trackBarForwardPower.Value = (int)(forwardP);
                trackBarTurnPower.Value = (int)(turnP);
            }
            else
            {
                controlMode = MANUAL;
                trackBarForwardPower.Value = 0;
                trackBarTurnPower.Value = 0;
                realJaguar.DcMotorPwmNonTimeCtrAll(NOCONTROL, NOCONTROL, NOCONTROL, INIPWM, INIPWM, NOCONTROL);
            }
           
        }
        
        private void UpdateArmControl()
        {
        }
        
        private void tmrJoyStickPoll_Tick(object sender, EventArgs e)
        {
            GetData();
        }

        #endregion

        #region TrackBar control
        private void trackBarForwardPower_ValueChanged(object sender, EventArgs e)
        {
            controlMode = MANUAL;
            trackBarTurnPower.Value = 0;
            forwardVel = 0;
            if ((!protectMotorTemp) &&(!protectMotorStuck))
            {
                forwardVel = zeroOutput + (MOTDIR * trackBarForwardPower.Value) * (maxPosOutput - zeroOutput) / 100;
                forwardVel = Math.Min(maxPosOutput, Math.Max(0, forwardVel));
            }
            else
                forwardVel = zeroOutput;

            
            // seems a minimum of 16 is needed for forwardVel to move.
            if (Simulating())
                simulatedJaguar.DcMotorPwmNonTimeCtrAll(0, 0, 0, (short)forwardVel, (short)(zeroOutput - (forwardVel-zeroOutput)), 0);
            else
            {
                Console.WriteLine(forwardVel);
                realJaguar.DcMotorPwmNonTimeCtrAll(0, 0, 0, (short)forwardVel, (short)(zeroOutput - (forwardVel - zeroOutput)), 0);
            }
        }

        private void trackBarTurnPower_ValueChanged(object sender, EventArgs e)
        {
            controlMode = MANUAL;
            trackBarForwardPower.Value = 0;
            turnVel = 0;

            if ((!protectMotorTemp) && (!protectMotorStuck))
            {
                turnVel = zeroOutput + (MOTDIR*trackBarTurnPower.Value)*(maxPosOutput - zeroOutput)/100;
                turnVel = Math.Min(maxPosOutput, Math.Max(0, turnVel));
            }
            else
                turnVel = zeroOutput;

            if (Simulating())
                simulatedJaguar.DcMotorPwmNonTimeCtrAll(0, 0, 0, (short) turnVel, (short) turnVel, 0);
            else
            {
                Console.WriteLine(turnVel);
                realJaguar.DcMotorPwmNonTimeCtrAll(0, 0, 0, (short)turnVel, (short)turnVel, 0);
            }
    }


 

        private void SetFrontLight()
        {
            if (lightOn)
            {
                expandIO = expandIO & 0x7f;     //bit 7 turn off lights
                lightOn = false;
                realJaguar.SetCustomDOUT((short)expandIO);
            }
            else
            {
                expandIO = expandIO | 0x80;     //bit 7 turn on lights
                lightOn = true;
                realJaguar.SetCustomDOUT((short)expandIO);
            }
        }

        private void SetIniPos()
        {
            preArmPos[0] = 0;
            preArmPos[1] = 0;
            preArmEncoder[0] = armEncoder[0];
            armResetPos[0] = armEncoder[0];
            armResetPos[1] = armEncoder[1];
            firstEncoderData = true;
            //armPosIndicator.SetArmPositionIndicatorIni(0,0);
        }

        private void SetCameraSize(bool setting)
        {
            if (setting)
            {
                //big size
                panel1.Top = 3;
                panel1.Left = 4;
                panel1.Width = 1022;
                panel1.Height = 730;
                panel1.BringToFront();
                myAMC.Width = 1022;
                myAMC.Height = 730;
                myAMC.BringToFront();
            }
            else
            {
                //original size
                panel1.Top = 3;
                panel1.Left = 545;
                panel1.Width = 462;
                panel1.Height = 378;
                panel1.BringToFront();
                myAMC.Width = 460;
                myAMC.Height = 372;
                myAMC.BringToFront();
            }
        }

        #endregion

        # region Button Clicks
        private void btnLaserScan_Click(object sender, EventArgs e)
        {
            if (btnLaserScan.Text == "LaserScan")
            {
                btnLaserScan.Text = "Camera";
                myAMC.SendToBack();
                pictureBoxSensor.BringToFront();
                pictureBoxLaser.BringToFront();
                btnScan.BringToFront();
                btnTurnOn.BringToFront();
            }
            else
            {
                btnLaserScan.Text = "LaserScan";
                myAMC.BringToFront();
                pictureBoxSensor.SendToBack();
                pictureBoxLaser.SendToBack();
                btnScan.SendToBack();
                btnTurnOn.SendToBack();
            }
        }

        private void tmrDisplay_Tick(object sender, EventArgs e)
        {
            lblCOG.Text = gpsRecord.cog.ToString();
            lblVOG.Text = gpsRecord.vog .ToString("0.00");

            if (gpsRecord.qi == 0)
            {
                lblGPSQI.Text = "Fix not available";
            }
            else if (gpsRecord.qi == 1)
            {
                lblGPSQI.Text = "Non DGPS fix available";
            }
            else if (gpsRecord.qi == 2)
            {
                lblGPSQI.Text = "DGPS fix available";
            }
            else if (gpsRecord.qi == 6)
            {
                lblGPSQI.Text = "Estimate";
            }
            else
            {
                lblGPSQI.Text = "Fix not available";
            }


            int dataLen = 0;
            if (drawEndPoint != drawStartPoint)
            {
                if (drawEndPoint > drawStartPoint)
                    dataLen = drawEndPoint - drawStartPoint;
                else
                    dataLen = DrawDataLen;

                DrawDataPic(pictureBoxAccelX, "Accel_X", Color.Crimson, draw_AccelX, drawStartPoint, drawEndPoint, dataLen, 512);
                DrawDataPic(pictureBoxAccelY, "Accel_Y", Color.Fuchsia, draw_AccelY, drawStartPoint, drawEndPoint, dataLen, 512);
                DrawDataPic(pictureBoxAccelZ, "Accel_Z", Color.DeepSkyBlue, draw_AccelZ, drawStartPoint, drawEndPoint, dataLen, 512);

                DrawDataPic(pictureBoxGyroX, "Pitch", Color.Lime, draw_GyroY, drawStartPoint, drawEndPoint, dataLen, 16384);    //actual the data is 2^16 = 65536
                DrawDataPic(pictureBoxGyroY, "Roll", Color.DeepPink, draw_GyroX, drawStartPoint, drawEndPoint, dataLen, 16384);
                DrawDataPic(pictureBoxGyroZ, "Yaw", Color.Blue, draw_GyroZ, drawStartPoint, drawEndPoint, dataLen, 16384);
            }
        }

        private void DrawDataPic(PictureBox picCtrl, string dataName, Color penColor, double[] data, int startPos, int endPos, int length, int maxData)
        {
            Bitmap bmp = new Bitmap(picCtrl.Width, picCtrl.Height);
            Graphics g = Graphics.FromImage(bmp);

            int width = bmp.Width - 4;
            int height = bmp.Height - 4;
            Pen drawAxisPen = new Pen(Color.Black);
            Pen drawDataPen = new Pen(penColor);

            g.Clear(System.Drawing.Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            GraphicsPath path = new GraphicsPath();

            g.DrawLine(drawAxisPen, 0, height / 2, width, height / 2);
            int x = 0;
            int y = 0;
            int lastX = 0;
            int lastY = height / 2;
            double scaleY = ((double)height / 2) / maxData;
            double scaleX = (double)width / data.Length;
            int dataPos = 0;

            for (int i = 0; i < length; i++)
            {
                x = (int)(i * scaleX);

                dataPos = i + startPos;
                if (dataPos >= length)
                {
                    dataPos = dataPos - length;
                }
                y = (int)(data[dataPos] * scaleY) + height / 2;
                g.DrawLine(drawDataPen, lastX, lastY, x, y);
                lastX = x;
                lastY = y;
            }

            String drawString = dataName;

            // Create font and brush.
            Font drawFont = new Font("Arial", 8, FontStyle.Bold);
            SolidBrush drawBrush = new SolidBrush(Color.Black);

            // Create point for upper-left corner of drawing.
            float fontPosX = 3.0F;
            float fontPosY = 3.0F;
            
            // Set format of string.
            StringFormat drawFormat = new StringFormat();

            // Draw string to screen.
            g.DrawString(drawString, drawFont, drawBrush, fontPosX, fontPosY, drawFormat);
            picCtrl.Image = bmp;
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (btnRecord.Text == "Record")
            {
                btnRecord.Text = "StopRec";
                navigation.TurnLoggingOn();
                recordCnt = 0;
                startRecord = true;
                SW = File.CreateText(fileNme + fileCnt.ToString() + ".txt");
                fileCnt++;
            }
            else
            {
                btnRecord.Text = "Record";
                navigation.TurnLoggingOff();
                startRecord = false;
                if (SW != null)
                    SW.Close();
            }
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            if (clientSocketLaser != null)
                sendCommandLaser(SCANCOMMAND );
            firstData = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            controlMode = MANUAL;
            trackBarForwardPower.Value = 0;
            trackBarTurnPower.Value = 0;
            turnVel = 0;
            forwardVel = 0;

            if (Simulating())
                simulatedJaguar.DcMotorVelocityNonTimeCtrAll(0, 0, 0, 0, 0, 0);
            else
                realJaguar.DcMotorVelocityNonTimeCtrAll(0, 0, 0, 0, 0, 0);

        }

        private void btnDisArm_Click(object sender, EventArgs e)
        {
            if (btnDisArm.Text == "Dis Arm")
            {
                btnDisArm.BackColor = Color.Green;
                realJaguar.DcMotorPwmNonTimeCtrAll((short)INIPWM, (short)INIPWM, (short)INIPWM, (short)INIPWM, (short)INIPWM, (short)INIPWM);
                btnDisArm.Text = "En Arm";
                forceStop = true;
            }
            else
            {
                btnDisArm.BackColor = Color.Red;
                btnDisArm.Text = "Dis Arm";
                forceStop = false;
            }
        }

        private void checkBoxMotorProtect_CheckedChanged(object sender, EventArgs e)
        {
            protectMotorStuck = false;
            protectMotorTemp = false;
        }

        private void checkBoxHardware_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxHardware.Checked)
            {
                navigation.CalibrateIMU();
                experimentMode = HARDWARE;
            }
            else
                experimentMode = SIMULATOR;
            navigation.Reset();
        }

        private void btnTurnOn_Click(object sender, EventArgs e)
        {
            if (clientSocketLaser != null)
            {
                sendCommandLaser("BM");
                btnScan.Enabled = true;
            }
            realJaguar.DcMotorPwmNonTimeCtr(0, 30000);
        }
        
        private void btnReset_Click(object sender, EventArgs e)
        {
            navigation.Reset();
        }
        
        private void btnSetStartPoint_Click(object sender, EventArgs e)
        {
            try
            {
                navigation.desiredX = double.Parse(txtStartLat.Text);
                navigation.desiredY = double.Parse(txtStartLong.Text);
                navigation.desiredT = double.Parse(txtStartTheta.Text);
            }
            catch
            {
            }
            controlMode = AUTONOMOUS;
            AUTOMODE = FLYTOPOINT;
            navigation._accumL = 0;
            navigation._accumR = 0;

            navigation.motionPlanRequired = true;

        }

        # endregion


        private void btn_flytraj_Click(object sender, EventArgs e)
        {
            try
            {
                navigation.desiredX = navigation._trajX.First.Value;
                navigation.desiredY = navigation._trajY.First.Value;
                navigation.desiredT = navigation._trajT.First.Value;

                navigation._trajX.RemoveFirst();
                navigation._trajY.RemoveFirst();
                navigation._trajT.RemoveFirst();
            }
            catch
            {
            }
            AUTOMODE = TRACKTRAJ;
            controlMode = AUTONOMOUS;
            navigation._accumL = 0;
            navigation._accumR = 0;
            
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                navigation.desiredX = double.Parse(txtStartLat.Text);
                navigation.desiredY = double.Parse(txtStartLong.Text);
                navigation.desiredR = double.Parse(txtStartTheta.Text);
            }
            catch
            {
            }
            AUTOMODE = CIRCLE;
            controlMode = AUTONOMOUS;
            navigation._accumL = 0;
            navigation._accumR = 0;
        }

        private void label15_Click(object sender, EventArgs e)
        {

        }


    }
}
